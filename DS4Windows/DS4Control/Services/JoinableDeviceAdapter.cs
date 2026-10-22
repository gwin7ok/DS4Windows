using System;
using DS4Windows.DI;
using DS4Windows.InputDevices;

namespace DS4Windows.Services
{
    /// <summary>
    /// Phase9 9-E E-6d: 実機の <see cref="DS4Device"/>（Joy-Con なら <c>JoyConDevice</c>）を <see cref="IJoinableDevice"/> として包む。
    /// 旧コードの <c>device as JoyConDevice</c> と同じ型の絞り込みを、結合相手の設定のときに行う。
    /// </summary>
    public sealed class JoinableDeviceAdapter : IJoinableDevice
    {
        private readonly DS4Device _device;

        public JoinableDeviceAdapter(DS4Device device)
        {
            _device = device ?? throw new ArgumentNullException(nameof(device));
        }

        public InputDeviceType DeviceType => _device.DeviceType;

        public bool PerformStateMerge => _device.PerformStateMerge;

        public bool PrimaryDevice => _device.PrimaryDevice;

        public int JointDeviceSlotNumber => _device.JointDeviceSlotNumber;

        public void SetJointDevice(IJoinableDevice other)
        {
            ((JoyConDevice)_device).JointDevice = (other as JoinableDeviceAdapter)?._device as JoyConDevice;
        }

        public void CopyJointStateFrom(IJoinableDevice other)
        {
            _device.JointState = ((JoinableDeviceAdapter)other)._device.JointState;
        }

        public void OnRemoval(Action handler)
        {
            _device.Removal += (sender, args) => handler();
        }
    }
}
