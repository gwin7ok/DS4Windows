using System;
using System.Diagnostics;
using System.Linq;
using DS4Windows;
using Xunit;
using Xunit.Abstractions;

namespace DS4WindowsTests
{
    /// <summary>
    /// Phase9-0（Phase9-0-Plan.md §3）: <c>Mapping.SetCurveAndDeadzone</c>（毎フレーム、毎秒 250〜1000 回呼ばれるホットパス）の 1 回あたりの処理時間の<b>手動計測</b>。
    /// Phase9 の各 Wave（9-A の切り出しなど）の前後で実行し、中央値を比べる（許容: 悪化 5% 以内。承認済みの初期値）。
    /// 通常のテスト実行では、何もせず合格する（環境変数 <c>DS4W_RUN_BENCH=1</c> のときだけ計測する。処理時間は実行環境の負荷で揺れるため、合否の基準にしない）。
    /// 割り当て 0 の検査は <c>MappingHotPathAllocationTests</c> が担う。実行例（PowerShell）:
    /// <c>$env:DS4W_RUN_BENCH='1'; dotnet test ./DS4WindowsTests/DS4Windows.Actions.Tests.csproj -c Release /p:platform=x64 --filter FullyQualifiedName~Phase9HotPathTimingBenchmark -v n</c>
    /// （Release で計測する。Debug は最適化がなく、比較に向かない）。
    /// </summary>
    public class Phase9HotPathTimingBenchmark
    {
        private const int Slot = 0;
        private const int Iterations = 50000;
        private const int Rounds = 7;

        private static readonly Mapping Mapper = TestMappingFactory.Create();
        private readonly ITestOutputHelper _output;

        public Phase9HotPathTimingBenchmark(ITestOutputHelper output)
        {
            _output = output;
        }

        private static bool Enabled => Environment.GetEnvironmentVariable("DS4W_RUN_BENCH") == "1";

        private double MeasureNanosecondsPerCall(string name, BackingStore store)
        {
            var settings = TestRepositoryFactory.CreateProfileSettingsService(store);
            var cState = new DS4State();
            var dState = new DS4State();
            int sink = 0;

            void RunOnce()
            {
                cState.LX = 200; cState.LY = 140; cState.RX = 60; cState.RY = 180;
                cState.L2 = 200; cState.R2 = 50;
                sink += Mapper.SetCurveAndDeadzone(Slot, cState, dState, settings).LX;
            }

            for (int i = 0; i < 5000; i++) RunOnce();   // ウォームアップ（JIT）

            var samples = new double[Rounds];
            for (int round = 0; round < Rounds; round++)
            {
                long start = Stopwatch.GetTimestamp();
                for (int i = 0; i < Iterations; i++) RunOnce();
                long ticks = Stopwatch.GetTimestamp() - start;
                samples[round] = ticks * 1e9 / Stopwatch.Frequency / Iterations;
            }

            double median = samples.OrderBy(x => x).ElementAt(Rounds / 2);
            _output.WriteLine($"{name}: 中央値 {median:F1} ns / 回（最小 {samples.Min():F1}、最大 {samples.Max():F1}。sink={sink}）");
            return median;
        }

        [Fact]
        public void Measure_SetCurveAndDeadzone_Scenarios()
        {
            if (!Enabled)
            {
                _output.WriteLine("計測は、環境変数 DS4W_RUN_BENCH=1 のときだけ行う（通常は、何もしない）。");
                return;
            }

            // 1. 既定（Radial のデッドゾーン、カーブなし）
            MeasureNanosecondsPerCall("既定", new BackingStore(new EnvironmentService()));

            // 2. 出力カーブあり（左右のスティックと L2／R2。ベジェ曲線の分岐を通す）
            var curves = new BackingStore(new EnvironmentService());
            curves.setLsOutCurveMode(Slot, 2);
            curves.setRsOutCurveMode(Slot, 2);
            curves.setL2OutCurveMode(Slot, 2);
            curves.setR2OutCurveMode(Slot, 2);
            MeasureNanosecondsPerCall("出力カーブあり（LS／RS／L2／R2）", curves);

            // 3. トリガーのデッドゾーン・アンチデッドゾーン・最大ゾーン
            var triggers = new BackingStore(new EnvironmentService());
            triggers.l2ModInfo[Slot].deadZone = 20;
            triggers.l2ModInfo[Slot].antiDeadZone = 10;
            triggers.l2ModInfo[Slot].maxZone = 90;
            triggers.r2ModInfo[Slot].deadZone = 20;
            triggers.r2ModInfo[Slot].antiDeadZone = 10;
            triggers.r2ModInfo[Slot].maxZone = 90;
            MeasureNanosecondsPerCall("トリガーの補正（L2／R2）", triggers);

            // 4. ジャイロ → コントロール＋ジャイロの出力カーブ
            var gyro = new BackingStore(new EnvironmentService());
            gyro.gyroOutMode[Slot] = GyroOutMode.Controls;
            gyro.setSXOutCurveMode(Slot, 2);
            gyro.setSZOutCurveMode(Slot, 2);
            MeasureNanosecondsPerCall("ジャイロ → コントロール", gyro);
        }
    }
}
