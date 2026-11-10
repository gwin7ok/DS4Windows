using System.Collections.Generic;
using DS4Windows.Processing.Abstractions;
using DS4Windows.UI.Services;
using Xunit;

namespace DS4WindowsTests
{
    /// <summary>
    /// 出力ピッカーの振動テストの動作（<see cref="ActionRumbleTestController"/>）。
    /// 元は <c>BindingWindow</c> のクリック処理にあった分岐で、強さの上限・反転での入れ替わり・開始と停止を固定する。
    /// </summary>
    public class ActionRumbleTestControllerTests
    {
        private sealed class FakeCommands : IControllerSlotCommands
        {
            public bool Connected = true;
            public readonly List<(byte a, byte b)> Rumbles = new List<(byte, byte)>();
            public bool IsConnected(int slot) => Connected;
            public bool SetRumble(int slot, byte rightLightFast, byte leftHeavySlow) { Rumbles.Add((rightLightFast, leftHeavySlow)); return true; }
            public bool SetHeavyRumble(int slot, byte strength) => true;
            public bool SetLightRumble(int slot, byte strength) => true;
            public bool ApplyDualSenseRumbleOptions(int slot, int rumbleEmulationMode, byte hapticPowerLevel) => false;
            public bool ResetGyroCalibration(int slot) => true;
        }

        [Fact]
        public void Start_PassesLightThenHeavy()
        {
            var cmd = new FakeCommands();
            Assert.Equal(RumbleTestOutcome.Started, new ActionRumbleTestController(cmd).Toggle(0, false, heavy: 200, light: 50, inverseMotors: false));
            Assert.Equal((50, 200), cmd.Rumbles[0]);
        }

        [Fact]
        public void Inverse_SwapsMotors_AndCapsAt255()
        {
            var cmd = new FakeCommands();
            new ActionRumbleTestController(cmd).Toggle(0, false, heavy: 300, light: 50, inverseMotors: true);
            Assert.Equal((255, 50), cmd.Rumbles[0]);
        }

        [Fact]
        public void WhileActive_StopsBothMotors()
        {
            var cmd = new FakeCommands();
            Assert.Equal(RumbleTestOutcome.Stopped, new ActionRumbleTestController(cmd).Toggle(0, true, 10, 10, false));
            Assert.Equal((0, 0), cmd.Rumbles[0]);
        }

        [Fact]
        public void NotConnected_DoesNothing()
        {
            var cmd = new FakeCommands { Connected = false };
            Assert.Equal(RumbleTestOutcome.None, new ActionRumbleTestController(cmd).Toggle(0, false, 10, 10, false));
            Assert.Empty(cmd.Rumbles);
        }
    }
}
