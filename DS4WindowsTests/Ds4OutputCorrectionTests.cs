using System;
using System.Windows;
using Xunit;
using DS4Windows.Core.Model;
using DS4Windows.Core.Platform;
using DS4Windows.Core.Settings;
using DS4Windows.UI.ViewModels;

namespace DS4WindowsTests
{
    /// <summary>
    /// Phase11 W4（Issue #4 の 1）: DS4 の出力でしか成り立たない設定が ON のとき、出力の種類を DS4 へ補正する
    /// （<see cref="ProfileSettingsViewModel.CorrectOutputTypeToDs4"/>）。
    /// 2026-09-14 の保存・適用の統合（<c>9da60b85</c>）で失われた、旧 <c>ProfileEditor</c> の補正の動きの特性化:
    /// <code>
    /// 旧 SaveBtn_Click（9da60b85 の親）   : if (UseDs3PitchRollSim) TempControllerIndex = 1;   ← 無条件。GUI ログなし。保存だけ（適用には無かった）
    /// 旧 SetLateProperties（同）          : if (EnableOutputDataToDS4 &amp;&amp; TempConType != DS4) { TempControllerIndex = 1; LogToGui(...); }
    ///                                       ← 保存・適用の両方が通る。(a) のあとなので、擬似が ON なら、ここは動かない
    /// </code>
    /// 復元後は、保存・適用の合流点（<c>ExecuteSaveOrApply</c>）から、両方の補正を 1 つの判定で行う。
    /// OFF のときは何もしない（DS4 から X360 へ戻すことはしない）。
    /// </summary>
    public class Ds4OutputCorrectionTests
    {
        private const int Device = 0;

        static Ds4OutputCorrectionTests()
        {
            if (string.IsNullOrEmpty(TestGlobals.AppDataPath))
            {
                TestGlobals.AppDataPath = AppContext.BaseDirectory;
            }

            if (Application.Current == null)
            {
                try { _ = new Application(); }
                catch { }
            }
        }

        /// <summary>他のテストと設定を共有しないよう、専用の BackingStore で作る</summary>
        private static (ProfileSettingsViewModel Vm, ProfileSettingsService Service) Create(OutContType type, bool ds3Sim, bool enableDs4Data)
        {
            ProfileSettingsService service = TestRepositoryFactory.CreateProfileSettingsService(new BackingStore(new EnvironmentService()));
            service.OutContType[Device] = type;
            service.UseDs3PitchRollSim = ds3Sim;
            service.EnableOutputDataToDS4[Device] = enableDs4Data;
            return (ProfileSettingsViewModelTestFactory.Create(Device, service), service);
        }

        [Theory]
        // 擬似が ON: 出力が DS4 でなければ DS4 へ（旧 SaveBtn_Click）
        [InlineData(OutContType.X360, true, false, Ds4OutputCorrectionReason.Ds3PitchRollSim, OutContType.DS4)]
        [InlineData(OutContType.DS4, true, false, Ds4OutputCorrectionReason.None, OutContType.DS4)]
        // Phase11 W6c（K-W6-12）: EnableOutputDataToDS4 が ON でも、出力の種類は強制しない（旧 SetLateProperties の (c)-2 を廃止）
        [InlineData(OutContType.X360, false, true, Ds4OutputCorrectionReason.None, OutContType.X360)]
        [InlineData(OutContType.DS4, false, true, Ds4OutputCorrectionReason.None, OutContType.DS4)]
        // 両方 ON: (a) が DS4 にする（理由は擬似）
        [InlineData(OutContType.X360, true, true, Ds4OutputCorrectionReason.Ds3PitchRollSim, OutContType.DS4)]
        // 両方 OFF: 何もしない。DS4 を X360 へ戻すこともしない
        [InlineData(OutContType.X360, false, false, Ds4OutputCorrectionReason.None, OutContType.X360)]
        [InlineData(OutContType.DS4, false, false, Ds4OutputCorrectionReason.None, OutContType.DS4)]
        public void CorrectOutputTypeToDs4_FollowsTheOldSaveAndSetLatePropertiesRules(
            OutContType start, bool ds3Sim, bool enableDs4Data, Ds4OutputCorrectionReason expectedReason, OutContType expectedType)
        {
            var (vm, service) = Create(start, ds3Sim, enableDs4Data);

            Ds4OutputCorrectionReason reason = vm.CorrectOutputTypeToDs4();

            Assert.Equal(expectedReason, reason);
            // 保存される値（OutContType）と、画面の選択（TempControllerIndex）が、どちらも補正後の種類
            Assert.Equal(expectedType, service.OutContType[Device]);
            Assert.Equal(expectedType == OutContType.DS4 ? 1 : 0, vm.Output.ControllerTypeIndex);
            Assert.Equal(expectedType == OutContType.DS4 ? 1 : 0, vm.Output.TempControllerIndex);
            Assert.Equal(expectedType, vm.Output.TempConType);
        }

        [Fact]
        public void CorrectOutputTypeToDs4_UsesTheSavedValueAsTheReference_NotOnlyTheScreenSelection()
        {
            // 画面の選択（TempControllerIndex）が DS4 でも、保存される値（OutContType）が X360 にずれていたら、DS4 へそろえる
            var (vm, service) = Create(OutContType.DS4, ds3Sim: true, enableDs4Data: false);
            service.OutContType[Device] = OutContType.X360;
            Assert.Equal(1, vm.Output.TempControllerIndex);

            Ds4OutputCorrectionReason reason = vm.CorrectOutputTypeToDs4();

            Assert.Equal(Ds4OutputCorrectionReason.Ds3PitchRollSim, reason);
            Assert.Equal(OutContType.DS4, service.OutContType[Device]);
        }

        [Fact]
        public void CorrectOutputTypeToDs4_IsIdempotent()
        {
            var (vm, service) = Create(OutContType.X360, ds3Sim: true, enableDs4Data: false);

            Assert.Equal(Ds4OutputCorrectionReason.Ds3PitchRollSim, vm.CorrectOutputTypeToDs4());
            Assert.Equal(Ds4OutputCorrectionReason.None, vm.CorrectOutputTypeToDs4());
            Assert.Equal(OutContType.DS4, service.OutContType[Device]);
        }

        [Fact]
        public void HasUseDs3PitchRollSimChanged_IsSetByAChange_AndClearedWhenTheRestartIsRequested()
        {
            var (vm, _) = Create(OutContType.X360, ds3Sim: false, enableDs4Data: false);
            Assert.False(vm.HasUseDs3PitchRollSimChanged);

            vm.UseDs3PitchRollSim = true;
            Assert.True(vm.HasUseDs3PitchRollSimChanged);

            // 再起動を求めたあとは、保存・適用をくり返しても、再起動をくり返さない
            vm.AcknowledgeUseDs3PitchRollSimChange();
            Assert.False(vm.HasUseDs3PitchRollSimChanged);
        }

        [Fact]
        public void SettingTheSameUseDs3PitchRollSimValue_IsNotAChange()
        {
            var (vm, _) = Create(OutContType.X360, ds3Sim: true, enableDs4Data: false);

            vm.UseDs3PitchRollSim = true;

            Assert.False(vm.HasUseDs3PitchRollSimChanged);
        }
    }
}
