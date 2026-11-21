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
using System.Threading;
using System.Diagnostics;

using DS4Windows.Core.Logging;
using DS4Windows.Core.Model;
using DS4Windows.Core.Settings;
using DS4Windows.Input.Abstractions;

namespace DS4Windows.Input
{

    /// <summary>
    /// 全ての入力機種が共有する中核: 状態（現在・直前）、電池・充電、イベント、設定、切断、出力の合成。
    /// 読み取りのループは持たず、<see cref="DS4Windows.Input.Devices.InputLoopDevice"/> が機種ごとの差し込み口で動かす。
    /// </summary>
    public abstract partial class InputDeviceCore : DS4Windows.Core.Abstractions.IConfigurableDevice, DS4Windows.Input.Abstractions.ILightbarDevice,
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

        public bool IsDisconnectingStatus()
        {
            return this.isDisconnecting;
        }

        public object removeLocker = new object();

        public string MacAddress => Mac;
        public event EventHandler? MacAddressChanged;
        public string GetMacAddress()
        {
            return this.Mac;
        }

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

        public byte GetLeftHeavySlowRumble()
        {
            return Haptics.Current.rumbleState.RumbleMotorStrengthLeftHeavySlow;
        }

        public byte GetLightBarOnDuration()
        {
            return Haptics.Current.lightbarState.LightBarFlashDurationOn;
        }

        public int GetBTPollRate()
        {
            return btPollRate;
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

        private readonly DeviceEventQueue _events = new DeviceEventQueue();

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

        public virtual bool UsesGenericRumbleRescaleSetting => false;

        public virtual void StartUpdate()
        {
        }

        /// <summary>触覚（振動・ライトバー）の出力の状態。機種の出力の組み立てが読む。</summary>
        private HapticOutputState? _haptics;
        // 初期化を通らずに作られた機器（テストの GetUninitializedObject）でも使えるよう、初回の参照で作る
        protected HapticOutputState Haptics => _haptics ??= new HapticOutputState();

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

        public void SetHapticState(ref DS4HapticState hs) => Haptics.SetHaptic(ref hs);

        public ref DS4LightbarState GetLightbarStateRef() => ref Haptics.Current.lightbarState;

        public void SetRumbleState(ref DS4ForceFeedbackState rumbleState) => Haptics.Current.rumbleState = rumbleState;

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
