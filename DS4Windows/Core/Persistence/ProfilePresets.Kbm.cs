using System.Windows.Input;
using DS4Windows.Core.Abstractions;
using DS4Windows.Core.Model;
using DS4Windows.Core.Settings;

namespace DS4Windows.Core.Persistence
{
    /// <summary>キーボード・マウス用の既定のプロファイルの読み込み。</summary>
    public partial class ProfilePresets
    {
        public void LoadDefaultKBMProfile(int device, bool launchprogram, IProfileLoadTarget control,
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

            StickDeadZoneInfo lsInfo = store.stickSlots[device].LSMod;
            lsInfo.antiDeadZone = 0;

            StickDeadZoneInfo rsInfo = store.stickSlots[device].RSMod;
            rsInfo.deadZone = (int)(0.035 * 127);
            rsInfo.antiDeadZone = 0;
            rsInfo.maxZone = 90;

            TriggerDeadZoneZInfo l2Info = store.triggerSlots[device].L2Mod;
            l2Info.deadZone = (byte)(0.20 * 255);

            TriggerDeadZoneZInfo r2Info = store.triggerSlots[device].R2Mod;
            r2Info.deadZone = (byte)(0.20 * 255);

            // Flag to unplug virtual controller
            store.basicSlots[device].DinputOnly = true;

            DS4ControlSettings? setting = store.GetRequiredDS4CSetting(device, DS4Controls.LYNeg);
            setting.UpdateSettings(KeyInterop.VirtualKeyFromKey(Key.W), "", DS4KeyType.None);
            setting = store.GetRequiredDS4CSetting(device, DS4Controls.LXNeg);
            setting.UpdateSettings(KeyInterop.VirtualKeyFromKey(Key.A), "", DS4KeyType.None);
            setting = store.GetRequiredDS4CSetting(device, DS4Controls.LYPos);
            setting.UpdateSettings(KeyInterop.VirtualKeyFromKey(Key.S), "", DS4KeyType.None);
            setting = store.GetRequiredDS4CSetting(device, DS4Controls.LXPos);
            setting.UpdateSettings(KeyInterop.VirtualKeyFromKey(Key.D), "", DS4KeyType.None);
            setting = store.GetRequiredDS4CSetting(device, DS4Controls.L3);
            setting.UpdateSettings(KeyInterop.VirtualKeyFromKey(Key.LeftShift), "", DS4KeyType.None);

            setting = store.GetRequiredDS4CSetting(device, DS4Controls.RYNeg);
            setting.UpdateSettings(X360Controls.MouseUp, "", DS4KeyType.None);
            setting = store.GetRequiredDS4CSetting(device, DS4Controls.RYPos);
            setting.UpdateSettings(X360Controls.MouseDown, "", DS4KeyType.None);
            setting = store.GetRequiredDS4CSetting(device, DS4Controls.RXNeg);
            setting.UpdateSettings(X360Controls.MouseLeft, "", DS4KeyType.None);
            setting = store.GetRequiredDS4CSetting(device, DS4Controls.RXPos);
            setting.UpdateSettings(X360Controls.MouseRight, "", DS4KeyType.None);
            setting = store.GetRequiredDS4CSetting(device, DS4Controls.R3);
            setting.UpdateSettings(KeyInterop.VirtualKeyFromKey(Key.LeftCtrl), "", DS4KeyType.None);

            setting = store.GetRequiredDS4CSetting(device, DS4Controls.DpadUp);
            setting.UpdateSettings(X360Controls.Unbound, "", DS4KeyType.None);
            setting = store.GetRequiredDS4CSetting(device, DS4Controls.DpadRight);
            setting.UpdateSettings(X360Controls.WDOWN, "", DS4KeyType.None);
            setting = store.GetRequiredDS4CSetting(device, DS4Controls.DpadDown);
            setting.UpdateSettings(X360Controls.Unbound, "", DS4KeyType.None);
            setting = store.GetRequiredDS4CSetting(device, DS4Controls.DpadLeft);
            setting.UpdateSettings(X360Controls.WUP, "", DS4KeyType.None);

            setting = store.GetRequiredDS4CSetting(device, DS4Controls.Cross);
            setting.UpdateSettings(KeyInterop.VirtualKeyFromKey(Key.Space), "", DS4KeyType.None);
            setting = store.GetRequiredDS4CSetting(device, DS4Controls.Square);
            setting.UpdateSettings(KeyInterop.VirtualKeyFromKey(Key.F), "", DS4KeyType.None);
            setting = store.GetRequiredDS4CSetting(device, DS4Controls.Triangle);
            setting.UpdateSettings(KeyInterop.VirtualKeyFromKey(Key.E), "", DS4KeyType.None);
            setting = store.GetRequiredDS4CSetting(device, DS4Controls.Circle);
            setting.UpdateSettings(KeyInterop.VirtualKeyFromKey(Key.C), "", DS4KeyType.None);

            setting = store.GetRequiredDS4CSetting(device, DS4Controls.L1);
            setting.UpdateSettings(KeyInterop.VirtualKeyFromKey(Key.Q), "", DS4KeyType.None);
            setting = store.GetRequiredDS4CSetting(device, DS4Controls.L2);
            setting.UpdateSettings(X360Controls.RightMouse, "", DS4KeyType.None);
            setting = store.GetRequiredDS4CSetting(device, DS4Controls.R1);
            setting.UpdateSettings(KeyInterop.VirtualKeyFromKey(Key.R), "", DS4KeyType.None);
            setting = store.GetRequiredDS4CSetting(device, DS4Controls.R2);
            setting.UpdateSettings(X360Controls.LeftMouse, "", DS4KeyType.None);

            setting = store.GetRequiredDS4CSetting(device, DS4Controls.Share);
            setting.UpdateSettings(KeyInterop.VirtualKeyFromKey(Key.Tab), "", DS4KeyType.None);
            setting = store.GetRequiredDS4CSetting(device, DS4Controls.Options);
            setting.UpdateSettings(KeyInterop.VirtualKeyFromKey(Key.Escape), "", DS4KeyType.None);

            // If a device exists, make sure to transfer relevant profile device
            // options to device instance
            if (postLoad && device < ControllerLimits.MaxControllerCount)
            {
                store.PostLoadSnippet(device, control, xinputStatus, xinputPlug);
            }
        }

        public void LoadDefaultKBMGyroMouseProfile(int device, bool launchprogram, IProfileLoadTarget control,
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

            StickDeadZoneInfo lsInfo = store.stickSlots[device].LSMod;
            lsInfo.antiDeadZone = 0;

            StickDeadZoneInfo rsInfo = store.stickSlots[device].RSMod;
            rsInfo.deadZone = (int)(0.105 * 127);
            rsInfo.antiDeadZone = 0;
            rsInfo.maxZone = 90;

            TriggerDeadZoneZInfo l2Info = store.triggerSlots[device].L2Mod;
            l2Info.deadZone = (byte)(0.20 * 255);

            TriggerDeadZoneZInfo r2Info = store.triggerSlots[device].R2Mod;
            r2Info.deadZone = (byte)(0.20 * 255);

            store.gyroSlots[device].OutputMode = GyroOutMode.Mouse;
            store.gyroSlots[device].MouseTriggers = "4";
            store.gyroSlots[device].MouseTriggerCondAnd = true;
            store.gyroSlots[device].TriggerTurns = false;
            store.gyroSlots[device].Mouse.enableSmoothing = true;
            store.gyroSlots[device].Mouse.smoothingMethod = GyroSmoothingMethod.OneEuro;

            // Flag to unplug virtual controller
            store.basicSlots[device].DinputOnly = true;

            DS4ControlSettings? setting = store.GetRequiredDS4CSetting(device, DS4Controls.LYNeg);
            setting.UpdateSettings(KeyInterop.VirtualKeyFromKey(Key.W), "", DS4KeyType.None);
            setting = store.GetRequiredDS4CSetting(device, DS4Controls.LXNeg);
            setting.UpdateSettings(KeyInterop.VirtualKeyFromKey(Key.A), "", DS4KeyType.None);
            setting = store.GetRequiredDS4CSetting(device, DS4Controls.LYPos);
            setting.UpdateSettings(KeyInterop.VirtualKeyFromKey(Key.S), "", DS4KeyType.None);
            setting = store.GetRequiredDS4CSetting(device, DS4Controls.LXPos);
            setting.UpdateSettings(KeyInterop.VirtualKeyFromKey(Key.D), "", DS4KeyType.None);
            setting = store.GetRequiredDS4CSetting(device, DS4Controls.L3);
            setting.UpdateSettings(KeyInterop.VirtualKeyFromKey(Key.LeftShift), "", DS4KeyType.None);

            setting = store.GetRequiredDS4CSetting(device, DS4Controls.RYNeg);
            setting.UpdateSettings(X360Controls.MouseUp, "", DS4KeyType.None);
            setting = store.GetRequiredDS4CSetting(device, DS4Controls.RYPos);
            setting.UpdateSettings(X360Controls.MouseDown, "", DS4KeyType.None);
            setting = store.GetRequiredDS4CSetting(device, DS4Controls.RXNeg);
            setting.UpdateSettings(X360Controls.MouseLeft, "", DS4KeyType.None);
            setting = store.GetRequiredDS4CSetting(device, DS4Controls.RXPos);
            setting.UpdateSettings(X360Controls.MouseRight, "", DS4KeyType.None);
            setting = store.GetRequiredDS4CSetting(device, DS4Controls.R3);
            setting.UpdateSettings(KeyInterop.VirtualKeyFromKey(Key.LeftCtrl), "", DS4KeyType.None);

            setting = store.GetRequiredDS4CSetting(device, DS4Controls.DpadUp);
            setting.UpdateSettings(X360Controls.Unbound, "", DS4KeyType.None);
            setting = store.GetRequiredDS4CSetting(device, DS4Controls.DpadRight);
            setting.UpdateSettings(X360Controls.WDOWN, "", DS4KeyType.None);
            setting = store.GetRequiredDS4CSetting(device, DS4Controls.DpadDown);
            setting.UpdateSettings(X360Controls.Unbound, "", DS4KeyType.None);
            setting = store.GetRequiredDS4CSetting(device, DS4Controls.DpadLeft);
            setting.UpdateSettings(X360Controls.WUP, "", DS4KeyType.None);

            setting = store.GetRequiredDS4CSetting(device, DS4Controls.Cross);
            setting.UpdateSettings(KeyInterop.VirtualKeyFromKey(Key.Space), "", DS4KeyType.None);
            setting = store.GetRequiredDS4CSetting(device, DS4Controls.Square);
            setting.UpdateSettings(KeyInterop.VirtualKeyFromKey(Key.F), "", DS4KeyType.None);
            setting = store.GetRequiredDS4CSetting(device, DS4Controls.Triangle);
            setting.UpdateSettings(KeyInterop.VirtualKeyFromKey(Key.E), "", DS4KeyType.None);
            setting = store.GetRequiredDS4CSetting(device, DS4Controls.Circle);
            setting.UpdateSettings(KeyInterop.VirtualKeyFromKey(Key.C), "", DS4KeyType.None);

            setting = store.GetRequiredDS4CSetting(device, DS4Controls.L1);
            //setting.UpdateSettings(KeyInterop.VirtualKeyFromKey(Key.Q), "", DS4KeyType.None);
            setting.UpdateSettings(X360Controls.Unbound, "", DS4KeyType.None);
            setting = store.GetRequiredDS4CSetting(device, DS4Controls.L2);
            setting.UpdateSettings(X360Controls.RightMouse, "", DS4KeyType.None);
            setting = store.GetRequiredDS4CSetting(device, DS4Controls.R1);
            setting.UpdateSettings(KeyInterop.VirtualKeyFromKey(Key.R), "", DS4KeyType.None);
            setting = store.GetRequiredDS4CSetting(device, DS4Controls.R2);
            setting.UpdateSettings(X360Controls.LeftMouse, "", DS4KeyType.None);

            setting = store.GetRequiredDS4CSetting(device, DS4Controls.Share);
            setting.UpdateSettings(KeyInterop.VirtualKeyFromKey(Key.Tab), "", DS4KeyType.None);
            setting = store.GetRequiredDS4CSetting(device, DS4Controls.Options);
            setting.UpdateSettings(KeyInterop.VirtualKeyFromKey(Key.Escape), "", DS4KeyType.None);

            // If a device exists, make sure to transfer relevant profile device
            // options to device instance
            if (postLoad && device < ControllerLimits.MaxControllerCount)
            {
                store.PostLoadSnippet(device, control, xinputStatus, xinputPlug);
            }
        }
    }
}
