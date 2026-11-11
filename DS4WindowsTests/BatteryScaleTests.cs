using DS4Windows.Input;
using Xunit;

namespace DS4WindowsTests
{
    /// <summary>
    /// 電池の段階を % にする共通の計算 <see cref="BatteryScale"/>。DS4・DualSense・Switch Pro・Xbox が、同じ式（<c>value * 100 / max</c>、100 を超えない）を使うこと。
    /// 置き換え前の各機種の式と、同じ値になる。
    /// </summary>
    public class BatteryScaleTests
    {
        [Theory]
        [InlineData(0, 8, 0)]
        [InlineData(4, 8, 50)]
        [InlineData(8, 8, 100)]
        [InlineData(1, 3, 33)]      // Xbox の 4 段階（低）
        [InlineData(2, 3, 66)]
        [InlineData(3, 3, 100)]
        [InlineData(11, 10, 100)]   // 最大を超える値（DS4 の USB の満充電の表記）は 100 で止める
        [InlineData(5, 0, 0)]       // 最大が 0 の誤りでも、割り算で落ちない
        public void ToPercent_FollowsTheSharedFormula(int value, int max, int expected)
        {
            Assert.Equal(expected, BatteryScale.ToPercent(value, max));
        }

        [Fact]
        public void ToPercent_MatchesTheFormerPerDeviceFormula_ForEveryDs4Value()
        {
            for (int max = 1; max <= 11; max++)
            {
                for (int value = 0; value <= 15; value++)
                {
                    Assert.Equal(System.Math.Min(value * 100 / max, 100), BatteryScale.ToPercent(value, max));
                }
            }
        }
    }
}
