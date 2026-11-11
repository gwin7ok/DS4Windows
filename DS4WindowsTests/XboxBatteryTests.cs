using DS4Windows.Input.Abstractions;
using DS4Windows.Input.Hid;
using DS4Windows.Input.Xbox;
using Xunit;

namespace DS4WindowsTests
{
    /// <summary>
    /// Xbox（XInput）の電池。通信路が <c>XInputGetBatteryInformation</c> の結果を合成レポートの末尾へ入れ、
    /// <see cref="XboxProtocol"/> が % にする（電池式は 4 段階を 0／33／66／100%、有線・不明・読めないときは値なし＝99% 固定）。
    /// </summary>
    public class XboxBatteryTests
    {
        private const string Path = @"\\?\usb#vid_045e&pid_028e#battery1#{ec87f1e3-c13b-4100-b5f7-8b84d54260cb}";

        private static byte[] Report(byte type, byte level)
        {
            var report = new byte[XInputReportLayout.ReportLength];
            report[XInputReportLayout.BatteryTypeOffset] = type;
            report[XInputReportLayout.BatteryLevelOffset] = level;
            return report;
        }

        [Theory]
        [InlineData(XInputBattery.TypeAlkaline, 0, 0)]
        [InlineData(XInputBattery.TypeAlkaline, 1, 33)]
        [InlineData(XInputBattery.TypeNiMh, 2, 66)]
        [InlineData(XInputBattery.TypeNiMh, 3, 100)]
        public void TheProtocol_ConvertsTheFourLevelsToPercent_ForBatteryPoweredPads(byte type, byte level, int expected)
        {
            Assert.True(new XboxProtocol().TryReadBattery(Report(type, level), out int percent, out bool charging));
            Assert.Equal(expected, percent);
            Assert.False(charging);
        }

        [Theory]
        [InlineData(XInputBattery.TypeWired)]
        [InlineData(XInputBattery.TypeUnknown)]
        [InlineData((byte)0)]
        public void TheProtocol_ReportsNoValue_ForWiredUnknownOrDisconnected(byte type)
        {
            Assert.False(new XboxProtocol().TryReadBattery(Report(type, 3), out _, out _));
        }

        [Fact]
        public void TheProtocol_ReportsNoValue_ForAShortReport()
        {
            Assert.False(new XboxProtocol().TryReadBattery(new byte[XInputReportLayout.ReportLength - 1], out _, out _));
        }

        [Fact]
        public void TheTransport_PutsTheBatteryAtTheEndOfTheReport_AndMarksItUnknownWhenItCannotBeRead()
        {
            var api = new FakeXInputApi();
            var interfaces = new FakeXusbInterfaceSource();
            interfaces.Add(Path, 0x045E, 0x028E, 0);
            api.Set(0, new XInputSnapshot(1, 0, 0, 0, 0, 0, 0, 0));
            api.Batteries[0] = new XInputBattery(XInputBattery.TypeNiMh, 2);
            var transport = new XInputTransport(api, interfaces, new XusbInterfaceInfo(Path, "USB\\X", 0x045E, 0x028E, 0));
            var buffer = new byte[XInputReportLayout.ReportLength];

            Assert.Equal(HidReadStatus.Success, transport.ReadFile(buffer));
            Assert.Equal(XInputBattery.TypeNiMh, buffer[XInputReportLayout.BatteryTypeOffset]);
            Assert.Equal(2, buffer[XInputReportLayout.BatteryLevelOffset]);

            // 読めない機器は、種類が不明になる（残量は 99% 固定へ）
            api.Batteries.Clear();
            var unreadable = new XInputTransport(api, interfaces, new XusbInterfaceInfo(Path, "USB\\X", 0x045E, 0x028E, 0));
            Assert.Equal(HidReadStatus.Success, unreadable.ReadFile(buffer));
            Assert.Equal(XInputBattery.TypeUnknown, buffer[XInputReportLayout.BatteryTypeOffset]);
        }
    }
}
