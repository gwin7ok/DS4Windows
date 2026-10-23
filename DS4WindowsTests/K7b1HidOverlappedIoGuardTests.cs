using System.IO;
using System.Text.RegularExpressions;
using Xunit;
using Xunit.Abstractions;

namespace DS4WindowsTests
{
    /// <summary>
    /// K7b-1（異常終了。メモリ破壊の疑い）の是正の保護。
    /// 非同期 I/O（ReadFile／WriteFile の保留）の間は、バッファの固定（fixed）、NativeOverlapped（スタック）、イベントを有効に保つ。
    /// 実機の I/O は単体テストできないため、ソースの形で固定する。
    /// </summary>
    public class K7b1HidOverlappedIoGuardTests
    {
        private readonly ITestOutputHelper _output;

        public K7b1HidOverlappedIoGuardTests(ITestOutputHelper output)
        {
            _output = output;
        }

        private string ReadMethod(string signature)
        {
            string path = SourceFileLocator.Find("DS4Windows/HidLibrary/HidDevice.cs");
            if (path == null)
            {
                _output.WriteLine("HidDevice.cs が見つからないため検査を省略した。");
                return null;
            }

            string text = File.ReadAllText(path);
            text = Regex.Replace(text, @"/\*.*?\*/", string.Empty, RegexOptions.Singleline);
            text = Regex.Replace(text, @"//[^\r\n]*", string.Empty);
            int start = text.IndexOf(signature);
            Assert.True(start >= 0, signature + " が見つからない");
            int end = text.IndexOf("\n        public ", start + signature.Length);
            return end < 0 ? text.Substring(start) : text.Substring(start, end - start);
        }

        [Theory]
        [InlineData("public unsafe ReadStatus ReadFile(Span<byte> inputBuffer")]
        [InlineData("public unsafe bool WriteOutputReportViaInterrupt(")]
        public void TheWaitForAPendingIo_IsInsideTheFixedBlock(string signature)
        {
            string method = ReadMethod(signature);
            if (method == null) return;

            int fixedAt = method.IndexOf("fixed (byte* pBuffer");
            int waitAt = method.IndexOf("GetOverlappedResult");
            Assert.True(fixedAt >= 0 && waitAt > fixedAt, "完了の待ちが fixed の外にある（保留中にバッファが固定されない）");

            // fixed ブロックの閉じ括弧が、待ちより後にあること
            int open = method.IndexOf('{', fixedAt);
            int depth = 0, close = -1;
            for (int i = open; i < method.Length; i++)
            {
                if (method[i] == '{') depth++;
                else if (method[i] == '}' && --depth == 0) { close = i; break; }
            }
            Assert.True(close > waitAt, "完了の待ちが fixed ブロックの外にある");
        }

        [Theory]
        [InlineData("public unsafe ReadStatus ReadFile(Span<byte> inputBuffer")]
        [InlineData("public unsafe bool WriteOutputReportViaInterrupt(")]
        public void WhenTheWaitFails_TheIoIsCancelledAndAwaited_BeforeTheBuffersAreReleased(string signature)
        {
            string method = ReadMethod(signature);
            if (method == null) return;

            int cancel = method.IndexOf("CancelIoEx(");
            Assert.True(cancel >= 0, "待ちの失敗時に I/O を取り消していない");
            Assert.True(method.IndexOf("GetOverlappedResult(", cancel) > cancel, "取り消した後、完了を待っていない");
        }
    }
}
