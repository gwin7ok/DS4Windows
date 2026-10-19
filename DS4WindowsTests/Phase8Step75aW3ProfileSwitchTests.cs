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
    /// Phase8-Step7 7.5a-W3（Phase8-Step7-5-Plan.md §3 7.5a の W3）: Profile（切替のみ）をスイッチに入れた状態の、ロールバック・復帰つきの扱い・割り当て・性能の確認。
    /// プロファイルの適用は、記録用の実装で置き換える。他の Host 構築テストと同じコレクションで直列に実行する。
    /// </summary>
    [Collection("ServiceProviderHolder")]
    public class Phase8Step75aW3ProfileSwitchTests
    {
        private readonly ITestOutputHelper _output;

        public Phase8Step75aW3ProfileSwitchTests(ITestOutputHelper output)
        {
            _output = output;
        }

        private static DS4State Frame(bool l1)
        {
            DS4State state = SpecialActionTestScope.Neutral();
            state.L1 = l1;
            return state;
        }

        private static SpecialAction AddProfile(SpecialActionTestScope t, string triggers, string extras = "")
        {
            SpecialAction sa = t.Add(new SpecialAction("p8s75aw3_profile_" + Guid.NewGuid().ToString("N"), triggers, "Profile", "p8s75-target", 0, extras));
            ActionInstanceState state = t.StateOf(sa);
            state.RequiresFreshPressAfterReset = false;
            state.IsExecuting = false;
            return sa;
        }

        [Fact]
        public void RemovingTheProfileLine_BringsBackTheLegacyPath_NoBindingIsBuilt()
        {
            var options = TestHost.GetService<BindingMigrationOptions>();
            var builder = TestHost.GetService<IActionBindingBuilder>();
            var saved = options.MigratedTypes.ToArray();
            try
            {
                using var t = new SpecialActionTestScope(true);
                RecordingProfileApplicationService recorder = t.InstallProfileRecorder();
                SpecialAction sa = AddProfile(t, "L1");
                Assert.Single(builder.Build(t.Device));

                options.MigratedTypes.Remove(SpecialAction.ActionTypeId.Profile);   // 既定の一覧から Profile だけを外した状態

                Assert.Empty(builder.Build(t.Device));
                t.PublishBindings();
                Assert.False(t.Engine.Owns(t.Device, sa));

                // 旧経路が処理する（旧ループが切り替える）
                t.RunFrame(Frame(true));
                Assert.Single(recorder.ApplyFromActionCalls);
            }
            finally
            {
                options.MigratedTypes.Clear();
                foreach (var type in saved) options.MigratedTypes.Add(type);
            }
        }

        [Theory]
        [InlineData("AutomaticUntrigger")]
        [InlineData("R1")]
        public void AProfileWithAnUntrigger_IsNotBuilt_AndTheLegacyPathStillHandlesIt(string extras)
        {
            // 7.5a: 復帰（Unload on trigger release／解除トリガー）を持つ Profile は、束縛を作らず、旧経路が切替と復帰を行う（7.5b でエンジンへ移す）
            var builder = TestHost.GetService<IActionBindingBuilder>();
            using var t = new SpecialActionTestScope(true);
            RecordingProfileApplicationService recorder = t.InstallProfileRecorder();
            SpecialAction sa = AddProfile(t, "L1", extras);

            Assert.Empty(builder.Build(t.Device));
            Assert.False(t.Engine.Owns(t.Device, sa));

            t.RunFrame(Frame(true));

            Assert.Single(recorder.ApplyFromActionCalls);                                  // 旧経路が切り替える
            Assert.Same(sa, t.Mapping.deviceRuntime[t.Device].UntriggerAction);            // 復帰の合図は、旧経路が持つ
        }

        private static long Allocated(SpecialActionTestScope t, DS4State input)
        {
            long before = GC.GetAllocatedBytesForCurrentThread();
            for (int i = 0; i < 1000; i++) t.RunFrame(input);
            return GC.GetAllocatedBytesForCurrentThread() - before;
        }

        [Fact]
        public void TheNewPath_DoesNotAddAllocationsToTheFrame_ForProfile()
        {
            long legacyHeld, legacyIdle, migratedHeld, migratedIdle;
            DS4State held = Frame(true), idle = Frame(false);

            using (var t = new SpecialActionTestScope(false))
            {
                t.InstallProfileRecorder().SimulateStateReset = false;
                AddProfile(t, "L1");
                for (int i = 0; i < 300; i++) { t.RunFrame(held); t.RunFrame(idle); }
                t.RunFrame(held);   // 立ち上がりの 1 フレーム（切替で、1 回だけ大きく割り当てる）は、測定の外へ
                legacyHeld = Allocated(t, held);
                legacyIdle = Allocated(t, idle);
            }

            using (var t = new SpecialActionTestScope(true))
            {
                t.InstallProfileRecorder().SimulateStateReset = false;
                AddProfile(t, "L1");
                for (int i = 0; i < 300; i++) { t.RunFrame(held); t.RunFrame(idle); }
                t.RunFrame(held);
                migratedHeld = Allocated(t, held);
                migratedIdle = Allocated(t, idle);
            }

            _output.WriteLine($"[Phase8Step7.5a Profile] held legacy={legacyHeld}B migrated={migratedHeld}B / idle legacy={legacyIdle}B migrated={migratedIdle}B");
            Assert.True(migratedHeld <= legacyHeld + 4096, $"押下: 新 {migratedHeld} B > 旧 {legacyHeld} B");
            Assert.True(migratedIdle <= legacyIdle + 4096, $"未操作: 新 {migratedIdle} B > 旧 {legacyIdle} B");
        }

        [Fact]
        public void MapCustom_ProfileMigrated_PerFrameTime_IsReported()
        {
            foreach (bool migrated in new[] { false, true })
            {
                using var t = new SpecialActionTestScope(migrated);
                t.InstallProfileRecorder().SimulateStateReset = false;
                string[] triggers = { "Cross", "Circle", "Square", "Triangle", "L1", "R1", "Cross/L1", "Circle/R1", "Share", "Options" };
                for (int i = 0; i < 30; i++)
                    AddProfile(t, triggers[i % triggers.Length]);

                // 押している間（切替のあと、再び切り替えない）の費用
                DS4State pressed = SpecialActionTestScope.Neutral();
                pressed.Share = true;
                for (int i = 0; i < 300; i++) t.RunFrame(pressed);

                var sw = Stopwatch.StartNew();
                for (int i = 0; i < 5000; i++) t.RunFrame(pressed);
                sw.Stop();
                _output.WriteLine($"[Phase8Step7.5a Profile] {(migrated ? "migrated" : "legacy  ")} actions=30 perFrame={sw.Elapsed.TotalMilliseconds * 1000.0 / 5000:F2}us");
            }
        }
    }
}
