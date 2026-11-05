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
    /// Phase6-Step7b-2: ボタン設定画面の ViewModel（`BindingWindowViewModel`）が、設定の読み書き先
    /// （`DeviceNum`＝編集スロット）とは別に実機のスロット番号 `targetDevice` を持ち、ライトバー強制色の
    /// プレビューが `targetDevice` にだけ効くことを固定する。
    /// Step7b-3 以降、編集スロットは常に `Global.TEST_PROFILE_INDEX`（8）になるため、編集スロットに 8 を渡して確認する。
    /// `DS4LightBar` の静的配列は、テストの前後で保存・復元する（Phase6-Status.md §6.4-1）。
    /// </summary>
    public class BindingWindowViewModelTargetDeviceTests
    {
        // CURRENT_DS4_CONTROLLER_LIMIT は環境により 4 または 8 のため、どちらでも範囲内の 1 を使う
        private const int TargetSlot = 1;

        static BindingWindowViewModelTargetDeviceTests()
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

        private static BindingWindowViewModel CreateViewModel(int targetDevice)
        {
            return new BindingWindowViewModel(TestLightbar.State, DS4Windows.Core.Settings.ControllerLimits.TestProfileIndex,
                new DS4ControlSettings(DS4Controls.Cross),
                TestRepositoryFactory.CreateProfileSettingsService(), TestRepositoryFactory.CreateOutputSlotService(), new DS4Windows.Core.Platform.EnvironmentService(), targetDevice);
        }

        [Fact]
        public void TargetDevice_IsKeptSeparateFromEditSlot()
        {
            var vm = CreateViewModel(TargetSlot);

            Assert.Equal(DS4Windows.Core.Settings.ControllerLimits.TestProfileIndex, vm.DeviceNum);
            Assert.Equal(TargetSlot, vm.TargetDevice);
            Assert.True(vm.HasTargetDevice);
        }

        [Fact]
        public void TargetDeviceIsOptional_DefaultsToNoTargetDevice()
        {
            var vm = new BindingWindowViewModel(TestLightbar.State, DS4Windows.Core.Settings.ControllerLimits.TestProfileIndex, new DS4ControlSettings(DS4Controls.Cross),
                TestRepositoryFactory.CreateProfileSettingsService(), TestRepositoryFactory.CreateOutputSlotService(), new DS4Windows.Core.Platform.EnvironmentService());

            Assert.Equal(-1, vm.TargetDevice);
            Assert.False(vm.HasTargetDevice);
        }

        [Fact]
        public void OutOfRangeTargetDevice_IsTreatedAsNoTargetDevice()
        {
            using var scope = new LightBarStateScope();

            var vm = CreateViewModel(new DS4Windows.Core.Platform.EnvironmentService().ControllerSlotLimit);

            Assert.False(vm.HasTargetDevice);
            // 範囲外の添字で DS4LightBar の配列に触れない（例外にならず、何も変えない）
            vm.StartForcedColor(Colors.Red);
            vm.UpdateForcedColor(Colors.Blue);
            vm.EndForcedColor();
            Assert.All(TestLightbar.State.forcelight, b => Assert.False(b));
        }

        [Fact]
        public void ForcedColorPreview_AffectsOnlyTargetDevice()
        {
            using var scope = new LightBarStateScope();

            var vm = CreateViewModel(TargetSlot);

            vm.StartForcedColor(Color.FromRgb(10, 20, 30));
            Assert.True(TestLightbar.State.forcelight[TargetSlot]);
            Assert.Equal(new DS4Color(10, 20, 30), TestLightbar.State.forcedColor[TargetSlot]);
            Assert.Equal((byte)0, TestLightbar.State.forcedFlash[TargetSlot]);

            vm.UpdateForcedColor(Color.FromRgb(40, 50, 60));
            Assert.Equal(new DS4Color(40, 50, 60), TestLightbar.State.forcedColor[TargetSlot]);

            for (int i = 0; i < TestLightbar.State.forcelight.Length; i++)
            {
                if (i == TargetSlot) continue;
                Assert.False(TestLightbar.State.forcelight[i], $"slot {i} の forcelight が変わった");
            }

            vm.EndForcedColor();
            Assert.False(TestLightbar.State.forcelight[TargetSlot]);
            Assert.Equal(new DS4Color(0, 0, 0), TestLightbar.State.forcedColor[TargetSlot]);
        }

        [Fact]
        public void ForcedColorPreview_WithoutTargetDevice_DoesNothing()
        {
            using var scope = new LightBarStateScope();

            var vm = CreateViewModel(-1);

            vm.StartForcedColor(Colors.Red);
            vm.UpdateForcedColor(Colors.Blue);
            vm.EndForcedColor();
            Assert.All(TestLightbar.State.forcelight, b => Assert.False(b));
            Assert.All(TestLightbar.State.forcedColor, c => Assert.Equal(new DS4Color(0, 0, 0), c));
        }
    }
}
