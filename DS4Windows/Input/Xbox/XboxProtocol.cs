using System;
using DS4Windows.Core.Model;
using DS4Windows.Input.Abstractions;
using DS4Windows.Input.Hid;
using DS4Windows.Input.Parsers;

namespace DS4Windows.Input.Xbox
{
    /// <summary>
    /// Xbox 系コントローラー（XInput で読む）のプロトコル。入力は <see cref="XInputReportParser"/> が受け持つ。XInput は電池の情報を持たないため
    /// （電池の残量は別の API）、残量は 99% 固定になる。出力（振動）は後から足す。
    /// </summary>
    public sealed class XboxProtocol : IDeviceProtocol
    {
        private static readonly IInputReportParser parser = new XInputReportParser();

        public InputDeviceType DeviceType => InputDeviceType.Xbox;

        // XInput は接続方式を教えない。XUSB のインターフェースのパスから決める
        public ConnectionType DetermineConnectionType(IHidTransport hidDevice) => XusbConnectionClassifier.Classify(hidDevice?.DevicePath);

        public IInputReportParser Parser => parser;

        public bool TryReadBattery(ReadOnlySpan<byte> report, out int percent, out bool charging)
        {
            percent = 0;
            charging = false;
            return false;
        }

        public bool TryBuildOutputReport(DS4HapticState state, int batteryPercent, byte[] report) => false;
    }
}
