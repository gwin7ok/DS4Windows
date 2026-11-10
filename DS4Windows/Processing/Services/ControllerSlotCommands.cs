using System;
using DS4Windows.Input;
using DS4Windows.Input.Devices;
using DS4Windows.Processing.Abstractions;
using DS4Windows.Core.Model;

namespace DS4Windows.Processing.Services
{
    /// <summary>
    /// <see cref="IControllerSlotCommands"/> の実装。スロットの状態の持ち主（<see cref="IDeviceSlotState"/>）から機器を引いて操作する。
    /// </summary>
    public sealed class ControllerSlotCommands : IControllerSlotCommands
    {
        private readonly IDeviceSlotState _slotState;

        public ControllerSlotCommands(IDeviceSlotState slotState)
        {
            _slotState = slotState ?? throw new ArgumentNullException(nameof(slotState));
        }

        public bool IsConnected(int slot) => DeviceAt(slot) != null;

        public bool SetRumble(int slot, byte rightLightFast, byte leftHeavySlow)
        {
            DS4Device device = DeviceAt(slot);
            if (device == null) return false;

            device.setRumble(rightLightFast, leftHeavySlow);
            return true;
        }

        public bool SetHeavyRumble(int slot, byte strength)
        {
            DS4Device device = DeviceAt(slot);
            if (device == null) return false;

            device.setRumble(device.RightLightFastRumble, strength);
            return true;
        }

        public bool SetLightRumble(int slot, byte strength)
        {
            DS4Device device = DeviceAt(slot);
            if (device == null) return false;

            device.setRumble(strength, device.LeftHeavySlowRumble);
            return true;
        }

        public bool ApplyDualSenseRumbleOptions(int slot, int rumbleEmulationMode, byte hapticPowerLevel)
        {
            if (!(DeviceAt(slot) is DualSenseDevice dualsense)) return false;

            switch ((RumbleEmulationMode)rumbleEmulationMode)
            {
                case RumbleEmulationMode.Disabled:
                    dualsense.UseRumble = false;
                    dualsense.UseAccurateRumble = false;
                    break;
                case RumbleEmulationMode.Legacy:
                    dualsense.UseRumble = true;
                    dualsense.UseAccurateRumble = false;
                    break;
                case RumbleEmulationMode.Accurate:
                default:
                    dualsense.UseRumble = true;
                    dualsense.UseAccurateRumble = true;
                    break;
            }

            dualsense.HapticPowerLevel = hapticPowerLevel;
            return true;
        }

        public bool ResetGyroCalibration(int slot)
        {
            DS4Device device = DeviceAt(slot);
            if (device == null) return false;

            device.SixAxis.ResetContinuousCalibration();
            if (device.JointDeviceSlotNumber != DS4Device.DEFAULT_JOINT_SLOT_NUMBER)
            {
                // 相手側が未接続（null）でも落ちない
                DeviceAt(device.JointDeviceSlotNumber)?.SixAxis.ResetContinuousCalibration();
            }

            return true;
        }

        private DS4Device DeviceAt(int slot)
        {
            DS4Device[] controllers = _slotState.DS4Controllers;
            if (controllers == null || slot < 0 || slot >= controllers.Length)
                return null;

            return controllers[slot];
        }
    }
}
