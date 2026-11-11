using System;
using System.Linq;
using System.Text.RegularExpressions;
using DS4Windows.Core.Model;

namespace DS4Windows.Input.Xbox
{
    /// <summary>
    /// XInput は接続方式を教えないため、XUSB のインターフェースのパスの文字列から、USB か Bluetooth かを決める純粋関数。
    /// Bluetooth の Xbox は、BLE の HID サービス（<c>{00001812-…}</c>）の下に XUSB のインターフェースを持つ（パスにその ID が入る）。
    /// 接続方式の判定の入り口（供給元の <c>checkConnection</c> と、プロトコルの <c>DetermineConnectionType</c>）は、必ずこれを通す（食い違わせない）。
    /// </summary>
    public static class XusbConnectionClassifier
    {
        private const string BleHidService = "{00001812-0000-1000-8000-00805f9b34fb}";

        // Bluetooth の Xbox は、パスに機器の Bluetooth アドレス（12 桁の 16 進）を含む
        // 例: …&dev&vid_045e&pid_0b22&rev_0521&3cfa0617287c&col01&ig_00#…
        private static readonly Regex AddressInPath = new Regex(@"&pid_[0-9a-f]{4}(?:&rev_[0-9a-f]{4})?&([0-9a-f]{12})&", RegexOptions.IgnoreCase | RegexOptions.Compiled);

        /// <summary>
        /// Bluetooth 接続のパスから、機器の Bluetooth アドレス（<c>AA:BB:CC:DD:EE:FF</c>）を取り出す。USB、または取り出せないときは null。
        /// 登録簿の識別（シリアル）とデバイスの識別（Mac）が、必ず同じ値になるよう、両方がこれを使う（食い違うと、切断のあとの再接続が拒まれる）。
        /// </summary>
        public static string TryGetBluetoothAddress(string devicePath)
        {
            if (Classify(devicePath) != ConnectionType.BT)
            {
                return null;
            }

            Match match = AddressInPath.Match(devicePath);
            if (!match.Success)
            {
                return null;
            }

            string hex = match.Groups[1].Value.ToUpperInvariant();
            return string.Join(":", Enumerable.Range(0, 6).Select(i => hex.Substring(i * 2, 2)));
        }

        /// <summary>Bluetooth のアドレスを、数値（Windows の Bluetooth の API が使う 48 ビットの値）で返す。</summary>
        public static bool TryGetBluetoothAddressValue(string devicePath, out ulong address)
        {
            string text = TryGetBluetoothAddress(devicePath);
            address = 0;
            return text != null && ulong.TryParse(text.Replace(":", string.Empty), System.Globalization.NumberStyles.HexNumber, null, out address);
        }

        public static ConnectionType Classify(string devicePath)
        {
            if (string.IsNullOrEmpty(devicePath))
            {
                return ConnectionType.USB;
            }

            bool bluetooth =
                devicePath.IndexOf(BleHidService, StringComparison.OrdinalIgnoreCase) >= 0 ||
                devicePath.IndexOf("bthenum", StringComparison.OrdinalIgnoreCase) >= 0 ||
                devicePath.IndexOf("bthle", StringComparison.OrdinalIgnoreCase) >= 0;
            return bluetooth ? ConnectionType.BT : ConnectionType.USB;
        }
    }
}
