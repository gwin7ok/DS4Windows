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
    /// 「タッチパッド」タブ（<c>TouchpadSettingsPanel</c>）と <see cref="TouchpadSettingsViewModel"/>、
    /// 設定サービスの領域の窓口（<see cref="TouchpadSlotSettings"/>）の守り。
    /// Binding の名前の誤りはビルドで見つからないため、パネルの XAML の Binding を、ViewModel の公開プロパティと突き合わせる。
    /// </summary>
    // パネルのテストは Application のリソース（テーマ）を一時的に足すため、共有のホストの状態を使う他のテストとは並列に走らせない
    [Collection(SharedHostStateCollection.Name)]
    public class TouchpadSettingsViewModelTests
    {
        private static readonly Regex BindingPath = new Regex(@"\{Binding\s+(?:Path=)?(?<path>[A-Za-z_][A-Za-z0-9_]*)", RegexOptions.Compiled);

        [Fact]
        public void EveryBindingInThePanel_NamesAPublicPropertyOfTheViewModel()
        {
            var xaml = XamlTestHelper.Load("DS4Windows/UI/TouchpadSettingsPanel.xaml");
            if (xaml == null) return;

            HashSet<string> properties = typeof(TouchpadSettingsViewModel).GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Select(p => p.Name).ToHashSet();
            // MappingName は、割り当ての表示用のラベル（swipe*Lb）が持つ DataContext（MappedControl）の項目
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
            var vm = ProfileSettingsViewModelTestFactory.Create(0).Touchpad;
            foreach (PropertyInfo property in typeof(TouchpadSettingsViewModel).GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                Assert.Null(Record.Exception(() => property.GetValue(vm)));
            }
        }

        [Fact]
        public void TheValues_AreReadFromAndWrittenToTheSettingsService()
        {
            var service = TestGlobals.ProfileSettingsServiceInstance;
            var vm = ProfileSettingsViewModelTestFactory.Create(0).Touchpad;
            byte touchSens = service.TouchSensitivity[0];
            int invert = service.TouchpadInvert[0];
            uint stickInverted = service.TouchMouseStickInf[0].inverted;
            bool lowerRc = service.LowerRCOn[0];
            try
            {
                vm.TouchSens = 55;
                Assert.Equal(55, service.TouchSensitivity[0]);
                // 0 にすると「感度あり」の印が外れる
                var names = new List<string>();
                vm.PropertyChanged += (s, e) => names.Add(e.PropertyName);
                vm.TouchSens = 0;
                Assert.Contains(nameof(TouchpadSettingsViewModel.TouchSenExists), names);
                vm.TouchSenExists = true;
                Assert.Equal(100, service.TouchSensitivity[0]);

                // 反転の選択番号は、保存する値（0, 2, 1, 3）と対応する
                vm.TouchInvertIndex = 1;
                Assert.Equal(2, service.TouchpadInvert[0]);
                Assert.Equal(1, vm.TouchInvertIndex);

                // マウスジョイスティックの反転は 1 つの整数のビット（1＝横、2＝縦）
                vm.TouchMouseStickInvertX = true;
                vm.TouchMouseStickInvertY = true;
                Assert.Equal(3u, service.TouchMouseStickInf[0].inverted & 3u);
                vm.TouchMouseStickInvertX = false;
                Assert.Equal(2u, service.TouchMouseStickInf[0].inverted & 3u);
                Assert.True(vm.TouchMouseStickInvertY);

                vm.LowerRightTouchRMB = !lowerRc;
                Assert.Equal(!lowerRc, service.LowerRCOn[0]);
            }
            finally
            {
                service.TouchSensitivity[0] = touchSens;
                service.TouchpadInvert[0] = invert;
                service.TouchMouseStickInf[0].inverted = stickInverted;
                service.LowerRCOn[0] = lowerRc;
            }
        }

        [Fact]
        public void TheOutputMode_MapsTheIndexBothWays_AndRaisesTheNotification()
        {
            var service = TestGlobals.ProfileSettingsServiceInstance;
            var vm = ProfileSettingsViewModelTestFactory.Create(0).Touchpad;
            TouchpadOutMode original = service.TouchOutMode[0];
            var names = new List<string>();
            vm.PropertyChanged += (s, e) => names.Add(e.PropertyName);
            try
            {
                vm.TouchpadOutputIndex = 2;
                Assert.Equal(TouchpadOutMode.MouseJoystick, service.TouchOutMode[0]);
                Assert.Equal(2, vm.TouchpadOutputIndex);
                Assert.Contains(nameof(TouchpadSettingsViewModel.TouchpadOutputIndex), names);

                vm.TouchpadOutputIndex = 3;
                Assert.Equal(TouchpadOutMode.Passthru, service.TouchOutMode[0]);
            }
            finally
            {
                service.TouchOutMode[0] = original;
            }
        }

        [Fact]
        public void TheSlotSettings_ComeFromTheServiceEntryPoint_AndTheOldMembersAreGoneFromTheEditorViewModel()
        {
            var service = TestGlobals.ProfileSettingsServiceInstance;
            Assert.Same(service.TouchMouseStickInf[3], service.ForSlot(3).Touchpad.MouseStick);
            Assert.Same(service.TouchRelMouse[3], service.ForSlot(3).Touchpad.RelMouse);

            // タッチパッドの項目は、ProfileSettingsViewModel から Touchpad（サブ ViewModel）へ移った
            // （UsingMinViGEm173333 は、複数のタブが同じ判定を使うため、親にも残る）
            string[] moved = typeof(TouchpadSettingsViewModel).GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Select(p => p.Name)
                .Where(n => n != nameof(TouchpadSettingsViewModel.UsingMinViGEm173333))
                .ToArray();
            string[] left = typeof(ProfileSettingsViewModel).GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Select(p => p.Name)
                .Where(n => moved.Contains(n))
                .ToArray();
            Assert.True(left.Length == 0, "ProfileSettingsViewModel に残っている: " + string.Join(", ", left));
        }

        /// <summary>
        /// 実物のパネルを読み込めること（XAML の誤りは、ビルドでは見つからず、実行時にだけ出る）。
        /// 反転を無効にするトリガーのメニューが、保存された値を表示へ反映すること。
        /// </summary>
        [Fact]
        public void ThePanel_LoadsWithTheTheme_AndShowsTheDisInvertTriggers()
        {
            Exception failure = null;
            string step = "start";
            var thread = new System.Threading.Thread(() =>
            {
                try
                {
                    var vm = ProfileSettingsViewModelTestFactory.Create(0).Touchpad;
                    // アプリと同じく、テーマは Application のリソースにある（StaticResource は、読み込みの時点で解決される）
                    var theme = (System.Windows.ResourceDictionary)System.Windows.Application.LoadComponent(
                        new Uri("/DS4Windows;component/UI/Themes/DefaultTheme.xaml", UriKind.Relative));
                    System.Windows.Application.Current.Resources.MergedDictionaries.Add(theme);
                    try
                    {
                        step = "new";
                        var panel = new DS4Windows.UI.TouchpadSettingsPanel();
                        panel.DataContext = vm;

                        step = "populate";
                        panel.PopulateDisInvertMenu(vm);
                        step = "place";
                        // 出力モードごとの置き場所の切り替えで落ちないこと（部品が無くても置き場所の選択はできる）
                        for (int mode = 0; mode <= 3; mode++) panel.PlaceTouchButtonControl(null, mode);
                        step = "measure";
                        panel.Measure(new System.Windows.Size(1000, double.PositiveInfinity));
                        panel.Arrange(new System.Windows.Rect(0, 0, 1000, panel.DesiredSize.Height));
                        panel.UpdateLayout();

                        Assert.True(panel.DesiredSize.Height > 0);
                        Assert.False(string.IsNullOrEmpty(vm.TouchDisInvertString));
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
            Assert.True(thread.Join(30000), "パネルの読み込みが終わらない（最後の段階: " + step + "）");
            if (failure != null) throw new InvalidOperationException("パネルの読み込みに失敗: " + failure, failure);
        }
    }
}
