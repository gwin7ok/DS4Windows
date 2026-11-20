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
    /// <summary>入力レポートの受信とフレームの処理。</summary>
    public partial class DualShock4Device
    {
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

                    switch (VerifyBtInputCrc(btInputReport, CrcMaxConsecutiveFailures))
                    {
                        case BtCrcResult.Skipped:
                            return FrameResult.Skip;
                        case BtCrcResult.LimitReached:
                            // The incoming data packet does not have the native DS4 type or CRC-32 checks keep failing. Fail out and disconnect controller.
                            AppLogger.LogUser(AppLogLevel.Warn, "Log_Device_CrcFailedDisconnect", Mac.ToString(), CrcMaxConsecutiveFailures);

                            readWaitEv.Reset();
                            SendOutputReport(true, true); // Kick Windows into noticing the disconnection.
                            StopAndRemove();
                            return FrameResult.Terminate;
                    }
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
                    SendOutputReport(true, true); // Kick Windows into noticing the disconnection.
                    StopAndRemove();
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
                    StopAndRemove();
                    return FrameResult.Terminate;
                }

                rawReceivedTime = Stopwatch.GetTimestamp();
            }

            readWaitEv.Wait();
            readWaitEv.Reset();
            return FrameResult.Accept;
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
                    int tempBattery = BatteryScale.ToPercent(tempByte & 0x0f, maxBatteryValue);
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

                // XXX ControllerState mapping needs fixup, turn touches into an array[4] of structs.  And include the touchpad details there instead.
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
                            touchpad.HandleTouchpad(inputReport, cState, DS4Touchpad.DS4_TOUCHPAD_DATA_OFFSET, touchOffset);
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
                        sixAxis.HandleSixaxis(pbGyro, pbAccel, cState, elapsedDeltaTime);
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
    }
}
