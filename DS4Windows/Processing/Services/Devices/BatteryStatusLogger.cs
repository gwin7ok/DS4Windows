using System;
using System.Threading;
using DS4Windows.Core.Logging;
using DS4Windows.Input;
using DS4Windows.Processing.Abstractions;

namespace DS4Windows.Processing.Services.Devices
{
    /// <summary>
    /// <see cref="IBatteryStatusLogger"/> の実装（DI の Singleton）。電池の値は、機種ごとの取得の結果として各機器が持つ
    /// （<see cref="InputDeviceCore.Battery"/>）。ここは、その値を、全スロットまとめて同じ間隔・同じ形式で記録するだけで、取得の経路には触れない
    /// （画面の表示の更新頻度や、Bluetooth の Xbox の読み取りの間隔は、これとは独立）。
    /// </summary>
    public sealed class BatteryStatusLogger : IBatteryStatusLogger, IDisposable
    {
        /// <summary>記録の間隔の既定。電池は急には変わらないため。</summary>
        public static readonly TimeSpan DefaultInterval = TimeSpan.FromMinutes(5);

        private readonly IDeviceSlotState _slots;
        private readonly TimeSpan _interval;
        private readonly object _gate = new object();
        private ITimer? _timer;

        public BatteryStatusLogger(IDeviceSlotState slots)
            : this(slots, DefaultInterval)
        {
        }

        public BatteryStatusLogger(IDeviceSlotState slots, TimeSpan interval)
        {
            ArgumentNullException.ThrowIfNull(slots);
            _slots = slots;
            _interval = interval;
        }

        public void Attach()
        {
            lock (_gate)
            {
                if (_timer != null) return;
                _timer = TimeProvider.System.CreateTimer(_ => LogOnce(), null, _interval, _interval);
            }
        }

        /// <summary>接続中の全コントローラーの電池を、1 回記録する。</summary>
        public void LogOnce()
        {
            if (!AppLogger.IsDebugEnabled) return;

            InputDeviceCore[] controllers = _slots.Controllers;
            for (int i = 0; i < controllers.Length; i++)
            {
                InputDeviceCore device = controllers[i];
                if (device == null || device.IsRemoved) continue;

                try
                {
                    if (AppLogger.IsDebugEnabled) AppLogger.LogDebug(Format(i + 1, device.DeviceType.ToString(), device.ConnectionType.ToString(),
                        device.MacAddress, device.Battery, device.Charging));
                }
                catch (Exception ex)
                {
                    // 記録できなくても、ほかの機器の記録と、本来の処理を止めない
                    if (AppLogger.IsDebugEnabled) AppLogger.LogDebug($"[Battery] slot={i + 1}: failed: {ex.Message}");
                }
            }
        }

        /// <summary>1 台分のログの行。機種や取得方法によらず、同じ形式にする。</summary>
        public static string Format(int slot, string deviceType, string connection, string address, int percent, bool charging)
        {
            return $"[Battery] slot={slot} {deviceType} {connection} {address}: {percent}%{(charging ? " (charging)" : string.Empty)}";
        }

        public void Dispose()
        {
            lock (_gate)
            {
                _timer?.Dispose();
                _timer = null;
            }
        }
    }
}
