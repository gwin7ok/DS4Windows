using System.Linq;
using DS4Windows.Core.Model;
using DS4Windows.Input;
using DS4Windows.Input.Abstractions;
using DS4Windows.Input.Xbox;
using Xunit;

namespace DS4WindowsTests
{
    /// <summary>
    /// Bluetooth の Xbox の切断。切断の手順は DS4 などと共通の基底（<c>InputDeviceCore.DisconnectBT</c>）を使い、Xbox は、パスから Bluetooth のアドレスだけを教える。
    /// USB の Xbox は、これまでどおり切断できない（false）。実際の Windows の切断の API は、ここでは呼ばない（OS の状態に触れない）。
    /// </summary>
    public class XboxBluetoothDisconnectTests
    {
        private const string BlePath = @"\\?\hid#{00001812-0000-1000-8000-00805f9b34fb}&dev&vid_045e&pid_0b22&rev_0521&3cfa0617287c&col01&ig_00#d&14fe1d93&0&0000#{ec87f1e3-c13b-4100-b5f7-8b84d54260cb}";
        private const string UsbPath = @"\\?\usb#vid_045e&pid_028e#a02014100201#{ec87f1e3-c13b-4100-b5f7-8b84d54260cb}";

        private static InputDeviceCore NewDevice(string path, int pid)
        {
            var interfaces = new FakeXusbInterfaceSource();
            interfaces.Add(path, 0x045E, pid, 0);
            InputTransportCandidate candidate = new XInputTransportSource(new FakeXInputApi(), interfaces).Enumerate().Single();
            InputDeviceCore device = new XboxDeviceCartridge().Create(InputDeviceType.Xbox, candidate.Transport, candidate.Info.name, candidate.Info.featureSet);
            device.PostInit();
            return device;
        }

        [Fact]
        public void TheProtocol_ReadsTheBluetoothAddressFromTheBlePath()
        {
            var interfaces = new FakeXusbInterfaceSource();
            interfaces.Add(BlePath, 0x045E, 0x0B22, 0);
            InputTransportCandidate candidate = new XInputTransportSource(new FakeXInputApi(), interfaces).Enumerate().Single();

            Assert.Equal("3C:FA:06:17:28:7C", new XboxProtocol().TryGetBluetoothAddress(candidate.Transport));
        }

        [Fact]
        public void TheProtocol_HasNoBluetoothAddress_ForAUsbPad()
        {
            var interfaces = new FakeXusbInterfaceSource();
            interfaces.Add(UsbPath, 0x045E, 0x028E, 0);
            InputTransportCandidate candidate = new XInputTransportSource(new FakeXInputApi(), interfaces).Enumerate().Single();

            Assert.Null(new XboxProtocol().TryGetBluetoothAddress(candidate.Transport));
        }

        [Fact]
        public void ABluetoothDevice_UsesTheAddressAsItsIdentity()
        {
            InputDeviceCore device = NewDevice(BlePath, 0x0B22);

            Assert.Equal(ConnectionType.BT, device.getConnectionType());
            Assert.Equal("3C:FA:06:17:28:7C", device.getMacAddress());
        }

        [Fact]
        public void AUsbDevice_CannotBeDisconnected_AsBefore()
        {
            InputDeviceCore device = NewDevice(UsbPath, 0x028E);

            Assert.False(device.DisconnectBT());
            Assert.False(device.DisconnectWireless());
            Assert.False(device.DisconnectDongle());
        }
    }
}
