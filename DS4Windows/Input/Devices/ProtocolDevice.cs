using System;
using System.Diagnostics;
using System.Threading;
using DS4Windows.Core.Logging;
using DS4Windows.Core.Model;
using DS4Windows.Input.Abstractions;
using DS4Windows.Input.Hid;

namespace DS4Windows.Input.Devices
{
    /// <summary>
    /// <see cref="IDeviceProtocol"/> で動く、共通の単純なデバイス。読み取りのループ（読み取り→解析→電池→<c>Report</c>→出力）、
    /// 切断（接続が切れたら <c>Removal</c>）、停止を一通り持つ。機種固有のバイト列との変換は、プロトコルが受け持つ。
    /// ジャイロ、タッチパッド、アイドル切断、複数の接続方式の動作は持たない（必要な機種は <c>InputDeviceCore</c> の派生で書く）。
    /// </summary>
    public class ProtocolDevice : InputLoopDevice
    {
        private readonly IDeviceProtocol protocol;
        private DS4HapticState previousHapticState = new DS4HapticState();
        private bool outputDirty;

        public ProtocolDevice(IDeviceProtocol protocol, IHidTransport hidDevice, string displayName, VidPidFeatureSet featureSet = VidPidFeatureSet.DefaultDS4)
            : base(hidDevice, displayName, featureSet)
        {
            this.protocol = protocol ?? throw new ArgumentNullException(nameof(protocol));
            synced = true;
        }

        public override void PostInit()
        {
            conType = protocol.DetermineConnectionType(hDevice);
            // Bluetooth のアドレスが取れる機種は、それを識別に使う（Bluetooth の切断に必要）。取れなければ、擬似のシリアル
            Mac = (conType == ConnectionType.BT ? protocol.TryGetBluetoothAddress(hDevice) : null) ?? hDevice.GenerateFakeHwSerial();
            deviceType = protocol.DeviceType;
            gyroMouseSensSettings = new GyroMouseSens();
            inputReport = new byte[hDevice.Capabilities.InputReportByteLength];
            outputReport = new byte[hDevice.Capabilities.OutputReportByteLength];
            warnInterval = WARN_INTERVAL_USB;
        }

        // Bluetooth のアドレスを持つ機種（Bluetooth 接続）だけ、共通の切断（基底）を使う。擬似のシリアルの機種は、切断できない
        public override bool DisconnectBT(bool callRemoval = false)
        {
            if (conType != ConnectionType.BT)
            {
                return false;
            }

            // 機種固有の手順（Bluetooth LE など）があれば、それを使う。なければ共通の手順（従来の Bluetooth 向け）
            bool? custom = protocol.TryDisconnectBluetooth(hDevice);
            if (custom.HasValue)
            {
                if (custom.Value)
                {
                    StopOutputUpdate();
                    IsDisconnecting = true;
                    if (callRemoval)
                    {
                        RaiseRemoval();
                    }
                }

                return custom.Value;
            }

            return HasBluetoothAddress && base.DisconnectBT(callRemoval);
        }

        public override bool DisconnectDongle(bool remove = false) => false;

        public override bool DisconnectWireless(bool callRemoval = false) => DisconnectBT(callRemoval);

        private bool HasBluetoothAddress => protocol.TryGetBluetoothAddress(hDevice) != null;

        // 接続時のログ・画面が 0% を出さないよう、読み取りのスレッドの前に最初のレポートから電池だけを取り込む（DS4 の PrimeBatteryReading と同じ考え方）
        protected override void PrimeBatteryForOtherDevice()
        {
            if (ds4Input != null || isDisconnecting || inputReport == null)
            {
                return;
            }

            try
            {
                if (hDevice.ReadFile(inputReport) != HidReadStatus.Success)
                {
                    return;
                }

                battery = protocol.TryReadBattery(inputReport, out int percent, out _) ? percent : 99;
            }
            catch (Exception ex)
            {
                AppLogger.LogWarn($"[DI] PrimeBatteryReading failed: {ex.Message}");
            }
        }

        // DS4 と同じく、最初の入力を受けるまでは「生きている」とみなさない。接続時のプロファイルの適用が、生きている機器に対して
        // 仮想パッドの付け直し（取り外し→接続）を予約するため、すぐ true だと、接続のたびに仮想パッドが二重に接続される
        private volatile bool receivedFrame;

        public override bool IsAlive() => synced && receivedFrame;

        public override void StartUpdate()
        {
            inputReportErrorCount = 0;
            if (ds4Input != null)
            {
                return;
            }

            ds4Input = new Thread(ReadInput)
            {
                Priority = ThreadPriority.AboveNormal,
                Name = protocol.DeviceType + " input thread: " + Mac,
                IsBackground = true,
            };
            ds4Input.Start();
        }

        protected override void OnLoopStarting()
        {
            exitInputThread = false;
            timeoutExecuted = false;
        }

        protected override void ProcessFrame()
        {
            receivedFrame = true;
            protocol.Parser.Parse(inputReport, cState);

            if (protocol.TryReadBattery(inputReport, out int percent, out bool isCharging))
            {
                UpdateCharging(isCharging);
                if (percent != battery)
                {
                    UpdateBattery(percent);
                    outputDirty = true;
                }

                cState.Battery = (byte)battery;
            }
            else
            {
                // 電池の値を送らない機種は、低電池の警告が出続けないよう 99% にする
                battery = 99;
                cState.Battery = 99;
            }

            double elapsedSeconds = lastTimeElapsedDouble * 0.001;
            cState.elapsedTime = elapsedSeconds;
            cState.totalMicroSec = pState.totalMicroSec + (uint)(elapsedSeconds * 1000000);
        }

        protected override void WriteOutputFrame() => WriteOutputIfChanged();

        private void WriteOutputIfChanged()
        {
            MergeStates();
            bool changed = currentHap.dirty || !previousHapticState.Equals(currentHap);
            if (changed)
            {
                outputDirty = true;
            }

            if (outputDirty && protocol.TryBuildOutputReport(currentHap, battery, outputReport))
            {
                hDevice.WriteOutputReportViaInterrupt(outputReport, READ_STREAM_TIMEOUT);
                previousHapticState = currentHap;
            }

            outputDirty = false;
            currentHap.dirty = false;
        }

        // 停止のとき、全ての出力を止める（空の状態を書く）
        protected override void StopOutputUpdate()
        {
            if (outputReport != null && protocol.TryBuildOutputReport(new DS4HapticState(), battery, outputReport))
            {
                hDevice.WriteOutputReportViaInterrupt(outputReport, READ_STREAM_TIMEOUT);
            }
        }
    }
}
