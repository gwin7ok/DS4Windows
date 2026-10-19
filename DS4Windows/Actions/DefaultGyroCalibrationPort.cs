using System;
using DS4Windows.Services;

namespace DS4Windows.Actions
{
    /// <summary>
    /// Phase8-Step7 7.6a: <see cref="IGyroCalibrationPort"/> の本番の実装。<c>IDeviceStateAccessor</c>（＝ControlService）越しに <c>DS4Device</c> を操作する。
    /// ControlService は後から作られるため、遅延（<c>Func</c>）で受ける（<c>Mapping</c> と同じ方式）。
    /// 旧 <c>Mapping.MapCustomAction</c> の GyroCalibrate の分岐と同じ処理（結合デバイスも含む）。旧経路と違い、コントローラーがいなくても例外にしない。
    /// </summary>
    public sealed class DefaultGyroCalibrationPort : IGyroCalibrationPort
    {
        private readonly Func<IDeviceStateAccessor> _deviceStateAccessor;

        public DefaultGyroCalibrationPort(Func<IDeviceStateAccessor> deviceStateAccessor)
        {
            _deviceStateAccessor = deviceStateAccessor ?? throw new ArgumentNullException(nameof(deviceStateAccessor));
        }

        public void ResetContinuousCalibration(int deviceIndex)
        {
            IDeviceStateAccessor accessor = _deviceStateAccessor();
            DS4Device d = accessor?.GetController(deviceIndex);
            if (d == null) return;

            d.SixAxis.ResetContinuousCalibration();
            if (d.JointDeviceSlotNumber != DS4Device.DEFAULT_JOINT_SLOT_NUMBER)
            {
                DS4Device tempDev = accessor.GetController(d.JointDeviceSlotNumber);
                tempDev?.SixAxis.ResetContinuousCalibration();
            }
        }
    }
}
