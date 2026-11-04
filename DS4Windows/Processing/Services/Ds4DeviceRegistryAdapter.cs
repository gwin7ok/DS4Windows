using System;
using System.Collections.Generic;
using System.Linq;
using DS4Windows;
using DS4Windows.DI;

namespace DS4Windows.Services
{
    public class Ds4DeviceRegistryAdapter : IDs4DeviceRegistry
    {
        // Phase7-Step10d W3（Q4＝A）: 第 1 層（DS4Devices）が読む設定値を、列挙の前に渡す（旧: 第 1 層が Global を直接参照していた）
        private readonly IDeviceLayerSettings _layerSettings;

        /// <summary>
        /// ホストを構築しない昇格した補助プロセス（デバイスの再有効化のみ）用。設定値は使わない（列挙はしない）。
        /// </summary>
        public Ds4DeviceRegistryAdapter()
        {
        }

        public Ds4DeviceRegistryAdapter(IDeviceLayerSettings layerSettings)
        {
            _layerSettings = layerSettings ?? throw new ArgumentNullException(nameof(layerSettings));
            DS4Devices.LayerSettings = _layerSettings;
        }

        public IEnumerable<DS4Device> FindControllers()
        {
            if (_layerSettings != null) DS4Devices.LayerSettings = _layerSettings;
            DS4Devices.findControllers();
            return DS4Devices.getDS4Controllers();
        }

        public IEnumerable<DS4Device> GetDS4Controllers() => DS4Devices.getDS4Controllers();

        public void StopControllers()
        {
            DS4Devices.stopControllers();
            if (AppLogger.IsTraceEnabled)
                AppLogger.LogTrace("[DI] Ds4DeviceRegistryAdapter.StopControllers called");
        }

        public bool RemoveDevice(DS4Device device)
        {
            if (device == null) return false;
            DS4Devices.RemoveDevice(device);
            if (AppLogger.IsTraceEnabled)
                AppLogger.LogTrace("[DI] Ds4DeviceRegistryAdapter.RemoveDevice: Device removed");
            return true;
        }

        public void OnRemoval(object sender, EventArgs e)
        {
            DS4Devices.On_Removal(sender, e);
        }

        public void UpdateSerial(object sender, EventArgs e)
        {
            DS4Devices.UpdateSerial(sender, e);
        }

        public void ReEnableDevice(string deviceInstanceId)
        {
            if (string.IsNullOrEmpty(deviceInstanceId)) return;
            DS4Devices.reEnableDevice(deviceInstanceId);
        }

        public bool IsExclusiveMode
        {
            get => DS4Devices.isExclusiveMode;
            set => DS4Devices.isExclusiveMode = value;
        }

        public event RequestElevationDelegate RequestElevation
        {
            add => DS4Devices.RequestElevation += value;
            remove => DS4Devices.RequestElevation -= value;
        }

        public PrepareInitDelegate PrepareDS4Init
        {
            get => DS4Devices.PrepareDS4Init;
            set => DS4Devices.PrepareDS4Init = value;
        }

        public PrepareInitDelegate PostDS4Init
        {
            get => DS4Devices.PostDS4Init;
            set => DS4Devices.PostDS4Init = value;
        }

        public CheckPendingDevice PreparePendingDevice
        {
            get => DS4Devices.PreparePendingDevice;
            set => DS4Devices.PreparePendingDevice = value;
        }
    }
}
