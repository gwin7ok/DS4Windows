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

        private static RecordingProxy? _inputReport;

        private static (DeviceConnector Connector, List<string> Order) CreateConnector(RecordingConnectDevice device)
        {
            var order = device.Calls;
            var selector = RecordingProxy.Create<IConnectionProfileSelector>();
            var selectorProxy = RecordingProxy.Of(selector);
            selectorProxy.Returns["ApplyOnConnect"] = true;
            selectorProxy.OnCall = name => order.Add("selector." + name);

            var modules = RecordingProxy.Create<IInputDeviceModuleRegistry>();
            var module = RecordingProxy.Create<IInputDeviceModule>();
            RecordingProxy.Of(module).OnCall = name => order.Add("module." + name);
            RecordingProxy.Of(modules).Returns["Resolve"] = module;

            var outputs = RecordingProxy.Create<IConnectedDeviceOutputs>();
            RecordingProxy.Of(outputs).OnCall = name => order.Add("outputs." + name);
            var setup = RecordingProxy.Create<IDeviceConnectionSetup>();
            RecordingProxy.Of(setup).OnCall = name => order.Add("setup." + name);

            var xmlStoreFake = RecordingProxy.Create<IProfileXmlStore>();
            RecordingProxy.Of(xmlStoreFake).OnCall = name => order.Add("xmlStore." + name);
            var xmlStore = xmlStoreFake;
            var inputReport = RecordingProxy.Create<IInputReportProcessor>();
            _inputReport = RecordingProxy.Of(inputReport);
            var mouseFactory = RecordingProxy.Create<ITouchpadMouseFactory>();
            RecordingProxy.Of(mouseFactory).OnCall = name => order.Add("mouseFactory." + name);
            var hooks = new DeviceLifecycleHooks(RecordingProxy.Create<IDeviceDisconnectHandler>(), RecordingProxy.Create<IInputDeviceRegistry>());
            var connector = new DeviceConnector(TestRepositoryFactory.CreateProfileSettingsService(),
                xmlStore, RecordingProxy.Create<IAppSettingsService>(), hooks, selector, setup, modules,
                RecordingProxy.Create<IHidHideService>(), new DeviceSlotState(), inputReport,
                mouseFactory, outputs);
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
        public void TheProfileIsSelected_BeforeTheVirtualPadIsPlugged_AndTheOptionsAreAppliedBeforeTheHooksAreWired()
        {
            var device = NewDevice();
            var (connector, order) = CreateConnector(device);

            connector.PrepareConnected(1, device, Slot);

            Assert.True(order.IndexOf("selector.ApplyOnConnect") < order.IndexOf("outputs.PlugVirtualPad"), string.Join(" → ", order));
            Assert.True(order.IndexOf("setup.ApplyProfileOptions") >= 0
                && order.IndexOf("setup.ApplyProfileOptions") < order.IndexOf("setup.WireInitialHooks"), string.Join(" → ", order));
        }

        [Fact]
        public void TheDeviceModule_IsAskedForTheExtraButtons_AndHooksTheSettings()
        {
            var device = NewDevice();
            var (connector, order) = CreateConnector(device);

            connector.BeginPrepare(device, showlog: false);
            connector.PrepareConnected(1, device, Slot);

            Assert.Contains("module.GetKnownExtraButtons", order);
            Assert.Contains("module.HookSettings", order);
        }

        [Fact]
        public void TheConnection_KeepsTheWholeOrder_FromTheSettingsToTheReadingStart()
        {
            var device = NewDevice();
            var (connector, order) = CreateConnector(device);

            connector.PrepareConnected(1, device, Slot);

            string[] expected =
            {
                "xmlStore.LoadControllerConfigsForDevice", "device.LoadStoreSettings", "device.CheckControllerNumDeviceSettings",
                "outputs.NotifyPlugged", "mouseFactory.Create", "device.PrimeBatteryReading", "selector.ApplyOnConnect",
                "outputs.PlugVirtualPad", "setup.ApplyProfileOptions", "setup.WireInitialHooks", "device.Report+=",
                "outputs.PrepareMotion", "device.StartUpdate",
            };
            Assert.Equal(expected, order.Where(expected.Contains).ToArray());
        }

        [Fact]
        public void TheReportSubscription_CallsTheProcessorOnReport_WithTheSlotIndex()
        {
            var device = NewDevice();
            var (connector, _) = CreateConnector(device);
            connector.PrepareConnected(1, device, Slot);
            var args = System.EventArgs.Empty;

            device.ReportHandlers.Single()(device, args);

            object?[] call = _inputReport!.ArgsOf("OnReport").Single();
            Assert.Same(device, call[0]);
            Assert.Same(args, call[1]);
            Assert.Equal(Slot, call[2]);
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
