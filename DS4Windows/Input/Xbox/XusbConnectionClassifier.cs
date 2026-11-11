using System;
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
