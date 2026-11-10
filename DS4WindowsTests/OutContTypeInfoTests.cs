using DS4Windows.Core.Model;
using Xunit;

namespace DS4WindowsTests
{
    /// <summary>
    /// 出力の種類と、保存するキー・画面の選択肢の番号の対応表（<see cref="OutContTypeInfo"/>）が、種類ごとの if／switch を置き換える前の挙動と同じであること。
    /// </summary>
    public class OutContTypeInfoTests
    {
        [Theory]
        [InlineData(OutContType.None, "X360")]   // None は X360 として保存する
        [InlineData(OutContType.X360, "X360")]
        [InlineData(OutContType.DS4, "DS4")]
        public void SaveKey_MatchesTheFormerSwitch(OutContType type, string expected)
        {
            Assert.Equal(expected, OutContTypeInfo.SaveKey(type));
        }

        [Theory]
        [InlineData("None", OutContType.X360)]
        [InlineData("X360", OutContType.X360)]
        [InlineData("DS4", OutContType.DS4)]
        [InlineData("Unknown", OutContType.X360)]
        [InlineData(null, OutContType.X360)]
        public void FromSaveKey_MatchesTheFormerSwitch(string key, OutContType expected)
        {
            Assert.Equal(expected, OutContTypeInfo.FromSaveKey(key));
        }

        [Theory]
        [InlineData(-1, OutContType.X360)]
        [InlineData(0, OutContType.X360)]
        [InlineData(1, OutContType.DS4)]
        [InlineData(2, OutContType.X360)]
        public void ProfileChoice_IsDs4OnlyForIndexOne(int index, OutContType expected)
        {
            Assert.Equal(expected, OutContTypeInfo.FromProfileChoiceIndex(index));
        }

        [Theory]
        [InlineData(OutContType.None, 0)]
        [InlineData(OutContType.X360, 0)]
        [InlineData(OutContType.DS4, 1)]
        public void ProfileChoiceIndex_MatchesTheFormerTernary(OutContType type, int expected)
        {
            Assert.Equal(expected, OutContTypeInfo.ProfileChoiceIndex(type));
        }

        [Theory]
        [InlineData(-1, false, OutContType.None)]
        [InlineData(0, true, OutContType.X360)]
        [InlineData(1, true, OutContType.DS4)]
        [InlineData(2, false, OutContType.None)]
        public void TryFromProfileChoiceIndex_LeavesTheCallerUnchangedForOtherIndexes(int index, bool ok, OutContType expected)
        {
            Assert.Equal(ok, OutContTypeInfo.TryFromProfileChoiceIndex(index, out OutContType type));
            Assert.Equal(expected, type);
        }

        [Theory]
        [InlineData(OutContType.None, 0)]
        [InlineData(OutContType.X360, 1)]
        [InlineData(OutContType.DS4, 2)]
        public void SlotChoice_RoundTrips(OutContType type, int index)
        {
            Assert.Equal(index, OutContTypeInfo.SlotChoiceIndex(type));
            Assert.Equal(type, OutContTypeInfo.FromSlotChoiceIndex(index));
        }

        [Theory]
        [InlineData(-1)]
        [InlineData(3)]
        public void SlotChoice_OutOfRange_IsNone(int index)
        {
            Assert.Equal(OutContType.None, OutContTypeInfo.FromSlotChoiceIndex(index));
        }
    }
}
