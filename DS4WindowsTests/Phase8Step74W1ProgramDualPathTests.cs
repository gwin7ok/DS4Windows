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
        private static List<(int Launches, bool Being, bool Executing)> Run(bool migrated, string triggers, string extras, double delay,
            bool freshPressGuard, bool executing, DS4State[] frames, int applyBeforeFrame = -1)
        {
            string path = NewPath();
            using var counter = new LaunchCounter(path);
            using var t = new SpecialActionTestScope(migrated);
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
        public void Sequence_BothPathsGiveTheSameLaunchesAndState()
        {
            DS4State[] frames = { Frame(false), Frame(true), Frame(true), Frame(true), Frame(false), Frame(false), Frame(true), Frame(true), Frame(false) };

            var legacy = Run(false, "L1", string.Empty, 0, false, false, frames);
            var migrated = Run(true, "L1", string.Empty, 0, false, false, frames);

            Assert.Equal(legacy, migrated);
            Assert.Equal(2, legacy.Last().Launches);    // 押すたびに 1 回（保持中は起動しない）。比較が「何もしない」同士にならないこと
        }

        [Fact]
        public void WithTheHiddenKeyword_BothPathsGiveTheSameLaunches()
        {
            DS4State[] frames = { Frame(false), Frame(true), Frame(true), Frame(false), Frame(true) };

            var legacy = Run(false, "L1", "-a $hidden -b", 0, false, false, frames);
            var migrated = Run(true, "L1", "-a $hidden -b", 0, false, false, frames);

            Assert.Equal(legacy, migrated);
            Assert.Equal(2, legacy.Last().Launches);
        }

        [Fact]
        public void WhileTheFreshPressGuardIsOn_NothingLaunches_OnEitherPath()
        {
            DS4State[] frames = { Frame(true), Frame(true), Frame(true) };

            var legacy = Run(false, "L1", string.Empty, 0, true, false, frames);
            var migrated = Run(true, "L1", string.Empty, 0, true, false, frames);

            Assert.Equal(legacy, migrated);
            Assert.Equal(0, legacy.Last().Launches);
            Assert.DoesNotContain(legacy, f => f.Being);
        }

        [Fact]
        public void WhileExecuting_NothingLaunches_OnEitherPath()
        {
            DS4State[] frames = { Frame(true), Frame(true), Frame(false), Frame(true) };

            var legacy = Run(false, "L1", string.Empty, 0, false, true, frames);
            var migrated = Run(true, "L1", string.Empty, 0, false, true, frames);

            Assert.Equal(legacy, migrated);
            Assert.Equal(0, legacy.Last().Launches);
        }

        [Fact]
        public void AndCondition_BothPathsGiveTheSameLaunches()
        {
            DS4State[] frames = { Frame(false, false), Frame(true, false), Frame(true, true), Frame(true, true), Frame(false, true), Frame(false, false) };

            var legacy = Run(false, "L1/R1", string.Empty, 0, false, false, frames);
            var migrated = Run(true, "L1/R1", string.Empty, 0, false, false, frames);

            Assert.Equal(legacy, migrated);
            Assert.Equal(1, legacy.Last().Launches);
        }

        [Fact]
        public void ADelay_BehavesTheSame_OnEitherPath()
        {
            DS4State[] frames = { Frame(true), Frame(true), Frame(false), Frame(true) };

            var legacy = Run(false, "L1", string.Empty, 60, false, false, frames);   // 60 秒
            var migrated = Run(true, "L1", string.Empty, 60, false, false, frames);

            // 現行の挙動に新経路を合わせる（Key・Macro と同じく、遅延の基準時刻の初期値が最小値のため、最初の押下は遅延なしで成立しうる）
            Assert.Equal(legacy, migrated);
        }

        [Fact]
        public void AProfileApplyWhileHeld_BothPathsGiveTheSameLaunches()
        {
            // 押している最中（フレーム 3 の前）に、適用の後始末が入る。適用の直後は Fresh-press ガードが有効で、押し直すまで起動しない
            DS4State[] frames = { Frame(false), Frame(true), Frame(true), Frame(true), Frame(true), Frame(false), Frame(false) };

            var legacy = Run(false, "L1", string.Empty, 0, false, false, frames, applyBeforeFrame: 3);
            var migrated = Run(true, "L1", string.Empty, 0, false, false, frames, applyBeforeFrame: 3);

            Assert.Equal(legacy, migrated);
        }
    }
}
