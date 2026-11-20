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
    /// <summary>接続の初期化（機種の判定、較正データ、更新の開始）。</summary>
    public partial class DualSenseDevice
    {
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
                synced = IsValidSerial();
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
                featureFirmRead = hDevice.ReadFeatureData(firmwareInfoData);
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
                hDevice.ReadFeatureData(buffer);
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

        public override void RefreshCalibration()
        {
            byte[] calibration = new byte[41];
            calibration[0] = conType == ConnectionType.BT ? (byte)0x05 : (byte)0x05;

            if (conType == ConnectionType.BT)
            {
                bool found = false;
                for (int tries = 0; !found && tries < 5; tries++)
                {
                    hDevice.ReadFeatureData(calibration);
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

                sixAxis.SetCalibrationData(ref calibration, true);
            }
            else
            {
                hDevice.ReadFeatureData(calibration);
                sixAxis.SetCalibrationData(ref calibration, true);
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
            hDevice.ReadFeatureData(tmpFeatureData);
        }

        protected override void OnLoopStarting()
        {
            timeoutEvent = false;
            reportOffset = conType == ConnectionType.BT ? 1 : 0;
            reportParser = new DualSenseReportParser(reportOffset);

            // Run continuous calibration on Gyro when starting input loop
            sixAxis.ResetContinuousCalibration();
            standbySw.Start();
        }
    }
}
