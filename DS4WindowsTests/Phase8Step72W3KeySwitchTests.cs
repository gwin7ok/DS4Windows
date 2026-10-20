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

        private static long Allocated(SpecialActionTestScope t, DS4State input)
        {
            long before = GC.GetAllocatedBytesForCurrentThread();
            for (int i = 0; i < 1000; i++) t.RunFrame(input);
            return GC.GetAllocatedBytesForCurrentThread() - before;
        }

        [Fact]
        public void MapCustom_KeyMigrated_PerFrameTime_IsReported()
        {
            using var t = new SpecialActionTestScope();
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
            _output.WriteLine($"[Phase8Step7.2 Key] new actions=30 perFrame={sw.Elapsed.TotalMilliseconds * 1000.0 / 5000:F2}us");
        }
    }
}
