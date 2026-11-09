using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using DS4Windows.Core.Settings;
using DS4Windows.Processing;
using DS4Windows.UI.ViewModels;
using Xunit;

namespace DS4WindowsTests
{
    /// <summary>
    /// 「その他」タブの一般項目（<c>GeneralSettingsPanel</c>）と <see cref="GeneralSettingsViewModel"/>、
    /// 設定サービスの領域の窓口（<see cref="GeneralSlotSettings"/>）の守り。
    /// Binding の名前の誤りはビルドで見つからないため、パネルの XAML の Binding を、ViewModel の公開プロパティと突き合わせる。
    /// </summary>
    // パネルのテストは Application のリソース（テーマ）を一時的に足すため、共有のホストの状態を使う他のテストとは並列に走らせない
    [Collection(SharedHostStateCollection.Name)]
    public class GeneralSettingsViewModelTests
    {
        private static readonly Regex BindingPath = new Regex(@"\{Binding\s+(?:Path=)?(?<path>[A-Za-z_][A-Za-z0-9_]*)", RegexOptions.Compiled);

        [Fact]
        public void EveryBindingInThePanel_NamesAPublicPropertyOfTheViewModel()
        {
            var xaml = XamlTestHelper.Load("DS4Windows/UI/GeneralSettingsPanel.xaml");
            if (xaml == null) return;

            HashSet<string> properties = typeof(GeneralSettingsViewModel).GetProperties(BindingFlags.Public | BindingFlags.Instance)
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
            var vm = ProfileSettingsViewModelTestFactory.Create(0).General;
            foreach (PropertyInfo property in typeof(GeneralSettingsViewModel).GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                Assert.Null(Record.Exception(() => property.GetValue(vm)));
            }
        }

        [Fact]
        public void TheMouseSpeedDisplay_FollowsTheSensitivityAndTheOffset()
        {
            var service = TestGlobals.ProfileSettingsServiceInstance;
            var vm = ProfileSettingsViewModelTestFactory.Create(0).General;
            int sensitivity = service.ButtonMouseInfos[0].buttonSensitivity;
            double offset = service.ButtonMouseInfos[0].mouseVelocityOffset;
            try
            {
                vm.ButtonMouseSensitivity = 30;
                Assert.Equal(30, service.ButtonMouseInfos[0].buttonSensitivity);
                Assert.Equal(30 * Mapping.MOUSESPEEDFACTOR, vm.OutputMouseSpeed);

                vm.ButtonMouseOffset = 50;
                Assert.Equal(0.5, service.ButtonMouseInfos[0].mouseVelocityOffset, 6);
                Assert.Equal(0.5 * vm.OutputMouseSpeed, vm.MouseOffsetSpeed, 6);
            }
            finally
            {
                service.ButtonMouseInfos[0].ButtonSensitivity = sensitivity;
                service.ButtonMouseInfos[0].mouseVelocityOffset = offset;
            }
        }

        [Fact]
        public void TheValues_AreReadFromAndWrittenToTheSettingsService()
        {
            var service = TestGlobals.ProfileSettingsServiceInstance;
            var vm = ProfileSettingsViewModelTestFactory.Create(0).General;
            int idle = service.IdleDisconnectTimeout[0];
            int poll = service.BTPollRate[0];
            string program = service.LaunchProgram[0];
            bool toggle = service.EnableTouchToggle[0];
            try
            {
                // 無操作での切断: 分で入れると、設定は秒で持つ。有効にすると既定の時間になる
                vm.IdleDisconnectExists = true;
                Assert.Equal(BackingStore.DEFAULT_ENABLE_IDLE_DISCONN_MINS * 60, service.IdleDisconnectTimeout[0]);
                vm.IdleDisconnect = 7;
                Assert.Equal(420, service.IdleDisconnectTimeout[0]);
                vm.IdleDisconnectExists = false;
                Assert.Equal(0, service.IdleDisconnectTimeout[0]);

                vm.TempBTPollRateIndex = 3;
                Assert.Equal(3, service.BTPollRate[0]);

                vm.EnableTouchpadToggle = !toggle;
                Assert.Equal(!toggle, service.EnableTouchToggle[0]);

                // プログラムの同時起動: 設定すると関連する表示用の項目が再通知される
                var names = new List<string>();
                vm.PropertyChanged += (s, e) => names.Add(e.PropertyName);
                vm.UpdateLaunchProgram(@"C:\Windows\notepad.exe");
                Assert.True(vm.LaunchProgramExists);
                Assert.Equal("notepad", vm.LaunchProgramName);
                Assert.Contains(nameof(GeneralSettingsViewModel.LaunchProgramIcon), names);
                vm.LaunchProgramExists = false;
                Assert.Equal(string.Empty, service.LaunchProgram[0]);
                Assert.Equal("Browse", vm.LaunchProgramName);
            }
            finally
            {
                service.IdleDisconnectTimeout[0] = idle;
                service.BTPollRate[0] = poll;
                service.LaunchProgram[0] = program;
                service.EnableTouchToggle[0] = toggle;
            }
        }

        [Fact]
        public void TheSlotSettings_ComeFromTheServiceEntryPoint_AndTheOldMembersAreGoneFromTheEditorViewModel()
        {
            var service = TestGlobals.ProfileSettingsServiceInstance;
            Assert.Same(service.ButtonMouseInfos[3], service.ForSlot(3).General.ButtonMouse);

            // 一般項目は、ProfileSettingsViewModel から General（サブ ViewModel）へ移った
            string[] moved = typeof(GeneralSettingsViewModel).GetProperties(BindingFlags.Public | BindingFlags.Instance)
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
                var vm = ProfileSettingsViewModelTestFactory.Create(0).General;
                var theme = (System.Windows.ResourceDictionary)System.Windows.Application.LoadComponent(
                    new Uri("/DS4Windows;component/UI/Themes/DefaultTheme.xaml", UriKind.Relative));
                System.Windows.Application.Current.Resources.MergedDictionaries.Add(theme);
                try
                {
                    var panel = new DS4Windows.UI.GeneralSettingsPanel();
                    panel.DataContext = vm;

                    // チェックの押下は、パネルの外へ知らせる
                    int clicks = 0;
                    panel.UseControllerReadoutClick += (s, e) => clicks++;
                    var check = (System.Windows.Controls.CheckBox)panel.FindName("useControllerReadoutCk");
                    check.RaiseEvent(new System.Windows.RoutedEventArgs(System.Windows.Controls.Primitives.ButtonBase.ClickEvent));
                    Assert.Equal(1, clicks);

                    panel.Measure(new System.Windows.Size(1000, double.PositiveInfinity));
                    panel.Arrange(new System.Windows.Rect(0, 0, 1000, panel.DesiredSize.Height));
                    panel.UpdateLayout();

                    Assert.True(panel.DesiredSize.Height > 0);
                    Assert.NotNull(panel.UseControllerUD);
                }
                finally
                {
                    System.Windows.Application.Current.Resources.MergedDictionaries.Remove(theme);
                }
            });
        }
    }
}
