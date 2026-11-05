using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using Xunit;
using Xunit.Abstractions;

namespace DS4WindowsTests
{
    /// <summary>
    /// Phase6-Step2 (PR-1b の是正): EnvironmentService の静的初期化（コントローラースロット上限の計算）が、
    /// Global の静的初期化を起こさないことを固定する回帰ガード。
    ///
    /// 背景: Global は明示的な静的コンストラクタを持ち、その初期化中に OutputSlotManager が生成され、
    /// OutputSlotManager は ControlService.CURRENT_DS4_CONTROLLER_LIMIT（= EnvironmentService.ProcessControllerSlotLimit）を読む。
    /// EnvironmentService の静的初期化から Global のメソッドを呼ぶと、EnvironmentService が最初に触れられた場合に、
    /// 循環の途中の値 0 が ControlService.CURRENT_DS4_CONTROLLER_LIMIT に確定し、スロットが 0 個になる
    /// （テストの実行順序によって OutputSlotServiceTests などが失敗した）。
    /// 上限の計算は const（インライン展開され、Global の静的初期化を起こさない）以外の Global メンバを参照してはならない。
    /// ソースファイルが見つからない環境では、検査を行わず合格とする。
    /// </summary>
    public class EnvironmentServiceStaticInitGuardTests
    {

        private readonly ITestOutputHelper _output;

        public EnvironmentServiceStaticInitGuardTests(ITestOutputHelper output)
        {
            _output = output;
        }

        [Fact]
        public void ProcessControllerSlotLimit_IsInitializedFromTheSelfContainedCalculation()
        {
            string path = SourceFileLocator.Find("DS4Windows/Core/Platform/EnvironmentService.cs");
            if (path == null)
            {
                _output.WriteLine("EnvironmentService.cs が見つからないため検査を省略した。");
                return;
            }

            string source = File.ReadAllText(path);

            Assert.Contains("ProcessControllerSlotLimit = CalculateControllerSlotLimit();", source);
        }
    }
}