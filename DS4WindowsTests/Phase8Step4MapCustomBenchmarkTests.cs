using System;
using System.Collections.Generic;
using System.Diagnostics;
using DS4Windows;
using DS4Windows.Actions;
using DS4Windows.DI;
using Xunit;
using Xunit.Abstractions;

namespace DS4WindowsTests
{
    /// <summary>
    /// Phase8-Step4 W1／W5（Phase8-Step4-Plan.md §2.1／§3）: <c>Mapping.MapCustom</c> 全体の簡易ベンチマーク（出力のみ。失敗条件にしない）。
    /// ボタン種別（Press）30 件の特殊アクションを登録し、何も押さない状態と、トリガーのボタンを押した状態を、それぞれ繰り返し評価する。
    /// W4（配線）の追加分の費用は、<see cref="WiringOverhead_PerFrame_IsReported"/> が、同じ実行の中で <c>MapCustom</c> 全体と並べて出力する
    /// （配線の前のコードを別に実行しなくても、割合を比べられる。許容: +5% 以内）。
    /// 結果は標準出力に出る。コンソールに出すには <c>--logger "console;verbosity=detailed"</c> を付けて実行する。
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

        private static List<SpecialAction> AddActions(SpecialActionTestScope t)
        {
            string[] triggers = { "Cross", "Circle", "Square", "Triangle", "L1", "R1", "Cross/L1", "Circle/R1", "Share", "Options" };
            var list = new List<SpecialAction>();
            for (int i = 0; i < Actions; i++)
                list.Add(t.AddButtonPress(triggers[i % triggers.Length]));

            return list;
        }

        [Fact]
        public void MapCustom_PerFrameTime_IsReported()
        {
            using (var t = new SpecialActionTestScope())
            {
                AddActions(t);

                DS4State idle = SpecialActionTestScope.Neutral();
                DS4State pressed = SpecialActionTestScope.Neutral();
                pressed.Cross = true;
                pressed.L1 = true;

                Measure(t, "idle   ", idle);
                Measure(t, "pressed", pressed);
            }
        }

        private double Measure(SpecialActionTestScope t, string label, DS4State input)
        {
            for (int i = 0; i < Warmup; i++) t.RunFrame(input);

            var sw = new Stopwatch();
            long allocBefore = GC.GetAllocatedBytesForCurrentThread();
            sw.Start();
            for (int i = 0; i < Frames; i++) t.RunFrame(input);
            sw.Stop();
            long allocated = GC.GetAllocatedBytesForCurrentThread() - allocBefore;

            double perFrame = sw.Elapsed.TotalMilliseconds * 1000.0 / Frames;
            _output.WriteLine($"[Phase8Step4 MapCustom] {label} actions={Actions} frames={Frames} " +
                $"perFrame={perFrame:F2}us allocatedPerFrame={allocated / (double)Frames:F0}B");
            return perFrame;
        }

        /// <summary>
        /// W4 で <c>MapCustom</c> に足した処理（移行対象が空の間に毎フレーム走る分）だけを取り出して計り、<c>MapCustom</c> 全体の時間と並べる:
        /// 入力の更新（<c>TriggerInput.Update</c>）、旧ループと Stage3 の <c>Owns</c>（アクションごとに 2 回）、<c>ReleaseSuppressed</c>、<c>ApplyFrameOutputs</c>。
        /// </summary>
        [Fact]
        public void WiringOverhead_PerFrame_IsReported()
        {
            using (var t = new SpecialActionTestScope())
            {
                List<SpecialAction> actions = AddActions(t);
                DS4State pressed = SpecialActionTestScope.Neutral();
                pressed.Cross = true;
                pressed.L1 = true;
                double mapCustom = Measure(t, "baseline", pressed);

                ITriggerDispatchEngine engine = t.Engine;
                var input = new TriggerInput(t.Device, TestHost.GetService<IProfileSettingsService>());
                var fields = new DS4StateFieldMapping();
                var frame = new FrameOutput();
                frame.Bind(fields, new SyntheticState());

                void OneFrame()
                {
                    input.Update(pressed, null, null, null, fields);
                    for (int i = 0; i < actions.Count; i++)
                    {
                        engine.Owns(t.Device, actions[i]);   // 旧ループ
                        engine.Owns(t.Device, actions[i]);   // Stage3
                    }

                    engine.ReleaseSuppressed(t.Device, input);
                    engine.ApplyFrameOutputs(t.Device, input, frame);
                }

                for (int i = 0; i < Warmup; i++) OneFrame();

                var sw = new Stopwatch();
                long allocBefore = GC.GetAllocatedBytesForCurrentThread();
                sw.Start();
                for (int i = 0; i < Frames * 4; i++) OneFrame();
                sw.Stop();
                long allocated = GC.GetAllocatedBytesForCurrentThread() - allocBefore;

                double overhead = sw.Elapsed.TotalMilliseconds * 1000.0 / (Frames * 4);
                _output.WriteLine($"[Phase8Step4 wiring] overheadPerFrame={overhead:F3}us mapCustomPerFrame={mapCustom:F2}us " +
                    $"ratio={overhead / mapCustom * 100.0:F2}% allocatedPerFrame={allocated / (double)(Frames * 4):F1}B");

                // 割り当ては厳密に検査する（時間は出力のみ）
                Assert.Equal(0L, allocated);
            }
        }
    }
}
