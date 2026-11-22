using DS4Windows.Core.Settings;
using DS4Windows.Core.Model;

namespace DS4Windows.Core.Persistence.ProfileSections
{
    /// <summary>
    /// DualSense 固有の設定・トリガーの二段階・曲線・SX/SZ の曲線（DualSenseControllerSettings〜SZOutputCurveMode）の読込。
    /// 要素の順序・既定値・欠けた設定の扱いは、読み込みの変異ゴールデン（<c>ProfileLoadMutationGoldenTests</c>）が固定している。
    /// </summary>
    internal sealed class ProfileTriggerCurveLoadSection : IProfileLoadSection
    {
        public void Load(ProfileLoadContext context)
        {
            BackingStore store = context.Store;
            int device = context.Device;
            string path(string name) => "/" + context.RootName + "/" + name;
            int axisOutputCurveId(string name) => store.StickOutputCurveId(name);

            // DualSense 固有の設定。グループ（DualSenseControllerSettings、RumbleSettings）がないときは、中の 3 要素もないので、どれも「欠けた設定」になる
            string rumble = path("DualSenseControllerSettings") + "/RumbleSettings/";
            context.ReadLegacy(rumble + "EmulationMode", t =>
            {
                RumbleEmulationMode.TryParse(t, out RumbleEmulationMode temp);
                store.dualSenseSlots[device].RumbleEmulationMode = temp;
            });
            context.ReadLegacy(rumble + "EnableGenericRumbleRescale", t =>
            {
                bool.TryParse(t, out bool temp);
                store.dualSenseSlots[device].UseGenericRumbleRescale = temp;
            });
            context.ReadLegacy(rumble + "HapticPowerLevel", t =>
            {
                byte.TryParse(t, out byte temp);
                store.dualSenseSlots[device].HapticPowerLevel = temp;
            });

            context.ReadLegacy(path("L2OutputCurveCustom"), t => store.triggerSlots[device].L2Bezier.CustomDefinition = t);
            context.ReadLegacy(path("L2OutputCurveMode"), t => store.triggerSlots[device].ApplyL2CurveMode(axisOutputCurveId(t)), () => store.triggerSlots[device].ApplyL2CurveMode(0));

            context.ReadEnum<TwoStageTriggerMode>(context.Xdoc, path("L2TwoStageMode"), v => store.triggerSlots[device].L2Output.TwoStageMode = v);
            context.ReadInt(context.Xdoc, path("L2HipFireTime"), 0, 5000, v => store.triggerSlots[device].L2Output.hipFireMS = v);
            context.ReadEnum<TriggerEffects>(context.Xdoc, path("L2TriggerEffect"), v => store.triggerSlots[device].L2Output.TriggerEffect = v);

            context.ReadLegacy(path("R2OutputCurveCustom"), t => store.triggerSlots[device].R2Bezier.CustomDefinition = t);
            context.ReadLegacy(path("R2OutputCurveMode"), t => store.triggerSlots[device].ApplyR2CurveMode(axisOutputCurveId(t)), () => store.triggerSlots[device].ApplyR2CurveMode(0));

            context.ReadEnum<TwoStageTriggerMode>(context.Xdoc, path("R2TwoStageMode"), v => store.triggerSlots[device].R2Output.TwoStageMode = v);
            context.ReadInt(context.Xdoc, path("R2HipFireTime"), 0, 5000, v => store.triggerSlots[device].R2Output.hipFireMS = v);
            context.ReadEnum<TriggerEffects>(context.Xdoc, path("R2TriggerEffect"), v => store.triggerSlots[device].R2Output.TriggerEffect = v);

            context.ReadLegacy(path("SXOutputCurveCustom"), t => store.sixAxisSlots[device].SXCurve.CustomDefinition = t);
            context.ReadLegacy(path("SXOutputCurveMode"), t => store.sixAxisSlots[device].ApplySXCurveMode(axisOutputCurveId(t)), () => store.sixAxisSlots[device].ApplySXCurveMode(0));

            context.ReadLegacy(path("SZOutputCurveCustom"), t => store.sixAxisSlots[device].SZCurve.CustomDefinition = t);
            context.ReadLegacy(path("SZOutputCurveMode"), t => store.sixAxisSlots[device].ApplySZCurveMode(axisOutputCurveId(t)), () => store.sixAxisSlots[device].ApplySZCurveMode(0));
        }
    }
}
