#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using DS4Windows.Core.Abstractions;
using DS4Windows.Core.Model;
using DS4Windows.Core.Platform;
using DS4Windows.Input;
using DS4Windows.Processing.Abstractions;
using DS4Windows.Processing.Services.Connection;
using DS4Windows.Processing.Services.Devices;
using Xunit;

namespace DS4WindowsTests
{
    /// <summary>
    /// 開始・停止の安全弁の順序（<see cref="ServiceLifecycle"/>）を、呼び出しの記録で確かめる。
    /// 開始: 仮想出力の開始 → 排他モード → HidHide → ネットワーク出力 → 恒久スロット → 列挙 → 接続の組み立て → UDP の待ち受け → 通知。
    /// 停止: 事前通知 → 機器の停止と仮想パッドの取り外し → 登録簿の停止 → ネットワーク出力の停止 → 仮想出力の停止（ドライバーの破棄）→ 通知。
    /// </summary>
    [Collection(SharedHostStateCollection.Name)]
    public class ServiceLifecycleOrderTests
    {
        private sealed class SyncDispatcher : IEventThreadDispatcher
        {
            public void Queue(Action action) => action();
            public void Invoke(Action action) => action();
            public void Shutdown() { }
        }

        private sealed class Rig
        {
            public readonly List<string> Order = new List<string>();
            public readonly DeviceSlotState Slots = new DeviceSlotState();
            public readonly RecordingConnectDevice Device = RecordingConnectDevice.Create();
            public readonly ServiceLifecycle Lifecycle;

            private T Fake<T>(string prefix, Action<RecordingProxy>? configure = null) where T : class
            {
                var fake = RecordingProxy.Create<T>();
                var proxy = RecordingProxy.Of(fake);
                proxy.OnCall = name => Order.Add(prefix + "." + name);
                configure?.Invoke(proxy);
                return fake;
            }

            public Rig()
            {
                Device.Calls = Order;
                Device.SetIdentity("AA:BB:CC:DD:EE:01", InputDeviceType.DS4);
                var outputs = Fake<IOutputServicesLifecycle>("outputs", p => p.Returns["StartVirtualOutputs"] = true);
                var registry = Fake<IInputDeviceRegistry>("registry", p => p.Returns["GetControllers"] = new List<InputDeviceCore> { Device });
                var virtualPad = Fake<IVirtualPadService>("virtualPad", p => p.Returns["HasOutputDevice"] = true);

                Lifecycle = new ServiceLifecycle(new LightbarOverrides(), Fake<ILightbarService>("lightbar"),
                    Fake<IAppSettingsService>("appSettings"), new EnvironmentService(), TestRepositoryFactory.CreateProfileSettingsService(),
                    registry, virtualPad, Fake<ILatencyMonitor>("latency"), Fake<IDeviceConnector>("connector"), Slots,
                    new SyncDispatcher(), outputs);
                Lifecycle.ServiceStarted += (s, e) => Order.Add("event.ServiceStarted");
                Lifecycle.ServiceStopped += (s, e) => Order.Add("event.ServiceStopped");
                Lifecycle.PreServiceStop += (s, e) => Order.Add("event.PreServiceStop");
                Lifecycle.RunningChanged += (s, e) => Order.Add("event.RunningChanged");
            }

            public void AssertOrder(params string[] steps)
            {
                int last = -1;
                foreach (string step in steps)
                {
                    int index = Order.IndexOf(step);
                    Assert.True(index > last, $"'{step}' の順序が違う: " + string.Join(" → ", Order));
                    last = index;
                }
            }
        }

        [Fact]
        public void Start_KeepsTheSafetyOrder()
        {
            var rig = new Rig();

            rig.Lifecycle.Start();

            rig.AssertOrder("outputs.StartVirtualOutputs", "outputs.UpdateHidHide", "outputs.StartNetworkOutputs",
                "virtualPad.AssignInitialDevices", "registry.FindControllers", "connector.BeginPrepare", "connector.PrepareConnected",
                "outputs.StartUdpListening", "event.ServiceStarted", "event.RunningChanged");
            Assert.True(rig.Lifecycle.Running);
            Assert.False(rig.Lifecycle.InServiceTask);
            Assert.Same(rig.Device, rig.Slots.Controllers[0]);
        }

        [Fact]
        public void Start_DoesNotRunTheRestWhenTheDriverCannotStart()
        {
            var rig = new Rig();
            RecordingProxy.Of(Outputs(rig)).Returns["StartVirtualOutputs"] = false;

            rig.Lifecycle.Start();

            Assert.DoesNotContain("registry.FindControllers", rig.Order);
            Assert.False(rig.Lifecycle.Running);
            Assert.Contains("event.ServiceStarted", rig.Order);   // 通知は従来どおり出す
        }

        private static object Outputs(Rig rig) => typeof(ServiceLifecycle).GetField("_outputs", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!.GetValue(rig.Lifecycle)!;

        [Fact]
        public void Stop_KeepsTheSafetyOrder_IncludingTheDriverDestructionOrder()
        {
            var rig = new Rig();
            rig.Lifecycle.Start();
            rig.Order.Clear();

            rig.Lifecycle.Stop();

            rig.AssertOrder("event.PreServiceStop", "device.StopUpdate", "registry.RemoveDevice", "virtualPad.UnplugOutDev",
                "outputs.ResetDeviceState", "latency.Reset", "registry.StopControllers", "outputs.StopNetworkOutputs",
                "outputs.StopVirtualOutputs", "event.ServiceStopped", "event.RunningChanged");
            Assert.False(rig.Lifecycle.Running);
            Assert.Equal(0, rig.Lifecycle.ActiveControllers);
            Assert.Null(rig.Slots.Controllers[0]);
        }

        [Fact]
        public void Start_AppliesTheSavedProcessPriority_ThroughTheOptionsTable()
        {
            // 保存済みの設定は、ProcessPriorityOptions.Classes の添字。1 = AboveNormal（安全な値で確かめ、終わりに元へ戻す）
            using var process = System.Diagnostics.Process.GetCurrentProcess();
            var original = process.PriorityClass;
            try
            {
                var rig = new Rig();
                RecordingProxy.Of(AppSettingsOf(rig)).Returns["get_ProcessPriority"] = 1;

                rig.Lifecycle.Start();

                process.Refresh();
                Assert.Equal(DS4Windows.Core.Platform.ProcessPriorityOptions.Classes[1], process.PriorityClass);
                Assert.Equal(System.Diagnostics.ProcessPriorityClass.AboveNormal, process.PriorityClass);
            }
            finally
            {
                process.PriorityClass = original;
            }
        }

        private static object AppSettingsOf(Rig rig) => typeof(ServiceLifecycle).GetField("_appSettings", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!.GetValue(rig.Lifecycle)!;

        [Fact]
        public void Stop_WhenNotRunning_OnlyNotifies()
        {
            var rig = new Rig();

            rig.Lifecycle.Stop();

            Assert.Equal(new[] { "appSettings.set_RunHotPlug", "event.ServiceStopped", "event.RunningChanged" }, rig.Order);   // ホットプラグの停止と通知だけ
        }
    }
}
