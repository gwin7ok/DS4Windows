using System;
using System.Xml;
using DS4Windows.Core.Settings;

namespace DS4Windows.Core.Persistence.ProfileSections
{
    /// <summary>
    /// スティックの軸別の不感帯・デルタ加速（LSAxialDeadOptions〜RSDeltaAccelSettings）の読込。右スティックのデルタ加速の条件は、旧コードのまま左スティックの要素を見る（挙動を変えない）。
    /// <c>LegacyProfileXmlSerializer.LoadProfile</c> から移したもの。移動のみで、要素の順序・既定値・例外の扱いは変えない。
    /// </summary>
    internal sealed class ProfileStickAxialLoadSection : IProfileLoadSection
    {
        public void Load(ProfileLoadContext context)
        {
            BackingStore store = context.Store;
            XmlDocument m_Xdoc = context.Xdoc;
            string rootname = context.RootName;
            int device = context.Device;
            bool missingSetting = context.MissingSetting;

            bool lsAxialDeadGroup = false;
            XmlNode? lsAxialDeadElement =
                m_Xdoc.SelectSingleNode("/" + rootname + "/LSAxialDeadOptions");
            lsAxialDeadGroup = lsAxialDeadElement != null;

            if (lsAxialDeadGroup)
            {
                context.ReadInt(lsAxialDeadElement, "DeadZoneX", 0, 127, v => store.stickSlots[device].LSMod.xAxisDeadInfo.deadZone = v);

                context.ReadInt(lsAxialDeadElement, "DeadZoneY", 0, 127, v => store.stickSlots[device].LSMod.yAxisDeadInfo.deadZone = v);

                context.ReadInt(lsAxialDeadElement, "MaxZoneX", 0, 100, v => store.stickSlots[device].LSMod.xAxisDeadInfo.maxZone = v);

                context.ReadInt(lsAxialDeadElement, "MaxZoneY", 0, 100, v => store.stickSlots[device].LSMod.yAxisDeadInfo.maxZone = v);

                context.ReadInt(lsAxialDeadElement, "AntiDeadZoneX", 0, 100, v => store.stickSlots[device].LSMod.xAxisDeadInfo.antiDeadZone = v);

                context.ReadInt(lsAxialDeadElement, "AntiDeadZoneY", 0, 100, v => store.stickSlots[device].LSMod.yAxisDeadInfo.antiDeadZone = v);

                context.ReadDouble(lsAxialDeadElement, "MaxOutputX", 0.0, 100.0, v => store.stickSlots[device].LSMod.xAxisDeadInfo.maxOutput = v);

                context.ReadDouble(lsAxialDeadElement, "MaxOutputY", 0.0, 100.0, v => store.stickSlots[device].LSMod.yAxisDeadInfo.maxOutput = v);
            }

            XmlNode? lsDeltaAccelElement =
                m_Xdoc.SelectSingleNode($"/{rootname}/LSDeltaAccelSettings");
            if (lsDeltaAccelElement != null)
            {
                context.ReadBool(lsDeltaAccelElement, "Enabled", v => store.stickSlots[device].LSOutput.outputSettings.controlSettings.deltaAccelSettings.enabled = v);

                context.ReadDouble(lsDeltaAccelElement, "Multiplier", 0.0, 10.0, v => store.stickSlots[device].LSOutput.outputSettings.controlSettings.deltaAccelSettings.multiplier = v);

                context.ReadDouble(lsDeltaAccelElement, "MaxTravel", 0.0, 1.0, v => store.stickSlots[device].LSOutput.outputSettings.controlSettings.deltaAccelSettings.maxTravel = v);

                context.ReadDouble(lsDeltaAccelElement, "MinTravel", 0.0, 1.0, v => store.stickSlots[device].LSOutput.outputSettings.controlSettings.deltaAccelSettings.minTravel = v);

                context.ReadDouble(lsDeltaAccelElement, "EasingDuration", 0.0, 600.0, v => store.stickSlots[device].LSOutput.outputSettings.controlSettings.deltaAccelSettings.easingDuration = v);

                context.ReadDouble(lsDeltaAccelElement, "MinFactor", 1.0, 10.0, v => store.stickSlots[device].LSOutput.outputSettings.controlSettings.deltaAccelSettings.minfactor = v);
            }

            context.ReadEnum<StickDeadZoneInfo.DeadZoneType>(m_Xdoc, "/" + rootname + "/RSDeadZoneType", v => store.stickSlots[device].RSMod.deadzoneType = v);

            bool rsAxialDeadGroup = false;
            XmlNode? rsAxialDeadElement =
                m_Xdoc.SelectSingleNode("/" + rootname + "/RSAxialDeadOptions");
            rsAxialDeadGroup = rsAxialDeadElement != null;

            if (rsAxialDeadGroup)
            {
                context.ReadInt(rsAxialDeadElement, "DeadZoneX", 0, 127, v => store.stickSlots[device].RSMod.xAxisDeadInfo.deadZone = v);

                context.ReadInt(rsAxialDeadElement, "DeadZoneY", 0, 127, v => store.stickSlots[device].RSMod.yAxisDeadInfo.deadZone = v);

                context.ReadInt(rsAxialDeadElement, "MaxZoneX", 0, 100, v => store.stickSlots[device].RSMod.xAxisDeadInfo.maxZone = v);

                context.ReadInt(rsAxialDeadElement, "MaxZoneY", 0, 100, v => store.stickSlots[device].RSMod.yAxisDeadInfo.maxZone = v);

                context.ReadInt(rsAxialDeadElement, "AntiDeadZoneX", 0, 100, v => store.stickSlots[device].RSMod.xAxisDeadInfo.antiDeadZone = v);

                context.ReadInt(rsAxialDeadElement, "AntiDeadZoneY", 0, 100, v => store.stickSlots[device].RSMod.yAxisDeadInfo.antiDeadZone = v);

                context.ReadDouble(rsAxialDeadElement, "MaxOutputX", 0.0, 100.0, v => store.stickSlots[device].RSMod.xAxisDeadInfo.maxOutput = v);

                context.ReadDouble(rsAxialDeadElement, "MaxOutputY", 0.0, 100.0, v => store.stickSlots[device].RSMod.yAxisDeadInfo.maxOutput = v);
            }

            XmlNode? rsDeltaAccelElement =
                m_Xdoc.SelectSingleNode($"/{rootname}/RSDeltaAccelSettings");
            if (lsDeltaAccelElement != null)
            {
                context.ReadBool(rsDeltaAccelElement, "Enabled", v => store.stickSlots[device].RSOutput.outputSettings.controlSettings.deltaAccelSettings.enabled = v);

                context.ReadDouble(rsDeltaAccelElement, "Multiplier", 0.0, 10.0, v => store.stickSlots[device].RSOutput.outputSettings.controlSettings.deltaAccelSettings.multiplier = v);

                context.ReadDouble(rsDeltaAccelElement, "MaxTravel", 0.0, 1.0, v => store.stickSlots[device].RSOutput.outputSettings.controlSettings.deltaAccelSettings.maxTravel = v);

                context.ReadDouble(rsDeltaAccelElement, "MinTravel", 0.0, 1.0, v => store.stickSlots[device].RSOutput.outputSettings.controlSettings.deltaAccelSettings.minTravel = v);

                context.ReadDouble(rsDeltaAccelElement, "EasingDuration", 0.0, 600.0, v => store.stickSlots[device].RSOutput.outputSettings.controlSettings.deltaAccelSettings.easingDuration = v);

                context.ReadDouble(rsDeltaAccelElement, "MinFactor", 1.0, 10.0, v => store.stickSlots[device].RSOutput.outputSettings.controlSettings.deltaAccelSettings.minfactor = v);
            }

            context.MissingSetting = missingSetting;
        }
    }
}
