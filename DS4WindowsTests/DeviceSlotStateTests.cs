using Xunit;
using DS4Windows.Core.Settings;
using DS4Windows.Input;
using DS4Windows.Processing.Abstractions;
using DS4Windows.Processing.Services;

namespace DS4WindowsTests
{
    /// <summary>Phase13-Step5 5a: <see cref="DeviceSlotState"/> の単体テスト（配列の長さ、参照の不変、範囲外の扱い）。</summary>
    public class DeviceSlotStateTests
    {
        [Fact]
        public void Arrays_HaveTheControllerLimitLength()
        {
            IDeviceSlotState s = new DeviceSlotState();
            int n = ControllerLimits.MaxControllerCount;
            Assert.Equal(n, s.DS4Controllers.Length);
            Assert.Equal(n, s.CurrentStates.Length);
            Assert.Equal(n, s.PreviousStates.Length);
            Assert.Equal(n, s.TempStates.Length);
            Assert.Equal(n, s.TouchPads.Length);
            Assert.NotNull(s.SlotManager);
        }

        [Fact]
        public void ArrayReferences_AreNeverReassigned()
        {
            IDeviceSlotState s = new DeviceSlotState();
            Assert.Same(s.DS4Controllers, s.DS4Controllers);
            Assert.Same(s.CurrentStates, s.CurrentStates);
            Assert.Same(s.TempStates, s.TempStates);
            Assert.Same(s.TouchPads, s.TouchPads);
            Assert.Same(s.SlotManager, s.SlotManager);

            // 要素の書き込みは、同じ配列を通して見える
            var state = new DS4State();
            s.CurrentStates[1] = state;
            Assert.Same(state, s.GetCurrentState(1));
            var temp = new DS4State();
            s.TempStates[2] = temp;
            Assert.Same(temp, s.GetTempState(2));
        }

        [Fact]
        public void ArrayProperties_AreGetOnly()
        {
            foreach (string name in new[] { "DS4Controllers", "CurrentStates", "PreviousStates", "TempStates", "TouchPads", "SlotManager" })
            {
                var p = typeof(DeviceSlotState).GetProperty(name);
                Assert.NotNull(p);
                Assert.Null(p.SetMethod);
            }
        }

        [Theory]
        [InlineData(-1)]
        [InlineData(ControllerLimits.MaxControllerCount)]
        [InlineData(int.MaxValue)]
        public void GetController_OutOfRange_ReturnsNull(int index)
        {
            IDeviceStateAccessor s = new DeviceSlotState();
            Assert.Null(s.GetController(index));
        }

        [Fact]
        public void GetController_ReturnsTheDeviceOfTheSlot_OrNullWhenEmpty()
        {
            var s = new DeviceSlotState();
            Assert.Null(s.GetController(0));
            Assert.Null(s.CurrentStates[0]);
        }
    }
}
