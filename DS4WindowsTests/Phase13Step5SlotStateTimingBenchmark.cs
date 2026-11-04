using System;
using System.Diagnostics;
using System.Linq;
using DS4Windows;
using DS4Windows.DI;
using DS4Windows.Services;
using Xunit;
using Xunit.Abstractions;

namespace DS4WindowsTests
{
    /// <summary>
    /// Phase13-Step5（Phase13-Step5-Plan.md §6 計測②）: スロット状態の読み取りの 1 回あたりの処理時間の<b>手動計測</b>。
    /// 通常のテスト実行では何もせず合格する（環境変数 <c>DS4W_RUN_BENCH=1</c> のときだけ計測する。処理時間は実行環境の負荷で揺れるため、合否の基準にしない）。
    /// 比べる対象:
    ///   (a) 現状: 旧 <c>ControlService</c> と同じ形（クラスの private フィールドの配列を、毎回そのフィールドから読む）
    ///   (b) 5b 以降: 窓口から一度だけ取得して <c>readonly</c> フィールドへ保持した配列を読む
    ///   (c) 窓口（interface のプロパティ）を毎回通して読む（接続・切断の経路。ホットパスではない）
    /// 合格基準: (b) が (a) に対して悪化 5% 以内。実行例（PowerShell）:
    /// <c>$env:DS4W_RUN_BENCH='1'; dotnet test ./DS4WindowsTests/DS4Windows.Actions.Tests.csproj -c Release /p:platform=x64 --filter FullyQualifiedName~Phase13Step5SlotStateTimingBenchmark -v n</c>
    /// </summary>
    public class Phase13Step5SlotStateTimingBenchmark
    {
        private const int Iterations = 5_000_000;
        private const int Rounds = 7;
        private readonly ITestOutputHelper _output;

        public Phase13Step5SlotStateTimingBenchmark(ITestOutputHelper output)
        {
            _output = output;
        }

        private static bool Enabled => Environment.GetEnvironmentVariable("DS4W_RUN_BENCH") == "1";

        /// <summary>(a) 旧 ControlService と同じ形。配列はこのクラスが所有する private フィールド。</summary>
        private sealed class OwnerWithArrays
        {
            private readonly DS4State[] CurrentState = new DS4State[ControllerLimits.MaxControllerCount];
            private readonly DS4State[] TempState = new DS4State[ControllerLimits.MaxControllerCount];
            private readonly Mouse[] touchPad = new Mouse[ControllerLimits.MaxControllerCount];

            public OwnerWithArrays()
            {
                for (int i = 0; i < CurrentState.Length; i++) { CurrentState[i] = new DS4State(); TempState[i] = new DS4State(); }
            }

            public int Read(int ind)
            {
                int acc = 0;
                if (CurrentState[ind] != null) acc++;
                if (TempState[ind] != null) acc++;
                if (touchPad[ind] != null) acc++;
                return acc;
            }
        }

        /// <summary>(b) 5b 以降の形。窓口からコンストラクターで一度だけ取得して保持する。</summary>
        private sealed class OwnerWithCachedReferences
        {
            private readonly DS4State[] _currentState;
            private readonly DS4State[] _tempState;
            private readonly Mouse[] _touchPad;

            public OwnerWithCachedReferences(IDeviceSlotState slots)
            {
                _currentState = slots.CurrentStates;
                _tempState = slots.TempStates;
                _touchPad = slots.TouchPads;
                for (int i = 0; i < _currentState.Length; i++) { _currentState[i] = new DS4State(); _tempState[i] = new DS4State(); }
            }

            public int Read(int ind)
            {
                int acc = 0;
                if (_currentState[ind] != null) acc++;
                if (_tempState[ind] != null) acc++;
                if (_touchPad[ind] != null) acc++;
                return acc;
            }
        }

        private double Measure(string name, Func<int, int> read)
        {
            int sink = 0;
            for (int i = 0; i < 100000; i++) sink += read(i & 3);   // ウォームアップ（JIT）

            var samples = new double[Rounds];
            for (int round = 0; round < Rounds; round++)
            {
                long start = Stopwatch.GetTimestamp();
                for (int i = 0; i < Iterations; i++) sink += read(i & 3);
                long ticks = Stopwatch.GetTimestamp() - start;
                samples[round] = ticks * 1e9 / Stopwatch.Frequency / Iterations;
            }

            double median = samples.OrderBy(x => x).ElementAt(Rounds / 2);
            _output.WriteLine($"{name}: 中央値 {median:F2} ns / 回（最小 {samples.Min():F2}、最大 {samples.Max():F2}。sink={sink}）");
            return median;
        }

        [Fact]
        public void SlotStateReads_Timing()
        {
            if (!Enabled)
            {
                _output.WriteLine("DS4W_RUN_BENCH=1 のときだけ計測する（通常は何もしない）");
                return;
            }

            var a = new OwnerWithArrays();
            IDeviceSlotState slots = new DeviceSlotState();
            var b = new OwnerWithCachedReferences(slots);

            // 同じデリゲート呼び出しのコストが両方に乗るので、差を比べられる。順序の偏り（先に測る方が有利・不利）を見るため、a→b→b→a の順に 2 回ずつ測る
            double ta1 = Measure("(a) 旧形: private フィールドの配列 [1]", a.Read);
            double tb1 = Measure("(b) 新形: 保持した readonly の配列 [1]", b.Read);
            double tb2 = Measure("(b) 新形: 保持した readonly の配列 [2]", b.Read);
            double ta2 = Measure("(a) 旧形: private フィールドの配列 [2]", a.Read);
            double tc = Measure("(c) 窓口を毎回通す", ind =>
            {
                int acc = 0;
                if (slots.CurrentStates[ind] != null) acc++;
                if (slots.TempStates[ind] != null) acc++;
                if (slots.TouchPads[ind] != null) acc++;
                return acc;
            });

            double ta = (ta1 + ta2) / 2, tb = (tb1 + tb2) / 2;
            _output.WriteLine($"(b)/(a) = {tb / ta:P1}、(c)/(a) = {tc / ta:P1}（合格基準: (b) が (a) の 105% 以内）");
        }
    }
}
