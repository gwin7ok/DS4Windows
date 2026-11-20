using System;
using DS4Windows.Input;
using DS4Windows.Processing.Abstractions;
using DS4Windows.Core.Model;

namespace DS4Windows.Processing.Actions
{
    /// <summary>
    /// <see cref="IControllerDisconnectPort"/> の本番の実装。<c>IDeviceStateAccessor</c>（＝IDeviceSlotState。Phase13-Step5 で直接注入に変更）越しに <c>InputDeviceCore</c> を操作する。
    /// ControlService は後から作られるため、遅延（<c>Func</c>）で受ける（<see cref="DefaultGyroCalibrationPort"/> と同じ方式）。
    /// 旧経路と違い、コントローラーがいなくても例外にしない。
    /// </summary>
    public sealed class DefaultControllerDisconnectPort : IControllerDisconnectPort
    {
        private readonly IDeviceStateAccessor _deviceStateAccessor;

        public DefaultControllerDisconnectPort(IDeviceStateAccessor deviceStateAccessor)
        {
            ArgumentNullException.ThrowIfNull(deviceStateAccessor);
            _deviceStateAccessor = deviceStateAccessor;
        }

        private InputDeviceCore? Controller(int deviceIndex) => _deviceStateAccessor.GetController(deviceIndex);

        public DisconnectTargetKind GetTargetKind(int deviceIndex)
        {
            InputDeviceCore? d = Controller(deviceIndex);
            if (d == null || !d.IsSynced() || d.IsCharging()) return DisconnectTargetKind.None;

            switch (d.GetConnectionType())
            {
                case ConnectionType.BT: return DisconnectTargetKind.Bluetooth;
                case ConnectionType.SONYWA: return DisconnectTargetKind.Dongle;
                default: return DisconnectTargetKind.None;
            }
        }

        public bool DisconnectBluetooth(int deviceIndex)
        {
            InputDeviceCore? d = Controller(deviceIndex);
            return d != null && d.DisconnectBT();
        }

        public bool IsDongleSynced(int deviceIndex)
        {
            InputDeviceCore? d = Controller(deviceIndex);
            return d != null && d.GetConnectionType() == ConnectionType.SONYWA && d.IsSynced();
        }

        public bool IsIdle(int deviceIndex)
        {
            InputDeviceCore? d = Controller(deviceIndex);
            return d != null && d.IsDS4Idle();
        }

        public bool DisconnectDongle(int deviceIndex)
        {
            InputDeviceCore? d = Controller(deviceIndex);
            return d != null && d.DisconnectDongle();
        }
    }
}
