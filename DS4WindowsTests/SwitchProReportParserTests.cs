using System;
using DS4Windows.Core.Model;
using DS4Windows.Input.Parsers;
using Xunit;

namespace DS4WindowsTests
{
    /// <summary>
    /// Switch Pro コントローラー（ボタン・方向キー・ZL/ZR） の入力レポートの解析（<see cref="SwitchProReportParser"/>）が、読み取りループに直接書かれていた頃の解析と一致することの差分テスト。
    /// 基準（<see cref="LegacyParse"/>）は、切り出す前のループの該当部分の写し。固定シードの乱数と境界値（全 0、全 0xFF）で比べる。
    /// </summary>
    public class SwitchProReportParserTests
    {
        private static void LegacyParse(byte[] inputReport, ControllerState cState)
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
            
            tempByte = inputReport[4];
            cState.Share = (tempByte & 0x01) != 0;
            cState.Options = (tempByte & 0x02) != 0;
            cState.PS = (tempByte & 0x10) != 0;
            cState.Capture = (tempByte & 0x20) != 0;
            cState.L3 = (tempByte & 0x08) != 0;
            cState.R3 = (tempByte & 0x04) != 0;
            
            tempByte = inputReport[5];
            cState.DpadUp = (tempByte & 0x02) != 0;
            cState.DpadDown = (tempByte & 0x01) != 0;
            cState.DpadLeft = (tempByte & 0x08) != 0;
            cState.DpadRight = (tempByte & 0x04) != 0;
            cState.L1 = (tempByte & 0x40) != 0;
            cState.L2Btn = (tempByte & 0x80) != 0;
            cState.L2 = (byte)(cState.L2Btn ? 255 : 0);
            cState.L2Raw = cState.L2;
        }

        private static void AssertSame(byte[] report)
        {
            var expected = new ControllerState();
            var actual = new ControllerState();
            LegacyParse(report, expected);
            new SwitchProReportParser().Parse(report, actual);
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
                AssertSame(report);
            }
        }

        [Theory]
        [InlineData(0x00)]
        [InlineData(0xFF)]
        public void FilledReports_ProduceTheSameStateAsTheLegacyParsing(byte fill)
        {
            var report = new byte[64];
            Array.Fill(report, fill);
            AssertSame(report);
        }

        [Fact]
        public void Parsing_DoesNotAllocate_BecauseItIsOnTheEveryFramePath()
        {
            var parser = new SwitchProReportParser();
            var report = new byte[64];
            new Random(1).NextBytes(report);
            var state = new ControllerState();
            parser.Parse(report, state);

            long before = GC.GetAllocatedBytesForCurrentThread();
            for (int i = 0; i < 1000; i++) parser.Parse(report, state);

            Assert.Equal(0, GC.GetAllocatedBytesForCurrentThread() - before);
        }

        [Fact]
        public void TheTriggersAreTwoValued_255WhenPressedAndZeroOtherwise()
        {
            var report = new byte[64];
            report[3] = 0x80;   // ZR
            var state = new ControllerState();

            new SwitchProReportParser().Parse(report, state);

            Assert.True(state.R2Btn);
            Assert.Equal(255, state.R2);
            Assert.False(state.L2Btn);
            Assert.Equal(0, state.L2);
        }
    }
}
