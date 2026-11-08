using System.Linq;
using System.Reflection;
using Xunit;
using DS4Windows.Processing;
using DS4Windows.Processing.Abstractions;

namespace DS4WindowsTests
{
    /// <summary>
    /// Phase13-Step5 5b: <c>ControlService</c> が、スロット状態の配列を <see cref="IDeviceSlotState"/> から一度だけ取得し、
    /// <c>readonly</c> のフィールドとして保持していること（ホットパスの命令列を変えない。再代入できない）の検査。
    /// </summary>
    [Collection(SharedHostStateCollection.Name)]
    public class ControlServiceSlotStateGuardTests
    {
        private const BindingFlags All = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

        [Theory]
        [InlineData("DS4Controllers")]
        [InlineData("PreviousState")]
        [InlineData("TempState")]
        public void SlotStateFields_AreReadonly(string name)
        {
            FieldInfo f = typeof(ControlService).GetField(name, All);
            Assert.NotNull(f);
            Assert.True(f.IsInitOnly, name + " は readonly でなければならない（配列の参照は再代入されない）");
        }

        [Fact]
        public void ControlService_HoldsTheSameArraysAsTheSlotState()
        {
            DS4Windows.Composition.AppHost.CreateHost();
            var slots = TestHost.GetService<IDeviceSlotState>();
            var control = TestHost.GetService<ControlService>();

            Assert.Same(slots.DS4Controllers, typeof(ControlService).GetField("DS4Controllers", All).GetValue(control));
            Assert.Same(slots.PreviousStates, typeof(ControlService).GetField("PreviousState", All).GetValue(control));
            Assert.Same(slots.TempStates, typeof(ControlService).GetField("TempState", All).GetValue(control));
        }

        [Fact]
        public void GetController_ReadsTheSlotArray()
        {
            DS4Windows.Composition.AppHost.CreateHost();
            var slots = TestHost.GetService<IDeviceSlotState>();

            Assert.Null(slots.GetController(-1));
            Assert.Null(slots.GetController(slots.DS4Controllers.Length));
            Assert.Same(slots.DS4Controllers[0], slots.GetController(0));
        }
    }
}
