using System;
using DS4Windows.Core.Abstractions;
using DS4Windows.Input;
using DS4Windows.Processing.Abstractions;

namespace DS4Windows.Processing.Services
{
    /// <summary>
    /// Phase13-Step6 6-b: <see cref="IDeviceBatteryText"/> の実装（DI の Singleton。状態なし）。
    /// 旧 <c>ControlService.GetDS4Battery</c> の本体をそのまま移したもの（文言の組み立ては変えていない）。
    /// 旧: <c>DS4Controllers[index]</c> の直接参照（範囲外は例外）。今回: <see cref="IDeviceStateAccessor.GetController"/>（範囲外は null ＝ 「N/A」）。呼び出し側は有効なスロットしか渡さない。
    /// </summary>
    public sealed class DeviceBatteryText : IDeviceBatteryText
    {
        private readonly IDeviceStateAccessor _devices;

        public DeviceBatteryText(IDeviceStateAccessor devices)
        {
            _devices = devices ?? throw new ArgumentNullException(nameof(devices));
        }

        public string GetBatteryText(int index)
        {
            InputDeviceCore d = _devices.GetController(index);
            if (d != null)
            {
                string battery;
                if (!d.IsAlive())
                    battery = "...";

                if (d.isCharging())
                {
                    if (d.getBattery() >= 100)
                        battery = DS4Windows.Properties.Resources.Full;
                    else
                        battery = d.getBattery() + "%+";
                }
                else
                {
                    battery = d.getBattery() + "%";
                }

                return battery;
            }
            else
                return DS4Windows.Properties.Resources.NA;
        }
    }
}
