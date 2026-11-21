#nullable enable

using System;
using DS4Windows.Input;
using DS4Windows.Processing.Abstractions;

namespace DS4Windows.Processing.Services.Connection
{
    /// <summary>
    /// 機器の「取り外し・同期の変化・シリアルの変化」を、切断の処理（<see cref="IDeviceDisconnectHandler"/>）と
    /// 機器の登録簿（<see cref="IInputDeviceRegistry"/>）へ結ぶ。接続の組み立て（<see cref="DeviceConnector"/>）が、機器ごとに 1 回呼ぶ。
    /// 購読の順序は、切断の処理が先、登録簿が後（取り外しの通知の順序を変えない）。
    /// </summary>
    public sealed class DeviceLifecycleHooks
    {
        private readonly IDeviceDisconnectHandler _deviceDisconnect;
        private readonly IInputDeviceRegistry _deviceRegistry;

        public DeviceLifecycleHooks(IDeviceDisconnectHandler deviceDisconnect, IInputDeviceRegistry deviceRegistry)
        {
            ArgumentNullException.ThrowIfNull(deviceDisconnect);
            ArgumentNullException.ThrowIfNull(deviceRegistry);
            _deviceDisconnect = deviceDisconnect;
            _deviceRegistry = deviceRegistry;
        }

        public void Attach(InputDeviceCore device)
        {
            device.Removal += _deviceDisconnect.OnRemoval;
            device.Removal += _deviceRegistry.OnRemoval;
            device.SyncChange += _deviceDisconnect.OnSyncChange;
            device.SyncChange += _deviceRegistry.UpdateSerial;
            device.SerialChange += _deviceDisconnect.OnSerialChange;
        }
    }
}
