using System;
using System.Linq;
using System.Text.RegularExpressions;
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

        public InputDeviceType DeviceType => InputDeviceType.Xbox;

        // XInput は接続方式を教えない。XUSB のインターフェースのパスから決める
        public ConnectionType DetermineConnectionType(IHidTransport hidDevice) => XusbConnectionClassifier.Classify(hidDevice?.DevicePath);

        // Bluetooth の Xbox は、XUSB のインターフェースのパスに、機器の Bluetooth アドレス（12 桁の 16 進）を含む
        // 例: …&dev&vid_045e&pid_0b22&rev_0521&3cfa0617287c&col01&ig_00#…
        public string TryGetBluetoothAddress(IHidTransport hidDevice)
        {
            if (XusbConnectionClassifier.Classify(hidDevice?.DevicePath) != ConnectionType.BT)
            {
                return null;
            }

            Match match = AddressInPath.Match(hidDevice.DevicePath);
            if (!match.Success)
            {
                return null;
            }

            string hex = match.Groups[1].Value.ToUpperInvariant();
            return string.Join(":", Enumerable.Range(0, 6).Select(i => hex.Substring(i * 2, 2)));
        }

        private static readonly Regex AddressInPath = new Regex(@"&pid_[0-9a-f]{4}(?:&rev_[0-9a-f]{4})?&([0-9a-f]{12})&", RegexOptions.IgnoreCase | RegexOptions.Compiled);

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
            if (type != XInputBattery.TypeAlkaline && type != XInputBattery.TypeNiMh)
            {
                return false;
            }

            percent = PercentOf(report[XInputReportLayout.BatteryLevelOffset]);
            return true;
        }

        // XInput の残量は 4 段階（空・低・中・満）。0／33／66／100% にする
        private static int PercentOf(byte level)
        {
            switch (level)
            {
                case 0: return 0;
                case 1: return 33;
                case 2: return 66;
                default: return 100;
            }
        }

        public bool TryBuildOutputReport(DS4HapticState state, int batteryPercent, byte[] report) => false;
    }
}
