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
    /// Phase8-Step7 7.6d（Phase8-Step7-6-Plan.md §3.3）: ステアリング較正（SASteeringWheelEmulationCalibrate）の新経路の確認。
    /// 実機のコントローラーがないため、旧経路は効果の分岐（<c>ctrl.DS4Controllers[device]</c>）で止まる。効果は記録用のポートで確かめる。
    /// 3 秒の間隔は、<c>SpecialAction.firstTap</c>（旧経路と同じ項目）を直接書き換えて再現する（待たない）。
    /// 他の Host 構築テストと同じコレクションで直列に実行する。
    /// </summary>
    [Collection("ServiceProviderHolder")]
    public class Phase8Step76dSteeringWheelCalibrateTests
    {
        private static DS4State Frame(bool l1, bool r1 = false)
        {
            DS4State state = SpecialActionTestScope.Neutral();
            state.L1 = l1;
            state.R1 = r1;
            return state;
        }

        private static ActionBindingBuilder NewBuilder(IWheelCalibrationPort port)
            => new ActionBindingBuilder(TestHost.GetService<IProfileActionProvider>(), TestHost.GetService<IProfileSettingsService>(),
                TestHost.GetService<IManagedActionManager>(), TestHost.GetService<IMacroPlayer>(), TestHost.GetService<IProcessLauncher>(),
                TestHost.GetService<IProfileSwitcher>(), TestHost.GetService<BindingMigrationOptions>(), TestHost.GetService<ISpecialActionRepository>(),
                TestHost.GetService<Func<IMapping>>(), null, null, null, port);

        private static void Publish(SpecialActionTestScope t, IWheelCalibrationPort port)
            => TestHost.GetService<IActionRegistry>().Replace(t.Device, NewBuilder(port).Build(t.Device));

        private static SpecialAction AddWheel(SpecialActionTestScope t, string triggers, IWheelCalibrationPort port, bool freshPressGuard = false)
        {
            SpecialAction sa = t.Add(new SpecialAction("p8s76d_wheel_" + Guid.NewGuid().ToString("N"), triggers, "SASteeringWheelEmulationCalibrate", string.Empty, 0, string.Empty));
            ActionInstanceState state = t.StateOf(sa);
            state.RequiresFreshPressAfterReset = freshPressGuard;
            state.IsExecuting = false;
            Publish(t, port);
            return sa;
        }

        [Fact]
        public void AtTheRisingEdge_AWaitingController_StartsTheCalibration_OnceUntilItMovesOn()
        {
            using var t = new SpecialActionTestScope(true);
            var port = new RecordingWheelCalibrationPort { State = 0 };
            SpecialAction sa = AddWheel(t, "L1", port);
            Assert.True(t.Engine.Owns(t.Device, sa));
            DateTime before = DateTime.UtcNow;

            t.RunFrame(Frame(false));
            Assert.Empty(port.Writes);

            t.RunFrame(Frame(true));
            Assert.Equal(new[] { 1 }, port.Writes.ToArray());   // 0 → 1（較正の開始）。成立の最初のフレームから判定する
            Assert.True(sa.firstTap >= before);                  // 3 秒の間隔の基準を更新する

            t.RunFrame(Frame(true));
            t.RunFrame(Frame(true));
            Assert.Equal(new[] { 1 }, port.Writes.ToArray());   // 状態が 1 の間は、何もしない（Mapping の較正の処理が 2 へ進める）
        }

        [Fact]
        public void WhenTheCalibrationIsRunning_AHeldTriggerCompletesIt_AfterThreeSeconds()
        {
            using var t = new SpecialActionTestScope(true);
            var port = new RecordingWheelCalibrationPort { State = 2 };   // 較正の中（確定の待ち）
            SpecialAction sa = AddWheel(t, "L1", port);
            sa.firstTap = DateTime.UtcNow;   // 直前に操作した

            t.RunFrame(Frame(true));
            t.RunFrame(Frame(true));
            Assert.Empty(port.Writes);        // 3 秒たつまで、確定しない

            sa.firstTap = DateTime.UtcNow.AddSeconds(-10);
            t.RunFrame(Frame(true));
            Assert.Equal(new[] { 3 }, port.Writes.ToArray());   // 2 → 3（較正の確定）
        }

        [Fact]
        public void WithinThreeSecondsOfTheLastOperation_NothingStarts()
        {
            using var t = new SpecialActionTestScope(true);
            var port = new RecordingWheelCalibrationPort { State = 0 };
            SpecialAction sa = AddWheel(t, "L1", port);
            sa.firstTap = DateTime.UtcNow;

            t.RunFrame(Frame(true));
            t.RunFrame(Frame(true));

            Assert.Empty(port.Writes);
        }

        [Fact]
        public void AfterTheTriggerIsReleased_ThePressStartsAgainWhenTheControllerIsWaiting()
        {
            using var t = new SpecialActionTestScope(true);
            var port = new RecordingWheelCalibrationPort { State = 0 };
            SpecialAction sa = AddWheel(t, "L1", port);

            t.RunFrame(Frame(true));
            Assert.Equal(new[] { 1 }, port.Writes.ToArray());

            t.RunFrame(Frame(false));
            Assert.Equal(new[] { 1 }, port.Writes.ToArray());   // 離したときは何もしない

            port.State = 0;                                      // 較正が終わって待機に戻った
            sa.firstTap = DateTime.UtcNow.AddSeconds(-10);
            t.RunFrame(Frame(true));
            Assert.Equal(new[] { 1, 1 }, port.Writes.ToArray());
        }

        [Fact]
        public void WhenTheControllerIsMissing_NothingIsWritten_AndItDoesNotThrow()
        {
            using var t = new SpecialActionTestScope(true);
            var port = new RecordingWheelCalibrationPort { State = -1 };
            AddWheel(t, "L1", port);

            t.RunFrame(Frame(true));
            t.RunFrame(Frame(true));

            Assert.Empty(port.Writes);
        }

        [Fact]
        public void WhileTheFreshPressGuardIsOn_NothingHappens()
        {
            using var t = new SpecialActionTestScope(true);
            var port = new RecordingWheelCalibrationPort { State = 0 };
            AddWheel(t, "L1", port, freshPressGuard: true);

            t.RunFrame(Frame(true));
            t.RunFrame(Frame(true));

            Assert.Empty(port.Writes);
        }

        [Fact]
        public void AndCondition_NeedsAllTheTriggers()
        {
            using var t = new SpecialActionTestScope(true);
            var port = new RecordingWheelCalibrationPort { State = 0 };
            AddWheel(t, "L1/R1", port);

            t.RunFrame(Frame(true, false));
            t.RunFrame(Frame(false, true));
            Assert.Empty(port.Writes);

            t.RunFrame(Frame(true, true));
            Assert.Equal(new[] { 1 }, port.Writes.ToArray());
        }

        // ---- ビルダー・スイッチ ----

        [Fact]
        public void TheBuilder_BuildsTheBindingOnlyWithAPort()
        {
            using var t = new SpecialActionTestScope(true);
            var port = new RecordingWheelCalibrationPort();
            SpecialAction sa = AddWheel(t, "L1", port);

            IActionBinding binding = Assert.Single(NewBuilder(port).Build(t.Device));
            Assert.Same(sa, binding.Source);
            Assert.IsType<SteeringWheelCalibrateAction>(Assert.Single(binding.Outputs));
            Assert.True(binding.Policy.RespectsFreshPressGuard);
            Assert.False(binding.Policy.BlocksWhileExecuting);
            Assert.Equal(UntriggerPolicy.None, binding.Policy.Untrigger);

            Assert.Empty(NewBuilder(null).Build(t.Device));   // ポートがなければ、束縛を作らず、旧経路が処理する
        }

        [Fact]
        public void RemovingTheLine_NoBindingIsBuilt_AndNothingHappens()
        {
            var options = TestHost.GetService<BindingMigrationOptions>();
            var builder = TestHost.GetService<IActionBindingBuilder>();
            var saved = options.MigratedTypes.ToArray();
            try
            {
                using var t = new SpecialActionTestScope(true);
                SpecialAction sa = AddWheel(t, "L1", new RecordingWheelCalibrationPort());
                Assert.Single(builder.Build(t.Device));   // 本番のビルダー（本番のポート）でも束縛が作られる

                options.MigratedTypes.Remove(SpecialAction.ActionTypeId.SASteeringWheelEmulationCalibrate);   // 既定の一覧からステアリング較正だけを外した状態

                Assert.Empty(builder.Build(t.Device));
                t.PublishBindings();
                Assert.False(t.Engine.Owns(t.Device, sa));
            }
            finally
            {
                options.MigratedTypes.Clear();
                foreach (var type in saved) options.MigratedTypes.Add(type);
            }
        }

        // ---- ログ ----

        private static List<string> RunLogged(string name, LogLevel minLevel, RecordingWheelCalibrationPort port)
        {
            LoggingConfiguration saved = LogManager.Configuration;
            var target = new MemoryTarget("p8s76d") { Layout = "${level}|${message}" };
            var config = new LoggingConfiguration();
            config.AddTarget(target);
            config.AddRule(minLevel, LogLevel.Fatal, target);
            LogManager.Configuration = config;
            try
            {
                using (var t = new SpecialActionTestScope(true))
                {
                    SpecialAction sa = t.Add(new SpecialAction(name, "L1", "SASteeringWheelEmulationCalibrate", string.Empty, 0, string.Empty));
                    t.StateOf(sa).RequiresFreshPressAfterReset = false;
                    Publish(t, port);

                    t.RunFrame(Frame(false));
                    t.RunFrame(Frame(true));    // 0 → 1（ログ 4 行）
                    t.RunFrame(Frame(true));    // 状態 1: 何もしない（ログなし）
                    port.State = 2;
                    sa.firstTap = DateTime.UtcNow.AddSeconds(-10);
                    t.RunFrame(Frame(true));    // 2 → 3（ログ 4 行）
                    t.RunFrame(Frame(true));    // 状態 3: 何もしない（ログなし）
                }

                return target.Logs.Where(l => l.Contains(name)
                    && (l.Contains("Trigger detected for SA") || l.Contains("maps to Type") || l.Contains("Trigger keys for SA")
                        || l.Contains("output combo") || l.Contains("SpecialAction "))).ToList();
            }
            finally
            {
                LogManager.Configuration = saved;
            }
        }

        [Fact]
        public void EachStateChange_LogsTheFourTraceLines_AndThereIsNoDebugLine()
        {
            string name = "p8s76d_log_" + Guid.NewGuid().ToString("N");
            var port = new RecordingWheelCalibrationPort { State = 0 };

            List<string> lines = RunLogged(name, LogLevel.Trace, port);

            Assert.Equal(new[] { 1, 3 }, port.Writes.ToArray());
            Assert.Equal(8, lines.Count);   // 状態を変えるたびに Trace 4 行（保持中の、何もしないフレームでは出さない）
            Assert.Equal(lines.Take(4).ToList(), lines.Skip(4).Take(4).ToList());
            Assert.Contains(lines, l => l.StartsWith("Trace|Trigger detected for SA") && l.Contains("on device"));
            Assert.Contains(lines, l => l.Contains("maps to Type:SASteeringWheelEmulationCalibrate"));
            Assert.Contains(lines, l => l.Contains("Trigger keys for SA") && l.EndsWith("<none>"));
            Assert.Contains(lines, l => l.Contains("output combo: "));
            Assert.DoesNotContain(lines, l => l.StartsWith("Debug|"));
        }

        [Fact]
        public void WhenOnlyWarningsAreEnabled_NothingIsLogged()
        {
            string name = "p8s76d_off_" + Guid.NewGuid().ToString("N");

            Assert.Empty(RunLogged(name, LogLevel.Warn, new RecordingWheelCalibrationPort { State = 0 }));
        }

        // ---- 割り当て ----

        private static long AllocatedWhileHeld(SpecialActionTestScope t)
        {
            DS4State held = Frame(true);
            for (int i = 0; i < 300; i++) t.RunFrame(held);   // 立ち上がり（状態の変更）と JIT は、測定の外へ

            long before = GC.GetAllocatedBytesForCurrentThread();
            for (int i = 0; i < 1000; i++) t.RunFrame(held);
            return GC.GetAllocatedBytesForCurrentThread() - before;
        }

        [Fact]
        public void TheNewPath_DoesNotAddAllocationsToTheFrame_WhileHeld()
        {
            // 同じ件数のステアリング較正で、トリガーが押されていない（R1。押すのは L1）場合を基準にする（Phase8Step76aGyroCalibrateTests と同じ方式）
            long baseline;
            using (var t = new SpecialActionTestScope(true))
            {
                AddWheel(t, "R1", new RecordingWheelCalibrationPort { State = 0 });
                baseline = AllocatedWhileHeld(t);
            }

            long withWheel;
            var port = new RecordingWheelCalibrationPort { State = 0 };
            using (var t = new SpecialActionTestScope(true))
            {
                AddWheel(t, "L1", port);
                withWheel = AllocatedWhileHeld(t);
            }

            Assert.True(withWheel <= baseline + 4096, $"押している間: あり {withWheel} B > 基準 {baseline} B");
            Assert.Equal(new[] { 1 }, port.Writes.ToArray());
        }

        // ---- 本番のポート ----

        [Fact]
        public void TheDefaultPort_DoesNothingWhenThereIsNoController()
        {
            var port = new DefaultWheelCalibrationPort(() => new NoControllerAccessor());

            Assert.Equal(-1, port.GetCalibrationState(0));
            port.SetCalibrationState(0, 1);
            Assert.Equal(-1, port.GetCalibrationState(-1));
            Assert.Equal(-1, port.GetCalibrationState(99));

            var nullAccessor = new DefaultWheelCalibrationPort(() => null);
            Assert.Equal(-1, nullAccessor.GetCalibrationState(0));
            nullAccessor.SetCalibrationState(0, 1);
        }

        private sealed class NoControllerAccessor : IDeviceStateAccessor
        {
            public DS4Device GetController(int deviceIndex) => null;
        }
    }
}
