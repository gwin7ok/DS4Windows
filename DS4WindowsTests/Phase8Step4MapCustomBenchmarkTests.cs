using System;
using System.Diagnostics;
using DS4Windows;
using Xunit;
using Xunit.Abstractions;

namespace DS4WindowsTests
{
    /// <summary>
    /// Phase8-Step4 W1（Phase8-Step4-Plan.md §2.1／§3）: <c>Mapping.MapCustom</c> 全体の簡易ベンチマーク（出力のみ。失敗条件にしない）。
    /// Step4 の配線（W4）の前後で結果を比較する（許容: 現行の +5% 以内）。配線の<b>前</b>の値を、W1 の時点で記録しておくこと。
    /// ボタン種別（Press）30 件の特殊アクションを登録し、何も押さない状態と、トリガーのボタンを押した状態を、それぞれ繰り返し評価する。
    /// </summary>
    [Collection("ServiceProviderHolder")]
    public class Phase8Step4MapCustomBenchmarkTests
    {
        private const int Actions = 30;
        private const int Warmup = 300;
        private const int Frames = 5000;

        private readonly ITestOutputHelper _output;

        public Phase8Step4MapCustomBenchmarkTests(ITestOutputHelper output)
        {
            _output = output;
        }

        [Fact]
        public void MapCustom_PerFrameTime_IsReported()
        {
            using (var t = new SpecialActionTestScope())
            {
                string[] triggers = { "Cross", "Circle", "Square", "Triangle", "L1", "R1", "Cross/L1", "Circle/R1", "Share", "Options" };
                for (int i = 0; i < Actions; i++)
                    t.AddButtonPress(triggers[i % triggers.Length]);

                DS4State idle = SpecialActionTestScope.Neutral();
                DS4State pressed = SpecialActionTestScope.Neutral();
                pressed.Cross = true;
                pressed.L1 = true;

                Measure(t, "idle   ", idle);
                Measure(t, "pressed", pressed);
            }
        }

        private void Measure(SpecialActionTestScope t, string label, DS4State input)
        {
            for (int i = 0; i < Warmup; i++) t.RunFrame(input);

            var sw = new Stopwatch();
            long allocBefore = GC.GetAllocatedBytesForCurrentThread();
            sw.Start();
            for (int i = 0; i < Frames; i++) t.RunFrame(input);
            sw.Stop();
            long allocated = GC.GetAllocatedBytesForCurrentThread() - allocBefore;

            _output.WriteLine($"[Phase8Step4 MapCustom] {label} actions={Actions} frames={Frames} " +
                $"perFrame={sw.Elapsed.TotalMilliseconds * 1000.0 / Frames:F2}us allocatedPerFrame={allocated / (double)Frames:F0}B");
        }
    }
}
