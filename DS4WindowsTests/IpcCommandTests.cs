using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Xunit;
using DS4Windows.UI;

namespace DS4WindowsTests
{
    /// <summary>
    /// Phase9 9-F F-2（Phase9-F-Plan.md §6）: <c>MainWindow.WndProc</c> の分割。F-2a: コマンドの読み取りの値、F-2b: 分岐の順序とコマンド名の集合（ソースの照合）。
    /// </summary>
    public class IpcCommandTests
    {
        // ---- F-2a: IpcCommandParser ----

        [Theory]
        [InlineData(3, false)]
        [InlineData(4, true)]
        [InlineData(256, true)]
        [InlineData(257, false)]
        [InlineData(0, false)]
        public void TheCommandSize_MustBeBetween4And256Bytes(int size, bool valid)
        {
            Assert.Equal(valid, IpcCommandParser.IsValidSize(size));
        }

        [Fact]
        public void Parse_SplitsByDots_AndLowercasesOnlyTheCommandName()
        {
            string[] parts = IpcCommandParser.Parse(Encoding.ASCII.GetBytes("LoadProfile.1.GameSnake"));

            Assert.Equal(new[] { "loadprofile", "1", "GameSnake" }, parts);   // 引数（プロファイル名）の大文字・小文字は、そのまま
        }

        [Fact]
        public void Parse_ASingleWord_IsTheCommandName()
        {
            Assert.Equal(new[] { "start" }, IpcCommandParser.Parse(Encoding.ASCII.GetBytes("Start")));
            Assert.Equal(new[] { "disconnect", "1" }, IpcCommandParser.Parse(Encoding.ASCII.GetBytes("Disconnect.1")));
        }

        // ---- F-2b: 分岐の順序とコマンド名 ----

        [Fact]
        public void TheWndProc_DispatchesTheTwoMessageKinds()
        {
            string main = SourceText.Read("DS4Windows/UI/MainWindow.xaml.cs");
            if (main == null) return;

            Assert.Matches(@"case Util\.WM_DEVICECHANGE:\s*HandleDeviceChange\(wParam\);\s*break;\s*case WM_COPYDATA:\s*HandleCopyData\(lParam\);", main);
        }

        [Fact]
        public void TheCommands_AreDispatchedInTheOriginalOrder_WithTheOriginalConditions()
        {
            // Phase13-Step11 11-b: コマンドの実行は IpcCommandProcessor へ移した（受け付けの順序と条件は同じ）
            string ipc = SourceText.Read("DS4Windows/Processing/Services/IpcCommandProcessor.cs");
            if (ipc == null) return;

            string[] order =
            {
                "strData[0] == \"start\"",
                "strData[0] == \"stop\"",
                "strData[0] == \"cycle\"",
                "strData[0] == \"shutdown\"",
                "strData[0] == \"disconnect\"", "Disconnect(host, strData);",
                "(strData[0] == \"changeledcolor\") && strData.Length >= 5", "ChangeLedColor(host, strData);",
                "(strData[0] == \"loadprofile\" || strData[0] == \"loadtempprofile\") && strData.Length >= 3", "LoadProfile(host, strData);",
                "strData[0] == \"outputslot\" && strData.Length >= 3", "OutputSlot(strData);",
                "strData[0] == \"query\" && strData.Length >= 3", "Query(host, strData);",
            };

            int at = -1;
            foreach (string token in order)
            {
                int next = ipc.IndexOf(token, at + 1, System.StringComparison.Ordinal);
                Assert.True(next > at, token + " が、期待の順序にない");
                at = next;
            }
        }

        [Fact]
        public void TheCopyData_StillEatsEveryException_AndChecksTheSizeFirst()
        {
            string ipc = SourceText.Read("DS4Windows/UI/MainWindow.Ipc.cs");
            if (ipc == null) return;

            Match m = Regex.Match(ipc, @"private void HandleCopyData\(IntPtr lParam\)\s*\{\s*try\s*\{(?<body>.*?)\}\s*catch\s*\{", RegexOptions.Singleline);
            Assert.True(m.Success, "HandleCopyData は、全体を try／catch（握りつぶし）で囲む");
            string body = m.Groups["body"].Value;
            Assert.True(body.IndexOf("IpcCommandParser.IsValidSize(", System.StringComparison.Ordinal) < body.IndexOf("Marshal.Copy(", System.StringComparison.Ordinal), "サイズの検査が、バッファの読み取りより先");
        }

        [Fact]
        public void TheOldWndProcBody_IsGone()
        {
            string main = SourceText.Read("DS4Windows/UI/MainWindow.xaml.cs");
            if (main == null) return;

            Assert.DoesNotContain("strData[0]", main);
            Assert.DoesNotContain("Encoding.ASCII.GetString(buffer)", main);
        }
    }
}
