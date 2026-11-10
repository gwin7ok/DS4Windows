using System;
using DS4Windows.Core.Model;
using DS4Windows.Input.Abstractions;
using DS4Windows.Input.Parsers;
using Xunit;

namespace DS4WindowsTests
{
    /// <summary>
    /// Joy-Con（左・右）の入力レポートの解析（<see cref="JoyConLeftReportParser"/>、<see cref="JoyConRightReportParser"/>）が、
    /// 読み取りループに直接書かれていた頃の解析と一致することの差分テスト。基準（<c>Legacy*</c>）は、切り出す前のループの該当部分の写し。
    /// 固定シードの乱数と境界値（全 0、全 0xFF）で比べる。
    /// </summary>
    public class JoyConReportParserTests
    {
        private static void LegacyLeft(byte[] inputReport, DS4State cState)
        {
            byte tempByte;
            tempByte = inputReport[4];
            cState.Share = (tempByte & 0x01) != 0;
            cState.L3 = (tempByte & 0x08) != 0;

            // Capture Button
            //cState.PS = (tempByte & 0x20) != 0;
            cState.Capture = (tempByte & 0x20) != 0;

            tempByte = inputReport[5];
            cState.DpadUp = (tempByte & 0x02) != 0;
            cState.DpadDown = (tempByte & 0x01) != 0;
            cState.DpadLeft = (tempByte & 0x08) != 0;
            cState.DpadRight = (tempByte & 0x04) != 0;
            cState.L1 = (tempByte & 0x40) != 0;
            cState.L2Btn = (tempByte & 0x80) != 0;

            cState.L2 = (byte)(cState.L2Btn ? 255 : 0);
            cState.L2Raw = cState.L2;

            cState.SideL = (tempByte & 0x20) != 0;
            cState.SideR = (tempByte & 0x10) != 0;
        }

        private static void LegacyRight(byte[] inputReport, DS4State cState)
        {
            byte tempByte;
            tempByte = inputReport[3];
            cState.Circle = (tempByte & 0x08) != 0;
            cState.Cross = (tempByte & 0x04) != 0;
            cState.Triangle = (tempByte & 0x02) != 0;
            cState.Square = (tempByte & 0x01) != 0;
            cState.R1 = (tempByte & 0x40) != 0;

            cState.R2Btn = (tempByte & 0x80) != 0;
            cState.R2 = (byte)(cState.R2Btn ? 255 : 0);
            cState.R2Raw = cState.R2;

            cState.SideL = (tempByte & 0x20) != 0;
            cState.SideR = (tempByte & 0x10) != 0;

            tempByte = inputReport[4];
            cState.Options = (tempByte & 0x02) != 0;
            cState.R3 = (tempByte & 0x04) != 0;
            cState.PS = (tempByte & 0x10) != 0;
        }

        private static void AssertSame(Action<byte[], DS4State> legacy, IInputReportParser parser, byte[] report)
        {
            var expected = new DS4State();
            var actual = new DS4State();
            legacy(report, expected);
            parser.Parse(report, actual);
            Assert.Equal(DS4StateSnapshot.Of(expected), DS4StateSnapshot.Of(actual));
        }

        [Fact]
        public void RandomReports_ProduceTheSameStateAsTheLegacyParsing()
        {
            var rng = new Random(20261110);
            for (int i = 0; i < 20000; i++)
            {
                var report = new byte[64];
                rng.NextBytes(report);
                AssertSame(LegacyLeft, new JoyConLeftReportParser(), report);
                AssertSame(LegacyRight, new JoyConRightReportParser(), report);
            }
        }

        [Theory]
        [InlineData(0x00)]
        [InlineData(0xFF)]
        public void FilledReports_ProduceTheSameStateAsTheLegacyParsing(byte fill)
        {
            var report = new byte[64];
            Array.Fill(report, fill);
            AssertSame(LegacyLeft, new JoyConLeftReportParser(), report);
            AssertSame(LegacyRight, new JoyConRightReportParser(), report);
        }

        [Fact]
        public void TheLeftSide_WritesOnlyTheLeftControls_AndTheRightSideOnlyTheRightOnes()
        {
            var report = new byte[64];
            Array.Fill(report, (byte)0xFF);
            var left = new DS4State();
            var right = new DS4State();

            new JoyConLeftReportParser().Parse(report, left);
            new JoyConRightReportParser().Parse(report, right);

            Assert.True(left.L1);
            Assert.False(left.R1);
            Assert.True(right.R1);
            Assert.False(right.L1);
        }

        [Fact]
        public void Parsing_DoesNotAllocate_BecauseItIsOnTheEveryFramePath()
        {
            var parser = new JoyConLeftReportParser();
            var report = new byte[64];
            new Random(1).NextBytes(report);
            var state = new DS4State();
            parser.Parse(report, state);

            long before = GC.GetAllocatedBytesForCurrentThread();
            for (int i = 0; i < 1000; i++) parser.Parse(report, state);

            Assert.Equal(0, GC.GetAllocatedBytesForCurrentThread() - before);
        }
    }
}
