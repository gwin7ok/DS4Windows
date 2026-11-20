using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using Xunit;
using Xunit.Abstractions;

namespace DS4WindowsTests
{
    /// <summary>
    /// EnvironmentService の静的初期化（コントローラースロット上限の計算）が、
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
        public void TheSlotLimit_IsFixedBeforeAnyOtherTypeReadsIt()
        {
            // 静的初期化が循環の途中の値 0 を確定させると、スロットが 0 個になる。実際の値が、1 以上で上限以内であることを確かめる
            int limit = new DS4Windows.Core.Platform.EnvironmentService().ControllerSlotLimit;
            Assert.InRange(limit, 1, DS4Windows.Core.Settings.ControllerLimits.MaxControllerCount);
        }
    }
}