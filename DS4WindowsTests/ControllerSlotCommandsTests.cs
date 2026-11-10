using DS4Windows.Processing.Services;
using Xunit;

namespace DS4WindowsTests
{
    /// <summary>
    /// UI 向けの、スロット指定の機器操作の窓口（<see cref="ControllerSlotCommands"/>）の振る舞い。
    /// 機器は HID なしでは作れない部分があるため、ここでは接続の有無と範囲外の扱い（例外にせず false）を守る。
    /// </summary>
    public class ControllerSlotCommandsTests
    {
        private static ControllerSlotCommands Create(out DeviceSlotState state)
        {
            state = new DeviceSlotState();
            return new ControllerSlotCommands(state);
        }

        [Fact]
        public void IsConnected_ReflectsTheSlotContent()
        {
            var commands = Create(out var state);
            Assert.False(commands.IsConnected(0));
            Assert.False(commands.IsConnected(-1));
            Assert.False(commands.IsConnected(state.Controllers.Length));

            state.Controllers[2] = TestDs4Device.Create();

            Assert.True(commands.IsConnected(2));
        }

        [Fact]
        public void RumbleAndDualSenseCommands_ReturnFalse_WhenNoDeviceIsConnected()
        {
            var commands = Create(out _);

            Assert.False(commands.SetRumble(0, 10, 20));
            Assert.False(commands.SetHeavyRumble(0, 10));
            Assert.False(commands.SetLightRumble(0, 10));
            Assert.False(commands.ApplyDualSenseRumbleOptions(0, 0, 50));
        }

        [Fact]
        public void ApplyDualSenseRumbleOptions_ReturnsFalse_ForANonDualSenseDevice()
        {
            var commands = Create(out var state);
            state.Controllers[0] = TestDs4Device.Create();

            Assert.False(commands.ApplyDualSenseRumbleOptions(0, 0, 50));
        }
    }
}
