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
    /// Phase8-Step4b W6（Phase8-Step4b-Plan.md §3.3）: マウス加速・起動プログラム・DInput のみ・SA ステアリング・ジャイロの制御（MouseAcceleration〜）の保存。
    /// <c>LegacyProfileXmlSerializer.SaveProfile</c> から移したもの。移動のみで、要素の順序・値の書式は変えない（XML の順序はゴールデンが固定している）。
    /// </summary>
    internal sealed class ProfileMotionSaveSection : IProfileSaveSection
    {
        public void Save(ProfileSaveContext context)
        {
            BackingStore store = context.Store;
            XmlDocument m_Xdoc = context.Xdoc;
            XmlElement rootElement = context.RootElement;
            int device = context.Device;

            XmlNode xmlMouseAccel = m_Xdoc.CreateNode(XmlNodeType.Element, "MouseAcceleration", null); xmlMouseAccel.InnerText = store.buttonMouseInfos[device].mouseAccel.ToString(); rootElement.AppendChild(xmlMouseAccel);
            XmlNode xmlMouseVerticalScale = m_Xdoc.CreateNode(XmlNodeType.Element, "ButtonMouseVerticalScale", null); xmlMouseVerticalScale.InnerText = Convert.ToInt32(store.buttonMouseInfos[device].buttonVerticalScale * 100).ToString(); rootElement.AppendChild(xmlMouseVerticalScale);
            //XmlNode xmlShiftMod = m_Xdoc.CreateNode(XmlNodeType.Element, "ShiftModifier", null); xmlShiftMod.InnerText = shiftModifier[device].ToString(); rootElement.AppendChild(xmlShiftMod);
            XmlNode xmlLaunchProgram = m_Xdoc.CreateNode(XmlNodeType.Element, "LaunchProgram", null); xmlLaunchProgram.InnerText = store.launchProgram[device].ToString(); rootElement.AppendChild(xmlLaunchProgram);
            XmlNode xmlDinput = m_Xdoc.CreateNode(XmlNodeType.Element, "DinputOnly", null); xmlDinput.InnerText = store.dinputOnly[device].ToString(); rootElement.AppendChild(xmlDinput);
            XmlNode xmlStartTouchpadOff = m_Xdoc.CreateNode(XmlNodeType.Element, "StartTouchpadOff", null); xmlStartTouchpadOff.InnerText = store.startTouchpadOff[device].ToString(); rootElement.AppendChild(xmlStartTouchpadOff);
            XmlNode xmlTouchOutMode = m_Xdoc.CreateNode(XmlNodeType.Element, "TouchpadOutputMode", null); xmlTouchOutMode.InnerText = store.touchOutMode[device].ToString(); rootElement.AppendChild(xmlTouchOutMode);
            XmlNode xmlSATriggers = m_Xdoc.CreateNode(XmlNodeType.Element, "SATriggers", null); xmlSATriggers.InnerText = store.sATriggers[device].ToString(); rootElement.AppendChild(xmlSATriggers);
            XmlNode xmlSATriggerCond = m_Xdoc.CreateNode(XmlNodeType.Element, "SATriggerCond", null); xmlSATriggerCond.InnerText = BackingStore.SaTriggerCondString(store.sATriggerCond[device]); rootElement.AppendChild(xmlSATriggerCond);
            // Phase9 Steering 削除 W4: 旧「SASteeringWheelEmulationAxis／SASteeringWheelEmulationRange／SASteeringWheelFuzz／SASteeringWheelSmoothingOptions」（ステアリングの設定）の書き込みは、機能ごと削除した。

            XmlNode xmlTouchDisInvTriggers = m_Xdoc.CreateNode(XmlNodeType.Element, "TouchDisInvTriggers", null);
            string tempTouchDisInv = string.Join(",", store.touchDisInvertTriggers[device]);
            xmlTouchDisInvTriggers.InnerText = tempTouchDisInv;
            rootElement.AppendChild(xmlTouchDisInvTriggers);

            XmlNode xmlGyroSensitivity = m_Xdoc.CreateNode(XmlNodeType.Element, "GyroSensitivity", null); xmlGyroSensitivity.InnerText = store.gyroSensitivity[device].ToString(); rootElement.AppendChild(xmlGyroSensitivity);
            XmlNode xmlGyroSensVerticalScale = m_Xdoc.CreateNode(XmlNodeType.Element, "GyroSensVerticalScale", null); xmlGyroSensVerticalScale.InnerText = store.gyroSensVerticalScale[device].ToString(); rootElement.AppendChild(xmlGyroSensVerticalScale);
            XmlNode xmlGyroInvert = m_Xdoc.CreateNode(XmlNodeType.Element, "GyroInvert", null); xmlGyroInvert.InnerText = store.gyroInvert[device].ToString(); rootElement.AppendChild(xmlGyroInvert);
            XmlNode xmlGyroTriggerTurns = m_Xdoc.CreateNode(XmlNodeType.Element, "GyroTriggerTurns", null); xmlGyroTriggerTurns.InnerText = store.gyroTriggerTurns[device].ToString(); rootElement.AppendChild(xmlGyroTriggerTurns);
            /*XmlNode xmlGyroSmoothWeight = m_Xdoc.CreateNode(XmlNodeType.Element, "GyroSmoothingWeight", null); xmlGyroSmoothWeight.InnerText = Convert.ToInt32(gyroSmoothWeight[device] * 100).ToString(); rootElement.AppendChild(xmlGyroSmoothWeight);
            XmlNode xmlGyroSmoothing = m_Xdoc.CreateNode(XmlNodeType.Element, "GyroSmoothing", null); xmlGyroSmoothing.InnerText = gyroSmoothing[device].ToString(); rootElement.AppendChild(xmlGyroSmoothing);
            */

            XmlElement xmlGyroControlsSettingsElement = m_Xdoc.CreateElement("GyroControlsSettings");
            XmlNode xmlGyroControlsTriggers = m_Xdoc.CreateNode(XmlNodeType.Element, "Triggers", null); xmlGyroControlsTriggers.InnerText = store.gyroControlsInf[device].triggers; xmlGyroControlsSettingsElement.AppendChild(xmlGyroControlsTriggers);
            XmlNode xmlGyroControlsTriggerCond = m_Xdoc.CreateNode(XmlNodeType.Element, "TriggerCond", null); xmlGyroControlsTriggerCond.InnerText = BackingStore.SaTriggerCondString(store.gyroControlsInf[device].triggerCond); xmlGyroControlsSettingsElement.AppendChild(xmlGyroControlsTriggerCond);
            XmlNode xmlGyroControlsTriggerTurns = m_Xdoc.CreateNode(XmlNodeType.Element, "TriggerTurns", null); xmlGyroControlsTriggerTurns.InnerText = store.gyroControlsInf[device].triggerTurns.ToString(); xmlGyroControlsSettingsElement.AppendChild(xmlGyroControlsTriggerTurns);
            XmlNode xmlGyroControlsToggle = m_Xdoc.CreateNode(XmlNodeType.Element, "Toggle", null); xmlGyroControlsToggle.InnerText = store.gyroControlsInf[device].triggerToggle.ToString(); xmlGyroControlsSettingsElement.AppendChild(xmlGyroControlsToggle);
            rootElement.AppendChild(xmlGyroControlsSettingsElement);

            XmlElement xmlGyroSmoothingElement = m_Xdoc.CreateElement("GyroMouseSmoothingSettings");
            XmlNode xmlGyroSmoothing = m_Xdoc.CreateNode(XmlNodeType.Element, "UseSmoothing", null); xmlGyroSmoothing.InnerText = store.gyroMouseInfo[device].enableSmoothing.ToString(); xmlGyroSmoothingElement.AppendChild(xmlGyroSmoothing);
            XmlNode xmlGyroSmoothingMethod = m_Xdoc.CreateNode(XmlNodeType.Element, "SmoothingMethod", null); xmlGyroSmoothingMethod.InnerText = store.gyroMouseInfo[device].SmoothMethodIdentifier(); xmlGyroSmoothingElement.AppendChild(xmlGyroSmoothingMethod);
            XmlNode xmlGyroSmoothWeight = m_Xdoc.CreateNode(XmlNodeType.Element, "SmoothingWeight", null); xmlGyroSmoothWeight.InnerText = Convert.ToInt32(store.gyroMouseInfo[device].smoothingWeight * 100).ToString(); xmlGyroSmoothingElement.AppendChild(xmlGyroSmoothWeight);
            XmlNode xmlGyroSmoothMincutoff = m_Xdoc.CreateNode(XmlNodeType.Element, "SmoothingMinCutoff", null); xmlGyroSmoothMincutoff.InnerText = store.gyroMouseInfo[device].minCutoff.ToString(); xmlGyroSmoothingElement.AppendChild(xmlGyroSmoothMincutoff);
            XmlNode xmlGyroSmoothBeta = m_Xdoc.CreateNode(XmlNodeType.Element, "SmoothingBeta", null); xmlGyroSmoothBeta.InnerText = store.gyroMouseInfo[device].beta.ToString(); xmlGyroSmoothingElement.AppendChild(xmlGyroSmoothBeta);
            rootElement.AppendChild(xmlGyroSmoothingElement);

            XmlNode xmlGyroMouseHAxis = m_Xdoc.CreateNode(XmlNodeType.Element, "GyroMouseHAxis", null); xmlGyroMouseHAxis.InnerText = store.gyroMouseHorizontalAxis[device].ToString(); rootElement.AppendChild(xmlGyroMouseHAxis);
            XmlNode xmlGyroMouseDZ = m_Xdoc.CreateNode(XmlNodeType.Element, "GyroMouseDeadZone", null); xmlGyroMouseDZ.InnerText = store.gyroMouseDZ[device].ToString(); rootElement.AppendChild(xmlGyroMouseDZ);
            XmlNode xmlGyroMinThreshold = m_Xdoc.CreateNode(XmlNodeType.Element, "GyroMouseMinThreshold", null); xmlGyroMinThreshold.InnerText = store.gyroMouseInfo[device].minThreshold.ToString(); rootElement.AppendChild(xmlGyroMinThreshold);
            XmlNode xmlGyroMouseToggle = m_Xdoc.CreateNode(XmlNodeType.Element, "GyroMouseToggle", null); xmlGyroMouseToggle.InnerText = store.gyroMouseToggle[device].ToString(); rootElement.AppendChild(xmlGyroMouseToggle);

            XmlNode xmlGyroOutMode = m_Xdoc.CreateNode(XmlNodeType.Element, "GyroOutputMode", null); xmlGyroOutMode.InnerText = store.removedGyroSwipeSettings[device].GyroOutputModeTextForSave(store.gyroOutMode[device].ToString()); rootElement.AppendChild(xmlGyroOutMode);
            XmlNode xmlGyroMStickTriggers = m_Xdoc.CreateNode(XmlNodeType.Element, "GyroMouseStickTriggers", null); xmlGyroMStickTriggers.InnerText = store.sAMouseStickTriggers[device].ToString(); rootElement.AppendChild(xmlGyroMStickTriggers);
            XmlNode xmlGyroMStickTriggerCond = m_Xdoc.CreateNode(XmlNodeType.Element, "GyroMouseStickTriggerCond", null); xmlGyroMStickTriggerCond.InnerText = BackingStore.SaTriggerCondString(store.sAMouseStickTriggerCond[device]); rootElement.AppendChild(xmlGyroMStickTriggerCond);
            XmlNode xmlGyroMStickTriggerTurns = m_Xdoc.CreateNode(XmlNodeType.Element, "GyroMouseStickTriggerTurns", null); xmlGyroMStickTriggerTurns.InnerText = store.gyroMouseStickTriggerTurns[device].ToString(); rootElement.AppendChild(xmlGyroMStickTriggerTurns);
            XmlNode xmlGyroMStickHAxis = m_Xdoc.CreateNode(XmlNodeType.Element, "GyroMouseStickHAxis", null); xmlGyroMStickHAxis.InnerText = store.gyroMouseStickHorizontalAxis[device].ToString(); rootElement.AppendChild(xmlGyroMStickHAxis);
            XmlNode xmlGyroMStickDZ = m_Xdoc.CreateNode(XmlNodeType.Element, "GyroMouseStickDeadZone", null); xmlGyroMStickDZ.InnerText = store.gyroMStickInfo[device].deadZone.ToString(); rootElement.AppendChild(xmlGyroMStickDZ);
            XmlNode xmlGyroMStickMaxZ = m_Xdoc.CreateNode(XmlNodeType.Element, "GyroMouseStickMaxZone", null); xmlGyroMStickMaxZ.InnerText = store.gyroMStickInfo[device].maxZone.ToString(); rootElement.AppendChild(xmlGyroMStickMaxZ);
            XmlNode xmlGyroMStickOutputStick = m_Xdoc.CreateNode(XmlNodeType.Element, "GyroMouseStickOutputStick", null); xmlGyroMStickOutputStick.InnerText = store.gyroMStickInfo[device].outputStick.ToString(); rootElement.AppendChild(xmlGyroMStickOutputStick);
            XmlNode xmlGyroMStickOutputStickAxes = m_Xdoc.CreateNode(XmlNodeType.Element, "GyroMouseStickOutputStickAxes", null); xmlGyroMStickOutputStickAxes.InnerText = store.gyroMStickInfo[device].outputStickDir.ToString(); rootElement.AppendChild(xmlGyroMStickOutputStickAxes);
            XmlNode xmlGyroMStickAntiDX = m_Xdoc.CreateNode(XmlNodeType.Element, "GyroMouseStickAntiDeadX", null); xmlGyroMStickAntiDX.InnerText = store.gyroMStickInfo[device].antiDeadX.ToString(); rootElement.AppendChild(xmlGyroMStickAntiDX);
            XmlNode xmlGyroMStickAntiDY = m_Xdoc.CreateNode(XmlNodeType.Element, "GyroMouseStickAntiDeadY", null); xmlGyroMStickAntiDY.InnerText = store.gyroMStickInfo[device].antiDeadY.ToString(); rootElement.AppendChild(xmlGyroMStickAntiDY);
            XmlNode xmlGyroMStickInvert = m_Xdoc.CreateNode(XmlNodeType.Element, "GyroMouseStickInvert", null); xmlGyroMStickInvert.InnerText = store.gyroMStickInfo[device].inverted.ToString(); rootElement.AppendChild(xmlGyroMStickInvert);
            XmlNode xmlGyroMStickToggle = m_Xdoc.CreateNode(XmlNodeType.Element, "GyroMouseStickToggle", null); xmlGyroMStickToggle.InnerText = store.gyroMouseStickToggle[device].ToString(); rootElement.AppendChild(xmlGyroMStickToggle);
            XmlNode xmlGyroMStickMaxOutput = m_Xdoc.CreateNode(XmlNodeType.Element, "GyroMouseStickMaxOutput", null); xmlGyroMStickMaxOutput.InnerText = store.gyroMStickInfo[device].maxOutput.ToString(); rootElement.AppendChild(xmlGyroMStickMaxOutput);
            XmlNode xmlGyroMStickMaxOutputEnabled = m_Xdoc.CreateNode(XmlNodeType.Element, "GyroMouseStickMaxOutputEnabled", null); xmlGyroMStickMaxOutputEnabled.InnerText = store.gyroMStickInfo[device].maxOutputEnabled.ToString(); rootElement.AppendChild(xmlGyroMStickMaxOutputEnabled);
            XmlNode xmlGyroMStickVerticalScale = m_Xdoc.CreateNode(XmlNodeType.Element, "GyroMouseStickVerticalScale", null); xmlGyroMStickVerticalScale.InnerText = store.gyroMStickInfo[device].vertScale.ToString(); rootElement.AppendChild(xmlGyroMStickVerticalScale);


            /*XmlNode xmlGyroMStickSmoothing = m_Xdoc.CreateNode(XmlNodeType.Element, "GyroMouseStickSmoothing", null); xmlGyroMStickSmoothing.InnerText = gyroMStickInfo[device].useSmoothing.ToString(); rootElement.AppendChild(xmlGyroMStickSmoothing);
            XmlNode xmlGyroMStickSmoothWeight = m_Xdoc.CreateNode(XmlNodeType.Element, "GyroMouseStickSmoothingWeight", null); xmlGyroMStickSmoothWeight.InnerText = Convert.ToInt32(gyroMStickInfo[device].smoothWeight * 100).ToString(); rootElement.AppendChild(xmlGyroMStickSmoothWeight);
            */
            XmlElement xmlGyroMStickSmoothingElement = m_Xdoc.CreateElement("GyroMouseStickSmoothingSettings");
            XmlNode xmlGyroMStickSmoothing = m_Xdoc.CreateNode(XmlNodeType.Element, "UseSmoothing", null); xmlGyroMStickSmoothing.InnerText = store.gyroMStickInfo[device].useSmoothing.ToString(); xmlGyroMStickSmoothingElement.AppendChild(xmlGyroMStickSmoothing);
            XmlNode xmlGyroMStickSmoothingMethod = m_Xdoc.CreateNode(XmlNodeType.Element, "SmoothingMethod", null); xmlGyroMStickSmoothingMethod.InnerText = store.gyroMStickInfo[device].SmoothMethodIdentifier(); xmlGyroMStickSmoothingElement.AppendChild(xmlGyroMStickSmoothingMethod);
            XmlNode xmlGyroMStickSmoothWeight = m_Xdoc.CreateNode(XmlNodeType.Element, "SmoothingWeight", null); xmlGyroMStickSmoothWeight.InnerText = Convert.ToInt32(store.gyroMStickInfo[device].smoothWeight * 100).ToString(); xmlGyroMStickSmoothingElement.AppendChild(xmlGyroMStickSmoothWeight);
            XmlNode xmlGyroMStickSmoothMincutoff = m_Xdoc.CreateNode(XmlNodeType.Element, "SmoothingMinCutoff", null); xmlGyroMStickSmoothMincutoff.InnerText = store.gyroMStickInfo[device].minCutoff.ToString(); xmlGyroMStickSmoothingElement.AppendChild(xmlGyroMStickSmoothMincutoff);
            XmlNode xmlGyroMStickSmoothBeta = m_Xdoc.CreateNode(XmlNodeType.Element, "SmoothingBeta", null); xmlGyroMStickSmoothBeta.InnerText = store.gyroMStickInfo[device].beta.ToString(); xmlGyroMStickSmoothingElement.AppendChild(xmlGyroMStickSmoothBeta);
            rootElement.AppendChild(xmlGyroMStickSmoothingElement);

            // Phase8-Step8 8-0: 廃止された「ジャイロスワイプ」の設定は、保持した内容をそのまま書き出す（なければ、従来と同じ既定値の要素）
            store.removedGyroSwipeSettings[device].AppendSettingsTo(m_Xdoc, rootElement);

            // Ensure profileActions list exists. Do NOT auto-add "Disconnect Controller"
            // here — respect the stored list so users can intentionally disable
            // the Bluetooth-disconnect special action for an existing profile.
            if (store.profileActions[device] == null)
            {
                store.profileActions[device] = new List<string>();
            }
        }
    }
}
