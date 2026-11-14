using DS4Windows.Core.Settings;
using DS4Windows.UI.Services;
using DS4Windows.UI.ViewModels;
using Xunit;

namespace DS4WindowsTests
{
    /// <summary>
    /// 入力の表示へ渡すデッドゾーンの値の集め方（<see cref="ReadingsDeadZoneSnapshot"/>）。
    /// 元は <c>ProfileEditor.Reload</c> にあった分岐で、円形は 1 つの値を X・Y の両方へ、軸ごとは軸の ViewModel の値、どちらでもなければ値なしを固定する。
    /// </summary>
    public class ReadingsDeadZoneSnapshotTests
    {
        [Fact]
        public void Radial_UsesSingleValueForBothAxes()
        {
            var vm = ProfileSettingsViewModelTestFactory.Create(0);
            vm.Sticks.Left.Mod.deadzoneType = StickDeadZoneInfo.DeadZoneType.Radial;
            var snap = ReadingsDeadZoneSnapshot.Create(vm, new AxialStickControlViewModel(vm.Sticks.Left.Mod), new AxialStickControlViewModel(vm.Sticks.Right.Mod));
            Assert.Equal(vm.Sticks.Left.DeadZone, snap.LsDeadX);
            Assert.Equal(vm.Sticks.Left.DeadZone, snap.LsDeadY);
        }

        [Fact]
        public void Axial_UsesAxialViewModelValues()
        {
            var vm = ProfileSettingsViewModelTestFactory.Create(0);
            vm.Sticks.Right.Mod.deadzoneType = StickDeadZoneInfo.DeadZoneType.Axial;
            var right = new AxialStickControlViewModel(vm.Sticks.Right.Mod);
            var snap = ReadingsDeadZoneSnapshot.Create(vm, new AxialStickControlViewModel(vm.Sticks.Left.Mod), right);
            Assert.Equal(right.DeadZoneX, snap.RsDeadX);
            Assert.Equal(right.DeadZoneY, snap.RsDeadY);
        }

        [Fact]
        public void OtherType_LeavesValuesUnset_AndCopiesTheRest()
        {
            var vm = ProfileSettingsViewModelTestFactory.Create(0);
            vm.Sticks.Left.Mod.deadzoneType = StickDeadZoneInfo.DeadZoneType.Radial;
            vm.Sticks.Right.Mod.deadzoneType = (StickDeadZoneInfo.DeadZoneType)99;
            var snap = ReadingsDeadZoneSnapshot.Create(vm, new AxialStickControlViewModel(vm.Sticks.Left.Mod), new AxialStickControlViewModel(vm.Sticks.Right.Mod));
            Assert.Null(snap.RsDeadX);
            Assert.Null(snap.RsDeadY);
            Assert.Equal(vm.Triggers.L2DeadZone, snap.L2Dead);
            Assert.Equal(vm.SixAxis.SZDeadZone, snap.SixAxisZDead);
            Assert.Equal(vm.Sticks.Left.DriftXAxis, snap.LsDriftX);
        }
    }
}
