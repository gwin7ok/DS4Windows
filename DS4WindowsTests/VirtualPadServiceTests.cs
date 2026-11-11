using System;
using System.Collections.Generic;
using Xunit;
using DS4Windows.Core.Abstractions;
using DS4Windows.Core.Settings;
using DS4Windows.Input;
using DS4Windows.Output.Services;
using DS4Windows.Processing;
using DS4Windows.Processing.Abstractions;

namespace DS4WindowsTests
{
    /// <summary>
    /// Phase9 9-E E-4（Phase9-E-Plan.md）: <see cref="VirtualPadService"/> の特性化テスト。
    /// ViGEm の本物（仮想パッドの作成、振動・ライトバーの戻り）は、ViGEmBus とコントローラーが必要なため、台帳 §36 の実機確認で担保する。
    /// ここでは、ViGEm なしで確かめられる範囲（窓口、事前条件による何もしない経路、ホストへの委譲、コンストラクター）を調べる。
    /// </summary>
    public class VirtualPadServiceTests
    {
        // Phase13-Step3: 旧 IVirtualPadHost の偽実装を、イベントスレッドと振動の偽実装へ分けた
        private sealed class RecordingHost : IEventThreadDispatcher
        {
            public readonly List<Action> Queued = new List<Action>();
            public void Queue(Action action) { Queued.Add(action); }
            public void Invoke(Action action) { action(); }
            public void Shutdown() { }
        }

        private sealed class NoRumble : IDeviceRumbleService
        {
            public void SetDevRumble(InputDeviceCore device, byte heavyMotor, byte lightMotor, int deviceNum) { }
        }

        private sealed class NoControllers : IDeviceStateAccessor
        {
            public InputDeviceCore GetController(int deviceIndex) => null;
        }

        private static VirtualPadService Create(RecordingHost host = null, IProfileSettingsService settings = null, DS4Windows.Output.Backend.IVirtualPadBackend backend = null, IDeviceRumbleService rumble = null)
        {
            return new VirtualPadService(TestHost.GetService<IEnvironmentService>(),
                settings ?? TestHost.GetService<IProfileSettingsService>(),
                TestHost.GetService<IOutputDeviceTypeState>(),   // Phase13-Step7 7-c: 旧 Func<IOutputSlotService>
                new NoControllers(),
                host ?? new RecordingHost(), rumble ?? new NoRumble(), backend ?? new FakeVirtualPadBackend(),
                TestOutputCartridges.For(settings ?? TestHost.GetService<IProfileSettingsService>(), rumble ?? new NoRumble()));
        }

        private sealed class RecordingRumble : IDeviceRumbleService
        {
            public readonly System.Collections.Generic.List<(byte Heavy, byte Light, int Device)> Calls = new System.Collections.Generic.List<(byte, byte, int)>();
            public void SetDevRumble(InputDeviceCore device, byte heavyMotor, byte lightMotor, int deviceNum) => Calls.Add((heavyMotor, lightMotor, deviceNum));
        }

        [Fact]
        public void HasViGEmClient_AndStopViGEm_FollowTheBackend()
        {
            var backend = new FakeVirtualPadBackend();
            VirtualPadService service = Create(backend: backend);
            Assert.False(service.HasViGEmClient);

            backend.Start();
            Assert.True(service.HasViGEmClient);

            service.StopViGEm();
            Assert.Equal(1, backend.StopCount);
            Assert.False(service.HasViGEmClient);
        }

        [Fact]
        public void EstablishOutFeedback_ForAnXbox360_ForwardsTheGamesRumbleToTheDevice_AndTheSubscriptionCanBeReleased()
        {
            var backend = new FakeVirtualPadBackend();
            backend.Start();
            var rumble = new RecordingRumble();
            VirtualPadService service = Create(backend: backend, rumble: rumble);
            DS4Windows.Output.OutputDevice dev = TestOutputCartridges.Allocate(DS4Windows.Core.Model.OutContType.X360, backend);
            FakeXbox360Target target = backend.Xbox360Targets[0];

            service.EstablishOutFeedback(2, DS4Windows.Core.Model.OutContType.X360, dev, null);
            Assert.Equal(1, target.ActiveSubscriptions);
            target.RaiseFeedback(10, 20);
            Assert.Equal((10, 20, 2), Assert.Single(rumble.Calls));

            dev.RemoveFeedbacks();
            Assert.Equal(0, target.ActiveSubscriptions);
            target.RaiseFeedback(1, 1);
            Assert.Single(rumble.Calls);
        }

        [Fact]
        public void SendReport_ForwardsToTheAssignedDevice_AndIgnoresAnEmptySlot()
        {
            var backend = new FakeVirtualPadBackend();
            backend.Start();
            VirtualPadService service = Create(backend: backend);
            service.SendReport(0, new DS4Windows.Core.Model.ControllerState());   // 割り当てなし: 何もしない
            Assert.False(service.HasOutputDevice(0));

            service.OutputDevices[0] = TestOutputCartridges.Allocate(DS4Windows.Core.Model.OutContType.X360, backend);
            service.OutputDevices[0].Connect();
            Assert.True(service.HasOutputDevice(0));
            service.SendReport(0, new DS4Windows.Core.Model.ControllerState { Cross = true });

            FakeXbox360Target target = backend.Xbox360Targets[0];
            Assert.Equal(1, target.SubmitCount);
            Assert.Equal(DS4Windows.Output.Backend.Xbox360Buttons.A, target.Buttons);
        }

        [Fact]
        public void DescribeSlot_ReturnsTheSlotState_AsText()
        {
            VirtualPadService service = Create();
            Assert.Equal(service.OutputSlotManager.OutputSlots[0].CurrentType.ToString(), service.DescribeSlot(0, DS4Windows.Processing.Abstractions.OutputSlotInfoKind.CurrentType));
            Assert.Equal(service.OutputSlotManager.OutputSlots[0].PermanentType.ToString(), service.DescribeSlot(0, DS4Windows.Processing.Abstractions.OutputSlotInfoKind.PermanentType));
            Assert.Equal(service.OutputSlotManager.OutputSlots[0].CurrentAttachedStatus.ToString(), service.DescribeSlot(0, DS4Windows.Processing.Abstractions.OutputSlotInfoKind.AttachedStatus));
            Assert.Equal(service.OutputSlotManager.OutputSlots[0].CurrentInputBound.ToString(), service.DescribeSlot(0, DS4Windows.Processing.Abstractions.OutputSlotInfoKind.InputBound));
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
            IOutputDeviceTypeState slots = new DS4Windows.Output.Services.OutputDeviceTypeState();   // Phase13-Step7 7-c: 旧 Func<IOutputSlotService>
            IDeviceStateAccessor accessor = new NoControllers();
            IEventThreadDispatcher host = new RecordingHost();
            IDeviceRumbleService rumble = new NoRumble();
            DS4Windows.Output.Backend.IVirtualPadBackend backend = new FakeVirtualPadBackend();
            var cartridges = TestOutputCartridges.Default();
            Assert.Throws<ArgumentNullException>(() => new VirtualPadService(env, settings, slots, accessor, host, rumble, backend, null));
            Assert.Throws<ArgumentNullException>(() => new VirtualPadService(null, settings, slots, accessor, host, rumble, backend, cartridges));
            Assert.Throws<ArgumentNullException>(() => new VirtualPadService(env, null, slots, accessor, host, rumble, backend, cartridges));
            Assert.Throws<ArgumentNullException>(() => new VirtualPadService(env, settings, null, accessor, host, rumble, backend, cartridges));
            Assert.Throws<ArgumentNullException>(() => new VirtualPadService(env, settings, slots, null, host, rumble, backend, cartridges));
            Assert.Throws<ArgumentNullException>(() => new VirtualPadService(env, settings, slots, accessor, null, rumble, backend, cartridges));
            Assert.Throws<ArgumentNullException>(() => new VirtualPadService(env, settings, slots, accessor, host, null, backend, cartridges));
            Assert.Throws<ArgumentNullException>(() => new VirtualPadService(env, settings, slots, accessor, host, rumble, null, cartridges));
        }
    }
}
