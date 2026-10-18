using System;
using DS4Windows;
using Xunit;

namespace DS4WindowsTests
{
    /// <summary>
    /// Phase8-Step1 W3（Phase8-Step1-Plan.md 項目 B-6）: マクロ種別の特殊アクション（<c>MapCustomAction</c> の Macro 分岐）の特性化テスト。
    /// マクロの再生そのもの（キーの送出の時系列）は固定せず、トリガーの成立・解除と、<c>BeingTriggered</c>、<c>firstTouch</c> の遷移だけを固定する。
    ///  - 通常（押している間ではなく、押した瞬間に 1 回）: Fresh-press ガード中は実行しない。成立で <c>BeingTriggered</c>、離すと解除。
    ///  - <c>RunOnRelease</c>（<c>pressRelease</c>）: 押したときは「準備」（<c>firstTouch = true</c>）だけで実行せず、離したときに 1 回実行する。
    /// 台帳: <c>Phase8-Step1-Trigger-Spec-Ledger.md</c> §5（Macro）。マクロの二重実行防止（K3）は <c>Phase7Step9W3MacroDispatchGuardTests</c> が固定している。
    /// </summary>
    [Collection("ServiceProviderHolder")]
    public class Phase8Step1MacroBranchCharacterizationTests
    {
        private static SpecialAction AddMacro(SpecialActionTestScope t, string extras)
        {
            var sa = t.Add(new SpecialAction("p8s1_macro_" + Guid.NewGuid().ToString("N"), "L1", "Macro", "65", 0, extras));
            ActionInstanceState state = t.StateOf(sa);
            state.RequiresFreshPressAfterReset = false;
            state.IsExecuting = false;
            return sa;
        }

        private static DS4State HeldL1()
        {
            DS4State s = SpecialActionTestScope.Neutral();
            s.L1 = true;
            return s;
        }

        [Fact]
        public void Macro_WhileTheFreshPressGuardIsOn_DoesNotTrigger()
        {
            using var t = new SpecialActionTestScope();
            SpecialAction sa = AddMacro(t, string.Empty);
            t.StateOf(sa).RequiresFreshPressAfterReset = true;

            t.RunFrame(HeldL1());

            Assert.False(ActionManager.IsBeingTriggered(sa, t.Device));
        }

        [Fact]
        public void Macro_Press_TriggersOnceWhenEstablished_AndClearsWhenReleased()
        {
            using var t = new SpecialActionTestScope();
            SpecialAction sa = AddMacro(t, string.Empty);

            t.RunFrame(HeldL1());
            Assert.True(ActionManager.IsBeingTriggered(sa, t.Device));

            t.RunFrame(HeldL1()); // 保持中も、成立のまま（再実行しない）
            Assert.True(ActionManager.IsBeingTriggered(sa, t.Device));

            t.RunFrame(SpecialActionTestScope.Neutral()); // 離す
            Assert.False(ActionManager.IsBeingTriggered(sa, t.Device));
        }

        [Fact]
        public void Macro_RunOnRelease_ArmsOnPress_AndFiresOnlyWhenTheTriggerIsReleased()
        {
            using var t = new SpecialActionTestScope();
            SpecialAction sa = AddMacro(t, "RunOnRelease");
            Assert.True(sa.pressRelease);
            Assert.False(sa.firstTouch);

            t.RunFrame(SpecialActionTestScope.Neutral()); // 押す前は、何も起きない
            Assert.False(ActionManager.IsBeingTriggered(sa, t.Device));
            Assert.False(sa.firstTouch);

            t.RunFrame(HeldL1()); // 押したとき: 準備（firstTouch）だけで、実行しない
            Assert.True(sa.firstTouch);
            Assert.False(ActionManager.IsBeingTriggered(sa, t.Device));

            t.RunFrame(SpecialActionTestScope.Neutral()); // 離したとき: 1 回実行（成立を反転して扱う）
            Assert.False(sa.firstTouch);
            Assert.True(ActionManager.IsBeingTriggered(sa, t.Device));
        }
    }
}
