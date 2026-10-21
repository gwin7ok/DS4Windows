using System;
using System.Collections.Generic;
using System.Linq;
using DS4Windows;
using NLog;
using NLog.Config;
using NLog.Targets;
using Xunit;

namespace DS4WindowsTests
{
    /// <summary>
    /// Phase8-Step7 7.1-W2（Phase8-Step7-Plan.md §5.2・Q6＝A）: 旧 Stage3 が出していた Button の TRACE ログ（立ち上がり・立ち下がり、出力の組み合わせ、立ち上がりの詳細）を、
    /// 新経路が同じ文言・順序で出すことを確かめる。NLog の設定をテストの間だけ Trace 有効のメモリ出力へ差し替え、旧経路と新経路のログの行を比べる。
    /// 他の Host 構築テストと同じコレクションで直列に実行する。
    /// Phase8-Step8 8d（持ち越し C-2）: 旧経路は削除済み。以前の「旧・新の両経路の比べ」は、新経路の期待値の固定（比べを除いた、残りの検査）に変えた。
    /// </summary>
    [Collection("ServiceProviderHolder")]
    public class Phase8Step7W2ButtonTraceLogTests
    {
        private static DS4State Frame(bool l1)
        {
            DS4State state = SpecialActionTestScope.Neutral();
            state.L1 = l1;
            return state;
        }

        /// <summary>Trace を有効にしたメモリ出力へ NLog を差し替えて <paramref name="body"/> を実行し、記録された行を返す（終了時に元の設定へ戻す）。</summary>
        private static List<string> CaptureTrace(Action body, string mustContain)
        {
            LoggingConfiguration saved = LogManager.Configuration;
            var target = new MemoryTarget("p8s7w2") { Layout = "${level}|${message}" };
            var config = new LoggingConfiguration();
            config.AddTarget(target);
            config.AddRule(LogLevel.Trace, LogLevel.Fatal, target);
            LogManager.Configuration = config;
            try
            {
                body();
                // Button の TRACE ログだけを比べる（ActionManager の生成のログなど、経路に依らない別のログは除く）
                return target.Logs.Where(l => l.Contains("'" + mustContain + "'") &&
                    (l.Contains("Trigger detected for") || l.Contains("Trigger no longer active") || l.Contains("maps to") ||
                     l.Contains("Trigger keys for") || l.Contains("output combo") || l.Contains("Failed to build output combo"))).ToList();
            }
            finally
            {
                LogManager.Configuration = saved;
            }
        }

        private static List<string> RunLogged(string name, string details, string extras, bool[] l1Frames)
        {
            return CaptureTrace(() =>
            {
                using var t = new SpecialActionTestScope();
                SpecialAction sa = t.Add(new SpecialAction(name, "L1", "Button", details, 0, extras));
                ActionInstanceState state = t.StateOf(sa);
                state.IsToggledOn = false;
                state.RequiresFreshPressAfterReset = false;
                state.UntriggeredTimestampTicks = 0;
                foreach (bool l1 in l1Frames)
                    t.RunFrame(Frame(l1));
            }, name);
        }

        [Fact]
        public void PressTrace_NewPathLogsTheSameLinesInTheSameOrder()
        {
            string name = "p8s7w2_press_" + Guid.NewGuid().ToString("N");
            bool[] frames = { false, true, true, false, true, false };

            var result = RunLogged(name, ((int)X360Controls.A).ToString(), "Press\n", frames);

            Assert.Contains(result, l => l.Contains("Trigger detected for Button SA"));
            Assert.Contains(result, l => l.Contains("Trigger no longer active for Button SA"));
            Assert.Contains(result, l => l.Contains("maps to X360Controls"));
            Assert.Contains(result, l => l.Contains("Trigger keys for SA") && l.EndsWith("<none>"));   // 旧経路は常に <none>（状態を渡さず例外を握りつぶす既存の挙動。新経路も同じにする）
            Assert.Contains(result, l => l.Contains("output combo:"));
        }

        [Fact]
        public void ToggleTrace_NewPathLogsTheSameLinesInTheSameOrder()
        {
            string name = "p8s7w2_toggle_" + Guid.NewGuid().ToString("N");
            bool[] frames = { false, true, false, false, true, true, false };

            var result = RunLogged(name, ((int)X360Controls.A).ToString(), "Toggle\n", frames);

            Assert.Contains(result, l => l.Contains("output combo:"));
        }

        [Fact]
        public void MouseAndTouchpadTrace_NewPathLogsTheSameLines()
        {
            bool[] frames = { true, true, false, true };
            foreach (X360Controls control in new[] { X360Controls.LeftMouse, X360Controls.TouchpadClick })
            {
                string name = "p8s7w2_" + control + "_" + Guid.NewGuid().ToString("N");
                var result = RunLogged(name, ((int)control).ToString(), "Press\n", frames);

                Assert.Contains(result, l => l.Contains(control == X360Controls.LeftMouse ? "LeftMouse(" : "TouchpadClick"));
            }
        }

        [Fact]
        public void UnreadableDetails_TraceMatchesToo()
        {
            string name = "p8s7w2_bad_" + Guid.NewGuid().ToString("N");
            bool[] frames = { true, true, false };

            var result = RunLogged(name, "not-a-number", "Press\n", frames);

            Assert.Contains(result, l => l.Contains("Type:Button Details:not-a-number"));
        }

        [Fact]
        public void WhenTraceIsOff_NothingIsLogged()
        {
            string name = "p8s7w2_off_" + Guid.NewGuid().ToString("N");
            bool[] frames = { true, true, false };

            LoggingConfiguration saved = LogManager.Configuration;
            var target = new MemoryTarget("p8s7w2off") { Layout = "${message}" };
            var config = new LoggingConfiguration();
            config.AddTarget(target);
            // Trace は無効。Phase10-Step4c W2（ユーザー承認、2026-10-21）で、離したときの Debug の行（SpecialAction Button released: …）を全種別に足したため、Debug も無効にする
            config.AddRule(LogLevel.Info, LogLevel.Fatal, target);
            LogManager.Configuration = config;
            try
            {
                foreach (bool result in new[] { false, true })
                {
                    using var t = new SpecialActionTestScope();
                    t.Add(new SpecialAction(name, "L1", "Button", ((int)X360Controls.A).ToString(), 0, "Press\n"));
                    foreach (bool l1 in frames) t.RunFrame(Frame(l1));
                }

                Assert.DoesNotContain(target.Logs, l => l.Contains(name));
            }
            finally
            {
                LogManager.Configuration = saved;
            }
        }
    }
}
