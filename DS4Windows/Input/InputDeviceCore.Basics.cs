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
    /// <summary>定数と、電池・無操作の設定・切断の状態の小さな部品。</summary>
    public partial class InputDeviceCore
    {
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

        public void PauseMotionCallback()
        {
            if (MotionEvent != null)
            {
                Report -= MotionEvent;
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

        public void SetIdleTimeout(int value)
        {
            if (idleTimeout != value)
            {
                idleTimeout = value;
            }
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

        public void SetBTPollRate(int value)
        {
            if (btPollRate != value && value >= 0 && value <= 16)
            {
                btPollRate = value;
            }
        }

        protected Thread? timeoutCheckThread = null;
        protected bool timeoutExecuted = false;
        protected bool timeoutEvent = false;
        protected bool runCalib;
        protected string displayName;
        public string DisplayName => displayName;
        public bool ShouldRunCalib()
        {
            return runCalib;
        }
    }
}
