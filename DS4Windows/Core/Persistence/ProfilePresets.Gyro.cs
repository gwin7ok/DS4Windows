using DS4Windows.Core.Abstractions;
using DS4Windows.Core.Model;
using DS4Windows.Core.Settings;

namespace DS4Windows.Core.Persistence
{
    /// <summary>ジャイロを使う既定のプロファイルの読み込み。</summary>
    public partial class ProfilePresets
    {
        public void LoadDefaultGamepadGyroProfile(int device, bool launchprogram, IProfileLoadTarget control,
            string propath = "", bool xinputChange = true, bool postLoad = true)
        {
            bool xinputPlug = false;
            bool xinputStatus = false;

            OutContType oldContType = store.ActiveOutDevType[device];

            // Make sure to reset currently set profile values before parsing
            store.ResetProfile(device);
            store.ResetMouseProperties(device, control);

            // Only change xinput devices under certain conditions. Avoid
            // performing this upon program startup before loading devices.
            if (xinputChange)
            {
                store.CheckOldDevicestatus(device, control, oldContType,
                    out xinputPlug, out xinputStatus);
            }

            foreach (DS4ControlSettings dcs in store.ds4settings[device])
                dcs.Reset();

            store.GetRequiredProfileActions(device).Clear();
            store.containsCustomAction[device] = false;
            store.containsCustomExtras[device] = false;

            store.gyroSlots[device].OutputMode = GyroOutMode.MouseJoystick;
            store.gyroSlots[device].MouseStickTriggers = "4";
            store.gyroSlots[device].MouseStickTriggerCondAnd = true;
            store.gyroSlots[device].MouseStickTriggerTurns = false;
            store.gyroSlots[device].MouseStick.useSmoothing = true;
            store.gyroSlots[device].MouseStick.smoothingMethod = GyroSmoothingMethod.OneEuro;

            // If a device exists, make sure to transfer relevant profile device
            // options to device instance
            if (postLoad && device < ControllerLimits.MaxControllerCount)
            {
                store.PostLoadSnippet(device, control, xinputStatus, xinputPlug);
            }
        }

        public void LoadDefaultDS4GamepadGyroProfile(int device, bool launchprogram, IProfileLoadTarget control,
            string propath = "", bool xinputChange = true, bool postLoad = true)
        {
            PrepareBlankingProfile(device, control, out bool xinputPlug, out bool xinputStatus, xinputChange);

            StickDeadZoneInfo lsInfo = store.stickSlots[device].LSMod;
            lsInfo.deadZone = (int)(0.00 * 127);
            lsInfo.antiDeadZone = 0;
            lsInfo.maxZone = 100;

            StickDeadZoneInfo rsInfo = store.stickSlots[device].RSMod;
            rsInfo.deadZone = (int)(0.00 * 127);
            rsInfo.antiDeadZone = 0;
            rsInfo.maxZone = 100;

            TriggerDeadZoneZInfo l2Info = store.triggerSlots[device].L2Mod;
            l2Info.deadZone = (byte)(0.00 * 255);

            TriggerDeadZoneZInfo r2Info = store.triggerSlots[device].R2Mod;
            r2Info.deadZone = (byte)(0.00 * 255);

            store.gyroSlots[device].OutputMode = GyroOutMode.MouseJoystick;
            store.gyroSlots[device].MouseStickTriggers = "4";
            store.gyroSlots[device].MouseStickTriggerCondAnd = true;
            store.gyroSlots[device].MouseStickTriggerTurns = false;
            store.gyroSlots[device].MouseStick.useSmoothing = true;
            store.gyroSlots[device].MouseStick.smoothingMethod = GyroSmoothingMethod.OneEuro;

            store.outputTypeSlots[device].ContType = OutContType.DS4;

            // If a device exists, make sure to transfer relevant profile device
            // options to device instance
            if (postLoad && device < ControllerLimits.MaxControllerCount)
            {
                store.PostLoadSnippet(device, control, xinputStatus, xinputPlug);
            }
        }

        public void LoadDefaultMixedGyroMouseProfile(int device, bool launchprogram, IProfileLoadTarget control,
            string propath = "", bool xinputChange = true, bool postLoad = true)
        {
            bool xinputPlug = false;
            bool xinputStatus = false;

            OutContType oldContType = store.ActiveOutDevType[device];

            // Make sure to reset currently set profile values before parsing
            store.ResetProfile(device);
            store.ResetMouseProperties(device, control);

            // Only change xinput devices under certain conditions. Avoid
            // performing this upon program startup before loading devices.
            if (xinputChange)
            {
                store.CheckOldDevicestatus(device, control, oldContType,
                    out xinputPlug, out xinputStatus);
            }

            foreach (DS4ControlSettings dcs in store.ds4settings[device])
                dcs.Reset();

            store.GetRequiredProfileActions(device).Clear();
            store.containsCustomAction[device] = false;
            store.containsCustomExtras[device] = false;

            store.gyroSlots[device].OutputMode = GyroOutMode.Mouse;
            store.gyroSlots[device].MouseTriggers = "4";
            store.gyroSlots[device].MouseTriggerCondAnd = true;
            store.gyroSlots[device].TriggerTurns = false;
            store.gyroSlots[device].Mouse.enableSmoothing = true;
            store.gyroSlots[device].Mouse.smoothingMethod = GyroSmoothingMethod.OneEuro;

            StickDeadZoneInfo rsInfo = store.stickSlots[device].RSMod;
            rsInfo.deadZone = (int)(0.10 * 127);
            rsInfo.antiDeadZone = 0;
            rsInfo.maxZone = 90;

            // If a device exists, make sure to transfer relevant profile device
            // options to device instance
            if (postLoad && device < ControllerLimits.MaxControllerCount)
            {
                store.PostLoadSnippet(device, control, xinputStatus, xinputPlug);
            }
        }

        public void LoadDefaultDS4MixedGyroMouseProfile(int device, bool launchprogram, IProfileLoadTarget control,
            string propath = "", bool xinputChange = true, bool postLoad = true)
        {
            PrepareBlankingProfile(device, control, out bool xinputPlug, out bool xinputStatus, xinputChange);

            StickDeadZoneInfo lsInfo = store.stickSlots[device].LSMod;
            lsInfo.deadZone = (int)(0.00 * 127);
            lsInfo.antiDeadZone = 0;
            lsInfo.maxZone = 100;

            StickDeadZoneInfo rsInfo = store.stickSlots[device].RSMod;
            rsInfo.deadZone = (int)(0.10 * 127);
            rsInfo.antiDeadZone = 0;
            rsInfo.maxZone = 100;

            TriggerDeadZoneZInfo l2Info = store.triggerSlots[device].L2Mod;
            l2Info.deadZone = (byte)(0.00 * 255);

            TriggerDeadZoneZInfo r2Info = store.triggerSlots[device].R2Mod;
            r2Info.deadZone = (byte)(0.00 * 255);

            store.gyroSlots[device].OutputMode = GyroOutMode.Mouse;
            store.gyroSlots[device].MouseTriggers = "4";
            store.gyroSlots[device].MouseTriggerCondAnd = true;
            store.gyroSlots[device].TriggerTurns = false;
            store.gyroSlots[device].Mouse.enableSmoothing = true;
            store.gyroSlots[device].Mouse.smoothingMethod = GyroSmoothingMethod.OneEuro;

            store.outputTypeSlots[device].ContType = OutContType.DS4;

            // If a device exists, make sure to transfer relevant profile device
            // options to device instance
            if (postLoad && device < ControllerLimits.MaxControllerCount)
            {
                store.PostLoadSnippet(device, control, xinputStatus, xinputPlug);
            }
        }
    }
}
