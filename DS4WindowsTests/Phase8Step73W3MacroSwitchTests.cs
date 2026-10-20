using System;
using System.Diagnostics;
using System.Linq;
using DS4Windows;
using DS4Windows.Actions;
using NLog;
using NLog.Config;
using NLog.Targets;
using Xunit;
using Xunit.Abstractions;

namespace DS4WindowsTests
{
    /// <summary>
    /// Phase8-Step7 7.3-W3（Phase8-Step7-3-Plan.md §3 W3）: Macro をスイッチに入れた状態の、ロールバック・割り当て・性能の確認。
    /// 他の Host 構築テストと同じコレクションで直列に実行する。
    /// </summary>
    [Collection("ServiceProviderHolder")]
    public class Phase8Step73W3MacroSwitchTests
    {
        private readonly ITestOutputHelper _output;

        public Phase8Step73W3MacroSwitchTests(ITestOutputHelper output)
        {
            _output = output;
        }

        private static DS4State Frame(bool l1)
        {
            DS4State state = SpecialActionTestScope.Neutral();
            state.L1 = l1;
            return state;
        }

        private static SpecialAction AddMacro(SpecialActionTestScope t, string triggers)
        {
            SpecialAction sa = t.Add(new SpecialAction("p8s73w3_macro_" + Guid.NewGuid().ToString("N"), triggers, "Macro", "65", 0, string.Empty));
            ActionInstanceState state = t.StateOf(sa);
            state.RequiresFreshPressAfterReset = false;
            state.IsExecuting = false;
            return sa;
        }

        private static long Allocated(SpecialActionTestScope t, DS4State input)
        {
            long before = GC.GetAllocatedBytesForCurrentThread();
            for (int i = 0; i < 1000; i++) t.RunFrame(input);
            return GC.GetAllocatedBytesForCurrentThread() - before;
        }

        private static int CountMacroFirings(int heldFrames)
        {
            LoggingConfiguration saved = LogManager.Configuration;
            var target = new MemoryTarget("p8s73w3") { Layout = "${message}" };
            var config = new LoggingConfiguration();
            config.AddTarget(target);
            config.AddRule(LogLevel.Debug, LogLevel.Fatal, target);
            LogManager.Configuration = config;
            IMacroPlayer player = TestHost.GetService<IMacroPlayer>();
            string name = "p8s73w3_once_" + Guid.NewGuid().ToString("N");
            try
            {
                using (var t = new SpecialActionTestScope())
                {
                    SpecialAction sa = t.Add(new SpecialAction(name, "L1", "Macro", "65", 0, string.Empty));
                    t.StateOf(sa).RequiresFreshPressAfterReset = false;
                    player.Stop(t.Device);
                    t.RunFrame(Frame(false));
                    for (int i = 0; i < heldFrames; i++)
                    {
                        t.RunFrame(Frame(true));
                        if (i % 100 == 0) System.Threading.Thread.Sleep(5);   // 短いマクロの再生が終わる時間を与える
                    }

                    player.Stop(t.Device);
                }

                return target.Logs.Count(l => l.Contains("SpecialAction Macro:") && l.Contains(name));
            }
            finally
            {
                LogManager.Configuration = saved;
            }
        }

        [Fact]
        public void HeldMacro_FiresOnlyOnce()
        {
            int firings = CountMacroFirings(1000);

            _output.WriteLine($"[Phase8Step7.3 Macro] firings while held (1000 frames): {firings}");
            Assert.Equal(1, firings);
        }

        [Fact]
        public void MapCustom_MacroMigrated_PerFrameTime_IsReported()
        {
            IMacroPlayer player = TestHost.GetService<IMacroPlayer>();
            using var t = new SpecialActionTestScope();
            string[] triggers = { "Cross", "Circle", "Square", "Triangle", "L1", "R1", "Cross/L1", "Circle/R1", "Share", "Options" };
            for (int i = 0; i < 30; i++)
                AddMacro(t, triggers[i % triggers.Length]);

            DS4State pressed = SpecialActionTestScope.Neutral();
            pressed.Cross = true;
            pressed.L1 = true;
            for (int i = 0; i < 300; i++) t.RunFrame(pressed);

            var sw = Stopwatch.StartNew();
            for (int i = 0; i < 5000; i++) t.RunFrame(pressed);
            sw.Stop();
            player.Stop(t.Device);
            _output.WriteLine($"[Phase8Step7.3 Macro] new actions=30 perFrame={sw.Elapsed.TotalMilliseconds * 1000.0 / 5000:F2}us");
        }
    }
}
