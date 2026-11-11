using System;
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
        public void ABluetoothDevice_CanBeFoundAgain_AfterItWasRemoved()
        {
            // 登録簿は、追加のときにシリアル、削除のときに Mac をキーにする。2 つが食い違うと、切断のあとの再接続が「すでにある」として拒まれる
            var interfaces = new FakeXusbInterfaceSource();
            interfaces.Add(BlePath, 0x045E, 0x0B22, 0);
            var registry = new InputDeviceRegistry(null, new IInputDeviceCartridge[] { new XboxDeviceCartridge() },
                new IInputTransportSource[] { new XInputTransportSource(new FakeXInputApi(), interfaces) });
            registry.PreparePendingDevice = (device, info) => true;

            registry.findControllers();
            InputDeviceCore first = Assert.Single(registry.GetControllers());
            registry.RemoveDevice(first);
            Assert.Empty(registry.GetControllers());

            registry.findControllers();

            Assert.Single(registry.GetControllers());
            registry.stopControllers();
        }

        [Fact]
        public void TheTransportSerial_EqualsTheDeviceIdentity_ForBluetoothAndUsb()
        {
            foreach ((string path, int pid) in new[] { (BlePath, 0x0B22), (UsbPath, 0x028E) })
            {
                var interfaces = new FakeXusbInterfaceSource();
                interfaces.Add(path, 0x045E, pid, 0);
                InputTransportCandidate candidate = new XInputTransportSource(new FakeXInputApi(), interfaces).Enumerate().Single();
                InputDeviceCore device = new XboxDeviceCartridge().Create(InputDeviceType.Xbox, candidate.Transport, candidate.Info.name, candidate.Info.featureSet);
                device.PostInit();

                Assert.Equal(candidate.Transport.ReadSerial(), device.getMacAddress());
            }
        }

        private static InputDeviceCore NewDeviceWith(string path, int pid, Func<string, bool> disconnectBle)
        {
            var interfaces = new FakeXusbInterfaceSource();
            interfaces.Add(path, 0x045E, pid, 0);
            InputTransportCandidate candidate = new XInputTransportSource(new FakeXInputApi(), interfaces).Enumerate().Single();
            var device = new DS4Windows.Input.Devices.ProtocolDevice(new XboxProtocol(disconnectBle), candidate.Transport, candidate.Info.name, candidate.Info.featureSet);
            device.PostInit();
            return device;
        }

        [Fact]
        public void ABleDevice_IsDisconnectedByTheDeviceNodeProcedure_AndReportsItsRemoval()
        {
            string called = null;
            InputDeviceCore device = NewDeviceWith(BlePath, 0x0B22, p => { called = p; return true; });
            int removals = 0;
            device.Removal += (s, e) => removals++;

            Assert.True(device.DisconnectBT(callRemoval: true));

            Assert.Equal(BlePath, called);
            Assert.Equal(1, removals);
            Assert.True(device.IsDisconnecting);
        }

        [Fact]
        public void ABleDevice_StaysConnected_WhenTheDeviceNodeProcedureFails()
        {
            InputDeviceCore device = NewDeviceWith(BlePath, 0x0B22, p => false);
            int removals = 0;
            device.Removal += (s, e) => removals++;

            Assert.False(device.DisconnectBT(callRemoval: true));
            Assert.False(device.DisconnectWireless(callRemoval: true));

            Assert.Equal(0, removals);
            Assert.False(device.IsDisconnecting);
        }

        [Fact]
        public void AUsbDevice_NeverUsesTheDeviceNodeProcedure()
        {
            bool called = false;
            InputDeviceCore device = NewDeviceWith(UsbPath, 0x028E, p => { called = true; return true; });

            Assert.False(device.DisconnectBT());
            Assert.False(called);
        }

        [Fact]
        public void TheDeviceNode_IsFoundByClimbingFromTheHidChild()
        {
            var parents = new System.Collections.Generic.Dictionary<string, string>
            {
                [@"HID\{00001812}&DEV&COL01&IG_00\D&1&0&0000"] = @"BTHLEDEVICE\{00001812}_DEV_VID&02045E_PID&0B22\C&1&0&0016",
                [@"BTHLEDEVICE\{00001812}_DEV_VID&02045E_PID&0B22\C&1&0&0016"] = @"BTHLE\DEV_3CFA0617287C\B&1&0&3CFA0617287C",
            };
            string ParentOf(string id) => parents.TryGetValue(id, out string p) ? p : string.Empty;

            Assert.Equal(@"BTHLE\DEV_3CFA0617287C\B&1&0&3CFA0617287C",
                DS4Windows.Core.Platform.DevicePathProbe.FindBluetoothLeDeviceNode(@"HID\{00001812}&DEV&COL01&IG_00\D&1&0&0000", ParentOf));
            Assert.Equal(string.Empty, DS4Windows.Core.Platform.DevicePathProbe.FindBluetoothLeDeviceNode(@"USB\VID_045E&PID_028E\A1", ParentOf));
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
