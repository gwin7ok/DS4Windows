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
    /// Phase8-Step4b W6（Phase8-Step4b-Plan.md §3.3）: タッチパッド・トリガー・スティックの不感帯・最大域・軸別・デルタ加速・レインボー・ChargingType（lowerRCOn〜ChargingType）の保存。
    /// <c>LegacyProfileXmlSerializer.SaveProfile</c> から移したもの。移動のみで、要素の順序・値の書式は変えない（XML の順序はゴールデンが固定している）。
    /// </summary>
    internal sealed class ProfileTriggerStickSaveSection : IProfileSaveSection
    {
        public void Save(ProfileSaveContext context)
        {
            BackingStore store = context.Store;
            XmlDocument m_Xdoc = context.Xdoc;
            XmlElement rootElement = context.RootElement;
            int device = context.Device;
            LightbarDS4WinInfo lightInfo = context.LightInfo;

            XmlNode xmlLowerRCOn = m_Xdoc.CreateNode(XmlNodeType.Element, "lowerRCOn", null); xmlLowerRCOn.InnerText = store.lowerRCOn[device].ToString(); rootElement.AppendChild(xmlLowerRCOn);
            XmlNode xmlTapSensitivity = m_Xdoc.CreateNode(XmlNodeType.Element, "tapSensitivity", null); xmlTapSensitivity.InnerText = store.tapSensitivity[device].ToString(); rootElement.AppendChild(xmlTapSensitivity);
            XmlNode xmlDouble = m_Xdoc.CreateNode(XmlNodeType.Element, "doubleTap", null); xmlDouble.InnerText = store.doubleTap[device].ToString(); rootElement.AppendChild(xmlDouble);
            XmlNode xmlScrollSensitivity = m_Xdoc.CreateNode(XmlNodeType.Element, "scrollSensitivity", null); xmlScrollSensitivity.InnerText = store.scrollSensitivity[device].ToString(); rootElement.AppendChild(xmlScrollSensitivity);
            XmlNode xmlLeftTriggerMiddle = m_Xdoc.CreateNode(XmlNodeType.Element, "LeftTriggerMiddle", null); xmlLeftTriggerMiddle.InnerText = store.l2ModInfo[device].deadZone.ToString(); rootElement.AppendChild(xmlLeftTriggerMiddle);
            XmlNode xmlRightTriggerMiddle = m_Xdoc.CreateNode(XmlNodeType.Element, "RightTriggerMiddle", null); xmlRightTriggerMiddle.InnerText = store.r2ModInfo[device].deadZone.ToString(); rootElement.AppendChild(xmlRightTriggerMiddle);
            XmlNode xmlTouchpadInvert = m_Xdoc.CreateNode(XmlNodeType.Element, "TouchpadInvert", null); xmlTouchpadInvert.InnerText = store.touchpadInvert[device].ToString(); rootElement.AppendChild(xmlTouchpadInvert);
            XmlNode xmlTouchClickPasthru = m_Xdoc.CreateNode(XmlNodeType.Element, "TouchpadClickPassthru", null); xmlTouchClickPasthru.InnerText = store.touchClickPassthru[device].ToString(); rootElement.AppendChild(xmlTouchClickPasthru);

            XmlNode xmlL2AD = m_Xdoc.CreateNode(XmlNodeType.Element, "L2AntiDeadZone", null); xmlL2AD.InnerText = store.l2ModInfo[device].antiDeadZone.ToString(); rootElement.AppendChild(xmlL2AD);
            XmlNode xmlR2AD = m_Xdoc.CreateNode(XmlNodeType.Element, "R2AntiDeadZone", null); xmlR2AD.InnerText = store.r2ModInfo[device].antiDeadZone.ToString(); rootElement.AppendChild(xmlR2AD);
            XmlNode xmlL2Maxzone = m_Xdoc.CreateNode(XmlNodeType.Element, "L2MaxZone", null); xmlL2Maxzone.InnerText = store.l2ModInfo[device].maxZone.ToString(); rootElement.AppendChild(xmlL2Maxzone);
            XmlNode xmlR2Maxzone = m_Xdoc.CreateNode(XmlNodeType.Element, "R2MaxZone", null); xmlR2Maxzone.InnerText = store.r2ModInfo[device].maxZone.ToString(); rootElement.AppendChild(xmlR2Maxzone);
            XmlNode xmlL2MaxOutput = m_Xdoc.CreateNode(XmlNodeType.Element, "L2MaxOutput", null); xmlL2MaxOutput.InnerText = store.l2ModInfo[device].maxOutput.ToString(); rootElement.AppendChild(xmlL2MaxOutput);
            XmlNode xmlR2MaxOutput = m_Xdoc.CreateNode(XmlNodeType.Element, "R2MaxOutput", null); xmlR2MaxOutput.InnerText = store.r2ModInfo[device].maxOutput.ToString(); rootElement.AppendChild(xmlR2MaxOutput);
            XmlNode xmlButtonMouseSensitivity = m_Xdoc.CreateNode(XmlNodeType.Element, "ButtonMouseSensitivity", null); xmlButtonMouseSensitivity.InnerText = store.buttonMouseInfos[device].buttonSensitivity.ToString(); rootElement.AppendChild(xmlButtonMouseSensitivity);
            XmlNode xmlButtonMouseOffset = m_Xdoc.CreateNode(XmlNodeType.Element, "ButtonMouseOffset", null); xmlButtonMouseOffset.InnerText = store.buttonMouseInfos[device].mouseVelocityOffset.ToString(); rootElement.AppendChild(xmlButtonMouseOffset);
            XmlNode xmlRainbow = m_Xdoc.CreateNode(XmlNodeType.Element, "Rainbow", null); xmlRainbow.InnerText = lightInfo.rainbow.ToString(); rootElement.AppendChild(xmlRainbow);
            XmlNode xmlMaxSatRainbow = m_Xdoc.CreateNode(XmlNodeType.Element, "MaxSatRainbow", null); xmlMaxSatRainbow.InnerText = Convert.ToInt32(lightInfo.maxRainbowSat * 100.0).ToString(); rootElement.AppendChild(xmlMaxSatRainbow);
            XmlNode xmlLSD = m_Xdoc.CreateNode(XmlNodeType.Element, "LSDeadZone", null); xmlLSD.InnerText = store.lsModInfo[device].deadZone.ToString(); rootElement.AppendChild(xmlLSD);
            XmlNode xmlRSD = m_Xdoc.CreateNode(XmlNodeType.Element, "RSDeadZone", null); xmlRSD.InnerText = store.rsModInfo[device].deadZone.ToString(); rootElement.AppendChild(xmlRSD);
            XmlNode xmlLSAD = m_Xdoc.CreateNode(XmlNodeType.Element, "LSAntiDeadZone", null); xmlLSAD.InnerText = store.lsModInfo[device].antiDeadZone.ToString(); rootElement.AppendChild(xmlLSAD);
            XmlNode xmlRSAD = m_Xdoc.CreateNode(XmlNodeType.Element, "RSAntiDeadZone", null); xmlRSAD.InnerText = store.rsModInfo[device].antiDeadZone.ToString(); rootElement.AppendChild(xmlRSAD);
            XmlNode xmlLSMaxZone = m_Xdoc.CreateNode(XmlNodeType.Element, "LSMaxZone", null); xmlLSMaxZone.InnerText = store.lsModInfo[device].maxZone.ToString(); rootElement.AppendChild(xmlLSMaxZone);
            XmlNode xmlRSMaxZone = m_Xdoc.CreateNode(XmlNodeType.Element, "RSMaxZone", null); xmlRSMaxZone.InnerText = store.rsModInfo[device].maxZone.ToString(); rootElement.AppendChild(xmlRSMaxZone);
            XmlNode xmlLSVerticalScale = m_Xdoc.CreateNode(XmlNodeType.Element, "LSVerticalScale", null); xmlLSVerticalScale.InnerText = store.lsModInfo[device].verticalScale.ToString(); rootElement.AppendChild(xmlLSVerticalScale);
            XmlNode xmlRSVerticalScale = m_Xdoc.CreateNode(XmlNodeType.Element, "RSVerticalScale", null); xmlRSVerticalScale.InnerText = store.rsModInfo[device].verticalScale.ToString(); rootElement.AppendChild(xmlRSVerticalScale);
            XmlNode xmlLSMaxOutput = m_Xdoc.CreateNode(XmlNodeType.Element, "LSMaxOutput", null); xmlLSMaxOutput.InnerText = store.lsModInfo[device].maxOutput.ToString(); rootElement.AppendChild(xmlLSMaxOutput);
            XmlNode xmlRSMaxOutput = m_Xdoc.CreateNode(XmlNodeType.Element, "RSMaxOutput", null); xmlRSMaxOutput.InnerText = store.rsModInfo[device].maxOutput.ToString(); rootElement.AppendChild(xmlRSMaxOutput);
            XmlNode xmlLSMaxOutputForce = m_Xdoc.CreateNode(XmlNodeType.Element, "LSMaxOutputForce", null); xmlLSMaxOutputForce.InnerText = store.lsModInfo[device].maxOutputForce.ToString(); rootElement.AppendChild(xmlLSMaxOutputForce);
            XmlNode xmlRSMaxOutputForce = m_Xdoc.CreateNode(XmlNodeType.Element, "RSMaxOutputForce", null); xmlRSMaxOutputForce.InnerText = store.rsModInfo[device].maxOutputForce.ToString(); rootElement.AppendChild(xmlRSMaxOutputForce);
            XmlNode xmlLSDeadZoneType = m_Xdoc.CreateNode(XmlNodeType.Element, "LSDeadZoneType", null); xmlLSDeadZoneType.InnerText = store.lsModInfo[device].deadzoneType.ToString(); rootElement.AppendChild(xmlLSDeadZoneType);
            XmlNode xmlRSDeadZoneType = m_Xdoc.CreateNode(XmlNodeType.Element, "RSDeadZoneType", null); xmlRSDeadZoneType.InnerText = store.rsModInfo[device].deadzoneType.ToString(); rootElement.AppendChild(xmlRSDeadZoneType);

            XmlElement xmlLSAxialDeadGroupEl = m_Xdoc.CreateElement("LSAxialDeadOptions");
            XmlElement xmlLSAxialDeadX = m_Xdoc.CreateElement("DeadZoneX"); xmlLSAxialDeadX.InnerText = store.lsModInfo[device].xAxisDeadInfo.deadZone.ToString(); xmlLSAxialDeadGroupEl.AppendChild(xmlLSAxialDeadX);
            XmlElement xmlLSAxialDeadY = m_Xdoc.CreateElement("DeadZoneY"); xmlLSAxialDeadY.InnerText = store.lsModInfo[device].yAxisDeadInfo.deadZone.ToString(); xmlLSAxialDeadGroupEl.AppendChild(xmlLSAxialDeadY);
            XmlElement xmlLSAxialMaxX = m_Xdoc.CreateElement("MaxZoneX"); xmlLSAxialMaxX.InnerText = store.lsModInfo[device].xAxisDeadInfo.maxZone.ToString(); xmlLSAxialDeadGroupEl.AppendChild(xmlLSAxialMaxX);
            XmlElement xmlLSAxialMaxY = m_Xdoc.CreateElement("MaxZoneY"); xmlLSAxialMaxY.InnerText = store.lsModInfo[device].yAxisDeadInfo.maxZone.ToString(); xmlLSAxialDeadGroupEl.AppendChild(xmlLSAxialMaxY);
            XmlElement xmlLSAxialAntiDeadX = m_Xdoc.CreateElement("AntiDeadZoneX"); xmlLSAxialAntiDeadX.InnerText = store.lsModInfo[device].xAxisDeadInfo.antiDeadZone.ToString(); xmlLSAxialDeadGroupEl.AppendChild(xmlLSAxialAntiDeadX);
            XmlElement xmlLSAxialAntiDeadY = m_Xdoc.CreateElement("AntiDeadZoneY"); xmlLSAxialAntiDeadY.InnerText = store.lsModInfo[device].yAxisDeadInfo.antiDeadZone.ToString(); xmlLSAxialDeadGroupEl.AppendChild(xmlLSAxialAntiDeadY);
            XmlElement xmlLSAxialMaxOutputX = m_Xdoc.CreateElement("MaxOutputX"); xmlLSAxialMaxOutputX.InnerText = store.lsModInfo[device].xAxisDeadInfo.maxOutput.ToString(); xmlLSAxialDeadGroupEl.AppendChild(xmlLSAxialMaxOutputX);
            XmlElement xmlLSAxialMaxOutputY = m_Xdoc.CreateElement("MaxOutputY"); xmlLSAxialMaxOutputY.InnerText = store.lsModInfo[device].yAxisDeadInfo.maxOutput.ToString(); xmlLSAxialDeadGroupEl.AppendChild(xmlLSAxialMaxOutputY);
            rootElement.AppendChild(xmlLSAxialDeadGroupEl);

            XmlElement xmlRSAxialDeadGroupEl = m_Xdoc.CreateElement("RSAxialDeadOptions");
            XmlElement xmlRSAxialDeadX = m_Xdoc.CreateElement("DeadZoneX"); xmlRSAxialDeadX.InnerText = store.rsModInfo[device].xAxisDeadInfo.deadZone.ToString(); xmlRSAxialDeadGroupEl.AppendChild(xmlRSAxialDeadX);
            XmlElement xmlRSAxialDeadY = m_Xdoc.CreateElement("DeadZoneY"); xmlRSAxialDeadY.InnerText = store.rsModInfo[device].yAxisDeadInfo.deadZone.ToString(); xmlRSAxialDeadGroupEl.AppendChild(xmlRSAxialDeadY);
            XmlElement xmlRSAxialMaxX = m_Xdoc.CreateElement("MaxZoneX"); xmlRSAxialMaxX.InnerText = store.rsModInfo[device].xAxisDeadInfo.maxZone.ToString(); xmlRSAxialDeadGroupEl.AppendChild(xmlRSAxialMaxX);
            XmlElement xmlRSAxialMaxY = m_Xdoc.CreateElement("MaxZoneY"); xmlRSAxialMaxY.InnerText = store.rsModInfo[device].yAxisDeadInfo.maxZone.ToString(); xmlRSAxialDeadGroupEl.AppendChild(xmlRSAxialMaxY);
            XmlElement xmlRSAxialAntiDeadX = m_Xdoc.CreateElement("AntiDeadZoneX"); xmlRSAxialAntiDeadX.InnerText = store.rsModInfo[device].xAxisDeadInfo.antiDeadZone.ToString(); xmlRSAxialDeadGroupEl.AppendChild(xmlRSAxialAntiDeadX);
            XmlElement xmlRSAxialAntiDeadY = m_Xdoc.CreateElement("AntiDeadZoneY"); xmlRSAxialAntiDeadY.InnerText = store.rsModInfo[device].yAxisDeadInfo.antiDeadZone.ToString(); xmlRSAxialDeadGroupEl.AppendChild(xmlRSAxialAntiDeadY);
            XmlElement xmlRSAxialMaxOutputX = m_Xdoc.CreateElement("MaxOutputX"); xmlRSAxialMaxOutputX.InnerText = store.rsModInfo[device].xAxisDeadInfo.maxOutput.ToString(); xmlRSAxialDeadGroupEl.AppendChild(xmlRSAxialMaxOutputX);
            XmlElement xmlRSAxialMaxOutputY = m_Xdoc.CreateElement("MaxOutputY"); xmlRSAxialMaxOutputY.InnerText = store.rsModInfo[device].yAxisDeadInfo.maxOutput.ToString(); xmlRSAxialDeadGroupEl.AppendChild(xmlRSAxialMaxOutputY);
            rootElement.AppendChild(xmlRSAxialDeadGroupEl);

            // Output rotation values to profile in degrees
            XmlNode xmlLSRotation = m_Xdoc.CreateNode(XmlNodeType.Element, "LSRotation", null); xmlLSRotation.InnerText = Convert.ToInt32(store.LSRotation[device] * 180.0 / Math.PI).ToString(); rootElement.AppendChild(xmlLSRotation);
            XmlNode xmlRSRotation = m_Xdoc.CreateNode(XmlNodeType.Element, "RSRotation", null); xmlRSRotation.InnerText = Convert.ToInt32(store.RSRotation[device] * 180.0 / Math.PI).ToString(); rootElement.AppendChild(xmlRSRotation);

            XmlNode xmlLSFuzz = m_Xdoc.CreateNode(XmlNodeType.Element, "LSFuzz", null); xmlLSFuzz.InnerText = store.lsModInfo[device].fuzz.ToString(); rootElement.AppendChild(xmlLSFuzz);
            XmlNode xmlRSFuzz = m_Xdoc.CreateNode(XmlNodeType.Element, "RSFuzz", null); xmlRSFuzz.InnerText = store.rsModInfo[device].fuzz.ToString(); rootElement.AppendChild(xmlRSFuzz);
            XmlNode xmlLSOuterBindDead = m_Xdoc.CreateNode(XmlNodeType.Element, "LSOuterBindDead", null); xmlLSOuterBindDead.InnerText = Convert.ToInt32(store.lsModInfo[device].outerBindDeadZone).ToString(); rootElement.AppendChild(xmlLSOuterBindDead);
            XmlNode xmlRSOuterBindDead = m_Xdoc.CreateNode(XmlNodeType.Element, "RSOuterBindDead", null); xmlRSOuterBindDead.InnerText = Convert.ToInt32(store.rsModInfo[device].outerBindDeadZone).ToString(); rootElement.AppendChild(xmlRSOuterBindDead);
            XmlNode xmlLSOuterBindInvert = m_Xdoc.CreateNode(XmlNodeType.Element, "LSOuterBindInvert", null); xmlLSOuterBindInvert.InnerText = store.lsModInfo[device].outerBindInvert.ToString(); rootElement.AppendChild(xmlLSOuterBindInvert);
            XmlNode xmlRSOuterBindInvert = m_Xdoc.CreateNode(XmlNodeType.Element, "RSOuterBindInvert", null); xmlRSOuterBindInvert.InnerText = store.rsModInfo[device].outerBindInvert.ToString(); rootElement.AppendChild(xmlRSOuterBindInvert);

            XmlElement xmlLSDeltaAccelGroupEl = m_Xdoc.CreateElement("LSDeltaAccelSettings");
            XmlElement xmlLSDeltaEnabled = m_Xdoc.CreateElement("Enabled"); xmlLSDeltaEnabled.InnerText = store.lsOutputSettings[device].outputSettings.controlSettings.deltaAccelSettings.enabled.ToString(); xmlLSDeltaAccelGroupEl.AppendChild(xmlLSDeltaEnabled);
            XmlElement xmlLSDeltaMultiplier = m_Xdoc.CreateElement("Multiplier"); xmlLSDeltaMultiplier.InnerText = store.lsOutputSettings[device].outputSettings.controlSettings.deltaAccelSettings.multiplier.ToString(); xmlLSDeltaAccelGroupEl.AppendChild(xmlLSDeltaMultiplier);
            XmlElement xmlLSDeltaMaxTravel = m_Xdoc.CreateElement("MaxTravel"); xmlLSDeltaMaxTravel.InnerText = store.lsOutputSettings[device].outputSettings.controlSettings.deltaAccelSettings.maxTravel.ToString(); xmlLSDeltaAccelGroupEl.AppendChild(xmlLSDeltaMaxTravel);
            XmlElement xmlLSDeltaMinTravel = m_Xdoc.CreateElement("MinTravel"); xmlLSDeltaMinTravel.InnerText = store.lsOutputSettings[device].outputSettings.controlSettings.deltaAccelSettings.minTravel.ToString(); xmlLSDeltaAccelGroupEl.AppendChild(xmlLSDeltaMinTravel);
            XmlElement xmlLSDeltaEasingDuration = m_Xdoc.CreateElement("EasingDuration"); xmlLSDeltaEasingDuration.InnerText = store.lsOutputSettings[device].outputSettings.controlSettings.deltaAccelSettings.easingDuration.ToString(); xmlLSDeltaAccelGroupEl.AppendChild(xmlLSDeltaEasingDuration);
            XmlElement xmlLSDeltaMinFactor = m_Xdoc.CreateElement("MinFactor"); xmlLSDeltaMinFactor.InnerText = store.lsOutputSettings[device].outputSettings.controlSettings.deltaAccelSettings.minfactor.ToString(); xmlLSDeltaAccelGroupEl.AppendChild(xmlLSDeltaMinFactor);
            rootElement.AppendChild(xmlLSDeltaAccelGroupEl);

            XmlElement xmlRSDeltaAccelGroupEl = m_Xdoc.CreateElement("RSDeltaAccelSettings");
            XmlElement xmlRSDeltaEnabled = m_Xdoc.CreateElement("Enabled"); xmlRSDeltaEnabled.InnerText = store.rsOutputSettings[device].outputSettings.controlSettings.deltaAccelSettings.enabled.ToString(); xmlRSDeltaAccelGroupEl.AppendChild(xmlRSDeltaEnabled);
            XmlElement xmlRSDeltaMultiplier = m_Xdoc.CreateElement("Multiplier"); xmlRSDeltaMultiplier.InnerText = store.rsOutputSettings[device].outputSettings.controlSettings.deltaAccelSettings.multiplier.ToString(); xmlRSDeltaAccelGroupEl.AppendChild(xmlRSDeltaMultiplier);
            XmlElement xmlRSDeltaMaxTravel = m_Xdoc.CreateElement("MaxTravel"); xmlRSDeltaMaxTravel.InnerText = store.rsOutputSettings[device].outputSettings.controlSettings.deltaAccelSettings.maxTravel.ToString(); xmlRSDeltaAccelGroupEl.AppendChild(xmlRSDeltaMaxTravel);
            XmlElement xmlRSDeltaMinTravel = m_Xdoc.CreateElement("MinTravel"); xmlRSDeltaMinTravel.InnerText = store.rsOutputSettings[device].outputSettings.controlSettings.deltaAccelSettings.minTravel.ToString(); xmlRSDeltaAccelGroupEl.AppendChild(xmlRSDeltaMinTravel);
            XmlElement xmlRSDeltaEasingDuration = m_Xdoc.CreateElement("EasingDuration"); xmlRSDeltaEasingDuration.InnerText = store.rsOutputSettings[device].outputSettings.controlSettings.deltaAccelSettings.easingDuration.ToString(); xmlRSDeltaAccelGroupEl.AppendChild(xmlRSDeltaEasingDuration);
            XmlElement xmlRSDeltaMinFactor = m_Xdoc.CreateElement("MinFactor"); xmlRSDeltaMinFactor.InnerText = store.rsOutputSettings[device].outputSettings.controlSettings.deltaAccelSettings.minfactor.ToString(); xmlRSDeltaAccelGroupEl.AppendChild(xmlRSDeltaMinFactor);
            rootElement.AppendChild(xmlRSDeltaAccelGroupEl);

            XmlNode xmlSXD = m_Xdoc.CreateNode(XmlNodeType.Element, "SXDeadZone", null); xmlSXD.InnerText = store.SXDeadzone[device].ToString(); rootElement.AppendChild(xmlSXD);
            XmlNode xmlSZD = m_Xdoc.CreateNode(XmlNodeType.Element, "SZDeadZone", null); xmlSZD.InnerText = store.SZDeadzone[device].ToString(); rootElement.AppendChild(xmlSZD);

            XmlNode xmlSXMaxzone = m_Xdoc.CreateNode(XmlNodeType.Element, "SXMaxZone", null); xmlSXMaxzone.InnerText = Convert.ToInt32(store.SXMaxzone[device] * 100.0).ToString(); rootElement.AppendChild(xmlSXMaxzone);
            XmlNode xmlSZMaxzone = m_Xdoc.CreateNode(XmlNodeType.Element, "SZMaxZone", null); xmlSZMaxzone.InnerText = Convert.ToInt32(store.SZMaxzone[device] * 100.0).ToString(); rootElement.AppendChild(xmlSZMaxzone);

            XmlNode xmlSXAntiDeadzone = m_Xdoc.CreateNode(XmlNodeType.Element, "SXAntiDeadZone", null); xmlSXAntiDeadzone.InnerText = Convert.ToInt32(store.SXAntiDeadzone[device] * 100.0).ToString(); rootElement.AppendChild(xmlSXAntiDeadzone);
            XmlNode xmlSZAntiDeadzone = m_Xdoc.CreateNode(XmlNodeType.Element, "SZAntiDeadZone", null); xmlSZAntiDeadzone.InnerText = Convert.ToInt32(store.SZAntiDeadzone[device] * 100.0).ToString(); rootElement.AppendChild(xmlSZAntiDeadzone);

            XmlNode xmlSens = m_Xdoc.CreateNode(XmlNodeType.Element, "Sensitivity", null);
            xmlSens.InnerText = $"{store.LSSens[device]}|{store.RSSens[device]}|{store.l2Sens[device]}|{store.r2Sens[device]}|{store.SXSens[device]}|{store.SZSens[device]}";
            rootElement.AppendChild(xmlSens);

            XmlNode xmlChargingType = m_Xdoc.CreateNode(XmlNodeType.Element, "ChargingType", null); xmlChargingType.InnerText = lightInfo.chargingType.ToString(); rootElement.AppendChild(xmlChargingType);
        }
    }
}
