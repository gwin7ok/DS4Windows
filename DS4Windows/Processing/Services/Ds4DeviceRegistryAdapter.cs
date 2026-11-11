using System;
using System.Collections.Generic;
using System.Linq;
using DS4Windows.Core.Logging;
using DS4Windows.Input.Abstractions;
using DS4Windows.Input;
using DS4Windows.Processing.Abstractions;

namespace DS4Windows.Processing.Services
{
    public class InputDeviceRegistryAdapter : IInputDeviceRegistry
    {
        private readonly InputDeviceRegistry _devices;

        /// <summary>
        /// ホストを構築しない昇格した補助プロセス（デバイスの再有効化のみ）用。設定値は使わない（列挙はしない）。
        /// </summary>
        public InputDeviceRegistryAdapter()
            : this(new InputDeviceRegistry(null, System.Array.Empty<IInputDeviceCartridge>()))
        {
        }

        /// <summary>登録簿のインスタンスを受ける（DI とテスト）。</summary>
        public InputDeviceRegistryAdapter(InputDeviceRegistry devices)
        {
            _devices = devices ?? throw new ArgumentNullException(nameof(devices));
        }

        public IEnumerable<InputDeviceCore> FindControllers()
        {
            _devices.findControllers();
            return _devices.GetControllers();
        }

        public IEnumerable<InputDeviceCore> GetControllers() => _devices.GetControllers();

        public void StopControllers()
        {
            _devices.stopControllers();
            if (AppLogger.IsTraceEnabled)
                AppLogger.LogTrace("[DI] InputDeviceRegistryAdapter.StopControllers called");
        }

        public bool RemoveDevice(InputDeviceCore device)
        {
            if (device == null) return false;
            _devices.RemoveDevice(device);
            if (AppLogger.IsTraceEnabled)
                AppLogger.LogTrace("[DI] InputDeviceRegistryAdapter.RemoveDevice: Device removed");
            return true;
        }

        public void OnRemoval(object sender, EventArgs e)
        {
            _devices.On_Removal(sender, e);
        }

        public void UpdateSerial(object sender, EventArgs e)
        {
            _devices.UpdateSerial(sender, e);
        }

        public void ReEnableDevice(string deviceInstanceId)
        {
            if (string.IsNullOrEmpty(deviceInstanceId)) return;
            _devices.reEnableDevice(deviceInstanceId);
        }

        public bool IsExclusiveMode
        {
            get => _devices.isExclusiveMode;
            set => _devices.isExclusiveMode = value;
        }

        public event RequestElevationDelegate RequestElevation
        {
            add => _devices.RequestElevation += value;
            remove => _devices.RequestElevation -= value;
        }

        public PrepareInitDelegate PrepareDS4Init
        {
            get => _devices.PrepareDS4Init;
            set => _devices.PrepareDS4Init = value;
        }

        public PrepareInitDelegate PostDS4Init
        {
            get => _devices.PostDS4Init;
            set => _devices.PostDS4Init = value;
        }

        public CheckPendingDevice PreparePendingDevice
        {
            get => _devices.PreparePendingDevice;
            set => _devices.PreparePendingDevice = value;
        }
    }
}
