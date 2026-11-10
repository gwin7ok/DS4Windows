using System;
using System.Diagnostics;
using System.Threading;
using DS4Windows.Core.Model;
using DS4Windows.Input.Abstractions;
using DS4Windows.Input.Hid;

namespace DS4Windows.Input.Devices
{
    /// <summary>
    /// <see cref="IDeviceProtocol"/> で動く、共通の単純なデバイス。読み取りのループ（読み取り→解析→電池→<c>Report</c>→出力）、
    /// 切断（接続が切れたら <c>Removal</c>）、停止を一通り持つ。機種固有のバイト列との変換は、プロトコルが受け持つ。
    /// ジャイロ、タッチパッド、アイドル切断、複数の接続方式の動作は持たない（必要な機種は <c>DS4Device</c> の派生で書く）。
    /// </summary>
    public class ProtocolDevice : DS4Device
    {
        private readonly IDeviceProtocol protocol;
        private DS4HapticState previousHapticState = new DS4HapticState();
        private bool outputDirty;

        // 基底の Report は仮想のイベント。派生で持ち直し、読み取りのスレッドから発火する
        public override event ReportHandler<EventArgs> Report = null;

        public ProtocolDevice(IDeviceProtocol protocol, IHidTransport hidDevice, string displayName, VidPidFeatureSet featureSet = VidPidFeatureSet.DefaultDS4)
            : base(hidDevice, displayName, featureSet)
        {
            this.protocol = protocol ?? throw new ArgumentNullException(nameof(protocol));
            synced = true;
        }

        public override void PostInit()
        {
            conType = protocol.DetermineConnectionType(hDevice);
            Mac = hDevice.GenerateFakeHwSerial();
            deviceType = protocol.DeviceType;
            gyroMouseSensSettings = new GyroMouseSens();
            inputReport = new byte[hDevice.Capabilities.InputReportByteLength];
            outputReport = new byte[hDevice.Capabilities.OutputReportByteLength];
            warnInterval = WARN_INTERVAL_USB;
        }

        // 偽のシリアルを使う機種のため、OS 側の切断の手順は持たない
        public override bool DisconnectBT(bool callRemoval = false) => false;

        public override bool DisconnectDongle(bool remove = false) => false;

        public override bool DisconnectWireless(bool callRemoval = false) => false;

        public override bool IsAlive() => synced;

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

        /// <summary>読み取りのループの本体（スレッドから呼ぶ。テストでは直接呼べる）。</summary>
        protected internal void ReadInput()
        {
            firstActive = DateTime.UtcNow;
            hDevice.SetNumInputBuffers(3);
            long oldTime = 0;
            exitInputThread = false;
            timeoutExecuted = false;
            ds4InactiveFrame = true;
            idleInput = true;

            while (!exitInputThread)
            {
                // 読み取りで待つ間は「待機中」（HaltReportingRunAction が、この間に報告を止める）
                readWaitEv.Set();
                HidReadStatus status = hDevice.ReadFile(inputReport);
                readWaitEv.Wait();
                readWaitEv.Reset();

                if (status == HidReadStatus.NotConnected || status == HidReadStatus.ReadError)
                {
                    exitInputThread = true;
                    StopOutputUpdate();
                    isDisconnecting = true;
                    RunRemoval();
                    break;
                }

                if (status != HidReadStatus.Success)
                {
                    continue;
                }

                long now = Stopwatch.GetTimestamp();
                lastTimeElapsedDouble = (now - oldTime) * (1.0 / Stopwatch.Frequency) * 1000.0;
                lastTimeElapsed = (long)lastTimeElapsedDouble;
                oldTime = now;

                utcNow = DateTime.UtcNow;
                cState.PacketCounter = pState.PacketCounter + 1;
                cState.ReportTimeStamp = utcNow;
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
                lastActive = utcNow;

                if (fireReport)
                {
                    Report?.Invoke(this, EventArgs.Empty);
                }

                WriteOutputIfChanged();
                cState.CopyTo(pState);
                RunQueuedEvents();
            }

            timeoutExecuted = true;
        }

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

        private void RunQueuedEvents()
        {
            if (!hasInputEvts)
            {
                return;
            }

            lock (eventQueueLock)
            {
                while (eventQueue.Count > 0)
                {
                    eventQueue.Dequeue().Invoke();
                }

                hasInputEvts = false;
            }
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
