using System;
using System.Diagnostics;
using System.Linq;
using DS4Windows;
using DS4Windows.Actions;
using Xunit;
using Xunit.Abstractions;

namespace DS4WindowsTests
{
    /// <summary>
    /// Phase8-Step7 7.2-W3（Phase8-Step7-2-Plan.md §3 W3）: Key をスイッチに入れた状態の、ロールバック・割り当て・性能の確認。
    /// 他の Host 構築テストと同じコレクションで直列に実行する。
    /// </summary>
    [Collection("ServiceProviderHolder")]
    public class Phase8Step72W3KeySwitchTests
    {
        private readonly ITestOutputHelper _output;

        public Phase8Step72W3KeySwitchTests(ITestOutputHelper output)
        {
            _output = output;
        }

        private static DS4State Frame(bool l1)
        {
            DS4State state = SpecialActionTestScope.Neutral();
            state.L1 = l1;
            return state;
        }

        [Fact]
        public void RemovingTheKeyLine_BringsBackTheLegacyPath_NoBindingIsBuilt()
        {
            var options = TestHost.GetService<BindingMigrationOptions>();
            var builder = TestHost.GetService<IActionBindingBuilder>();
            var saved = options.MigratedTypes.ToArray();
            try
            {
                using var t = new SpecialActionTestScope(true);
                t.AddKey("L1", toggle: false);
                Assert.Single(builder.Build(t.Device));

                options.MigratedTypes.Remove(SpecialAction.ActionTypeId.Key);   // 既定の一覧から Key だけを外した状態

                Assert.Empty(builder.Build(t.Device));
                t.PublishBindings();
                Assert.Empty(TestHost.GetService<IActionRegistry>().GetBindings(t.Device));

                // 旧経路が処理する（旧ループが、キーを押した状態にする）
                t.StateOf(t.Add(new SpecialAction("p8s72w3_key_" + Guid.NewGuid().ToString("N"), "R1", "Key", "66", 0, "Press\n"))).RequiresFreshPressAfterReset = false;
                DS4State input = SpecialActionTestScope.Neutral();
                input.R1 = true;
                t.RunFrame(input);
                Assert.True(t.Engine.IsSuppressed(t.Device, DS4Controls.R1));
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
        public void TheNewPath_DoesNotAddAllocationsToTheFrame_ForKey()
        {
            long LegacyHeld, LegacyIdle, MigratedHeld, MigratedIdle;
            DS4State held = Frame(true), idle = Frame(false);

            using (var t = new SpecialActionTestScope(false))
            {
                SpecialAction sa = t.AddKey("L1", toggle: false);
                t.StateOf(sa).RequiresFreshPressAfterReset = false;
                for (int i = 0; i < 300; i++) { t.RunFrame(held); t.RunFrame(idle); }
                LegacyHeld = Allocated(t, held);
                LegacyIdle = Allocated(t, idle);
            }

            using (var t = new SpecialActionTestScope(true))
            {
                SpecialAction sa = t.AddKey("L1", toggle: false);
                t.StateOf(sa).RequiresFreshPressAfterReset = false;
                for (int i = 0; i < 300; i++) { t.RunFrame(held); t.RunFrame(idle); }
                MigratedHeld = Allocated(t, held);
                MigratedIdle = Allocated(t, idle);
            }

            _output.WriteLine($"[Phase8Step7.2 Key] held legacy={LegacyHeld}B migrated={MigratedHeld}B / idle legacy={LegacyIdle}B migrated={MigratedIdle}B");
            Assert.True(MigratedHeld <= LegacyHeld, $"押下: 新 {MigratedHeld} B > 旧 {LegacyHeld} B");
            Assert.True(MigratedIdle <= LegacyIdle, $"未操作: 新 {MigratedIdle} B > 旧 {LegacyIdle} B");
        }

        [Fact]
        public void MapCustom_KeyMigrated_PerFrameTime_IsReported()
        {
            foreach (bool migrated in new[] { false, true })
            {
                using var t = new SpecialActionTestScope(migrated);
                string[] triggers = { "Cross", "Circle", "Square", "Triangle", "L1", "R1", "Cross/L1", "Circle/R1", "Share", "Options" };
                for (int i = 0; i < 30; i++)
                    t.StateOf(t.AddKey(triggers[i % triggers.Length], toggle: false)).RequiresFreshPressAfterReset = false;

                DS4State pressed = SpecialActionTestScope.Neutral();
                pressed.Cross = true;
                pressed.L1 = true;
                for (int i = 0; i < 300; i++) t.RunFrame(pressed);

                var sw = Stopwatch.StartNew();
                for (int i = 0; i < 5000; i++) t.RunFrame(pressed);
                sw.Stop();
                _output.WriteLine($"[Phase8Step7.2 Key] {(migrated ? "migrated" : "legacy  ")} actions=30 perFrame={sw.Elapsed.TotalMilliseconds * 1000.0 / 5000:F2}us");
            }
        }
    }
}
