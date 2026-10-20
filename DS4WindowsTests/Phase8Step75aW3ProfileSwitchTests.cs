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

        [Theory]
        [InlineData("R1")]
        public void AProfileWithAnUnloadTrigger_IsBuilt_SwitchesButNeverRestores(string extras)
        {
            // Phase8-Step8 8b（機能廃止。ユーザー承認）: 解除トリガー（uTrigger）のボタンによる復帰は廃止した。uTrigger を持つ Profile も束縛になり、切替は行う
            var builder = TestHost.GetService<IActionBindingBuilder>();
            using var t = new SpecialActionTestScope();
            RecordingProfileApplicationService recorder = t.InstallProfileRecorder();
            SpecialAction sa = AddProfile(t, "L1", extras);

            Assert.Single(builder.Build(t.Device));
            Assert.True(t.Engine.Owns(t.Device, sa));

            t.RunFrame(Frame(true));
            Assert.Single(recorder.ApplyFromActionCalls);

            t.RunFrame(Frame(false));
            Assert.Empty(recorder.RestoreFromActionCalls);
        }

        private static long Allocated(SpecialActionTestScope t, DS4State input)
        {
            long before = GC.GetAllocatedBytesForCurrentThread();
            for (int i = 0; i < 1000; i++) t.RunFrame(input);
            return GC.GetAllocatedBytesForCurrentThread() - before;
        }

        [Fact]
        public void MapCustom_ProfileMigrated_PerFrameTime_IsReported()
        {
            using var t = new SpecialActionTestScope();
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
            _output.WriteLine($"[Phase8Step7.5a Profile] new actions=30 perFrame={sw.Elapsed.TotalMilliseconds * 1000.0 / 5000:F2}us");
        }
    }
}
