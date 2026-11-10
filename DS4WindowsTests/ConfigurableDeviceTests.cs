using DS4Windows.Core.Abstractions;
using DS4Windows.Core.Model;
using DS4Windows.Core.Settings;
using DS4Windows.Input;
using DS4Windows.Processing.Abstractions;
using DS4Windows.Processing.Services;
using Xunit;

namespace DS4WindowsTests
{
    /// <summary>
    /// Core が接続中のデバイスを扱う狭い契約（<see cref="IConfigurableDevice"/>、<see cref="IConfigurableDeviceLookup"/>）の確認。
    /// DS4Device が実装すること、スロット状態が同じインスタンスで引けること、未接続・範囲外は null であること。
    /// </summary>
    public class ConfigurableDeviceTests
    {
        [Fact]
        public void DS4Device_ImplementsTheNarrowContract()
        {
            Assert.True(typeof(IConfigurableDevice).IsAssignableFrom(typeof(DS4Device)));
            Assert.True(typeof(IConfigurableDevice).IsAssignableFrom(typeof(DS4Windows.Input.Devices.DualSenseDevice)));
        }

        [Fact]
        public void TheSlotState_LooksUpTheSameDeviceThroughBothWindows()
        {
            var slots = new DeviceSlotState();
            var device = TestDs4Device.Create();
            slots.DS4Controllers[1] = device;

            IConfigurableDeviceLookup lookup = slots;
            Assert.Same(device, lookup.GetConfigurableDevice(1));
            Assert.Null(lookup.GetConfigurableDevice(0));
            Assert.Null(lookup.GetConfigurableDevice(-1));
            Assert.Null(lookup.GetConfigurableDevice(ControllerLimits.MaxControllerCount));
            Assert.Equal(InputDeviceType.DS4, lookup.GetConfigurableDevice(1).DeviceType);
        }

        [Fact]
        public void TheLookup_IsTheSameInstanceAsTheSlotState_InTheContainer()
        {
            Assert.Same(TestHost.GetService<IDeviceSlotState>(), TestHost.GetService<IConfigurableDeviceLookup>());
        }
    }
}
