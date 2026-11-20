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
    /// <summary>状態の読み取り（接続の種類・電池・振動・ライトバー・排他の状態）と停止の処理。</summary>
    public partial class InputDeviceCore
    {
        public void StopMotionCallback()
        {
            if (MotionEvent != null)
            {
                Report -= MotionEvent;
                MotionEvent = null;
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

        public ConnectionType ConnectionType => conType;
        // ILightbarDevice（ライトバーの色の計算が読む値）。既存のメソッドへ委譲する
        bool DS4Windows.Input.Abstractions.ILightbarDevice.IsCharging => IsCharging();
        int DS4Windows.Input.Abstractions.ILightbarDevice.Battery => GetBattery();
        ConnectionType DS4Windows.Input.Abstractions.ILightbarDevice.ConnectionType => GetConnectionType();
        DateTime DS4Windows.Input.Abstractions.ILightbarDevice.LastActive => lastActive;
        byte DS4Windows.Input.Abstractions.ILightbarDevice.LeftHeavySlowRumble => GetLeftHeavySlowRumble();
        byte DS4Windows.Input.Abstractions.ILightbarDevice.LightBarOnDuration => GetLightBarOnDuration();

        public int Battery => battery;
        public delegate void BatteryUpdateHandler(object sender, EventArgs e);
        // 通知用のイベントは基底の 1 組だけ（各機種の override 再宣言は廃止）。
        // 書き込みは UpdateBattery／UpdateCharging を通す（値が変わったときだけ通知する）
        public event EventHandler? BatteryChanged;
        public int GetBattery()
        {
            return battery;
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
    }
}
