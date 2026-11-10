using DS4Windows.Core.Model;
using System;
using System.Diagnostics;
using System.Linq;
using Xunit;
using Xunit.Abstractions;
using DS4Windows.Core.Settings;
using DS4Windows.Input;
using DS4Windows.Processing.Actions;

namespace DS4WindowsTests
{
    /// <summary>
    /// Phase8-Step7 7.4-W3（Phase8-Step7-4-Plan.md §3 W3）: Program をスイッチに入れた状態の、ロールバック・割り当て・性能の確認。
    /// 実際のプログラムは起動しない（存在しない実行ファイルのパス）。他の Host 構築テストと同じコレクションで直列に実行する。
    /// </summary>
    [Collection(SharedHostStateCollection.Name)]
    public class ProgramSwitchTests
    {
        private readonly ITestOutputHelper _output;

        public ProgramSwitchTests(ITestOutputHelper output)
        {
            _output = output;
        }

        private static ControllerState Frame(bool l1)
        {
            ControllerState state = SpecialActionTestScope.Neutral();
            state.L1 = l1;
            return state;
        }

        private static SpecialAction AddProgram(SpecialActionTestScope t, string triggers)
        {
            SpecialAction sa = t.Add(new SpecialAction("p8s74w3_prog_" + Guid.NewGuid().ToString("N"), triggers, "Program",
                @"C:\p8s74-nonexistent\" + Guid.NewGuid().ToString("N") + ".exe", 0, string.Empty));
            ActionInstanceState state = t.StateOf(sa);
            state.RequiresFreshPressAfterReset = false;
            state.IsExecuting = false;
            return sa;
        }

        private static long Allocated(SpecialActionTestScope t, ControllerState input)
        {
            long before = GC.GetAllocatedBytesForCurrentThread();
            for (int i = 0; i < 1000; i++) t.RunFrame(input);
            return GC.GetAllocatedBytesForCurrentThread() - before;
        }

        [Fact]
        public void MapCustom_ProgramMigrated_PerFrameTime_IsReported()
        {
            using var t = new SpecialActionTestScope();
            string[] triggers = { "Cross", "Circle", "Square", "Triangle", "L1", "R1", "Cross/L1", "Circle/R1", "Share", "Options" };
            for (int i = 0; i < 30; i++)
                AddProgram(t, triggers[i % triggers.Length]);

            ControllerState pressed = SpecialActionTestScope.Neutral();
            pressed.Cross = true;
            pressed.L1 = true;
            for (int i = 0; i < 300; i++) t.RunFrame(pressed);

            var sw = Stopwatch.StartNew();
            for (int i = 0; i < 5000; i++) t.RunFrame(pressed);
            sw.Stop();
            _output.WriteLine($"[Phase8Step7.4 Program] new actions=30 perFrame={sw.Elapsed.TotalMilliseconds * 1000.0 / 5000:F2}us");
        }
    }
}
