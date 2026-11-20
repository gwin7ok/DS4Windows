using System.IO;
using System.Text.RegularExpressions;
using Xunit;

namespace DS4WindowsTests
{
    /// <summary>
    /// タップ判定の位置ずれ許容が 100（上流の 10 から拡大。2026-10-15 承認）であること。
    /// </summary>
    public class MouseTapToleranceTests
    {
        [Fact]
        public void TapTolerance_Is100()
        {
            Assert.Equal(100, ConstantValueReader.Get<int>(typeof(DS4Windows.Processing.Touchpad.TapClickDetector), "TapMaxMoveHw"));
        }
    }
}
