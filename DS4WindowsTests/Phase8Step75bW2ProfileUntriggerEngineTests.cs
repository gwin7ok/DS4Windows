using System;
using System.Linq;
using DS4Windows;
using DS4Windows.Actions;
using DS4Windows.DI;
using Xunit;

namespace DS4WindowsTests
{
    /// <summary>
    /// Phase8-Step7 7.5b-W2（Phase8-Step7-5b-Plan.md §3 の W2）: 復帰の合図をエンジンが持つ新経路の確認（新経路だけの項目）。
    /// 旧・新の両経路の比べは <c>Phase8Step75bW1ProfileUntriggerDualPathTests</c>。
    /// プロファイルの適用は、記録用の実装で置き換える。他の Host 構築テストと同じコレクションで直列に実行する。
    /// </summary>
    [Collection("ServiceProviderHolder")]
    public class Phase8Step75bW2ProfileUntriggerEngineTests
    {
        private static DS4State Frame(bool l1, bool r1 = false)
        {
            DS4State state = SpecialActionTestScope.Neutral();
            state.L1 = l1;
            state.R1 = r1;
            return state;
        }

        private static SpecialAction AddProfile(SpecialActionTestScope t, string triggers, string extras)
        {
            SpecialAction sa = t.Add(new SpecialAction("p8s75bw2_profile_" + Guid.NewGuid().ToString("N"), triggers, "Profile", "p8s75b-target", 0, extras));
            ActionInstanceState state = t.StateOf(sa);
            state.RequiresFreshPressAfterReset = false;
            state.IsExecuting = false;
            return sa;
        }

        [Fact]
        public void TheSignalIsHeldByTheEngine_NotByMapping()
        {
            using var t = new SpecialActionTestScope(true);
            RecordingProfileApplicationService recorder = t.InstallProfileRecorder();
            SpecialAction sa = AddProfile(t, "L1", "AutomaticUntrigger");
            Assert.True(t.Engine.Owns(t.Device, sa));   // 復帰つきの Profile も、新経路が処理する

            t.RunFrame(Frame(true));

            Assert.Single(recorder.ApplyFromActionCalls);
            Assert.Null(t.Mapping.deviceRuntime[t.Device].UntriggerAction);   // 合図は、Mapping ではなくエンジンにある

            t.RunFrame(Frame(false));

            Assert.Single(recorder.RestoreFromActionCalls);
        }

        [Fact]
        public void AProfileWithUntriggerButtons_IsOwned_AndHasNoSignal_AfterTheRemovalOfTheUnloadTrigger()
        {
            // Phase8-Step8 8b（機能廃止。ユーザー承認）: 解除トリガー（uTrigger）のボタンによる復帰は廃止した。切替は新経路が行い、復帰の合図は立たない
            using var t = new SpecialActionTestScope(true);
            RecordingProfileApplicationService recorder = t.InstallProfileRecorder();
            SpecialAction sa = AddProfile(t, "L1", "R1");

            Assert.True(t.Engine.Owns(t.Device, sa));

            t.RunFrame(Frame(true));

            Assert.Null(t.Mapping.deviceRuntime[t.Device].UntriggerAction);
            Assert.Single(recorder.ApplyFromActionCalls);
        }

        [Fact]
        public void TheChainPrevention_AlsoSeesTheEnginesSignal()
        {
            // 一時プロファイルの間、復帰待ちの Profile（エンジンの合図）があれば、別の Profile 切替を始めない
            IProfileSettingsService settings = TestHost.GetService<IProfileSettingsService>();
            using var t = new SpecialActionTestScope(true);
            bool savedTemp = settings.GetUseTempProfile(t.Device);
            try
            {
                RecordingProfileApplicationService recorder = t.InstallProfileRecorder();
                AddProfile(t, "L1", "AutomaticUntrigger");
                SpecialAction other = AddProfile(t, "R1", string.Empty);

                t.RunFrame(Frame(true));   // 1 つ目が切り替わり、エンジンに合図が立つ
                Assert.Single(recorder.ApplyFromActionCalls);

                // 適用が状態を作り直したので、2 つ目の Fresh-press ガードを外す（連鎖の防止だけを確かめる）
                ActionInstanceState state = t.StateOf(other);
                state.RequiresFreshPressAfterReset = false;
                state.IsExecuting = false;
                settings.SetUseTempProfile(t.Device, true);

                t.RunFrame(Frame(true, true));
                t.RunFrame(Frame(true, true));
                Assert.Single(recorder.ApplyFromActionCalls);   // 切り替えない

                settings.SetUseTempProfile(t.Device, false);
                t.RunFrame(Frame(true, true));
                Assert.Equal(2, recorder.ApplyFromActionCalls.Count);   // 一時プロファイルでなければ、切り替える
            }
            finally
            {
                settings.SetUseTempProfile(t.Device, savedTemp);
            }
        }

        [Fact]
        public void TheReleaseEdgeDoesNotRestoreTwice_WhenTheApplyHasNotResetTheState()
        {
            // 適用が終わっていない（状態が作り直されていない）状態で離しても、復帰は 1 回だけ（離したエッジの Stop を、復帰つきの Profile は呼ばない。Q4）
            using var t = new SpecialActionTestScope(true);
            RecordingProfileApplicationService recorder = t.InstallProfileRecorder();
            recorder.SimulateStateReset = false;
            AddProfile(t, "L1", "AutomaticUntrigger");

            t.RunFrame(Frame(false));
            t.RunFrame(Frame(true));
            t.RunFrame(Frame(false));
            t.RunFrame(Frame(false));

            Assert.Single(recorder.RestoreFromActionCalls);
        }

        [Fact]
        public void TheBindingForAnAutomaticProfile_DoesNotStopOnRelease_AndASwitchOnlyOneDoes()
        {
            var builder = TestHost.GetService<IActionBindingBuilder>();
            using var t = new SpecialActionTestScope(true);
            t.InstallProfileRecorder();
            AddProfile(t, "L1", "AutomaticUntrigger");
            AddProfile(t, "R1", string.Empty);

            var bindings = builder.Build(t.Device);

            Assert.Equal(2, bindings.Count);
            Assert.Equal(UntriggerPolicy.Automatic, bindings[0].Policy.Untrigger);
            Assert.False(bindings[0].Policy.StopOnRelease);
            Assert.Equal(UntriggerPolicy.None, bindings[1].Policy.Untrigger);
            Assert.True(bindings[1].Policy.StopOnRelease);
        }
    }
}
