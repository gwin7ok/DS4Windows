#nullable enable
using DS4Windows.Core.Model;
using DS4Windows.Input.Abstractions;
using DS4Windows.Input.Parsers;
using DS4Windows.Input.Xbox;
using Xunit;

namespace DS4WindowsTests
{
    /// <summary>
    /// XInput の値の範囲と電池の値を、Microsoft の文書（XINPUT_GAMEPAD、XINPUT_BATTERY_INFORMATION。2026-11-22 に取得）と
    /// <c>xinput.h</c> の定数と突き合わせる。スティックは符号付き 16 ビット（−32768〜32767、中立 0、Y は上が正）、トリガーは 0〜255。
    /// 文書は、デッドゾーンの数値（7849・8689・30）を載せていない。このアプリは XInput のデッドゾーンを使わず、利用者の設定で決める。
    /// </summary>
    public class XInputValueRangeSpecConformanceTests
    {
        private static ControllerState Parse(short lx, short ly, short rx, short ry, byte lt, byte rt)
        {
            var report = new byte[XInputReportLayout.ReportLength];
            report[XInputReportLayout.LeftTriggerOffset] = lt;
            report[XInputReportLayout.RightTriggerOffset] = rt;
            Put(report, XInputReportLayout.LeftStickXOffset, lx);
            Put(report, XInputReportLayout.LeftStickYOffset, ly);
            Put(report, XInputReportLayout.RightStickXOffset, rx);
            Put(report, XInputReportLayout.RightStickYOffset, ry);
            var s = new ControllerState();
            new XInputReportParser().Parse(report, s);
            return s;
        }

        private static void Put(byte[] report, int offset, short value)
        {
            report[offset] = (byte)value;
            report[offset + 1] = (byte)(value >> 8);
        }

        [Fact]
        public void TheSticks_MapTheFullSignedRange_To0To255_WithTheNeutralAt128()
        {
            Assert.Equal(128, Parse(0, 0, 0, 0, 0, 0).LX);
            Assert.Equal(0, Parse(short.MinValue, 0, 0, 0, 0, 0).LX);
            Assert.Equal(255, Parse(short.MaxValue, 0, 0, 0, 0, 0).LX);
            Assert.Equal(0, Parse(0, 0, short.MinValue, 0, 0, 0).RX);
            Assert.Equal(255, Parse(0, 0, short.MaxValue, 0, 0, 0).RX);
        }

        [Fact]
        public void TheYAxes_AreInverted_BecauseXInputIsPositiveUp()
        {
            Assert.Equal(128, Parse(0, 0, 0, 0, 0, 0).LY);
            Assert.Equal(0, Parse(0, short.MaxValue, 0, 0, 0, 0).LY);      // 上いっぱい
            Assert.Equal(255, Parse(0, short.MinValue, 0, 0, 0, 0).LY);    // 下いっぱい
            Assert.Equal(0, Parse(0, 0, 0, short.MaxValue, 0, 0).RY);
            Assert.Equal(255, Parse(0, 0, 0, short.MinValue, 0, 0).RY);
        }

        [Fact]
        public void TheTriggers_AreTheRawBytes_0To255()
        {
            ControllerState s = Parse(0, 0, 0, 0, 0, 255);

            Assert.Equal(0, s.L2);
            Assert.Equal(255, s.R2);
            Assert.Equal(200, Parse(0, 0, 0, 0, 200, 0).L2);
        }

        [Fact]
        public void TheBatteryTypes_AreTheXinputHeaderValues()
        {
            // xinput.h: BATTERY_TYPE_WIRED 0x01、ALKALINE 0x02、NIMH 0x03、UNKNOWN 0xFF（DISCONNECTED は 0x00）
            Assert.Equal(0x01, XInputBattery.TypeWired);
            Assert.Equal(0x02, XInputBattery.TypeAlkaline);
            Assert.Equal(0x03, XInputBattery.TypeNiMh);
            Assert.Equal(0xFF, XInputBattery.TypeUnknown);

            // BATTERY_LEVEL_EMPTY 0、LOW 1、MEDIUM 2、FULL 3
            Assert.Equal(3, XInputBattery.MaxLevel);
        }

        [Theory]
        [InlineData(XInputBattery.TypeAlkaline, 0, 0)]
        [InlineData(XInputBattery.TypeAlkaline, 1, 33)]
        [InlineData(XInputBattery.TypeNiMh, 2, 66)]
        [InlineData(XInputBattery.TypeNiMh, 3, 100)]
        public void TheBatteryLevel_OfABatteryPoweredPad_IsThe4StepsAs0To100Percent(byte type, byte level, int percent)
        {
            var report = new byte[XInputReportLayout.ReportLength];
            report[XInputReportLayout.BatteryTypeOffset] = type;
            report[XInputReportLayout.BatteryLevelOffset] = level;

            Assert.True(new XboxProtocol().TryReadBattery(report, out int actual, out bool charging));
            Assert.Equal(percent, actual);
            Assert.False(charging);   // XInput は充電中かを教えない
        }

        [Theory]
        [InlineData(XInputBattery.TypeWired)]
        [InlineData(XInputBattery.TypeUnknown)]
        public void TheBatteryLevel_OfAWiredOrUnknownPad_IsNotRead(byte type)
        {
            // 文書: BatteryLevel は、電池の種類が分かる無線の機器でだけ有効
            var report = new byte[XInputReportLayout.ReportLength];
            report[XInputReportLayout.BatteryTypeOffset] = type;
            report[XInputReportLayout.BatteryLevelOffset] = 3;

            Assert.False(new XboxProtocol().TryReadBattery(report, out _, out _));
        }
    }
}
