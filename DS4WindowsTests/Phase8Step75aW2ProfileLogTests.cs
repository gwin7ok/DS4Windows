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
    /// Phase8-Step7 7.5a-W2（Phase8-Step7-5-Plan.md Q4＝A）: 旧 <c>MapCustomAction</c> の Profile の分岐が出していたログを、新経路が同じ文言・順序で出すことを確かめる。
    /// NLog の設定をテストの間だけ Trace 有効のメモリ出力へ差し替え、旧経路と新経路のログの行を比べる（7.4 の <c>Phase8Step74W2ProgramLogTests</c> と同じ方式）。
    /// プロファイルの適用は、記録用の実装で置き換える。他の Host 構築テストと同じコレクションで直列に実行する。
    /// Phase8-Step8 8d（持ち越し C-2）: 旧経路は削除済み。以前の「旧・新の両経路の比べ」は、新経路の期待値の固定（比べを除いた、残りの検査）に変えた。
    /// </summary>
    [Collection("ServiceProviderHolder")]
    public class Phase8Step75aW2ProfileLogTests
    {
        private static DS4State Frame(bool l1)
        {
            DS4State state = SpecialActionTestScope.Neutral();
            state.L1 = l1;
            return state;
        }

        private static bool IsProfileLogLine(string line, string name)
        {
            // 「SpecialAction PROFILE: beingTriggered=…, useTempProfile=…」の行は、特殊アクションの名前を含まない（旧経路と同じ文言）
            if (line.Contains("SpecialAction PROFILE: beingTriggered=")) return true;
            if (!line.Contains(name)) return false;
            return line.Contains("SpecialAction PROFILE") || line.Contains("SpecialAction Profile:") || line.Contains("Trigger detected for SA")
                || line.Contains("maps to Type:Profile") || line.Contains("Trigger keys for SA") || line.Contains("output combo");
        }

        private static List<string> RunLogged(string name, bool[] l1Frames, LogLevel minLevel)
        {
            LoggingConfiguration saved = LogManager.Configuration;
            var target = new MemoryTarget("p8s75aw2") { Layout = "${level}|${message}" };
            var config = new LoggingConfiguration();
            config.AddTarget(target);
            config.AddRule(minLevel, LogLevel.Fatal, target);
            LogManager.Configuration = config;
            try
            {
                using (var t = new SpecialActionTestScope())
                {
                    t.InstallProfileRecorder();
                    SpecialAction sa = t.Add(new SpecialAction(name, "L1", "Profile", "p8s75-target", 0, string.Empty));
                    t.StateOf(sa).RequiresFreshPressAfterReset = false;
                    t.StateOf(sa).IsExecuting = false;
                    foreach (bool l1 in l1Frames)
                        t.RunFrame(Frame(l1));
                }

                return target.Logs.Where(l => IsProfileLogLine(l, name)).ToList();
            }
            finally
            {
                LogManager.Configuration = saved;
            }
        }

        [Fact]
        public void TheProfileLogs_NewPathLogsTheSameLinesInTheSameOrder()
        {
            string name = "p8s75aw2_profile_" + Guid.NewGuid().ToString("N");
            bool[] frames = { false, true, true, false, false };

            var result = RunLogged(name, frames, LogLevel.Trace);

            Assert.Contains(result, l => l.Contains("SpecialAction PROFILE: Triggered for device") && l.Contains("target=p8s75-target"));
            Assert.Contains(result, l => l.Contains("SpecialAction PROFILE: beingTriggered=False, useTempProfile="));
            Assert.Contains(result, l => l.Contains("SpecialAction Profile:") && l.Contains("ActionEntries="));
            Assert.Contains(result, l => l.Contains("maps to Type:Profile Details:p8s75-target"));
        }

        [Fact]
        public void WhenOnlyWarningsAreEnabled_NothingIsLogged()
        {
            string name = "p8s75aw2_off_" + Guid.NewGuid().ToString("N");
            bool[] frames = { true, true, false };

            Assert.Empty(RunLogged(name, frames, LogLevel.Warn));
            Assert.Empty(RunLogged(name, frames, LogLevel.Warn));
        }
    }
}
