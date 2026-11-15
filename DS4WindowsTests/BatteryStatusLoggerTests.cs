using System;
using DS4Windows.Processing.Services.Devices;
using Xunit;

namespace DS4WindowsTests
{
    /// <summary>
    /// 全機器の電池の記録。取得の経路（DS4 のレポート、XInput、GATT）によらず、同じ形式の行にする。
    /// </summary>
    public class BatteryStatusLoggerTests
    {
        [Fact]
        public void TheLine_HasTheSameFormat_ForEveryDevice()
        {
            Assert.Equal("[Battery] slot=1 DS4 BT DC:AF:68:EA:2C:C9: 50%",
                BatteryStatusLogger.Format(1, "DS4", "BT", "DC:AF:68:EA:2C:C9", 50, false));
            Assert.Equal("[Battery] slot=3 Xbox SONYWA 7B:D5:A7:76:72:8B: 99% (charging)",
                BatteryStatusLogger.Format(3, "Xbox", "SONYWA", "7B:D5:A7:76:72:8B", 99, true));
        }

        [Fact]
        public void TheDefaultInterval_IsFiveMinutes()
        {
            Assert.Equal(TimeSpan.FromMinutes(5), BatteryStatusLogger.DefaultInterval);
        }

        [Fact]
        public void LogOnce_WithoutDevices_DoesNotThrow()
        {
            using var logger = new BatteryStatusLogger(new DeviceSlotState());

            logger.LogOnce();
        }

        [Fact]
        public void Attach_CanBeCalledTwice_AndDisposeStopsIt()
        {
            using var logger = new BatteryStatusLogger(new DeviceSlotState(), TimeSpan.FromHours(1));

            logger.Attach();
            logger.Attach();
            logger.Dispose();
            logger.Dispose();
        }
    }
}
