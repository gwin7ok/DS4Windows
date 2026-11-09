using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using DS4Windows.Core.Settings;
using DS4Windows.UI.ViewModels;
using Xunit;

namespace DS4WindowsTests
{
    /// <summary>
    /// 「Axis Config」タブの 6 軸（<c>SixAxisSettingsPanel</c>）と <see cref="SixAxisSettingsViewModel"/>、
    /// 設定サービスの領域の窓口（<see cref="SixAxisSlotSettings"/>）の守り。
    /// Binding の名前の誤りはビルドで見つからないため、パネルの XAML の Binding を、ViewModel の公開プロパティと突き合わせる。
    /// </summary>
    // パネルのテストは Application のリソース（テーマ）を一時的に足すため、共有のホストの状態を使う他のテストとは並列に走らせない
    [Collection(SharedHostStateCollection.Name)]
    public class SixAxisSettingsViewModelTests
    {
        private static readonly Regex BindingPath = new Regex(@"\{Binding\s+(?:Path=)?(?<path>[A-Za-z_][A-Za-z0-9_]*)", RegexOptions.Compiled);

        [Fact]
        public void EveryBindingInThePanel_NamesAPublicPropertyOfTheViewModel()
        {
            var xaml = XamlTestHelper.Load("DS4Windows/UI/SixAxisSettingsPanel.xaml");
            if (xaml == null) return;

            HashSet<string> properties = typeof(SixAxisSettingsViewModel).GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Select(p => p.Name).ToHashSet();

            var missing = new List<string>();
            foreach (string value in XamlTestHelper.AllAttributeValues(xaml.Root))
            {
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
            var vm = ProfileSettingsViewModelTestFactory.Create(0).SixAxis;
            foreach (PropertyInfo property in typeof(SixAxisSettingsViewModel).GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                Assert.Null(Record.Exception(() => property.GetValue(vm)));
            }
        }

        [Fact]
        public void TheValues_AreReadFromAndWrittenToTheSettingsService()
        {
            var service = TestGlobals.ProfileSettingsServiceInstance;
            var vm = ProfileSettingsViewModelTestFactory.Create(0).SixAxis;
            double dead = service.SXDeadzone[0];
            double sens = service.SZSens[0];
            int mode = service.GetSxOutCurveMode(0);
            try
            {
                var names = new List<string>();
                vm.PropertyChanged += (s, e) => names.Add(e.PropertyName);
                vm.SXDeadZone = dead + 0.25;
                Assert.Equal(dead + 0.25, service.SXDeadzone[0]);
                Assert.Contains(nameof(SixAxisSettingsViewModel.SXDeadZone), names);

                vm.SZSens = 1.5;
                Assert.Equal(1.5, service.SZSens[0]);

                // 曲線の選択を 6（カスタム）にすると、カスタム曲線の入力が有効になる通知が出る
                vm.SXOutputCurveIndex = 6;
                Assert.Equal(6, service.GetSxOutCurveMode(0));
                Assert.True(vm.SXCustomCurveSelected);
                Assert.Contains(nameof(SixAxisSettingsViewModel.SXCustomCurveSelected), names);
            }
            finally
            {
                service.SXDeadzone[0] = dead;
                service.SZSens[0] = sens;
                service.SetSxOutCurveMode(0, mode);
            }
        }

        [Fact]
        public void TheSlotSettings_ComeFromTheServiceEntryPoint_AndTheOldMembersAreGoneFromTheEditorViewModel()
        {
            var service = TestGlobals.ProfileSettingsServiceInstance;
            Assert.Same(service.SxOutBezierCurveObj[3], service.ForSlot(3).SixAxis.SXCurve);

            // 6 軸の項目は、ProfileSettingsViewModel から SixAxis（サブ ViewModel）へ移った
            string[] moved = typeof(SixAxisSettingsViewModel).GetProperties(BindingFlags.Public | BindingFlags.Instance)
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
                var vm = ProfileSettingsViewModelTestFactory.Create(0).SixAxis;
                var theme = (System.Windows.ResourceDictionary)System.Windows.Application.LoadComponent(
                    new Uri("/DS4Windows;component/UI/Themes/DefaultTheme.xaml", UriKind.Relative));
                System.Windows.Application.Current.Resources.MergedDictionaries.Add(theme);
                try
                {
                    var panel = new DS4Windows.UI.SixAxisSettingsPanel();
                    panel.DataContext = vm;

                    // カスタム曲線のボタンが押されたことは、対象を付けてパネルの外へ知らせる
                    string target = null;
                    panel.CustomCurveEditorRequested += (s, t) => target = t;
                    var button = (System.Windows.Controls.Button)panel.FindName("szCustomEditorBtn");
                    button.RaiseEvent(new System.Windows.RoutedEventArgs(System.Windows.Controls.Primitives.ButtonBase.ClickEvent));
                    Assert.Equal("SZ", target);

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
