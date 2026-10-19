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
    /// Phase8-Step4b W6（Phase8-Step4b-Plan.md §3.3）: BT のポーリング・スティックとトリガーの出力（曲線、四角形、アンチスナップバック、出力モード）・タッチパッドのマウス／スティック・DualSense（BTPollRate〜）の保存。
    /// <c>LegacyProfileXmlSerializer.SaveProfile</c> から移したもの。移動のみで、要素の順序・値の書式は変えない（XML の順序はゴールデンが固定している）。
    /// </summary>
    internal sealed class ProfileOutputCurveSaveSection : IProfileSaveSection
    {
        public void Save(ProfileSaveContext context)
        {
            BackingStore store = context.Store;
            XmlDocument m_Xdoc = context.Xdoc;
            XmlElement rootElement = context.RootElement;
            int device = context.Device;

            string axisOutputCurveString(int id)
            {
                return store.stickOutputCurveString(id);
            }

            XmlNode xmlBTPollRate = m_Xdoc.CreateNode(XmlNodeType.Element, "BTPollRate", null); xmlBTPollRate.InnerText = store.btPollRate[device].ToString(); rootElement.AppendChild(xmlBTPollRate);

            XmlNode xmlLsOutputCurveMode = m_Xdoc.CreateNode(XmlNodeType.Element, "LSOutputCurveMode", null); xmlLsOutputCurveMode.InnerText = store.stickOutputCurveString(store.getLsOutCurveMode(device)); rootElement.AppendChild(xmlLsOutputCurveMode);
            XmlNode xmlLsOutputCurveCustom = m_Xdoc.CreateNode(XmlNodeType.Element, "LSOutputCurveCustom", null); xmlLsOutputCurveCustom.InnerText = store.lsOutBezierCurveObj[device].ToString(); rootElement.AppendChild(xmlLsOutputCurveCustom);

            XmlNode xmlRsOutputCurveMode = m_Xdoc.CreateNode(XmlNodeType.Element, "RSOutputCurveMode", null); xmlRsOutputCurveMode.InnerText = store.stickOutputCurveString(store.getRsOutCurveMode(device)); rootElement.AppendChild(xmlRsOutputCurveMode);
            XmlNode xmlRsOutputCurveCustom = m_Xdoc.CreateNode(XmlNodeType.Element, "RSOutputCurveCustom", null); xmlRsOutputCurveCustom.InnerText = store.rsOutBezierCurveObj[device].ToString(); rootElement.AppendChild(xmlRsOutputCurveCustom);

            XmlNode xmlLsSquareStickMode = m_Xdoc.CreateNode(XmlNodeType.Element, "LSSquareStick", null); xmlLsSquareStickMode.InnerText = store.squStickInfo[device].lsMode.ToString(); rootElement.AppendChild(xmlLsSquareStickMode);
            XmlNode xmlRsSquareStickMode = m_Xdoc.CreateNode(XmlNodeType.Element, "RSSquareStick", null); xmlRsSquareStickMode.InnerText = store.squStickInfo[device].rsMode.ToString(); rootElement.AppendChild(xmlRsSquareStickMode);

            XmlNode xmlSquareStickRoundness = m_Xdoc.CreateNode(XmlNodeType.Element, "SquareStickRoundness", null); xmlSquareStickRoundness.InnerText = store.squStickInfo[device].lsRoundness.ToString(); rootElement.AppendChild(xmlSquareStickRoundness);
            XmlNode xmlSquareRStickRoundness = m_Xdoc.CreateNode(XmlNodeType.Element, "SquareRStickRoundness", null); xmlSquareRStickRoundness.InnerText = store.squStickInfo[device].rsRoundness.ToString(); rootElement.AppendChild(xmlSquareRStickRoundness);

            XmlNode xmlLsAntiSnapbackEnabled = m_Xdoc.CreateNode(XmlNodeType.Element, "LSAntiSnapback", null); xmlLsAntiSnapbackEnabled.InnerText = store.lsAntiSnapbackInfo[device].enabled.ToString(); rootElement.AppendChild(xmlLsAntiSnapbackEnabled);
            XmlNode xmlRsAntiSnapbackEnabled = m_Xdoc.CreateNode(XmlNodeType.Element, "RSAntiSnapback", null); xmlRsAntiSnapbackEnabled.InnerText = store.rsAntiSnapbackInfo[device].enabled.ToString(); rootElement.AppendChild(xmlRsAntiSnapbackEnabled);

            XmlNode xmlLsAntiSnapbackDelta = m_Xdoc.CreateNode(XmlNodeType.Element, "LSAntiSnapbackDelta", null); xmlLsAntiSnapbackDelta.InnerText = store.lsAntiSnapbackInfo[device].delta.ToString(); rootElement.AppendChild(xmlLsAntiSnapbackDelta);
            XmlNode xmlRsAntiSnapbackDelta = m_Xdoc.CreateNode(XmlNodeType.Element, "RSAntiSnapbackDelta", null); xmlRsAntiSnapbackDelta.InnerText = store.rsAntiSnapbackInfo[device].delta.ToString(); rootElement.AppendChild(xmlRsAntiSnapbackDelta);

            XmlNode xmlLsAntiSnapbackTimeout = m_Xdoc.CreateNode(XmlNodeType.Element, "LSAntiSnapbackTimeout", null); xmlLsAntiSnapbackTimeout.InnerText = store.lsAntiSnapbackInfo[device].timeout.ToString(); rootElement.AppendChild(xmlLsAntiSnapbackTimeout);
            XmlNode xmlRsAntiSnapbackTimeout = m_Xdoc.CreateNode(XmlNodeType.Element, "RSAntiSnapbackTimeout", null); xmlRsAntiSnapbackTimeout.InnerText = store.rsAntiSnapbackInfo[device].timeout.ToString(); rootElement.AppendChild(xmlRsAntiSnapbackTimeout);

            XmlNode xmlLsOutputMode = m_Xdoc.CreateNode(XmlNodeType.Element, "LSOutputMode", null); xmlLsOutputMode.InnerText = store.lsOutputSettings[device].mode.ToString(); rootElement.AppendChild(xmlLsOutputMode);
            XmlNode xmlRsOutputMode = m_Xdoc.CreateNode(XmlNodeType.Element, "RSOutputMode", null); xmlRsOutputMode.InnerText = store.rsOutputSettings[device].mode.ToString(); rootElement.AppendChild(xmlRsOutputMode);

            XmlElement xmlLsOutputSettingsElement = m_Xdoc.CreateElement("LSOutputSettings");
            XmlElement xmlLsFlickStickGroupElement = m_Xdoc.CreateElement("FlickStickSettings"); xmlLsOutputSettingsElement.AppendChild(xmlLsFlickStickGroupElement);
            XmlNode xmlLsFlickStickRWC = m_Xdoc.CreateNode(XmlNodeType.Element, "RealWorldCalibration", null); xmlLsFlickStickRWC.InnerText = store.lsOutputSettings[device].outputSettings.flickSettings.realWorldCalibration.ToString(); xmlLsFlickStickGroupElement.AppendChild(xmlLsFlickStickRWC);
            XmlNode xmlLsFlickStickThreshold = m_Xdoc.CreateNode(XmlNodeType.Element, "FlickThreshold", null); xmlLsFlickStickThreshold.InnerText = store.lsOutputSettings[device].outputSettings.flickSettings.flickThreshold.ToString(); xmlLsFlickStickGroupElement.AppendChild(xmlLsFlickStickThreshold);
            XmlNode xmlLsFlickStickTime = m_Xdoc.CreateNode(XmlNodeType.Element, "FlickTime", null); xmlLsFlickStickTime.InnerText = store.lsOutputSettings[device].outputSettings.flickSettings.flickTime.ToString(); xmlLsFlickStickGroupElement.AppendChild(xmlLsFlickStickTime);
            XmlNode xmlLsMinAngleThreshold = m_Xdoc.CreateNode(XmlNodeType.Element, "MinAngleThreshold", null); xmlLsMinAngleThreshold.InnerText = store.lsOutputSettings[device].outputSettings.flickSettings.minAngleThreshold.ToString(); xmlLsFlickStickGroupElement.AppendChild(xmlLsMinAngleThreshold);
            rootElement.AppendChild(xmlLsOutputSettingsElement);

            XmlElement xmlRsOutputSettingsElement = m_Xdoc.CreateElement("RSOutputSettings");
            XmlElement xmlRsFlickStickGroupElement = m_Xdoc.CreateElement("FlickStickSettings"); xmlRsOutputSettingsElement.AppendChild(xmlRsFlickStickGroupElement);
            XmlNode xmlRsFlickStickRWC = m_Xdoc.CreateNode(XmlNodeType.Element, "RealWorldCalibration", null); xmlRsFlickStickRWC.InnerText = store.rsOutputSettings[device].outputSettings.flickSettings.realWorldCalibration.ToString(); xmlRsFlickStickGroupElement.AppendChild(xmlRsFlickStickRWC);
            XmlNode xmlRsFlickStickThreshold = m_Xdoc.CreateNode(XmlNodeType.Element, "FlickThreshold", null); xmlRsFlickStickThreshold.InnerText = store.rsOutputSettings[device].outputSettings.flickSettings.flickThreshold.ToString(); xmlRsFlickStickGroupElement.AppendChild(xmlRsFlickStickThreshold);
            XmlNode xmlRsFlickStickTime = m_Xdoc.CreateNode(XmlNodeType.Element, "FlickTime", null); xmlRsFlickStickTime.InnerText = store.rsOutputSettings[device].outputSettings.flickSettings.flickTime.ToString(); xmlRsFlickStickGroupElement.AppendChild(xmlRsFlickStickTime);
            XmlNode xmlRsMinAngleThreshold = m_Xdoc.CreateNode(XmlNodeType.Element, "MinAngleThreshold", null); xmlRsMinAngleThreshold.InnerText = store.rsOutputSettings[device].outputSettings.flickSettings.minAngleThreshold.ToString(); xmlRsFlickStickGroupElement.AppendChild(xmlRsMinAngleThreshold);
            rootElement.AppendChild(xmlRsOutputSettingsElement);

            XmlNode xmlL2OutputCurveMode = m_Xdoc.CreateNode(XmlNodeType.Element, "L2OutputCurveMode", null); xmlL2OutputCurveMode.InnerText = axisOutputCurveString(store.getL2OutCurveMode(device)); rootElement.AppendChild(xmlL2OutputCurveMode);
            XmlNode xmlL2OutputCurveCustom = m_Xdoc.CreateNode(XmlNodeType.Element, "L2OutputCurveCustom", null); xmlL2OutputCurveCustom.InnerText = store.l2OutBezierCurveObj[device].ToString(); rootElement.AppendChild(xmlL2OutputCurveCustom);

            XmlNode xmlL2TwoStageMode = m_Xdoc.CreateNode(XmlNodeType.Element, "L2TwoStageMode", null); xmlL2TwoStageMode.InnerText = store.l2OutputSettings[device].twoStageMode.ToString(); rootElement.AppendChild(xmlL2TwoStageMode);
            XmlNode xmlR2TwoStageMode = m_Xdoc.CreateNode(XmlNodeType.Element, "R2TwoStageMode", null); xmlR2TwoStageMode.InnerText = store.r2OutputSettings[device].twoStageMode.ToString(); rootElement.AppendChild(xmlR2TwoStageMode);

            XmlNode xmlL2HipFireTime = m_Xdoc.CreateNode(XmlNodeType.Element, "L2HipFireTime", null); xmlL2HipFireTime.InnerText = store.l2OutputSettings[device].hipFireMS.ToString(); rootElement.AppendChild(xmlL2HipFireTime);
            XmlNode xmlR2HipFireTime = m_Xdoc.CreateNode(XmlNodeType.Element, "R2HipFireTime", null); xmlR2HipFireTime.InnerText = store.r2OutputSettings[device].hipFireMS.ToString(); rootElement.AppendChild(xmlR2HipFireTime);

            XmlNode xmlL2TriggerEffect = m_Xdoc.CreateNode(XmlNodeType.Element, "L2TriggerEffect", null); xmlL2TriggerEffect.InnerText = store.l2OutputSettings[device].triggerEffect.ToString(); rootElement.AppendChild(xmlL2TriggerEffect);
            XmlNode xmlR2TriggerEffect = m_Xdoc.CreateNode(XmlNodeType.Element, "R2TriggerEffect", null); xmlR2TriggerEffect.InnerText = store.r2OutputSettings[device].triggerEffect.ToString(); rootElement.AppendChild(xmlR2TriggerEffect);

            XmlNode xmlR2OutputCurveMode = m_Xdoc.CreateNode(XmlNodeType.Element, "R2OutputCurveMode", null); xmlR2OutputCurveMode.InnerText = axisOutputCurveString(store.getR2OutCurveMode(device)); rootElement.AppendChild(xmlR2OutputCurveMode);
            XmlNode xmlR2OutputCurveCustom = m_Xdoc.CreateNode(XmlNodeType.Element, "R2OutputCurveCustom", null); xmlR2OutputCurveCustom.InnerText = store.r2OutBezierCurveObj[device].ToString(); rootElement.AppendChild(xmlR2OutputCurveCustom);

            XmlNode xmlSXOutputCurveMode = m_Xdoc.CreateNode(XmlNodeType.Element, "SXOutputCurveMode", null); xmlSXOutputCurveMode.InnerText = axisOutputCurveString(store.getSXOutCurveMode(device)); rootElement.AppendChild(xmlSXOutputCurveMode);
            XmlNode xmlSXOutputCurveCustom = m_Xdoc.CreateNode(XmlNodeType.Element, "SXOutputCurveCustom", null); xmlSXOutputCurveCustom.InnerText = store.sxOutBezierCurveObj[device].ToString(); rootElement.AppendChild(xmlSXOutputCurveCustom);

            XmlNode xmlSZOutputCurveMode = m_Xdoc.CreateNode(XmlNodeType.Element, "SZOutputCurveMode", null); xmlSZOutputCurveMode.InnerText = axisOutputCurveString(store.getSZOutCurveMode(device)); rootElement.AppendChild(xmlSZOutputCurveMode);
            XmlNode xmlSZOutputCurveCustom = m_Xdoc.CreateNode(XmlNodeType.Element, "SZOutputCurveCustom", null); xmlSZOutputCurveCustom.InnerText = store.szOutBezierCurveObj[device].ToString(); rootElement.AppendChild(xmlSZOutputCurveCustom);

            XmlNode xmlTrackBallMode = m_Xdoc.CreateNode(XmlNodeType.Element, "TrackballMode", null); xmlTrackBallMode.InnerText = store.trackballMode[device].ToString(); rootElement.AppendChild(xmlTrackBallMode);
            XmlNode xmlTrackBallFriction = m_Xdoc.CreateNode(XmlNodeType.Element, "TrackballFriction", null); xmlTrackBallFriction.InnerText = store.trackballFriction[device].ToString(); rootElement.AppendChild(xmlTrackBallFriction);

            XmlNode xmlTouchRelMouseRotation = m_Xdoc.CreateNode(XmlNodeType.Element, "TouchRelMouseRotation", null); xmlTouchRelMouseRotation.InnerText = Convert.ToInt32(store.touchpadRelMouse[device].rotation * 180.0 / Math.PI).ToString(); rootElement.AppendChild(xmlTouchRelMouseRotation);
            XmlNode xmlTouchRelMouseMinThreshold = m_Xdoc.CreateNode(XmlNodeType.Element, "TouchRelMouseMinThreshold", null); xmlTouchRelMouseMinThreshold.InnerText = store.touchpadRelMouse[device].minThreshold.ToString(); rootElement.AppendChild(xmlTouchRelMouseMinThreshold);

            // Isolate as a group. More readable for this???
            //if (false)
            {
                XmlElement xmlTouchMouseStickGroupEl = m_Xdoc.CreateElement("TouchpadMouseStick");
                XmlElement xmlTouchMouseStickDeadZone = m_Xdoc.CreateElement("DeadZone"); xmlTouchMouseStickDeadZone.InnerText = store.touchMStickInfo[device].deadZone.ToString(); xmlTouchMouseStickGroupEl.AppendChild(xmlTouchMouseStickDeadZone);
                XmlElement xmlTouchMouseStickMaxZone = m_Xdoc.CreateElement("MaxZone"); xmlTouchMouseStickMaxZone.InnerText = store.touchMStickInfo[device].maxZone.ToString(); xmlTouchMouseStickGroupEl.AppendChild(xmlTouchMouseStickMaxZone);
                XmlElement xmlTouchMouseStickOutStick = m_Xdoc.CreateElement("OutputStick"); xmlTouchMouseStickOutStick.InnerText = store.touchMStickInfo[device].outputStick.ToString(); xmlTouchMouseStickGroupEl.AppendChild(xmlTouchMouseStickOutStick);
                XmlElement xmlTouchMouseStickOutStickAxes = m_Xdoc.CreateElement("OutputStickAxes"); xmlTouchMouseStickOutStickAxes.InnerText = store.touchMStickInfo[device].outputStickDir.ToString(); xmlTouchMouseStickGroupEl.AppendChild(xmlTouchMouseStickOutStickAxes);
                XmlElement xmlTouchMouseStickAntiDeadX = m_Xdoc.CreateElement("AntiDeadX"); xmlTouchMouseStickAntiDeadX.InnerText = store.touchMStickInfo[device].antiDeadX.ToString(); xmlTouchMouseStickGroupEl.AppendChild(xmlTouchMouseStickAntiDeadX);
                XmlElement xmlTouchMouseStickAntiDeadY = m_Xdoc.CreateElement("AntiDeadY"); xmlTouchMouseStickAntiDeadY.InnerText = store.touchMStickInfo[device].antiDeadY.ToString(); xmlTouchMouseStickGroupEl.AppendChild(xmlTouchMouseStickAntiDeadY);
                XmlElement xmlTouchMouseStickInvert = m_Xdoc.CreateElement("Invert"); xmlTouchMouseStickInvert.InnerText = store.touchMStickInfo[device].inverted.ToString(); xmlTouchMouseStickGroupEl.AppendChild(xmlTouchMouseStickInvert);
                XmlElement xmlTouchMouseStickMaxOutput = m_Xdoc.CreateElement("MaxOutput"); xmlTouchMouseStickMaxOutput.InnerText = store.touchMStickInfo[device].maxOutput.ToString(); xmlTouchMouseStickGroupEl.AppendChild(xmlTouchMouseStickMaxOutput);
                XmlElement xmlTouchMouseStickMaxOutputEnabled = m_Xdoc.CreateElement("MaxOutputEnabled"); xmlTouchMouseStickMaxOutputEnabled.InnerText = store.touchMStickInfo[device].maxOutputEnabled.ToString(); xmlTouchMouseStickGroupEl.AppendChild(xmlTouchMouseStickMaxOutputEnabled);
                XmlElement xmlTouchMouseStickVerticalScale = m_Xdoc.CreateElement("VerticalScale"); xmlTouchMouseStickVerticalScale.InnerText = store.touchMStickInfo[device].vertScale.ToString(); xmlTouchMouseStickGroupEl.AppendChild(xmlTouchMouseStickVerticalScale);
                XmlElement xmlTouchMouseStickOutputCurve = m_Xdoc.CreateElement("OutputCurve"); xmlTouchMouseStickOutputCurve.InnerText = store.touchMStickInfo[device].outputCurve.ToString(); xmlTouchMouseStickGroupEl.AppendChild(xmlTouchMouseStickOutputCurve);
                XmlElement xmlTouchMouseStickRotation = m_Xdoc.CreateElement("Rotation"); xmlTouchMouseStickRotation.InnerText = Convert.ToInt32(store.touchMStickInfo[device].rotationRad * 180.0 / Math.PI).ToString(); xmlTouchMouseStickGroupEl.AppendChild(xmlTouchMouseStickRotation);

                XmlElement xmlTouchMouseStickSmoothSettingsEl = m_Xdoc.CreateElement("SmoothingSettings");
                XmlElement xmlTouchMouseStickSmoothMethod = m_Xdoc.CreateElement("SmoothingMethod"); xmlTouchMouseStickSmoothMethod.InnerText = store.touchMStickInfo[device].smoothingMethod.ToString(); xmlTouchMouseStickSmoothSettingsEl.AppendChild(xmlTouchMouseStickSmoothMethod);
                XmlElement xmlTouchMouseStickSmoothMinCutoff = m_Xdoc.CreateElement("SmoothingMinCutoff"); xmlTouchMouseStickSmoothMinCutoff.InnerText = store.touchMStickInfo[device].minCutoff.ToString(); xmlTouchMouseStickSmoothSettingsEl.AppendChild(xmlTouchMouseStickSmoothMinCutoff);
                XmlElement xmlTouchMouseStickSmoothBeta = m_Xdoc.CreateElement("SmoothingBeta"); xmlTouchMouseStickSmoothBeta.InnerText = store.touchMStickInfo[device].beta.ToString(); xmlTouchMouseStickSmoothSettingsEl.AppendChild(xmlTouchMouseStickSmoothBeta);
                xmlTouchMouseStickGroupEl.AppendChild(xmlTouchMouseStickSmoothSettingsEl);
                rootElement.AppendChild(xmlTouchMouseStickGroupEl);
            }

            XmlNode xmlTouchButtonMode = m_Xdoc.CreateNode(XmlNodeType.Element, "TouchpadButtonMode", null); xmlTouchButtonMode.InnerText = store.touchpadButtonMode[device].ToString(); rootElement.AppendChild(xmlTouchButtonMode);
            // Start of DualSense specific settings
            // xmlDSRumbleGroupElement.AppendChild();
            XmlElement xmlDualSenseControllerSettingsElement = m_Xdoc.CreateElement("DualSenseControllerSettings");
            XmlElement xmlDSRumbleGroupElement = m_Xdoc.CreateElement("RumbleSettings"); xmlDualSenseControllerSettingsElement.AppendChild(xmlDSRumbleGroupElement);
            XmlNode xmlDSREmulationModeElement = m_Xdoc.CreateNode(XmlNodeType.Element, "EmulationMode", null); xmlDSREmulationModeElement.InnerText = store.dualSenseRumbleEmulationMode[device].ToString(); xmlDSRumbleGroupElement.AppendChild(xmlDSREmulationModeElement);
            XmlNode xmlDSREnableGenericRumbleRescaleElement = m_Xdoc.CreateNode(XmlNodeType.Element, "EnableGenericRumbleRescale", null); xmlDSREnableGenericRumbleRescaleElement.InnerText = store.useGenericRumbleRescaleForDualSenses[device].ToString(); xmlDSRumbleGroupElement.AppendChild(xmlDSREnableGenericRumbleRescaleElement);
            XmlNode xmlDSRHapticPowerLevelElement = m_Xdoc.CreateNode(XmlNodeType.Element, "HapticPowerLevel", null); xmlDSRHapticPowerLevelElement.InnerText = store.dualSenseHapticPowerLevel[device].ToString(); xmlDSRumbleGroupElement.AppendChild(xmlDSRHapticPowerLevelElement);
            rootElement.AppendChild(xmlDualSenseControllerSettingsElement);
            //
            // End of DualSense specific settings
        }
    }
}
