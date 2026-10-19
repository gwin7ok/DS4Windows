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
using LightbarMacro = DS4WinWPF.DS4Forms.ViewModels.LightbarMacro;

namespace DS4Windows.Services
{
    /// <summary>
    /// Phase8-Step4b W6（Phase8-Step4b-Plan.md §3.3）: 全般・ライトバー・振動・バッテリー・タッチパッドのジッター補正（touchToggle〜touchpadJitterCompensation）の保存。
    /// <c>LegacyProfileXmlSerializer.SaveProfile</c> から移したもの。移動のみで、要素の順序・値の書式は変えない（XML の順序はゴールデンが固定している）。
    /// </summary>
    internal sealed class ProfileGeneralSaveSection : IProfileSaveSection
    {
        public void Save(ProfileSaveContext context)
        {
            BackingStore store = context.Store;
            XmlDocument m_Xdoc = context.Xdoc;
            XmlElement rootElement = context.RootElement;
            int device = context.Device;
            LightbarSettingInfo lightbarSettings = context.LightbarSettings;
            LightbarDS4WinInfo lightInfo = context.LightInfo;

            string GetLightbarModeString(LightbarMode mode)
            {
                string result = "DS4Win";
                switch (mode)
                {
                    case LightbarMode.DS4Win:
                        result = "DS4Win";
                        break;
                    case LightbarMode.Passthru:
                        result = "Passthru";
                        break;
                    default:
                        break;
                }
                return result;
            }

            XmlNode xmlTouchToggle = m_Xdoc.CreateNode(XmlNodeType.Element, "touchToggle", null); xmlTouchToggle.InnerText = store.enableTouchToggle[device].ToString(); rootElement.AppendChild(xmlTouchToggle);
            XmlNode xmlIdleDisconnectTimeout = m_Xdoc.CreateNode(XmlNodeType.Element, "idleDisconnectTimeout", null); xmlIdleDisconnectTimeout.InnerText = store.idleDisconnectTimeout[device].ToString(); rootElement.AppendChild(xmlIdleDisconnectTimeout);
            XmlNode xmlOutputDataToDS4 = m_Xdoc.CreateNode(XmlNodeType.Element, "outputDataToDS4", null); xmlOutputDataToDS4.InnerText = store.enableOutputDataToDS4[device].ToString(); rootElement.AppendChild(xmlOutputDataToDS4);
            XmlNode xmlColor = m_Xdoc.CreateNode(XmlNodeType.Element, "Color", null);
            xmlColor.InnerText = lightInfo.m_Led.red.ToString() + "," + lightInfo.m_Led.green.ToString() + "," + lightInfo.m_Led.blue.ToString();
            rootElement.AppendChild(xmlColor);
            XmlNode xmlRumbleBoost = m_Xdoc.CreateNode(XmlNodeType.Element, "RumbleBoost", null); xmlRumbleBoost.InnerText = store.rumble[device].ToString(); rootElement.AppendChild(xmlRumbleBoost);
            XmlNode xmlRumbleAutostopTime = m_Xdoc.CreateNode(XmlNodeType.Element, "RumbleAutostopTime", null); xmlRumbleAutostopTime.InnerText = store.rumbleAutostopTime[device].ToString(); rootElement.AppendChild(xmlRumbleAutostopTime);
            XmlNode xmlLightbarMode = m_Xdoc.CreateNode(XmlNodeType.Element, "LightbarMode", null); xmlLightbarMode.InnerText = GetLightbarModeString(lightbarSettings.mode); rootElement.AppendChild(xmlLightbarMode);
            XmlNode xmlLedAsBatteryIndicator = m_Xdoc.CreateNode(XmlNodeType.Element, "ledAsBatteryIndicator", null); xmlLedAsBatteryIndicator.InnerText = lightInfo.ledAsBattery.ToString(); rootElement.AppendChild(xmlLedAsBatteryIndicator);
            XmlNode xmlLowBatteryFlash = m_Xdoc.CreateNode(XmlNodeType.Element, "FlashType", null); xmlLowBatteryFlash.InnerText = lightInfo.flashType.ToString(); rootElement.AppendChild(xmlLowBatteryFlash);
            XmlNode xmlFlashBatterAt = m_Xdoc.CreateNode(XmlNodeType.Element, "flashBatteryAt", null); xmlFlashBatterAt.InnerText = lightInfo.flashAt.ToString(); rootElement.AppendChild(xmlFlashBatterAt);
            XmlNode xmlTouchSensitivity = m_Xdoc.CreateNode(XmlNodeType.Element, "touchSensitivity", null); xmlTouchSensitivity.InnerText = store.touchSensitivity[device].ToString(); rootElement.AppendChild(xmlTouchSensitivity);
            XmlNode xmlLowColor = m_Xdoc.CreateNode(XmlNodeType.Element, "LowColor", null);
            xmlLowColor.InnerText = lightInfo.m_LowLed.red.ToString() + "," + lightInfo.m_LowLed.green.ToString() + "," + lightInfo.m_LowLed.blue.ToString();
            rootElement.AppendChild(xmlLowColor);
            XmlNode xmlChargingColor = m_Xdoc.CreateNode(XmlNodeType.Element, "ChargingColor", null);
            xmlChargingColor.InnerText = lightInfo.m_ChargingLed.red.ToString() + "," + lightInfo.m_ChargingLed.green.ToString() + "," + lightInfo.m_ChargingLed.blue.ToString();
            rootElement.AppendChild(xmlChargingColor);
            XmlNode xmlFlashColor = m_Xdoc.CreateNode(XmlNodeType.Element, "FlashColor", null);
            xmlFlashColor.InnerText = lightInfo.m_FlashLed.red.ToString() + "," + lightInfo.m_FlashLed.green.ToString() + "," + lightInfo.m_FlashLed.blue.ToString();
            rootElement.AppendChild(xmlFlashColor);
            XmlNode xmlTouchpadJitterCompensation = m_Xdoc.CreateNode(XmlNodeType.Element, "touchpadJitterCompensation", null); xmlTouchpadJitterCompensation.InnerText = store.touchpadJitterCompensation[device].ToString(); rootElement.AppendChild(xmlTouchpadJitterCompensation);
        }
    }
}
