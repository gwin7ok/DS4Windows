using System;
using DS4Windows;
using DS4WinWPF.DS4Forms.ViewModels;
using Xunit;

namespace DS4WindowsTests
{
    /// <summary>
    /// Phase7b（既存の不具合。上流の元から）: L2／R2 のデッドゾーンを上下ボタンで 0.10 ずつ増減すると、0.20 → 0.29 のように 0.09 しか変わらない所があった。
    /// 原因: 保存値（byte）への変換が切り捨て（<c>(byte)(value * 255.0)</c>）で、読み出しが丸められておらず、1 回ごとに誤差が溜まる。
    /// 是正: 読み出しは小数 2 桁へ丸め、設定は四捨五入する（スティックの <c>LSDeadZone</c> と同じ形）。
    /// 最大ゾーン・アンチデッドゾーンの百分率への変換も、切り捨て（0.29 × 100 ＝ 28.999… → 28）から四捨五入へ。
    /// 作業スロット（<c>ControllerLimits.TestProfileIndex</c>）を使い、終了時に値を戻す。
    /// </summary>
    public class Phase7bTriggerDeadZoneRoundingTests
    {
        private static ProfileSettingsViewModel NewViewModel() =>
            ProfileSettingsViewModelTestFactory.Create(ControllerLimits.TestProfileIndex);

        [Fact]
        public void DeadZone_TenthSteps_ReachEveryTenth_WithoutDrift()
        {
            var vm = NewViewModel();
            int slot = ControllerLimits.TestProfileIndex;
            var settings = TestGlobals.ProfileSettingsServiceInstance;
            try
            {
                foreach (bool isL2 in new[] { true, false })
                {
                    vm.GetType().GetProperty(isL2 ? "L2DeadZone" : "R2DeadZone").SetValue(vm, 0.0);
                    for (int i = 1; i <= 10; i++)
                    {
                        // 上下ボタン（Increment=0.1）の動き: 現在の表示値に 0.1 を足して設定し、次の表示値を読む
                        double current = (double)vm.GetType().GetProperty(isL2 ? "L2DeadZone" : "R2DeadZone").GetValue(vm);
                        double next = Math.Round(current + 0.1, 10);
                        vm.GetType().GetProperty(isL2 ? "L2DeadZone" : "R2DeadZone").SetValue(vm, Math.Min(next, 1.0));

                        double shown = (double)vm.GetType().GetProperty(isL2 ? "L2DeadZone" : "R2DeadZone").GetValue(vm);
                        Assert.Equal(Math.Round(i / 10.0, 2), shown);
                    }
                }
            }
            finally
            {
                settings.L2ModInfo[slot].deadZone = 0;
                settings.R2ModInfo[slot].deadZone = 0;
            }
        }

        [Fact]
        public void DeadZone_StoresTheRoundedByteValue_AndKeepsTheRangeBounds()
        {
            var vm = NewViewModel();
            int slot = ControllerLimits.TestProfileIndex;
            var settings = TestGlobals.ProfileSettingsServiceInstance;
            try
            {
                vm.L2DeadZone = 0.5;   // 127.5 → 128（四捨五入。旧は 127 に切り捨て）
                Assert.Equal(128, settings.L2ModInfo[slot].deadZone);
                vm.L2DeadZone = 1.0;
                Assert.Equal(255, settings.L2ModInfo[slot].deadZone);
                vm.L2DeadZone = 1.5;   // 範囲外でも、byte が溢れない
                Assert.Equal(255, settings.L2ModInfo[slot].deadZone);
                vm.R2DeadZone = -0.5;
                Assert.Equal(0, settings.R2ModInfo[slot].deadZone);
            }
            finally
            {
                settings.L2ModInfo[slot].deadZone = 0;
                settings.R2ModInfo[slot].deadZone = 0;
            }
        }

        [Fact]
        public void PercentValues_AreRoundedNotTruncated()
        {
            var vm = NewViewModel();
            int slot = ControllerLimits.TestProfileIndex;
            var settings = TestGlobals.ProfileSettingsServiceInstance;
            try
            {
                // 0.29 * 100.0 ＝ 28.999999999999996。旧は (int) の切り捨てで 28 になった
                vm.L2MaxZone = 0.29; vm.R2MaxZone = 0.29;
                vm.L2AntiDeadZone = 0.29; vm.R2AntiDeadZone = 0.29;
                vm.LSMaxZone = 0.29; vm.RSMaxZone = 0.29;
                vm.LSAntiDeadZone = 0.29; vm.RSAntiDeadZone = 0.29;

                Assert.Equal(29, settings.L2ModInfo[slot].maxZone);
                Assert.Equal(29, settings.R2ModInfo[slot].maxZone);
                Assert.Equal(29, settings.L2ModInfo[slot].antiDeadZone);
                Assert.Equal(29, settings.R2ModInfo[slot].antiDeadZone);
                Assert.Equal(29, settings.LSModInfo[slot].maxZone);
                Assert.Equal(29, settings.RSModInfo[slot].maxZone);
                Assert.Equal(29, settings.LSModInfo[slot].antiDeadZone);
                Assert.Equal(29, settings.RSModInfo[slot].antiDeadZone);
            }
            finally
            {
                settings.L2ModInfo[slot].Reset(); settings.R2ModInfo[slot].Reset();
                settings.LSModInfo[slot].Reset(); settings.RSModInfo[slot].Reset();
            }
        }
    }
}
