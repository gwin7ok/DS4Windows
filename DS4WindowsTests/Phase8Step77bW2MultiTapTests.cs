using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
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
    /// Phase8-Step7 7.7b-W2（Phase8-Step7-7-Plan.md §3.2。Q2＝B: 独立した入力アクション）: <see cref="MultiTapInputAction"/>、<see cref="MultiActionOutput"/>、
    /// ビルダー、ログ、割り当ての確認。旧・新の両経路の比べは <c>Phase8Step77bW1MultiActionDualPathTests</c>。
    /// 入力アクションは、時刻と押下を差し込める偽の入力（待たない）で確かめる。他の Host 構築テストと同じコレクションで直列に実行する。
    /// </summary>
    [Collection("ServiceProviderHolder")]
    public class Phase8Step77bW2MultiTapTests
    {
        private static readonly DateTime T0 = new DateTime(2026, 10, 20, 12, 0, 0, DateTimeKind.Utc);

        /// <summary>押下と時刻を差し込める偽の入力。<see cref="Advance"/> で、前フレームの押下を保存して時刻を進める。</summary>
        private sealed class FakeInput : ITriggerInput
        {
            private readonly HashSet<DS4Controls> _active = new HashSet<DS4Controls>();
            private readonly HashSet<DS4Controls> _previous = new HashSet<DS4Controls>();

            public int Device { get; set; } = 3;
            public DelayClock Delay { get; } = new DelayClock();
            public DateTime UtcNow { get; set; } = T0;
            public long TickCount64 => 0;

            public bool IsActive(DS4Controls control) => _active.Contains(control);
            public bool WasActive(DS4Controls control) => _previous.Contains(control);

            public void Press(DS4Controls control) => _active.Add(control);
            public void Release(DS4Controls control) => _active.Remove(control);

            /// <summary>評価のあと、次のフレームへ（前フレームの押下を保存し、時刻を進める）。</summary>
            public void Advance(int milliseconds)
            {
                _previous.Clear();
                foreach (DS4Controls c in _active) _previous.Add(c);
                UtcNow = UtcNow.AddMilliseconds(milliseconds);
            }
        }

        private static MultiTapInputAction NewInput(params DS4Controls[] triggers)
            => new MultiTapInputAction("p8s77b_multi", triggers, TestHost.GetService<IProfileSettingsService>());

        /// <summary>1 フレーム評価し、ジェスチャーを返す。</summary>
        private static int Frame(MultiTapInputAction input, FakeInput fake, int advanceMilliseconds)
        {
            int gesture = input.Evaluate(fake).Gesture;
            fake.Advance(advanceMilliseconds);
            return gesture;
        }

        [Fact]
        public void ATap_ReportsTheTapGesture_OnlyAfterTheInterval_AndOnlyOnce()
        {
            var input = NewInput(DS4Controls.L1);
            var fake = new FakeInput();

            Assert.Equal(TriggerState.NoGesture, Frame(input, fake, 10));    // 何も押していない
            fake.Press(DS4Controls.L1);
            Assert.Equal(TriggerState.NoGesture, Frame(input, fake, 50));    // 押した
            fake.Release(DS4Controls.L1);
            Assert.Equal(TriggerState.NoGesture, Frame(input, fake, 50));    // 離した（タップ成立。まだ実行しない）
            Assert.Equal(TriggerState.NoGesture, Frame(input, fake, 60));    // 離してから 50ms
            Assert.Equal(TriggerState.NoGesture, Frame(input, fake, 60));    // 110ms
            Assert.Equal(TriggerState.Tap, Frame(input, fake, 60));          // 170ms（> 150ms）
            Assert.Equal(TriggerState.NoGesture, Frame(input, fake, 60));
            Assert.Equal(TriggerState.NoGesture, Frame(input, fake, 60));
        }

        [Fact]
        public void AHold_ReportsTheHoldGestureOnce_AndTheReleaseReportsNothing()
        {
            var input = NewInput(DS4Controls.L1);
            var fake = new FakeInput();

            fake.Press(DS4Controls.L1);
            Assert.Equal(TriggerState.NoGesture, Frame(input, fake, 200));   // 押した
            Assert.Equal(TriggerState.NoGesture, Frame(input, fake, 200));   // 200ms
            Assert.Equal(TriggerState.NoGesture, Frame(input, fake, 200));   // 400ms
            Assert.Equal(TriggerState.Hold, Frame(input, fake, 200));        // 600ms（> 500ms）
            Assert.Equal(TriggerState.NoGesture, Frame(input, fake, 200));   // 800ms: 1 回だけ

            fake.Release(DS4Controls.L1);
            Assert.Equal(TriggerState.NoGesture, Frame(input, fake, 200));   // 離した: 実行済みなので、タップにならない
            Assert.Equal(TriggerState.NoGesture, Frame(input, fake, 200));
        }

        [Fact]
        public void ADoubleTap_ReportsTheDoubleTapGesture_AtTheSecondPress_AndNotTheTap()
        {
            var input = NewInput(DS4Controls.L1);
            var fake = new FakeInput();

            fake.Press(DS4Controls.L1);
            Assert.Equal(TriggerState.NoGesture, Frame(input, fake, 30));    // 1 回目の押下
            fake.Release(DS4Controls.L1);
            Assert.Equal(TriggerState.NoGesture, Frame(input, fake, 30));    // 1 回目の解放（タップ成立）
            fake.Press(DS4Controls.L1);
            Assert.Equal(TriggerState.DoubleTap, Frame(input, fake, 30));    // 間隔以内の 2 回目の押下: ダブルタップ
            fake.Release(DS4Controls.L1);
            Assert.Equal(TriggerState.NoGesture, Frame(input, fake, 300));   // 解放
            Assert.Equal(TriggerState.NoGesture, Frame(input, fake, 300));   // タップは報告しない
        }

        [Fact]
        public void ASlowSecondPress_IsANewFirstTouch_NotADoubleTap()
        {
            var input = NewInput(DS4Controls.L1);
            var fake = new FakeInput();

            fake.Press(DS4Controls.L1);
            Frame(input, fake, 30);
            fake.Release(DS4Controls.L1);
            Frame(input, fake, 200);                                          // 解放から 200ms（> 150ms）: タップが先に報告される
            // 上の評価で、タップが報告済み（次の評価で返る）
            int gesture = Frame(input, fake, 30);
            Assert.True(gesture == TriggerState.Tap || gesture == TriggerState.NoGesture);

            fake.Press(DS4Controls.L1);
            Assert.NotEqual(TriggerState.DoubleTap, Frame(input, fake, 30)); // 間隔を過ぎた押下は、ダブルタップではない
        }

        [Fact]
        public void TheDebouncingMs_ExtendsTheIntervals()
        {
            IProfileSettingsService settings = TestHost.GetService<IProfileSettingsService>();
            int device = 3;
            int saved = settings.DebouncingMs[device];
            try
            {
                settings.DebouncingMs[device] = 200;
                var input = NewInput(DS4Controls.L1);
                var fake = new FakeInput { Device = device };

                fake.Press(DS4Controls.L1);
                Frame(input, fake, 30);
                fake.Release(DS4Controls.L1);
                Frame(input, fake, 100);                                       // 離した
                Assert.Equal(TriggerState.NoGesture, Frame(input, fake, 100)); // 100ms
                Assert.Equal(TriggerState.NoGesture, Frame(input, fake, 100)); // 200ms（150ms ＋ 200ms ＝ 350ms までは待つ）
                Assert.Equal(TriggerState.NoGesture, Frame(input, fake, 100)); // 300ms
                Assert.Equal(TriggerState.Tap, Frame(input, fake, 100));       // 400ms（> 350ms）
            }
            finally
            {
                settings.DebouncingMs[device] = saved;
            }
        }

        [Fact]
        public void TheEstablishedState_FollowsTheAndConditionOfAllTheTriggers()
        {
            var input = NewInput(DS4Controls.L1, DS4Controls.R1);
            var fake = new FakeInput();

            fake.Press(DS4Controls.L1);
            Assert.False(input.Evaluate(fake).IsEstablished);
            fake.Advance(10);
            fake.Press(DS4Controls.R1);
            Assert.True(input.Evaluate(fake).IsEstablished);
            fake.Advance(10);
            fake.Release(DS4Controls.L1);
            Assert.False(input.Evaluate(fake).IsEstablished);
        }

        [Fact]
        public void TheGesture_IsJudgedByTheFirstTriggerOnly()
        {
            var input = NewInput(DS4Controls.L1, DS4Controls.R1);
            var fake = new FakeInput();

            fake.Press(DS4Controls.L1);                                        // 1 つ目だけ
            Frame(input, fake, 30);
            fake.Release(DS4Controls.L1);
            Frame(input, fake, 200);
            Assert.Equal(TriggerState.Tap, Frame(input, fake, 30));            // R1 を押していなくても、1 つ目のタップは報告する
        }

        [Fact]
        public void Reset_ClearsTheGestureState()
        {
            var input = NewInput(DS4Controls.L1);
            var fake = new FakeInput();

            fake.Press(DS4Controls.L1);
            Frame(input, fake, 30);
            fake.Release(DS4Controls.L1);
            Frame(input, fake, 30);   // タップ成立
            input.Reset();

            Assert.Equal(TriggerState.NoGesture, Frame(input, fake, 300));   // リセット後は、タップを報告しない
            Assert.Equal(TriggerState.NoGesture, Frame(input, fake, 300));
        }

        // ---- 出力 ----

        private sealed class RecordingPlayer : IMacroStringPlayer
        {
            public readonly List<(int Device, string Macro)> Calls = new List<(int, string)>();

            public void PlayMacroString(int deviceIndex, string macro) => Calls.Add((deviceIndex, macro));
        }

        private sealed class DummyContext : IOutputContext
        {
            public int Device => 2;
            public IVirtualKBM OutputHandler => null;
            public IFrameOutput Frame => null;
            public void RequestEndFrame() { }
        }

        [Fact]
        public void TheMacroStringAction_PlaysItsMacro_AndIgnoresAnEmptyOne()
        {
            var player = new RecordingPlayer();
            var sa = new SpecialAction("p8s77b_out", "L1", "MultiAction", "65/65,,67/67", 0, string.Empty);
            var ctx = new DummyContext();

            new MacroStringAction(sa, 4, "65/65", player).Execute(ctx);
            new MacroStringAction(sa, 4, string.Empty, player).Execute(ctx);   // 空: 何もしない
            new MacroStringAction(sa, 4, null, player).Execute(ctx);            // null: 空として扱う
            new MacroStringAction(sa, -1, "67/67", player).Execute(ctx);        // デバイスの添字がなければ、文脈のデバイス

            Assert.Equal(new[] { (4, "65/65"), (2, "67/67") }, player.Calls.ToArray());

            var output = new MacroStringAction(sa, 4, "66/66", player);
            output.Stop(ctx);
            Assert.Equal(2, player.Calls.Count);   // 離したときは何もしない
        }

        // ---- ビルダー・スイッチ ----

        private static ActionBindingBuilder NewBuilder(IMacroStringPlayer player)
            => new ActionBindingBuilder(TestHost.GetService<IProfileActionProvider>(), TestHost.GetService<IProfileSettingsService>(),
                TestHost.GetService<IManagedActionManager>(), TestHost.GetService<IMacroPlayer>(), TestHost.GetService<IProcessLauncher>(),
                TestHost.GetService<IProfileSwitcher>(), TestHost.GetService<ISpecialActionRepository>(),
                TestHost.GetService<Func<IMapping>>(), null, null, null, null, player);

        private static void Publish(SpecialActionTestScope t, IMacroStringPlayer player)
            => TestHost.GetService<IActionRegistry>().Replace(t.Device, NewBuilder(player).Build(t.Device));

        private static SpecialAction AddMulti(SpecialActionTestScope t, string triggers, IMacroStringPlayer player, string details = "65/65,66/66,67/67")
        {
            SpecialAction sa = t.Add(new SpecialAction("p8s77b_bind_" + Guid.NewGuid().ToString("N"), triggers, "MultiAction", details, 0, string.Empty));
            t.StateOf(sa).RequiresFreshPressAfterReset = false;
            Publish(t, player);
            return sa;
        }

        [Fact]
        public void TheBuilder_BuildsTheIndependentInput_AndOneOutputPerGesture_OnlyWithAPlayer()
        {
            using var t = new SpecialActionTestScope();
            var player = new RecordingPlayer();
            SpecialAction sa = AddMulti(t, "L1", player);

            IActionBinding binding = Assert.Single(NewBuilder(player).Build(t.Device));
            Assert.Same(sa, binding.Source);
            Assert.IsType<MultiTapInputAction>(binding.Input);
            Assert.Equal(3, binding.Outputs.Count);   // タップ、長押し、ダブルタップの順に、ジェスチャーごとの出力
            Assert.All(binding.Outputs, o => Assert.IsType<MacroStringAction>(o));
            Assert.True(binding.Policy.RoutesOutputsByGesture);
            Assert.False(binding.Policy.RespectsFreshPressGuard);
            Assert.False(binding.Policy.BlocksWhileExecuting);
            Assert.False(binding.Policy.StopOnRelease);
            Assert.Equal(UntriggerPolicy.None, binding.Policy.Untrigger);
            Assert.True(t.Engine.Owns(t.Device, sa));

            Assert.Empty(NewBuilder(null).Build(t.Device));   // 窓口がなければ、束縛を作らず、旧経路が処理する
        }

        [Fact]
        public void TheBuilder_MapsEachGestureToItsOwnMacro_AndKeepsTheSlotsWhenTheMacrosAreEmptyOrMissing()
        {
            // 「どのジェスチャーでどのマクロか」の対応は、変換層（ビルダーが作る束縛）が決める
            var player = new RecordingPlayer();
            var ctx = new DummyContext();

            using (var t = new SpecialActionTestScope())
            {
                AddMulti(t, "L1", player, "65/65,,67/67");
                IActionBinding binding = Assert.Single(NewBuilder(player).Build(t.Device));
                for (int g = 0; g < binding.Outputs.Count; g++) binding.Outputs[g].Execute(ctx);
                Assert.Equal(new[] { (t.Device, "65/65"), (t.Device, "67/67") }, player.Calls.ToArray());   // 長押しは空: 何もしない
            }

            player.Calls.Clear();
            using (var t = new SpecialActionTestScope())
            {
                AddMulti(t, "L1", player, "65/65");   // details が足りない
                IActionBinding few = Assert.Single(NewBuilder(player).Build(t.Device));
                Assert.Equal(3, few.Outputs.Count);     // 添字を固定するため、足りなくても 3 つそろえる
                for (int g = 0; g < few.Outputs.Count; g++) few.Outputs[g].Execute(ctx);
                Assert.Equal(new[] { (t.Device, "65/65") }, player.Calls.ToArray());
            }
        }

        [Fact]
        public void TheDefaultPolicy_DoesNotRouteOutputsByGesture()
        {
            Assert.False(BindingPolicy.Default.RoutesOutputsByGesture);
            Assert.False(new BindingPolicy(blocksWhileExecuting: true).RoutesOutputsByGesture);
            Assert.True(new BindingPolicy(routesOutputsByGesture: true).RoutesOutputsByGesture);
        }

        // ---- 窓口 ----

        [Fact]
        public void TheHostPlayer_DelegatesToMapping_AndIgnoresEmptyOrOutOfRange()
        {
            var player = TestHost.GetService<IMacroStringPlayer>();
            Assert.IsType<DefaultMacroStringPlayer>(player);
            Assert.IsAssignableFrom<IMacroStringPlayer>(TestHost.GetService<IMapping>());   // Mapping が実装する（IMapping のメンバーではない）

            player.PlayMacroString(0, string.Empty);
            player.PlayMacroString(-1, "65/65");
            player.PlayMacroString(99, "65/65");

            new DefaultMacroStringPlayer(() => null).PlayMacroString(0, "65/65");   // Mapping がなくても、例外にしない
        }

        [Fact]
        public void TheMappingPlayer_PlaysTheMacroString_ThroughTheOutputHandler()
        {
            using var scope = new MappingCharacterizationScope();
            var player = (IMacroStringPlayer)scope.Mapper;

            player.PlayMacroString(MappingCharacterizationScope.Device, "65/65");

            var sw = Stopwatch.StartNew();
            while (scope.Recorder.KeyPresses.Count < 1 && sw.ElapsedMilliseconds < 1500)
                Thread.Sleep(10);

            uint native = scope.KbmMapping.macroKeyTranslate ? scope.KbmMapping.GetRealEventKey(65) : 65;
            Assert.Equal(new[] { native }, scope.Recorder.KeyPresses.ToArray());
        }

        // ---- ログ ----

        private static List<string> RunLogged(string name, LogLevel minLevel)
        {
            LoggingConfiguration saved = LogManager.Configuration;
            var target = new MemoryTarget("p8s77b") { Layout = "${level}|${message}" };
            var config = new LoggingConfiguration();
            config.AddTarget(target);
            config.AddRule(minLevel, LogLevel.Fatal, target);
            LogManager.Configuration = config;
            try
            {
                using (var t = new SpecialActionTestScope())
                {
                    TestDs4Device device = TestDs4Device.Create();
                    t.Scope.Control.DS4Controllers[t.Device] = device;
                    try
                    {
                        SpecialAction sa = t.Add(new SpecialAction(name, "L1", "MultiAction", "65/65,,", 0, string.Empty));
                        t.StateOf(sa).RequiresFreshPressAfterReset = false;

                        DS4State previous = SpecialActionTestScope.Neutral();
                        void Step(bool l1)
                        {
                            DS4State input = SpecialActionTestScope.Neutral();
                            input.L1 = l1;
                            device.Previous = previous;
                            t.RunFrame(input);
                            previous = input;
                        }

                        Step(false);
                        Step(true);
                        Thread.Sleep(30);
                        Step(false);
                        var sw = Stopwatch.StartNew();
                        while (sw.ElapsedMilliseconds < 400)
                        {
                            Step(false);
                            Thread.Sleep(10);
                        }

                        Thread.Sleep(100);   // マクロ（背景のタスク）の完了を待つ
                    }
                    finally
                    {
                        t.Scope.Control.DS4Controllers[t.Device] = null;
                    }
                }

                return target.Logs.Where(l => l.Contains(name)
                    && (l.Contains("Trigger detected for SA") || l.Contains("maps to Type") || l.Contains("Trigger keys for SA") || l.Contains("output combo")
                        || l.Contains("SpecialAction MultiAction"))).ToList();
            }
            finally
            {
                LogManager.Configuration = saved;
            }
        }

        [Fact]
        public void TheTapLogs_AreLogged()
        {
            string name = "p8s77b_log_" + Guid.NewGuid().ToString("N");

            List<string> logged = RunLogged(name, LogLevel.Trace);

            Assert.Equal(4, logged.Count);   // マクロを実行する直前の Trace 4 行。Debug はない
            Assert.Contains(logged, l => l.Contains("maps to Type:MultiAction"));
        }

        [Fact]
        public void WhenOnlyWarningsAreEnabled_NothingIsLogged()
        {
            string name = "p8s77b_off_" + Guid.NewGuid().ToString("N");

            Assert.Empty(RunLogged(name, LogLevel.Warn));
        }

        // ---- 割り当て ----

        private static long AllocatedWhileHeld(SpecialActionTestScope t)
        {
            DS4State held = SpecialActionTestScope.Neutral();
            held.L1 = true;
            for (int i = 0; i < 300; i++) t.RunFrame(held);   // JIT は、測定の外へ

            // Phase10-Step4 W4: 5 ラウンド測って最小値を使う（一度きりの割り当てが測定の窓に入っても結果が変わらないように。Phase8Step76cBatteryCheckTests と同じ）
            long min = long.MaxValue;
            for (int round = 0; round < 5; round++)
            {
                long before = GC.GetAllocatedBytesForCurrentThread();
                for (int i = 0; i < 1000; i++) t.RunFrame(held);
                min = Math.Min(min, GC.GetAllocatedBytesForCurrentThread() - before);
            }

            return min;
        }

        [Fact]
        public void TheNewPath_DoesNotAddAllocationsToTheFrame_WhileHeld()
        {
            // 同じ件数の MultiAction で、トリガーが押されていない（R1。押すのは L1）場合を基準にする（Phase8Step76aGyroCalibrateTests と同じ方式）
            long baseline;
            using (var t = new SpecialActionTestScope())
            {
                AddMulti(t, "R1", new RecordingPlayer());
                baseline = AllocatedWhileHeld(t);
            }

            long withMulti;
            using (var t = new SpecialActionTestScope())
            {
                AddMulti(t, "L1", new RecordingPlayer());
                withMulti = AllocatedWhileHeld(t);
            }

            Assert.True(withMulti <= baseline + 4096, $"押している間: あり {withMulti} B > 基準 {baseline} B");
        }
    }
}
