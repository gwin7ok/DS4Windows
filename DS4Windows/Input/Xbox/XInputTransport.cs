using System;
using System.Buffers.Binary;
using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using DS4Windows.Core.Logging;
using DS4Windows.Input.Abstractions;
using DS4Windows.Input.Hid;

namespace DS4Windows.Input.Xbox
{
    /// <summary>
    /// XInput の機器 1 台を、<see cref="IHidTransport"/> に見せる通信路。XInput は、変化を待つ読み取りを持たないため、一定の間隔（4ms）で状態を読み、
    /// <see cref="XInputReportLayout"/> の合成レポートにして返す。これで、共通のデバイス（<c>ProtocolDevice</c>）の読み取りのループを、そのまま使える。
    /// XInput の番号は、機器を繋ぎ直すと変わるため、読めなくなったら XUSB のインターフェースから番号を引き直し、それでも読めなければ切断（<see cref="HidReadStatus.NotConnected"/>）にする。
    /// 出力レポートは [0]=レポート ID、[1]=左モーター、[2]=右モーター（各 0〜255）で、XInput の振動へ変える。
    /// </summary>
    public sealed class XInputTransport : IHidTransport
    {
        /// <summary>出力レポートの長さ（レポート ID＋左右のモーター）。</summary>
        public const int OutputReportLength = 3;

        private const int PollIntervalMs = 4;

        // 電池は、4 ms ごとに読む必要がない（変化はゆっくり）。間は前回の値を使う
        private const int BatteryIntervalMs = 1000;

        private readonly IXInputApi api;
        private readonly IXusbInterfaceSource interfaces;
        private readonly ManualResetEventSlim cancelled = new ManualResetEventSlim(false);
        private long nextPollTicks;
        private readonly IBluetoothBatteryReader bluetoothBattery;
        private long nextBatteryTicks;
        private bool batteryLogged;
        private XInputBattery battery = new XInputBattery(XInputBattery.TypeUnknown, 0);
        private int playerIndex;
        private bool disposed;

        public XInputTransport(IXInputApi api, IXusbInterfaceSource interfaces, XusbInterfaceInfo info)
            : this(api, interfaces, info, null)
        {
        }

        /// <param name="bluetoothBattery">Bluetooth の機器の電池を、GATT のバッテリーサービスから読む窓口（なければ XInput の API だけ）。</param>
        public XInputTransport(IXInputApi api, IXusbInterfaceSource interfaces, XusbInterfaceInfo info, IBluetoothBatteryReader bluetoothBattery)
        {
            this.bluetoothBattery = bluetoothBattery;
            this.api = api ?? throw new ArgumentNullException(nameof(api));
            this.interfaces = interfaces ?? throw new ArgumentNullException(nameof(interfaces));
            if (info == null)
            {
                throw new ArgumentNullException(nameof(info));
            }

            Info = info;
            playerIndex = info.PlayerIndex;
            DevicePath = info.DevicePath;
            Attributes = new HidDeviceAttributes(info.VendorId, info.ProductId, 0);
            Capabilities = new HidDeviceCapabilities((short)XInputReportLayout.ReportLength, (short)OutputReportLength);
        }

        /// <summary>この通信路の元の XUSB インターフェース。</summary>
        public XusbInterfaceInfo Info { get; }

        /// <summary>いま使っている XInput の番号（0〜3）。</summary>
        public int PlayerIndex => playerIndex;

        public bool IsOpen { get; private set; } = true;
        public bool IsExclusive => false;
        public bool IsConnected => !disposed && !cancelled.IsSet;
        public string Description => "XInput";
        public HidDeviceCapabilities Capabilities { get; }
        public HidDeviceAttributes Attributes { get; }
        public string DevicePath { get; }
        public string ParentPath => null;

        public void OpenDevice(bool exclusive)
        {
            IsOpen = true;
        }

        public void CloseDevice()
        {
            IsOpen = false;
        }

        public void CancelIO()
        {
            cancelled.Set();
        }

        public void Dispose()
        {
            if (disposed)
            {
                return;
            }

            disposed = true;
            cancelled.Set();
            IsOpen = false;
        }

        public HidReadStatus ReadFile(Span<byte> inputBuffer, uint timeout = uint.MaxValue)
        {
            if (disposed || inputBuffer.Length < XInputReportLayout.ReportLength)
            {
                return HidReadStatus.NotConnected;
            }

            WaitForNextPoll();
            if (cancelled.IsSet)
            {
                return HidReadStatus.NotConnected;
            }

            if (!api.TryGetState(playerIndex, out XInputSnapshot state))
            {
                // 番号が変わっただけかもしれない。引き直して、読めれば続け、だめなら切断にする
                int moved = interfaces.ReadPlayerIndex(DevicePath);
                if (moved < 0 || !api.TryGetState(moved, out state))
                {
                    return HidReadStatus.NotConnected;
                }

                playerIndex = moved;
            }

            RefreshBattery();
            Compose(state, inputBuffer);
            inputBuffer[XInputReportLayout.BatteryTypeOffset] = battery.Type;
            inputBuffer[XInputReportLayout.BatteryLevelOffset] = battery.Level;
            return HidReadStatus.Success;
        }

        private void RefreshBattery()
        {
            long now = Stopwatch.GetTimestamp();
            if (now < nextBatteryTicks)
            {
                return;
            }

            nextBatteryTicks = now + Stopwatch.Frequency * BatteryIntervalMs / 1000;
            // Bluetooth の Xbox は、XInput の電池の API が動かないことがある。GATT のバッテリーサービスの値（正確な %）を先に使う。
            // まだ読めていない間は、XInput の API にもどる
            bool ok;
            XInputBattery read = default;
            if (bluetoothBattery != null && XusbConnectionClassifier.TryGetBluetoothAddressValue(DevicePath, out ulong address) &&
                bluetoothBattery.TryGetPercent(address, out int percent))
            {
                ok = true;
                read = new XInputBattery(XInputBattery.TypePercent, (byte)Math.Max(0, Math.Min(percent, 100)));
            }
            else
            {
                ok = api.TryGetBattery(playerIndex, out read);
            }

            XInputBattery next = ok ? read : new XInputBattery(XInputBattery.TypeUnknown, 0);
            if (!batteryLogged || next.Type != battery.Type || next.Level != battery.Level)
            {
                batteryLogged = true;
                AppLogger.LogDebug($"[XInput] battery (slot {playerIndex}): read={ok} type={next.Type} level={next.Level}");
            }

            battery = next;
        }

        private void WaitForNextPoll()
        {
            long now = Stopwatch.GetTimestamp();
            if (nextPollTicks != 0 && now < nextPollTicks)
            {
                int waitMs = (int)((nextPollTicks - now) * 1000 / Stopwatch.Frequency);
                if (waitMs > 0)
                {
                    cancelled.Wait(waitMs);
                }
            }

            now = Stopwatch.GetTimestamp();
            long interval = Stopwatch.Frequency * PollIntervalMs / 1000;
            // 遅れが溜まらないよう、次の時刻は今を基準にする
            nextPollTicks = now + interval;
        }

        private static void Compose(XInputSnapshot state, Span<byte> report)
        {
            report.Slice(0, XInputReportLayout.ReportLength).Clear();
            BinaryPrimitives.WriteUInt16LittleEndian(report.Slice(XInputReportLayout.ButtonsOffset), state.Buttons);
            report[XInputReportLayout.LeftTriggerOffset] = state.LeftTrigger;
            report[XInputReportLayout.RightTriggerOffset] = state.RightTrigger;
            BinaryPrimitives.WriteInt16LittleEndian(report.Slice(XInputReportLayout.LeftStickXOffset), state.LX);
            BinaryPrimitives.WriteInt16LittleEndian(report.Slice(XInputReportLayout.LeftStickYOffset), state.LY);
            BinaryPrimitives.WriteInt16LittleEndian(report.Slice(XInputReportLayout.RightStickXOffset), state.RX);
            BinaryPrimitives.WriteInt16LittleEndian(report.Slice(XInputReportLayout.RightStickYOffset), state.RY);
        }

        public bool WriteOutputReportViaInterrupt(byte[] outputBuffer, int timeout)
        {
            if (disposed || outputBuffer == null || outputBuffer.Length < OutputReportLength)
            {
                return false;
            }

            // 0〜255 を 0〜65535 に広げる（255 → 65535）
            return api.TrySetVibration(playerIndex, (ushort)(outputBuffer[1] * 257), (ushort)(outputBuffer[2] * 257));
        }

        public bool WriteOutputReportViaControl(byte[] outputBuffer) => WriteOutputReportViaInterrupt(outputBuffer, 0);

        // XInput には、特徴レポートがない
        public bool WriteFeatureReport(byte[] data) => false;

        public bool readFeatureData(byte[] inputBuffer) => false;

        public void resetSerial()
        {
        }

        public string ReadSerial(byte featureID = 18) => GenerateFakeHwSerial();

        /// <summary>
        /// 機器のインスタンス ID から、他の機種と同じ形（6 バイトの 16 進）の擬似のシリアルを作る。同じ機器なら、繋ぎ直しても同じ値になる
        /// （インスタンス ID が USB のシリアルを含む機器。含まない機器は、ポートで決まる）。
        /// </summary>
        public string GenerateFakeHwSerial()
        {
            // Bluetooth 接続は、機器の Bluetooth アドレスをそのまま使う（デバイスの識別と同じ値。登録簿が、追加と削除で同じキーを使えるように）
            string address = XusbConnectionClassifier.TryGetBluetoothAddress(DevicePath);
            if (address != null)
            {
                return address;
            }

            string source = string.IsNullOrEmpty(Info.InstanceId) ? DevicePath : Info.InstanceId;
            byte[] hash;
            using (SHA1 sha = SHA1.Create())
            {
                hash = sha.ComputeHash(Encoding.UTF8.GetBytes(source.ToUpperInvariant()));
            }

            var builder = new StringBuilder(17);
            for (int i = 0; i < 6; i++)
            {
                if (i > 0)
                {
                    builder.Append(':');
                }

                builder.Append(hash[i].ToString("X2"));
            }

            return builder.ToString();
        }

        // XInput には、OS 側の入力バッファの設定がない
        public void SetNumInputBuffers(int count)
        {
        }
    }
}
