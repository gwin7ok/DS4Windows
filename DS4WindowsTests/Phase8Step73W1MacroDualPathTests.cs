using System;
using System.Collections.Generic;
using DS4Windows;
using DS4Windows.Actions;
using Xunit;

namespace DS4WindowsTests
{
    /// <summary>
    /// Phase8-Step7 7.3-W1（Phase8-Step7-3-Plan.md §3 W1）: Macro 種別の「旧経路（<c>MapCustomAction</c> の Macro の分岐）」と「新経路（エンジン）」の特性化。
    /// 同じ入力の列を両経路に流し、フレームごとの状態（<c>BeingTriggered</c>、再生中の旗［<c>IMacroPlayer.IsPlaying</c>］）が一致することを確かめる。
    /// マクロの再生（キーの送出の時系列）は、非同期のタスクのため固定しない。再生の入口（<c>Play</c>）と停止（<c>Stop</c>）が呼ばれるかを、再生中の旗で見る。
    /// 他の Host 構築テストと同じコレクションで直列に実行する。
    /// Phase8-Step8 8d（持ち越し C-2）: 旧経路は削除済み。以前の「旧・新の両経路の比べ」は、新経路の期待値の固定（比べを除いた、残りの検査）に変えた。
    /// </summary>
    [Collection("ServiceProviderHolder")]
    public class Phase8Step73W1MacroDualPathTests
    {
        private static DS4State Frame(bool l1, bool r1 = false)
        {
            DS4State state = SpecialActionTestScope.Neutral();
            state.L1 = l1;
            state.R1 = r1;
            return state;
        }

        private static List<(bool Being, bool Playing)> Run(string triggers, string extras, double delay, bool freshPressGuard,
            DS4State[] frames, int applyBeforeFrame = -1)
        {
            using var t = new SpecialActionTestScope();
            SpecialAction sa = t.Add(new SpecialAction("p8s73_macro_" + Guid.NewGuid().ToString("N"), triggers, "Macro", "65", delay, extras));
            ActionInstanceState state = t.StateOf(sa);
            state.RequiresFreshPressAfterReset = freshPressGuard;
            state.IsExecuting = false;
            state.UntriggeredTimestampTicks = 0;

            IMacroPlayer player = TestHost.GetService<IMacroPlayer>();
            player.Stop(t.Device);   // 前のテストの再生中の旗を下げる

            var result = new List<(bool, bool)>();
            try
            {
                for (int i = 0; i < frames.Length; i++)
                {
                    if (i == applyBeforeFrame)
                        t.SimulateProfileApply();

                    t.RunFrame(frames[i]);
                    result.Add((state.BeingTriggered, player.IsPlaying(t.Device)));
                }
            }
            finally
            {
                player.Stop(t.Device);
            }

            return result;
        }

        [Fact]
        public void Press_Sequence_GivesTheState()
        {
            DS4State[] frames = { Frame(false), Frame(true), Frame(true), Frame(true), Frame(false), Frame(false), Frame(true), Frame(false) };

            var result = Run("L1", string.Empty, 0, false, frames);

            Assert.Contains(result, f => f.Being);      // 何もしないテストにならないこと
        }

        [Fact]
        public void RunOnRelease_Sequence_GivesTheState()
        {
            DS4State[] frames = { Frame(false), Frame(true), Frame(true), Frame(false), Frame(false), Frame(true), Frame(false), Frame(false) };

            var result = Run("L1", "RunOnRelease", 0, false, frames);

            Assert.Equal((false, false), result[1]);    // 押したときは、実行しない
            Assert.Contains(result, f => f.Being);      // 離したときに、実行する
        }

        [Theory]
        [InlineData("")]
        [InlineData("RunOnRelease")]
        public void WhileTheFreshPressGuardIsOn_NothingRuns(string extras)
        {
            DS4State[] frames = { Frame(true), Frame(true), Frame(true) };

            var result = Run("L1", extras, 0, true, frames);

            Assert.DoesNotContain(result, f => f.Being || f.Playing);
        }

        [Fact]
        public void AndCondition_GivesTheState()
        {
            DS4State[] frames = { Frame(false, false), Frame(true, false), Frame(true, true), Frame(true, true), Frame(false, true), Frame(false, false) };

            var result = Run("L1/R1", string.Empty, 0, false, frames);

            Assert.Contains(result, f => f.Being);
        }

    }
}
