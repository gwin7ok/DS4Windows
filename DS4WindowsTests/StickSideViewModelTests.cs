using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using DS4Windows.Core.Settings;
using DS4Windows.UI.ViewModels;
using Xunit;

namespace DS4WindowsTests
{
    /// <summary>
    /// <see cref="StickSideViewModel"/>（左右のスティックで共通の ViewModel）の守り。
    /// 左右が同じ項目の集まりであること、片側の変更がもう一方へ影響しないこと、変更が自分の側の設定の値へ書き込まれることを、
    /// 公開プロパティを全部たどって確かめる（項目を足したとき、検査の書き足しなしで対象に入る）。
    /// </summary>
    public class StickSideViewModelTests
    {
        private static IEnumerable<PropertyInfo> WritableProperties()
            => typeof(StickSideViewModel).GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(p => p.CanRead && p.CanWrite && p.Name != nameof(StickSideViewModel.CustomCurve));

        private static object Different(PropertyInfo property, object current)
        {
            Type type = property.PropertyType;
            if (type == typeof(bool)) return !(bool)current;
            if (type == typeof(int)) return (int)current == 1 ? 0 : 1;
            if (type == typeof(sbyte)) return (sbyte)current == 5 ? (sbyte)6 : (sbyte)5;
            if (type == typeof(double)) return (double)current + 0.5;
            throw new InvalidOperationException("未対応の型の項目: " + property.Name + "（" + type + "）。検査へ型を足す");
        }

        private static Dictionary<string, object> Snapshot(StickSideViewModel vm)
            => typeof(StickSideViewModel).GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(p => p.CanRead)
                .ToDictionary(p => p.Name, p => p.GetValue(vm));

        [Fact]
        public void TheSidesHaveTheSameProperties_AndTheViewModelOfTheStickPairHoldsNoItemsOfItsOwn()
        {
            var sticks = new StickSettingsViewModel(new StickSlotSettings(0));
            Assert.True(sticks.Left.IsLeft);
            Assert.False(sticks.Right.IsLeft);

            // 左右で共通の項目は、片側の型だけが持つ（スティックの組には、項目を足さない）
            string[] own = typeof(StickSettingsViewModel).GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
                .Select(p => p.Name).OrderBy(n => n).ToArray();
            Assert.Equal(new[] { nameof(StickSettingsViewModel.Left), nameof(StickSettingsViewModel.Right) }, own);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void ChangingAnyProperty_ChangesOnlyThatSide(bool left)
        {
            foreach (PropertyInfo property in WritableProperties())
            {
                var sticks = new StickSettingsViewModel(new StickSlotSettings(0));
                StickSideViewModel target = left ? sticks.Left : sticks.Right;
                StickSideViewModel other = left ? sticks.Right : sticks.Left;

                Dictionary<string, object> otherBefore = Snapshot(other);
                object before = property.GetValue(target);
                property.SetValue(target, Different(property, before));

                Assert.True(!Equals(before, property.GetValue(target)),
                    $"{property.Name}（{(left ? "左" : "右")}）: 書き込んだ値が読み出しへ反映されない");
                Dictionary<string, object> otherAfter = Snapshot(other);
                foreach (string name in otherBefore.Keys)
                    Assert.True(Equals(otherBefore[name], otherAfter[name]), $"{property.Name}（{(left ? "左" : "右")}）を変えると、反対側の {name} が変わった");
            }
        }

        [Fact]
        public void TheValuesOfBothSides_AreWrittenToTheirOwnSettings()
        {
            var settings = new StickSlotSettings(0);
            var sticks = new StickSettingsViewModel(settings);

            sticks.Left.Sens = 1.5; sticks.Right.Sens = 2.5;
            Assert.Equal(1.5, settings.LSSens); Assert.Equal(2.5, settings.RSSens);

            sticks.Left.Rotation = 90.0; sticks.Right.Rotation = 45.0;
            Assert.Equal(Math.PI / 2, settings.LSRotation, 6); Assert.Equal(Math.PI / 4, settings.RSRotation, 6);

            sticks.Left.SquareStick = true; sticks.Right.SquareStick = false;
            Assert.True(settings.SquStick.lsMode); Assert.False(settings.SquStick.rsMode);

            sticks.Left.DriftXAxis = 3; sticks.Right.DriftYAxis = -4;
            Assert.Equal(3, settings.LeftStickDriftXAxis); Assert.Equal(-4, settings.RightStickDriftYAxis);
            Assert.Equal(0, settings.RightStickDriftXAxis);

            sticks.Left.OutputIndex = 2; sticks.Right.OutputIndex = 1;
            Assert.Equal(StickMode.FlickStick, settings.LSOutput.mode); Assert.Equal(StickMode.Controls, settings.RSOutput.mode);

            sticks.Left.DeadZone = 0.5; sticks.Right.DeadZone = 0.25;
            Assert.Equal(64, settings.LSMod.deadZone); Assert.Equal(32, settings.RSMod.deadZone);

            sticks.Right.FlickTime = 0.7; sticks.Left.DeltaMinFactor = 0.3;
            Assert.Equal(0.7, settings.RSOutput.outputSettings.flickSettings.flickTime);
            Assert.Equal(0.3, settings.LSOutput.outputSettings.controlSettings.deltaAccelSettings.minfactor);
        }

        [Fact]
        public void TheNotificationNames_AreTheSameForBothSides()
        {
            foreach (bool left in new[] { true, false })
            {
                var sticks = new StickSettingsViewModel(new StickSlotSettings(0));
                StickSideViewModel vm = left ? sticks.Left : sticks.Right;
                var names = new List<string>();
                vm.PropertyChanged += (s, e) => names.Add(e.PropertyName);

                vm.DeadZone = 0.5;
                vm.DeadTypeIndex = 1;
                vm.OutputCurveIndex = 6;
                vm.OutputIndex = 2;
                vm.DeltaAccelEnabled = true;
                vm.DriftXAxis = 2;
                vm.DriftYAxis = 2;

                Assert.Equal(new[]
                {
                    nameof(StickSideViewModel.DeadZone), nameof(StickSideViewModel.DeadTypeIndex),
                    nameof(StickSideViewModel.CustomCurveSelected), nameof(StickSideViewModel.OutputIndex),
                    nameof(StickSideViewModel.DeltaAccelEnabled), nameof(StickSideViewModel.DriftXAxis),
                    nameof(StickSideViewModel.DriftYAxis),
                }, names);
            }
        }
    }
}
