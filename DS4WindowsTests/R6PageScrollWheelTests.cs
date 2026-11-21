using System.Text.RegularExpressions;
using Xunit;
using DS4Windows.UI;

namespace DS4WindowsTests
{
    /// <summary>
    /// R-6（Release-Backlog.md）: マウスホイールを、Windows の「1 画面ずつ」の設定に合わせる。
    /// ノッチ数の計算（端数の持ち越し）と、起動時の登録の位置を守る。実際のスクロールは、実機（画面）で確認する。
    /// </summary>
    public class R6PageScrollWheelTests
    {
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

    }
}
