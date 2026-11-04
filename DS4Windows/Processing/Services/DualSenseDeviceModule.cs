using System;
using System.Collections.Generic;
using DS4Windows.DI;
using DS4Windows.InputDevices;

namespace DS4Windows.Services
{
    /// <summary>
    /// Phase9 9-E E-6c: DualSense の部品。設定の有効／無効、追加ボタン（Edge の FnL／FnR／BLP／BRP）、LED モード変更への反応、振動エミュレーションと触覚の強さの反映。
    /// 旧 <c>ControlService</c> の該当する分岐を、そのまま移したもの。
    /// </summary>
    public sealed class DualSenseDeviceModule : InputDeviceModuleBase
    {
        public override bool Handles(InputDeviceType type) => type == InputDeviceType.DualSense;

        public override bool IsEnabled(ControlServiceDeviceOptions options) => options.DualSenseOpts.Enabled;

        public override List<DS4Controls> GetKnownExtraButtons(DS4Device device)
        {
            List<DS4Controls> result = new List<DS4Controls>();
            InputDevices.DualSenseDevice tempDev = device as InputDevices.DualSenseDevice;
            if (tempDev != null &&
                tempDev.SubType == InputDevices.DualSenseDevice.DeviceSubType.DSEdge)
            {
                // Added extra DualSense Edge buttons as extra in the mapper.
                // Keeps from checking non-existent buttons on other device types.
                result.AddRange(new DS4Controls[] { DS4Controls.FnL, DS4Controls.FnR, DS4Controls.BLP, DS4Controls.BRP });
            }

            return result;
        }

        public override void HookSettings(DS4Device device, Func<int> activeControllers)
        {
            InputDevices.DualSenseDevice tempDSDev = device as InputDevices.DualSenseDevice;

            DualSenseControllerOptions dSOpts = tempDSDev.NativeOptionsStore;
            dSOpts.LedModeChanged += (sender, e) => { tempDSDev.CheckControllerNumDeviceSettings(activeControllers()); };
        }

        public override void ApplyProfileOptions(DS4Device device, int ind, IProfileSettingsService settings)
        {
            // DualSense specific profile settings
            if (device is InputDevices.DualSenseDevice dualsense)
            {
                switch (settings.DualSenseRumbleEmulationMode[ind])
                {
                    case InputDevices.DualSenseDevice.RumbleEmulationMode.Disabled:
                        dualsense.UseRumble = false;
                        dualsense.UseAccurateRumble = false;
                        break;
                    case InputDevices.DualSenseDevice.RumbleEmulationMode.Legacy:
                        dualsense.UseRumble = true;
                        dualsense.UseAccurateRumble = false;
                        break;
                    case InputDevices.DualSenseDevice.RumbleEmulationMode.Accurate:
                    default:
                        dualsense.UseRumble = true;
                        dualsense.UseAccurateRumble = true;
                        break;
                }
                dualsense.HapticPowerLevel = settings.DualSenseHapticPowerLevel[ind];
            }
        }
    }
}
