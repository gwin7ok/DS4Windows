#nullable enable

using System;
using System.Collections.Generic;
using DS4Windows.Core.Abstractions;
using DS4Windows.Core.Model;
using DS4Windows.Core.Settings;
using DS4Windows.Input;

namespace DS4Windows.Processing.Services.Devices
{
    /// <summary>
    /// DualSense の部品。設定の有効／無効、追加ボタン（Edge の FnL／FnR／BLP／BRP）、LED モード変更への反応、振動エミュレーションと触覚の強さの反映。
    /// 旧 <c>ControlService</c> の該当する分岐を、そのまま移したもの。
    /// </summary>
    public sealed class DualSenseDeviceModule : InputDeviceModuleBase
    {
        protected override InputDeviceFamily? Family => InputDeviceFamily.DualSense;

        public override List<DS4Controls> GetKnownExtraButtons(InputDeviceCore device)
        {
            List<DS4Controls> result = new List<DS4Controls>();
            DS4Windows.Input.Devices.DualSenseDevice? tempDev = device as DS4Windows.Input.Devices.DualSenseDevice;
            if (tempDev != null &&
                tempDev.SubType == DS4Windows.Input.Devices.DualSenseDevice.DeviceSubType.DSEdge)
            {
                // Added extra DualSense Edge buttons as extra in the mapper.
                // Keeps from checking non-existent buttons on other device types.
                result.AddRange(new DS4Controls[] { DS4Controls.FnL, DS4Controls.FnR, DS4Controls.BLP, DS4Controls.BRP });
            }

            return result;
        }

        public override void HookSettings(InputDeviceCore device, Func<int> activeControllers)
        {
            DS4Windows.Input.Devices.DualSenseDevice? tempDSDev = device as DS4Windows.Input.Devices.DualSenseDevice;

            DualSenseControllerOptions dSOpts = tempDSDev!.NativeOptionsStore; // 実行中は設定済み（null なら従来どおり NullReferenceException）
            dSOpts.LedModeChanged += (sender, e) => { tempDSDev.CheckControllerNumDeviceSettings(activeControllers()); };
        }

        public override void ApplyProfileOptions(InputDeviceCore device, int ind, IProfileSettingsService settings)
        {
            DualSenseSlotSettings dualSense = settings.DualSenseSlot(ind);
            device.ApplyRumbleEmulation(dualSense.RumbleEmulationMode, dualSense.HapticPowerLevel);
        }
    }
}
