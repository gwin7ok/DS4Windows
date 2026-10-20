using System;
using System.Collections.Generic;
using System.Linq;
using DS4Windows;
using Xunit;

namespace DS4WindowsTests
{
    /// <summary>
    /// Phase8-Step7 7.4-W1（Phase8-Step7-4-Plan.md §3 W1）: Program 種別の「旧経路（<c>MapCustomAction</c> の Program の分岐）」と「新経路（エンジン）」の特性化。
    /// 実際のプログラムを起動しないよう、存在しない実行ファイルのパスを使う。<c>DefaultProcessLauncher</c> は、起動に失敗すると
    /// <c>AppLogger.LogToGui("Process launch failed for '&lt;パス&gt;': …")</c> を出すため、<c>AppLogger.GuiLog</c> を購読して、パスごとの起動の試行を数える
    /// （旧・新とも同じ部品を通るため、回数を比べられる）。
    /// 他の Host 構築テストと同じコレクションで直列に実行する。
    /// Phase8-Step8 8d（持ち越し C-2）: 旧経路は削除済み。以前の「旧・新の両経路の比べ」は、新経路の期待値の固定（比べを除いた、残りの検査）に変えた。
    /// </summary>
    [Collection("ServiceProviderHolder")]
    public class Phase8Step74W1ProgramDualPathTests
    {
        private static DS4State Frame(bool l1, bool r1 = false)
        {
            DS4State state = SpecialActionTestScope.Neutral();
            state.L1 = l1;
            state.R1 = r1;
            return state;
        }

        private static string NewPath() => @"C:\p8s74-nonexistent\" + Guid.NewGuid().ToString("N") + ".exe";

        private sealed class LaunchCounter : IDisposable
        {
            private readonly string _path;
            private readonly EventHandler<DebugEventArgs> _sink;
            public int Count;

            public LaunchCounter(string path)
            {
                _path = path;
                _sink = (sender, e) =>
                {
                    if (e?.Data != null && e.Data.Contains("Process launch failed for '" + _path + "'"))
                        System.Threading.Interlocked.Increment(ref Count);
                };
                AppLogger.GuiLog += _sink;
            }

            public void Dispose() => AppLogger.GuiLog -= _sink;
        }

        /// <summary>入力の列を流し、フレームごとの（累計の起動の試行、<c>BeingTriggered</c>、<c>IsExecuting</c>）を返す。</summary>
        private static List<(int Launches, bool Being, bool Executing)> Run(string triggers, string extras, double delay,
            bool freshPressGuard, bool executing, DS4State[] frames, int applyBeforeFrame = -1)
        {
            string path = NewPath();
            using var counter = new LaunchCounter(path);
            using var t = new SpecialActionTestScope();
            SpecialAction sa = t.Add(new SpecialAction("p8s74_prog_" + Guid.NewGuid().ToString("N"), triggers, "Program", path, delay, extras));
            ActionInstanceState state = t.StateOf(sa);
            state.RequiresFreshPressAfterReset = freshPressGuard;
            state.IsExecuting = executing;
            state.UntriggeredTimestampTicks = 0;

            var result = new List<(int, bool, bool)>();
            try
            {
                for (int i = 0; i < frames.Length; i++)
                {
                    if (i == applyBeforeFrame)
                        t.SimulateProfileApply();

                    t.RunFrame(frames[i]);
                    result.Add((counter.Count, state.BeingTriggered, state.IsExecuting));
                }
            }
            finally
            {
                state.IsExecuting = false;
            }

            return result;
        }

        [Fact]
        public void Sequence_GivesTheLaunchesAndState()
        {
            DS4State[] frames = { Frame(false), Frame(true), Frame(true), Frame(true), Frame(false), Frame(false), Frame(true), Frame(true), Frame(false) };

            var result = Run("L1", string.Empty, 0, false, false, frames);

            Assert.Equal(2, result.Last().Launches);    // 押すたびに 1 回（保持中は起動しない）。何もしないテストにならないこと
        }

        [Fact]
        public void WithTheHiddenKeyword_GivesTheLaunches()
        {
            DS4State[] frames = { Frame(false), Frame(true), Frame(true), Frame(false), Frame(true) };

            var result = Run("L1", "-a $hidden -b", 0, false, false, frames);

            Assert.Equal(2, result.Last().Launches);
        }

        [Fact]
        public void WhileTheFreshPressGuardIsOn_NothingLaunches()
        {
            DS4State[] frames = { Frame(true), Frame(true), Frame(true) };

            var result = Run("L1", string.Empty, 0, true, false, frames);

            Assert.Equal(0, result.Last().Launches);
            Assert.DoesNotContain(result, f => f.Being);
        }

        [Fact]
        public void WhileExecuting_NothingLaunches()
        {
            DS4State[] frames = { Frame(true), Frame(true), Frame(false), Frame(true) };

            var result = Run("L1", string.Empty, 0, false, true, frames);

            Assert.Equal(0, result.Last().Launches);
        }

        [Fact]
        public void AndCondition_GivesTheLaunches()
        {
            DS4State[] frames = { Frame(false, false), Frame(true, false), Frame(true, true), Frame(true, true), Frame(false, true), Frame(false, false) };

            var result = Run("L1/R1", string.Empty, 0, false, false, frames);

            Assert.Equal(1, result.Last().Launches);
        }

    }
}
