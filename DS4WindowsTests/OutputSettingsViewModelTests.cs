using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using DS4Windows.Core.Model;
using DS4Windows.Core.Settings;
using DS4Windows.UI.ViewModels;
using Xunit;

namespace DS4WindowsTests
{
    /// <summary>
    /// 「その他」タブの「仮想コントローラー」と「振動」（<c>OutputSettingsPanel</c>）と <see cref="OutputSettingsViewModel"/>、
    /// 設定サービスの領域の窓口（<see cref="OutputSlotSettings"/>）の守り。
    /// Binding の名前の誤りはビルドで見つからないため、パネルの XAML の Binding を、ViewModel の公開プロパティと突き合わせる。
    /// </summary>
    // パネルのテストは Application のリソース（テーマ）を一時的に足すため、共有のホストの状態を使う他のテストとは並列に走らせない
    [Collection(SharedHostStateCollection.Name)]
    public class OutputSettingsViewModelTests
    {
        private static readonly Regex BindingPath = new Regex(@"\{Binding\s+(?:Path=)?(?<path>[A-Za-z_][A-Za-z0-9_]*)", RegexOptions.Compiled);

        [Fact]
        public void EveryBindingInThePanel_NamesAPublicPropertyOfTheViewModel()
        {
            var xaml = XamlTestHelper.Load("DS4Windows/UI/OutputSettingsPanel.xaml");
            if (xaml == null) return;

            HashSet<string> properties = typeof(OutputSettingsViewModel).GetProperties(BindingFlags.Public | BindingFlags.Instance)
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
            var vm = ProfileSettingsViewModelTestFactory.Create(0).Output;
            foreach (PropertyInfo property in typeof(OutputSettingsViewModel).GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                Assert.Null(Record.Exception(() => property.GetValue(vm)));
            }
        }

        [Fact]
        public void TheValues_AreReadFromAndWrittenToTheSettingsService()
        {
            var service = TestGlobals.ProfileSettingsServiceInstance;
            var vm = ProfileSettingsViewModelTestFactory.Create(0).Output;
            bool dinputOnly = service.DinputOnly[0];
            byte boost = service.RumbleBoost[0];
            int autostop = service.GetRumbleAutostopTime(0);
            bool inverse = service.InverseRumbleMotors[0];
            var emulation = service.DualSenseRumbleEmulationMode[0];
            try
            {
                var names = new List<string>();
                vm.PropertyChanged += (s, e) => names.Add(e.PropertyName);
                vm.DInputOnly = !dinputOnly;
                Assert.Equal(!dinputOnly, service.DinputOnly[0]);
                Assert.Contains(nameof(OutputSettingsViewModel.DInputOnly), names);

                vm.RumbleBoost = 150;
                Assert.Equal(150, service.RumbleBoost[0]);

                // 画面は秒、設定はミリ秒
                vm.RumbleAutostopTime = 12;
                Assert.Equal(12000, service.GetRumbleAutostopTime(0));
                Assert.Equal(12, vm.RumbleAutostopTime);

                vm.InverseRumbleMotors = !inverse;
                Assert.Equal(!inverse, service.InverseRumbleMotors[0]);

                vm.DualSenseRumbleEmulationPerIndex = 1;
                Assert.Equal(DS4Windows.Input.Devices.DualSenseDevice.RumbleEmulationMode.Legacy, service.DualSenseRumbleEmulationMode[0]);
                Assert.Equal(1, vm.DualSenseRumbleEmulationPerIndex);
            }
            finally
            {
                service.DinputOnly[0] = dinputOnly;
                service.RumbleBoost[0] = boost;
                service.SetRumbleAutostopTime(0, autostop);
                service.InverseRumbleMotors[0] = inverse;
                service.DualSenseRumbleEmulationMode[0] = emulation;
            }
        }

        [Fact]
        public void TheSelectedOutputType_IsWrittenToTheSavedValueAndTheTemporaryType()
        {
            var service = TestGlobals.ProfileSettingsServiceInstance;
            var vm = ProfileSettingsViewModelTestFactory.Create(0).Output;
            OutContType saved = service.OutContType[0];
            try
            {
                vm.TempControllerIndex = 1;
                Assert.Equal(OutContType.DS4, service.OutContType[0]);
                Assert.Equal(1, vm.ControllerTypeIndex);
                Assert.Equal(OutContType.DS4, vm.TempConType);

                vm.TempConType = OutContType.X360;
                Assert.Equal(OutContType.X360, service.OutContType[0]);
                Assert.Equal(0, vm.TempControllerIndex);
            }
            finally
            {
                service.OutContType[0] = saved;
            }
        }

        [Fact]
        public void TheSlotSettings_ComeFromTheServiceEntryPoint_AndTheOldMembersAreGoneFromTheEditorViewModel()
        {
            var service = TestGlobals.ProfileSettingsServiceInstance;
            OutputSlotSettings output = service.ForSlot(3).Output;
            bool saved = service.InverseRumbleMotors[3];
            try
            {
                output.InverseRumbleMotors = !saved;
                Assert.Equal(!saved, service.InverseRumbleMotors[3]);
            }
            finally
            {
                service.InverseRumbleMotors[3] = saved;
            }

            // 出力の項目は、ProfileSettingsViewModel から Output（サブ ViewModel）へ移った
            string[] moved = typeof(OutputSettingsViewModel).GetProperties(BindingFlags.Public | BindingFlags.Instance)
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
                var vm = ProfileSettingsViewModelTestFactory.Create(0).Output;
                var theme = (System.Windows.ResourceDictionary)System.Windows.Application.LoadComponent(
                    new Uri("/DS4Windows;component/UI/Themes/DefaultTheme.xaml", UriKind.Relative));
                System.Windows.Application.Current.Resources.MergedDictionaries.Add(theme);
                try
                {
                    var panel = new DS4Windows.UI.OutputSettingsPanel();
                    panel.DataContext = vm;

                    // 振動のテストのボタンが押されたことは、パネルの外へ知らせる
                    object sender = null;
                    panel.RumbleTestClick += (s, e) => sender = s;
                    panel.HeavyRumbleTestBtn.RaiseEvent(new System.Windows.RoutedEventArgs(System.Windows.Controls.Primitives.ButtonBase.ClickEvent));
                    Assert.Same(panel.HeavyRumbleTestBtn, sender);

                    panel.Measure(new System.Windows.Size(1000, double.PositiveInfinity));
                    panel.Arrange(new System.Windows.Rect(0, 0, 1000, panel.DesiredSize.Height));
                    panel.UpdateLayout();

                    Assert.True(panel.DesiredSize.Height > 0);
                    Assert.NotNull(panel.OutConTypeCombo);
                }
                finally
                {
                    System.Windows.Application.Current.Resources.MergedDictionaries.Remove(theme);
                }
            });
        }
    }
}
