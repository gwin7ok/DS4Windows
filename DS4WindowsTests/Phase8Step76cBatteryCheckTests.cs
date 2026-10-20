using System;
using System.Collections.Generic;
using System.Linq;
using DS4Windows;
using DS4Windows.Actions;
using DS4Windows.DI;
using DS4Windows.Services;
using NLog;
using NLog.Config;
using NLog.Targets;
using Xunit;

namespace DS4WindowsTests
{
    /// <summary>
    /// Phase8-Step7 7.6c（Phase8-Step7-6-Plan.md §3.3。Q6＝A: 静的な窓口はポートの内側／Q8＝B: コピーの名残りのログを削除）: BatteryCheck の新経路の確認。
    /// 実機のコントローラーがないため、旧経路は効果の分岐（<c>ctrl.DS4Controllers[device]</c>）で止まる。効果は記録用のポートで確かめる。
    /// 他の Host 構築テストと同じコレクションで直列に実行する。
    /// </summary>
    [Collection("ServiceProviderHolder")]
    public class Phase8Step76cBatteryCheckTests
    {
        private const string FullDetails = "|True|True|255|0|0|0|255|0";   // 通知あり、ライトバーあり、空＝赤、満タン＝緑

        private static DS4State Frame(bool l1, bool r1 = false)
        {
            DS4State state = SpecialActionTestScope.Neutral();
            state.L1 = l1;
            state.R1 = r1;
            return state;
        }

        private static ActionBindingBuilder NewBuilder(IBatteryCheckPort port)
            => new ActionBindingBuilder(TestHost.GetService<IProfileActionProvider>(), TestHost.GetService<IProfileSettingsService>(),
                TestHost.GetService<IManagedActionManager>(), TestHost.GetService<IMacroPlayer>(), TestHost.GetService<IProcessLauncher>(),
                TestHost.GetService<IProfileSwitcher>(), TestHost.GetService<ISpecialActionRepository>(),
                TestHost.GetService<Func<IMapping>>(), null, null, port);

        private static void Publish(SpecialActionTestScope t, IBatteryCheckPort port)
            => TestHost.GetService<IActionRegistry>().Replace(t.Device, NewBuilder(port).Build(t.Device));

        private static SpecialAction AddBattery(SpecialActionTestScope t, string triggers, IBatteryCheckPort port, string details = FullDetails, bool freshPressGuard = false)
        {
            SpecialAction sa = t.Add(new SpecialAction("p8s76c_bat_" + Guid.NewGuid().ToString("N"), triggers, "BatteryCheck", details, 0, string.Empty));
            ActionInstanceState state = t.StateOf(sa);
            state.RequiresFreshPressAfterReset = freshPressGuard;
            state.IsExecuting = false;
            Publish(t, port);
            return sa;
        }

        private static DS4Color Transition(DS4Color from, DS4Color to, double ratio) => ColorUtil.getTransitionedColor(ref from, ref to, ratio);

        [Fact]
        public void TheRisingEdge_NotifiesSavesTheColorAndStartsTheFade()
        {
            using var t = new SpecialActionTestScope();
            var port = new RecordingBatteryCheckPort();
            SpecialAction sa = AddBattery(t, "L1", port);
            Assert.True(t.Engine.Owns(t.Device, sa));

            t.RunFrame(Frame(false));
            Assert.Empty(port.Events);

            t.RunFrame(Frame(true));

            Assert.Equal(new[] { "notify:" + t.Device, "force:on", "color" }, port.Events.ToArray());   // 旧経路と同じ順序: 通知 → 強制色を有効に → 色の遷移の 1 ステップ
            DS4Color trans = Transition(new DS4Color(255, 0, 0), new DS4Color(0, 255, 0), 50);
            Assert.Equal(Transition(port.LightBar, trans, 2), port.Colors[0]);
        }

        [Fact]
        public void WhileHeld_TheFadeAdvancesByTwoEveryFrame_UpToOneHundred_AndThenStops()
        {
            using var t = new SpecialActionTestScope();
            var port = new RecordingBatteryCheckPort();
            AddBattery(t, "L1", port);

            for (int i = 0; i < 70; i++) t.RunFrame(Frame(true));

            Assert.Equal(50, port.Colors.Count);   // 2 ずつ、100 まで（50 ステップ）。それ以降は書かない
            DS4Color trans = Transition(new DS4Color(255, 0, 0), new DS4Color(0, 255, 0), 50);
            Assert.Equal(Transition(port.LightBar, trans, 2), port.Colors[0]);
            Assert.Equal(Transition(port.LightBar, trans, 4), port.Colors[1]);
            Assert.Equal(Transition(port.LightBar, trans, 100), port.Colors[49]);
            Assert.Equal(1, port.Events.Count(e => e.StartsWith("notify")));   // 通知は立ち上がりの 1 回だけ
        }

        [Fact]
        public void OnRelease_TheFadeAndTheForcedColorAreReset_AndTheNextPressStartsOver()
        {
            using var t = new SpecialActionTestScope();
            var port = new RecordingBatteryCheckPort();
            AddBattery(t, "L1", port);

            t.RunFrame(Frame(true));
            t.RunFrame(Frame(true));
            Assert.Equal(2, port.Colors.Count);

            t.RunFrame(Frame(false));
            Assert.Equal("force:off", port.Events.Last());

            port.Events.Clear();
            port.Colors.Clear();
            t.RunFrame(Frame(true));
            Assert.Equal(new[] { "notify:" + t.Device, "force:on", "color" }, port.Events.ToArray());   // 押し直すと、最初から
            DS4Color trans = Transition(new DS4Color(255, 0, 0), new DS4Color(0, 255, 0), 50);
            Assert.Equal(Transition(port.LightBar, trans, 2), port.Colors[0]);
        }

        [Fact]
        public void TheFlagsInTheDetails_TurnTheNotificationAndTheLightBarOffIndependently()
        {
            using (var t = new SpecialActionTestScope())
            {
                var port = new RecordingBatteryCheckPort();
                AddBattery(t, "L1", port, "|False|True|255|0|0|0|255|0");
                t.RunFrame(Frame(true));
                Assert.Equal(new[] { "force:on", "color" }, port.Events.ToArray());   // 通知なし
            }

            using (var t = new SpecialActionTestScope())
            {
                var port = new RecordingBatteryCheckPort();
                AddBattery(t, "L1", port, "|True|False|0|0|0|0|0|0");
                t.RunFrame(Frame(true));
                t.RunFrame(Frame(true));
                Assert.Equal(new[] { "notify:" + t.Device }, port.Events.ToArray());   // ライトバーなし
                t.RunFrame(Frame(false));
                Assert.Equal("force:off", port.Events.Last());                           // 離したときの戻しは、旧経路と同じく、フラグによらず行う
            }
        }

        [Fact]
        public void WithoutAController_NothingIsDrawn()
        {
            using var t = new SpecialActionTestScope();
            var port = new RecordingBatteryCheckPort { Present = false };
            AddBattery(t, "L1", port);

            t.RunFrame(Frame(true));
            t.RunFrame(Frame(true));

            Assert.Equal(new[] { "notify:" + t.Device }, port.Events.ToArray());   // 通知だけ（旧経路は、コントローラーがなければ色の処理で例外になる）
            Assert.Equal(0, port.ColorWrites);
        }

        [Fact]
        public void AnInvalidDetails_DoesNothing_AndDoesNotThrow()
        {
            using var t = new SpecialActionTestScope();
            var port = new RecordingBatteryCheckPort();
            AddBattery(t, "L1", port, "|maybe|True|255|0|0|0|255|0");

            t.RunFrame(Frame(true));
            t.RunFrame(Frame(true));

            Assert.Empty(port.Events);
        }

        [Fact]
        public void WhileTheFreshPressGuardIsOn_NothingHappens()
        {
            using var t = new SpecialActionTestScope();
            var port = new RecordingBatteryCheckPort();
            AddBattery(t, "L1", port, freshPressGuard: true);

            t.RunFrame(Frame(true));
            t.RunFrame(Frame(true));

            Assert.Empty(port.Events);
        }

        [Fact]
        public void AndCondition_NeedsAllTheTriggers()
        {
            using var t = new SpecialActionTestScope();
            var port = new RecordingBatteryCheckPort();
            AddBattery(t, "L1/R1", port);

            t.RunFrame(Frame(true, false));
            t.RunFrame(Frame(false, true));
            Assert.Empty(port.Events);

            t.RunFrame(Frame(true, true));
            Assert.Contains("force:on", port.Events);
        }

        // ---- ビルダー・スイッチ ----

        [Fact]
        public void TheBuilder_BuildsTheBindingOnlyWithAPort()
        {
            using var t = new SpecialActionTestScope();
            var port = new RecordingBatteryCheckPort();
            SpecialAction sa = AddBattery(t, "L1", port);

            IActionBinding binding = Assert.Single(NewBuilder(port).Build(t.Device));
            Assert.Same(sa, binding.Source);
            Assert.IsType<BatteryCheckAction>(Assert.Single(binding.Outputs));
            Assert.True(binding.Policy.RespectsFreshPressGuard);
            Assert.True(binding.Policy.StopOnRelease);
            Assert.False(binding.Policy.BlocksWhileExecuting);
            Assert.Equal(UntriggerPolicy.None, binding.Policy.Untrigger);

            Assert.Empty(NewBuilder(null).Build(t.Device));   // ポートがなければ、束縛を作らず、旧経路が処理する
        }

        // ---- ログ ----

        private static List<string> RunLogged(string name, LogLevel minLevel, RecordingBatteryCheckPort port)
        {
            LoggingConfiguration saved = LogManager.Configuration;
            var target = new MemoryTarget("p8s76c") { Layout = "${level}|${message}" };
            var config = new LoggingConfiguration();
            config.AddTarget(target);
            config.AddRule(minLevel, LogLevel.Fatal, target);
            LogManager.Configuration = config;
            try
            {
                using (var t = new SpecialActionTestScope())
                {
                    SpecialAction sa = t.Add(new SpecialAction(name, "L1", "BatteryCheck", FullDetails, 0, string.Empty));
                    t.StateOf(sa).RequiresFreshPressAfterReset = false;
                    Publish(t, port);
                    foreach (bool l1 in new[] { false, true, true, true, false })
                        t.RunFrame(Frame(l1));
                }

                return target.Logs.Where(l => l.Contains(name)
                    && (l.Contains("Trigger detected for SA") || l.Contains("maps to Type") || l.Contains("Trigger keys for SA")
                        || l.Contains("output combo") || l.Contains("SpecialAction BatteryCheck:") || l.Contains("WheelRecalibrate"))).ToList();
            }
            finally
            {
                LogManager.Configuration = saved;
            }
        }

        [Fact]
        public void TheRisingEdge_LogsTheFourTraceLinesAndOneDebugLine_AndNoWheelRecalibrateLine()
        {
            string name = "p8s76c_log_" + Guid.NewGuid().ToString("N");

            List<string> lines = RunLogged(name, LogLevel.Trace, new RecordingBatteryCheckPort());

            Assert.Equal(5, lines.Count);   // 立ち上がりの 1 回だけ: Trace 4 行 + Debug 1 行（保持中は出さない）
            Assert.Contains(lines, l => l.StartsWith("Trace|Trigger detected for SA") && l.Contains("on device"));
            Assert.Contains(lines, l => l.Contains("maps to Type:BatteryCheck"));
            Assert.Contains(lines, l => l.Contains("Trigger keys for SA") && l.EndsWith("<none>"));
            Assert.Contains(lines, l => l.Contains("output combo: "));
            Assert.Contains(lines, l => l.StartsWith("Debug|SpecialAction BatteryCheck: device=") && l.Contains($"name={name}") && l.Contains("ActionEntries="));
            Assert.DoesNotContain(lines, l => l.Contains("WheelRecalibrate"));   // Q8＝B: コピーの名残りのログは削除した
        }

        [Fact]
        public void WhenOnlyWarningsAreEnabled_NothingIsLogged()
        {
            string name = "p8s76c_off_" + Guid.NewGuid().ToString("N");

            Assert.Empty(RunLogged(name, LogLevel.Warn, new RecordingBatteryCheckPort()));
        }

        // ---- 割り当て ----

        private static long AllocatedWhileHeld(SpecialActionTestScope t)
        {
            DS4State held = Frame(true);
            for (int i = 0; i < 300; i++) t.RunFrame(held);   // 立ち上がり、フェードの完了（50 フレーム）、JIT は、測定の外へ

            long before = GC.GetAllocatedBytesForCurrentThread();
            for (int i = 0; i < 1000; i++) t.RunFrame(held);
            return GC.GetAllocatedBytesForCurrentThread() - before;
        }

        [Fact]
        public void TheNewPath_DoesNotAddAllocationsToTheFrame_WhileHeld()
        {
            // 同じ件数の BatteryCheck で、トリガーが押されていない（R1。押すのは L1）場合を基準にする（Phase8Step76aGyroCalibrateTests と同じ方式）
            long baseline;
            using (var t = new SpecialActionTestScope())
            {
                AddBattery(t, "R1", new RecordingBatteryCheckPort { Record = false });
                baseline = AllocatedWhileHeld(t);
            }

            long withBattery;
            var port = new RecordingBatteryCheckPort { Record = false };
            using (var t = new SpecialActionTestScope())
            {
                AddBattery(t, "L1", port);
                withBattery = AllocatedWhileHeld(t);
            }

            Assert.True(withBattery <= baseline + 4096, $"押している間: あり {withBattery} B > 基準 {baseline} B");
            Assert.Equal(50, port.ColorWrites);   // フェードは 50 ステップで終わる
        }

        // ---- 本番のポート ----

        [Fact]
        public void TheDefaultPort_DoesNothingWhenThereIsNoController()
        {
            var port = new DefaultBatteryCheckPort(() => new NoControllerAccessor(), () => null);

            Assert.False(port.HasController(0));
            port.NotifyBattery(0);   // ControlService がなくても、例外にしない
            Assert.Equal(default(DS4Color), port.GetLightBarColor(0));
            Assert.Equal(0, port.GetBattery(0));
            port.SetForceLight(-1, true);
            port.SetForcedColor(99, new DS4Color(1, 2, 3));   // 範囲外は何もしない

            var nullAccessor = new DefaultBatteryCheckPort(() => null, () => null);
            Assert.False(nullAccessor.HasController(0));
        }

        [Fact]
        public void TheDefaultPort_WritesTheLightBarStatics_ForTheGivenDevice()
        {
            var port = new DefaultBatteryCheckPort(() => new NoControllerAccessor(), () => null);
            int device = 6;
            bool savedLight = DS4LightBar.forcelight[device];
            DS4Color savedColor = DS4LightBar.forcedColor[device];
            try
            {
                port.SetForceLight(device, true);
                port.SetForcedColor(device, new DS4Color(1, 2, 3));

                Assert.True(DS4LightBar.forcelight[device]);
                Assert.Equal(new DS4Color(1, 2, 3), DS4LightBar.forcedColor[device]);

                port.SetForceLight(device, false);
                Assert.False(DS4LightBar.forcelight[device]);
            }
            finally
            {
                DS4LightBar.forcelight[device] = savedLight;
                DS4LightBar.forcedColor[device] = savedColor;
            }
        }

        private sealed class NoControllerAccessor : IDeviceStateAccessor
        {
            public DS4Device GetController(int deviceIndex) => null;
        }
    }
}
