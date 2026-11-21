using DS4Windows.Core.Abstractions;
using DS4Windows.Core.Settings;
using DS4Windows.Processing;
using DS4Windows.Processing.Touchpad;
using Xunit;

namespace DS4WindowsTests
{
    /// <summary>タッチ → スティック（<see cref="TouchStickOutput"/>）の出力。TouchpadMouse から切り出したときの特性化。</summary>
    public class TouchStickOutputTests
    {
        private const int Slot = 0;

        private static (TouchStickOutput output, StickMotionState sticks, TouchMouseStickInfo info) Create()
        {
            var settings = TestRepositoryFactory.CreateProfileSettingsService(
                new BackingStore(new DS4Windows.Core.Platform.EnvironmentService()));
            var sticks = new StickMotionState();
            TouchMouseStickInfo info = settings.ForSlot(Slot).Touchpad.MouseStick;
            info.outputStick = TouchMouseStickInfo.OutputStick.LeftStick;
            info.outputStickDir = TouchMouseStickInfo.OutputStickAxes.XY;
            info.UseSmoothing = false;
            return (new TouchStickOutput(Slot, settings, sticks), sticks, info);
        }

        [Fact]
        public void NoMovement_LeavesTheStickUntouched()
        {
            var (output, sticks, _) = Create();
            output.Apply(0, 0, 0.004);
            Assert.False(sticks.Devices[Slot].MapStickActionData.dirty);
            Assert.Equal(128, sticks.Devices[Slot].MapStickActionData.LX);
        }

        [Fact]
        public void LargeMovementRight_PushesTheLeftStickRight_AndNotTheRightStick()
        {
            var (output, sticks, info) = Create();
            output.Apply(info.maxZone, 0, 0.004);
            PostMapStickData d = sticks.Devices[Slot].MapStickActionData;
            Assert.True(d.dirty);
            Assert.True(d.LX > 128, $"LX = {d.LX}");
            Assert.Equal(128, d.RX);
        }

        [Fact]
        public void LargeMovementLeft_PushesTheStickLeft()
        {
            var (output, sticks, info) = Create();
            output.Apply(-info.maxZone, 0, 0.004);
            Assert.True(sticks.Devices[Slot].MapStickActionData.LX < 128);
        }

        [Fact]
        public void TheSameValuesAreRepeatable_AndTheWeakerPushNeverOverwritesAStrongerOne()
        {
            var (output, sticks, info) = Create();
            output.Apply(info.maxZone, 0, 0.004);
            byte strong = sticks.Devices[Slot].MapStickActionData.LX;
            output.Apply(info.maxZone / 4, 0, 0.004);
            Assert.Equal(strong, sticks.Devices[Slot].MapStickActionData.LX);
        }
    }
}
