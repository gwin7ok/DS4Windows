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
    /// <summary>
    /// DualShock 4（USB／Bluetooth／ワイヤレスアダプター）。読み取りの骨組み（<see cref="InputLoopDevice"/>）の差し込み口で、
    /// DS4 のレポート（BT は 0x11＋CRC32）の検証、電池、タイムスタンプ、タッチパッド、動き、アダプターの同期、アイドル切断を書く。
    /// </summary>
    public partial class DualShock4Device : InputLoopDevice
    {
        private static readonly IInputReportParser ReportParser = new Ds4ReportParser();

        private readonly TimestampClock timestampClock = new TimestampClock(ushort.MaxValue, 16u);
        private DS4ControllerOptions nativeOptionsStore = null!; // 初期化で設定する
        private const byte DEFAULT_BT_REPORT_TYPE = 0x11;
        private byte knownGoodBTOutputReportType = DEFAULT_BT_REPORT_TYPE;
        private int btOutputPayloadLen = BT_OUTPUT_REPORT_LENGTH;

        //private const byte DEFAULT_OUTPUT_FEATURES = 0xF7;
        private const byte DEFAULT_OUTPUT_FEATURES = 0x07;
        //private const byte COPYCAT_OUTPUT_FEATURES = 0xF3;
        private const byte COPYCAT_OUTPUT_FEATURES = 0x03;
        private byte outputFeaturesByte = DEFAULT_OUTPUT_FEATURES;

        private const int BT_OUTPUT_REPORT_0x15_LENGTH = BT_OUTPUT_REPORT_LENGTH;
        private const int BT_OUTPUT_REPORT_0x11_LENGTH = 78;
        private byte[] outputBTCrc32Head = new byte[] { 0xA2 };

        private long rawReceivedTime;
        private bool forceWrite;

        public DualShock4Device(IHidTransport hidDevice, string displayName, VidPidFeatureSet featureSet = VidPidFeatureSet.DefaultDS4)
            : base(hidDevice, displayName, featureSet)
        {
        }

        public override void PostInit()
        {
            conType = HidConnectionType(hDevice);
            Mac = hDevice.ReadSerial(SerialReportID);

            //HidDevice hidDevice = hDevice;
            deviceType = DS4Windows.Core.Model.InputDeviceType.DS4;
            gyroMouseSensSettings = new GyroMouseSens();
            optionsStore = nativeOptionsStore = new DS4ControllerOptions(deviceType);
            SetupOptionsEvents();

            if (conType == ConnectionType.USB || conType == ConnectionType.SONYWA)
            {
                inputReport = new byte[64];
                outputReport = new byte[hDevice.Capabilities.OutputReportByteLength];
                outReportBuffer = new byte[hDevice.Capabilities.OutputReportByteLength];
                if (conType == ConnectionType.USB)
                {
                    warnInterval = WARN_INTERVAL_USB;
                    synced = true;
                }
                else
                {
                    warnInterval = WARN_INTERVAL_BT;
                    runCalib = synced = IsValidSerial();
                }
            }
            else
            {
                btInputReport = new byte[BT_INPUT_REPORT_LENGTH];
                inputReport = new byte[BT_INPUT_REPORT_LENGTH - 2];
                // If OnlyOutputData0x05 feature is not set then use the default DS4 output buffer size. However, some Razer gamepads use 32 bytes output buffer and output data type 0x05 in BT mode (writeData fails if the code tries to write too many unnecessary bytes)
                if ((this.featureSet & VidPidFeatureSet.OnlyOutputData0x05) == 0)
                {
                    // Default DS4 logic while writing data to gamepad
                    outputReport = new byte[BT_OUTPUT_REPORT_LENGTH];
                    outReportBuffer = new byte[BT_OUTPUT_REPORT_LENGTH];

                    // Buffer len and output report payload len will differ
                    btOutputPayloadLen = BT_OUTPUT_REPORT_0x11_LENGTH;
                }
                else
                {
                    // Use the gamepad specific output buffer size (but minimum of 15 bytes to avoid out-of-index errors in this app)
                    outputReport = new byte[hDevice.Capabilities.OutputReportByteLength <= 15 ? 15 : hDevice.Capabilities.OutputReportByteLength];
                    outReportBuffer = new byte[hDevice.Capabilities.OutputReportByteLength <= 15 ? 15 : hDevice.Capabilities.OutputReportByteLength];

                    // Use custom buffer len
                    btOutputPayloadLen = outputReport.Length;
                }
                warnInterval = WARN_INTERVAL_BT;
                synced = IsValidSerial();
            }

            if (runCalib)
                RefreshCalibration();

            // if (!hDevice.IsFileStreamOpen())
            // {
            //     hDevice.OpenFileStream(outputReport.Length);
            // }

            // Temporarily disable this check as it does not seem to help
            // detect fake DS4 controllers
            //if (conType == ConnectionType.BT &&
            //    !featureSet.HasFlag(VidPidFeatureSet.NoOutputData) &&
            //    !featureSet.HasFlag(VidPidFeatureSet.OnlyOutputData0x05))
            //{
            //    CheckOutputReportTypes();
            //}

            SendOutputReport(true, true, false); // initialize the output report (don't force disconnect the gamepad on initialization even if writeData fails because some fake DS4 gamepads don't support writeData over BT)

            // Mark PostInit complete so StartUpdate won't race with uninitialized buffers
            postInitDone = true;
        }

        protected override void KickWindowsToNoticeDisconnect()
        {
            this.SendOutputReport(true, true);
        }

        public override void RefreshCalibration()
        {
            byte[] calibration = new byte[41];
            calibration[0] = conType == ConnectionType.BT ? (byte)0x05 : (byte)0x02;

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

                sixAxis.SetCalibrationData(ref calibration, conType == ConnectionType.USB);

                if (hDevice.Attributes.ProductId == 0x5C4 && hDevice.Attributes.VendorId == 0x054C &&
                    sixAxis.FixupInvertedGyroAxis())
                    AppLogger.LogUser(AppLogLevel.Info, "Log_Device_YawAxisFixed", Mac.ToString());
            }
            else
            {
                hDevice.ReadFeatureData(calibration);
                sixAxis.SetCalibrationData(ref calibration, conType == ConnectionType.USB);
            }
        }

        public override void PrimeBatteryReading()
        {
            if (!postInitDone || ds4Input != null || isDisconnecting)
                return;

            if ((featureSet & VidPidFeatureSet.NoBatteryReading) != 0)
            {
                // 読み取りのスレッドと同じ、固定値（電池の値を送らないゲームパッド）
                battery = 99;
                return;
            }

            try
            {
                bool btReport = conType == ConnectionType.BT && (featureSet & VidPidFeatureSet.OnlyInputData0x01) == 0;
                // BT で、最初の数個が 0x11 でない（読み取りのスレッドは読み飛ばす）ことがあるため、数回まで読む。読めなければ、すぐにやめる
                for (int attempt = 0; attempt < 3; attempt++)
                {
                    if (btReport)
                    {
                        if (hDevice.ReadFile(btInputReport) != HidReadStatus.Success) return;
                        if (btInputReport[0] != 0x11) continue;
                        battery = DecodeBatteryPercent(btInputReport[2 + 30], out _);   // inputReport[30] は btInputReport[2 + 30]
                    }
                    else
                    {
                        if (hDevice.ReadFile(inputReport) != HidReadStatus.Success) return;
                        battery = DecodeBatteryPercent(inputReport[30], out _);
                    }

                    return;
                }
            }
            catch (Exception ex)
            {
                AppLogger.LogWarn($"[DI] PrimeBatteryReading failed: {ex.Message}");
            }
        }

        public override void StartUpdate()
        {
            this.inputReportErrorCount = 0;

            // Ensure PostInit finished; wait briefly if needed to avoid race where StartUpdate
            // runs before output buffers are initialized in PostInit.
            int waited = 0;
            while (!postInitDone && waited < 500)
            {
                Thread.Sleep(25); // 待機の理由: 読み取りスレッドの中。PostInit の完了（出力バッファーの初期化）を、最大 500ms 待つ
                waited += 25;
            }

            if (ds4Input == null)
            {
                if (conType == ConnectionType.BT)
                {
                    timeoutCheckThread = StartBackgroundThread(TimeoutTestThread, ThreadPriority.BelowNormal, "DS4 Timeout thread: " + Mac);
                }
                else
                {
                    ds4Output = new Thread(OutReportCopy);
                    ds4Output.Priority = ThreadPriority.Normal;
                    ds4Output.Name = "DS4 Arr Copy thread: " + Mac;
                    ds4Output.IsBackground = true;
                    ds4Output.Start();
                }

                ds4Input = StartBackgroundThread(RunInputLoop, ThreadPriority.AboveNormal, "DS4 Input thread: " + Mac);
            }
            else
            {
                AppLogger.LogDebug("Thread already running for DS4: " + Mac);
            }
        }

        private void SetupOptionsEvents()
        {
            if (nativeOptionsStore != null)
            {
                nativeOptionsStore.IsCopyCatChanged += (sender, e) =>
                {
                    PrepareOutputFeaturesByte();
                };
            }
        }

        public override void LoadStoreSettings()
        {
            if (nativeOptionsStore != null)
            {
                PrepareOutputFeaturesByte();
            }
        }

        protected override void OnLoopStarting()
        {
            timeoutEvent = false;
            forceWrite = false;

            // Run continuous calibration on Gyro when starting input loop
            sixAxis.ResetContinuousCalibration();
            standbySw.Start();
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

        protected override void WriteOutputFrame()
        {
            SendOutputReport(true, forceWrite);
            forceWrite = false;
        }
    }
}
