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
    /// 「L2 & R2」（トリガー）（<c>TriggerSettingsPanel</c>）と <see cref="TriggerSettingsViewModel"/>、
    /// 設定サービスの領域の窓口（<see cref="TriggerSlotSettings"/>）の守り。
    /// Binding の名前の誤りはビルドで見つからないため、パネルの XAML の Binding を、ViewModel の公開プロパティと突き合わせる。
    /// </summary>
    // パネルのテストは Application のリソース（テーマ）を一時的に足すため、共有のホストの状態を使う他のテストとは並列に走らせない
    [Collection(SharedHostStateCollection.Name)]
    public class TriggerSettingsViewModelTests
    {
        private static readonly Regex BindingPath = new Regex(@"\{Binding\s+(?:Path=)?(?<path>[A-Za-z_][A-Za-z0-9_]*)", RegexOptions.Compiled);

        [Fact]
        public void EveryBindingInThePanel_NamesAPublicPropertyOfTheViewModel()
        {
            var xaml = XamlTestHelper.Load("DS4Windows/UI/TriggerSettingsPanel.xaml");
            if (xaml == null) return;

            HashSet<string> properties = typeof(TriggerSettingsViewModel).GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Select(p => p.Name).ToHashSet();
            // MappingName は、割り当ての表示用のラベル（*FullPullLb）が持つ DataContext（MappedControl）の項目
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
            var vm = ProfileSettingsViewModelTestFactory.Create(0).Triggers;
            foreach (PropertyInfo property in typeof(TriggerSettingsViewModel).GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                Assert.Null(Record.Exception(() => property.GetValue(vm)));
            }
        }

        [Fact]
        public void TheValues_AreReadFromAndWrittenToTheSettingsService()
        {
            var service = TestGlobals.ProfileSettingsServiceInstance;
            var vm = ProfileSettingsViewModelTestFactory.Create(0).Triggers;
            double sens = service.L2Sens[0];
            int mode = service.GetR2OutCurveMode(0);
            var twoStage = service.L2OutputSettings[0].TwoStageMode;
            try
            {
                vm.L2Sens = sens + 0.5;
                Assert.Equal(sens + 0.5, service.L2Sens[0]);

                // 曲線の選択を 6（カスタム）にすると、カスタム曲線の入力が有効になる通知が出る
                var names = new List<string>();
                vm.PropertyChanged += (s, e) => names.Add(e.PropertyName);
                vm.R2OutputCurveIndex = 6;
                Assert.Equal(6, service.GetR2OutCurveMode(0));
                Assert.True(vm.R2CustomCurveSelected);
                Assert.Contains(nameof(TriggerSettingsViewModel.R2CustomCurveSelected), names);

                vm.L2TriggerMode = TwoStageTriggerMode.HipFire;
                Assert.Equal(TwoStageTriggerMode.HipFire, service.L2OutputSettings[0].TwoStageMode);
            }
            finally
            {
                service.L2Sens[0] = sens;
                service.SetR2OutCurveMode(0, mode);
                service.L2OutputSettings[0].TwoStageMode = twoStage;
            }
        }

        [Fact]
        public void TheSlotSettings_ComeFromTheServiceEntryPoint_AndTheOldMembersAreGoneFromTheEditorViewModel()
        {
            var service = TestGlobals.ProfileSettingsServiceInstance;
            Assert.Same(service.L2ModInfo[3], service.ForSlot(3).Triggers.L2Mod);
            Assert.Same(service.R2OutputSettings[3], service.ForSlot(3).Triggers.R2Output);

            // トリガーの項目は、ProfileSettingsViewModel から Triggers（サブ ViewModel）へ移った
            string[] moved = typeof(TriggerSettingsViewModel).GetProperties(BindingFlags.Public | BindingFlags.Instance)
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
                var vm = ProfileSettingsViewModelTestFactory.Create(0).Triggers;
                // アプリと同じく、テーマは Application のリソースにある（StaticResource は、読み込みの時点で解決される）
                var theme = (System.Windows.ResourceDictionary)System.Windows.Application.LoadComponent(
                    new Uri("/DS4Windows;component/UI/Themes/DefaultTheme.xaml", UriKind.Relative));
                System.Windows.Application.Current.Resources.MergedDictionaries.Add(theme);
                try
                {
                    var panel = new DS4Windows.UI.TriggerSettingsPanel();
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
