using System.Diagnostics;
using DS4Windows.Core.Settings;
using DS4Windows.Input;
using DS4Windows.Input.Reports;
using Xunit;

namespace DS4WindowsTests
{
    /// <summary>
    /// DualSense の LED の点灯の値（<c>DualSenseLedMasks</c>）と、振動の送り直しの判定（<c>HapticStandby</c>）。
    /// 出力レポートの組み立ての前後で、値が変わらないことの固定。
    /// </summary>
    public class DualSenseLedMasksTests
    {
        [Theory]
        [InlineData(0, 0x04)]
        [InlineData(1, 0x0A)]
        [InlineData(2, 0x15)]
        [InlineData(3, 0x1B)]
        [InlineData(4, 0x11)]
        [InlineData(5, 0x0E)]
        [InlineData(6, 0x1F)]
        [InlineData(7, 0x00)]
        [InlineData(-1, 0x00)]
        public void FromSlot_MapsToSymmetricFiveLedPattern(int slot, int expected)
        {
            Assert.Equal((byte)expected, DualSenseLedMasks.FromSlot(slot));
        }

        [Theory]
        [InlineData(100, 0x1B)]
        [InlineData(95, 0x1B)]
        [InlineData(94, 0x0B)]
        [InlineData(70, 0x0B)]
        [InlineData(50, 0x03)]
        [InlineData(20, 0x01)]
        [InlineData(5, 0x07)]
        [InlineData(4, 0x00)]
        public void BatteryBar_UsesTheDocumentedThresholds(int percent, int expected)
        {
            Assert.Equal((byte)expected, DualSenseLedMasks.BatteryBar(percent));
        }

        [Fact]
        public void MuteLed_MapsModes()
        {
            Assert.Equal(0x00, DualSenseLedMasks.MuteLed(DualSenseControllerOptions.MuteLEDMode.Off));
            Assert.Equal(0x01, DualSenseLedMasks.MuteLed(DualSenseControllerOptions.MuteLEDMode.On));
            Assert.Equal(0x02, DualSenseLedMasks.MuteLed(DualSenseControllerOptions.MuteLEDMode.Pulse));
        }

        [Fact]
        public void Standby_SendsOnChange_AndManagesTheTimer()
        {
            var sw = new Stopwatch();
            Assert.True(HapticStandby.ShouldSend(sw, change: true, rumbleSet: true));
            Assert.True(sw.IsRunning);

            Assert.True(HapticStandby.ShouldSend(sw, change: true, rumbleSet: false));
            Assert.False(sw.IsRunning);                       // 振動が止まったら、時計を止める
            Assert.False(HapticStandby.ShouldSend(sw, change: false, rumbleSet: true));   // 時計が 4 秒に満たない
        }
    }
}
