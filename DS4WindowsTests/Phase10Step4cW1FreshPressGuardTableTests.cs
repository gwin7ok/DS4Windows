using System;
using DS4Windows;
using DS4Windows.Actions;
using Xunit;

namespace DS4WindowsTests
{
    /// <summary>
    /// Phase10-Step4c W1（Phase10-Step4c-Plan.md §2 項目 2・§4 W1）: Fresh-press ガードを見るかの規則の表（<see cref="FreshPressGuardTable"/>）の値を固定する。
    /// 値は W1 の前と同じ（挙動は変えない）。束縛に現れる値は <c>Phase10Step4cW0PolicyTableTests</c>（Controls）と SA の表のテストが守る。
    /// </summary>
    public class Phase10Step4cW1FreshPressGuardTableTests
    {
        [Theory]
        [InlineData(SpecialAction.ActionTypeId.Key, true)]
        [InlineData(SpecialAction.ActionTypeId.Macro, true)]
        [InlineData(SpecialAction.ActionTypeId.Program, true)]
        [InlineData(SpecialAction.ActionTypeId.Profile, true)]
        [InlineData(SpecialAction.ActionTypeId.DisconnectBT, true)]
        [InlineData(SpecialAction.ActionTypeId.BatteryCheck, true)]
        [InlineData(SpecialAction.ActionTypeId.GyroCalibrate, true)]
        [InlineData(SpecialAction.ActionTypeId.SASteeringWheelEmulationCalibrate, true)]
        [InlineData(SpecialAction.ActionTypeId.MultiAction, false)]
        public void SpecialActionTypes_FollowTheCurrentRule(SpecialAction.ActionTypeId type, bool expected)
        {
            Assert.Equal(expected, FreshPressGuardTable.ForSpecialAction(type));
            Assert.Equal(expected, FreshPressGuardTable.ForSpecialAction(type, toggle: true));   // Toggle は Button だけに効く
        }

        [Fact]
        public void Button_SeesTheGuardOnlyWhenToggle()
        {
            Assert.False(FreshPressGuardTable.ForSpecialAction(SpecialAction.ActionTypeId.Button, toggle: false));
            Assert.True(FreshPressGuardTable.ForSpecialAction(SpecialAction.ActionTypeId.Button, toggle: true));
        }

        [Theory]
        [InlineData(ControlBindingKind.Key, false)]
        [InlineData(ControlBindingKind.Macro, true)]
        [InlineData(ControlBindingKind.ButtonPad, false)]
        [InlineData(ControlBindingKind.ButtonMouse, false)]
        [InlineData(ControlBindingKind.ButtonAnalogMouse, false)]
        [InlineData(ControlBindingKind.Extras, false)]
        [InlineData(ControlBindingKind.Special, true)]
        public void ControlsKinds_FollowTheCurrentRule(ControlBindingKind kind, bool expected)
        {
            Assert.Equal(expected, FreshPressGuardTable.ForControls(kind));
        }

        [Fact]
        public void EveryControlsKind_HasARowInThisTest()
        {
            // 区分を足したとき、この表のテストに行を足し忘れないようにする（既定の「見ない」へ黙って落ちるのを防ぐ）
            Assert.Equal(7, Enum.GetValues(typeof(ControlBindingKind)).Length);
        }
    }
}
