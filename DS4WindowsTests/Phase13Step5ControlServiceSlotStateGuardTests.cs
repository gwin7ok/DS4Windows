using System.Linq;
using System.Reflection;
using DS4Windows;
using DS4Windows.DI;
using Xunit;

namespace DS4WindowsTests
{
    /// <summary>
    /// Phase13-Step5 5b: <c>ControlService</c> が、スロット状態の配列を <see cref="IDeviceSlotState"/> から一度だけ取得し、
    /// <c>readonly</c> のフィールドとして保持していること（ホットパスの命令列を変えない。再代入できない）の検査。
    /// </summary>
    [Collection("ServiceProviderHolder")]
    public class Phase13Step5ControlServiceSlotStateGuardTests
    {
        private const BindingFlags All = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

        [Theory]
        [InlineData("DS4Controllers")]
        [InlineData("touchPad")]
        [InlineData("slotManager")]
        [InlineData("CurrentState")]
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
            DS4WinWPF.AppHost.CreateHost();
            var slots = TestHost.GetService<IDeviceSlotState>();
            var control = TestHost.GetService<ControlService>();

            Assert.Same(slots.DS4Controllers, control.DS4Controllers);
            Assert.Same(slots.TouchPads, control.touchPad);
            Assert.Same(slots.SlotManager, control.slotManager);
            Assert.Same(slots.CurrentStates, typeof(ControlService).GetField("CurrentState", All).GetValue(control));
            Assert.Same(slots.PreviousStates, typeof(ControlService).GetField("PreviousState", All).GetValue(control));
            Assert.Same(slots.TempStates, typeof(ControlService).GetField("TempState", All).GetValue(control));
        }

        [Fact]
        public void GetController_ReadsTheSlotState()
        {
            DS4WinWPF.AppHost.CreateHost();
            var slots = TestHost.GetService<IDeviceSlotState>();
            var control = TestHost.GetService<ControlService>();

            Assert.Null(control.GetController(-1));
            Assert.Null(control.GetController(slots.DS4Controllers.Length));
            Assert.Same(slots.GetController(0), control.GetController(0));
        }
    }
}
