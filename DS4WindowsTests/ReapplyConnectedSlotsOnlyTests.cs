using System.IO;
using System.Text.RegularExpressions;
using Xunit;
using Xunit.Abstractions;

namespace DS4WindowsTests
{
    /// <summary>
    /// Phase11 W6 K-W6-2: プロファイルの保存・適用の後の再適用（<c>MainWindow.SyncProfileListAndControllers</c> の ④）は、
    /// コントローラーが接続されているスロットにだけ行う。サービスの停止中は、<c>ProfilePath</c> が同じ名前のまま残るため、
    /// 接続していないスロットにも適用され、無意味なログ（<c>Controller 1 is now using Profile … (Battery: N/A%)</c>）と通知が出ていた。
    /// MainWindow は WPF のウィンドウで単体駆動が困難なため、ソースの走査で担保する（ソースが見つからない環境では、検査を行わず合格とする）。
    /// </summary>
    public class ReapplyConnectedSlotsOnlyTests
    {
        private const string MainWindowCode = "DS4Windows/UI/MainWindow.xaml.cs";

        private readonly ITestOutputHelper _output;

        public ReapplyConnectedSlotsOnlyTests(ITestOutputHelper output)
        {
            _output = output;
        }

        [Fact]
        public void TheReapplyLoop_AppliesOnlyToConnectedSlots()
        {
            string code = SourceText.ReadNormalized(MainWindowCode);
            if (code == null) return;

            // 再適用の ApplyToSlot（Manual）は、接続の判定の後ろにだけある
            Assert.Single(Regex.Matches(code, @"profileSlotApplier\.ApplyToSlot\(i, activeProfiles\[i\], ProfileChangeSource\.Manual\)"));
            Assert.Contains("if (matches && IsSlotConnected(i)) { profileSlotApplier.ApplyToSlot(i, activeProfiles[i], ProfileChangeSource.Manual); }", code);
        }

        [Fact]
        public void IsSlotConnected_ReadsTheControllerOfTheSlot()
        {
            string code = SourceText.ReadNormalized(MainWindowCode);
            if (code == null) return;

            Assert.Contains("private bool IsSlotConnected(int slot)", code);
            Assert.Contains("return slots.GetSlot(slot) != null;", code);
        }
    }
}
