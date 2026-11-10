using System;
using System.Windows;
using System.Windows.Media;
using Xunit;
using DS4Windows.Core.Model;
using DS4Windows.Core.Settings;
using DS4Windows.Input;
using DS4Windows.UI.ViewModels;

namespace DS4WindowsTests
{
    /// <summary>
    /// Phase6-Step7b-2: マクロ記録画面の ViewModel（`RecordBoxViewModel`）が、設定の読み書き先
    /// （`DeviceNum`＝編集スロット）とは別に実機のスロット番号 `targetDevice` を持ち、
    /// (a) 記録中のタッチパッド Passthru を実機のスロットにだけ設定して、`RevertControlsSettings` で元に戻すこと、
    /// (b) ライトバー強制色のプレビューが実機にだけ効くこと、を固定する。
    /// 編集スロットには、Step7b-3 以降と同じ `Global.TEST_PROFILE_INDEX`（8）を渡す。
    /// 設定は独立した `BackingStore` に対して操作し、`Global.store` は変えない。
    /// `DS4LightBar` の静的配列は、テストの前後で保存・復元する（Phase6-Status.md §6.4-1）。
    /// </summary>
    public class RecordBoxViewModelTargetDeviceTests
    {
        // CURRENT_DS4_CONTROLLER_LIMIT は環境により 4 または 8 のため、どちらでも範囲内の 1 を使う
        private const int TargetSlot = 1;

        static RecordBoxViewModelTargetDeviceTests()
        {
            if (string.IsNullOrEmpty(TestGlobals.AppDataPath))
            {
                TestGlobals.AppDataPath = AppContext.BaseDirectory;
            }

            if (Application.Current == null)
            {
                try
                {
                    _ = new Application();
                }
                catch { }
            }

            if (Application.ResourceAssembly == null)
            {
                try
                {
                    Application.ResourceAssembly = typeof(DS4Windows.Composition.AppHost).Assembly;
                }
                catch { }
            }

            // RecordBoxViewModel のコンストラクタ（MacroStepItem.CacheImgLocations）が参照するリソースキー。
            // PatternCViewModelTests と同じ準備（実際の画像は不要）
            if (Application.Current != null)
            {
                string[] resourceKeys = new string[]
                {
                    "KeyDownImg", "KeyUpImg", "KeyWaitImg", "KeyHoldImg",
                    "ClockImg", "MouseImg", "CustomKeyImg", "TouchpadImg",
                    "GyroImg", "WheelImg", "UnboundImg", "WaitImg", "HoldImg",
                    "LeftImg", "RightImg", "MiddleImg", "Clock", "Mouse", "CustomKey"
                };

                foreach (var key in resourceKeys)
                {
                    if (!Application.Current.Resources.Contains(key))
                    {
                        Application.Current.Resources[key] = "";
                    }
                }
            }
        }

        /// <summary>DS4LightBar の強制色関連の静的配列を保存・復元する。</summary>
        private sealed class LightBarStateScope : IDisposable
        {
            private readonly bool[] forcelight = (bool[])TestLightbar.State.forcelight.Clone();
            private readonly DS4Color[] forcedColor = (DS4Color[])TestLightbar.State.forcedColor.Clone();
            private readonly byte[] forcedFlash = (byte[])TestLightbar.State.forcedFlash.Clone();

            public LightBarStateScope()
            {
                for (int i = 0; i < TestLightbar.State.forcelight.Length; i++)
                {
                    TestLightbar.State.forcelight[i] = false;
                    TestLightbar.State.forcedColor[i] = new DS4Color(0, 0, 0);
                    TestLightbar.State.forcedFlash[i] = 0;
                }
            }

            public void Dispose()
            {
                Array.Copy(forcelight, TestLightbar.State.forcelight, forcelight.Length);
                Array.Copy(forcedColor, TestLightbar.State.forcedColor, forcedColor.Length);
                Array.Copy(forcedFlash, TestLightbar.State.forcedFlash, forcedFlash.Length);
            }
        }

        private static ProfileSettingsService CreateIsolatedSettings()
        {
            var settings = TestRepositoryFactory.CreateProfileSettingsService(new DS4Windows.Core.Settings.BackingStore(new DS4Windows.Core.Platform.EnvironmentService()));
            for (int i = 0; i < DS4Windows.Core.Settings.ControllerLimits.TestProfileItemCount; i++)
            {
                settings.ForSlot(i).Touchpad.OutputMode = TouchpadOutMode.Mouse;
            }
            return settings;
        }

        private static RecordBoxViewModel CreateViewModel(ProfileSettingsService settings, int targetDevice)
        {
            return new RecordBoxViewModel(TestLightbar.State, DS4Windows.Core.Settings.ControllerLimits.TestProfileIndex, new DS4ControlSettings(DS4Controls.Cross),
                true, settings, new DS4Windows.Core.Platform.EnvironmentService(), null, targetDevice);
        }

        [Fact]
        public void TouchpadPassthru_IsAppliedToTargetDeviceOnly_AndReverted()
        {
            var settings = CreateIsolatedSettings();

            var vm = CreateViewModel(settings, TargetSlot);

            Assert.Equal(DS4Windows.Core.Settings.ControllerLimits.TestProfileIndex, vm.DeviceNum);
            Assert.Equal(TargetSlot, vm.TargetDevice);
            Assert.Equal(TouchpadOutMode.Passthru, settings.ForSlot(TargetSlot).Touchpad.OutputMode);
            // 編集スロットとほかのスロットの設定は変えない
            for (int i = 0; i < DS4Windows.Core.Settings.ControllerLimits.TestProfileItemCount; i++)
            {
                if (i == TargetSlot) continue;
                Assert.Equal(TouchpadOutMode.Mouse, settings.ForSlot(i).Touchpad.OutputMode);
            }

            vm.RevertControlsSettings();
            Assert.Equal(TouchpadOutMode.Mouse, settings.ForSlot(TargetSlot).Touchpad.OutputMode);

            // Save と Cancel の両方から呼ばれても、2 回目で値を壊さない
            vm.RevertControlsSettings();
            Assert.Equal(TouchpadOutMode.Mouse, settings.ForSlot(TargetSlot).Touchpad.OutputMode);
        }

        [Fact]
        public void WithoutTargetDevice_NoTouchpadModeIsChanged()
        {
            var settings = CreateIsolatedSettings();

            var vm = CreateViewModel(settings, -1);
            Assert.Equal(-1, vm.TargetDevice);
            Assert.All(System.Linq.Enumerable.Range(0, DS4Windows.Core.Settings.ControllerLimits.TestProfileItemCount), i => Assert.Equal(TouchpadOutMode.Mouse, settings.ForSlot(i).Touchpad.OutputMode));

            vm.RevertControlsSettings();
            Assert.All(System.Linq.Enumerable.Range(0, DS4Windows.Core.Settings.ControllerLimits.TestProfileItemCount), i => Assert.Equal(TouchpadOutMode.Mouse, settings.ForSlot(i).Touchpad.OutputMode));
        }

        [Fact]
        public void TargetDeviceIsOptional_DefaultsToNoTargetDevice()
        {
            var settings = CreateIsolatedSettings();

            var vm = new RecordBoxViewModel(TestLightbar.State, DS4Windows.Core.Settings.ControllerLimits.TestProfileIndex, new DS4ControlSettings(DS4Controls.Cross),
                true, settings, new DS4Windows.Core.Platform.EnvironmentService(), null);

            Assert.Equal(-1, vm.TargetDevice);
            Assert.All(System.Linq.Enumerable.Range(0, DS4Windows.Core.Settings.ControllerLimits.TestProfileItemCount), i => Assert.Equal(TouchpadOutMode.Mouse, settings.ForSlot(i).Touchpad.OutputMode));
        }

        [Fact]
        public void ForcedColorPreview_AffectsOnlyTargetDevice()
        {
            using var scope = new LightBarStateScope();
            var settings = CreateIsolatedSettings();

            var vm = CreateViewModel(settings, TargetSlot);

            vm.StartForcedColor(Color.FromRgb(10, 20, 30));
            Assert.True(TestLightbar.State.forcelight[TargetSlot]);
            Assert.Equal(new DS4Color(10, 20, 30), TestLightbar.State.forcedColor[TargetSlot]);

            vm.UpdateForcedColor(Color.FromRgb(40, 50, 60));
            Assert.Equal(new DS4Color(40, 50, 60), TestLightbar.State.forcedColor[TargetSlot]);

            for (int i = 0; i < TestLightbar.State.forcelight.Length; i++)
            {
                if (i == TargetSlot) continue;
                Assert.False(TestLightbar.State.forcelight[i], $"slot {i} の forcelight が変わった");
            }

            vm.EndForcedColor();
            Assert.False(TestLightbar.State.forcelight[TargetSlot]);
            vm.RevertControlsSettings();
        }

        [Fact]
        public void ForcedColorPreview_WithoutTargetDevice_DoesNothing()
        {
            using var scope = new LightBarStateScope();
            var settings = CreateIsolatedSettings();

            var vm = CreateViewModel(settings, -1);

            vm.StartForcedColor(Colors.Red);
            vm.UpdateForcedColor(Colors.Blue);
            vm.EndForcedColor();
            Assert.All(TestLightbar.State.forcelight, b => Assert.False(b));
            Assert.All(TestLightbar.State.forcedColor, c => Assert.Equal(new DS4Color(0, 0, 0), c));
        }
    }
}
