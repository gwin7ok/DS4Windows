using System;
using DS4Windows.Core.Abstractions;
using DS4Windows.Core.Model;
using DS4Windows.Core.Settings;

namespace DS4Windows.Core.Persistence
{
    /// <summary>
    /// プロファイルのプリセット（空のプロファイルと、既定の割り当て 8 種）の読込。
    /// 旧: <c>BackingStore</c> の <c>LoadBlankDS4Profile</c>／<c>LoadBlankProfile</c>／<c>LoadDefault*Profile</c>（8 種）／<c>PrepareBlankingProfile</c>（移動のみ。
    /// ロジック・値・ログは変えない）。<c>BackingStore</c> の公開状態を <c>store.</c> で書き換える。呼び出し元は <see cref="ProfilePresetLoader"/> だけ。
    /// </summary>
    public partial class ProfilePresets
    {
        private readonly BackingStore store;

        public ProfilePresets(BackingStore store)
        {
            ArgumentNullException.ThrowIfNull(store);
            this.store = store;
        }

        private void PrepareBlankingProfile(int device, IProfileLoadTarget control, out bool xinputPlug, out bool xinputStatus, bool xinputChange = true)
        {
            xinputPlug = false;
            xinputStatus = false;

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
        }

        public void LoadBlankDS4Profile(int device, bool launchprogram, IProfileLoadTarget control,
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

            store.outputTypeSlots[device].ContType = OutContType.DS4;

            // If a device exists, make sure to transfer relevant profile device
            // options to device instance
            if (postLoad && device < ControllerLimits.MaxControllerCount)
            {
                store.PostLoadSnippet(device, control, xinputStatus, xinputPlug);
            }
        }

        public void LoadBlankProfile(int device, bool launchprogram, IProfileLoadTarget control,
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

            // If a device exists, make sure to transfer relevant profile device
            // options to device instance
            if (postLoad && device < ControllerLimits.MaxControllerCount)
            {
                store.PostLoadSnippet(device, control, xinputStatus, xinputPlug);
            }
        }

        public void LoadDefaultDS4MixedControlsProfile(int device, bool launchprogram, IProfileLoadTarget control,
            string propath = "", bool xinputChange = true, bool postLoad = true)
        {
            PrepareBlankingProfile(device, control, out bool xinputPlug, out bool xinputStatus, xinputChange);

            DS4ControlSettings? setting = store.GetRequiredDS4CSetting(device, DS4Controls.RYNeg);
            setting.UpdateSettings(X360Controls.MouseUp, "", DS4KeyType.None);
            setting = store.GetRequiredDS4CSetting(device, DS4Controls.RYPos);
            setting.UpdateSettings(X360Controls.MouseDown, "", DS4KeyType.None);
            setting = store.GetRequiredDS4CSetting(device, DS4Controls.RXNeg);
            setting.UpdateSettings(X360Controls.MouseLeft, "", DS4KeyType.None);
            setting = store.GetRequiredDS4CSetting(device, DS4Controls.RXPos);
            setting.UpdateSettings(X360Controls.MouseRight, "", DS4KeyType.None);

            StickDeadZoneInfo rsInfo = store.stickSlots[device].RSMod;
            rsInfo.deadZone = (int)(0.035 * 127);
            rsInfo.antiDeadZone = 0;
            rsInfo.maxZone = 90;

            store.outputTypeSlots[device].ContType = OutContType.DS4;

            // If a device exists, make sure to transfer relevant profile device
            // options to device instance
            if (postLoad && device < ControllerLimits.MaxControllerCount)
            {
                store.PostLoadSnippet(device, control, xinputStatus, xinputPlug);
            }
        }

        public void LoadDefaultMixedControlsProfile(int device, bool launchprogram, IProfileLoadTarget control,
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

            DS4ControlSettings? setting = store.GetRequiredDS4CSetting(device, DS4Controls.RYNeg);
            setting.UpdateSettings(X360Controls.MouseUp, "", DS4KeyType.None);
            setting = store.GetRequiredDS4CSetting(device, DS4Controls.RYPos);
            setting.UpdateSettings(X360Controls.MouseDown, "", DS4KeyType.None);
            setting = store.GetRequiredDS4CSetting(device, DS4Controls.RXNeg);
            setting.UpdateSettings(X360Controls.MouseLeft, "", DS4KeyType.None);
            setting = store.GetRequiredDS4CSetting(device, DS4Controls.RXPos);
            setting.UpdateSettings(X360Controls.MouseRight, "", DS4KeyType.None);

            StickDeadZoneInfo rsInfo = store.stickSlots[device].RSMod;
            rsInfo.deadZone = (int)(0.035 * 127);
            rsInfo.antiDeadZone = 0;
            rsInfo.maxZone = 90;

            // If a device exists, make sure to transfer relevant profile device
            // options to device instance
            if (postLoad && device < ControllerLimits.MaxControllerCount)
            {
                store.PostLoadSnippet(device, control, xinputStatus, xinputPlug);
            }
        }
    }
}
