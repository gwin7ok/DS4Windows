using System.Text;
using System.Text.RegularExpressions;
using Xunit;
using DS4Windows.UI;

namespace DS4WindowsTests
{
    /// <summary>
    /// <c>MainWindow.WndProc</c> の分割。F-2a: コマンドの読み取りの値、F-2b: ウィンドウ手続きの分岐と、受け取りの握りつぶし（ソースの照合）。コマンドの順序と条件は `IpcCommandProcessorTests` が挙動で確かめる。
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
        public void TheCopyData_StillEatsEveryException_AndChecksTheSizeFirst()
        {
            string ipc = SourceText.Read("DS4Windows/UI/MainWindow.Ipc.cs");
            if (ipc == null) return;

            Match m = Regex.Match(ipc, @"private void HandleCopyData\(IntPtr lParam\)\s*\{\s*try\s*\{(?<body>.*?)\}\s*catch\s*\{", RegexOptions.Singleline);
            Assert.True(m.Success, "HandleCopyData は、全体を try／catch（握りつぶし）で囲む");
            string body = m.Groups["body"].Value;
            Assert.True(body.IndexOf("IpcCommandParser.IsValidSize(", System.StringComparison.Ordinal) < body.IndexOf("Marshal.Copy(", System.StringComparison.Ordinal), "サイズの検査が、バッファの読み取りより先");
        }

    }
}
