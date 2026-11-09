using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using DS4Windows.Core.Settings;
using DS4Windows.Core.Utilities;
using DS4Windows.UI.ViewModels;
using Xunit;

namespace DS4WindowsTests
{
    /// <summary>
    /// 「LS」「RS」（スティック）（<c>StickSettingsPanel</c>）と <see cref="StickSettingsViewModel"/>、
    /// 設定サービスの領域の窓口（<see cref="StickSlotSettings"/>）の守り。
    /// Binding の名前の誤りはビルドで見つからないため、パネルの XAML の Binding を、ViewModel の公開プロパティと突き合わせる。
    /// </summary>
    // パネルのテストは Application のリソース（テーマ）を一時的に足すため、共有のホストの状態を使う他のテストとは並列に走らせない
    [Collection(SharedHostStateCollection.Name)]
    public class StickSettingsViewModelTests
    {
        private static readonly Regex BindingPath = new Regex(@"\{Binding\s+(?:Path=)?(?<path>[A-Za-z_][A-Za-z0-9_]*)", RegexOptions.Compiled);

        [Fact]
        public void EveryBindingInThePanel_NamesAPublicPropertyOfTheViewModel()
        {
            var xaml = XamlTestHelper.Load("DS4Windows/UI/StickSettingsPanel.xaml");
            if (xaml == null) return;

            HashSet<string> properties = typeof(StickSettingsViewModel).GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Select(p => p.Name).ToHashSet();
            // MappingName は、割り当ての表示用のラベル（*OuterBindLb）が持つ DataContext（MappedControl）の項目
            properties.Add("MappingName");

            var missing = new List<string>();
            foreach (string value in XamlTestHelper.AllAttributeValues(xaml.Root))
            {
                // Source= で別の資源を指す Binding（Tag の controlIndexCheck）は、ViewModel の項目ではない
                if (value.Contains("Source=")) continue;
                foreach (Match m in BindingPath.Matches(value))
                {
                    if (!properties.Contains(m.Groups["path"].Value)) missing.Add(m.Groups["path"].Value);
                }
            }

            Assert.True(missing.Count == 0, "ViewModel にない Binding: " + string.Join(", ", missing.Distinct()));
        }

        [Fact]
        public void EveryPublicPropertyOfTheViewModel_CanBeReadForARealSlot()
        {
            var vm = ProfileSettingsViewModelTestFactory.Create(0).Sticks;
            foreach (PropertyInfo property in typeof(StickSettingsViewModel).GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                Assert.Null(Record.Exception(() => property.GetValue(vm)));
            }
        }

        [Fact]
        public void TheValues_AreReadFromAndWrittenToTheSettingsService()
        {
            var service = TestGlobals.ProfileSettingsServiceInstance;
            var vm = ProfileSettingsViewModelTestFactory.Create(0).Sticks;
            double sens = service.LSSens[0];
            double rotation = service.RSRotation[0];
            int mode = service.GetLsOutCurveMode(0);
            var deadType = service.RSModInfo[0].deadzoneType;
            try
            {
                vm.LSSens = sens + 0.25;
                Assert.Equal(sens + 0.25, service.LSSens[0]);

                // 回転は、画面では度、保存ではラジアン
                vm.RSRotation = 90.0;
                Assert.Equal(Math.PI / 2, service.RSRotation[0], 6);
                Assert.Equal(90.0, vm.RSRotation, 6);

                // 曲線の選択を 6（カスタム）にすると、カスタム曲線の入力が有効になる通知が出る
                var names = new List<string>();
                vm.PropertyChanged += (s, e) => names.Add(e.PropertyName);
                vm.LSOutputCurveIndex = 6;
                Assert.Equal(6, service.GetLsOutCurveMode(0));
                Assert.True(vm.LSCustomCurveSelected);
                Assert.Contains(nameof(StickSettingsViewModel.LSCustomCurveSelected), names);

                // デッドゾーンの種類（0＝Radial、1＝Axial）
                vm.RSDeadTypeIndex = 1;
                Assert.Equal(StickDeadZoneInfo.DeadZoneType.Axial, service.RSModInfo[0].deadzoneType);
                Assert.Equal(1, vm.RSDeadTypeIndex);
            }
            finally
            {
                service.LSSens[0] = sens;
                service.RSRotation[0] = rotation;
                service.SetLsOutCurveMode(0, mode);
                service.RSModInfo[0].deadzoneType = deadType;
            }
        }

        [Fact]
        public void TheOutputIndex_MapsBothWays_AndRaisesTheNotification()
        {
            var service = TestGlobals.ProfileSettingsServiceInstance;
            var vm = ProfileSettingsViewModelTestFactory.Create(0).Sticks;
            StickMode original = service.RSOutputSettings[0].mode;
            var names = new List<string>();
            vm.PropertyChanged += (s, e) => names.Add(e.PropertyName);
            try
            {
                vm.RSOutputIndex = 2;
                Assert.Equal(StickMode.FlickStick, service.RSOutputSettings[0].mode);
                Assert.Equal(2, vm.RSOutputIndex);
                Assert.Contains(nameof(StickSettingsViewModel.RSOutputIndex), names);
            }
            finally
            {
                service.RSOutputSettings[0].mode = original;
            }
        }

        [Fact]
        public void TheSlotSettings_ComeFromTheServiceEntryPoint_AndTheOldMembersAreGoneFromTheEditorViewModel()
        {
            var service = TestGlobals.ProfileSettingsServiceInstance;
            Assert.Same(service.LSModInfo[3], service.ForSlot(3).Sticks.LSMod);
            Assert.Same(service.RsOutBezierCurveObj[3], service.ForSlot(3).Sticks.RsBezier);

            // スティックの項目は、ProfileSettingsViewModel から Sticks（サブ ViewModel）へ移った
            string[] moved = typeof(StickSettingsViewModel).GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Select(p => p.Name).ToArray();
            string[] left = typeof(ProfileSettingsViewModel).GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Select(p => p.Name).Where(n => moved.Contains(n)).ToArray();
            Assert.True(left.Length == 0, "ProfileSettingsViewModel に残っている: " + string.Join(", ", left));
        }

        /// <summary>
        /// 実物のパネルを読み込めること（XAML の誤りは、ビルドでは見つからず、実行時にだけ出る）。
        /// </summary>
        [Fact]
        public void ThePanel_LoadsWithTheTheme()
        {
            WpfPanelTestRunner.Run(() =>
            {
                var vm = ProfileSettingsViewModelTestFactory.Create(0).Sticks;
                // アプリと同じく、テーマは Application のリソースにある（StaticResource は、読み込みの時点で解決される）
                var theme = (System.Windows.ResourceDictionary)System.Windows.Application.LoadComponent(
                    new Uri("/DS4Windows;component/UI/Themes/DefaultTheme.xaml", UriKind.Relative));
                System.Windows.Application.Current.Resources.MergedDictionaries.Add(theme);
                try
                {
                    var panel = new DS4Windows.UI.StickSettingsPanel();
                    panel.DataContext = vm;

                    // 軸別デッドゾーンの部品が読み込めること
                Assert.NotNull(panel.AxialLeft);
                Assert.NotNull(panel.AxialRight);
                // 割り当ての表示の結び付けは、割り当ての一覧が要るため、ここでは読み込みと配置だけ確かめる

                    panel.Measure(new System.Windows.Size(1000, double.PositiveInfinity));
                    panel.Arrange(new System.Windows.Rect(0, 0, 1000, panel.DesiredSize.Height));
                    panel.UpdateLayout();

                    Assert.True(panel.DesiredSize.Height > 0);
                }
                finally
                {
                    System.Windows.Application.Current.Resources.MergedDictionaries.Remove(theme);
                }
            });
        }
    }
}
