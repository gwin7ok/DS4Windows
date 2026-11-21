using System;
using System.Threading;
using System.Runtime.InteropServices;
using DS4Windows.Core.Logging;
using DS4Windows.Core.Model;
using DS4Windows.Input.Reports;

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
                Haptics.Current.lightbarState.LightBarColor = new DS4Color(0, 0, 0);
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
                DualShock4OutputReportBuilder.FillBluetooth(outReportBuffer, knownGoodBTOutputReportType, btPollRate, outputFeaturesByte, in Haptics.Current);

                fixed (byte* byteR = outputReport, byteB = outReportBuffer)
                {
                    for (int i = 0, arlen = BT_OUTPUT_CHANGE_LENGTH; !change && i < arlen; i++)
                        change = byteR[i] != byteB[i];
                }

                change = change || Haptics.Current.dirty;
                /*if (change)
                {
                    Console.WriteLine("CHANGE: {0} {1} {2} {3} {4} {5}", Haptics.Current.LightBarColor.red, Haptics.Current.LightBarColor.green, Haptics.Current.LightBarColor.blue, Haptics.Current.RumbleMotorStrengthRightLightFast, Haptics.Current.RumbleMotorStrengthLeftHeavySlow, DateTime.Now.ToString());
                }
                */

                haptime = haptime || change;
            }
            else
            {
                DualShock4OutputReportBuilder.FillUsb(outReportBuffer, outputFeaturesByte, in Haptics.Current);

                fixed (byte* byteR = outputReport, byteB = outReportBuffer)
                {
                    for (int i = 0, arlen = USB_OUTPUT_CHANGE_LENGTH; !change && i < arlen; i++)
                        change = byteR[i] != byteB[i];
                }

                change = change || Haptics.Current.dirty;
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

            if (Haptics.AutostopTimer.IsRunning)
            {
                // Workaround to a bug in ViGem driver. Force stop potentially stuck rumble motor on the next output report if there haven't been new rumble events within X seconds
                if (Haptics.AutostopTimer.ElapsedMilliseconds >= Haptics.AutostopTime)
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

            Haptics.Current.dirty = false;
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
