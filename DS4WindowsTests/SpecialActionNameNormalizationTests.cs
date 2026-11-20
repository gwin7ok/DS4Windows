using Xunit;
using System;
using DS4Windows.Core.Utilities;

namespace DS4WindowsTests
{
        public class SpecialActionNameNormalizationTests
    {
        [Fact]
        public void NormalizeActionName_TrimsWhitespace()
        {
            string input = "  My Action Name  ";
            string expected = "My Action Name";
            string actual = Util.NormalizeActionName(input);
            Assert.Equal(expected, actual);
        }

        [Fact]
        public void NormalizeActionName_NullReturnsEmpty()
        {
            string input = null;
            string expected = string.Empty;
            string actual = Util.NormalizeActionName(input);
            Assert.Equal(expected, actual);
        }

        [Fact]
        public void Util_NormalizeActionName_FixedExpectations()
        {
            // 重複実装だった Global.NormalizeActionName を削除したため、Util 側の期待値を固定する。
            Assert.Equal("Action", Util.NormalizeActionName("  Action\t\n"));
            Assert.Equal(string.Empty, Util.NormalizeActionName(null));
            Assert.Equal("My Action Name", Util.NormalizeActionName("  My Action Name  "));
            Assert.Equal(string.Empty, Util.NormalizeActionName("   "));
        }
    }
}
