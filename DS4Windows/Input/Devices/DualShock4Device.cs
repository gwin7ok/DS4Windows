using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using DS4Windows.Core.Logging;
using DS4Windows.Core.Model;
using DS4Windows.Input.Abstractions;
using DS4Windows.Input.Hid;
using DS4Windows.Input.Parsers;

namespace DS4Windows.Input.Devices
{
    /// <summary>
    /// DualShock 4（USB／Bluetooth／ワイヤレスアダプター）。読み取りの骨組み（<see cref="InputLoopDevice"/>）の差し込み口で、
    /// DS4 のレポート（BT は 0x11＋CRC32）の検証、電池、タイムスタンプ、タッチパッド、動き、アダプターの同期、アイドル切断を書く。
    /// </summary>
    public class DualShock4Device : InputLoopDevice
    {
        private static readonly IInputReportParser ReportParser = new Ds4ReportParser();
        private const int CRC32_NUM_ATTEMPTS = 10;

        private readonly TimestampClock timestampClock = new TimestampClock(ushort.MaxValue, 16u);
        private long rawReceivedTime;
        private bool forceWrite;
        private int crcOffset;
        private int crcPos = BT_INPUT_REPORT_CRC32_POS;

        public DualShock4Device(IHidTransport hidDevice, string displayName, VidPidFeatureSet featureSet = VidPidFeatureSet.DefaultDS4)
            : base(hidDevice, displayName, featureSet)
        {
        }

        protected override void OnLoopStarting()
        {
            timeoutEvent = false;
            forceWrite = false;
            crcOffset = 0;
            crcPos = BT_INPUT_REPORT_CRC32_POS;

            // Run continuous calibration on Gyro when starting input loop
            sixAxis.ResetContinuousCalibration();
            standbySw.Start();
        }

        // 切断のときは、出力を止めて Removal を通知し、スレッドを抜ける（exitInputThread は立てない）
        protected override unsafe FrameResult ReceiveFrame()
        {
            // Sony DS4 and compatible gamepads send data packets with 0x11 type code in BT mode.
            if (conType == ConnectionType.BT && (this.featureSet & VidPidFeatureSet.OnlyInputData0x01) == 0)
            {
                HidReadStatus res = hDevice.ReadFile(btInputReport);
                if (res == HidReadStatus.Success)
                {
                    rawReceivedTime = Stopwatch.GetTimestamp();
                    timeoutEvent = false;

                    fixed (byte* byteP = &btInputReport[2], imp = inputReport)
                    {
                        for (int j = 0; j < BT_INPUT_REPORT_LENGTH - 2; j++)
                        {
                            imp[j] = byteP[j];
                        }
                    }

                    uint recvCrc32 = btInputReport[BT_INPUT_REPORT_CRC32_POS] |
                        (uint)(btInputReport[BT_INPUT_REPORT_CRC32_POS + 1] << 8) |
                        (uint)(btInputReport[BT_INPUT_REPORT_CRC32_POS + 2] << 16) |
                        (uint)(btInputReport[BT_INPUT_REPORT_CRC32_POS + 3] << 24);

                    uint calcCrc32 = ~Crc32Algorithm.CalculateFasterBT78Hash(ref HamSeed, ref btInputReport, ref crcOffset, ref crcPos);
                    if (recvCrc32 != calcCrc32)
                    {
                        cState.PacketCounter = pState.PacketCounter + 1; //still increase so we know there were lost packets

                        // If the incoming data packet does not have the native DS4 type or CRC-32 checks keep failing. Fail out and disconnect controller.
                        if (this.inputReportErrorCount >= CRC32_NUM_ATTEMPTS)
                        {
                            AppLogger.LogUser(AppLogLevel.Warn, "Log_Device_CrcFailedDisconnect", Mac.ToString(), CRC32_NUM_ATTEMPTS);

                            readWaitEv.Reset();
                            sendOutputReport(true, true); // Kick Windows into noticing the disconnection.
                            StopOutputUpdate();
                            isDisconnecting = true;
                            RunRemoval();

                            timeoutExecuted = true;
                            return FrameResult.Terminate;
                        }

                        this.inputReportErrorCount++;
                        readWaitEv.Reset();
                        return FrameResult.Skip;
                    }

                    this.inputReportErrorCount = 0;
                }
                else
                {
                    if (res == HidReadStatus.WaitTimedOut)
                    {
                        AppLogger.LogUser(AppLogLevel.Warn, "Log_Device_DisconnectedTimeout", Mac.ToString());
                    }
                    else
                    {
                        int winError = Marshal.GetLastWin32Error();
                        AppLogger.LogDebug($"{Mac} {DateTime.UtcNow.ToString("o")}> disconnect due to read failure: {winError.ToString("x8")}");
                        AppLogger.LogUser(AppLogLevel.Warn, "Log_Device_DisconnectedReadFailure", Mac.ToString(), winError);
                    }

                    readWaitEv.Reset();
                    sendOutputReport(true, true); // Kick Windows into noticing the disconnection.
                    StopOutputUpdate();
                    isDisconnecting = true;
                    RunRemoval();

                    timeoutExecuted = true;
                    return FrameResult.Terminate;
                }
            }
            else
            {
                HidReadStatus res = hDevice.ReadFile(inputReport);
                if (res != HidReadStatus.Success)
                {
                    if (res == HidReadStatus.WaitTimedOut)
                    {
                        AppLogger.LogUser(AppLogLevel.Warn, "Log_Device_DisconnectedTimeout", Mac.ToString());
                    }
                    else
                    {
                        int winError = Marshal.GetLastWin32Error();
                        AppLogger.LogDebug($"{Mac} {DateTime.UtcNow.ToString("o")}> disconnect due to read failure: {winError.ToString("x8")}");
                    }

                    readWaitEv.Reset();
                    StopOutputUpdate();
                    isDisconnecting = true;
                    RunRemoval();

                    timeoutExecuted = true;
                    return FrameResult.Terminate;
                }

                rawReceivedTime = Stopwatch.GetTimestamp();
            }

            readWaitEv.Wait();
            readWaitEv.Reset();
            return FrameResult.Accept;
        }

        // フレームの時刻は、読み取りが返った直後の時刻（待機中の印の解除を待つ前）を使う
        protected override long GetFrameTicks()
        {
            lastInputReportTimestamp = rawReceivedTime; // 受信開始時刻を保持
            return rawReceivedTime;
        }

        // BT は 0x11 のレポートだけを扱う
        protected override bool IsExpectedReport()
        {
            return !(conType == ConnectionType.BT && btInputReport[0] != 0x11 && (this.featureSet & VidPidFeatureSet.OnlyInputData0x01) == 0);
        }

        protected override unsafe void ProcessFrame()
        {
            unchecked
            {
                ReportParser.Parse(inputReport, cState);

                if ((this.featureSet & VidPidFeatureSet.NoBatteryReading) == 0)
                {
                    byte tempByte = inputReport[30];
                    bool tempCharging = (tempByte & 0x10) != 0;
                    UpdateCharging(tempCharging);

                    int maxBatteryValue = charging ? BATTERY_MAX_USB : BATTERY_MAX;
                    int tempBattery = (tempByte & 0x0f) * 100 / maxBatteryValue;
                    tempBattery = Math.Min(tempBattery, 100);
                    UpdateBattery(tempBattery);

                    cState.Battery = (byte)battery;
                    if (tempByte != priorInputReport30)
                    {
                        priorInputReport30 = tempByte;
                    }
                }
                else
                {
                    // Some gamepads don't send battery values in DS4 compatible data fields, so use dummy 99% value to avoid constant low battery warnings
                    priorInputReport30 = 0x0F;
                    battery = 99;
                    cState.Battery = 99;
                }

                uint tempStamp = (uint)((ushort)(inputReport[11] << 8) | inputReport[10]);
                timestampClock.Advance(tempStamp, lastTimeElapsedDouble * .001, pState.totalMicroSec, out double elapsedDeltaTime, out ulong clockTotalMicroSec);
                cState.totalMicroSec = clockTotalMicroSec;

                cState.elapsedTime = elapsedDeltaTime;
                cState.ds4Timestamp = (ushort)tempStamp;

                if (conType == ConnectionType.SONYWA)
                {
                    bool controllerSynced = inputReport[31] == 0;
                    if (controllerSynced != synced)
                    {
                        runCalib = synced = controllerSynced;
                        RaiseSyncChange();
                        if (synced)
                        {
                            forceWrite = true;
                            sixAxis.ResetContinuousCalibration();
                        }
                        else
                        {
                            standbySw.Reset();
                            sixAxis.StopContinuousCalibration();
                        }
                    }
                }

                // XXX DS4State mapping needs fixup, turn touches into an array[4] of structs.  And include the touchpad details there instead.
                try
                {
                    // Only care if one touch packet is detected. Other touch packets
                    // don't seem to contain relevant data. ds4drv does not use them either.
                    for (int touches = Math.Max((int)(inputReport[-1 + DS4Touchpad.DS4_TOUCHPAD_DATA_OFFSET - 1]), 1), touchOffset = 0; touches > 0; touches--, touchOffset += 9)
                    {
                        cState.TouchPacketCounter = inputReport[-1 + DS4Touchpad.DS4_TOUCHPAD_DATA_OFFSET + touchOffset];
                        cState.Touch1 = (inputReport[0 + DS4Touchpad.DS4_TOUCHPAD_DATA_OFFSET + touchOffset] >> 7) != 0 ? false : true; // finger 1 detected
                        cState.Touch1Identifier = (byte)(inputReport[0 + DS4Touchpad.DS4_TOUCHPAD_DATA_OFFSET + touchOffset] & 0x7f);
                        cState.Touch2 = (inputReport[4 + DS4Touchpad.DS4_TOUCHPAD_DATA_OFFSET + touchOffset] >> 7) != 0 ? false : true; // finger 2 detected
                        cState.Touch2Identifier = (byte)(inputReport[4 + DS4Touchpad.DS4_TOUCHPAD_DATA_OFFSET + touchOffset] & 0x7f);
                        cState.Touch1Finger = cState.Touch1 || cState.Touch2; // >= 1 touch detected
                        cState.Touch2Fingers = cState.Touch1 && cState.Touch2; // 2 touches detected
                        int touchX = (((inputReport[2 + DS4Touchpad.DS4_TOUCHPAD_DATA_OFFSET + touchOffset] & 0xF) << 8) | inputReport[1 + DS4Touchpad.DS4_TOUCHPAD_DATA_OFFSET + touchOffset]);
                        cState.TouchLeft = touchX >= DS4Touchpad.RESOLUTION_X_MAX * 2 / 5 ? false : true;
                        cState.TouchRight = touchX < DS4Touchpad.RESOLUTION_X_MAX * 2 / 5 ? false : true;
                        // Even when idling there is still a touch packet indicating no touch 1 or 2
                        if (synced)
                        {
                            touchpad.handleTouchpad(inputReport, cState, DS4Touchpad.DS4_TOUCHPAD_DATA_OFFSET, touchOffset);
                        }
                    }
                }
                catch (Exception ex) { frameError = $"Touchpad: {ex.Message}"; }

                // Store Gyro and Accel values. Use pointers here as it seems faster than using Array.Copy
                fixed (byte* pbInput = &inputReport[13], pbGyro = gyro, pbAccel = accel)
                {
                    for (int i = 0; i < 6; i++)
                    {
                        pbGyro[i] = pbInput[i];
                    }

                    for (int i = 6; i < 12; i++)
                    {
                        pbAccel[i - 6] = pbInput[i];
                    }

                    if (synced)
                    {
                        sixAxis.handleSixaxis(pbGyro, pbAccel, cState, elapsedDeltaTime);
                    }
                }

                ds4InactiveFrame = cState.FrameCounter == pState.FrameCounter;
                if (!ds4InactiveFrame)
                {
                    isRemoved = false;
                }
            }
        }

        protected override bool ProcessIdle()
        {
            if (EvaluateIdleDisconnect(utcNow))
            {
                if (conType == ConnectionType.BT)
                {
                    if (DisconnectBT(true))
                    {
                        timeoutExecuted = true;
                        return true; // all done
                    }
                }
                else if (conType == ConnectionType.SONYWA)
                {
                    DisconnectDongle();
                }
            }

            return false;
        }

        protected override void WriteOutputFrame()
        {
            sendOutputReport(true, forceWrite);
            forceWrite = false;
        }
    }
}
