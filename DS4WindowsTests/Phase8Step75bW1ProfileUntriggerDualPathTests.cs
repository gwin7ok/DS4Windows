using System;
using System.Collections.Generic;
using System.Linq;
using DS4Windows;
using Xunit;
using Xunit.Abstractions;

namespace DS4WindowsTests
{
    /// <summary>
    /// Phase8-Step7 7.5b-W1（Phase8-Step7-5b-Plan.md §3 の W1）: Profile 種別の復帰（「Unload on trigger release」＝<c>AutomaticUntrigger</c>）の特性化。
    /// コードは変えない。復帰の合図の置き場所（旧 <c>Mapping</c>／新 エンジン）に依存しない、外から見える動き（復帰の回数）を、旧・新の両経路（<c>InlineData(false)</c>／<c>InlineData(true)</c>）で比べる。
    /// W1 の時点では、ビルダーが復帰つきの Profile の束縛を作らないため、新経路の指定でも旧経路が処理する。W2 で新経路が復帰を持つと、同じ検査が新経路を確かめる。
    /// 観察は <see cref="RecordingProfileApplicationService"/>（<c>InstallProfileRecorder</c>）で、実際のプロファイルの読込・適用は走らせない。
    /// 他の Host 構築テストと同じコレクションで直列に実行する。
    /// Phase8-Step8 8d（持ち越し C-2）: 旧経路は削除済み。以前の「旧・新の両経路の比べ」は、新経路の期待値の固定（比べを除いた、残りの検査）に変えた。
    /// </summary>
    [Collection("ServiceProviderHolder")]
    public class Phase8Step75bW1ProfileUntriggerDualPathTests
    {
        private readonly ITestOutputHelper _output;

        public Phase8Step75bW1ProfileUntriggerDualPathTests(ITestOutputHelper output)
        {
            _output = output;
        }

        private static DS4State Frame(bool l1, bool r1 = false)
        {
            DS4State state = SpecialActionTestScope.Neutral();
            state.L1 = l1;
            state.R1 = r1;
            return state;
        }

        private static SpecialAction AddProfile(SpecialActionTestScope t, string triggers, bool freshPressGuard = false, bool executing = false)
        {
            SpecialAction sa = t.Add(new SpecialAction("p8s75b_profile_" + Guid.NewGuid().ToString("N"), triggers, "Profile", "p8s75b-target", 0, "AutomaticUntrigger"));
            Assert.True(sa.automaticUntrigger);
            ActionInstanceState state = t.StateOf(sa);
            state.RequiresFreshPressAfterReset = freshPressGuard;
            state.IsExecuting = executing;
            return sa;
        }

        /// <summary>入力の列を流し、フレームごとの（累計の切替の回数、累計の復帰の回数）を返す。<paramref name="beforeFrame"/> は、フレームの前に行う操作（適用の後始末、切断など）。</summary>
        private static List<(int Applies, int Restores)> Run(string triggers, DS4State[] frames,
            bool freshPressGuard = false, bool executing = false, Action<SpecialActionTestScope, int> beforeFrame = null)
        {
            using var t = new SpecialActionTestScope();
            RecordingProfileApplicationService recorder = t.InstallProfileRecorder();
            AddProfile(t, triggers, freshPressGuard, executing);

            var result = new List<(int, int)>();
            for (int i = 0; i < frames.Length; i++)
            {
                beforeFrame?.Invoke(t, i);
                t.RunFrame(frames[i]);
                result.Add((recorder.ApplyFromActionCalls.Count, recorder.RestoreFromActionCalls.Count));
            }

            return result;
        }

        [Fact]
        public void RestoresOnce_WhenTheTriggerIsReleased()
        {
            DS4State[] frames = { Frame(false), Frame(true), Frame(true), Frame(true), Frame(false), Frame(false), Frame(false) };

            var counts = Run("L1", frames);

            Assert.Equal((0, 0), counts[0]);
            Assert.Equal((1, 0), counts[1]);   // 押して切り替える
            Assert.Equal((1, 0), counts[3]);   // 保持中は、再び切り替えず、復帰もしない
            Assert.Equal((1, 1), counts[4]);   // 離して復帰する
            Assert.Equal((1, 1), counts[6]);   // 合図が消えているので、繰り返し復帰しない
        }

        [Fact]
        public void AndCondition_RestoresWhenAnyOneOfTheTriggersIsReleased()
        {
            DS4State[] frames = { Frame(false, false), Frame(true, true), Frame(true, true), Frame(true, false), Frame(true, false), Frame(false, false) };

            var counts = Run("L1/R1", frames);

            Assert.Equal((1, 0), counts[1]);
            Assert.Equal((1, 0), counts[2]);
            Assert.Equal((1, 1), counts[3]);   // R1 だけ離れた時点で復帰する
            Assert.Equal((1, 1), counts[5]);
        }

        [Fact]
        public void WhenNothingSwitched_NothingIsRestored()
        {
            // Fresh-press ガード中、実行中は、切り替えない。合図が立たないので、離しても復帰しない
            DS4State[] frames = { Frame(true), Frame(true), Frame(false), Frame(false) };

            var guarded = Run("L1", frames, freshPressGuard: true);
            var executing = Run("L1", frames, executing: true);

            Assert.Equal((0, 0), guarded.Last());
            Assert.Equal((0, 0), executing.Last());
        }

        [Fact]
        public void AProfileApplyWhileHeld_KeepsTheSignal_AndTheReleaseStillRestoresOnce()
        {
            // Phase7b W1 の観点: 切替の適用の後始末（ClearDeviceState など）が入っても、復帰の合図は消えない
            DS4State[] frames = { Frame(false), Frame(true), Frame(true), Frame(true), Frame(false), Frame(false) };

            var counts = Run("L1", frames, beforeFrame: (t, i) =>
            {
                if (i == 3) t.SimulateProfileApply();
            });

            Assert.Equal((1, 0), counts[3]);
            Assert.Equal((1, 1), counts[4]);
            Assert.Equal((1, 1), counts[5]);
        }

        [Fact]
        public void APhysicalDisconnect_ClearsTheSignal_SoTheReleaseDoesNotRestore()
        {
            // 本番の物理切断は、Mapping.HandleDeviceDisconnect（ControlService）と、エンジンの OnDeviceDisconnected（ProfileSlotApplier.ReleaseSlot）の両方を呼ぶ
            DS4State[] frames = { Frame(false), Frame(true), Frame(true), Frame(false), Frame(false) };

            var counts = Run("L1", frames, beforeFrame: (t, i) =>
            {
                if (i == 2)
                {
                    t.Mapping.HandleDeviceDisconnect(t.Device);
                    t.Engine.OnDeviceDisconnected(t.Device);
                }
            });

            Assert.Equal((1, 0), counts[1]);
            Assert.Equal((1, 0), counts.Last());   // 合図が消えているので、離しても復帰しない
        }

        // ---- 報告のみ（Q4）: 適用が終わる前にトリガーを離したとき ----

        [Fact]
        public void ReleaseBeforeTheApplyCompletes_IsReported()
        {
            // 記録用の実装が状態の掃除をしない（適用が終わっていない）状態で、押してすぐ離す。復帰が 1 回か、2 回（離したエッジの Stop が加わる）かを報告する。
            // 判定は付けない。Q4 の決定（Profile の離したエッジの Stop を、新経路で呼ぶか）の材料にする
            using var t = new SpecialActionTestScope();
            RecordingProfileApplicationService recorder = t.InstallProfileRecorder();
            recorder.SimulateStateReset = false;
            AddProfile(t, "L1");

            var restores = new List<int>();
            foreach (bool l1 in new[] { false, true, true, false, false, false })
            {
                t.RunFrame(Frame(l1));
                restores.Add(recorder.RestoreFromActionCalls.Count);
            }

            _output.WriteLine($"[Phase8Step7.5b ReleaseBeforeApply] new applies={recorder.ApplyFromActionCalls.Count} restoresPerFrame=[{string.Join(",", restores)}]");
        }
    }
}
