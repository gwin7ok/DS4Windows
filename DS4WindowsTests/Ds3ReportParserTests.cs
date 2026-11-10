using System;
using DS4Windows.Core.Model;
using DS4Windows.Input.Parsers;
using Xunit;

namespace DS4WindowsTests
{
    /// <summary>
    /// DS3（特徴レポート） の入力レポートの解析（<see cref="Ds3ReportParser"/>）が、読み取りループに直接書かれていた頃の解析と一致することの差分テスト。
    /// 基準（<see cref="LegacyParse"/>）は、切り出す前のループの該当部分の写し。固定シードの乱数と境界値（全 0、全 0xFF）で比べる。
    /// </summary>
    public class Ds3ReportParserTests
    {
        private static void LegacyParse(byte[] inputReport, DS4State cState)
        {
            const int reportOffset = 1;
            cState.Share = (inputReport[2 + reportOffset] & 0x01) > 0;
            cState.L3 = (inputReport[2 + reportOffset] & 0x02) > 0;
            cState.R3 = (inputReport[2 + reportOffset] & 0x04) > 0;
            cState.Options = (inputReport[2 + reportOffset] & 0x08) > 0;
            cState.DpadUp = ((inputReport[2 + reportOffset] & 0x10) > 0) && inputReport[14 + reportOffset] > 0;
            cState.DpadRight = ((inputReport[2 + reportOffset] & 0x20) > 0) && inputReport[15 + reportOffset] > 0;
            cState.DpadDown = ((inputReport[2 + reportOffset] & 0x40) > 0) && inputReport[16 + reportOffset] > 0;
            cState.DpadLeft = ((inputReport[2 + reportOffset] & 0x80) > 0) && inputReport[17 + reportOffset] > 0;
            
            cState.L2 = ((inputReport[3 + reportOffset] & 0x01) > 0) ? inputReport[18 + reportOffset] : (byte)0x00;
            cState.R2 = ((inputReport[3 + reportOffset] & 0x02) > 0) ? inputReport[19 + reportOffset] : (byte)0x00;
            
            cState.L1 = ((inputReport[3 + reportOffset] & 0x04) > 0) && inputReport[20 + reportOffset] > 0;
            cState.R1 = ((inputReport[3 + reportOffset] & 0x08) > 0) && inputReport[21 + reportOffset] > 0;
            cState.Triangle = ((inputReport[3 + reportOffset] & 0x10) > 0) && inputReport[22 + reportOffset] > 0;
            cState.Circle = ((inputReport[3 + reportOffset] & 0x20) > 0) && inputReport[23 + reportOffset] > 0;
            cState.Cross = ((inputReport[3 + reportOffset] & 0x40) > 0) && inputReport[24 + reportOffset] > 0;
            cState.Square = ((inputReport[3 + reportOffset] & 0x80) > 0) && inputReport[25 + reportOffset] > 0;
            cState.PS = (inputReport[4 + reportOffset] & 0x01) > 0;
            
            cState.L2Btn = cState.L2 > 0;
            cState.R2Btn = cState.R2 > 0;
            
            cState.L2Raw = cState.L2;
            cState.R2Raw = cState.R2;
            
            cState.LX = inputReport[6 + reportOffset];
            cState.LY = inputReport[7 + reportOffset];
            cState.RX = inputReport[8 + reportOffset];
            cState.RY = inputReport[9 + reportOffset];
        }

        private static void AssertSame(byte[] report)
        {
            var expected = new DS4State();
            var actual = new DS4State();
            LegacyParse(report, expected);
            new Ds3ReportParser().Parse(report, actual);
            Assert.Equal(DS4StateSnapshot.Of(expected), DS4StateSnapshot.Of(actual));
        }

        [Fact]
        public void RandomReports_ProduceTheSameStateAsTheLegacyParsing()
        {
            var rng = new Random(20261110);
            for (int i = 0; i < 20000; i++)
            {
                var report = new byte[49];
                rng.NextBytes(report);
                AssertSame(report);
            }
        }

        [Theory]
        [InlineData(0x00)]
        [InlineData(0xFF)]
        public void FilledReports_ProduceTheSameStateAsTheLegacyParsing(byte fill)
        {
            var report = new byte[49];
            Array.Fill(report, fill);
            AssertSame(report);
        }

        [Fact]
        public void Parsing_DoesNotAllocate_BecauseItIsOnTheEveryFramePath()
        {
            var parser = new Ds3ReportParser();
            var report = new byte[49];
            new Random(1).NextBytes(report);
            var state = new DS4State();
            parser.Parse(report, state);

            long before = GC.GetAllocatedBytesForCurrentThread();
            for (int i = 0; i < 1000; i++) parser.Parse(report, state);

            Assert.Equal(0, GC.GetAllocatedBytesForCurrentThread() - before);
        }

        [Fact]
        public void ADpadBitWithoutPressure_IsNotTreatedAsPressed()
        {
            var report = new byte[49];
            report[2 + 1] = 0x10;   // 上のビット
            var state = new DS4State();
            new Ds3ReportParser().Parse(report, state);
            Assert.False(state.DpadUp);

            report[14 + 1] = 5;     // 圧力あり
            new Ds3ReportParser().Parse(report, state);
            Assert.True(state.DpadUp);
        }
    }
}
