using System;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using DS4Windows;
using Xunit;
using Xunit.Abstractions;

namespace DS4WindowsTests
{
    /// <summary>
    /// Phase13-Step5b（Phase13-Step5b-Plan.md §7 A/B 計測）: 入力レポート処理（<c>On_Report</c>）の 1 回あたりの処理時間の<b>手動計測</b>。
    /// 通常のテスト実行では何もせず合格する（環境変数 <c>DS4W_RUN_BENCH=1</c> のときだけ計測する。処理時間は実行環境の負荷で揺れるため、合否の基準にしない）。
    /// before＝5b-a（<c>ControlService.On_Report</c>）、after＝5b-c 後（<c>InputReportProcessor</c>）。同じ条件で測り、悪化 5% 以内を合格基準とする。
    /// 実行例（PowerShell）:
    /// <c>$env:DS4W_RUN_BENCH='1'; dotnet test ./DS4WindowsTests/DS4Windows.Actions.Tests.csproj -c Release /p:platform=x64 --filter FullyQualifiedName~Phase13Step5bOnReportTimingBenchmark -v n</c>
    /// </summary>
    [Collection("ServiceProviderHolder")]
    public class Phase13Step5bOnReportTimingBenchmark
    {
        private const int Iterations = 200_000;
        private const int Rounds = 7;
        private const BindingFlags All = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        private readonly ITestOutputHelper _output;

        public Phase13Step5bOnReportTimingBenchmark(ITestOutputHelper output)
        {
            _output = output;
        }

        private static bool Enabled => Environment.GetEnvironmentVariable("DS4W_RUN_BENCH") == "1";

        private static void Set(object target, string name, object value)
        {
            FieldInfo f = null;
            for (Type t = target.GetType(); t != null && f == null; t = t.BaseType)
                f = t.GetField(name, All | BindingFlags.DeclaredOnly);
            Assert.True(f != null, name + " が見つからない");
            f.SetValue(target, value);
        }

        [Fact]
        public void OnReport_Timing()
        {
            if (!Enabled)
            {
                _output.WriteLine("DS4W_RUN_BENCH=1 のときだけ計測する（通常は何もしない）");
                return;
            }

            DS4WinWPF.AppHost.CreateHost();
            // before（5b-a）は ControlService.On_Report のデリゲート。after（5b-c 以降）は IInputReportProcessor.OnReport のデリゲート
            TestHost.GetService<ControlService>();   // TempStates／PreviousStates は ControlService のコンストラクターが作る（本番では起動時に必ず構築される）
            var processor = TestHost.GetService<DS4Windows.DI.IInputReportProcessor>();
            var onReport = (Action<DS4Device, EventArgs, int>)Delegate.CreateDelegate(
                typeof(Action<DS4Device, EventArgs, int>), processor, typeof(DS4Windows.DI.IInputReportProcessor).GetMethod("OnReport"));

            TestDs4Device d = TestDs4Device.Create();
            var cState = new DS4State { LX = 130, LY = 120 };
            Set(d, "cState", cState);
            Set(d, "pState", new DS4State());
            Set(d, "jointState", new DS4State());
            Set(d, "jointPreviousState", new DS4State());
            Set(d, "primaryDevice", true);
            Set(d, "outputMapGyro", true);
            Set(d, "jointDeviceSlotNumber", DS4Device.DEFAULT_JOINT_SLOT_NUMBER);
            d.firstReport = false;
            d.firstActive = DateTime.UtcNow;

            for (int i = 0; i < 20000; i++) onReport(d, EventArgs.Empty, 0);   // ウォームアップ（JIT）

            // 順序の偏りを見るため 2 回に分けて測り、全体の中央値も出す
            var samples = new double[Rounds];
            for (int round = 0; round < Rounds; round++)
            {
                long start = Stopwatch.GetTimestamp();
                for (int i = 0; i < Iterations; i++) onReport(d, EventArgs.Empty, 0);
                long ticks = Stopwatch.GetTimestamp() - start;
                samples[round] = ticks * 1e9 / Stopwatch.Frequency / Iterations;
            }

            double median = samples.OrderBy(x => x).ElementAt(Rounds / 2);
            _output.WriteLine($"On_Report: 中央値 {median:F1} ns / 回（最小 {samples.Min():F1}、最大 {samples.Max():F1}）");

            long before = GC.GetAllocatedBytesForCurrentThread();
            for (int i = 0; i < Iterations; i++) onReport(d, EventArgs.Empty, 0);
            long allocated = GC.GetAllocatedBytesForCurrentThread() - before;
            _output.WriteLine($"On_Report の割り当て（デリゲート呼び出し）: {allocated} bytes / {Iterations} 回");
        }
    }
}
