using System.Linq;
using System.Threading;
using DS4Windows.Core.Model;
using DS4Windows.Input;
using DS4Windows.Input.Abstractions;
using DS4Windows.Input.Xbox;
using Xunit;

namespace DS4WindowsTests
{
    /// <summary>
    /// Xbox 系コントローラー（XInput）が、共通の読み取りのループ（<c>ProtocolDevice</c>）と登録簿（<c>InputDeviceRegistry</c>）の流れに乗ることの確認。
    /// HID の機種と同じカートリッジの仕組みで足してあり、登録簿に HID 以外の通信路の供給元を足しただけで、機器が見つかる。
    /// </summary>
    public class XboxDeviceFlowTests
    {
        private const string Path = @"\\?\usb#vid_045e&pid_028e#flowtest1#{ec87f1e3-c13b-4100-b5f7-8b84d54260cb}";

        private static (FakeXInputApi Api, FakeXusbInterfaceSource Interfaces) Fakes()
        {
            var api = new FakeXInputApi();
            var interfaces = new FakeXusbInterfaceSource();
            interfaces.Add(Path, 0x045E, 0x028E, 0);
            return (api, interfaces);
        }

        [Fact]
        public void TheCartridge_HasNoHidTable_AndFollowsTheFamilyContract()
        {
            IInputDeviceCartridge cartridge = new XboxDeviceCartridge();

            Assert.Empty(cartridge.KnownDevices);   // HID の列挙に、Xbox の HID の子を載せない
            Assert.True(cartridge.Handles(InputDeviceType.Xbox));
            Assert.False(cartridge.Handles(InputDeviceType.DS4));
            Assert.Equal(InputDeviceFamily.Xbox, cartridge.Family);
        }

        [Fact]
        public void TheDevice_RunsTheCommonLoop_ReportsButtons_AndRemovesItselfOnADisconnect()
        {
            var (api, interfaces) = Fakes();
            api.Set(0, new XInputSnapshot(1, XInputReportLayout.A, 255, 0, 0, 0, 0, 0));
            var candidate = new XInputTransportSource(api, interfaces).Enumerate().Single();

            InputDeviceCore device = new XboxDeviceCartridge().Create(InputDeviceType.Xbox, candidate.Transport, candidate.Info.name, candidate.Info.featureSet);
            device.PostInit();

            using var reported = new ManualResetEventSlim();
            bool cross = false;
            byte l2 = 0;
            device.Report += (sender, args) =>
            {
                ControllerState state = sender.getCurrentStateRef();
                cross = state.Cross;
                l2 = state.L2;
                reported.Set();
            };
            using var removed = new ManualResetEventSlim();
            device.Removal += (sender, args) => removed.Set();

            device.StartUpdate();
            Assert.True(reported.Wait(5000), "XInput の状態が、Report として届くはず");
            api.Disconnect(0);
            interfaces.MoveTo(Path, -1);
            Assert.True(removed.Wait(5000), "読めなくなったら、Removal が来るはず");
            device.StopUpdate();

            Assert.True(cross);   // A → Cross
            Assert.Equal(255, l2);
            Assert.Equal(InputDeviceType.Xbox, device.DeviceType);
            Assert.Equal(ConnectionType.USB, device.getConnectionType());
        }

        [Fact]
        public void TheRegistry_FindsXboxDevices_ThroughTheTransportSource_WhenTheModuleIsEnabled()
        {
            var (api, interfaces) = Fakes();
            var registry = new InputDeviceRegistry(null, new IInputDeviceCartridge[] { new XboxDeviceCartridge() },
                new IInputTransportSource[] { new XInputTransportSource(api, interfaces) });
            VidPidInfo seen = null;
            registry.PreparePendingDevice = (device, info) =>
            {
                seen = info;
                return true;
            };

            registry.findControllers();

            InputDeviceCore found = Assert.Single(registry.GetControllers());
            Assert.Equal(InputDeviceType.Xbox, found.DeviceType);
            Assert.Equal(InputDeviceType.Xbox, seen.inputDevType);
            registry.stopControllers();
        }

        [Fact]
        public void TheRegistry_FindsNothing_WhenTheModuleIsDisabled()
        {
            var (api, interfaces) = Fakes();
            var registry = new InputDeviceRegistry(null, new IInputDeviceCartridge[] { new XboxDeviceCartridge() },
                new IInputTransportSource[] { new XInputTransportSource(api, interfaces) });
            registry.PreparePendingDevice = (device, info) => false;

            registry.findControllers();

            Assert.Empty(registry.GetControllers());
        }

        [Fact]
        public void TheRegistry_DoesNotCreateTheSameDeviceTwice_OnTheSecondScan()
        {
            var (api, interfaces) = Fakes();
            var registry = new InputDeviceRegistry(null, new IInputDeviceCartridge[] { new XboxDeviceCartridge() },
                new IInputTransportSource[] { new XInputTransportSource(api, interfaces) });
            registry.PreparePendingDevice = (device, info) => true;

            registry.findControllers();
            registry.findControllers();

            Assert.Single(registry.GetControllers());
            registry.stopControllers();
        }
    }
}
