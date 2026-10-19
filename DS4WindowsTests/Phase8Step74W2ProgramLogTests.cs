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
    /// Phase8-Step7 7.4-W2（Phase8-Step7-4-Plan.md Q1＝A）: 旧 <c>MapCustomAction</c> の Program の分岐が出していたログを、新経路が同じ文言・順序で出すことを確かめる。
    /// NLog の設定をテストの間だけ Trace 有効のメモリ出力へ差し替え、旧経路と新経路のログの行を比べる（7.3 の <c>Phase8Step73W2MacroLogTests</c> と同じ方式）。
    /// 実際のプログラムは起動しない（存在しない実行ファイルのパス）。他の Host 構築テストと同じコレクションで直列に実行する。
    /// </summary>
    [Collection("ServiceProviderHolder")]
    public class Phase8Step74W2ProgramLogTests
    {
        private static DS4State Frame(bool l1)
        {
            DS4State state = SpecialActionTestScope.Neutral();
            state.L1 = l1;
            return state;
        }

        private static bool IsProgramLogLine(string line, string name)
        {
            if (!line.Contains(name)) return false;
            return line.Contains("SpecialAction Program") || line.Contains("Trigger detected for SA") || line.Contains("maps to Type:Program")
                || line.Contains("Trigger keys for SA") || line.Contains("output combo") || line.Contains("LaunchProcessAction.Execute: id=");
        }

        private static List<string> RunLogged(bool migrated, string name, bool[] l1Frames, LogLevel minLevel)
        {
            LoggingConfiguration saved = LogManager.Configuration;
            var target = new MemoryTarget("p8s74w2") { Layout = "${level}|${message}" };
            var config = new LoggingConfiguration();
            config.AddTarget(target);
            config.AddRule(minLevel, LogLevel.Fatal, target);
            LogManager.Configuration = config;
            try
            {
                using (var t = new SpecialActionTestScope(migrated))
                {
                    SpecialAction sa = t.Add(new SpecialAction(name, "L1", "Program", @"C:\p8s74-nonexistent\" + name + ".exe", 0, string.Empty));
                    ActionInstanceState state = t.StateOf(sa);
                    state.RequiresFreshPressAfterReset = false;
                    state.IsExecuting = false;
                    foreach (bool l1 in l1Frames)
                        t.RunFrame(Frame(l1));
                }

                return target.Logs.Where(l => IsProgramLogLine(l, name)).ToList();
            }
            finally
            {
                LogManager.Configuration = saved;
            }
        }

        [Fact]
        public void TheProgramLogs_NewPathLogsTheSameLinesInTheSameOrder()
        {
            string name = "p8s74w2_prog_" + Guid.NewGuid().ToString("N");
            bool[] frames = { false, true, true, false, false, true, false };

            var legacy = RunLogged(false, name, frames, LogLevel.Trace);
            var migrated = RunLogged(true, name, frames, LogLevel.Trace);

            Assert.Equal(legacy, migrated);
            Assert.Contains(legacy, l => l.Contains("SpecialAction Program:") && l.Contains("ActionEntries="));
            Assert.Contains(legacy, l => l.Contains("Trigger detected for SA"));
            Assert.Contains(legacy, l => l.Contains("maps to Type:Program Details:"));
        }

        [Fact]
        public void WhenOnlyWarningsAreEnabled_NothingIsLogged_OnEitherPath()
        {
            string name = "p8s74w2_off_" + Guid.NewGuid().ToString("N");
            bool[] frames = { true, true, false };

            Assert.Empty(RunLogged(false, name, frames, LogLevel.Warn));
            Assert.Empty(RunLogged(true, name, frames, LogLevel.Warn));
        }
    }
}
