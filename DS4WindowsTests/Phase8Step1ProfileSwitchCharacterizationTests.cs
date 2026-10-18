using System;
using DS4Windows;
using Xunit;

namespace DS4WindowsTests
{
    /// <summary>
    /// Phase8-Step1 W2（Phase8-Step1-Plan.md 項目 B-1・B-2、Q2＝A）: プロファイル切替の特殊アクション（Profile 種別）の、成立 → 切替 → 復帰の特性化テスト。
    /// <see cref="RecordingProfileApplicationService"/> を <c>DefaultProfileSwitcher</c> に差し込み、実際のプロファイルの読込・適用は走らせない。
    /// 固定する挙動:
    ///  - 成立のたびに 1 回だけ、<c>ApplyFromAction</c>（<c>ActionManager</c> 経由）で切り替わる。直接の <c>ApplyProfile</c>（フォールバック、チェーン）は呼ばれない。
    ///  - 「Unload on trigger release」（<c>AutomaticUntrigger</c>）: トリガーを離すと、<c>RestoreFromAction</c> が 1 回だけ呼ばれ、復帰の合図が消える。
    ///  - 解除トリガー（<c>uTrigger</c>）あり: 解除トリガーがすべて押されたときだけ復帰する。
    /// 台帳: <c>Phase8-Step1-Trigger-Spec-Ledger.md</c> §3・§5（Profile）。
    /// </summary>
    [Collection("ServiceProviderHolder")]
    public class Phase8Step1ProfileSwitchCharacterizationTests
    {
        private static SpecialAction AddProfile(SpecialActionTestScope t, string triggers, string extras)
        {
            var sa = t.Add(new SpecialAction("p8s1_profile_" + Guid.NewGuid().ToString("N"), triggers, "Profile", "p8s1-target", 0, extras));
            ActionInstanceState state = t.StateOf(sa);
            state.RequiresFreshPressAfterReset = false;
            state.IsExecuting = false;
            return sa;
        }

        private static DS4State Held(params DS4Controls[] controls)
        {
            DS4State s = SpecialActionTestScope.Neutral();
            foreach (DS4Controls c in controls)
            {
                switch (c)
                {
                    case DS4Controls.L1: s.L1 = true; break;
                    case DS4Controls.R1: s.R1 = true; break;
                    default: throw new ArgumentException("未対応のボタン: " + c);
                }
            }
            return s;
        }

        // ---- 成立 → 切替（B-2: チェーン／フォールバックの経路は使われない）----

        [Fact]
        public void Profile_Established_SwitchesOnceViaApplyFromAction_AndNeverCallsDirectApplyProfile()
        {
            using var t = new SpecialActionTestScope();
            var recorder = t.InstallProfileRecorder();
            AddProfile(t, "L1", "AutomaticUntrigger");

            t.RunFrame(Held(DS4Controls.L1));
            t.RunFrame(Held(DS4Controls.L1)); // 保持中も、再度は切り替えない

            Assert.Single(recorder.ApplyFromActionCalls);
            Assert.Equal(t.Device, recorder.ApplyFromActionCalls[0].Device);
            Assert.Equal("p8s1-target", recorder.ApplyFromActionCalls[0].ProfileName);
            Assert.Empty(recorder.ApplyProfileCalls); // 直接の ApplyProfile（フォールバック、チェーン）は、ActionManager が処理した場合は呼ばれない
        }

        // ---- 「Unload on trigger release」（AutomaticUntrigger）----

        [Fact]
        public void Profile_AutomaticUntrigger_RestoresOnceWhenTheTriggerIsReleased_AndClearsTheUntriggerSignal()
        {
            using var t = new SpecialActionTestScope();
            var recorder = t.InstallProfileRecorder();
            SpecialAction sa = AddProfile(t, "L1", "AutomaticUntrigger");
            int index = t.IndexOf(sa);

            t.RunFrame(Held(DS4Controls.L1));
            Assert.Same(sa, t.Mapping.deviceRuntime[t.Device].UntriggerAction);
            Assert.Equal(index, t.Mapping.deviceRuntime[t.Device].UntriggerIndex);
            Assert.Empty(recorder.RestoreFromActionCalls);

            t.RunFrame(Held(DS4Controls.L1)); // 保持中は復帰しない
            Assert.Empty(recorder.RestoreFromActionCalls);

            t.RunFrame(SpecialActionTestScope.Neutral()); // 離す
            Assert.Single(recorder.RestoreFromActionCalls);
            Assert.Equal(t.Device, recorder.RestoreFromActionCalls[0]);
            Assert.Null(t.Mapping.deviceRuntime[t.Device].UntriggerAction);
            Assert.Equal(-1, t.Mapping.deviceRuntime[t.Device].UntriggerIndex);

            t.RunFrame(SpecialActionTestScope.Neutral()); // 合図が消えているので、繰り返し復帰しない
            Assert.Single(recorder.RestoreFromActionCalls);
        }

        // ---- 解除トリガー（uTrigger）----

        [Fact]
        public void Profile_WithUntriggerButtons_RestoresOnlyWhenAllUntriggerButtonsArePressed()
        {
            using var t = new SpecialActionTestScope();
            var recorder = t.InstallProfileRecorder();
            SpecialAction sa = AddProfile(t, "L1", "R1"); // トリガー L1、解除トリガー R1
            Assert.False(sa.automaticUntrigger);
            Assert.Single(sa.uTrigger);

            t.RunFrame(Held(DS4Controls.L1));
            Assert.Same(sa, t.Mapping.deviceRuntime[t.Device].UntriggerAction);

            t.RunFrame(Held(DS4Controls.L1)); // 解除トリガーが押されていない間は、トリガーを離しても復帰しない
            t.RunFrame(SpecialActionTestScope.Neutral());
            Assert.Empty(recorder.RestoreFromActionCalls);
            Assert.Same(sa, t.Mapping.deviceRuntime[t.Device].UntriggerAction);

            t.RunFrame(Held(DS4Controls.R1)); // 解除トリガーを押す
            Assert.Single(recorder.RestoreFromActionCalls);
            Assert.Null(t.Mapping.deviceRuntime[t.Device].UntriggerAction);
        }

        // ---- 切替なしのトリガーは、復帰の合図を設定しない ----

        [Fact]
        public void Profile_WithoutUntriggerSettings_DoesNotRegisterAnUntriggerSignal_AndNeverRestores()
        {
            using var t = new SpecialActionTestScope();
            var recorder = t.InstallProfileRecorder();
            AddProfile(t, "L1", string.Empty);

            t.RunFrame(Held(DS4Controls.L1));
            t.RunFrame(SpecialActionTestScope.Neutral());

            Assert.Single(recorder.ApplyFromActionCalls);
            Assert.Null(t.Mapping.deviceRuntime[t.Device].UntriggerAction);
            Assert.Empty(recorder.RestoreFromActionCalls);
        }
    }
}
