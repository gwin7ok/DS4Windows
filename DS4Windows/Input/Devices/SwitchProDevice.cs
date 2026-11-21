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
using System.Diagnostics;
using System.Threading;
using DS4Windows.Core.Logging;
using DS4Windows.Core.Model;
using DS4Windows.Core.Settings;
using DS4Windows.Input.Hid;
using DS4Windows.Input.Abstractions;
using DS4Windows.Input.Parsers;

namespace DS4Windows.Input.Devices
{
    public partial class SwitchProDevice : InputLoopDevice
    {
        public static class SwitchProSubCmd
        {
            public const byte SET_INPUT_MODE = 0x03;
            public const byte SET_LOW_POWER_STATE = 0x08;
            public const byte SPI_FLASH_READ = 0x10;
            public const byte SET_LIGHTS = 0x30; // LEDs on controller
            public const byte SET_HOME_LIGHT = 0x38;
            public const byte ENABLE_IMU = 0x40;
            public const byte SET_IMU_SENS = 0x41;
            public const byte ENABLE_VIBRATION = 0x48;
        }

        public struct StickAxisData
        {
            public ushort max;
            public ushort mid;
            public ushort min;
        };

        public const int IMU_XAXIS_IDX = 0, IMU_YAW_IDX = 0;
        public const int IMU_YAXIS_IDX = 1, IMU_PITCH_IDX = 1;
        public const int IMU_ZAXIS_IDX = 2, IMU_ROLL_IDX = 2;

        public const short ACCEL_ORIG_HOR_OFFSET_X = -688;
        public const short ACCEL_ORIG_HOR_OFFSET_Y = 0;
        public const short ACCEL_ORIG_HOR_OFFSET_Z = 4038;

        //private const ushort SAMPLE_STICK_MAX = 3200;
        private const ushort SAMPLE_STICK_MAX = 3300;
        private const ushort SAMPLE_STICK_MIN = 500;
        private const ushort SAMPLE_STICK_MID = SAMPLE_STICK_MAX - SAMPLE_STICK_MIN;

        private readonly SwitchProCommandChannel _channel;
        private readonly SwitchProCalibration _calibration = new SwitchProCalibration();
        private readonly SwitchProSession _session;

        private const string BLUETOOTH_HID_GUID = "{00001124-0000-1000-8000-00805F9B34FB}";

        public byte FrameCount { get => _channel.FrameCount; set => _channel.FrameCount = value; }

        private const int INPUT_REPORT_LEN = SwitchProCommandChannel.InputReportLength;
        private const int OUTPUT_REPORT_LEN = 49;
        private const int RUMBLE_REPORT_LEN = SwitchProCommandChannel.RumbleReportLength;
        // Converts raw gyro input value to dps. Equal to (4588/65535)
        private const float GYRO_IN_DEG_SEC_FACTOR = 0.070f;
        private new const int WARN_INTERVAL_BT = 40;
        private new const int WARN_INTERVAL_USB = 30;
        private byte[] inputReportBuffer = null!; // 初期化で確保する
        // ボタン・方向キー・ZL/ZR の解析。状態を持たないため共有してよい
        private static readonly IInputReportParser reportParser = new SwitchProReportParser();
        private byte[] outputReportBuffer = null!; // 初期化で確保する
        private byte[] rumbleReportBuffer = null!; // 初期化で確保する

        public int InputReportLen { get => INPUT_REPORT_LEN; }
        public int OutputReportLen { get => OUTPUT_REPORT_LEN; }
        public int RumbleReportLen { get => RUMBLE_REPORT_LEN; }

        private double combLatency;
        public double CombLatency { get => combLatency; set => combLatency = value; }

        private bool enableHomeLED = true;
        public bool EnableHomeLED { get => enableHomeLED; set => enableHomeLED = value; }

        #pragma warning disable CS0649 // nativeOptionsStore may be set by native/interop code
        private SwitchProControllerOptions nativeOptionsStore = null!; // 初期化で設定する
        #pragma warning restore CS0649

        /// <summary>
        /// Flag to tell methods if device has been successfully initialized and opened
        /// </summary>
        private bool connectionOpened = false;

        #pragma warning disable CS0067 // events declared for compatibility but may be unused
        public override event EventHandler<EventArgs>? Removal = null;

        #pragma warning restore CS0067

        public SwitchProDevice(IHidTransport hidDevice,
            string disName, VidPidFeatureSet featureSet = VidPidFeatureSet.DefaultDS4) :
            base(hidDevice, disName, featureSet)
        {
            _channel = new SwitchProCommandChannel(hidDevice);
            _session = new SwitchProSession(_channel, _calibration, hidDevice);
            runCalib = false;
            synced = true;

            _calibration.LeftStickX.max = SAMPLE_STICK_MAX; _calibration.LeftStickX.min = SAMPLE_STICK_MIN;
            _calibration.LeftStickX.mid = SAMPLE_STICK_MID;

            _calibration.LeftStickY.max = SAMPLE_STICK_MAX; _calibration.LeftStickY.min = SAMPLE_STICK_MIN;
            _calibration.LeftStickY.mid = SAMPLE_STICK_MID;

            _calibration.RightStickX.max = SAMPLE_STICK_MAX; _calibration.RightStickX.min = SAMPLE_STICK_MIN;
            _calibration.RightStickX.mid = SAMPLE_STICK_MID;

            _calibration.RightStickY.max = SAMPLE_STICK_MAX; _calibration.RightStickY.min = SAMPLE_STICK_MIN;
            _calibration.RightStickY.mid = SAMPLE_STICK_MID;

            warnInterval = WARN_INTERVAL_BT;

            DeviceSlotNumberChanged += (sender, e) => {
                CalculateDeviceSlotMask();
            };

            Removal += SwitchProDevice_Removal;
        }

        private void SwitchProDevice_Removal(object? sender, EventArgs e)
        {
            connectionOpened = false;
        }

        public override void PostInit()
        {
            deviceType = InputDeviceType.SwitchPro;
            gyroMouseSensSettings = new GyroMouseSens();
            conType = DetermineConnectionType(hDevice);
            optionsStore = nativeOptionsStore = new SwitchProControllerOptions(deviceType);
            SetupOptionsEvents();
            Mac = hDevice.ReadSerial(SerialReportID);

            if (conType == ConnectionType.BT)
            {
                warnInterval = WARN_INTERVAL_BT;
            }
            else
            {
                warnInterval = WARN_INTERVAL_USB;
            }

            inputReportBuffer = new byte[INPUT_REPORT_LEN];
            outputReportBuffer = new byte[OUTPUT_REPORT_LEN];
            rumbleReportBuffer = new byte[RUMBLE_REPORT_LEN];
        }

        public static ConnectionType DetermineConnectionType(IHidTransport hDevice)
        {
            ConnectionType result;
            if (hDevice.DevicePath.ToUpper().Contains(BLUETOOTH_HID_GUID))
            {
                result = ConnectionType.BT;
            }
            else
            {
                result = ConnectionType.USB;
            }

            return result;
        }

        public override void StartUpdate()
        {
            this.inputReportErrorCount = 0;

            try
            {
                SetOperational();
            }
            catch (System.IO.IOException)
            {
                AppLogger.LogUser(AppLogLevel.Error, "Log_Device_InitFailed", MacAddress);
            }

            if (!connectionOpened)
            {
                // Failed to open device. Tell app to consider device detached
                isDisconnecting = true;
                Removal?.Invoke(this, EventArgs.Empty);
            }
            else if (ds4Input == null)
            {
                // Device is open. Create Reader Input thread
                ds4Input = StartBackgroundThread(ReadInput, ThreadPriority.AboveNormal, "Switch Pro Reader Thread");
            }
        }

        protected override void StopOutputUpdate()
        {
        }

        private readonly byte[] stick_raw = { 0, 0, 0 };
        private readonly byte[] stick_raw2 = { 0, 0, 0 };
        private readonly short[] accel_raw = { 0, 0, 0 };
        private readonly short[] gyro_raw = new short[9];
        private readonly short[] gyro_out = new short[9];
        private long previousCheckTime;

        protected override void OnLoopStarting()
        {
            timeoutEvent = false;
            previousCheckTime = 0;

            // Run continuous calibration on Gyro when starting input loop
            sixAxis.ResetContinuousCalibration();
            standbySw.Start();
        }

        // 標準の入力レポート（0x30）以外が続いたら、切断として扱う
        protected override FrameResult ReceiveFrame()
        {
            HidReadStatus res = hDevice.ReadFile(inputReportBuffer);
            if (res == HidReadStatus.Success)
            {
                if (inputReportBuffer[0] != 0x30)
                {
                    readWaitEv.Reset();
                    inputReportErrorCount++;
                    if (inputReportErrorCount > 10)
                    {
                        exitInputThread = true;
                        isDisconnecting = true;
                        Removal?.Invoke(this, EventArgs.Empty);
                    }

                    return FrameResult.Skip;
                }
            }
            else
            {
                readWaitEv.Reset();
                exitInputThread = true;
                isDisconnecting = true;
                Removal?.Invoke(this, EventArgs.Empty);
                return FrameResult.Skip;
            }

            readWaitEv.Wait();
            readWaitEv.Reset();

            inputReportErrorCount = 0;
            return FrameResult.Accept;
        }

        // 直前の読み取りから 5ms 以内のフレームは捨てる（捨てたフレームは、時刻を進めない）
        protected override bool IsExpectedReport()
        {
            combLatency = lastTimeElapsedDouble * .001;

            long deltaCheckElapsed = lastFrameTicks - previousCheckTime;
            double lastCheckElapsed = deltaCheckElapsed * (1.0 / Stopwatch.Frequency) * 1000.0;
            double lastCheckTimeElapsed = lastCheckElapsed * 0.001;
            previousCheckTime = lastFrameTicks;

            if (lastCheckTimeElapsed <= 0.005)
            {
                lastFrameTicks = previousFrameTicks;
                return false;
            }

            return true;
        }

        protected override void ProcessFrame()
        {
            unchecked
            {
                double elapsedDeltaTime = lastTimeElapsedDouble * .001;

                // DS4 Frame Counter range is [0-127]
                cState.FrameCounter = (byte)(cState.PacketCounter % 128);

                cState.elapsedTime = elapsedDeltaTime;
                cState.totalMicroSec = pState.totalMicroSec + (uint)(elapsedDeltaTime * 1000000);
                combLatency = 0.0;

                if ((this.featureSet & VidPidFeatureSet.NoBatteryReading) == 0)
                {
                    byte tempByte = inputReportBuffer[2];
                    // Strip out LSB from high nibble. Used as Charging flag and will be checked later
                    int tempBattery = BatteryScale.ToPercent((tempByte & 0xE0) >> 4, 8);
                    UpdateBattery(tempBattery);

                    cState.Battery = (byte)tempBattery;

                    bool tempCharging = (tempByte & 0x10) != 0;
                    UpdateCharging(tempCharging);
                }
                else
                {
                    battery = 99;
                    cState.Battery = 99;
                }

                reportParser.Parse(inputReportBuffer, cState);

                stick_raw[0] = inputReportBuffer[6];
                stick_raw[1] = inputReportBuffer[7];
                stick_raw[2] = inputReportBuffer[8];
                cState.LX = _calibration.ScaleLeftX(stick_raw[0] | ((stick_raw[1] & 0x0F) << 8));
                cState.LY = _calibration.ScaleLeftY((stick_raw[1] >> 4) | (stick_raw[2] << 4));

                stick_raw2[0] = inputReportBuffer[9];
                stick_raw2[1] = inputReportBuffer[10];
                stick_raw2[2] = inputReportBuffer[11];
                cState.RX = _calibration.ScaleRightX(stick_raw2[0] | ((stick_raw2[1] & 0x0F) << 8));
                cState.RY = _calibration.ScaleRightY((stick_raw2[1] >> 4) | (stick_raw2[2] << 4));

                short tempShort;
                for (int i = 0; i < 3; i++)
                {
                    int data_offset = i * 12;
                    int gyro_offset = i * 3;
                    accel_raw[IMU_XAXIS_IDX] = (short)((ushort)(inputReportBuffer[16 + data_offset] << 8) | inputReportBuffer[15 + data_offset]);
                    accel_raw[IMU_YAXIS_IDX] = (short)((ushort)(inputReportBuffer[14 + data_offset] << 8) | inputReportBuffer[13 + data_offset]);
                    accel_raw[IMU_ZAXIS_IDX] = (short)((ushort)(inputReportBuffer[18 + data_offset] << 8) | inputReportBuffer[17 + data_offset]);

                    tempShort = gyro_raw[IMU_YAW_IDX + gyro_offset] = (short)((ushort)(inputReportBuffer[24 + data_offset] << 8) | inputReportBuffer[23 + data_offset]);
                    gyro_out[IMU_YAW_IDX + gyro_offset] = (short)(tempShort);

                    tempShort = gyro_raw[IMU_PITCH_IDX + gyro_offset] = (short)((ushort)(inputReportBuffer[22 + data_offset] << 8) | inputReportBuffer[21 + data_offset]);
                    gyro_out[IMU_PITCH_IDX + gyro_offset] = (short)(tempShort);

                    tempShort = gyro_raw[IMU_ROLL_IDX + gyro_offset] = (short)((ushort)(inputReportBuffer[20 + data_offset] << 8) | inputReportBuffer[19 + data_offset]);
                    gyro_out[IMU_ROLL_IDX + gyro_offset] = (short)(tempShort);
                }

                // For Accel, just use most recent sampled values
                int accelX = accel_raw[IMU_XAXIS_IDX];
                int accelY = accel_raw[IMU_YAXIS_IDX];
                int accelZ = accel_raw[IMU_ZAXIS_IDX];

                // Just use most recent sample for now
                int gyroYaw = (short)(-1 * (gyro_out[6 + IMU_YAW_IDX] - _calibration.GyroBias[IMU_YAW_IDX] + _calibration.GyroCalibOffsets[IMU_YAW_IDX]));
                int gyroPitch = (short)(gyro_out[6 + IMU_PITCH_IDX] - _calibration.GyroBias[IMU_PITCH_IDX] - _calibration.GyroCalibOffsets[IMU_PITCH_IDX]);
                int gyroRoll = (short)(gyro_out[6 + IMU_ROLL_IDX] - _calibration.GyroBias[IMU_ROLL_IDX] - _calibration.GyroCalibOffsets[IMU_ROLL_IDX]);

                // Need to populate the SixAxis object manually to work around conversions
                SixAxis tempMotion = cState.Motion;
                sixAxis.PrepareNonDS4SixAxis(ref gyroYaw, ref gyroPitch, ref gyroRoll,
                    ref accelX, ref accelY, ref accelZ);

                tempMotion.gyroYawFull = gyroYaw; tempMotion.gyroPitchFull = -gyroPitch; tempMotion.gyroRollFull = gyroRoll;
                tempMotion.accelXFull = accelX * 2; tempMotion.accelYFull = -accelZ * 2; tempMotion.accelZFull = -accelY * 2;

                tempMotion.elapsed = elapsedDeltaTime;
                tempMotion.previousAxis = pState.Motion;
                tempMotion.gyroYaw = gyroYaw / 256; tempMotion.gyroPitch = -gyroPitch / 256; tempMotion.gyroRoll = gyroRoll / 256;
                tempMotion.accelX = accelX / 31; tempMotion.accelY = -accelZ / 31; tempMotion.accelZ = -accelY / 31;
                tempMotion.outputAccelX = 0; tempMotion.outputAccelY = 0; tempMotion.outputAccelZ = 0;
                tempMotion.outputGyroControls = false;
                tempMotion.accelXG = (accelX * 2) / DS4Windows.Core.Model.SixAxis.F_ACC_RES_PER_G;
                tempMotion.accelYG = (-accelZ * 2) / DS4Windows.Core.Model.SixAxis.F_ACC_RES_PER_G;
                tempMotion.accelZG = (-accelY * 2) / DS4Windows.Core.Model.SixAxis.F_ACC_RES_PER_G;

                tempMotion.angVelYaw = gyroYaw * GYRO_IN_DEG_SEC_FACTOR;
                tempMotion.angVelPitch = -gyroPitch * GYRO_IN_DEG_SEC_FACTOR;
                tempMotion.angVelRoll = gyroRoll * GYRO_IN_DEG_SEC_FACTOR;

                sixAxis.FireSixAxisEvent(cState.ReportTimeStamp, cState.Motion);
            }
        }

        // この機種は BT の切断のあと、ループを抜けるだけ（切断の通知は DisconnectBT が行う）
        protected override bool ProcessIdle()
        {
            if (EvaluateIdleDisconnect(utcNow) && conType == ConnectionType.BT && DisconnectBT(true))
            {
                timeoutExecuted = true;
                return true; // all done
            }

            return false;
        }

        protected override void WriteOutputFrame()
        {
            WriteReport();
            pState.Motion.Copy(cState.Motion);
        }

        /// <summary>通常の動作へ移す手順を実行し、完了したら接続済みにする（失敗の例外は呼び出し側が扱う）。</summary>
        public void SetOperational()
        {
            _session.SetOperational(conType, enableHomeLED, deviceSlotMask);
            connectionOpened = true;
        }

        public byte[]? Subcommand(byte subcommand, byte[] tmpBuffer, uint bufLen, bool checkResponse = false) =>
            _channel.Subcommand(subcommand, tmpBuffer, bufLen, checkResponse);

        public void SetInitRumble() => _channel.SetInitRumble();

        public void PrepareRumbleData(byte[] buffer) =>
            _channel.PrepareRumbleData(buffer, currentLeftAmpRatio, currentRightAmpRatio);

        public void CalibrationData() => _session.ReadCalibration();

        public double currentLeftAmpRatio;
        public double currentRightAmpRatio;

        public override bool IsAlive()
        {
            return !isDisconnecting && connectionOpened;
        }

        private void SetupOptionsEvents()
        {
            if (nativeOptionsStore != null)
            {
            }
        }

        public override void LoadStoreSettings()
        {
            if (nativeOptionsStore != null)
            {
                enableHomeLED = nativeOptionsStore.EnableHomeLED;
            }
        }
    }
}
