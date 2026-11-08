using System;
using System.Collections.Generic;
using Xunit;
using DS4Windows.Core.Abstractions;
using DS4Windows.Core.Platform;
using DS4Windows.Input;
using DS4Windows.Processing.Abstractions;
using DS4Windows.Processing.Actions;

namespace DS4WindowsTests
{
    /// <summary>
    /// Phase13-Step6 6-b: <c>DefaultControlExtrasPort.SetRumble</c> は、旧 <c>ControlService.setRumble</c> と同じ検査
    /// （スロット上限 → コントローラーの有無）を経てから <see cref="IDeviceRumbleService"/> を呼ぶ。
    /// </summary>
    public class ControlExtrasPortRumbleTests
    {
        private sealed class FakeAccessor : IDeviceStateAccessor
        {
            public DS4Device Device;
            public DS4Device GetController(int deviceIndex) => Device;
        }

        private sealed class FakeRumble : IDeviceRumbleService
        {
            public readonly List<(byte heavy, byte light, int slot)> Calls = new List<(byte, byte, int)>();
            public void SetDevRumble(DS4Device device, byte heavyMotor, byte lightMotor, int deviceNum) => Calls.Add((heavyMotor, lightMotor, deviceNum));
        }

        private static DefaultControlExtrasPort Create(FakeAccessor accessor, FakeRumble rumble)
            => new DefaultControlExtrasPort(TestLightbar.State, accessor, rumble, new EnvironmentService(), TestRepositoryFactory.CreateProfileSettingsService());

        [Fact]
        public void SetRumble_WithAController_CallsTheRumbleService()
        {
            var accessor = new FakeAccessor { Device = TestDs4Device.Create() };
            var rumble = new FakeRumble();
            Create(accessor, rumble).SetRumble(1, 200, 100);
            Assert.Equal(new[] { ((byte)200, (byte)100, 1) }, rumble.Calls);
        }

        [Fact]
        public void SetRumble_WithoutAController_DoesNothing()
        {
            var rumble = new FakeRumble();
            Create(new FakeAccessor(), rumble).SetRumble(1, 200, 100);
            Assert.Empty(rumble.Calls);
        }

        [Fact]
        public void SetRumble_AtOrAboveTheSlotLimit_DoesNothing()
        {
            var accessor = new FakeAccessor { Device = TestDs4Device.Create() };
            var rumble = new FakeRumble();
            int limit = new EnvironmentService().ControllerSlotLimit;
            Create(accessor, rumble).SetRumble(limit, 200, 100);
            Create(accessor, rumble).SetRumble(-1, 200, 100);
            Assert.Empty(rumble.Calls);
        }

        [Fact]
        public void Constructor_RequiresAllDependencies()
        {
            var accessor = new FakeAccessor();
            var rumble = new FakeRumble();
            var env = new EnvironmentService();
            var settings = TestRepositoryFactory.CreateProfileSettingsService();
            Assert.Throws<ArgumentNullException>(() => new DefaultControlExtrasPort(TestLightbar.State, null, rumble, env, settings));
            Assert.Throws<ArgumentNullException>(() => new DefaultControlExtrasPort(TestLightbar.State, accessor, null, env, settings));
            Assert.Throws<ArgumentNullException>(() => new DefaultControlExtrasPort(TestLightbar.State, accessor, rumble, null, settings));
            Assert.Throws<ArgumentNullException>(() => new DefaultControlExtrasPort(TestLightbar.State, accessor, rumble, env, null));
        }
    }
}
