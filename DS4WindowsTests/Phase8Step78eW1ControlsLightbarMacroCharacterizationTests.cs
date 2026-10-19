using System.Threading;
using DS4Windows;
using DS4WinWPF.DS4Forms.ViewModels;
using Xunit;

namespace DS4WindowsTests
{
    /// <summary>
    /// Phase8-Step7 7.8e-W1（Phase8-Step7-8e-Plan.md §5）: Controls タブの Lightbar マクロ（<c>ProcessControlSettingAction</c> の Lightbar マクロの枝）の特性化。
    /// 旧・新の両経路（<c>InlineData(false)</c>／<c>InlineData(true)</c>）で同じテストを走らせる。W1 の時点では新経路が未実装のため、どちらでも旧経路が処理する。
    /// 判定は、MapCustom の引数の状態ではなく、デバイスの生の前回・現在の状態の変化（<see cref="LightbarHarness"/>）。マクロは背景のタスクで動くため、実際に待つ。
    /// 他の Host 構築テストと同じコレクションで直列に実行する。
    /// </summary>
    [Collection("ServiceProviderHolder")]
    public class Phase8Step78eW1ControlsLightbarMacroCharacterizationTests
    {
        private static LightbarMacroElement Element(byte r, byte g, byte b, uint milliseconds)
            => new LightbarMacroElement(new DS4Color(r, g, b), milliseconds);

        private static bool IsRed(ControlsKeyTestScope t) => t.ForceLight && t.ForcedColor.red == 255 && t.ForcedColor.green == 0;

        private static bool IsGreen(ControlsKeyTestScope t) => t.ForceLight && t.ForcedColor.green == 255 && t.ForcedColor.red == 0;

        private static LightbarHarness Setup(bool migrated, LightbarMacroTrigger trigger, bool cancelCurrent, params LightbarMacroElement[] elements)
        {
            var h = new LightbarHarness(migrated);
            DS4ControlSettings dcs = h.T.MapExtras(DS4Controls.Cross, null);
            ControlsKeyTestScope.SetLightbarMacro(dcs, trigger, cancelCurrent, elements);
            h.T.Publish();
            h.Step(false);
            return h;
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void Press_StartsTheMacroOnTheRisingEdgeOnly(bool migrated)
        {
            using LightbarHarness h = Setup(migrated, LightbarMacroTrigger.Press, false, Element(255, 0, 0, 80), Element(0, 255, 0, 80));

            h.Step(true);
            Assert.True(LightbarHarness.WaitUntil(() => IsRed(h.T), 500));
            Assert.True(LightbarHarness.WaitUntil(() => IsGreen(h.T), 500));
            Assert.True(LightbarHarness.WaitUntil(() => !h.T.ForceLight, 1000));   // 終わると、強制を解除する

            // 押し続けても、再び始まらない。離しても（Press のマクロは）始まらない
            for (int i = 0; i < 20; i++) { h.Step(true); Thread.Sleep(10); }
            Assert.False(h.T.ForceLight);
            h.Step(false);
            Assert.False(LightbarHarness.WaitUntil(() => h.T.ForceLight, 250));
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void Release_StartsTheMacroOnTheFallingEdgeOnly(bool migrated)
        {
            using LightbarHarness h = Setup(migrated, LightbarMacroTrigger.Release, false, Element(255, 0, 0, 80), Element(0, 255, 0, 80));

            h.Step(true);
            Assert.False(LightbarHarness.WaitUntil(() => h.T.ForceLight, 250));   // 押しただけでは始まらない

            h.Step(false);
            Assert.True(LightbarHarness.WaitUntil(() => IsRed(h.T), 500));
            Assert.True(LightbarHarness.WaitUntil(() => !h.T.ForceLight, 1500));
        }

        [Theory]
        [InlineData(false, true)]
        [InlineData(true, true)]
        [InlineData(false, false)]
        [InlineData(true, false)]
        public void ASecondPressWhileRunning_RestartsOnlyWhenCancelCurrentIsOn(bool migrated, bool cancelCurrent)
        {
            using LightbarHarness h = Setup(migrated, LightbarMacroTrigger.Press, cancelCurrent, Element(255, 0, 0, 300), Element(0, 255, 0, 700));

            h.Step(true);
            Assert.True(LightbarHarness.WaitUntil(() => IsRed(h.T), 500));
            Assert.True(LightbarHarness.WaitUntil(() => IsGreen(h.T), 800));   // 緑の段階（700ms 続く）に入った

            h.Step(false);
            h.Step(true);   // 実行中の 2 回目の押下

            bool backToRed = LightbarHarness.WaitUntil(() => IsRed(h.T), 400);
            Assert.Equal(cancelCurrent, backToRed);   // 取り消しありなら、最初からやり直す。なしなら、無視して続ける
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void AControlThatIsNotAButtonInTheList_DoesNothing(bool migrated)
        {
            using var h = new LightbarHarness(migrated);
            DS4ControlSettings dcs = h.T.MapExtras(DS4Controls.L2, null);
            ControlsKeyTestScope.SetLightbarMacro(dcs, LightbarMacroTrigger.Press, false, Element(255, 0, 0, 80));
            h.T.Publish();

            DS4State pressed = ControlsKeyTestScope.Neutral();
            pressed.L2 = 255;
            h.T.Run(ControlsKeyTestScope.Neutral());
            for (int i = 0; i < 10; i++) { h.T.Run(pressed); Thread.Sleep(10); }

            Assert.False(LightbarHarness.WaitUntil(() => h.T.ForceLight, 200));
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void TheJudgementUsesTheDevicesRawStates_NotTheArgumentOfMapCustom(bool migrated)
        {
            using LightbarHarness h = Setup(migrated, LightbarMacroTrigger.Press, false, Element(255, 0, 0, 80));

            // 引数の状態は押下でも、デバイスの生の状態に変化がなければ、始まらない
            h.Step(true, rawCross: false);
            Assert.False(LightbarHarness.WaitUntil(() => h.T.ForceLight, 250));

            // 引数の状態が離されていても、デバイスの生の状態が立ち上がれば、始まる
            h.Step(false, rawCross: true);
            Assert.True(LightbarHarness.WaitUntil(() => IsRed(h.T), 500));
        }
    }
}
