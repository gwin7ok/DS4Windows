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
    /// Phase8-Step7 7.5b-W3（Phase8-Step7-5b-Plan.md §3 の W3）: 復帰を持つ Profile（Unload on trigger release）の、ロールバック・割り当て・性能の確認。
    /// 既定の一覧は 7.5a で Profile を含むため変わらない。プロファイルの適用は、記録用の実装で置き換える。他の Host 構築テストと同じコレクションで直列に実行する。
    /// </summary>
    [Collection("ServiceProviderHolder")]
    public class Phase8Step75bW3ProfileUntriggerSwitchTests
    {
        private readonly ITestOutputHelper _output;

        public Phase8Step75bW3ProfileUntriggerSwitchTests(ITestOutputHelper output)
        {
            _output = output;
        }

        private static DS4State Frame(bool l1)
        {
            DS4State state = SpecialActionTestScope.Neutral();
            state.L1 = l1;
            return state;
        }

        private static SpecialAction AddProfile(SpecialActionTestScope t, string triggers)
        {
            SpecialAction sa = t.Add(new SpecialAction("p8s75bw3_profile_" + Guid.NewGuid().ToString("N"), triggers, "Profile", "p8s75b-target", 0, "AutomaticUntrigger"));
            ActionInstanceState state = t.StateOf(sa);
            state.RequiresFreshPressAfterReset = false;
            state.IsExecuting = false;
            return sa;
        }

        private static long Allocated(SpecialActionTestScope t, DS4State input)
        {
            long before = GC.GetAllocatedBytesForCurrentThread();
            for (int i = 0; i < 1000; i++) t.RunFrame(input);
            return GC.GetAllocatedBytesForCurrentThread() - before;
        }

        [Fact]
        public void TheNewPath_DoesNotAddAllocationsToTheFrame_WhileWaitingForTheUntrigger()
        {
            long legacyHeld, legacyIdle, migratedHeld, migratedIdle;
            DS4State held = Frame(true), idle = Frame(false);

            // 押している間（復帰待ち）の割り当て。離すと復帰して合図が消えるため、「未操作」は、合図がない状態の割り当て
            using (var t = new SpecialActionTestScope(false))
            {
                t.InstallProfileRecorder();
                AddProfile(t, "L1");
                for (int i = 0; i < 300; i++) t.RunFrame(held);   // 切替の立ち上がり（1 回だけ大きく割り当てる）は、測定の外へ
                legacyHeld = Allocated(t, held);
                t.RunFrame(idle);                                  // 復帰して合図が消える
                legacyIdle = Allocated(t, idle);
            }

            using (var t = new SpecialActionTestScope(true))
            {
                t.InstallProfileRecorder();
                AddProfile(t, "L1");
                for (int i = 0; i < 300; i++) t.RunFrame(held);
                migratedHeld = Allocated(t, held);
                t.RunFrame(idle);
                migratedIdle = Allocated(t, idle);
            }

            _output.WriteLine($"[Phase8Step7.5b Profile untrigger] waiting legacy={legacyHeld}B migrated={migratedHeld}B / idle legacy={legacyIdle}B migrated={migratedIdle}B");
            Assert.True(migratedHeld <= legacyHeld + 4096, $"復帰待ち: 新 {migratedHeld} B > 旧 {legacyHeld} B");
            Assert.True(migratedIdle <= legacyIdle + 4096, $"未操作: 新 {migratedIdle} B > 旧 {legacyIdle} B");
        }

        [Fact]
        public void MapCustom_ProfileUntriggerMigrated_PerFrameTime_IsReported()
        {
            foreach (bool migrated in new[] { false, true })
            {
                using var t = new SpecialActionTestScope(migrated);
                t.InstallProfileRecorder();
                string[] triggers = { "Cross", "Circle", "Square", "Triangle", "L1", "R1", "Cross/L1", "Circle/R1", "Share", "Options" };
                for (int i = 0; i < 30; i++)
                    AddProfile(t, triggers[i % triggers.Length]);

                // 押している間（切替のあと、復帰待ち）の費用
                DS4State pressed = SpecialActionTestScope.Neutral();
                pressed.Share = true;
                for (int i = 0; i < 300; i++) t.RunFrame(pressed);

                var sw = Stopwatch.StartNew();
                for (int i = 0; i < 5000; i++) t.RunFrame(pressed);
                sw.Stop();
                _output.WriteLine($"[Phase8Step7.5b Profile untrigger] {(migrated ? "migrated" : "legacy  ")} actions=30 perFrame={sw.Elapsed.TotalMilliseconds * 1000.0 / 5000:F2}us");
            }
        }
    }
}
