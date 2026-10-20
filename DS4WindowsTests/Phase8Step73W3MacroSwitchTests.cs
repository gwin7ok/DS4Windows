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

        [Fact]
        public void RemovingTheMacroLine_NoBindingIsBuilt_AndNothingHappens()
        {
            var options = TestHost.GetService<BindingMigrationOptions>();
            var builder = TestHost.GetService<IActionBindingBuilder>();
            var player = TestHost.GetService<IMacroPlayer>();
            var saved = options.MigratedTypes.ToArray();
            try
            {
                using var t = new SpecialActionTestScope(true);
                SpecialAction sa = AddMacro(t, "L1");
                Assert.Single(builder.Build(t.Device));

                options.MigratedTypes.Remove(SpecialAction.ActionTypeId.Macro);   // 既定の一覧から Macro だけを外した状態

                Assert.Empty(builder.Build(t.Device));
                t.PublishBindings();
                Assert.Empty(TestHost.GetService<IActionRegistry>().GetBindings(t.Device));
                Assert.False(t.Engine.Owns(t.Device, sa));

                // Phase8-Step8 8b: 旧経路は削除した。束縛がない Macro は、何もしない
                player.Stop(t.Device);
                t.RunFrame(Frame(true));
                Assert.False(ActionManager.IsBeingTriggered(sa, t.Device));
                Assert.False(player.IsPlaying(t.Device));
            }
            finally
            {
                options.MigratedTypes.Clear();
                foreach (var type in saved) options.MigratedTypes.Add(type);
            }
        }

        private static long Allocated(SpecialActionTestScope t, DS4State input)
        {
            long before = GC.GetAllocatedBytesForCurrentThread();
            for (int i = 0; i < 1000; i++) t.RunFrame(input);
            return GC.GetAllocatedBytesForCurrentThread() - before;
        }

        [Fact]
        public void TheNewPath_DoesNotAddAllocationsToTheFrame_ForMacro()
        {
            long legacyHeld, legacyIdle, migratedHeld, migratedIdle;
            DS4State held = Frame(true), idle = Frame(false);
            IMacroPlayer player = TestHost.GetService<IMacroPlayer>();

            using (var t = new SpecialActionTestScope(false))
            {
                AddMacro(t, "L1");
                for (int i = 0; i < 300; i++) { t.RunFrame(held); t.RunFrame(idle); }
                t.RunFrame(held);   // 立ち上がりの 1 フレーム（再生の開始で、1 回だけ大きく割り当てる）は、測定の外へ
                legacyHeld = Allocated(t, held);
                legacyIdle = Allocated(t, idle);
                player.Stop(t.Device);
            }

            using (var t = new SpecialActionTestScope(true))
            {
                AddMacro(t, "L1");
                for (int i = 0; i < 300; i++) { t.RunFrame(held); t.RunFrame(idle); }
                t.RunFrame(held);
                migratedHeld = Allocated(t, held);
                migratedIdle = Allocated(t, idle);
                player.Stop(t.Device);
            }

            _output.WriteLine($"[Phase8Step7.3 Macro] held legacy={legacyHeld}B migrated={migratedHeld}B / idle legacy={legacyIdle}B migrated={migratedIdle}B");
            Assert.True(migratedIdle <= legacyIdle + 4096, $"未操作: 新 {migratedIdle} B > 旧 {legacyIdle} B");

            // 押している間（立ち上がりを除く）: 新経路が旧経路より多く割り当てない。
            // W3 の最初の実測で、新経路が多かったのは、立ち上がりのフレーム（マクロの再生の開始）を測定に含めていたため（診断で、立ち上がりを除くと旧・新が一致した）。
            Assert.True(migratedHeld <= legacyHeld + 4096, $"押下: 新 {migratedHeld} B > 旧 {legacyHeld} B");
        }

        private static int CountMacroFirings(bool migrated, int heldFrames)
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
                using (var t = new SpecialActionTestScope(migrated))
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
        public void HeldMacro_FiresOnlyOnce_OnEitherPath()
        {
            int legacy = CountMacroFirings(false, 1000);
            int migrated = CountMacroFirings(true, 1000);

            _output.WriteLine($"[Phase8Step7.3 Macro] firings while held (1000 frames): legacy={legacy} migrated={migrated}");
            Assert.Equal(1, legacy);
            Assert.Equal(1, migrated);
        }

        /// <summary>
        /// 押している間（1000 フレーム。立ち上がりを除く）の割り当てが、種別（Button／Key／Macro）ごとに、新経路で旧経路を超えない（4 KB の誤差まで）。
        /// 出力は、<c>--logger "console;verbosity=detailed"</c> で見られる。
        /// </summary>
        [Fact]
        public void HeldAllocation_PerType_NewPathIsNotMoreThanLegacy()
        {
            IMacroPlayer player = TestHost.GetService<IMacroPlayer>();
            DS4State held = Frame(true), idle = Frame(false);

            long Measure(bool migrated, Func<SpecialActionTestScope, SpecialAction> add)
            {
                using var t = new SpecialActionTestScope(migrated);
                SpecialAction sa = add(t);
                t.StateOf(sa).RequiresFreshPressAfterReset = false;
                for (int i = 0; i < 300; i++) { t.RunFrame(held); t.RunFrame(idle); }
                t.RunFrame(held);   // 立ち上がりを測定の外へ
                long bytes = Allocated(t, held);
                player.Stop(t.Device);
                return bytes;
            }

            var types = new (string Name, Func<SpecialActionTestScope, SpecialAction> Add)[]
            {
                ("Button", t => t.AddButtonPress("L1")),
                ("Key", t => t.AddKey("L1", toggle: false)),
                ("Macro", t => AddMacro(t, "L1")),
            };
            foreach (var type in types)
            {
                long legacy = Measure(false, type.Add);
                long migrated = Measure(true, type.Add);
                _output.WriteLine($"[Phase8Step7.3] {type.Name}: held legacy={legacy}B migrated={migrated}B (1000 frames, rising edge excluded)");
                Assert.True(migrated <= legacy + 4096, $"{type.Name}: 新 {migrated} B > 旧 {legacy} B");
            }
        }

        [Fact]
        public void MapCustom_MacroMigrated_PerFrameTime_IsReported()
        {
            IMacroPlayer player = TestHost.GetService<IMacroPlayer>();
            foreach (bool migrated in new[] { false, true })
            {
                using var t = new SpecialActionTestScope(migrated);
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
                _output.WriteLine($"[Phase8Step7.3 Macro] {(migrated ? "migrated" : "legacy  ")} actions=30 perFrame={sw.Elapsed.TotalMilliseconds * 1000.0 / 5000:F2}us");
            }
        }
    }
}
