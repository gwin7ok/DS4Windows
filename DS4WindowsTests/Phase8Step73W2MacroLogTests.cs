using System;
using System.Collections.Generic;
using System.Linq;
using DS4Windows;
using DS4Windows.Actions;
using NLog;
using NLog.Config;
using NLog.Targets;
using Xunit;

namespace DS4WindowsTests
{
    /// <summary>
    /// Phase8-Step7 7.3-W2（Phase8-Step7-3-Plan.md Q3＝A）: 旧 <c>MapCustomAction</c> の Macro の分岐が出していたログを、新経路が同じ文言・順序で出すことを確かめる。
    /// NLog の設定をテストの間だけ Trace 有効のメモリ出力へ差し替え、旧経路と新経路のログの行を比べる（7.2 の <c>Phase8Step72W2KeyLogTests</c> と同じ方式）。
    /// 他の Host 構築テストと同じコレクションで直列に実行する。
    /// </summary>
    [Collection("ServiceProviderHolder")]
    public class Phase8Step73W2MacroLogTests
    {
        private static DS4State Frame(bool l1)
        {
            DS4State state = SpecialActionTestScope.Neutral();
            state.L1 = l1;
            return state;
        }

        private static bool IsMacroLogLine(string line, string name)
        {
            if (!line.Contains(name)) return false;
            return line.Contains("SpecialAction Macro") || line.Contains("Trigger detected for SA") || line.Contains("maps to Type:Macro")
                || line.Contains("Trigger keys for SA") || line.Contains("output combo");
        }

        private static List<string> RunLogged(bool migrated, string name, string extras, bool[] l1Frames, LogLevel minLevel)
        {
            LoggingConfiguration saved = LogManager.Configuration;
            var target = new MemoryTarget("p8s73w2") { Layout = "${level}|${message}" };
            var config = new LoggingConfiguration();
            config.AddTarget(target);
            config.AddRule(minLevel, LogLevel.Fatal, target);
            LogManager.Configuration = config;
            IMacroPlayer player = TestHost.GetService<IMacroPlayer>();
            try
            {
                using (var t = new SpecialActionTestScope(migrated))
                {
                    SpecialAction sa = t.Add(new SpecialAction(name, "L1", "Macro", "65", 0, extras));
                    ActionInstanceState state = t.StateOf(sa);
                    state.RequiresFreshPressAfterReset = false;
                    state.IsExecuting = false;
                    player.Stop(t.Device);
                    foreach (bool l1 in l1Frames)
                        t.RunFrame(Frame(l1));
                    player.Stop(t.Device);
                }

                return target.Logs.Where(l => IsMacroLogLine(l, name)).ToList();
            }
            finally
            {
                LogManager.Configuration = saved;
            }
        }

        [Theory]
        [InlineData("")]
        [InlineData("RunOnRelease")]
        public void TheMacroLogs_NewPathLogsTheSameLinesInTheSameOrder(string extras)
        {
            string name = "p8s73w2_macro_" + Guid.NewGuid().ToString("N");
            bool[] frames = { false, true, true, false, false, true, false };

            var legacy = RunLogged(false, name, extras, frames, LogLevel.Trace);
            var migrated = RunLogged(true, name, extras, frames, LogLevel.Trace);

            Assert.Equal(legacy, migrated);
            Assert.Contains(legacy, l => l.Contains(extras == "RunOnRelease" ? "SpecialAction MacroRelease:" : "SpecialAction Macro:") && l.Contains("ActionEntries="));
            Assert.Contains(legacy, l => l.Contains("Trigger detected for SA"));
            Assert.Contains(legacy, l => l.Contains("maps to Type:Macro Details:65"));
        }

        [Fact]
        public void WhenOnlyWarningsAreEnabled_NothingIsLogged_OnEitherPath()
        {
            string name = "p8s73w2_off_" + Guid.NewGuid().ToString("N");
            bool[] frames = { true, true, false };

            Assert.Empty(RunLogged(false, name, string.Empty, frames, LogLevel.Warn));
            Assert.Empty(RunLogged(true, name, string.Empty, frames, LogLevel.Warn));
        }
    }
}
