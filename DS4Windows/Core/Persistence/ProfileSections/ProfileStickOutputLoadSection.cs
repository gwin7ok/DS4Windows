#nullable enable

using System;
using System.Xml;
using DS4Windows.Core.Settings;

namespace DS4Windows.Core.Persistence.ProfileSections
{
    /// <summary>
    /// BT のポーリング・スティックの出力（曲線、四角形、アンチスナップバック、出力モード、出力設定）の読込。
    /// <c>LegacyProfileXmlSerializer.LoadProfile</c> から移したもの。移動のみで、要素の順序・既定値・例外の扱いは変えない。
    /// </summary>
    internal sealed class ProfileStickOutputLoadSection : IProfileLoadSection
    {
        public void Load(ProfileLoadContext context)
        {
            BackingStore store = context.Store;
            XmlDocument m_Xdoc = context.Xdoc;
            string rootname = context.RootName;
            int device = context.Device;
            bool missingSetting = context.MissingSetting;
            XmlNode? Item;

            try
            {
                Item = m_Xdoc.SelectSingleNode("/" + rootname + "/BTPollRate");
                int temp = 0;
                int.TryParse(Item.Required().InnerText, out temp);
                store.basicSlots[device].BTPollRate = (temp >= 0 && temp <= 16) ? temp : 4;
            }
            catch { store.basicSlots[device].BTPollRate = 4; missingSetting = true; }

            // Note! xxOutputCurveCustom property needs to be read before xxOutputCurveMode property in case the curveMode is value 6
            try { Item = m_Xdoc.SelectSingleNode("/" + rootname + "/LSOutputCurveCustom"); store.stickSlots[device].LsBezier.CustomDefinition = Item.Required().InnerText; }
            catch { missingSetting = true; }
            try { Item = m_Xdoc.SelectSingleNode("/" + rootname + "/LSOutputCurveMode"); store.stickSlots[device].ApplyLSCurveMode(store.StickOutputCurveId(Item.Required().InnerText)); }
            catch { store.stickSlots[device].ApplyLSCurveMode(0); missingSetting = true; }

            try { Item = m_Xdoc.SelectSingleNode("/" + rootname + "/RSOutputCurveCustom"); store.stickSlots[device].RsBezier.CustomDefinition = Item.Required().InnerText; }
            catch { missingSetting = true; }
            try { Item = m_Xdoc.SelectSingleNode("/" + rootname + "/RSOutputCurveMode"); store.stickSlots[device].ApplyRSCurveMode(store.StickOutputCurveId(Item.Required().InnerText)); }
            catch { store.stickSlots[device].ApplyRSCurveMode(0); missingSetting = true; }

            try { Item = m_Xdoc.SelectSingleNode("/" + rootname + "/LSSquareStick"); bool.TryParse(Item.Required().InnerText, out store.stickSlots[device].SquStick.lsMode); }
            catch { store.stickSlots[device].SquStick.lsMode = false; missingSetting = true; }

            try { Item = m_Xdoc.SelectSingleNode("/" + rootname + "/SquareStickRoundness"); double.TryParse(Item.Required().InnerText, out store.stickSlots[device].SquStick.lsRoundness); }
            catch { store.stickSlots[device].SquStick.lsRoundness = 5.0; missingSetting = true; }
            try { Item = m_Xdoc.SelectSingleNode("/" + rootname + "/SquareRStickRoundness"); double.TryParse(Item.Required().InnerText, out store.stickSlots[device].SquStick.rsRoundness); }
            catch { store.stickSlots[device].SquStick.rsRoundness = 5.0; missingSetting = true; }

            try { Item = m_Xdoc.SelectSingleNode("/" + rootname + "/RSSquareStick"); bool.TryParse(Item.Required().InnerText, out store.stickSlots[device].SquStick.rsMode); }
            catch { store.stickSlots[device].SquStick.rsMode = false; missingSetting = true; }


            try { Item = m_Xdoc.SelectSingleNode("/" + rootname + "/LSAntiSnapback"); bool.TryParse(Item.Required().InnerText, out store.stickSlots[device].LSAntiSnapback.enabled); }
            catch { store.stickSlots[device].LSAntiSnapback.enabled = StickAntiSnapbackInfo.DEFAULT_ENABLED; missingSetting = true; }
            try { Item = m_Xdoc.SelectSingleNode("/" + rootname + "/RSAntiSnapback"); bool.TryParse(Item.Required().InnerText, out store.stickSlots[device].RSAntiSnapback.enabled); }
            catch { store.stickSlots[device].RSAntiSnapback.enabled = StickAntiSnapbackInfo.DEFAULT_ENABLED; missingSetting = true; }

            try { Item = m_Xdoc.SelectSingleNode("/" + rootname + "/LSAntiSnapbackDelta"); double.TryParse(Item.Required().InnerText, out store.stickSlots[device].LSAntiSnapback.delta); }
            catch { store.stickSlots[device].LSAntiSnapback.delta = StickAntiSnapbackInfo.DEFAULT_DELTA; missingSetting = true; }
            try { Item = m_Xdoc.SelectSingleNode("/" + rootname + "/RSAntiSnapbackDelta"); double.TryParse(Item.Required().InnerText, out store.stickSlots[device].RSAntiSnapback.delta); }
            catch { store.stickSlots[device].RSAntiSnapback.delta = StickAntiSnapbackInfo.DEFAULT_DELTA; missingSetting = true; }

            try { Item = m_Xdoc.SelectSingleNode("/" + rootname + "/LSAntiSnapbackTimeout"); int.TryParse(Item.Required().InnerText, out store.stickSlots[device].LSAntiSnapback.timeout); }
            catch { store.stickSlots[device].LSAntiSnapback.timeout = StickAntiSnapbackInfo.DEFAULT_TIMEOUT; missingSetting = true; }
            try { Item = m_Xdoc.SelectSingleNode("/" + rootname + "/RSAntiSnapbackTimeout"); int.TryParse(Item.Required().InnerText, out store.stickSlots[device].RSAntiSnapback.timeout); }
            catch { store.stickSlots[device].RSAntiSnapback.timeout = StickAntiSnapbackInfo.DEFAULT_TIMEOUT; missingSetting = true; }

            try { Item = m_Xdoc.SelectSingleNode("/" + rootname + "/LSOutputMode"); Enum.TryParse(Item.Required().InnerText, out store.stickSlots[device].LSOutput.mode); }
            catch { missingSetting = true; }

            try { Item = m_Xdoc.SelectSingleNode("/" + rootname + "/RSOutputMode"); Enum.TryParse(Item.Required().InnerText, out store.stickSlots[device].RSOutput.mode); }
            catch { missingSetting = true; }

            XmlNode? xmlLSOutputSettingsElement =
                m_Xdoc.SelectSingleNode("/" + rootname + "/LSOutputSettings");
            bool lsOutputGroup = xmlLSOutputSettingsElement != null;
            if (lsOutputGroup)
            {
                bool flickStickLSGroup = false;
                XmlNode? xmlFlickStickLSElement =
                    xmlLSOutputSettingsElement.Required().SelectSingleNode("FlickStickSettings");
                flickStickLSGroup = xmlFlickStickLSElement != null;

                if (flickStickLSGroup)
                {
                    try
                    {
                        Item = xmlFlickStickLSElement.Required().SelectSingleNode("RealWorldCalibration");
                        double.TryParse(Item.Required().InnerText, out double temp);
                        store.stickSlots[device].LSOutput.outputSettings.flickSettings.realWorldCalibration = temp;
                    }
                    catch { missingSetting = true; }

                    try
                    {
                        Item = xmlFlickStickLSElement.Required().SelectSingleNode("FlickThreshold");
                        double.TryParse(Item.Required().InnerText, out double temp);
                        store.stickSlots[device].LSOutput.outputSettings.flickSettings.flickThreshold = temp;
                    }
                    catch { missingSetting = true; }

                    try
                    {
                        Item = xmlFlickStickLSElement.Required().SelectSingleNode("FlickTime");
                        double.TryParse(Item.Required().InnerText, out double temp);
                        store.stickSlots[device].LSOutput.outputSettings.flickSettings.flickTime = temp;
                    }
                    catch { missingSetting = true; }

                    try
                    {
                        Item = xmlFlickStickLSElement.Required().SelectSingleNode("MinAngleThreshold");
                        double.TryParse(Item.Required().InnerText, out double temp);
                        store.stickSlots[device].LSOutput.outputSettings.flickSettings.minAngleThreshold = temp;
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

            XmlNode? xmlRSOutputSettingsElement =
                m_Xdoc.SelectSingleNode("/" + rootname + "/RSOutputSettings");
            bool rsOutputGroup = xmlRSOutputSettingsElement != null;
            if (rsOutputGroup)
            {
                bool flickStickRSGroup = false;
                XmlNode? xmlFlickStickRSElement =
                    xmlRSOutputSettingsElement.Required().SelectSingleNode("FlickStickSettings");
                flickStickRSGroup = xmlFlickStickRSElement != null;

                if (flickStickRSGroup)
                {
                    try
                    {
                        Item = xmlFlickStickRSElement.Required().SelectSingleNode("RealWorldCalibration");
                        double.TryParse(Item.Required().InnerText, out double temp);
                        store.stickSlots[device].RSOutput.outputSettings.flickSettings.realWorldCalibration = temp;
                    }
                    catch { missingSetting = true; }

                    try
                    {
                        Item = xmlFlickStickRSElement.Required().SelectSingleNode("FlickThreshold");
                        double.TryParse(Item.Required().InnerText, out double temp);
                        store.stickSlots[device].RSOutput.outputSettings.flickSettings.flickThreshold = temp;
                    }
                    catch { missingSetting = true; }

                    try
                    {
                        Item = xmlFlickStickRSElement.Required().SelectSingleNode("FlickTime");
                        double.TryParse(Item.Required().InnerText, out double temp);
                        store.stickSlots[device].RSOutput.outputSettings.flickSettings.flickTime = temp;
                    }
                    catch { missingSetting = true; }

                    try
                    {
                        Item = xmlFlickStickRSElement.Required().SelectSingleNode("MinAngleThreshold");
                        double.TryParse(Item.Required().InnerText, out double temp);
                        store.stickSlots[device].RSOutput.outputSettings.flickSettings.minAngleThreshold = temp;
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

            context.MissingSetting = missingSetting;
        }
    }
}
