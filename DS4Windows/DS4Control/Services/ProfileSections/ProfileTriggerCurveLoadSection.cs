using DS4Windows.DS4Control;
using DS4Windows.InputDevices;
using DS4Windows.StickModifiers;
using DS4WinWPF.DS4Control.DTOXml;
using DS4WinWPF.DS4Forms.ViewModels;
using Sensorit.Base;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http.Json;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Xml;
using System.Xml.Serialization;
using DS4WinWPF;
using DS4WinWPF.ApiDTO;
using WpfScreenHelper;
using static DS4Windows.Mouse;
using static DS4Windows.Util;

namespace DS4Windows.Services
{
    /// <summary>
    /// Phase8-Step4b W5（Phase8-Step4b-Plan.md §3.2）: DualSense 固有の設定・トリガーの二段階・曲線・SX/SZ の曲線（DualSenseControllerSettings〜SZOutputCurveMode）の読込。
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
            XmlNode Item;

            int axisOutputCurveId(string name)
            {
                return store.stickOutputCurveId(name);
            }

            // Start of DualSense specific profile load
            //
            XmlNode xmlDualSenseControllerSettingsElement =
                m_Xdoc.SelectSingleNode("/" + rootname + "/DualSenseControllerSettings");
            bool dSControllerSettingsGroup = xmlDualSenseControllerSettingsElement != null;
            if (dSControllerSettingsGroup)
            {
                XmlNode xmlDSRumbleGroupElement =
                    xmlDualSenseControllerSettingsElement.SelectSingleNode("RumbleSettings");
                bool dSRumbleGroup = xmlDSRumbleGroupElement != null;

                if (dSRumbleGroup)
                {
                    try
                    {
                        Item = xmlDSRumbleGroupElement.SelectSingleNode("EmulationMode");
                        DualSenseDevice.RumbleEmulationMode.TryParse(Item.InnerText, out DualSenseDevice.RumbleEmulationMode temp);
                        store.dualSenseRumbleEmulationMode[device] = temp;
                    }
                    catch { missingSetting = true; }

                    try
                    {
                        Item = xmlDSRumbleGroupElement.SelectSingleNode("EnableGenericRumbleRescale");
                        bool.TryParse(Item.InnerText, out bool temp);
                        store.useGenericRumbleRescaleForDualSenses[device] = temp;
                    }
                    catch { missingSetting = true; }

                    try
                    {
                        Item = xmlDSRumbleGroupElement.SelectSingleNode("HapticPowerLevel");
                        byte.TryParse(Item.InnerText, out byte temp);
                        store.dualSenseHapticPowerLevel[device] = temp;
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

            try { Item = m_Xdoc.SelectSingleNode("/" + rootname + "/L2OutputCurveCustom"); store.l2OutBezierCurveObj[device].CustomDefinition = Item.InnerText; }
            catch { missingSetting = true; }
            try { Item = m_Xdoc.SelectSingleNode("/" + rootname + "/L2OutputCurveMode"); store.setL2OutCurveMode(device, axisOutputCurveId(Item.InnerText)); }
            catch { store.setL2OutCurveMode(device, 0); missingSetting = true; }

            try
            {
                Item = m_Xdoc.SelectSingleNode("/" + rootname + "/L2TwoStageMode");
                if (Enum.TryParse(Item?.InnerText, out TwoStageTriggerMode tempMode))
                {
                    store.l2OutputSettings[device].TwoStageMode = tempMode;
                }
            }
            catch { }


            try
            {
                Item = m_Xdoc.SelectSingleNode("/" + rootname + "/L2HipFireTime");
                if (int.TryParse(Item?.InnerText, out int temp))
                {
                    store.l2OutputSettings[device].hipFireMS = Math.Clamp(temp, 0, 5000);
                }
            }
            catch { }

            try
            {
                Item = m_Xdoc.SelectSingleNode("/" + rootname + "/L2TriggerEffect");
                if (Enum.TryParse(Item?.InnerText, out InputDevices.TriggerEffects tempMode))
                {
                    store.l2OutputSettings[device].TriggerEffect = tempMode;
                }
            }
            catch { }

            try { Item = m_Xdoc.SelectSingleNode("/" + rootname + "/R2OutputCurveCustom"); store.r2OutBezierCurveObj[device].CustomDefinition = Item.InnerText; }
            catch { missingSetting = true; }
            try { Item = m_Xdoc.SelectSingleNode("/" + rootname + "/R2OutputCurveMode"); store.setR2OutCurveMode(device, axisOutputCurveId(Item.InnerText)); }
            catch { store.setR2OutCurveMode(device, 0); missingSetting = true; }

            try
            {
                Item = m_Xdoc.SelectSingleNode("/" + rootname + "/R2TwoStageMode");
                if (Enum.TryParse(Item?.InnerText, out TwoStageTriggerMode tempMode))
                {
                    store.r2OutputSettings[device].TwoStageMode = tempMode;
                }
            }
            catch { }

            try
            {
                Item = m_Xdoc.SelectSingleNode("/" + rootname + "/R2HipFireTime");
                if (int.TryParse(Item?.InnerText, out int temp))
                {
                    store.r2OutputSettings[device].hipFireMS = Math.Clamp(temp, 0, 5000);
                }
            }
            catch { }

            try
            {
                Item = m_Xdoc.SelectSingleNode("/" + rootname + "/R2TriggerEffect");
                if (Enum.TryParse(Item?.InnerText, out InputDevices.TriggerEffects tempMode))
                {
                    store.r2OutputSettings[device].TriggerEffect = tempMode;
                }
            }
            catch { }

            try { Item = m_Xdoc.SelectSingleNode("/" + rootname + "/SXOutputCurveCustom"); store.sxOutBezierCurveObj[device].CustomDefinition = Item.InnerText; }
            catch { missingSetting = true; }
            try { Item = m_Xdoc.SelectSingleNode("/" + rootname + "/SXOutputCurveMode"); store.setSXOutCurveMode(device, axisOutputCurveId(Item.InnerText)); }
            catch { store.setSXOutCurveMode(device, 0); missingSetting = true; }

            try { Item = m_Xdoc.SelectSingleNode("/" + rootname + "/SZOutputCurveCustom"); store.szOutBezierCurveObj[device].CustomDefinition = Item.InnerText; }
            catch { missingSetting = true; }
            try { Item = m_Xdoc.SelectSingleNode("/" + rootname + "/SZOutputCurveMode"); store.setSZOutCurveMode(device, axisOutputCurveId(Item.InnerText)); }
            catch { store.setSZOutCurveMode(device, 0); missingSetting = true; }

            context.MissingSetting = missingSetting;
        }
    }
}
