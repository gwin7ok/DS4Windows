/*
DS4Windows
Copyright (C) 2023  Travis Nickles

This program is free software: you can redistribute it and/or modify
it under the terms of the GNU General Public License as published by
the Free Software Foundation, either version 3 of the License, or
(at your option) any later version.

This program is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
GNU General Public License for more details.

You should have received a copy of the GNU General Public License
along with this program.  If not, see <https://www.gnu.org/licenses/>.
*/

using System;
using System.Runtime.InteropServices;
using DS4Windows.Core.Logging;
using DS4Windows.Core.Model;
using DS4Windows.Core.Settings;
using DS4Windows.Input.Hid;
using DS4Windows.Input.Abstractions;

namespace DS4Windows.Input.Devices
{
    public partial class DualSenseDevice : InputLoopDevice
    {
        public class InputReportDataBytesUSB : InputReportDataBytes
        {
        }

        public enum DeviceSubType : ushort
        {
            DualSense,
            DSEdge,
        }

        private const int BT_REPORT_OFFSET = 2;
        private InputReportDataBytes dataBytes = null!; // 初期化（Open）で、接続の種類に合わせて設定する
        protected new const int BT_OUTPUT_REPORT_LENGTH = 78;
        private new const int BT_INPUT_REPORT_LENGTH = 78;
        protected const int TOUCHPAD_DATA_OFFSET = 33;
        private new const int BATTERY_MAX = 8;

        public new const byte SERIAL_FEATURE_ID = 9;
        public override byte SerialReportID { get => SERIAL_FEATURE_ID; }

        private const byte OUTPUT_REPORT_ID_USB = 0x02;
        private const byte OUTPUT_REPORT_ID_BT = 0x31;
        private const byte OUTPUT_REPORT_ID_DATA = 0x02;
        private new const byte USB_OUTPUT_CHANGE_LENGTH = 48;
        private const int OUTPUT_MIN_COUNT_BT = 20;
        private const byte LED_PLAYER_BAR_TOGGLE = 0x10;
        private const int FEATURE_FIRMWARE_INFO_ID = 0x20;
        private readonly TimestampClock timestampClock = new TimestampClock(uint.MaxValue, 1u);
        private bool outputDirty = false;
        private DS4HapticState previousHapticState = new DS4HapticState();
        private byte[] outputBTCrc32Head = new byte[] { 0xA2 };
        //private byte outputPendCount = 0;
        private new GyroMouseSensDualSense gyroMouseSensSettings = null!; // 初期化で設定する
        public override GyroMouseSens GyroMouseSensSettings { get => gyroMouseSensSettings; }

        private byte activePlayerLEDMask = 0x00;

        private byte hapticPowerLevel = (byte)HapticPowerLevelFriendlyName.Str100;
        public byte HapticPowerLevel
        {
            get => hapticPowerLevel;
            set => hapticPowerLevel = value;
        }

        protected bool useRumble = true;
        public bool UseRumble { get => useRumble; set => useRumble = value; }

        // Accurate rumble emulation mode requires 2.24 firmware or newer. On official hardware it takes priority over normal/legacy rumble
        protected bool useAccurateRumble = true; 
        public bool UseAccurateRumble { get => useAccurateRumble; set => useAccurateRumble = value; }

        public override bool UsesGenericRumbleRescaleSetting => true;

        public override bool ApplyRumbleEmulation(RumbleEmulationMode mode, byte hapticPowerLevel)
        {
            switch (mode)
            {
                case RumbleEmulationMode.Disabled:
                    UseRumble = false;
                    UseAccurateRumble = false;
                    break;
                case RumbleEmulationMode.Legacy:
                    UseRumble = true;
                    UseAccurateRumble = false;
                    break;
                case RumbleEmulationMode.Accurate:
                default:
                    UseRumble = true;
                    UseAccurateRumble = true;
                    break;
            }

            HapticPowerLevel = hapticPowerLevel;
            return true;
        }

        private TriggerEffectData l2EffectData;
        private TriggerEffectData r2EffectData;

        private byte muteLEDByte = 0x00;
        private uint hwVersion;
        private uint fwVersion;
        private uint updateVersion;
        private DeviceSubType subType = DeviceSubType.DualSense;
        public DeviceSubType SubType => subType;

        private DualSenseControllerOptions nativeOptionsStore = null!; // 初期化で設定する
        public DualSenseControllerOptions NativeOptionsStore { get => nativeOptionsStore; }

        public DualSenseDevice(IHidTransport hidDevice, string disName, VidPidFeatureSet featureSet = VidPidFeatureSet.DefaultDS4) :
            base(hidDevice, disName, featureSet)
        {
            synced = true;
            DeviceSlotNumberChanged += (sender, e) => {
                CalculateDeviceSlotMask();
            };

            BatteryChanged += (sender, e) =>
            {
                PreparePlayerLEDBarByte();
            };
        }

        private int DSFeatureVersion(int major, int minor)
        {
            return ((major & 0xFF) << 8 | (minor & 0xFF));
        }

        public override bool DisconnectBT(bool callRemoval = false)
        {
            return base.DisconnectBT(callRemoval);
        }

        public override bool DisconnectDongle(bool remove = false)
        {
            // Do Nothing
            return true;
        }

        public override bool DisconnectWireless(bool callRemoval = false)
        {
            return base.DisconnectWireless(callRemoval);
        }

        public override bool IsAlive()
        {
            return synced;
        }

        private int reportOffset;
        private IInputReportParser reportParser = null!; // 接続の種類が決まる初期化で設定する

        // 切断のときは、独自のログと手順（BT は CRC の検証つき）で止める
        protected override FrameResult ReceiveFrame()
        {
            if (conType == ConnectionType.BT)
            {
                timeoutEvent = false;
                HidReadStatus res = hDevice.ReadFile(inputReport);
                if (res == HidReadStatus.Success)
                {
                    switch (VerifyBtInputCrc(inputReport, CrcMaxConsecutiveFailures))
                    {
                        case BtCrcResult.Skipped:
                            return FrameResult.Skip;
                        case BtCrcResult.LimitReached:
                            AppLogger.LogUser(AppLogLevel.Warn, "Log_Device_CrcFailedDisconnect", Mac.ToString(), CrcMaxConsecutiveFailures);

                            exitInputThread = true;
                            readWaitEv.Reset();
                            KickWindowsToNoticeDisconnect();
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

                    StopAfterReadFailure(kickWindows: true);
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

                    StopAfterReadFailure(kickWindows: false);
                    return FrameResult.Terminate;
                }
            }

            readWaitEv.Wait();
            readWaitEv.Reset();
            return FrameResult.Accept;
        }

        private void StopAfterReadFailure(bool kickWindows)
        {
            exitInputThread = true;
            readWaitEv.Reset();
            if (kickWindows) KickWindowsToNoticeDisconnect();
            StopAndRemove();
        }

        // BT は 0x31 のレポートだけを扱う
        protected override bool IsExpectedReport() => conType != ConnectionType.BT || inputReport[0] == 0x31;

        protected override unsafe void ProcessFrame()
        {
            unchecked
            {
                reportParser.Parse(inputReport, cState);

                if ((this.featureSet & VidPidFeatureSet.NoBatteryReading) == 0)
                {
                    byte tempByte = inputReport[54 + reportOffset];
                    bool tempCharging = (tempByte & 0x08) != 0;
                    UpdateCharging(tempCharging);

                    tempByte = inputReport[53 + reportOffset];
                    bool tempFull = (tempByte & 0x20) != 0; // Check for Full status
                    int tempBattery;
                    if (tempFull)
                    {
                        // Full Charge flag found
                        tempBattery = 100;
                    }
                    else
                    {
                        // Partial charge
                        tempBattery = BatteryScale.ToPercent(tempByte & 0x0F, BATTERY_MAX);
                    }

                    UpdateBattery(tempBattery);

                    cState.Battery = (byte)battery;
                }
                else
                {
                    // Some gamepads don't send battery values in DS4 compatible data fields, so use dummy 99% value to avoid constant low battery warnings
                    battery = 99;
                    cState.Battery = 99;
                }

                uint tempStamp = inputReport[28 + reportOffset] |
                            (uint)(inputReport[29 + reportOffset] << 8) |
                            (uint)(inputReport[30 + reportOffset] << 16) |
                            (uint)(inputReport[31 + reportOffset] << 24);

                timestampClock.Advance(tempStamp, lastTimeElapsedDouble * .001, pState.totalMicroSec, out double elapsedDeltaTime, out ulong clockTotalMicroSec);
                cState.totalMicroSec = clockTotalMicroSec;
                cState.elapsedTime = elapsedDeltaTime;
                cState.ds4Timestamp = (ushort)((tempStamp / 16) % ushort.MaxValue);

                // XXX ControllerState mapping needs fixup, turn touches into an array[4] of structs.  And include the touchpad details there instead.
                try
                {
                    // Only care if one touch packet is detected. Other touch packets
                    // don't seem to contain relevant data. ds4drv does not use them either.
                    int touchOffset = 0;

                    // TouchPacketCounter is at the end of the Touchpad payload with the DualSense
                    cState.TouchPacketCounter = inputReport[8 + TOUCHPAD_DATA_OFFSET + reportOffset + touchOffset];
                    cState.Touch1 = (inputReport[0 + TOUCHPAD_DATA_OFFSET + reportOffset + touchOffset] >> 7) != 0 ? false : true; // finger 1 detected
                    cState.Touch1Identifier = (byte)(inputReport[0 + TOUCHPAD_DATA_OFFSET + reportOffset + touchOffset] & 0x7f);
                    cState.Touch2 = (inputReport[4 + TOUCHPAD_DATA_OFFSET + reportOffset + touchOffset] >> 7) != 0 ? false : true; // finger 2 detected
                    cState.Touch2Identifier = (byte)(inputReport[4 + TOUCHPAD_DATA_OFFSET + reportOffset + touchOffset] & 0x7f);
                    cState.Touch1Finger = cState.Touch1 || cState.Touch2; // >= 1 touch detected
                    cState.Touch2Fingers = cState.Touch1 && cState.Touch2; // 2 touches detected
                    int touchX = (((inputReport[2 + TOUCHPAD_DATA_OFFSET + reportOffset + touchOffset] & 0xF) << 8) | inputReport[1 + TOUCHPAD_DATA_OFFSET + reportOffset + touchOffset]);
                    cState.TouchLeft = touchX >= DS4Touchpad.RESOLUTION_X_MAX * 2 / 5 ? false : true;
                    cState.TouchRight = touchX < DS4Touchpad.RESOLUTION_X_MAX * 2 / 5 ? false : true;
                    // Even when idling there is still a touch packet indicating no touch 1 or 2
                    if (synced)
                    {
                        touchpad.HandleTouchpad(inputReport, cState, TOUCHPAD_DATA_OFFSET + reportOffset, touchOffset);
                    }
                }
                catch (Exception ex) { frameError = $"Touchpad: {ex.Message}"; }

                fixed (byte* pbInput = &inputReport[16 + reportOffset], pbGyro = gyro, pbAccel = accel)
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
            }
        }

        protected override bool ProcessIdle()
        {
            if (EvaluateIdleDisconnect(utcNow) && conType == ConnectionType.BT && DisconnectBT(true))
            {
                exitInputThread = true;
                timeoutExecuted = true;
                return true; // all done
            }

            return false;
        }

        protected override void StopOutputUpdate()
        {
            SendEmptyOutputReport();
        }

        private void Detach()
        {
            SendEmptyOutputReport();
        }

        public override void CheckControllerNumDeviceSettings(int numControllers)
        {
            if (nativeOptionsStore != null)
            {
                if (nativeOptionsStore.LedMode ==
                    DualSenseControllerOptions.LEDBarMode.MultipleControllers)
                {
                    if (numControllers > 1)
                    {
                        activePlayerLEDMask = deviceSlotMask;
                    }
                    else
                    {
                        activePlayerLEDMask = 0x00;
                    }
                }
            }

            QueueEvent(() =>
            {
                outputDirty = true;
                //PrepareOutReport();
            });
        }

        private void SetupOptionsEvents()
        {
            if (nativeOptionsStore != null)
            {
                nativeOptionsStore.MuteLedModeChanged += (sender, e) =>
                {
                    PrepareMuteLEDByte();
                    QueueEvent(() => { outputDirty = true; });
                };

                nativeOptionsStore.LedModeChanged += (sender, e) =>
                {
                    PreparePlayerLEDBarByte();
                    QueueEvent(() => { outputDirty = true; });
                };
            }
        }

        public override void LoadStoreSettings()
        {
            if (nativeOptionsStore != null)
            {
                PrepareMuteLEDByte();
                PreparePlayerLEDBarByte();
            }
        }
    }
}
