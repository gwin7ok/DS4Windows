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
using System.Collections.Generic;
using System.Threading;

using System.Runtime.InteropServices;
using System.Diagnostics;

using DS4Windows.Core.Logging;
using DS4Windows.Core.Model;
using DS4Windows.Core.Settings;
using DS4Windows.Core.Platform;
using DS4Windows.Input.Abstractions;
using DS4Windows.Input.Parsers;

namespace DS4Windows.Input
{

    /// <summary>
    /// 全ての入力機種が共有する中核: 状態（現在・直前）、電池・充電、イベント、設定、切断、出力の合成。
    /// 読み取りのループは持たず、<see cref="DS4Windows.Input.Devices.InputLoopDevice"/> が機種ごとの差し込み口で動かす。
    /// </summary>
    public abstract class InputDeviceCore : DS4Windows.Core.Abstractions.IConfigurableDevice, DS4Windows.Input.Abstractions.ILightbarDevice,
        DS4Windows.Core.Abstractions.IOutputBoundDevice, DS4Windows.Core.Abstractions.IMotionSource
    {
        // Indicates PostInit completed and output buffers (etc.) are initialized
        protected volatile bool postInitDone = false;

        public class GyroMouseSens
        {
            public double mouseOffset = 0.2;
            public double mouseCoefficient = 0.012;
            public double mouseSmoothOffset = 0.2;
        }

        //internal const int BT_OUTPUT_REPORT_LENGTH = 78;
        protected const int BT_OUTPUT_REPORT_LENGTH = 334;
        internal const int BT_INPUT_REPORT_LENGTH = 547;
        internal const int BT_OUTPUT_CHANGE_LENGTH = 13;
        internal const int USB_OUTPUT_CHANGE_LENGTH = 11;
        // Use large value for worst case scenario
        internal const int READ_STREAM_TIMEOUT = 3000;
        // Isolated BT report can have latency as high as 15 ms
        // due to hardware.
        internal const int WARN_INTERVAL_BT = 500;
        internal const int WARN_INTERVAL_USB = 100;
        // Maximum values for battery level when no USB cable is connected
        // and when a USB cable is connected
        internal const int BATTERY_MAX = 8;
        internal const int BATTERY_MAX_USB = 11;
        public const string BLANK_SERIAL = "00:00:00:00:00:00";
        public const byte SERIAL_FEATURE_ID = 18;
        protected IHidTransport hDevice;
        // 入力レポートの解析（ボタン・スティック・トリガー・タッチの生値）。状態を持たないため全デバイスで共有してよい
        private static readonly IInputReportParser ReportParser = new Ds4ReportParser();
        protected string Mac = null!; // 派生クラスのコンストラクターで設定する
        protected ControllerState cState = new ControllerState();
        protected ControllerState pState = new ControllerState();
        protected ConnectionType conType;
        protected byte[] accel = new byte[6];
        protected byte[] gyro = new byte[6];
        protected byte[] inputReport = null!; // 派生クラスのコンストラクターで設定する
        protected byte[] btInputReport = null!; // Bluetooth の機種だけ確保する（読むのも Bluetooth のときだけ）
        protected byte[] outReportBuffer = null!, outputReport = null!; // 派生クラスのコンストラクターで確保する
        protected int inputReportErrorCount = 0; // Num of consequtive input report errors (fex if BT device fails 5 times in crc32 and 0x11 data type check then switch over to handle incoming BT packets as those were usb PC-friendly packets. Some fake DS4 gamepads needs this)
        protected readonly DS4Touchpad touchpad;
        protected readonly DS4SixAxis sixAxis;
        protected Thread? ds4Input, ds4Output;
        protected int battery;
        public DateTime lastActive = DateTime.UtcNow;
        public DateTime firstActive = DateTime.UtcNow;
        protected bool charging;
        protected bool readyQuickChargeDisconnect;
        protected int warnInterval = WARN_INTERVAL_USB;

        // Debouncer（デバウンスのフィルター）は機能廃止で削除した
        public int GetWarnInterval()
        {
            return warnInterval;
        }

        public bool ReadyQuickChargeDisconnect
        {
            get => readyQuickChargeDisconnect;
            set => readyQuickChargeDisconnect = value;
        }

        public ControllerOptionsStore optionsStore = null!; // 派生クラスのコンストラクターで設定する
        DS4Windows.Core.Settings.ControllerOptionsStore DS4Windows.Core.Abstractions.IConfigurableDevice.OptionsStore => optionsStore;

        // 旧「ステアリング（ジャイロでハンドル操作）の較正の状態」（wheelCenterPoint／wheel90DegPointLeft／Right／wheelCircle…／wheelPrev…／wheelFullTurnCount／WheelRecalibrateActiveState／
        // WheelCalibrationPoint／wheelCalibratedAxisBitmask）は、機能ごと削除した。

        protected bool exitOutputThread = false;
        public bool ExitOutputThread => exitOutputThread;
        protected bool exitInputThread = false;
        protected object exitLocker = new object();
        protected DeviceExclusiveStatus exclusiveStatus = DeviceExclusiveStatus.Shared;

        public delegate void ReportHandler<TEventArgs>(InputDeviceCore sender, TEventArgs args);

        //public event EventHandler<EventArgs> Report = null;
        public abstract event ReportHandler<EventArgs> Report;
        public virtual event EventHandler<EventArgs>? Removal = null;
        public event EventHandler<EventArgs>? SyncChange = null;
        public event EventHandler<EventArgs>? SerialChange = null;
        //public EventHandler<EventArgs> MotionEvent = null;
        public ReportHandler<EventArgs>? MotionEvent = null;

        public IHidTransport HidDevice => hDevice;

        /// <summary>HID のデバイスパス（<see cref="DS4Windows.Core.Abstractions.IOutputBoundDevice"/>）。</summary>
        public string DevicePath => hDevice.DevicePath;

        public void StartMotionCallback(Action onReport)
        {
            MotionEvent = (sender, args) => onReport();
            Report += MotionEvent;
        }

        public void StopMotionCallback()
        {
            if (MotionEvent != null)
            {
                Report -= MotionEvent;
                MotionEvent = null;
            }
        }

        public void PauseMotionCallback()
        {
            if (MotionEvent != null)
            {
                Report -= MotionEvent;
            }
        }

        public void ResumeMotionCallback()
        {
            if (MotionEvent != null)
            {
                Report += MotionEvent;
            }
        }
        public bool IsHidExclusive => HidDevice.IsExclusive;
        public bool IsExclusive
        {
            get { return exclusiveStatus > DeviceExclusiveStatus.Shared; }
        }

        public DeviceExclusiveStatus CurrentExclusiveStatus
        {
            get => exclusiveStatus;
            set
            {
                exclusiveStatus = value;
            }
        }

        protected bool isDisconnecting = false;
        public bool IsDisconnecting
        {
            get { return isDisconnecting; }
            protected set
            {
                this.isDisconnecting = value;
            }
        }

        public bool IsDisconnectingStatus()
        {
            return this.isDisconnecting;
        }

        protected bool isRemoving = false;
        public bool IsRemoving
        {
            get { return isRemoving; }
            set
            {
                this.isRemoving = value;
            }
        }

        protected bool isRemoved = false;
        public bool IsRemoved
        {
            get { return isRemoved; }
            set
            {
                this.isRemoved = value;
            }
        }

        public object removeLocker = new object();

        public string MacAddress => Mac;
        public event EventHandler? MacAddressChanged;
        public string GetMacAddress()
        {
            return this.Mac;
        }

        public ConnectionType ConnectionType => conType;
        // ILightbarDevice（ライトバーの色の計算が読む値）。既存のメソッドへ委譲する
        bool DS4Windows.Input.Abstractions.ILightbarDevice.IsCharging => IsCharging();
        int DS4Windows.Input.Abstractions.ILightbarDevice.Battery => GetBattery();
        ConnectionType DS4Windows.Input.Abstractions.ILightbarDevice.ConnectionType => GetConnectionType();
        DateTime DS4Windows.Input.Abstractions.ILightbarDevice.LastActive => lastActive;
        byte DS4Windows.Input.Abstractions.ILightbarDevice.LeftHeavySlowRumble => GetLeftHeavySlowRumble();
        byte DS4Windows.Input.Abstractions.ILightbarDevice.LightBarOnDuration => GetLightBarOnDuration();

        public ConnectionType GetConnectionType()
        {
            return this.conType;
        }

        // behavior only active when > 0
        protected int idleTimeout = 0;
        public int IdleTimeout
        {
            get { return idleTimeout; }
            set
            {
                idleTimeout = value;
            }
        }

        public int GetIdleTimeout()
        {
            return idleTimeout;
        }

        public void SetIdleTimeout(int value)
        {
            if (idleTimeout != value)
            {
                idleTimeout = value;
            }
        }

        // Feature set of gamepad (some non-official DS4 gamepads require a bit different logic than a genuine Sony DS4). 0=Default DS4 gamepad feature set.
        protected VidPidFeatureSet featureSet;
        public VidPidFeatureSet FeatureSet
        {
            get { return featureSet; }
            set { featureSet = value; }
        }
        public VidPidFeatureSet ModifyFeatureSetFlag(VidPidFeatureSet featureBitFlag, bool flagSet)
        {
            if (flagSet) featureSet |= featureBitFlag;
            else featureSet &= ~featureBitFlag;
            return featureSet;
        }

        /// <summary>
        /// 「物理コントローラーへ出力データを送る」を ON → OFF にする直前に、1 度だけ、ライトバー消灯・振動ゼロを書く差し込み口
        /// （フラグを立てたあとは何も書かれず、パッドが最後の色を保持するため）。既定は何もしない（出力を書く機種だけが override する）。
        /// 呼び出し側［<c>DeviceConnectionSetup.ApplyProfileOptions</c>］は、<see cref="ModifyFeatureSetFlag"/> で NoOutputData を立てる前に呼ぶ。
        /// </summary>
        public virtual void WriteOutputOffOnce()
        {
        }

        public int Battery => battery;
        public delegate void BatteryUpdateHandler(object sender, EventArgs e);
        // 通知用のイベントは基底の 1 組だけ（各機種の override 再宣言は廃止）。
        // 書き込みは UpdateBattery／UpdateCharging を通す（値が変わったときだけ通知する）
        public event EventHandler? BatteryChanged;
        public int GetBattery()
        {
            return battery;
        }

        protected void UpdateBattery(int percent)
        {
            if (percent == battery) return;

            battery = percent;
            BatteryChanged?.Invoke(this, EventArgs.Empty);
        }

        protected void UpdateCharging(bool IsCharging)
        {
            if (IsCharging == charging) return;

            charging = IsCharging;
            ChargingChanged?.Invoke(this, EventArgs.Empty);
        }

        public bool Charging => charging;
        public event EventHandler? ChargingChanged;
        public bool IsCharging()
        {
            return charging;
        }

        protected long lastTimeElapsed = 0;
        public long GetLastTimeElapsed()
        {
            return lastTimeElapsed;
        }

        public double lastTimeElapsedDouble = 0.0;
        public double GetLastTimeElapsedDouble()
        {
            return lastTimeElapsedDouble;
        }

        public byte RightLightFastRumble
        {
            get { return currentHap.rumbleState.RumbleMotorStrengthRightLightFast; }
            set
            {
                if (currentHap.rumbleState.RumbleMotorStrengthRightLightFast != value)
                    currentHap.rumbleState.RumbleMotorStrengthRightLightFast = value;
            }
        }

        public byte LeftHeavySlowRumble
        {
            get { return currentHap.rumbleState.RumbleMotorStrengthLeftHeavySlow; }
            set
            {
                if (currentHap.rumbleState.RumbleMotorStrengthLeftHeavySlow != value)
                    currentHap.rumbleState.RumbleMotorStrengthLeftHeavySlow = value;
            }
        }

        public byte GetLeftHeavySlowRumble()
        {
            return currentHap.rumbleState.RumbleMotorStrengthLeftHeavySlow;
        }


        protected int rumbleAutostopTime = 0;
        public int RumbleAutostopTime
        {
            get { return rumbleAutostopTime; }
            set
            {
                // Value in milliseconds
                rumbleAutostopTime = value;

                // If autostop timer is disabled (value 0) then stop existing autostop timer otherwise restart it
                if (value <= 0)
                    rumbleAutostopTimer.Reset();
                else
                    rumbleAutostopTimer.Restart();
            }
        }

        public DS4Color LightBarColor
        {
            get { return currentHap.lightbarState.LightBarColor; }
            set
            {
                if (currentHap.lightbarState.LightBarColor.red != value.red || currentHap.lightbarState.LightBarColor.green != value.green || currentHap.lightbarState.LightBarColor.blue != value.blue)
                {
                    currentHap.lightbarState.LightBarColor = value;
                }
            }
        }

        public byte GetLightBarOnDuration()
        {
            return currentHap.lightbarState.LightBarFlashDurationOn;
        }

        // Specify the poll rate interval used for the DS4 hardware when
        // connected via Bluetooth
        protected int btPollRate = 0;
        public int BTPollRate
        {
            get { return btPollRate; }
            set
            {
                if (btPollRate != value && value >= 0 && value <= 16)
                {
                    btPollRate = value;
                }
            }
        }

        public int GetBTPollRate()
        {
            return btPollRate;
        }

        public void SetBTPollRate(int value)
        {
            if (btPollRate != value && value >= 0 && value <= 16)
            {
                btPollRate = value;
            }
        }

        public DS4Touchpad Touchpad { get { return touchpad; } }
        public DS4SixAxis SixAxis { get { return sixAxis; } }

        // デバウンス時間などの設定値。InputDeviceRegistry が、デバイスの生成直後（入力の開始前）に設定する。
        // 未設定のとき（テスト）は、デバウンス 0ms として扱う。
        private DS4Windows.Input.Abstractions.IDeviceLayerSettings? layerSettings;
        public DS4Windows.Input.Abstractions.IDeviceLayerSettings? LayerSettings
        {
            get => layerSettings;
            set { layerSettings = value; }
        }

        public static ConnectionType HidConnectionType(IHidTransport hidDevice)
        {
            ConnectionType result = ConnectionType.USB;
            if (hidDevice.Capabilities.InputReportByteLength == 64)
            {
                if (hidDevice.Capabilities.NumberFeatureDataIndices == 22)
                {
                    result = ConnectionType.SONYWA;
                }
            }
            else
            {
                result = ConnectionType.BT;
            }

            return result;
        }

        protected Queue<Action> eventQueue = new Queue<Action>();
        protected object eventQueueLock = new object();

        protected Thread? timeoutCheckThread = null;
        protected bool timeoutExecuted = false;
        protected bool timeoutEvent = false;
        protected bool runCalib;
        protected bool hasInputEvts = false;
        protected string displayName;
        public string DisplayName => displayName;
        public bool ShouldRunCalib()
        {
            return runCalib;
        }

        protected ManualResetEventSlim readWaitEv = new ManualResetEventSlim();
        public ManualResetEventSlim ReadWaitEv { get => readWaitEv; }

        public virtual byte SerialReportID { get => SERIAL_FEATURE_ID; }

        protected DS4Windows.Core.Model.InputDeviceType deviceType;
        public DS4Windows.Core.Model.InputDeviceType DeviceType { get => deviceType; }

        protected GyroMouseSens gyroMouseSensSettings = null!; // 派生クラスのコンストラクターで設定する
        public virtual GyroMouseSens GyroMouseSensSettings { get => gyroMouseSensSettings; }

        public const int DEFAULT_SLOT_NUMBER = -1;
        protected int deviceSlotNumber = DEFAULT_SLOT_NUMBER;
        public int DeviceSlotNumber
        {
            get => deviceSlotNumber;
            set
            {
                if (deviceSlotNumber == value) return;
                deviceSlotNumber = value;
                DeviceSlotNumberChanged?.Invoke(this, EventArgs.Empty);
            }
        }
        protected event EventHandler? DeviceSlotNumberChanged;
        protected byte deviceSlotMask = 0x00;



        public InputDeviceCore(IHidTransport hidDevice, string disName, VidPidFeatureSet featureSet = VidPidFeatureSet.DefaultDS4)
        {
            hDevice = hidDevice;
            displayName = disName;
            this.featureSet = featureSet;

            exclusiveStatus = DeviceExclusiveStatus.Shared;
            if (hidDevice.IsExclusive)
            {
                exclusiveStatus = DeviceExclusiveStatus.Exclusive;
            }

            if (this.FeatureSet != VidPidFeatureSet.DefaultDS4)
                AppLogger.LogUser(AppLogLevel.Info, "Log_Device_CustomFeatureSet", displayName, conType, this.FeatureSet.ToString("F"));

            runCalib = (this.featureSet & VidPidFeatureSet.NoGyroCalib) == 0;

            touchpad = new DS4Touchpad();
            sixAxis = new DS4SixAxis();
        }

        public abstract void PostInit();




        protected const int DS4_FEATURE_REPORT_5_LEN = 41;
        protected const int DS4_FEATURE_REPORT_5_CRC32_POS = DS4_FEATURE_REPORT_5_LEN - 4;
        public virtual void RefreshCalibration()
        {
        }


        /// <summary>
        /// DS4 の入力レポートの電源のバイト（30 番）から、電池残量（％）と充電中かを求める。読み取りループ（<c>DualShock4Device</c>）と同じ式。
        /// </summary>
        public static int DecodeBatteryPercent(byte powerByte, out bool charging)
        {
            charging = (powerByte & 0x10) != 0;
            int maxBatteryValue = charging ? BATTERY_MAX_USB : BATTERY_MAX;
            return BatteryScale.ToPercent(powerByte & 0x0f, maxBatteryValue);
        }

        /// <summary>
        /// 読み取りのスレッドを始める前（<see cref="StartUpdate"/> の前）に、最初の入力レポートを同期で読み、電池残量だけを取り込む差し込み口。
        /// 接続時のプロファイルの適用（ログ・通知に電池残量を使う）が最初のレポートの読み取りより前に行われ、「Battery: 0%」と出るのを防ぐ。
        /// 既定は何もしない。充電の状態とイベントには触れない（最初のレポートで読み取りのスレッドが通知する）。
        /// </summary>
        public virtual void PrimeBatteryReading()
        {
        }

        /// <summary>
        /// 振動のエミュレーションの方式と触覚の強さを、この機種へ反映する差し込み口。反映できたら true。
        /// 既定は false（振動のエミュレーションの方式を持たない機種）。
        /// </summary>
        public virtual bool ApplyRumbleEmulation(RumbleEmulationMode mode, byte hapticPowerLevel)
        {
            return false;
        }

        public virtual bool UsesGenericRumbleRescaleSetting => false;

        public virtual void StartUpdate()
        {
        }


        public virtual void StopUpdate()
        {
            if (ds4Input != null &&
                ds4Input.IsAlive && !ds4Input.ThreadState.HasFlag(System.Threading.ThreadState.Stopped) &&
                !ds4Input.ThreadState.HasFlag(System.Threading.ThreadState.AbortRequested))
            {
                try
                {
                    exitInputThread = true;
                    //ds4Input.Interrupt();
                    if (!abortInputThread)
                    {
                        hDevice.CancelIO();
                        ds4Input.Join();
                    }
                }
                catch (Exception e)
                {
                    AppLogger.LogDebug($"[InputDeviceCore] {Mac}: exception: {e.Message}");
                }
            }

            StopOutputUpdate();
        }

        protected virtual void StopOutputUpdate()
        {
            lock (exitLocker)
            {
                if (ds4Output != null &&
                    ds4Output.IsAlive && !ds4Output.ThreadState.HasFlag(System.Threading.ThreadState.Stopped) &&
                    !ds4Output.ThreadState.HasFlag(System.Threading.ThreadState.AbortRequested))
                {
                    try
                    {
                        exitOutputThread = true;
                        ds4Output.Interrupt();
                        ds4Output.Join();
                    }
                    catch (Exception e)
                    {
                        AppLogger.LogDebug($"[InputDeviceCore] {Mac}: exception: {e.Message}");
                    }
                }
            }
        }

        protected bool WriteOutput(byte[] outputBuffer)
        {
            if (conType == ConnectionType.BT)
            {
                //if ((this.featureSet & VidPidFeatureSet.OnlyOutputData0x05) == 0)
                //    return hDevice.WriteOutputReportViaControl(outputReport);

                // Use Interrupt endpoint for BT DS4 connected devices now
                return hDevice.WriteOutputReportViaInterrupt(outputBuffer, READ_STREAM_TIMEOUT);
            }
            else
            {
                return hDevice.WriteOutputReportViaInterrupt(outputBuffer, READ_STREAM_TIMEOUT);
            }
        }

        protected bool WriteOutput()
        {
            if (conType == ConnectionType.BT)
            {
                //if ((this.featureSet & VidPidFeatureSet.OnlyOutputData0x05) == 0)
                //    return hDevice.WriteOutputReportViaControl(outputReport);

                // Use Interrupt endpoint for almost BT DS4 connected devices now
                return hDevice.WriteOutputReportViaInterrupt(outputReport, READ_STREAM_TIMEOUT);
            }
            else
            {
                return hDevice.WriteOutputReportViaInterrupt(outReportBuffer, READ_STREAM_TIMEOUT);
            }
        }

        protected readonly Stopwatch rumbleAutostopTimer = new Stopwatch(); // Autostop timer to stop rumble motors if those are stuck in a rumble state

#pragma warning disable CS0414 // outputPendCount is assigned in some flows but read paths may be conditional
        private byte outputPendCount = 0;
#pragma warning restore CS0414
        private const int OUTPUT_MIN_COUNT_BT = 3;
        protected readonly Stopwatch standbySw = new Stopwatch();

        /** Is the device alive and receiving valid sensor input reports? */
        public virtual bool IsAlive()
        {
            return priorInputReport30 != 0xff;
        }

        protected byte priorInputReport30 = 0xff;

        protected bool synced = false;
        public bool Synced
        {
            get { return synced; }
            set
            {
                if (synced != value)
                {
                    synced = value;
                }
            }
        }

        public bool IsSynced()
        {
            return synced;
        }

        /// <summary>
        /// Used to tell the input thread to temporarily stop firing the
        /// Report event. Keeps linked methods from being executed
        /// </summary>
        protected bool fireReport = true;
        public bool FireReport
        {
            get => fireReport;
            set
            {
                fireReport = value;
            }
        }

        public double Latency = 0.0;
        public long lastInputReportTimestamp = 0; // 実機レポート受信開始タイムスタンプ public string error;public double Latency = 0.0;
        public string? error;
        public bool firstReport = true;
        public bool oldCharging = false;
        protected DateTime utcNow = DateTime.UtcNow;
        protected bool ds4InactiveFrame = true;
        protected bool idleInput = true;


        protected const int BT_INPUT_REPORT_CRC32_POS = 74; //last 4 bytes of the 78-sized input report are crc32
        public const uint DefaultPolynomial = 0xedb88320u;
        private const int SONYWA_FEATURE_REPORT_LENGTH = 64;
        protected uint HamSeed = 2351727372;

        /// <summary>入力の読み取りのループ（専用のスレッドで走る）。機種ごとの派生が実装する（<see cref="DS4Windows.Input.Devices.InputLoopDevice"/>）。</summary>
        protected abstract void RunInputLoop();

        /// <summary>派生のループから、アダプターの同期の変化（<c>SyncChange</c>）を通知する。</summary>
        protected void RaiseSyncChange() => SyncChange?.Invoke(this, EventArgs.Empty);


        protected virtual void SendOutputReport(bool synchronous, bool force = false, bool quitOutputThreadOnError = true)
        {
        }



        public virtual bool DisconnectWireless(bool callRemoval = false)
        {
            bool result = false;
            if (conType == ConnectionType.BT)
            {
                result = DisconnectBT(callRemoval);
            }
            else if (conType == ConnectionType.SONYWA)
            {
                result = DisconnectDongle(callRemoval);
            }

            return result;
        }

        public virtual bool DisconnectBT(bool callRemoval = false)
        {
            if (Mac != null)
            {
                // Wait for output report to be written
                StopOutputUpdate();
                AppLogger.LogDebug("Trying to disconnect BT device " + Mac);
                IntPtr btHandle = IntPtr.Zero;
                uint IOCTL_BTH_DISCONNECT_DEVICE = 0x41000c;

                byte[] btAddr = new byte[8];
                string[] sbytes = Mac.Split(':');
                for (int i = 0; i < 6; i++)
                {
                    // parse hex byte in reverse order
                    btAddr[5 - i] = Convert.ToByte(sbytes[i], 16);
                }

                long lbtAddr = BitConverter.ToInt64(btAddr, 0);

                bool success = false;

                lock (outputReport)
                {
                    NativeMethods.BLUETOOTH_FIND_RADIO_PARAMS p = new NativeMethods.BLUETOOTH_FIND_RADIO_PARAMS();
                    p.dwSize = Marshal.SizeOf(typeof(NativeMethods.BLUETOOTH_FIND_RADIO_PARAMS));
                    IntPtr searchHandle = NativeMethods.BluetoothFindFirstRadio(ref p, ref btHandle);
                    int bytesReturned = 0;

                    while (!success && btHandle != IntPtr.Zero)
                    {
                        success = NativeMethods.DeviceIoControl(btHandle, IOCTL_BTH_DISCONNECT_DEVICE, ref lbtAddr, 8, IntPtr.Zero, 0, ref bytesReturned, IntPtr.Zero);
                        NativeMethods.CloseHandle(btHandle);
                        if (!success)
                        {
                            if (!NativeMethods.BluetoothFindNextRadio(searchHandle, ref btHandle))
                                btHandle = IntPtr.Zero;
                        }
                    }

                    NativeMethods.BluetoothFindRadioClose(searchHandle);
                    AppLogger.LogDebug("Disconnect successful: " + success);
                }

                success = true; // XXX return value indicates failure, but it still works?
                if (success)
                {
                    IsDisconnecting = true;

                    if (callRemoval)
                    {
                        Removal?.Invoke(this, EventArgs.Empty);

                        //System.Threading.Tasks.Task.Factory.StartNew(() => { Removal?.Invoke(this, EventArgs.Empty); });
                    }
                }

                return success;
            }

            return false;
        }

        public virtual bool DisconnectDongle(bool remove = false)
        {
            bool result = false;
            byte[] disconnectReport = new byte[SONYWA_FEATURE_REPORT_LENGTH];
            disconnectReport[0] = 0xe2;
            disconnectReport[1] = 0x02;
            Array.Clear(disconnectReport, 2, SONYWA_FEATURE_REPORT_LENGTH - 2);

            if (remove)
                StopOutputUpdate();

            lock (outputReport)
            {
                result = hDevice.WriteFeatureReport(disconnectReport);
            }

            if (result && remove)
            {
                isDisconnecting = true;

                Removal?.Invoke(this, EventArgs.Empty);

                //System.Threading.Tasks.Task.Factory.StartNew(() => { Removal?.Invoke(this, EventArgs.Empty); });
                //Removal?.Invoke(this, EventArgs.Empty);
            }
            else if (result && !remove)
            {
                isRemoved = true;
            }

            return result;
        }

        protected DS4HapticState testRumble = new DS4HapticState();

        public void SetRumble(byte rightLightFastMotor, byte leftHeavySlowMotor)
        {
            testRumble.rumbleState.RumbleMotorStrengthRightLightFast = rightLightFastMotor;
            testRumble.rumbleState.RumbleMotorStrengthLeftHeavySlow = leftHeavySlowMotor;
            testRumble.rumbleState.RumbleMotorsExplicitlyOff = rightLightFastMotor == 0 && leftHeavySlowMotor == 0;
            testRumble.dirty = true;

            // If rumble autostop timer (msecs) is enabled for this device then restart autostop timer everytime rumble is modified (or stop the timer if rumble is set to zero)
            if (rumbleAutostopTime > 0)
            {
                if (testRumble.rumbleState.RumbleMotorsExplicitlyOff)
                    rumbleAutostopTimer.Reset();   // Stop an autostop timer because ViGem driver sent properly a zero rumble notification
                else if (currentHap.rumbleState.RumbleMotorStrengthLeftHeavySlow != leftHeavySlowMotor || currentHap.rumbleState.RumbleMotorStrengthRightLightFast != rightLightFastMotor)
                    rumbleAutostopTimer.Restart(); // Start an autostop timer to stop potentially stuck rumble motor because of lost rumble notification events from ViGem driver
            }
        }

        protected void MergeStates()
        {
            if (testRumble.IsRumbleSet())
            {
                if (testRumble.rumbleState.RumbleMotorsExplicitlyOff)
                    testRumble.rumbleState.RumbleMotorsExplicitlyOff = false;

                //currentHap.rumbleState.RumbleMotorStrengthLeftHeavySlow = testRumble.rumbleState.RumbleMotorStrengthLeftHeavySlow;
                //currentHap.rumbleState.RumbleMotorStrengthRightLightFast = testRumble.rumbleState.RumbleMotorStrengthRightLightFast;
                currentHap.rumbleState = testRumble.rumbleState;
            }

            currentHap.dirty = testRumble.dirty;
            testRumble.dirty = false;
        }

        public ControllerState GetRawCurrentState()
        {
            return cState.Clone();
        }

        public ControllerState GetRawPreviousState()
        {
            return pState.Clone();
        }

        public void GetRawCurrentState(ControllerState state)
        {
            cState.CopyTo(state);
        }

        public void GetRawPreviousState(ControllerState state)
        {
            pState.CopyTo(state);
        }

        public virtual ControllerState GetCurrentStateRef()
        {
            return cState;
        }

        public virtual ControllerState GetPreviousStateRef()
        {
            return pState;
        }

        public ControllerState GetRawCurrentStateRef()
        {
            return cState;
        }

        public ControllerState GetRawPreviousStateRef()
        {
            return pState;
        }

        public bool IsDS4Idle()
        {
            if (cState.Square || cState.Cross || cState.Circle || cState.Triangle)
                return false;
            if (cState.DpadUp || cState.DpadLeft || cState.DpadDown || cState.DpadRight)
                return false;
            if (cState.L3 || cState.R3 || cState.L1 || cState.R1 || cState.Share || cState.Options || cState.PS)
                return false;
            if (cState.L2 != 0 || cState.R2 != 0)
                return false;
            // TODO calibrate to get an accurate jitter and center-play range and centered position
            const int slop = 64;
            if (cState.LX <= 127 - slop || cState.LX >= 128 + slop || cState.LY <= 127 - slop || cState.LY >= 128 + slop)
                return false;
            if (cState.RX <= 127 - slop || cState.RX >= 128 + slop || cState.RY <= 127 - slop || cState.RY >= 128 + slop)
                return false;
            if (cState.Touch1 || cState.Touch2 || cState.TouchButton)
                return false;
            return true;
        }

        /// <summary>
        /// 読み取りのループの 1 フレームごとに、アイドルの判定と最後に操作した時刻（<c>lastActive</c>）を更新する。
        /// USB は時刻の更新だけ。BT などは、アイドルが <c>idleTimeout</c> 秒続き、充電中でなければ true（切断すべき）を返す。
        /// 切断の手順（<c>DisconnectBT</c> など）は機種ごとに違うため、呼ぶ側が行う。
        /// </summary>
        protected bool EvaluateIdleDisconnect(DateTime now)
        {
            if (conType == ConnectionType.USB)
            {
                if (idleTimeout == 0)
                {
                    lastActive = now;
                }
                else
                {
                    idleInput = IsDS4Idle();
                    if (!idleInput)
                    {
                        lastActive = now;
                    }
                }

                return false;
            }

            bool shouldDisconnect = false;
            if (!isRemoved && idleTimeout > 0)
            {
                idleInput = IsDS4Idle();
                if (idleInput)
                {
                    DateTime timeout = lastActive + TimeSpan.FromSeconds(idleTimeout);
                    if (!charging)
                        shouldDisconnect = now >= timeout;
                }
                else
                {
                    lastActive = now;
                }
            }
            else
            {
                lastActive = now;
            }

            if (shouldDisconnect)
            {
                AppLogger.LogUser(AppLogLevel.Info, "Log_Device_IdleDisconnect", Mac.ToString());
            }

            return shouldDisconnect;
        }

        /// <summary>読み取りのループの末尾で、他のスレッドから積まれた処理（<c>eventQueue</c>）を、その時点の件数だけ実行する。</summary>
        protected void RunQueuedInputEvents()
        {
            if (!hasInputEvts)
            {
                return;
            }

            lock (eventQueueLock)
            {
                for (int actInd = 0, actLen = eventQueue.Count; actInd < actLen; actInd++)
                {
                    eventQueue.Dequeue().Invoke();
                }

                hasInputEvts = false;
            }
        }

        protected DS4HapticState currentHap = new DS4HapticState();
        public void SetHapticState(ref DS4HapticState hs)
        {
            currentHap = hs;
            currentHap.dirty = true;
        }

        public void SetLightbarState(ref DS4LightbarState lightState)
        {
            if (!currentHap.lightbarState.Equals(lightState))
            {
                currentHap.dirty = true;
            }

            currentHap.lightbarState = lightState;
        }

        public ref DS4LightbarState GetLightbarStateRef()
        {
            return ref currentHap.lightbarState;
        }

        public void SetRumbleState(ref DS4ForceFeedbackState rumbleState)
        {
            currentHap.rumbleState = rumbleState;
        }

        public override string ToString()
        {
            return Mac;
        }

        protected void RunRemoval()
        {
            Removal?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>登録済みの <c>Report</c> の購読者を全て外す。</summary>
        public abstract void RemoveReportHandlers();

        public void QueueEvent(Action act)
        {
            lock (eventQueueLock)
            {
                eventQueue.Enqueue(act);
                hasInputEvts = true;
            }
        }

        /// <summary>
        /// Must not be run from input thread. Waits for input thread to be in a wait state
        /// and then tell thread to no longer invoke the Report event. Input thread will then
        /// resume followed by invoking the action passed. Flag will be set to have
        /// Report event to resume being invoked after
        /// </summary>
        /// <param name="act">Action to execute in current thread</param>
        public void HaltReportingRunAction(Action act)
        {
            // Wait for controller to be in a wait period
            bool result = readWaitEv.Wait(millisecondsTimeout: 500);
            if (result)
            {
                readWaitEv.Reset();

                // Tell device to no longer fire reports
                fireReport = false;

                // Flag is set. Allow input thread to resume
                readWaitEv.Set();

                // Invoke main desired action
                act?.Invoke();

                // Start firing reports again
                fireReport = true;
            }
        }

        public void UpdateSerial()
        {
            hDevice.ResetSerial();
            string tempMac = hDevice.ReadSerial(SerialReportID);
            if (tempMac != Mac)
            {
                Mac = tempMac;
                SerialChange?.Invoke(this, EventArgs.Empty);
                MacAddressChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        public bool IsValidSerial()
        {
            return !Mac.Equals(BLANK_SERIAL);
        }

        public static bool IsValidSerial(string test)
        {
            return !test.Equals(BLANK_SERIAL);
        }

        private bool abortInputThread = false;
        public void PrepareAbort()
        {
            abortInputThread = true;
        }

        public virtual void PrepareTriggerEffect(DS4Windows.Input.Devices.TriggerId trigger,
            DS4Windows.Core.Model.TriggerEffects effect, DS4Windows.Core.Model.TriggerEffectSettings effectSettings)
        {
        }

        public virtual void CheckControllerNumDeviceSettings(int numControllers)
        {
        }

        public virtual void LoadStoreSettings()
        {
        }
    }
}
