using System;
using DS4Windows.Core.Model;
using DS4Windows.Input.Abstractions;
using DS4Windows.Input.Hid;
using DS4Windows.Input.Parsers;

namespace DS4Windows.Input.Xbox
{
    /// <summary>
    /// Xbox 系コントローラー（XInput で読む）のプロトコル。入力は <see cref="XInputReportParser"/> が受け持つ。電池は、状態とは別の API（<c>XInputGetBatteryInformation</c>）で読み、
    /// 通信路が合成レポートの末尾へ入れる。有線と、電池を読めない場合は、残量が 99% 固定になる。出力（振動）は後から足す。
    /// </summary>
    public sealed class XboxProtocol : IDeviceProtocol
    {
        private static readonly IInputReportParser parser = new XInputReportParser();

        private readonly Func<string, bool> disconnectBle;

        public XboxProtocol()
            : this(BleDeviceNodeCycler.Disconnect)
        {
        }

        /// <param name="disconnectBle">Bluetooth LE の機器を切断する手順（XUSB のインターフェースのパスを受ける）。テストで差し替える。</param>
        public XboxProtocol(Func<string, bool> disconnectBle)
        {
            this.disconnectBle = disconnectBle ?? throw new ArgumentNullException(nameof(disconnectBle));
        }

        public InputDeviceType DeviceType => InputDeviceType.Xbox;

        // BLE の Xbox は、従来の Bluetooth 向けの切断が効かない。まず XInput の電源オフ（XInputPowerOffController）を試し、
        // だめなら機器のノードを無効→有効にして切る。BLE でなければ、共通の手順（null）
        public bool? TryDisconnectBluetooth(IHidTransport hidDevice)
        {
            if (!XusbConnectionClassifier.IsBluetoothLowEnergy(hidDevice?.DevicePath))
            {
                return null;
            }

            if (hidDevice is XInputTransport transport && transport.TryPowerOff())
            {
                return true;
            }

            return disconnectBle(hidDevice.DevicePath);
        }

        // XInput は接続方式を教えない。XUSB のインターフェースのパスから決める
        public ConnectionType DetermineConnectionType(IHidTransport hidDevice) => XusbConnectionClassifier.Classify(hidDevice?.DevicePath);

        // Bluetooth のアドレスは、パスから（登録簿のシリアルと同じ値。XusbConnectionClassifier が 1 か所で決める）
        public string TryGetBluetoothAddress(IHidTransport hidDevice) => XusbConnectionClassifier.TryGetBluetoothAddress(hidDevice?.DevicePath);

        public IInputReportParser Parser => parser;

        // 電池式（アルカリ・ニッケル水素）のときだけ値を返す。有線・不明・読めないときは false（99% 固定。低電池の警告が出ない）
        public bool TryReadBattery(ReadOnlySpan<byte> report, out int percent, out bool charging)
        {
            percent = 0;
            charging = false;   // XInput は、充電中かを教えない
            if (report.Length < XInputReportLayout.ReportLength)
            {
                return false;
            }

            byte type = report[XInputReportLayout.BatteryTypeOffset];
            if (type == XInputBattery.TypePercent)
            {
                percent = Math.Min((int)report[XInputReportLayout.BatteryLevelOffset], 100);
                return true;
            }

            if (type !=XInputBattery.TypeAlkaline && type != XInputBattery.TypeNiMh)
            {
                return false;
            }

            // XInput の残量は 4 段階（空・低・中・満＝0〜3）。共通の計算で 0／33／66／100% になる
            percent = BatteryScale.ToPercent(report[XInputReportLayout.BatteryLevelOffset], XInputBattery.MaxLevel);
            return true;
        }

        public bool TryBuildOutputReport(DS4HapticState state, int batteryPercent, byte[] report) => false;
    }
}
