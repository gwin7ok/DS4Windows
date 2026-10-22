using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using DS4Windows;
using DS4Windows.Actions;
using DS4Windows.DI;
using NLog;
using NLog.Config;
using NLog.Targets;
using Xunit;

namespace DS4WindowsTests
{
    /// <summary>
    /// Phase10-Step4c W2（Phase10-Step4c-Plan.md §2 項目 4・§4 W2、ユーザー承認 2026-10-21）: 立ち上がり（トリガー成立）と離したときのログを、
    /// Controls／SA の全種別で出す。立ち上がりのログは、出力アクションが出す種別（SA の全種別、Controls の Key／Macro／Special）と、
    /// エンジンが出す種別（Controls の Pad／Mouse／AnalogMouse／Extras。<see cref="BindingPolicy.LogsRisingEdge"/>）がある。離したときのログは、エンジンが全種別で出す
    /// （<see cref="BindingPolicy.LogsReleaseEdge"/>。素通しと Unbound の消すだけの束縛を除く）。既存のログの文言・順序は変えない（増やすだけ）。
    /// 各種別の立ち上がり・離したときが 1 回ずつ出ることは <c>Phase10Step4cW0LogTableTests</c> が確かめる。
    /// </summary>
    [Collection("ServiceProviderHolder")]
    public class Phase10Step4cW2EdgeLogTests
    {
        private const int Device = MappingCharacterizationScope.Device;

        private static List<string> Capture(Action action, LogLevel minLevel = null)
        {
            LoggingConfiguration saved = LogManager.Configuration;
            var target = new MemoryTarget("p10s4cw2") { Layout = "${level}|${message}" };
            var config = new LoggingConfiguration();
            config.AddTarget(target);
            config.AddRule(minLevel ?? LogLevel.Trace, LogLevel.Fatal, target);
            LogManager.Configuration = config;
            try
            {
                action();
                // バックグラウンドの Task（マクロの再生など）がまだログを書いていると、一覧の複製の最中に変わって例外になることがある。
                // 少し待って取り直す（ログの内容は変えない）
                for (int attempt = 0; ; attempt++)
                {
                    try { return target.Logs.ToList(); }
                    catch (ArgumentException) when (attempt < 10) { System.Threading.Thread.Sleep(20); }
                    catch (InvalidOperationException) when (attempt < 10) { System.Threading.Thread.Sleep(20); }
                }
            }
            finally
            {
                LogManager.Configuration = saved;
            }
        }

        private static void RunFor(ControlsKeyTestScope t, DS4State input, int milliseconds)
        {
            var sw = Stopwatch.StartNew();
            do
            {
                t.Run(input);
                Thread.Sleep(10);
            }
            while (sw.ElapsedMilliseconds < milliseconds);
        }

        [Fact]
        public void TheControlsPolicies_SayWhoLogsTheEdges()
        {
            using var t = new ControlsKeyTestScope();
            t.MapKey(DS4Controls.L1, 65);
            t.MapMacro(DS4Controls.R1, new[] { 65, 65 });
            t.MapButton(DS4Controls.Cross, X360Controls.A);
            t.MapButton(DS4Controls.Circle, X360Controls.LeftMouse);
            t.MapButton(DS4Controls.Square, X360Controls.MouseUp);
            t.MapButton(DS4Controls.Triangle, X360Controls.Unbound);   // 出力先が Unbound の消すだけの束縛
            t.MapExtras(DS4Controls.Share, "1,2,0,0,0,0,0,0,0");
            t.MapKey(DS4Controls.LXPos, 66);   // 反対側の向き（LXNeg）は、素通しの束縛になる

            IReadOnlyList<IActionBinding> bindings = TestHost.GetService<IControlBindingBuilder>().Build(Device);

            // (名前の接頭辞, ボタン, エンジンが立ち上がりを出す, エンジンが離したときを出す)
            var expected = new (string Prefix, DS4Controls Control, bool Rising, bool Release)[]
            {
                ("Synthetic_Key", DS4Controls.L1, false, true),            // Key の立ち上がりは、Key の出力アクションが出す
                ("Synthetic_Macro", DS4Controls.R1, false, true),          // Macro も同じ
                ("Synthetic_Pad", DS4Controls.Cross, true, true),
                ("Synthetic_Mouse", DS4Controls.Circle, true, true),
                ("Synthetic_AnalogMouse", DS4Controls.Square, true, true),
                ("Synthetic_Pad", DS4Controls.Triangle, false, false),     // Unbound の消すだけ
                ("Synthetic_Extras", DS4Controls.Share, true, true),
                ("Synthetic_PadPassthrough", DS4Controls.LXNeg, false, false),   // 素通し
            };

            foreach ((string prefix, DS4Controls control, bool rising, bool release) in expected)
            {
                string name = $"{prefix}_{Device}_{(int)control}";
                IActionBinding b = bindings.SingleOrDefault(x => x.Source.name == name);
                Assert.True(b != null, name + " の束縛がない");
                Assert.True(rising == b.Policy.LogsRisingEdge, name + " のエンジンの立ち上がりのログ");
                Assert.True(release == b.Policy.LogsReleaseEdge, name + " のエンジンの離したときのログ");
            }
        }

        [Fact]
        public void ThePassthroughAndTheUnboundErase_LogNothing()
        {
            List<string> logs = Capture(() =>
            {
                using var t = new ControlsKeyTestScope();
                t.MapButton(DS4Controls.Triangle, X360Controls.Unbound);
                t.MapKey(DS4Controls.LXPos, 66);
                t.PublishControlsOnly();

                DS4State pressed = ControlsKeyTestScope.Neutral();
                pressed.Triangle = true;
                pressed.LX = 255;
                RunFor(t, ControlsKeyTestScope.Neutral(), 100);
                RunFor(t, pressed, 300);
                RunFor(t, ControlsKeyTestScope.Neutral(), 100);
            });

            string unbound = $"Synthetic_Pad_{Device}_{(int)DS4Controls.Triangle}";
            string passthrough = $"Synthetic_PadPassthrough_{Device}_{(int)DS4Controls.LXNeg}";

            // 立ち上がり・離したときのログの行だけを調べる（状態の生成・消去のログは、束縛があれば出る）
            bool IsEdgeLog(string l, string name) => l.Contains(name) && (l.Contains("Trigger detected for SA") || l.Contains("Trigger released for SA")
                || l.Contains("SpecialAction ") || l.Contains("maps to") || l.Contains("Trigger keys for SA"));
            Assert.True(!logs.Any(l => IsEdgeLog(l, unbound)), string.Join(" | ", logs.Where(l => IsEdgeLog(l, unbound))));
            Assert.True(!logs.Any(l => IsEdgeLog(l, passthrough)), string.Join(" | ", logs.Where(l => IsEdgeLog(l, passthrough))));
        }

        [Fact]
        public void WhenOnlyWarningsAreEnabled_NothingIsLogged()
        {
            List<string> logs = Capture(() =>
            {
                using var t = new ControlsKeyTestScope();
                t.MapButton(DS4Controls.Cross, X360Controls.A);
                t.PublishControlsOnly();

                DS4State pressed = ControlsKeyTestScope.Neutral();
                pressed.Cross = true;
                RunFor(t, ControlsKeyTestScope.Neutral(), 50);
                RunFor(t, pressed, 150);
                RunFor(t, ControlsKeyTestScope.Neutral(), 50);
            }, LogLevel.Warn);

            Assert.DoesNotContain(logs, l => l.Contains("Synthetic_Pad_"));
        }

        [Fact]
        public void ASpecialActionMacro_LogsTheReleaseToo()
        {
            string name = "p10s4cw2_macro_" + Guid.NewGuid().ToString("N");
            IMacroPlayer player = TestHost.GetService<IMacroPlayer>();

            List<string> logs = Capture(() =>
            {
                using var t = new SpecialActionTestScope();
                SpecialAction sa = t.Add(new SpecialAction(name, "L1", "Macro", "65", 0, string.Empty));
                ActionInstanceState state = t.StateOf(sa);
                state.RequiresFreshPressAfterReset = false;
                state.IsExecuting = false;
                player.Stop(t.Device);
                foreach (bool l1 in new[] { false, true, true, false, false })
                {
                    DS4State frame = SpecialActionTestScope.Neutral();
                    frame.L1 = l1;
                    t.RunFrame(frame);
                }

                player.Stop(t.Device);
            });

            Assert.Equal(1, logs.Count(l => l.Contains("Trigger detected for SA '" + name + "'")));
            Assert.Equal(1, logs.Count(l => l.Contains("Trigger released for SA '" + name + "'")));
            Assert.Equal(1, logs.Count(l => l.Contains("SpecialAction Macro released: device=") && l.Contains("name=" + name)));
        }
    }
}
