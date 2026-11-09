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
    /// 「ジャイロ」タブ（<c>GyroSettingsPanel</c>）と <see cref="GyroSettingsViewModel"/>、
    /// 設定サービスの領域の窓口（<see cref="GyroSlotSettings"/>）の守り。
    /// Binding の名前の誤りはビルドで見つからないため、パネルの XAML の Binding を、ViewModel の公開プロパティと突き合わせる。
    /// </summary>
    // パネルのテストは Application のリソース（テーマ）を一時的に足すため、共有のホストの状態を使う他のテストとは並列に走らせない
    [Collection(SharedHostStateCollection.Name)]
    public class GyroSettingsViewModelTests
    {
        private static readonly Regex BindingPath = new Regex(@"\{Binding\s+(?:Path=)?(?<path>[A-Za-z_][A-Za-z0-9_]*)", RegexOptions.Compiled);

        [Fact]
        public void EveryBindingInThePanel_NamesAPublicPropertyOfTheViewModel()
        {
            var xaml = XamlTestHelper.Load("DS4Windows/UI/GyroSettingsPanel.xaml");
            if (xaml == null) return;

            HashSet<string> properties = typeof(GyroSettingsViewModel).GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Select(p => p.Name).ToHashSet();
            // MappingName は、割り当ての表示用のラベル（gyro*Lb）が持つ DataContext（MappedControl）の項目
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
            var vm = ProfileSettingsViewModelTestFactory.Create(0).Gyro;
            foreach (PropertyInfo property in typeof(GyroSettingsViewModel).GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                Assert.Null(Record.Exception(() => property.GetValue(vm)));
            }
        }

        [Fact]
        public void TheValues_AreReadFromAndWrittenToTheSettingsService()
        {
            var service = TestGlobals.ProfileSettingsServiceInstance;
            var vm = ProfileSettingsViewModelTestFactory.Create(0).Gyro;
            int sensitivity = service.GyroSensitivity[0];
            int invert = service.GyroInvert[0];
            try
            {
                vm.GyroSensitivity = sensitivity + 7;
                Assert.Equal(sensitivity + 7, service.GyroSensitivity[0]);

                // 反転は 1 つの整数のビット（1＝縦、2＝横）
                vm.GyroMouseInvertX = true;
                vm.GyroMouseInvertY = true;
                Assert.Equal(3, service.GyroInvert[0] & 3);
                vm.GyroMouseInvertX = false;
                Assert.Equal(1, service.GyroInvert[0] & 3);
                Assert.False(vm.GyroMouseInvertX);
                Assert.True(vm.GyroMouseInvertY);
            }
            finally
            {
                service.GyroSensitivity[0] = sensitivity;
                service.GyroInvert[0] = invert;
            }
        }

        [Fact]
        public void TheOutputMode_MapsTheIndexBothWays_AndRaisesTheNotification()
        {
            var service = TestGlobals.ProfileSettingsServiceInstance;
            var vm = ProfileSettingsViewModelTestFactory.Create(0).Gyro;
            GyroOutMode original = service.GyroOutputMode[0];
            var names = new List<string>();
            vm.PropertyChanged += (s, e) => names.Add(e.PropertyName);
            try
            {
                vm.GyroOutModeIndex = 2;
                Assert.Equal(GyroOutMode.MouseJoystick, service.GyroOutputMode[0]);
                Assert.Equal(2, vm.GyroOutModeIndex);
                Assert.Contains(nameof(GyroSettingsViewModel.GyroOutModeIndex), names);

                vm.GyroOutModeIndex = 3;
                Assert.Equal(GyroOutMode.Passthru, service.GyroOutputMode[0]);
            }
            finally
            {
                service.GyroOutputMode[0] = original;
            }
        }

        [Fact]
        public void TheSmoothingMethodChange_RenotifiesTheVisibilityOfTheInputPanels()
        {
            var vm = ProfileSettingsViewModelTestFactory.Create(0).Gyro;
            var names = new List<string>();
            vm.PropertyChanged += (s, e) => names.Add(e.PropertyName);

            vm.GyroMouseSmoothMethodIndex = vm.GyroMouseSmoothMethodIndex == 0 ? 1 : 0;
            vm.GyroMouseStickSmoothMethodIndex = vm.GyroMouseStickSmoothMethodIndex == 0 ? 1 : 0;

            foreach (string expected in new[]
            {
                nameof(GyroSettingsViewModel.GyroMouseWeightAvgPanelVisibility), nameof(GyroSettingsViewModel.GyroMouseOneEuroPanelVisibility),
                nameof(GyroSettingsViewModel.GyroMouseStickWeightAvgPanelVisibility), nameof(GyroSettingsViewModel.GyroMouseStickOneEuroPanelVisibility),
            })
            {
                Assert.Contains(expected, names);
            }
        }

        [Fact]
        public void TheSlotSettings_ComeFromTheServiceEntryPoint_AndTheOldMembersAreGoneFromTheEditorViewModel()
        {
            var service = TestGlobals.ProfileSettingsServiceInstance;
            Assert.Equal(3, service.ForSlot(3).Slot);
            Assert.Same(service.GyroMouseInfo[3], service.ForSlot(3).Gyro.Mouse);

            // ジャイロの項目は、ProfileSettingsViewModel から Gyro（サブ ViewModel）へ移った
            string[] left = typeof(ProfileSettingsViewModel).GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Select(p => p.Name)
                .Where(n => n.StartsWith("Gyro", StringComparison.Ordinal) && n != nameof(ProfileSettingsViewModel.Gyro))
                .ToArray();
            Assert.True(left.Length == 0, "ProfileSettingsViewModel に残っている: " + string.Join(", ", left));
        }

        /// <summary>
        /// 実物のパネルを読み込めること（XAML の誤りは、ビルドでは見つからず、実行時にだけ出る）。
        /// トリガーのメニューの項目が作られ、保存されたトリガーが表示へ反映されること。
        /// </summary>
        [Fact]
        public void ThePanel_LoadsWithTheTheme_AndBuildsTheTriggerMenus()
        {
            Exception failure = null;
            var thread = new System.Threading.Thread(() =>
            {
                try
                {
                    var vm = ProfileSettingsViewModelTestFactory.Create(0).Gyro;
                    // アプリと同じく、テーマは Application のリソースにある（StaticResource は、読み込みの時点で解決される）
                    var theme = (System.Windows.ResourceDictionary)System.Windows.Application.LoadComponent(
                        new Uri("/DS4Windows;component/UI/Themes/DefaultTheme.xaml", UriKind.Relative));
                    System.Windows.Application.Current.Resources.MergedDictionaries.Add(theme);
                    try
                    {
                    var panel = new DS4Windows.UI.GyroSettingsPanel();
                    panel.DataContext = vm;

                    panel.CreateTriggerMenus(vm);
                    panel.PopulateTriggerMenus(vm);
                    panel.Measure(new System.Windows.Size(1000, double.PositiveInfinity));
                    panel.Arrange(new System.Windows.Rect(0, 0, 1000, panel.DesiredSize.Height));
                    panel.UpdateLayout();

                    Assert.True(panel.DesiredSize.Height > 0);
                    // 既定は「常にオン」
                    Assert.Equal("Always On", vm.GyroMouseTrigDisplay);
                    Assert.Equal("Always On", vm.GyroControlsTrigDisplay);
                    }
                    finally
                    {
                        System.Windows.Application.Current.Resources.MergedDictionaries.Remove(theme);
                    }
                }
                catch (Exception ex) { failure = ex; }
            });
            thread.SetApartmentState(System.Threading.ApartmentState.STA);
            thread.Start();
            thread.Join();
            if (failure != null) throw new InvalidOperationException("パネルの読み込みに失敗: " + failure, failure);
        }
    }
}
