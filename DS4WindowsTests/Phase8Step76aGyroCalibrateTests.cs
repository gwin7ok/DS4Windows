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
    /// Phase8-Step7 7.6a（Phase8-Step7-6-Plan.md §3.1。Q2＝A）: GyroCalibrate の新経路の確認。
    /// 実機のコントローラーがないため、旧経路は効果の分岐（<c>ctrl.DS4Controllers[device]</c>）で止まる。そのため、効果は記録用のポートで確かめ、
    /// 旧経路とは、立ち上がりのログの文言（Trace の 4 行）だけを比べる。ポートの本番の実装は、コントローラーがいない場合の安全性を確かめる。
    /// 他の Host 構築テストと同じコレクションで直列に実行する。
    /// </summary>
    [Collection("ServiceProviderHolder")]
    public class Phase8Step76aGyroCalibrateTests
    {
        private static DS4State Frame(bool l1, bool r1 = false)
        {
            DS4State state = SpecialActionTestScope.Neutral();
            state.L1 = l1;
            state.R1 = r1;
            return state;
        }

        private static ActionBindingBuilder NewBuilder(IGyroCalibrationPort port)
            => new ActionBindingBuilder(TestHost.GetService<IProfileActionProvider>(), TestHost.GetService<IProfileSettingsService>(),
                TestHost.GetService<IManagedActionManager>(), TestHost.GetService<IMacroPlayer>(), TestHost.GetService<IProcessLauncher>(),
                TestHost.GetService<IProfileSwitcher>(), TestHost.GetService<ISpecialActionRepository>(),
                TestHost.GetService<Func<IMapping>>(), port);

        /// <summary>登録した GyroCalibrate の特殊アクションの束縛を、記録用のポートを持つビルダーで作り直して登録簿へ差し替える。</summary>
        private static SpecialAction AddGyro(SpecialActionTestScope t, string triggers, RecordingGyroCalibrationPort port, bool freshPressGuard = false)
        {
            SpecialAction sa = t.Add(new SpecialAction("p8s76a_gyro_" + Guid.NewGuid().ToString("N"), triggers, "GyroCalibration", string.Empty, 0, string.Empty));
            ActionInstanceState state = t.StateOf(sa);
            state.RequiresFreshPressAfterReset = freshPressGuard;
            state.IsExecuting = false;
            TestHost.GetService<IActionRegistry>().Replace(t.Device, NewBuilder(port).Build(t.Device));
            return sa;
        }

        [Fact]
        public void Calibrates_OncePerPress_WithTheSlotIndex()
        {
            using var t = new SpecialActionTestScope();
            var port = new RecordingGyroCalibrationPort();
            SpecialAction sa = AddGyro(t, "L1", port);
            Assert.True(t.Engine.Owns(t.Device, sa));

            t.RunFrame(Frame(false));
            Assert.Empty(port.Calls);

            t.RunFrame(Frame(true));
            Assert.Equal(new[] { t.Device }, port.Calls.ToArray());   // 押して 1 回

            t.RunFrame(Frame(true));
            t.RunFrame(Frame(true));
            Assert.Single(port.Calls);                                // 保持中は繰り返さない

            t.RunFrame(Frame(false));
            Assert.Single(port.Calls);                                // 離したときは何もしない

            t.RunFrame(Frame(true));
            Assert.Equal(2, port.Calls.Count);                        // 押し直して、もう 1 回
        }

        [Fact]
        public void WhileTheFreshPressGuardIsOn_NothingIsCalibrated()
        {
            using var t = new SpecialActionTestScope();
            var port = new RecordingGyroCalibrationPort();
            AddGyro(t, "L1", port, freshPressGuard: true);

            t.RunFrame(Frame(true));
            t.RunFrame(Frame(true));
            t.RunFrame(Frame(true));

            Assert.Empty(port.Calls);
        }

        [Fact]
        public void AndCondition_NeedsAllTheTriggers()
        {
            using var t = new SpecialActionTestScope();
            var port = new RecordingGyroCalibrationPort();
            AddGyro(t, "L1/R1", port);

            t.RunFrame(Frame(true, false));
            t.RunFrame(Frame(false, true));
            Assert.Empty(port.Calls);

            t.RunFrame(Frame(true, true));
            Assert.Single(port.Calls);
        }

        [Fact]
        public void TheBuilder_BuildsTheBindingOnlyWithAPort()
        {
            using var t = new SpecialActionTestScope();
            var port = new RecordingGyroCalibrationPort();
            SpecialAction sa = AddGyro(t, "L1", port);

            IActionBinding binding = Assert.Single(NewBuilder(port).Build(t.Device));
            Assert.Same(sa, binding.Source);
            Assert.IsType<GyroCalibrateAction>(Assert.Single(binding.Outputs));
            Assert.True(binding.Policy.RespectsFreshPressGuard);
            Assert.False(binding.Policy.BlocksWhileExecuting);
            Assert.Equal(UntriggerPolicy.None, binding.Policy.Untrigger);

            Assert.Empty(NewBuilder(null).Build(t.Device));   // ポートがなければ、束縛を作らず、旧経路が処理する
        }

        // ---- ログ ----

        private static List<string> RunLogged(string name, LogLevel minLevel, RecordingGyroCalibrationPort port)
        {
            LoggingConfiguration saved = LogManager.Configuration;
            var target = new MemoryTarget("p8s76a") { Layout = "${level}|${message}" };
            var config = new LoggingConfiguration();
            config.AddTarget(target);
            config.AddRule(minLevel, LogLevel.Fatal, target);
            LogManager.Configuration = config;
            try
            {
                using (var t = new SpecialActionTestScope())
                {
                    SpecialAction sa = t.Add(new SpecialAction(name, "L1", "GyroCalibration", string.Empty, 0, string.Empty));
                    t.StateOf(sa).RequiresFreshPressAfterReset = false;
                    TestHost.GetService<IActionRegistry>().Replace(t.Device, NewBuilder(port).Build(t.Device));
                    t.RunFrame(Frame(false));
                    t.RunFrame(Frame(true));   // 立ち上がりの 1 フレームだけ
                }

                return target.Logs.Where(l => l.Contains(name)
                    && (l.Contains("Trigger detected for SA") || l.Contains("maps to Type") || l.Contains("Trigger keys for SA")
                        || l.Contains("output combo") || l.Contains("SpecialAction GyroCalibrate:"))).ToList();
            }
            finally
            {
                LogManager.Configuration = saved;
            }
        }

        [Fact]
        public void TheRisingTraceLines_AndTheDebugLineAreLogged()
        {
            string name = "p8s76a_log_" + Guid.NewGuid().ToString("N");

            List<string> logged = RunLogged(name, LogLevel.Trace, new RecordingGyroCalibrationPort());

            Assert.Contains(logged, l => l.Contains("Trigger detected for SA"));
            Assert.Contains(logged, l => l.Contains("maps to Type"));
            Assert.Contains(logged, l => l.StartsWith("Debug|SpecialAction GyroCalibrate: device=") && l.Contains($"name={name}") && l.Contains("ActionEntries="));
            Assert.Equal(5, logged.Count);   // Trace 4 行 + Debug 1 行（立ち上がりの 1 回だけ）
        }

        [Fact]
        public void WhenOnlyWarningsAreEnabled_NothingIsLogged()
        {
            string name = "p8s76a_off_" + Guid.NewGuid().ToString("N");

            Assert.Empty(RunLogged(name, LogLevel.Warn, new RecordingGyroCalibrationPort()));
        }

        // ---- 割り当て ----

        private static long AllocatedWhileHeld(SpecialActionTestScope t)
        {
            DS4State held = Frame(true);
            for (int i = 0; i < 300; i++) t.RunFrame(held);   // 立ち上がり（1 回だけ割り当てる）と JIT は、測定の外へ

            long before = GC.GetAllocatedBytesForCurrentThread();
            for (int i = 0; i < 1000; i++) t.RunFrame(held);
            return GC.GetAllocatedBytesForCurrentThread() - before;
        }

        [Fact]
        public void TheNewPath_DoesNotAddAllocationsToTheFrame_WhileHeld()
        {
            // 1 フレームの割り当ては、テストの足場（入力のコピー、特殊アクションがあるときの一覧の複製など）が約 6 KB 持つ。
            // 同じ件数の GyroCalibrate で、トリガーが押されていない（R1。押すのは L1）場合を基準にして、成立中に増えないことを確かめる
            long baseline;
            using (var t = new SpecialActionTestScope())
            {
                AddGyro(t, "R1", new RecordingGyroCalibrationPort());
                baseline = AllocatedWhileHeld(t);
            }

            long withGyro;
            var port = new RecordingGyroCalibrationPort();
            using (var t = new SpecialActionTestScope())
            {
                AddGyro(t, "L1", port);
                withGyro = AllocatedWhileHeld(t);
            }

            Assert.True(withGyro <= baseline + 4096, $"押している間: GyroCalibrate あり {withGyro} B > なし {baseline} B");
            Assert.Single(port.Calls);
        }

        // ---- 本番のポート ----

        [Fact]
        public void TheDefaultPort_DoesNothingWhenThereIsNoController()
        {
            var port = new DefaultGyroCalibrationPort(() => new NoControllerAccessor());

            port.ResetContinuousCalibration(0);
            port.ResetContinuousCalibration(-1);
            port.ResetContinuousCalibration(99);

            var nullAccessor = new DefaultGyroCalibrationPort(() => null);
            nullAccessor.ResetContinuousCalibration(0);
        }

        private sealed class NoControllerAccessor : IDeviceStateAccessor
        {
            public DS4Device GetController(int deviceIndex) => null;
        }
    }
}
