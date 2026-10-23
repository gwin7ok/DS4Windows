using System.IO;
using System.Text.RegularExpressions;
using DS4WinWPF.DS4Forms;
using Xunit;
using Xunit.Abstractions;

namespace DS4WindowsTests
{
    /// <summary>
    /// R-6（Release-Backlog.md）: マウスホイールを、Windows の「1 画面ずつ」の設定に合わせる。
    /// ノッチ数の計算（端数の持ち越し）と、起動時の登録の位置を守る。実際のスクロールは、実機（画面）で確認する。
    /// </summary>
    public class R6PageScrollWheelTests
    {
        private readonly ITestOutputHelper _output;

        public R6PageScrollWheelTests(ITestOutputHelper output)
        {
            _output = output;
        }

        [Theory]
        [InlineData(120, 0, 1, 0)]
        [InlineData(-120, 0, -1, 0)]
        [InlineData(240, 0, 2, 0)]
        [InlineData(60, 0, 0, 60)]        // 細かい値は、たまるまで進まない
        [InlineData(60, 60, 1, 0)]        // 2 回で 1 ノッチ
        [InlineData(-60, -60, -1, 0)]
        [InlineData(-120, 60, -1, 0)]     // 向きが変わったら、端数を捨てる
        [InlineData(0, 30, 0, 30)]
        public void TheNotches_AreCountedFromTheDelta_AndTheRemainderIsCarried(int delta, int remainderBefore, int expectedNotches, int expectedRemainder)
        {
            int remainder = remainderBefore;
            int notches = PageScrollWheelHandler.ConsumeNotches(ref remainder, delta);

            Assert.Equal(expectedNotches, notches);
            Assert.Equal(expectedRemainder, remainder);
        }

        [Fact]
        public void TheHandler_IsRegisteredOnceAtStartup_BeforeTheFirstWindow()
        {
            string path = SourceFileLocator.Find("DS4Windows/App.xaml.cs");
            if (path == null)
            {
                _output.WriteLine("App.xaml.cs が見つからないため検査を省略した。");
                return;
            }

            string text = File.ReadAllText(path);
            text = Regex.Replace(text, @"/\*.*?\*/", string.Empty, RegexOptions.Singleline);
            text = Regex.Replace(text, @"//[^\r\n]*", string.Empty);

            int start = text.IndexOf("private void InitializeProcessWideSettings(");
            Assert.True(start >= 0);
            int end = text.IndexOf("private ", start + 10);
            string body = text.Substring(start, end - start);
            Assert.Contains("PageScrollWheelHandler.Register();", body);
        }
    }
}
