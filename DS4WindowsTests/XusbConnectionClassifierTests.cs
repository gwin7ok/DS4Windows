using DS4Windows.Core.Model;
using DS4Windows.Input;
using DS4Windows.Input.Abstractions;
using DS4Windows.Input.Xbox;
using System.Linq;
using Xunit;

namespace DS4WindowsTests
{
    /// <summary>
    /// Xbox（XInput）の接続方式を、XUSB のインターフェースのパスから決める <see cref="XusbConnectionClassifier"/> の確認。
    /// 供給元とプロトコルの両方が同じ結果になること（一覧の状態列と、切断などの動作が食い違わないため）。
    /// </summary>
    public class XusbConnectionClassifierTests
    {
        private const string Xbox360Usb = @"\\?\usb#vid_045e&pid_028e#a02014100201#{ec87f1e3-c13b-4100-b5f7-8b84d54260cb}";
        private const string XboxOneUsb = @"\\?\hid#vid_045e&pid_02ff&ig_00#9&2a1b3c&0&0000#{ec87f1e3-c13b-4100-b5f7-8b84d54260cb}";
        private const string XboxBle = @"\\?\hid#{00001812-0000-1000-8000-00805f9b34fb}&dev&vid_045e&pid_0b22&rev_0521&3cfa0617287c&col01&ig_00#d&14fe1d93&0&0000#{ec87f1e3-c13b-4100-b5f7-8b84d54260cb}";

        [Theory]
        [InlineData(Xbox360Usb, ConnectionType.USB)]
        [InlineData(XboxOneUsb, ConnectionType.USB)]
        [InlineData(XboxBle, ConnectionType.BT)]
        [InlineData(@"\\?\BTHENUM#{00001124-0000-1000-8000-00805f9b34fb}_VID&0002045e#x", ConnectionType.BT)]
        [InlineData("", ConnectionType.USB)]
        [InlineData(null, ConnectionType.USB)]
        public void Classify_DecidesFromThePath(string path, ConnectionType expected)
        {
            Assert.Equal(expected, XusbConnectionClassifier.Classify(path));
        }

        [Fact]
        public void TheSourceAndTheProtocol_AgreeOnABluetoothDevice()
        {
            var interfaces = new FakeXusbInterfaceSource();
            interfaces.Add(XboxBle, 0x045E, 0x0B22, 0);
            InputTransportCandidate candidate = new XInputTransportSource(new FakeXInputApi(), interfaces).Enumerate().Single();

            Assert.Equal(ConnectionType.BT, candidate.Info.checkConnection(candidate.Transport));
            Assert.Equal(ConnectionType.BT, new XboxProtocol().DetermineConnectionType(candidate.Transport));
        }
    }
}
