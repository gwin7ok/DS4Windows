using DS4Windows.Core.Model;
using DS4Windows.Input;
using DS4Windows.Input.Reports;
using Xunit;

namespace DS4WindowsTests
{
    /// <summary>DS4 の出力レポートの組み立て（<c>DualShock4OutputReportBuilder</c>）。バイトの位置が、機器の仕様どおりに固定されていること。</summary>
    public class DualShock4OutputReportBuilderTests
    {
        private static DS4HapticState Hap()
        {
            var h = new DS4HapticState();
            h.rumbleState.RumbleMotorStrengthRightLightFast = 1;
            h.rumbleState.RumbleMotorStrengthLeftHeavySlow = 2;
            h.lightbarState.LightBarColor = new DS4Color(3, 4, 5);
            h.lightbarState.LightBarFlashDurationOn = 6;
            h.lightbarState.LightBarFlashDurationOff = 7;
            return h;
        }

        [Fact]
        public void Usb_PutsEachValueAtItsFixedOffset()
        {
            var buf = new byte[32];
            var hap = Hap();
            DualShock4OutputReportBuilder.FillUsb(buf, 0xF7, in hap);

            Assert.Equal(new byte[] { 0x05, 0xF7, 0x04, 0x00, 1, 2, 3, 4, 5, 6, 7 }, buf[..11]);
        }

        [Fact]
        public void Bluetooth_PutsEachValueAtItsFixedOffset()
        {
            var buf = new byte[78];
            var hap = Hap();
            DualShock4OutputReportBuilder.FillBluetooth(buf, 0x11, 4, 0xF7, in hap);

            Assert.Equal(new byte[] { 0x11, 0xC4, 0x00, 0xF7, 0x04, 0x00, 1, 2, 3, 4, 5, 6, 7 }, buf[..13]);
        }
    }
}
