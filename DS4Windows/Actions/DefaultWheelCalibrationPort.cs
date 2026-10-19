using System;
using DS4Windows.Services;

namespace DS4Windows.Actions
{
    /// <summary>
    /// Phase8-Step7 7.6d: <see cref="IWheelCalibrationPort"/> の本番の実装。<c>IDeviceStateAccessor</c>（＝ControlService）越しに <c>DS4Device.WheelRecalibrateActiveState</c> を読み書きする。
    /// ControlService は後から作られるため、遅延（<c>Func</c>）で受ける（<see cref="DefaultGyroCalibrationPort"/> と同じ方式）。
    /// 旧経路と違い、コントローラーがいなくても例外にしない。
    /// </summary>
    public sealed class DefaultWheelCalibrationPort : IWheelCalibrationPort
    {
        private readonly Func<IDeviceStateAccessor> _deviceStateAccessor;

        public DefaultWheelCalibrationPort(Func<IDeviceStateAccessor> deviceStateAccessor)
        {
            _deviceStateAccessor = deviceStateAccessor ?? throw new ArgumentNullException(nameof(deviceStateAccessor));
        }

        public int GetCalibrationState(int deviceIndex)
        {
            DS4Device d = _deviceStateAccessor()?.GetController(deviceIndex);
            return d != null ? d.WheelRecalibrateActiveState : -1;
        }

        public void SetCalibrationState(int deviceIndex, int state)
        {
            DS4Device d = _deviceStateAccessor()?.GetController(deviceIndex);
            if (d != null) d.WheelRecalibrateActiveState = state;
        }
    }
}
