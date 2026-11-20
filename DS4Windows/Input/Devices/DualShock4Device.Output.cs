using System;
using System.Diagnostics;
using System.Threading;
using System.Runtime.InteropServices;
using DS4Windows.Core.Logging;
using DS4Windows.Core.Model;
using DS4Windows.Core.Settings;
using DS4Windows.Input.Abstractions;
using DS4Windows.Input.Hid;
using DS4Windows.Input.Parsers;

namespace DS4Windows.Input.Devices
{
    /// <summary>出力レポートの組み立てと送信。</summary>
    public partial class DualShock4Device
    {
        // TODO: Possibly remove method
        private void CheckOutputReportTypes()
        {
            // Use Tuple here for convenience
            var reportIds = new (byte Id, int Length)[]
            {
                (0x15, BT_OUTPUT_REPORT_0x15_LENGTH),
                (0x11, BT_OUTPUT_REPORT_0x11_LENGTH),
            };

            byte finalReport = 0x00;
            foreach (var element in reportIds)
            {
                int len = element.Length;
                byte[] outputBuffer = new byte[element.Length];
                outputBuffer[0] = element.Id;
                //outputBuffer[1] = (byte)(0xC0 | 0x04);
                outputBuffer[2] = 0xA0;

                // Need to calculate and populate CRC-32 data so controller will accept the report
                uint calcCrc32 = ~Crc32Algorithm.Compute(outputBTCrc32Head);
                calcCrc32 = ~Crc32Algorithm.CalculateBasicHash(ref calcCrc32, ref outputBuffer, 0, len - 4);
                outputBuffer[len - 4] = (byte)calcCrc32;
                outputBuffer[len - 3] = (byte)(calcCrc32 >> 8);
                outputBuffer[len - 2] = (byte)(calcCrc32 >> 16);
                outputBuffer[len - 1] = (byte)(calcCrc32 >> 24);

                if (WriteOutput(outputBuffer))
                {
                    finalReport = element.Id;
                    knownGoodBTOutputReportType = element.Id;
                    outputReport = new byte[len];
                    outReportBuffer = new byte[len];
                    break;
                }
            }

            if (finalReport == 0x00)
            {
                ModifyFeatureSetFlag(VidPidFeatureSet.NoOutputData, true);
            }
        }

        public override void WriteOutputOffOnce()
        {
            if ((featureSet & VidPidFeatureSet.NoOutputData) != 0 || !postInitDone || isDisconnecting)
                return;

            try
            {
                currentHap.lightbarState.LightBarColor = new DS4Color(0, 0, 0);
                SetRumble(0, 0);
                SendOutputReport(true, true, false);
                AppLogger.LogDebug("[DI] WriteOutputOffOnce: wrote lightbar off / rumble zero once before disabling output data");
            }
            catch (Exception ex)
            {
                AppLogger.LogWarn($"[DI] WriteOutputOffOnce failed: {ex.Message}");
            }
        }

        private unsafe void PrepareOutputReportInner(ref bool change, ref bool haptime)
        {
            bool usingBT = conType == ConnectionType.BT;

            if (usingBT && (this.featureSet & VidPidFeatureSet.OnlyOutputData0x05) == 0)
            {
                outReportBuffer[0] = knownGoodBTOutputReportType;
                //outReportBuffer[0] = 0x15;
                //outReportBuffer[1] = (byte)(0x80 | btPollRate); // input report rate
                outReportBuffer[1] = (byte)(0xC0 | btPollRate); // input report rate
                //outReportBuffer[2] = 0xA0;

                // Headphone volume L (0x10), Headphone volume R (0x20), Mic volume (0x40), Speaker volume (0x80)
                // enable rumble (0x01), lightbar (0x02), flash (0x04). Default: 0x07
                outReportBuffer[3] = outputFeaturesByte;
                outReportBuffer[4] = 0x04;

                outReportBuffer[6] = currentHap.rumbleState.RumbleMotorStrengthRightLightFast; // fast motor
                outReportBuffer[7] = currentHap.rumbleState.RumbleMotorStrengthLeftHeavySlow; // slow motor
                outReportBuffer[8] = currentHap.lightbarState.LightBarColor.red; // red
                outReportBuffer[9] = currentHap.lightbarState.LightBarColor.green; // green
                outReportBuffer[10] = currentHap.lightbarState.LightBarColor.blue; // blue
                outReportBuffer[11] = currentHap.lightbarState.LightBarFlashDurationOn; // flash on duration
                outReportBuffer[12] = currentHap.lightbarState.LightBarFlashDurationOff; // flash off duration

                fixed (byte* byteR = outputReport, byteB = outReportBuffer)
                {
                    for (int i = 0, arlen = BT_OUTPUT_CHANGE_LENGTH; !change && i < arlen; i++)
                        change = byteR[i] != byteB[i];
                }

                change = change || currentHap.dirty;
                /*if (change)
                {
                    Console.WriteLine("CHANGE: {0} {1} {2} {3} {4} {5}", currentHap.LightBarColor.red, currentHap.LightBarColor.green, currentHap.LightBarColor.blue, currentHap.RumbleMotorStrengthRightLightFast, currentHap.RumbleMotorStrengthLeftHeavySlow, DateTime.Now.ToString());
                }
                */

                haptime = haptime || change;
            }
            else
            {
                outReportBuffer[0] = 0x05;
                // Headphone volume L (0x10), Headphone volume R (0x20), Mic volume (0x40), Speaker volume (0x80)
                // enable rumble (0x01), lightbar (0x02), flash (0x04). Default: 0x07
                outReportBuffer[1] = outputFeaturesByte;
                outReportBuffer[2] = 0x04;
                outReportBuffer[4] = currentHap.rumbleState.RumbleMotorStrengthRightLightFast; // fast motor
                outReportBuffer[5] = currentHap.rumbleState.RumbleMotorStrengthLeftHeavySlow; // slow  motor
                outReportBuffer[6] = currentHap.lightbarState.LightBarColor.red; // red
                outReportBuffer[7] = currentHap.lightbarState.LightBarColor.green; // green
                outReportBuffer[8] = currentHap.lightbarState.LightBarColor.blue; // blue
                outReportBuffer[9] = currentHap.lightbarState.LightBarFlashDurationOn; // flash on duration
                outReportBuffer[10] = currentHap.lightbarState.LightBarFlashDurationOff; // flash off duration

                fixed (byte* byteR = outputReport, byteB = outReportBuffer)
                {
                    for (int i = 0, arlen = USB_OUTPUT_CHANGE_LENGTH; !change && i < arlen; i++)
                        change = byteR[i] != byteB[i];
                }

                change = change || currentHap.dirty;
                haptime = haptime || change;
            }
        }

        protected override void SendOutputReport(bool synchronous, bool force = false, bool quitOutputThreadOnError = true)
        {
            MergeStates();
            //setTestRumble();
            //setHapticState();

            bool quitOutputThread = false;
            bool usingBT = conType == ConnectionType.BT;

            // Defensive: output buffers should be initialized in PostInit.
            // If they are not yet initialized (race or early call), log and skip to avoid NRE.
            if (outputReport == null || outReportBuffer == null)
            {
                AppLogger.Log(AppLogLevel.Warn, $"sendOutputReport: output buffers not initialized (outputReport={(outputReport == null)}, outReportBuffer={(outReportBuffer == null)})", LogOptions.UserFacing);
                return;
            }

            // Some gamepads don't support lightbar and rumble, so no need to write out anything (writeOut always fails, so DS4Windows would accidentally force quit the gamepad connection).
            // If noOutputData featureSet flag is set then don't try to write out anything to the gamepad device.
            if ((this.featureSet & VidPidFeatureSet.NoOutputData) != 0)
            {
                if (exitOutputThread == false && (IsRemoving || IsRemoved))
                {
                    // Gamepad disconnecting or disconnected. Signal closing of OutputUpdate thread
                    StopOutputUpdate();
                    exitOutputThread = true;
                }

                return;
            }

            //bool output = outputPendCount > 0, change = force;
            bool change = force;
            bool haptime = force || standbySw.ElapsedMilliseconds >= 4000L;

            PrepareOutputReportInner(ref change, ref haptime);

            if (rumbleAutostopTimer.IsRunning)
            {
                // Workaround to a bug in ViGem driver. Force stop potentially stuck rumble motor on the next output report if there haven't been new rumble events within X seconds
                if (rumbleAutostopTimer.ElapsedMilliseconds >= rumbleAutostopTime)
                    SetRumble(0, 0);
            }

            if (synchronous)
            {
                if (haptime)
                {
                    if (change)
                    {
                        standbySw.Reset();
                    }
                    else
                        standbySw.Restart();
                    //standbySw.Restart();

                    if (usingBT)
                    {
                        outReportBuffer.CopyTo(outputReport, 0);

                        if ((this.featureSet & VidPidFeatureSet.OnlyOutputData0x05) == 0)
                        {
                            // Need to calculate and populate CRC-32 data so controller will accept the report
                            //int len = outputReport.Length;
                            int len = btOutputPayloadLen;
                            uint calcCrc32 = ~Crc32Algorithm.Compute(outputBTCrc32Head);
                            calcCrc32 = ~Crc32Algorithm.CalculateBasicHash(ref calcCrc32, ref outputReport, 0, len - 4);
                            outputReport[len - 4] = (byte)calcCrc32;
                            outputReport[len - 3] = (byte)(calcCrc32 >> 8);
                            outputReport[len - 2] = (byte)(calcCrc32 >> 16);
                            outputReport[len - 1] = (byte)(calcCrc32 >> 24);

                            //Console.WriteLine("Write CRC-32 to output report");
                        }
                    }

                    try
                    {
                        if (!WriteOutput())
                        {
                            if (quitOutputThreadOnError)
                            {
                                int winError = Marshal.GetLastWin32Error();

                                // Logfile notification that the gamepad is force disconnected because of WriteOutput failed
                                if (quitOutputThread == false && !isDisconnecting)
                                    AppLogger.LogUser(AppLogLevel.Warn, "Log_Device_WriteConnectionLost", winError);

                                quitOutputThread = true;
                            }
                        }
                    }
                    catch { /* 失敗しても処理を続ける（呼び出し元に影響させない） */ } // If it's dead already, don't worry about it.

                    if (!usingBT)
                    {
                        lock (outReportBuffer)
                        {
                            Monitor.Pulse(outReportBuffer);
                        }
                    }
                }
            }
            else
            {
                //for (int i = 0, arlen = outputReport.Length; !change && i < arlen; i++)
                //    change = outputReport[i] != outReportBuffer[i];

                if (haptime)
                {
                    if (change)
                    {
                        standbySw.Reset();
                    }

                    Monitor.Pulse(outReportBuffer);
                }
            }

            if (quitOutputThread)
            {
                StopOutputUpdate();
                exitOutputThread = true;
            }

            currentHap.dirty = false;
        }

        // Perform outReportBuffer copy on a separate thread to save
        // time on main input thread
        private void OutReportCopy()
        {
            try
            {
                while (!exitOutputThread)
                {
                    // Defensive: if buffers are not initialized, wait briefly and retry
                    if (outReportBuffer == null || outputReport == null)
                    {
                        Thread.Sleep(50); // 待機の理由: 出力スレッドの中。バッファーの初期化を短く待って再試行する
                        continue;
                    }

                    lock (outReportBuffer)
                    {
                        outReportBuffer.CopyTo(outputReport, 0);
                        Monitor.Wait(outReportBuffer);
                    }
                }
            }
            catch (ThreadInterruptedException) { /* 後始末・解放の失敗は無視してよい（解放済み・切断済みでも続ける） */ }
        }

        private void PrepareOutputFeaturesByte()
        {
            if (nativeOptionsStore != null)
            {
                if (nativeOptionsStore.IsCopyCat)
                {
                    outputFeaturesByte = COPYCAT_OUTPUT_FEATURES;
                }
                else
                {
                    outputFeaturesByte = DEFAULT_OUTPUT_FEATURES;
                }
            }
        }
    }
}
