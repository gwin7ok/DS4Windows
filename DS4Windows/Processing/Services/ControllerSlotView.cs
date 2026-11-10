using System;
using DS4Windows.Core.Abstractions;
using DS4Windows.Core.Model;
using DS4Windows.Core.Settings;
using DS4Windows.Input;
using DS4Windows.Processing.Abstractions;

namespace DS4Windows.Processing.Services
{
    /// <summary>
    /// <see cref="InputDeviceCore"/> を <see cref="IControllerSlotView"/> として UI に見せるアダプター。
    /// 機器のイベントは作成時に一度だけ購読して転送する（機器と同じ寿命。機器が破棄されれば、機器が持つこの参照も消える）。
    /// </summary>
    internal sealed class ControllerSlotView : IControllerSlotView
    {
        private readonly IProfileXmlStore _profileXmlStore;

        internal ControllerSlotView(InputDeviceCore device, IProfileXmlStore profileXmlStore)
        {
            Device = device ?? throw new ArgumentNullException(nameof(device));
            _profileXmlStore = profileXmlStore;

            device.BatteryChanged += (s, e) => BatteryChanged?.Invoke(this, e);
            device.ChargingChanged += (s, e) => ChargingChanged?.Invoke(this, e);
            device.MacAddressChanged += (s, e) => MacAddressChanged?.Invoke(this, e);
            device.SyncChange += (s, e) => SyncChange?.Invoke(this, e);
            device.Removal += (s, e) => Removal?.Invoke(this, e);
        }

        /// <summary>包んでいる機器。第 2 層の内側（このアセンブリ）だけが使う。</summary>
        internal InputDeviceCore Device { get; }

        public string DisplayName => Device.DisplayName;

        public string MacAddress => Device.MacAddress;

        public ConnectionType ConnectionType => Device.ConnectionType;

        public int Battery => Device.Battery;

        public bool Charging => Device.Charging;

        public double Latency => Device.Latency;

        public bool Synced => Device.Synced;

        public bool HasValidSerial => Device.isValidSerial();

        public bool IsRemoving => Device.IsRemoving;

        public bool PrimaryDevice => Device.PrimaryDevice;

        public DeviceExclusiveStatus ExclusiveStatus => Device.CurrentExclusiveStatus;

        public ControllerOptionsStore OptionsStore => Device.optionsStore;

        public event EventHandler BatteryChanged;

        public event EventHandler ChargingChanged;

        public event EventHandler MacAddressChanged;

        public event EventHandler SyncChange;

        public event EventHandler Removal;

        public bool DisconnectBluetooth() => Device.DisconnectBT();

        public bool DisconnectDongle() => Device.DisconnectDongle();

        public void QueueEvent(Action action) => Device.queueEvent(action);

        public void HaltReportingRunAction(Action action) => Device.HaltReportingRunAction(action);

        public bool SaveControllerConfigs()
            => _profileXmlStore != null && _profileXmlStore.SaveControllerConfigsForDevice(Device);
    }
}
