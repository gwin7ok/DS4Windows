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
    /// 「ライトバー」タブ（<c>LightbarSettingsPanel</c>）と <see cref="LightbarSettingsViewModel"/>、
    /// 設定サービスの領域の窓口（<see cref="LightbarSlotSettings"/>）の守り。
    /// Binding の名前の誤りはビルドで見つからないため、パネルの XAML の Binding を、ViewModel の公開プロパティと突き合わせる。
    /// </summary>
    // パネルのテストは Application のリソース（テーマ）を一時的に足すため、共有のホストの状態を使う他のテストとは並列に走らせない
    [Collection(SharedHostStateCollection.Name)]
    public class LightbarSettingsViewModelTests
    {
        private static readonly Regex BindingPath = new Regex(@"\{Binding\s+(?:Path=)?(?<path>[A-Za-z_][A-Za-z0-9_]*)", RegexOptions.Compiled);

        [Fact]
        public void EveryBindingInThePanel_NamesAPublicPropertyOfTheViewModel()
        {
            var xaml = XamlTestHelper.Load("DS4Windows/UI/LightbarSettingsPanel.xaml");
            if (xaml == null) return;

            HashSet<string> properties = typeof(LightbarSettingsViewModel).GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Select(p => p.Name).ToHashSet();
            // Source= を持つ Binding は、別の資源を指す

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
            var vm = ProfileSettingsViewModelTestFactory.Create(0).Lightbar;
            foreach (PropertyInfo property in typeof(LightbarSettingsViewModel).GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                Assert.Null(Record.Exception(() => property.GetValue(vm)));
            }
        }

        [Fact]
        public void TheValues_AreReadFromAndWrittenToTheSettingsService()
        {
            var service = TestGlobals.ProfileSettingsServiceInstance;
            var vm = ProfileSettingsViewModelTestFactory.Create(0).Lightbar;
            var info = service.LightbarSettingsInfo[0];
            byte red = info.ds4winSettings.m_Led.red;
            double rainbow = info.ds4winSettings.rainbow;
            int chargingType = info.ds4winSettings.chargingType;
            try
            {
                var names = new List<string>();
                vm.PropertyChanged += (s, e) => names.Add(e.PropertyName);

                vm.MainColorR = 200;
                Assert.Equal(200, service.LightbarSettingsInfo[0].ds4winSettings.m_Led.red);
                // 色の成分を変えると、色の表示文字列とライトバーの表示も再通知される
                Assert.Contains(nameof(LightbarSettingsViewModel.MainColorString), names);
                Assert.Contains(nameof(LightbarSettingsViewModel.LightbarBrush), names);
                Assert.Equal("#C8FF0000", vm.MainColorRString);

                // 虹色にすると、ライトバーの表示は色ではなく画像になる
                vm.Rainbow = 5.0;
                Assert.True(vm.RainbowExists);
                Assert.IsType<System.Windows.Media.ImageBrush>(vm.LightbarBrush);
                vm.Rainbow = 0.0;
                Assert.IsType<System.Windows.Media.SolidColorBrush>(vm.LightbarBrush);

                // 充電時の色の表示は、種類が 3 のときだけ
                vm.ChargingType = 3;
                Assert.Equal(System.Windows.Visibility.Visible, vm.ChargingColorVisible);
                vm.ChargingType = 1;
                Assert.Equal(System.Windows.Visibility.Hidden, vm.ChargingColorVisible);
            }
            finally
            {
                info.ds4winSettings.m_Led.red = red;
                info.ds4winSettings.rainbow = rainbow;
                info.ds4winSettings.chargingType = chargingType;
            }
        }

        [Fact]
        public void TheModeIndex_MapsBothWays()
        {
            var service = TestGlobals.ProfileSettingsServiceInstance;
            var vm = ProfileSettingsViewModelTestFactory.Create(0).Lightbar;
            LightbarMode original = service.LightbarSettingsInfo[0].Mode;
            try
            {
                vm.LightbarModeIndex = 1;
                Assert.Equal(LightbarMode.Passthru, service.LightbarSettingsInfo[0].Mode);
                vm.LightbarModeIndex = 0;
                Assert.Equal(LightbarMode.DS4Win, service.LightbarSettingsInfo[0].Mode);
            }
            finally
            {
                service.LightbarSettingsInfo[0].Mode = original;
            }
        }

        [Fact]
        public void TheSlotSettings_ComeFromTheServiceEntryPoint_AndTheOldMembersAreGoneFromTheEditorViewModel()
        {
            var service = TestGlobals.ProfileSettingsServiceInstance;
            Assert.Same(service.LightbarSettingsInfo[3], service.ForSlot(3).Lightbar.Info);

            // ライトバーの項目は、ProfileSettingsViewModel から Lightbar（サブ ViewModel）へ移った
            string[] moved = typeof(LightbarSettingsViewModel).GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Select(p => p.Name).Where(n => n != nameof(LightbarSettingsViewModel.UsingMinViGEm173333)).ToArray();
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
                var vm = ProfileSettingsViewModelTestFactory.Create(0).Lightbar;
                // アプリと同じく、テーマは Application のリソースにある（StaticResource は、読み込みの時点で解決される）
                var theme = (System.Windows.ResourceDictionary)System.Windows.Application.LoadComponent(
                    new Uri("/DS4Windows;component/UI/Themes/DefaultTheme.xaml", UriKind.Relative));
                System.Windows.Application.Current.Resources.MergedDictionaries.Add(theme);
                try
                {
                    var panel = new DS4Windows.UI.LightbarSettingsPanel();
                    panel.DataContext = vm;

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
