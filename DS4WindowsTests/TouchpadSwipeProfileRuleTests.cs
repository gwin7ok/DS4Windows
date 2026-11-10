using DS4Windows.UI.Services;
using Xunit;

namespace DS4WindowsTests
{
    /// <summary>
    /// タッチパッドのスワイプによるプロファイル切替の規則（<see cref="TouchpadSwipeProfileRule"/>）。
    /// 元は <c>MainWindow</c> のタイマー処理にあった分岐で、先頭・末尾での回り込みと通知の要否を固定する。
    /// </summary>
    public class TouchpadSwipeProfileRuleTests
    {
        private readonly TouchpadSwipeProfileRule rule = new TouchpadSwipeProfileRule();

        [Theory]
        [InlineData(3, 5, "left", 2)]
        [InlineData(0, 5, "left", 4)]
        [InlineData(-1, 5, "left", 4)]
        [InlineData(3, 5, "right", 4)]
        [InlineData(4, 5, "right", 0)]
        [InlineData(2, 5, "", 2)]
        [InlineData(2, 5, "t", 2)]
        [InlineData(2, 5, null, 2)]
        [InlineData(0, 0, "left", 0)]
        [InlineData(0, 0, "right", 0)]
        public void NextIndex_WrapsAroundAndIgnoresNonSwipes(int selected, int count, string slide, int expected)
        {
            Assert.Equal(expected, rule.NextIndex(selected, count, slide));
        }

        [Theory]
        [InlineData("t", true)]
        [InlineData("leftt", true)]
        // "left" / "right" も文字 t を含むため、スワイプでも通知が出る（元の挙動のまま）
        [InlineData("left", true)]
        [InlineData("right", true)]
        [InlineData("up", false)]
        [InlineData("", false)]
        [InlineData(null, false)]
        public void WantsNotification_WhenSlideContainsT(string slide, bool expected)
        {
            Assert.Equal(expected, rule.WantsNotification(slide));
        }
    }
}
