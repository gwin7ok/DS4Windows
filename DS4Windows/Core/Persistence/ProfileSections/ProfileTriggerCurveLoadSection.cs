#nullable enable

using System;
using System.Xml;
using DS4Windows.Core.Settings;
using DS4Windows.Core.Model;

namespace DS4Windows.Core.Persistence.ProfileSections
{
    /// <summary>
    /// DualSense 固有の設定・トリガーの二段階・曲線・SX/SZ の曲線（DualSenseControllerSettings〜SZOutputCurveMode）の読込。
    /// <c>LegacyProfileXmlSerializer.LoadProfile</c> から移したもの。移動のみで、要素の順序・既定値・例外の扱いは変えない。
    /// </summary>
    internal sealed class ProfileTriggerCurveLoadSection : IProfileLoadSection
    {
        public void Load(ProfileLoadContext context)
        {
            BackingStore store = context.Store;
            XmlDocument m_Xdoc = context.Xdoc;
            string rootname = context.RootName;
            int device = context.Device;
            bool missingSetting = context.MissingSetting;
            XmlNode? Item;

            int axisOutputCurveId(string name)
            {
                return store.StickOutputCurveId(name);
            }

            // Start of DualSense specific profile load
            //
            XmlNode? xmlDualSenseControllerSettingsElement =
                m_Xdoc.SelectSingleNode("/" + rootname + "/DualSenseControllerSettings");
            bool dSControllerSettingsGroup = xmlDualSenseControllerSettingsElement != null;
            if (dSControllerSettingsGroup)
            {
                XmlNode? xmlDSRumbleGroupElement =
                    xmlDualSenseControllerSettingsElement.Required().SelectSingleNode("RumbleSettings");
                bool dSRumbleGroup = xmlDSRumbleGroupElement != null;

                if (dSRumbleGroup)
                {
                    try
                    {
                        Item = xmlDSRumbleGroupElement.Required().SelectSingleNode("EmulationMode");
                        RumbleEmulationMode.TryParse(Item.Required().InnerText, out RumbleEmulationMode temp);
                        store.dualSenseSlots[device].RumbleEmulationMode = temp;
                    }
                    catch { missingSetting = true; }

                    try
                    {
                        Item = xmlDSRumbleGroupElement.Required().SelectSingleNode("EnableGenericRumbleRescale");
                        bool.TryParse(Item.Required().InnerText, out bool temp);
                        store.dualSenseSlots[device].UseGenericRumbleRescale = temp;
                    }
                    catch { missingSetting = true; }

                    try
                    {
                        Item = xmlDSRumbleGroupElement.Required().SelectSingleNode("HapticPowerLevel");
                        byte.TryParse(Item.Required().InnerText, out byte temp);
                        store.dualSenseSlots[device].HapticPowerLevel = temp;
                    }
                    catch { missingSetting = true; }

                }
                else
                {
                    missingSetting = true;
                }
            }
            else
            {
                missingSetting = true;
            }
            //
            // End of DualSense specific profile load

            try { Item = m_Xdoc.SelectSingleNode("/" + rootname + "/L2OutputCurveCustom"); store.triggerSlots[device].L2Bezier.CustomDefinition = Item.Required().InnerText; }
            catch { missingSetting = true; }
            try { Item = m_Xdoc.SelectSingleNode("/" + rootname + "/L2OutputCurveMode"); store.triggerSlots[device].ApplyL2CurveMode(axisOutputCurveId(Item.Required().InnerText)); }
            catch { store.triggerSlots[device].ApplyL2CurveMode(0); missingSetting = true; }

            try
            {
                Item = m_Xdoc.SelectSingleNode("/" + rootname + "/L2TwoStageMode");
                if (Enum.TryParse(Item?.InnerText, out TwoStageTriggerMode tempMode))
                {
                    store.triggerSlots[device].L2Output.TwoStageMode = tempMode;
                }
            }
            catch { }


            try
            {
                Item = m_Xdoc.SelectSingleNode("/" + rootname + "/L2HipFireTime");
                if (int.TryParse(Item?.InnerText, out int temp))
                {
                    store.triggerSlots[device].L2Output.hipFireMS = Math.Clamp(temp, 0, 5000);
                }
            }
            catch { }

            try
            {
                Item = m_Xdoc.SelectSingleNode("/" + rootname + "/L2TriggerEffect");
                if (Enum.TryParse(Item?.InnerText, out DS4Windows.Core.Model.TriggerEffects tempMode))
                {
                    store.triggerSlots[device].L2Output.TriggerEffect = tempMode;
                }
            }
            catch { }

            try { Item = m_Xdoc.SelectSingleNode("/" + rootname + "/R2OutputCurveCustom"); store.triggerSlots[device].R2Bezier.CustomDefinition = Item.Required().InnerText; }
            catch { missingSetting = true; }
            try { Item = m_Xdoc.SelectSingleNode("/" + rootname + "/R2OutputCurveMode"); store.triggerSlots[device].ApplyR2CurveMode(axisOutputCurveId(Item.Required().InnerText)); }
            catch { store.triggerSlots[device].ApplyR2CurveMode(0); missingSetting = true; }

            try
            {
                Item = m_Xdoc.SelectSingleNode("/" + rootname + "/R2TwoStageMode");
                if (Enum.TryParse(Item?.InnerText, out TwoStageTriggerMode tempMode))
                {
                    store.triggerSlots[device].R2Output.TwoStageMode = tempMode;
                }
            }
            catch { }

            try
            {
                Item = m_Xdoc.SelectSingleNode("/" + rootname + "/R2HipFireTime");
                if (int.TryParse(Item?.InnerText, out int temp))
                {
                    store.triggerSlots[device].R2Output.hipFireMS = Math.Clamp(temp, 0, 5000);
                }
            }
            catch { }

            try
            {
                Item = m_Xdoc.SelectSingleNode("/" + rootname + "/R2TriggerEffect");
                if (Enum.TryParse(Item?.InnerText, out DS4Windows.Core.Model.TriggerEffects tempMode))
                {
                    store.triggerSlots[device].R2Output.TriggerEffect = tempMode;
                }
            }
            catch { }

            try { Item = m_Xdoc.SelectSingleNode("/" + rootname + "/SXOutputCurveCustom"); store.sixAxisSlots[device].SXCurve.CustomDefinition = Item.Required().InnerText; }
            catch { missingSetting = true; }
            try { Item = m_Xdoc.SelectSingleNode("/" + rootname + "/SXOutputCurveMode"); store.sixAxisSlots[device].ApplySXCurveMode(axisOutputCurveId(Item.Required().InnerText)); }
            catch { store.sixAxisSlots[device].ApplySXCurveMode(0); missingSetting = true; }

            try { Item = m_Xdoc.SelectSingleNode("/" + rootname + "/SZOutputCurveCustom"); store.sixAxisSlots[device].SZCurve.CustomDefinition = Item.Required().InnerText; }
            catch { missingSetting = true; }
            try { Item = m_Xdoc.SelectSingleNode("/" + rootname + "/SZOutputCurveMode"); store.sixAxisSlots[device].ApplySZCurveMode(axisOutputCurveId(Item.Required().InnerText)); }
            catch { store.sixAxisSlots[device].ApplySZCurveMode(0); missingSetting = true; }

            context.MissingSetting = missingSetting;
        }
    }
}
