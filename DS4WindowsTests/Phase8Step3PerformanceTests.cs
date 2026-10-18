using System;
using System.Diagnostics;
using DS4Windows;
using DS4Windows.Actions;
using DS4Windows.DI;
using Xunit;
using Xunit.Abstractions;

namespace DS4WindowsTests
{
    /// <summary>
    /// Phase8-Step3 W5（Phase8-Step3-Plan.md §2.5。Q5＝A）: 判定の割り当て 0 の検査と、処理時間の比較の基盤。
    /// 割り当てが 0 であることは厳密に検査する。処理時間は出力するだけで、失敗条件にしない（環境依存のため）。
    /// Step7 の各 PR の前後で、このテストの出力（1 フレームあたりの時間）を比較する。
    /// </summary>
    [Collection("ServiceProviderHolder")]
    public class Phase8Step3PerformanceTests
    {
        private const int Device = 2;
        private const int BindingCount = 30; // 1 フレームに評価する束縛の数（スペシャルアクション＋Controls タブの想定の上限寄り）
        private const int Frames = 20000;

        private readonly ITestOutputHelper _output;

        public Phase8Step3PerformanceTests(ITestOutputHelper output)
        {
            _output = output;
        }

        private static IProfileSettingsService Settings()
        {
            DS4WinWPF.AppHost.CreateHost();
            return TestHost.GetService<IProfileSettingsService>();
        }

        private static ComboTriggerInputAction[] BuildBindings(IProfileSettingsService settings)
        {
            var result = new ComboTriggerInputAction[BindingCount];
            var buttons = new[] { DS4Controls.L1, DS4Controls.R1, DS4Controls.Cross, DS4Controls.Circle, DS4Controls.Square, DS4Controls.Triangle, DS4Controls.L2, DS4Controls.R2 };
            for (int i = 0; i < result.Length; i++)
            {
                DS4Controls a = buttons[i % buttons.Length];
                DS4Controls b = buttons[(i + 3) % buttons.Length];
                result[i] = new ComboTriggerInputAction("b" + i, new[] { a, b }, settings,
                    delaySeconds: i % 5 == 0 ? 0.5 : 0.0, invertOnRelease: i % 7 == 0);
            }

            return result;
        }

        private static void RunFrame(TriggerInput input, DS4State state, DS4StateFieldMapping fields, ComboTriggerInputAction[] bindings, int frame)
        {
            input.Update(state, null, null, null, fields);
            for (int i = 0; i < bindings.Length; i++)
                bindings[i].Evaluate(input);
        }

        [Fact]
        public void OneFrame_UpdateAndEvaluateOfManyBindings_DoesNotAllocate()
        {
            IProfileSettingsService settings = Settings();
            var input = new TriggerInput(Device, settings);
            var state = new DS4State();
            var fields = new DS4StateFieldMapping();
            ComboTriggerInputAction[] bindings = BuildBindings(settings);

            // 押下と解放が混ざる入力（準備状態・遅延の更新も通る）
            for (int frame = 0; frame < 200; frame++)
            {
                SetInput(fields, frame);
                RunFrame(input, state, fields, bindings, frame);
            }

            long before = GC.GetAllocatedBytesForCurrentThread();
            for (int frame = 0; frame < Frames; frame++)
            {
                SetInput(fields, frame);
                RunFrame(input, state, fields, bindings, frame);
            }

            long allocated = GC.GetAllocatedBytesForCurrentThread() - before;
            Assert.True(allocated == 0, $"{Frames} フレームで {allocated} バイトを割り当てた");
        }

        [Fact]
        public void Timing_PerFrame_IsReported()
        {
            IProfileSettingsService settings = Settings();
            var input = new TriggerInput(Device, settings);
            var state = new DS4State();
            var fields = new DS4StateFieldMapping();
            ComboTriggerInputAction[] bindings = BuildBindings(settings);

            for (int frame = 0; frame < 500; frame++)
            {
                SetInput(fields, frame);
                RunFrame(input, state, fields, bindings, frame);
            }

            var sw = new Stopwatch(); // 計測対象の外で作る（Stopwatch 自体の 40 バイトを数えない）
            long allocBefore = GC.GetAllocatedBytesForCurrentThread();
            sw.Start();
            for (int frame = 0; frame < Frames; frame++)
            {
                SetInput(fields, frame);
                RunFrame(input, state, fields, bindings, frame);
            }

            sw.Stop();
            long allocated = GC.GetAllocatedBytesForCurrentThread() - allocBefore;

            double microsecondsPerFrame = sw.Elapsed.TotalMilliseconds * 1000.0 / Frames;
            double nanosecondsPerEvaluate = sw.Elapsed.TotalMilliseconds * 1000000.0 / Frames / BindingCount;
            _output.WriteLine($"[Phase8Step3 timing] bindings={BindingCount} frames={Frames} " +
                $"perFrame={microsecondsPerFrame:F2}us perEvaluate={nanosecondsPerEvaluate:F1}ns allocated={allocated}B");

            // 時間は出力するだけ。割り当ては厳密に検査する。
            Assert.Equal(0L, allocated);
        }

        private static void SetInput(DS4StateFieldMapping fields, int frame)
        {
            // 周期的に押下・解放する（ボタンごとに周期を変える）
            for (int c = 0; c < fields.buttons.Length; c++)
                fields.buttons[c] = ((frame + c) % 9) < 4;
        }
    }
}
