using System;
using System.IO;
using System.Text.RegularExpressions;
using Xunit;
using Xunit.Abstractions;
using DS4Windows.Core.Settings;
using DS4Windows.Core.Utilities;

namespace DS4WindowsTests
{
    /// <summary>
    /// Phase7-Step9 W2（Q2＝A）: <c>Mapping</c> と状態の保持クラス 3 つが <c>Global</c> を参照しないことと、
    /// 新設した <c>ControllerLimits</c>／<c>NumericUtil</c> が旧 <c>Global</c> の値・計算と同一であることの回帰ガード。
    /// ソースが見つからない環境では、ソースの検査を行わず合格とする。
    /// </summary>
    public class NoGlobalInMappingGuardTests
    {
        private readonly ITestOutputHelper _output;

        public NoGlobalInMappingGuardTests(ITestOutputHelper output)
        {
            _output = output;
        }

        [Fact]
        public void ControllerLimits_HaveTheSameValuesAsBefore()
        {
            Assert.Equal(8, ControllerLimits.MaxControllerCount);
            Assert.Equal(9, ControllerLimits.TestProfileItemCount);
            Assert.Equal(8, ControllerLimits.TestProfileIndex);
            Assert.Equal(4, ControllerLimits.OldXInputControllerCount);
            Assert.Equal((byte)128, ControllerLimits.StickAxisMidpoint);
        }

        // Phase7-Step10a W4: Global の別名（定数 5 つ・Clamp）は削除した。再導入の禁止は DeadShimRemovalGuardTests が固定する。

        [Theory]
        [InlineData(0.0, -5.0, 10.0)]
        [InlineData(0.0, 15.0, 10.0)]
        [InlineData(0.0, 5.0, 10.0)]
        [InlineData(0.0, 0.0, 10.0)]
        [InlineData(0.0, 10.0, 10.0)]
        [InlineData(-3.5, -3.5, 2.5)]
        [InlineData(5.0, 7.0, 3.0)] // min > max: 旧実装と同じく、value < min の判定が先に効く
        public void Clamp_IsIdenticalToTheOldImplementation(double min, double value, double max)
        {
            double expected = (value < min) ? min : (value > max) ? max : value;
            Assert.Equal(expected, NumericUtil.Clamp(min, value, max));
        }

        [Fact]
        public void Clamp_NaN_IsReturnedAsBefore()
        {
            // 旧実装: NaN は両方の比較が false になり、そのまま返る
            Assert.True(double.IsNaN(NumericUtil.Clamp(0, double.NaN, 10)));
        }
    }
}
