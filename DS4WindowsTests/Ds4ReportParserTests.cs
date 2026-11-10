using System;
using DS4Windows.Core.Model;
using DS4Windows.Input.Abstractions;
using DS4Windows.Input.Parsers;
using Xunit;

namespace DS4WindowsTests
{
    /// <summary>
    /// DS4 の入力レポートの解析（<see cref="Ds4ReportParser"/>）が、読み取りループに直接書かれていた頃の解析と一致することの差分テスト。
    /// 基準（<see cref="LegacyParse"/>）は、切り出す前のループの該当部分の写し。固定シードの乱数のレポートと、境界値（全 0、全 0xFF、方向キーの 16 通り）で比べる。
    /// </summary>
    public class Ds4ReportParserTests
    {
        private static void LegacyParse(byte[] inputReport, DS4State cState)
        {
            byte tempByte;
            cState.LX = inputReport[1];
                        cState.LY = inputReport[2];
                        cState.RX = inputReport[3];
                        cState.RY = inputReport[4];
                        cState.L2 = inputReport[8];
                        cState.R2 = inputReport[9];
                        cState.L2Raw = cState.L2;
                        cState.R2Raw = cState.R2;
                        
                        tempByte = inputReport[5];
                        cState.Triangle = (tempByte & (1 << 7)) != 0;
                        cState.Circle = (tempByte & (1 << 6)) != 0;
                        cState.Cross = (tempByte & (1 << 5)) != 0;
                        cState.Square = (tempByte & (1 << 4)) != 0;
                        
                        // First 4 bits denote dpad state. Clock representation
                        // with 8 meaning centered and 0 meaning DpadUp.
                        byte dpad_state = (byte)(tempByte & 0x0F);
                        
                        switch (dpad_state)
                        {
                            case 0: cState.DpadUp = true; cState.DpadDown = false; cState.DpadLeft = false; cState.DpadRight = false; break;
                            case 1: cState.DpadUp = true; cState.DpadDown = false; cState.DpadLeft = false; cState.DpadRight = true; break;
                            case 2: cState.DpadUp = false; cState.DpadDown = false; cState.DpadLeft = false; cState.DpadRight = true; break;
                            case 3: cState.DpadUp = false; cState.DpadDown = true; cState.DpadLeft = false; cState.DpadRight = true; break;
                            case 4: cState.DpadUp = false; cState.DpadDown = true; cState.DpadLeft = false; cState.DpadRight = false; break;
                            case 5: cState.DpadUp = false; cState.DpadDown = true; cState.DpadLeft = true; cState.DpadRight = false; break;
                            case 6: cState.DpadUp = false; cState.DpadDown = false; cState.DpadLeft = true; cState.DpadRight = false; break;
                            case 7: cState.DpadUp = true; cState.DpadDown = false; cState.DpadLeft = true; cState.DpadRight = false; break;
                            case 8:
                            default: cState.DpadUp = false; cState.DpadDown = false; cState.DpadLeft = false; cState.DpadRight = false; break;
                        }
                        
                        tempByte = inputReport[6];
                        cState.R3 = (tempByte & (1 << 7)) != 0;
                        cState.L3 = (tempByte & (1 << 6)) != 0;
                        cState.Options = (tempByte & (1 << 5)) != 0;
                        cState.Share = (tempByte & (1 << 4)) != 0;
                        cState.R2Btn = (inputReport[6] & (1 << 3)) != 0;
                        cState.L2Btn = (inputReport[6] & (1 << 2)) != 0;
                        cState.R1 = (tempByte & (1 << 1)) != 0;
                        cState.L1 = (tempByte & (1 << 0)) != 0;
                        
                        tempByte = inputReport[7];
                        cState.PS = (tempByte & (1 << 0)) != 0;
                        cState.TouchButton = (tempByte & 0x02) != 0;
                        
                        cState.OutputTouchButton = cState.TouchButton;
                        cState.FrameCounter = (byte)(tempByte >> 2);
                        
                        //Simpler touch storing
                        cState.TrackPadTouch0.RawTrackingNum = inputReport[35];
                        cState.TrackPadTouch0.Id = (byte)(inputReport[35] & 0x7f);
                        cState.TrackPadTouch0.IsActive = (inputReport[35] & 0x80) == 0;
                        cState.TrackPadTouch0.X = (short)(((ushort)(inputReport[37] & 0x0f) << 8) | (ushort)(inputReport[36]));
                        cState.TrackPadTouch0.Y = (short)(((ushort)(inputReport[38]) << 4) | ((ushort)(inputReport[37] & 0xf0) >> 4));
                        
                        cState.TrackPadTouch1.RawTrackingNum = inputReport[39];
                        cState.TrackPadTouch1.Id = (byte)(inputReport[39] & 0x7f);
                        cState.TrackPadTouch1.IsActive = (inputReport[39] & 0x80) == 0;
                        cState.TrackPadTouch1.X = (short)(((ushort)(inputReport[41] & 0x0f) << 8) | (ushort)(inputReport[40]));
                        cState.TrackPadTouch1.Y = (short)(((ushort)(inputReport[42]) << 4) | ((ushort)(inputReport[41] & 0xf0) >> 4));
        }

        private static void AssertSame(byte[] report)
        {
            var expected = new DS4State();
            var actual = new DS4State();
            LegacyParse(report, expected);
            new Ds4ReportParser().Parse(report, actual);
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
        public void EveryDpadValue_ProducesTheSameStateAsTheLegacyParsing()
        {
            for (int dpad = 0; dpad < 16; dpad++)
            {
                var report = new byte[64];
                report[5] = (byte)(0xF0 | dpad);
                AssertSame(report);
            }
        }

        [Fact]
        public void Parsing_DoesNotAllocate_BecauseItIsOnTheEveryFramePath()
        {
            var parser = new Ds4ReportParser();
            var report = new byte[64];
            new Random(1).NextBytes(report);
            var state = new DS4State();
            parser.Parse(report, state);   // JIT を済ませる

            long before = GC.GetAllocatedBytesForCurrentThread();
            for (int i = 0; i < 1000; i++) parser.Parse(report, state);
            long after = GC.GetAllocatedBytesForCurrentThread();

            Assert.Equal(0, after - before);
        }

        [Fact]
        public void Parser_IsSharedAsTheContract_AndKnownValuesAreDecoded()
        {
            IInputReportParser parser = new Ds4ReportParser();
            var report = new byte[64];
            report[1] = 10; report[2] = 20; report[3] = 30; report[4] = 40;
            report[5] = 0x28;   // Cross（bit5）+ 方向キー 8（中立）
            report[8] = 200; report[9] = 100;
            var state = new DS4State();

            parser.Parse(report, state);

            Assert.Equal(10, state.LX);
            Assert.Equal(40, state.RY);
            Assert.True(state.Cross);
            Assert.False(state.DpadUp);
            Assert.Equal(200, state.L2);
            Assert.Equal(200, state.L2Raw);
        }
    }
}
