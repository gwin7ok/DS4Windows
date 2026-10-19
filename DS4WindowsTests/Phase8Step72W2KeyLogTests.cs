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
    /// Phase8-Step7 7.2-W2（Phase8-Step7-2-Plan.md Q3＝A）: 旧 <c>MapCustomAction</c> の Key の分岐が出していた Debug／Trace のログを、新経路が同じ文言・順序で出すことを確かめる。
    /// NLog の設定をテストの間だけ Trace 有効のメモリ出力へ差し替え、旧経路と新経路のログの行を比べる（7.1 の <c>Phase8Step7W2ButtonTraceLogTests</c> と同じ方式）。
    /// 他の Host 構築テストと同じコレクションで直列に実行する。
    /// </summary>
    [Collection("ServiceProviderHolder")]
    public class Phase8Step72W2KeyLogTests
    {
        private static DS4State Frame(bool l1)
        {
            DS4State state = SpecialActionTestScope.Neutral();
            state.L1 = l1;
            return state;
        }

        private static bool IsKeyLogLine(string line, string name)
        {
            if (!line.Contains(name)) return false;
            return line.Contains("SpecialAction KEY entry") || line.Contains("SpecialAction KeyTriggered") || line.Contains("SpecialAction KEY triggered")
                || line.Contains("SpecialAction KeyReleased") || line.Contains("Trigger detected for SA") || line.Contains("maps to Type:Key")
                || line.Contains("Trigger keys for SA") || line.Contains("output combo");
        }

        private static List<string> RunLogged(bool migrated, string name, bool toggle, bool[] l1Frames, LogLevel minLevel)
        {
            LoggingConfiguration saved = LogManager.Configuration;
            var target = new MemoryTarget("p8s72w2") { Layout = "${level}|${message}" };
            var config = new LoggingConfiguration();
            config.AddTarget(target);
            config.AddRule(minLevel, LogLevel.Fatal, target);
            LogManager.Configuration = config;
            try
            {
                using (var t = new SpecialActionTestScope(migrated))
                {
                    SpecialAction sa = t.Add(new SpecialAction(name, "L1", "Key", "65", 0, (toggle ? "Toggle" : "Press") + "\n"));
                    ActionInstanceState state = t.StateOf(sa);
                    state.IsToggledOn = false;
                    state.RequiresFreshPressAfterReset = false;
                    foreach (bool l1 in l1Frames)
                        t.RunFrame(Frame(l1));
                }

                return target.Logs.Where(l => IsKeyLogLine(l, name)).ToList();
            }
            finally
            {
                LogManager.Configuration = saved;
            }
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void TheKeyLogs_NewPathLogsTheSameLinesInTheSameOrder(bool toggle)
        {
            string name = "p8s72w2_key_" + Guid.NewGuid().ToString("N");
            bool[] frames = { false, true, true, false, false, true, false };

            var legacy = RunLogged(false, name, toggle, frames, LogLevel.Trace);
            var migrated = RunLogged(true, name, toggle, frames, LogLevel.Trace);

            Assert.Equal(legacy, migrated);
            Assert.Contains(legacy, l => l.Contains("SpecialAction KEY entry"));
            Assert.Contains(legacy, l => l.Contains("SpecialAction KeyTriggered") && l.Contains("ActionEntries="));
            Assert.Contains(legacy, l => l.Contains("SpecialAction KEY triggered") && l.Contains("key=65"));
            Assert.Contains(legacy, l => l.Contains("Trigger detected for SA"));
            Assert.Contains(legacy, l => l.Contains("maps to Type:Key Details:65"));
        }

        [Fact]
        public void APressKey_LogsTheReleaseToo()
        {
            string name = "p8s72w2_press_" + Guid.NewGuid().ToString("N");

            var legacy = RunLogged(false, name, false, new[] { true, true, false }, LogLevel.Debug);

            Assert.Contains(legacy, l => l.Contains("SpecialAction KeyReleased") && l.Contains("released=L1"));
        }

        [Fact]
        public void WhenOnlyWarningsAreEnabled_NothingIsLogged_OnEitherPath()
        {
            string name = "p8s72w2_off_" + Guid.NewGuid().ToString("N");
            bool[] frames = { true, true, false };

            Assert.Empty(RunLogged(false, name, false, frames, LogLevel.Warn));
            Assert.Empty(RunLogged(true, name, false, frames, LogLevel.Warn));
        }
    }
}
