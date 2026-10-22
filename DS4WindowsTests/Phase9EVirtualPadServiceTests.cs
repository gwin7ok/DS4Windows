using System;
using System.Collections.Generic;
using DS4Windows;
using DS4Windows.DI;
using DS4Windows.Services;
using Xunit;

namespace DS4WindowsTests
{
    /// <summary>
    /// Phase9 9-E E-4（Phase9-E-Plan.md）: <see cref="VirtualPadService"/> の特性化テスト。
    /// ViGEm の本物（仮想パッドの作成、振動・ライトバーの戻り）は、ViGEmBus とコントローラーが必要なため、台帳 §36 の実機確認で担保する。
    /// ここでは、ViGEm なしで確かめられる範囲（窓口、事前条件による何もしない経路、ホストへの委譲、コンストラクター）を調べる。
    /// </summary>
    public class Phase9EVirtualPadServiceTests
    {
        private sealed class RecordingHost : IVirtualPadHost
        {
            public readonly List<Action> Queued = new List<Action>();
            public void SetDevRumble(DS4Device device, byte heavyMotor, byte lightMotor, int deviceNum) { }
            public void QueueOnEventThread(Action action) { Queued.Add(action); }
        }

        private sealed class NoControllers : IDeviceStateAccessor
        {
            public DS4Device GetController(int deviceIndex) => null;
        }

        private static VirtualPadService Create(RecordingHost host = null, IProfileSettingsService settings = null)
        {
            return new VirtualPadService(TestHost.GetService<IEnvironmentService>(),
                settings ?? TestHost.GetService<IProfileSettingsService>(),
                () => TestHost.GetService<IOutputSlotService>(),
                () => new NoControllers(),
                () => host ?? new RecordingHost());
        }

        [Fact]
        public void InitialState_HasASlotManager_AndEightEmptyOutputDevices_AndNoViGEmClient()
        {
            VirtualPadService service = Create();

            Assert.NotNull(service.OutputSlotManager);
            Assert.False(service.HasViGEmClient);
            Assert.Equal(ControllerLimits.MaxControllerCount, service.OutputDevices.Length);
            Assert.All(service.OutputDevices, d => Assert.Null(d));
            Assert.Same(service.OutputDevices, service.OutputDevices);   // 毎回、同じ配列（コピーしない）
        }

        [Fact]
        public void StopViGEm_WithoutAClient_DoesNothing()
        {
            VirtualPadService service = Create();
            Assert.Null(Record.Exception(() => service.StopViGEm()));
            Assert.False(service.HasViGEmClient);
        }

        [Fact]
        public void QueueOnEventThread_DelegatesToTheHost()
        {
            var host = new RecordingHost();
            VirtualPadService service = Create(host);
            Action action = () => { };

            service.QueueOnEventThread(action);

            Assert.Same(action, Assert.Single(host.Queued));
        }

        [Fact]
        public void UnplugOutDev_WhenTheSlotIsAlreadyDInputOnly_DoesNothing()
        {
            // 旧実装の事前条件: UseDInputOnlyArray[index] が true（仮想パッドなし）のときは、何もしない
            IProfileSettingsService settings = TestHost.GetService<IProfileSettingsService>();
            const int Index = 6;
            bool saved = settings.UseDInputOnlyArray[Index];
            try
            {
                settings.UseDInputOnlyArray[Index] = true;
                VirtualPadService service = Create();

                Assert.Null(Record.Exception(() => service.UnplugOutDev(Index, null)));
                Assert.True(settings.UseDInputOnlyArray[Index]);
            }
            finally
            {
                settings.UseDInputOnlyArray[Index] = saved;
            }
        }

        [Fact]
        public void PluginOutDev_WhenTheSlotAlreadyHasAVirtualPad_DoesNothing()
        {
            // 旧実装の事前条件: UseDInputOnlyArray[index] が false（既に仮想パッドあり）のときは、何もしない（種類の配列も変えない）
            IProfileSettingsService settings = TestHost.GetService<IProfileSettingsService>();
            const int Index = 6;
            bool saved = settings.UseDInputOnlyArray[Index];
            try
            {
                settings.UseDInputOnlyArray[Index] = false;
                VirtualPadService service = Create();

                Assert.Null(Record.Exception(() => service.PluginOutDev(Index, null)));
                Assert.False(settings.UseDInputOnlyArray[Index]);
                Assert.Null(service.OutputDevices[Index]);
            }
            finally
            {
                settings.UseDInputOnlyArray[Index] = saved;
            }
        }

        [Fact]
        public void Constructor_RejectsNull()
        {
            IEnvironmentService env = TestHost.GetService<IEnvironmentService>();
            IProfileSettingsService settings = TestHost.GetService<IProfileSettingsService>();
            Func<IOutputSlotService> slots = () => null;
            Func<IDeviceStateAccessor> accessor = () => null;
            Func<IVirtualPadHost> host = () => null;
            Assert.Throws<ArgumentNullException>(() => new VirtualPadService(null, settings, slots, accessor, host));
            Assert.Throws<ArgumentNullException>(() => new VirtualPadService(env, null, slots, accessor, host));
            Assert.Throws<ArgumentNullException>(() => new VirtualPadService(env, settings, null, accessor, host));
            Assert.Throws<ArgumentNullException>(() => new VirtualPadService(env, settings, slots, null, host));
            Assert.Throws<ArgumentNullException>(() => new VirtualPadService(env, settings, slots, accessor, null));
        }

        [Fact]
        public void ControlService_ExposesTheSameSlotManagerAndOutputDevices_AsTheService()
        {
            ControlService control = TestHost.GetService<ControlService>();
            IVirtualPadService service = TestHost.GetService<IVirtualPadService>();

            Assert.Same(service, control.VirtualPadService);
            Assert.Same(service.OutputSlotManager, control.OutputslotMan);
            Assert.Same(service.OutputDevices, control.outputDevices);
        }
    }
}
