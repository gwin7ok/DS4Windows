using System;
using DS4Windows.Services;

namespace DS4Windows.Actions
{
    /// <summary>
    /// Phase8-Step7 7.6b: <see cref="IControllerDisconnectPort"/> の本番の実装。<c>IDeviceStateAccessor</c>（＝ControlService）越しに <c>DS4Device</c> を操作する。
    /// ControlService は後から作られるため、遅延（<c>Func</c>）で受ける（<see cref="DefaultGyroCalibrationPort"/> と同じ方式）。
    /// 旧経路と違い、コントローラーがいなくても例外にしない。
    /// </summary>
    public sealed class DefaultControllerDisconnectPort : IControllerDisconnectPort
    {
        private readonly Func<IDeviceStateAccessor> _deviceStateAccessor;

        public DefaultControllerDisconnectPort(Func<IDeviceStateAccessor> deviceStateAccessor)
        {
            _deviceStateAccessor = deviceStateAccessor ?? throw new ArgumentNullException(nameof(deviceStateAccessor));
        }

        private DS4Device Controller(int deviceIndex) => _deviceStateAccessor()?.GetController(deviceIndex);

        public DisconnectTargetKind GetTargetKind(int deviceIndex)
        {
            DS4Device d = Controller(deviceIndex);
            if (d == null || !d.isSynced() || d.isCharging()) return DisconnectTargetKind.None;

            switch (d.getConnectionType())
            {
                case ConnectionType.BT: return DisconnectTargetKind.Bluetooth;
                case ConnectionType.SONYWA: return DisconnectTargetKind.Dongle;
                default: return DisconnectTargetKind.None;
            }
        }

        public bool DisconnectBluetooth(int deviceIndex)
        {
            DS4Device d = Controller(deviceIndex);
            return d != null && d.DisconnectBT();
        }

        public bool IsDongleSynced(int deviceIndex)
        {
            DS4Device d = Controller(deviceIndex);
            return d != null && d.getConnectionType() == ConnectionType.SONYWA && d.isSynced();
        }

        public bool IsIdle(int deviceIndex)
        {
            DS4Device d = Controller(deviceIndex);
            return d != null && d.isDS4Idle();
        }

        public bool DisconnectDongle(int deviceIndex)
        {
            DS4Device d = Controller(deviceIndex);
            return d != null && d.DisconnectDongle();
        }
    }
}
