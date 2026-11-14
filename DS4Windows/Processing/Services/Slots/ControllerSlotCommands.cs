using System;
using DS4Windows.Input;
using DS4Windows.Input.Devices;
using DS4Windows.Processing.Abstractions;
using DS4Windows.Core.Model;

namespace DS4Windows.Processing.Services.Slots
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
            InputDeviceCore device = DeviceAt(slot);
            if (device == null) return false;

            device.setRumble(rightLightFast, leftHeavySlow);
            return true;
        }

        public bool SetHeavyRumble(int slot, byte strength)
        {
            InputDeviceCore device = DeviceAt(slot);
            if (device == null) return false;

            device.setRumble(device.RightLightFastRumble, strength);
            return true;
        }

        public bool SetLightRumble(int slot, byte strength)
        {
            InputDeviceCore device = DeviceAt(slot);
            if (device == null) return false;

            device.setRumble(strength, device.LeftHeavySlowRumble);
            return true;
        }

        public bool ApplyDualSenseRumbleOptions(int slot, int rumbleEmulationMode, byte hapticPowerLevel)
        {
            InputDeviceCore device = DeviceAt(slot);
            return device != null && device.ApplyRumbleEmulation((RumbleEmulationMode)rumbleEmulationMode, hapticPowerLevel);
        }

        public bool ResetGyroCalibration(int slot)
        {
            InputDeviceCore device = DeviceAt(slot);
            if (device == null) return false;

            device.SixAxis.ResetContinuousCalibration();
            return true;
        }

        private InputDeviceCore DeviceAt(int slot)
        {
            InputDeviceCore[] controllers = _slotState.Controllers;
            if (controllers == null || slot < 0 || slot >= controllers.Length)
                return null;

            return controllers[slot];
        }
    }
}
