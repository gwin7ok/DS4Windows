#nullable enable
using System.Collections.Generic;
using System.Linq;
using DS4Windows.Core.Abstractions;
using DS4Windows.Core.Model;
using DS4Windows.Processing.Abstractions;
using DS4Windows.Processing.Services.Connection;
using DS4Windows.Processing.Services.Devices;
using Xunit;

namespace DS4WindowsTests
{
    /// <summary>
    /// 接続の組み立て（<see cref="DeviceConnector.PrepareConnected"/>）と、機器のイベントの購読（<see cref="DeviceLifecycleHooks"/>）の順序を、
    /// 呼び出しの記録で確かめる。接続直後のログ・通知の電池残量が 0% にならないよう、電池の取り込み → プロファイルの適用 → 読み取りの開始の順。
    /// 切断・同期・シリアル変更は、自分（切断の処理）→ 機器の登録簿の順に受ける。
    /// </summary>
    [Collection(SharedHostStateCollection.Name)]
    public class DeviceConnectionOrderTests
    {
        private const int Slot = 4;
        private const string Mac = "AA:BB:CC:DD:EE:04";

        private static RecordingConnectDevice NewDevice()
        {
            var device = RecordingConnectDevice.Create();
            device.SetIdentity(Mac, InputDeviceType.DS4);
            return device;
        }

        private static (DeviceConnector Connector, List<string> Order) CreateConnector(RecordingConnectDevice device)
        {
            var order = device.Calls;
            var selector = RecordingProxy.Create<IConnectionProfileSelector>();
            var selectorProxy = RecordingProxy.Of(selector);
            selectorProxy.Returns["ApplyOnConnect"] = true;
            selectorProxy.OnCall = name => order.Add("selector." + name);

            var modules = RecordingProxy.Create<IInputDeviceModuleRegistry>();
            RecordingProxy.Of(modules).Returns["Resolve"] = RecordingProxy.Create<IInputDeviceModule>();

            var outputs = RecordingProxy.Create<IConnectedDeviceOutputs>();
            RecordingProxy.Of(outputs).OnCall = name => order.Add("outputs." + name);
            var setup = RecordingProxy.Create<IDeviceConnectionSetup>();
            RecordingProxy.Of(setup).OnCall = name => order.Add("setup." + name);

            var hooks = new DeviceLifecycleHooks(RecordingProxy.Create<IDeviceDisconnectHandler>(), RecordingProxy.Create<IInputDeviceRegistry>());
            var connector = new DeviceConnector(TestRepositoryFactory.CreateProfileSettingsService(),
                RecordingProxy.Create<IProfileXmlStore>(), RecordingProxy.Create<IAppSettingsService>(), hooks, selector, setup, modules,
                RecordingProxy.Create<IHidHideService>(), new DeviceSlotState(), RecordingProxy.Create<IInputReportProcessor>(),
                RecordingProxy.Create<ITouchpadMouseFactory>(), outputs);
            return (connector, order);
        }

        [Fact]
        public void TheBatteryIsPrimed_BeforeTheProfileIsApplied_AndBeforeTheReadingStarts()
        {
            var device = NewDevice();
            var (connector, order) = CreateConnector(device);

            connector.PrepareConnected(1, device, Slot);

            int prime = order.IndexOf("device.PrimeBatteryReading");
            int apply = order.IndexOf("selector.ApplyOnConnect");
            int start = order.IndexOf("device.StartUpdate");
            Assert.True(prime >= 0 && apply > prime && start > apply, string.Join(" → ", order));
        }

        [Fact]
        public void TheSettingsAreLoaded_BeforeTheBatteryIsPrimed_AndTheReadingStartsLast()
        {
            var device = NewDevice();
            var (connector, order) = CreateConnector(device);

            connector.PrepareConnected(1, device, Slot);

            Assert.True(order.IndexOf("device.LoadStoreSettings") < order.IndexOf("device.PrimeBatteryReading"), string.Join(" → ", order));
            Assert.Equal("device.StartUpdate", order.Last());   // 読み取りの開始は、組み立ての最後
            Assert.Contains("outputs.PlugVirtualPad", order);
        }

        [Fact]
        public void TheLifecycleHooks_ReceiveTheEventsInTheOriginalOrder_HandlerFirstThenTheRegistry()
        {
            var order = new List<string>();
            var handler = RecordingProxy.Create<IDeviceDisconnectHandler>();
            RecordingProxy.Of(handler).OnCall = name => order.Add("handler." + name);
            var registry = RecordingProxy.Create<IInputDeviceRegistry>();
            RecordingProxy.Of(registry).OnCall = name => order.Add("registry." + name);
            var device = NewDevice();
            new DeviceLifecycleHooks(handler, registry).Attach(device);

            device.RaiseRemoval();
            device.RaiseSync();
            device.RaiseSerial();

            Assert.Equal(new[]
            {
                "handler.OnRemoval", "registry.OnRemoval",
                "handler.OnSyncChange", "registry.UpdateSerial",
                "handler.OnSerialChange",
            }, order);
        }
    }
}
