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

#nullable enable

using System;
using System.Runtime.InteropServices;
using System.Threading;
using DS4Windows.Core.Logging;
using DS4Windows.Core.Model;
using DS4Windows.Core.Settings;
using DS4Windows.Input.Hid;
using DS4Windows.Input.Reports;
using DS4Windows.Input.Abstractions;
using DS4Windows.Input.Parsers;

namespace DS4Windows.Input.Devices
{
    public class DualSenseDevice : InputLoopDevice
    {
        public class GyroMouseSensDualSense : GyroMouseSens
        {
            private const double MOUSE_COEFFICIENT = 0.009;
            private const double MOUSE_OFFSET = 0.15;
            private const double SMOOTH_MOUSE_OFFSET = 0.15;

            public GyroMouseSensDualSense() : base()
            {
                mouseCoefficient = MOUSE_COEFFICIENT;
                mouseOffset = MOUSE_OFFSET;
                mouseSmoothOffset = SMOOTH_MOUSE_OFFSET;
            }
        }

        public abstract class InputReportDataBytes
        {
            public const int REPORT_OFFSET = 0;

            public const int REPORT_ID = 0;
            public const int LX = 1;
            public const int LY = 2;
        }

        public class InputReportDataBytesUSB : InputReportDataBytes
        {
        }

        public class InputReportDataBytesBT : InputReportDataBytesUSB
        {
            public new const int REPORT_OFFSET = 2;

            public new const int REPORT_ID = InputReportDataBytes.REPORT_ID;
            public new const int LX = InputReportDataBytes.LX + REPORT_OFFSET;
            public new const int LY = InputReportDataBytes.LY + REPORT_OFFSET;
        }

        public struct TriggerEffectData
        {
            public byte triggerMotorMode;
            public byte triggerStartResistance;
            public byte triggerEffectForce;
            public byte triggerRangeForce;
            public byte triggerNearReleaseStrength;
            public byte triggerNearMiddleStrength;
            public byte triggerPressedStrength;
            public byte triggerActuationFrequency;

            public void ChangeData(TriggerEffects effect, TriggerEffectSettings effectSettings)
            {
                switch (effect)
                {
                    case TriggerEffects.None:
                        triggerMotorMode = triggerStartResistance = triggerEffectForce =
                            triggerRangeForce = triggerNearReleaseStrength = triggerNearMiddleStrength =
                            triggerPressedStrength = triggerActuationFrequency = 0;
                        break;
                    case TriggerEffects.FullClick:
                        int tempStartResValue = Math.Max((int)effectSettings.maxValue, 0);
                        //Debug.WriteLine(tempStartResValue);
                        triggerMotorMode = 0x02;
                        //triggerStartResistance = 0x94;
                        triggerStartResistance = (byte)(0x94 * (tempStartResValue / 255.0));
                        //triggerEffectForce = 0xB4;
                        triggerEffectForce = (byte)((0xB4 - triggerStartResistance) * (effectSettings.maxValue / 255.0) + triggerStartResistance);
                        //Debug.WriteLine(triggerEffectForce);
                        triggerRangeForce = 0xFF;
                        triggerNearReleaseStrength = 0x00;
                        triggerNearMiddleStrength = 0x00;
                        triggerPressedStrength = 0x00;
                        triggerActuationFrequency = 0x00;
                        break;
                    case TriggerEffects.Rigid:
                        triggerMotorMode = 0x01;
                        triggerStartResistance = 0x00;
                        triggerEffectForce = 0x00;
                        triggerRangeForce = 0x00;
                        triggerNearReleaseStrength = 0x00;
                        triggerNearMiddleStrength = 0x00;
                        triggerPressedStrength = 0x00;
                        triggerActuationFrequency = 0x00;
                        break;
                    case TriggerEffects.Pulse:
                        triggerMotorMode = 0x02;
                        triggerStartResistance = 0x00;
                        triggerEffectForce = 0x00;
                        triggerRangeForce = 0x00;
                        triggerNearReleaseStrength = 0x00;
                        triggerNearMiddleStrength = 0x00;
                        triggerPressedStrength = 0x00;
                        triggerActuationFrequency = 0x00;
                        break;
                    default:
                        break;
                }
            }
        }

   
        public enum HapticPowerLevelFriendlyName : ushort
        {
            Str100 = 0,
            Str87 = 1,
            Str75 = 2,
            Str62 = 3,
            Str50 = 4,
            Str37 = 5,
            Str25 = 6,
            Str12 = 7,
        }

        public enum DeviceSubType : ushort
        {
            DualSense,
            DSEdge,
        }
        
        private const int BT_REPORT_OFFSET = 2;
        private InputReportDataBytes dataBytes;
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
        private new GyroMouseSensDualSense gyroMouseSensSettings;
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

        private DualSenseControllerOptions nativeOptionsStore;
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

        public override void PostInit()
        {
            IHidTransport hidDevice = hDevice;
            deviceType = InputDeviceType.DualSense;
            DetermineSubType(hidDevice);

            gyroMouseSensSettings = new GyroMouseSensDualSense();
            optionsStore = nativeOptionsStore = new DualSenseControllerOptions(deviceType);
            SetupOptionsEvents();

            conType = DetermineConnectionType(hDevice);
            Mac = hDevice.ReadSerial(SerialReportID);

            if (conType == ConnectionType.USB)
            {
                dataBytes = new InputReportDataBytesUSB();

                inputReport = new byte[64];
                outputReport = new byte[hDevice.Capabilities.OutputReportByteLength];
                outReportBuffer = new byte[hDevice.Capabilities.OutputReportByteLength];

                warnInterval = WARN_INTERVAL_USB;
            }
            else
            {
                //btInputReport = new byte[BT_INPUT_REPORT_LENGTH];
                //inputReport = new byte[BT_INPUT_REPORT_LENGTH - 2];
                // Only plan to use one input report array. Avoid copying data
                inputReport = new byte[BT_INPUT_REPORT_LENGTH];
                // Default DS4 logic while writing data to gamepad
                outputReport = new byte[BT_OUTPUT_REPORT_LENGTH];
                outReportBuffer = new byte[BT_OUTPUT_REPORT_LENGTH];

                warnInterval = WARN_INTERVAL_BT;
                synced = isValidSerial();
            }

            if (runCalib)
                RefreshCalibration();

            // Attempt to grab hardware, firmware, and update version
            // data from DualSense controller. Referenced hid-playstation Linux
            // driver
            byte[] firmwareInfoData = new byte[64];
            firmwareInfoData[0] = FEATURE_FIRMWARE_INFO_ID;
            bool featureFirmRead = false;
            if (conType == ConnectionType.BT)
            {
                featureFirmRead = ReadBTFeatureReport(firmwareInfoData, 64);
            }
            else
            {
                featureFirmRead = hDevice.readFeatureData(firmwareInfoData);
            }

            if (featureFirmRead)
            {
                hwVersion = firmwareInfoData[24] |
                    (uint)(firmwareInfoData[25] << 8) |
                    (uint)(firmwareInfoData[26] << 16) |
                    (uint)(firmwareInfoData[27] << 24);

                fwVersion = firmwareInfoData[28] |
                    (uint)(firmwareInfoData[29] << 8) |
                    (uint)(firmwareInfoData[30] << 16) |
                    (uint)(firmwareInfoData[31] << 24);

                updateVersion = firmwareInfoData[44] | (uint)(firmwareInfoData[45] << 8);

                // Accurate rumble defaults to true. Made device default to false if
                // grabbed update version is too old
                int versionCheckAccurate = DSFeatureVersion(2, 21);
                if (updateVersion < versionCheckAccurate)
                {
                    useAccurateRumble = false;
                }
            }

            // Need to blank LED lights so lightbar will change colors
            // as requested
            if (conType == ConnectionType.BT)
            {
                SendInitialBTOutputReport();
            }
        }

        private bool ReadBTFeatureReport(byte[] buffer, int size)
        {
            bool result = true;
            bool found = false;
            int crc32Pos = size - 4;
            for (int tries = 0; !found && tries < 5; tries++)
            {
                hDevice.readFeatureData(buffer);
                uint recvCrc32 = buffer[crc32Pos] |
                                (uint)(buffer[crc32Pos + 1] << 8) |
                                (uint)(buffer[crc32Pos + 2] << 16) |
                                (uint)(buffer[crc32Pos + 3] << 24);

                uint calcCrc32 = ~Crc32Algorithm.Compute(new byte[] { 0xA3 });
                calcCrc32 = ~Crc32Algorithm.CalculateBasicHash(ref calcCrc32, ref buffer, 0, crc32Pos);
                bool validCrc = recvCrc32 == calcCrc32;
                if (!validCrc && tries >= 5)
                {
                    AppLogger.LogUser(AppLogLevel.Warn, "Log_Device_FeatureReportReadFailure");
                    continue;
                }
                else if (validCrc)
                {
                    found = true;
                }
            }

            result = found;
            return result;
        }

        private int DSFeatureVersion(int major, int minor)
        {
            return ((major & 0xFF) << 8 | (minor & 0xFF));
        }

        private void DetermineSubType(IHidTransport hidDevice)
        {
            subType = DeviceSubType.DualSense;
            if (hidDevice.Attributes.VendorId == InputDeviceRegistry.SONY_VID &&
                hidDevice.Attributes.ProductId == 0x0DF2)
            {
                subType = DeviceSubType.DSEdge;
            }
        }

        public static ConnectionType DetermineConnectionType(IHidTransport hidDevice)
        {
            ConnectionType result;
            if (hidDevice.Capabilities.InputReportByteLength == 64)
            {
                result = ConnectionType.USB;
            }
            else
            {
                result = ConnectionType.BT;
            }

            return result;
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

        public override void RefreshCalibration()
        {
            byte[] calibration = new byte[41];
            calibration[0] = conType == ConnectionType.BT ? (byte)0x05 : (byte)0x05;

            if (conType == ConnectionType.BT)
            {
                bool found = false;
                for (int tries = 0; !found && tries < 5; tries++)
                {
                    hDevice.readFeatureData(calibration);
                    uint recvCrc32 = calibration[DS4_FEATURE_REPORT_5_CRC32_POS] |
                                (uint)(calibration[DS4_FEATURE_REPORT_5_CRC32_POS + 1] << 8) |
                                (uint)(calibration[DS4_FEATURE_REPORT_5_CRC32_POS + 2] << 16) |
                                (uint)(calibration[DS4_FEATURE_REPORT_5_CRC32_POS + 3] << 24);

                    uint calcCrc32 = ~Crc32Algorithm.Compute(new byte[] { 0xA3 });
                    calcCrc32 = ~Crc32Algorithm.CalculateBasicHash(ref calcCrc32, ref calibration, 0, DS4_FEATURE_REPORT_5_LEN - 4);
                    bool validCrc = recvCrc32 == calcCrc32;
                    if (!validCrc && tries >= 5)
                    {
                        AppLogger.LogUser(AppLogLevel.Warn, "Log_Device_GyroCalibrationFailed");
                        continue;
                    }
                    else if (validCrc)
                    {
                        found = true;
                    }
                }

                sixAxis.setCalibrationData(ref calibration, true);
            }
            else
            {
                hDevice.readFeatureData(calibration);
                sixAxis.setCalibrationData(ref calibration, true);
            }
        }

        public override void StartUpdate()
        {
            this.inputReportErrorCount = 0;

            if (ds4Input == null)
            {
                if (conType == ConnectionType.BT)
                {
                    //ds4Output = new Thread(performDs4Output);
                    //ds4Output.Priority = ThreadPriority.Normal;
                    //ds4Output.Name = "DS4 Output thread: " + Mac;
                    //ds4Output.IsBackground = true;
                    //ds4Output.Start();

                    timeoutCheckThread = StartBackgroundThread(TimeoutTestThread, ThreadPriority.BelowNormal, "DualSense Timeout thread: " + Mac);
                }
                //else
                //{
                //    ds4Output = new Thread(OutReportCopy);
                //    ds4Output.Priority = ThreadPriority.Normal;
                //    ds4Output.Name = "DS4 Arr Copy thread: " + Mac;
                //    ds4Output.IsBackground = true;
                //    ds4Output.Start();
                //}

                ds4Input = StartBackgroundThread(ReadInput, ThreadPriority.AboveNormal, "DualSense Input thread: " + Mac);
            }
            else
                AppLogger.LogDebug("Thread already running for DS4: " + Mac);
        }

        protected override void KickWindowsToNoticeDisconnect()
        {
            // Request serial feature report data. Causes Windows to notice the dead
            // device.
            byte[] tmpFeatureData = new byte[64];
            tmpFeatureData[0] = SERIAL_FEATURE_ID;
            hDevice.readFeatureData(tmpFeatureData);
        }

        private int reportOffset;
        private IInputReportParser reportParser;

        protected override void OnLoopStarting()
        {
            timeoutEvent = false;
            reportOffset = conType == ConnectionType.BT ? 1 : 0;
            reportParser = new DualSenseReportParser(reportOffset);

            // Run continuous calibration on Gyro when starting input loop
            sixAxis.ResetContinuousCalibration();
            standbySw.Start();
        }

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
                        touchpad.handleTouchpad(inputReport, cState, TOUCHPAD_DATA_OFFSET + reportOffset, touchOffset);
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
                        sixAxis.handleSixaxis(pbGyro, pbAccel, cState, elapsedDeltaTime);
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

        protected override void WriteOutputFrame()
        {
            PrepareOutReport();
            if (outputDirty)
            {
                WriteReport();
                currentHap.dirty = false;
                previousHapticState = currentHap;
            }

            outputDirty = false;
            currentHap.dirty = false;
        }

        protected override void StopOutputUpdate()
        {
            SendEmptyOutputReport();
        }

        private void SendEmptyOutputReport()
        {
            int reportOffset = conType == ConnectionType.BT ? 1 : 0;
            Array.Clear(outputReport, 0, outputReport.Length);

            outputReport[0] = conType == ConnectionType.USB ? OUTPUT_REPORT_ID_USB :
                OUTPUT_REPORT_ID_BT;

            // Disable haptics and trigger motors
            outputReport[1 + reportOffset] = useRumble ? (byte)0x0F : (byte)0x0C;
            outputReport[2 + reportOffset] = 0x15; // Toggle all LED lights. 0x01 | 0x04 | 0x10

            // Set Lightbar to white
            outputReport[45 + reportOffset] = 0xFF; 
            outputReport[46 + reportOffset] = 0xFF;
            outputReport[47 + reportOffset] = 0xFF;

            if (conType == ConnectionType.BT)
            {
                outputReport[1] = OUTPUT_REPORT_ID_DATA;

                // Need to calculate and populate CRC32 data so controller will accept the report
                uint calcCrc32 = ~Crc32Algorithm.Compute(outputBTCrc32Head);
                calcCrc32 = ~Crc32Algorithm.CalculateBasicHash(ref calcCrc32, ref outputReport, 0, BT_OUTPUT_REPORT_LENGTH - 4);
                outputReport[74] = (byte)calcCrc32;
                outputReport[75] = (byte)(calcCrc32 >> 8);
                outputReport[76] = (byte)(calcCrc32 >> 16);
                outputReport[77] = (byte)(calcCrc32 >> 24);
            }

            WriteReport();
            //hDevice.fileStream.Flush();
        }

        private void SendInitialBTOutputReport()
        {
            Array.Clear(outputReport, 0, outputReport.Length);

            outputReport[0] = OUTPUT_REPORT_ID_BT; // Report ID
            outputReport[1] = OUTPUT_REPORT_ID_DATA;
            outputReport[3] = 0x15; // Toggle all LED lights. 0x01 | 0x04 | 0x10

            // Need to calculate and populate CRC32 data so controller will accept the report
            uint calcCrc32 = ~Crc32Algorithm.Compute(outputBTCrc32Head);
            calcCrc32 = ~Crc32Algorithm.CalculateBasicHash(ref calcCrc32, ref outputReport, 0, BT_OUTPUT_REPORT_LENGTH - 4);
            outputReport[74] = (byte)calcCrc32;
            outputReport[75] = (byte)(calcCrc32 >> 8);
            outputReport[76] = (byte)(calcCrc32 >> 16);
            outputReport[77] = (byte)(calcCrc32 >> 24);

            WriteReport();
        }

        private unsafe void PrepareOutReport()
        {
            MergeStates();

            bool change = false;
            bool rumbleSet = currentHap.IsRumbleSet();

            var outputState = new DualSenseOutputState
            {
                UseRumble = useRumble,
                UseAccurateRumble = useAccurateRumble,
                RumbleRightLightFast = currentHap.rumbleState.RumbleMotorStrengthRightLightFast,
                RumbleLeftHeavySlow = currentHap.rumbleState.RumbleMotorStrengthLeftHeavySlow,
                LightbarColor = currentHap.lightbarState.LightBarColor,
                HapticPowerLevel = hapticPowerLevel,
                MuteLedByte = muteLEDByte,
                PlayerLedMask = activePlayerLEDMask,
                R2Effect = r2EffectData,
                L2Effect = l2EffectData,
            };

            if (conType == ConnectionType.USB)
            {
                DualSenseOutputReportBuilder.Fill(outputReport, false, in outputState);

                if (currentHap.dirty || !previousHapticState.Equals(currentHap))
                {
                    change = true;
                }
                /*fixed (byte* bytePrevBuff = outputReport, byteTmpBuff = outReportBuffer)
                {
                    for (int i = 0, arlen = USB_OUTPUT_CHANGE_LENGTH; !change && i < arlen; i++)
                        change = bytePrevBuff[i] != byteTmpBuff[i];
                }
                */

                if (change)
                {
                    //Console.WriteLine("DIRTY");
                    outputDirty = true;
                    if (rumbleSet)
                    {
                        standbySw.Restart();
                    }
                    else
                    {
                        standbySw.Reset();
                    }

                    //outReportBuffer.CopyTo(outputReport, 0);
                }
                else if (rumbleSet && standbySw.ElapsedMilliseconds >= 4000L)
                {
                    outputDirty = true;
                    standbySw.Restart();
                }
                //bool res = hDevice.WriteOutputReportViaInterrupt(outputReport, READ_STREAM_TIMEOUT);
                //Console.WriteLine("STAUTS: {0}", res);
            }
            else
            {
                //outReportBuffer[0] = OUTPUT_REPORT_ID_BT; // Report ID
                DualSenseOutputReportBuilder.Fill(outputReport, true, in outputState);

                change = currentHap.dirty || !previousHapticState.Equals(currentHap);

                // Need to calculate and populate CRC32 data so controller will accept the report
                uint calcCrc32 = 0;
                if (change)
                //if (outputPendCount >= 1 || change)
                //if (!previousHapticState.Equals(currentHap))
                {
                    //change = true;
                    outputDirty = true;

                    if (rumbleSet)
                    {
                        standbySw.Restart();
                    }
                    else
                    {
                        standbySw.Reset();
                    }
                }
                else if (rumbleSet && standbySw.ElapsedMilliseconds >= 4000L)
                {
                    outputDirty = true;
                    standbySw.Restart();
                }

                if (outputDirty)
                {
                    int crcOffset = 0;
                    int crcpos = BT_OUTPUT_REPORT_LENGTH - 4;
                    calcCrc32 = ~Crc32Algorithm.Compute(outputBTCrc32Head);
                    //calcCrc32 = ~Crc32Algorithm.CalculateBasicHash(ref calcCrc32, ref outputReport, 0, BT_OUTPUT_REPORT_LENGTH-4);
                    calcCrc32 = ~Crc32Algorithm.CalculateFasterBT78Hash(ref calcCrc32, ref outputReport, ref crcOffset, ref crcpos);
                }

                outputReport[74] = (byte)calcCrc32;
                outputReport[75] = (byte)(calcCrc32 >> 8);
                outputReport[76] = (byte)(calcCrc32 >> 16);
                outputReport[77] = (byte)(calcCrc32 >> 24);

                /*fixed (byte* bytePrevBuff = outputReport, byteTmpBuff = outReportBuffer)
                {
                    for (int i = 0, arlen = BT_OUTPUT_CHANGE_LENGTH; !change && i < arlen; i++)
                        change = bytePrevBuff[i] != byteTmpBuff[i];
                }
                */

                /*if (change)
                {
                    outputPendCount = OUTPUT_MIN_COUNT_BT;
                    //Console.WriteLine("DIRTY");
                    outputDirty = true;
                    
                    //outReportBuffer.CopyTo(outputReport, 0);
                }
                else if (outputPendCount >= 1)
                {
                    outputPendCount--;
                    outputDirty = outputPendCount >= 1;
                }
                */

                //outputDirty = true;

                //bool res = hDevice.WriteOutputReportViaControl(outputReport);
                //Console.WriteLine("STAUTS: {0}", res);
            }
        }

        private bool WriteReport()
        {
            bool result;
            if (conType == ConnectionType.BT)
            {
                // DualSense seems to only accept output data via the Interrupt endpoint
                result = hDevice.WriteOutputReportViaInterrupt(outputReport, READ_STREAM_TIMEOUT);
                //result = hDevice.WriteOutputReportViaControl(outputReport);
            }
            else
            {
                result = hDevice.WriteOutputReportViaInterrupt(outputReport, READ_STREAM_TIMEOUT);
            }

            //Console.WriteLine("STAUTS: {0}", result);
            return result;
        }

        private void Detach()
        {
            SendEmptyOutputReport();
        }

        private void CalculateDeviceSlotMask()
        {
            // Map 1-8 to a symmetrical LED array from a set of
            // 5 LED lights
            switch (deviceSlotNumber)
            {
                case 0:
                    deviceSlotMask = 0x04;
                    break;
                case 1:
                    deviceSlotMask = 0x02 | 0x08;
                    break;
                case 2:
                    deviceSlotMask = 0x01 | 0x04 | 0x10;
                    break;
                case 3:
                    deviceSlotMask = 0x01 | 0x02 | 0x08 | 0x10;
                    break;
                case 4:
                    deviceSlotMask = 0x01 | 0x10;
                    break;
                case 5:
                    deviceSlotMask = 0x02 | 0x04 | 0x08;
                    break;
                case 6:
                    deviceSlotMask = 0x01 | 0x02 | 0x04 | 0x08 | 0x10;
                    break;
                case 7:
                default:
                    deviceSlotMask = 0x00;
                    break;
            }
        }

        private void PrepareMuteLEDByte()
        {
            if (nativeOptionsStore != null)
            {
                switch (nativeOptionsStore.MuteLedMode)
                {
                    case DualSenseControllerOptions.MuteLEDMode.Off:
                        muteLEDByte = 0x00;
                        break;
                    case DualSenseControllerOptions.MuteLEDMode.On:
                        muteLEDByte = 0x01;
                        break;
                    case DualSenseControllerOptions.MuteLEDMode.Pulse:
                        muteLEDByte = 0x02;
                        break;
                    default:
                        muteLEDByte = 0x00;
                        break;
                }
            }
        }

        private void PreparePlayerLEDBarByte()
        {
            if (nativeOptionsStore != null)
            {
                if (nativeOptionsStore.LedMode == DualSenseControllerOptions.LEDBarMode.Off)
                {
                    activePlayerLEDMask = 0x00;
                }
                else if (nativeOptionsStore.LedMode == DualSenseControllerOptions.LEDBarMode.On)
                {
                    activePlayerLEDMask = deviceSlotMask;
                }
                else if (nativeOptionsStore.LedMode == DualSenseControllerOptions.LEDBarMode.BatteryPercentage)
                {
                    activePlayerLEDMask = DeviceBatteryLinearMask(battery);
                }
            }
        }

        public override void PrepareTriggerEffect(TriggerId trigger, TriggerEffects effect, TriggerEffectSettings effectSettings)
        {
            if (trigger == TriggerId.LeftTrigger)
            {
                l2EffectData.ChangeData(effect, effectSettings);
            }
            else if (trigger == TriggerId.RightTrigger)
            {
                r2EffectData.ChangeData(effect, effectSettings);
            }
            else
            {
                throw new ArgumentOutOfRangeException("Invalid Trigger Id");
            }

            queueEvent(() =>
            {
                outputDirty = true;
                currentHap.dirty = true;
                PrepareOutReport();
            });
        }

        private byte DeviceBatteryLinearMask(int deviceBattery)
        {
            byte batteryMask;
            if (deviceBattery >= 95)
                batteryMask = 0x01 | 0x02 | 0x08 | 0x10;
            else if (deviceBattery >= 70)
                batteryMask = 0x01 | 0x02 | 0x08;
            else if (deviceBattery >= 50)
                batteryMask = 0x01 | 0x02;
            else if (deviceBattery >= 20)
                batteryMask = 0x01;
            else if (deviceBattery >= 5)
                batteryMask = 0x01 | 0x02 | 0x04;
            else
                batteryMask = 0x00;

            return batteryMask;
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

            queueEvent(() =>
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
                    queueEvent(() => { outputDirty = true; });
                };

                nativeOptionsStore.LedModeChanged += (sender, e) =>
                {
                    PreparePlayerLEDBarByte();
                    queueEvent(() => { outputDirty = true; });
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
