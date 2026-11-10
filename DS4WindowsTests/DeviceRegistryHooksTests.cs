using System;
using DS4Windows.Core.Abstractions;
using DS4Windows.Input;
using DS4Windows.Processing.Abstractions;
using DS4Windows.Processing.Services;
using Xunit;

namespace DS4WindowsTests
{
    /// <summary>
    /// コントローラーの登録簿（<see cref="DS4Devices"/>。インスタンス）と、そこへ結ぶフック（<see cref="DeviceRegistryHooks"/>）、
    /// 適用プロファイルの記憶（<see cref="AppliedProfileRecorder"/>）、仮想パッドの失敗の処理（<see cref="BackendFailureHandler"/>）の、
    /// 登録・独立性・引数の検査。記憶の判定の順序は <c>DisconnectClearsTempProfileTests</c> が固定する。
    /// </summary>
    public class DeviceRegistryHooksTests
    {
        [Fact]
        public void TwoRegistries_DoNotShareState()
        {
            var first = new DS4Devices(null);
            var second = new DS4Devices(null);

            first.isExclusiveMode = true;
            first.PrepareDS4Init = device => { };

            Assert.False(second.isExclusiveMode);
            Assert.Null(second.PrepareDS4Init);
            Assert.Empty(first.getDS4Controllers());
            Assert.Empty(second.getDS4Controllers());
        }

        [Fact]
        public void Registry_ComesFromTheContainerAsOneInstance()
        {
            Assert.Same(TestHost.GetService<DS4Devices>(), TestHost.GetService<DS4Devices>());
        }

        [Fact]
        public void Attach_ConnectsTheCallbacksToTheRegistry()
        {
            var devices = new DS4Devices(null);
            var registry = new Ds4DeviceRegistryAdapter(devices);
            var hooks = new DeviceRegistryHooks(registry, TestHost.GetService<IInputDeviceModuleRegistry>(),
                TestHost.GetService<IAppSettingsService>(), TestHost.GetService<IElevatedProcessLauncher>(), TestHost.GetService<IPathService>());

            Assert.Null(devices.PrepareDS4Init);
            hooks.Attach();

            Assert.NotNull(devices.PrepareDS4Init);
            Assert.NotNull(devices.PostDS4Init);
            Assert.NotNull(devices.PreparePendingDevice);
        }

        [Fact]
        public void TheCollaborators_AreRegisteredAsSingletons()
        {
            Assert.IsType<DeviceRegistryHooks>(TestHost.GetService<IDeviceRegistryHooks>());
            Assert.Same(TestHost.GetService<IDeviceRegistryHooks>(), TestHost.GetService<IDeviceRegistryHooks>());
            Assert.IsType<AppliedProfileRecorder>(TestHost.GetService<IAppliedProfileRecorder>());
            Assert.IsType<BackendFailureHandler>(TestHost.GetService<IBackendFailureHandler>());
        }

        [Fact]
        public void Constructors_RejectNull()
        {
            Assert.Throws<ArgumentNullException>(() => new Ds4DeviceRegistryAdapter((DS4Devices)null));
            var registry = TestHost.GetService<IDs4DeviceRegistry>();
            var modules = TestHost.GetService<IInputDeviceModuleRegistry>();
            var app = TestHost.GetService<IAppSettingsService>();
            var launcher = TestHost.GetService<IElevatedProcessLauncher>();
            var path = TestHost.GetService<IPathService>();
            Assert.Throws<ArgumentNullException>(() => new DeviceRegistryHooks(null, modules, app, launcher, path));
            Assert.Throws<ArgumentNullException>(() => new DeviceRegistryHooks(registry, null, app, launcher, path));
            Assert.Throws<ArgumentNullException>(() => new DeviceRegistryHooks(registry, modules, null, launcher, path));
            Assert.Throws<ArgumentNullException>(() => new DeviceRegistryHooks(registry, modules, app, null, path));
            Assert.Throws<ArgumentNullException>(() => new DeviceRegistryHooks(registry, modules, app, launcher, null));

            var repo = TestHost.GetService<IProfileRepository>();
            var settings = TestHost.GetService<IProfileSettingsService>();
            var state = TestHost.GetService<IDeviceStateService>();
            var slots = TestHost.GetService<IDeviceSlotState>();
            Assert.Throws<ArgumentNullException>(() => new AppliedProfileRecorder(null, settings, state, slots));
            Assert.Throws<ArgumentNullException>(() => new AppliedProfileRecorder(repo, null, state, slots));
            Assert.Throws<ArgumentNullException>(() => new AppliedProfileRecorder(repo, settings, null, slots));
            Assert.Throws<ArgumentNullException>(() => new AppliedProfileRecorder(repo, settings, state, null));

            var pad = TestHost.GetService<IVirtualPadService>();
            var thread = TestHost.GetService<IEventThreadDispatcher>();
            var lifecycle = TestHost.GetService<IServiceLifecycle>();
            Assert.Throws<ArgumentNullException>(() => new BackendFailureHandler(null, thread, lifecycle));
            Assert.Throws<ArgumentNullException>(() => new BackendFailureHandler(pad, null, lifecycle));
            Assert.Throws<ArgumentNullException>(() => new BackendFailureHandler(pad, thread, null));
        }
    }
}
