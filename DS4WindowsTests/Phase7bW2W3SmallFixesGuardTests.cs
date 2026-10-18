using System.IO;
using System.Text.RegularExpressions;
using DS4Windows;
using DS4Windows.DS4Control;
using Xunit;
using Xunit.Abstractions;

namespace DS4WindowsTests
{
    /// <summary>
    /// Phase7b W2・W3（Phase7b-Plan.md §2 項目 5・6、Q3＝A、Q4＝A）の回帰ガード。
    /// - W2（K4-1）: <c>Mouse.SixMouseStick</c> がジャイロのマウススティックの水平軸を、固定の 0（1 台目）ではなく、
    ///   受け持つ台（<c>deviceNum</c>）の設定から読む。ソースが見つからない環境では、この検査を省略する。
    /// - W3（K7-2）: FakerInput が未導入のとき、<c>-virtualkbm fakerinput</c> の明示指定でも SendInput にする。
    /// </summary>
    public class Phase7bW2W3SmallFixesGuardTests
    {
        private readonly ITestOutputHelper _output;

        public Phase7bW2W3SmallFixesGuardTests(ITestOutputHelper output)
        {
            _output = output;
        }

        private static string StripComments(string text)
        {
            text = Regex.Replace(text, @"/\*.*?\*/", string.Empty, RegexOptions.Singleline);
            return Regex.Replace(text, @"//[^\r\n]*", string.Empty);
        }

        [Fact]
        public void Mouse_ReadsTheGyroMouseStickHorizontalAxis_OfItsOwnDevice()
        {
            string path = SourceFileLocator.Find("DS4Windows/DS4Control/Mouse.cs");
            if (path == null)
            {
                _output.WriteLine("ソースが見つからないため検査を省略した。");
                return;
            }

            string code = StripComments(File.ReadAllText(path));
            Assert.Contains("GetGyroMouseStickHorizontalAxis(deviceNum)", code);
            Assert.DoesNotMatch(@"GetGyroMouseStickHorizontalAxis\(\s*0\s*\)", code);
        }

        [Fact]
        public void FakerInputRequestedButNotInstalled_UsesTheSendInputFallback()
        {
            bool original = EnvironmentService.FakerInputInstalledState;
            try
            {
                EnvironmentService.FakerInputInstalledState = false;

                Assert.Equal(SendInputHandler.IDENTIFIER, VirtualKBMFactory.DetermineHandler(FakerInputHandler.IDENTIFIER).GetIdentifier());
                // 既存の分岐（既定・SendInput の明示指定・未知の指定）は変わらない
                Assert.Equal(SendInputHandler.IDENTIFIER, VirtualKBMFactory.DetermineHandler(VirtualKBMFactory.DEFAULT_IDENTIFIER).GetIdentifier());
                Assert.Equal(SendInputHandler.IDENTIFIER, VirtualKBMFactory.DetermineHandler(SendInputHandler.IDENTIFIER).GetIdentifier());
                Assert.Equal(SendInputHandler.IDENTIFIER, VirtualKBMFactory.DetermineHandler("unknown-handler").GetIdentifier());
            }
            finally
            {
                EnvironmentService.FakerInputInstalledState = original;
            }
        }
    }
}
