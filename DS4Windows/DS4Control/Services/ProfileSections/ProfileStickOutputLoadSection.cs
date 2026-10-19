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
    /// Phase8-Step4b W5（Phase8-Step4b-Plan.md §3.2）: BT のポーリング・スティックの出力（曲線、四角形、アンチスナップバック、出力モード、出力設定）の読込。
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
            XmlNode Item;

            try
            {
                Item = m_Xdoc.SelectSingleNode("/" + rootname + "/BTPollRate");
                int temp = 0;
                int.TryParse(Item.InnerText, out temp);
                store.btPollRate[device] = (temp >= 0 && temp <= 16) ? temp : 4;
            }
            catch { store.btPollRate[device] = 4; missingSetting = true; }

            // Note! xxOutputCurveCustom property needs to be read before xxOutputCurveMode property in case the curveMode is value 6
            try { Item = m_Xdoc.SelectSingleNode("/" + rootname + "/LSOutputCurveCustom"); store.lsOutBezierCurveObj[device].CustomDefinition = Item.InnerText; }
            catch { missingSetting = true; }
            try { Item = m_Xdoc.SelectSingleNode("/" + rootname + "/LSOutputCurveMode"); store.setLsOutCurveMode(device, store.stickOutputCurveId(Item.InnerText)); }
            catch { store.setLsOutCurveMode(device, 0); missingSetting = true; }

            try { Item = m_Xdoc.SelectSingleNode("/" + rootname + "/RSOutputCurveCustom"); store.rsOutBezierCurveObj[device].CustomDefinition = Item.InnerText; }
            catch { missingSetting = true; }
            try { Item = m_Xdoc.SelectSingleNode("/" + rootname + "/RSOutputCurveMode"); store.setRsOutCurveMode(device, store.stickOutputCurveId(Item.InnerText)); }
            catch { store.setRsOutCurveMode(device, 0); missingSetting = true; }

            try { Item = m_Xdoc.SelectSingleNode("/" + rootname + "/LSSquareStick"); bool.TryParse(Item.InnerText, out store.squStickInfo[device].lsMode); }
            catch { store.squStickInfo[device].lsMode = false; missingSetting = true; }

            try { Item = m_Xdoc.SelectSingleNode("/" + rootname + "/SquareStickRoundness"); double.TryParse(Item.InnerText, out store.squStickInfo[device].lsRoundness); }
            catch { store.squStickInfo[device].lsRoundness = 5.0; missingSetting = true; }
            try { Item = m_Xdoc.SelectSingleNode("/" + rootname + "/SquareRStickRoundness"); double.TryParse(Item.InnerText, out store.squStickInfo[device].rsRoundness); }
            catch { store.squStickInfo[device].rsRoundness = 5.0; missingSetting = true; }

            try { Item = m_Xdoc.SelectSingleNode("/" + rootname + "/RSSquareStick"); bool.TryParse(Item.InnerText, out store.squStickInfo[device].rsMode); }
            catch { store.squStickInfo[device].rsMode = false; missingSetting = true; }


            try { Item = m_Xdoc.SelectSingleNode("/" + rootname + "/LSAntiSnapback"); bool.TryParse(Item.InnerText, out store.lsAntiSnapbackInfo[device].enabled); }
            catch { store.lsAntiSnapbackInfo[device].enabled = StickAntiSnapbackInfo.DEFAULT_ENABLED; missingSetting = true; }
            try { Item = m_Xdoc.SelectSingleNode("/" + rootname + "/RSAntiSnapback"); bool.TryParse(Item.InnerText, out store.rsAntiSnapbackInfo[device].enabled); }
            catch { store.rsAntiSnapbackInfo[device].enabled = StickAntiSnapbackInfo.DEFAULT_ENABLED; missingSetting = true; }

            try { Item = m_Xdoc.SelectSingleNode("/" + rootname + "/LSAntiSnapbackDelta"); double.TryParse(Item.InnerText, out store.lsAntiSnapbackInfo[device].delta); }
            catch { store.lsAntiSnapbackInfo[device].delta = StickAntiSnapbackInfo.DEFAULT_DELTA; missingSetting = true; }
            try { Item = m_Xdoc.SelectSingleNode("/" + rootname + "/RSAntiSnapbackDelta"); double.TryParse(Item.InnerText, out store.rsAntiSnapbackInfo[device].delta); }
            catch { store.rsAntiSnapbackInfo[device].delta = StickAntiSnapbackInfo.DEFAULT_DELTA; missingSetting = true; }

            try { Item = m_Xdoc.SelectSingleNode("/" + rootname + "/LSAntiSnapbackTimeout"); int.TryParse(Item.InnerText, out store.lsAntiSnapbackInfo[device].timeout); }
            catch { store.lsAntiSnapbackInfo[device].timeout = StickAntiSnapbackInfo.DEFAULT_TIMEOUT; missingSetting = true; }
            try { Item = m_Xdoc.SelectSingleNode("/" + rootname + "/RSAntiSnapbackTimeout"); int.TryParse(Item.InnerText, out store.rsAntiSnapbackInfo[device].timeout); }
            catch { store.rsAntiSnapbackInfo[device].timeout = StickAntiSnapbackInfo.DEFAULT_TIMEOUT; missingSetting = true; }

            try { Item = m_Xdoc.SelectSingleNode("/" + rootname + "/LSOutputMode"); Enum.TryParse(Item.InnerText, out store.lsOutputSettings[device].mode); }
            catch { missingSetting = true; }

            try { Item = m_Xdoc.SelectSingleNode("/" + rootname + "/RSOutputMode"); Enum.TryParse(Item.InnerText, out store.rsOutputSettings[device].mode); }
            catch { missingSetting = true; }

            XmlNode xmlLSOutputSettingsElement =
                m_Xdoc.SelectSingleNode("/" + rootname + "/LSOutputSettings");
            bool lsOutputGroup = xmlLSOutputSettingsElement != null;
            if (lsOutputGroup)
            {
                bool flickStickLSGroup = false;
                XmlNode xmlFlickStickLSElement =
                    xmlLSOutputSettingsElement.SelectSingleNode("FlickStickSettings");
                flickStickLSGroup = xmlFlickStickLSElement != null;

                if (flickStickLSGroup)
                {
                    try
                    {
                        Item = xmlFlickStickLSElement.SelectSingleNode("RealWorldCalibration");
                        double.TryParse(Item.InnerText, out double temp);
                        store.lsOutputSettings[device].outputSettings.flickSettings.realWorldCalibration = temp;
                    }
                    catch { missingSetting = true; }

                    try
                    {
                        Item = xmlFlickStickLSElement.SelectSingleNode("FlickThreshold");
                        double.TryParse(Item.InnerText, out double temp);
                        store.lsOutputSettings[device].outputSettings.flickSettings.flickThreshold = temp;
                    }
                    catch { missingSetting = true; }

                    try
                    {
                        Item = xmlFlickStickLSElement.SelectSingleNode("FlickTime");
                        double.TryParse(Item.InnerText, out double temp);
                        store.lsOutputSettings[device].outputSettings.flickSettings.flickTime = temp;
                    }
                    catch { missingSetting = true; }

                    try
                    {
                        Item = xmlFlickStickLSElement.SelectSingleNode("MinAngleThreshold");
                        double.TryParse(Item.InnerText, out double temp);
                        store.lsOutputSettings[device].outputSettings.flickSettings.minAngleThreshold = temp;
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

            XmlNode xmlRSOutputSettingsElement =
                m_Xdoc.SelectSingleNode("/" + rootname + "/RSOutputSettings");
            bool rsOutputGroup = xmlRSOutputSettingsElement != null;
            if (rsOutputGroup)
            {
                bool flickStickRSGroup = false;
                XmlNode xmlFlickStickRSElement =
                    xmlRSOutputSettingsElement.SelectSingleNode("FlickStickSettings");
                flickStickRSGroup = xmlFlickStickRSElement != null;

                if (flickStickRSGroup)
                {
                    try
                    {
                        Item = xmlFlickStickRSElement.SelectSingleNode("RealWorldCalibration");
                        double.TryParse(Item.InnerText, out double temp);
                        store.rsOutputSettings[device].outputSettings.flickSettings.realWorldCalibration = temp;
                    }
                    catch { missingSetting = true; }

                    try
                    {
                        Item = xmlFlickStickRSElement.SelectSingleNode("FlickThreshold");
                        double.TryParse(Item.InnerText, out double temp);
                        store.rsOutputSettings[device].outputSettings.flickSettings.flickThreshold = temp;
                    }
                    catch { missingSetting = true; }

                    try
                    {
                        Item = xmlFlickStickRSElement.SelectSingleNode("FlickTime");
                        double.TryParse(Item.InnerText, out double temp);
                        store.rsOutputSettings[device].outputSettings.flickSettings.flickTime = temp;
                    }
                    catch { missingSetting = true; }

                    try
                    {
                        Item = xmlFlickStickRSElement.SelectSingleNode("MinAngleThreshold");
                        double.TryParse(Item.InnerText, out double temp);
                        store.rsOutputSettings[device].outputSettings.flickSettings.minAngleThreshold = temp;
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
