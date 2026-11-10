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
using WpfScreenHelper;
using static DS4Windows.Core.Utilities.Util;
using DS4Windows.Core.Settings;

namespace DS4Windows.Core.Persistence.ProfileSections
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
            XmlNode xmlSATriggers = m_Xdoc.CreateNode(XmlNodeType.Element, "SATriggers", null); xmlSATriggers.InnerText = store.gyroSlots[device].MouseTriggers.ToString(); rootElement.AppendChild(xmlSATriggers);
            XmlNode xmlSATriggerCond = m_Xdoc.CreateNode(XmlNodeType.Element, "SATriggerCond", null); xmlSATriggerCond.InnerText = BackingStore.SaTriggerCondString(store.gyroSlots[device].MouseTriggerCondAnd); rootElement.AppendChild(xmlSATriggerCond);
            // Phase9 Steering 削除 W4: 旧「SASteeringWheelEmulationAxis／SASteeringWheelEmulationRange／SASteeringWheelFuzz／SASteeringWheelSmoothingOptions」（ステアリングの設定）の書き込みは、機能ごと削除した。

            XmlNode xmlTouchDisInvTriggers = m_Xdoc.CreateNode(XmlNodeType.Element, "TouchDisInvTriggers", null);
            string tempTouchDisInv = string.Join(",", store.touchDisInvertTriggers[device]);
            xmlTouchDisInvTriggers.InnerText = tempTouchDisInv;
            rootElement.AppendChild(xmlTouchDisInvTriggers);

            XmlNode xmlGyroSensitivity = m_Xdoc.CreateNode(XmlNodeType.Element, "GyroSensitivity", null); xmlGyroSensitivity.InnerText = store.gyroSlots[device].Sensitivity.ToString(); rootElement.AppendChild(xmlGyroSensitivity);
            XmlNode xmlGyroSensVerticalScale = m_Xdoc.CreateNode(XmlNodeType.Element, "GyroSensVerticalScale", null); xmlGyroSensVerticalScale.InnerText = store.gyroSlots[device].SensVerticalScale.ToString(); rootElement.AppendChild(xmlGyroSensVerticalScale);
            XmlNode xmlGyroInvert = m_Xdoc.CreateNode(XmlNodeType.Element, "GyroInvert", null); xmlGyroInvert.InnerText = store.gyroSlots[device].Invert.ToString(); rootElement.AppendChild(xmlGyroInvert);
            XmlNode xmlGyroTriggerTurns = m_Xdoc.CreateNode(XmlNodeType.Element, "GyroTriggerTurns", null); xmlGyroTriggerTurns.InnerText = store.gyroSlots[device].TriggerTurns.ToString(); rootElement.AppendChild(xmlGyroTriggerTurns);
            /*XmlNode xmlGyroSmoothWeight = m_Xdoc.CreateNode(XmlNodeType.Element, "GyroSmoothingWeight", null); xmlGyroSmoothWeight.InnerText = Convert.ToInt32(gyroSmoothWeight[device] * 100).ToString(); rootElement.AppendChild(xmlGyroSmoothWeight);
            XmlNode xmlGyroSmoothing = m_Xdoc.CreateNode(XmlNodeType.Element, "GyroSmoothing", null); xmlGyroSmoothing.InnerText = gyroSmoothing[device].ToString(); rootElement.AppendChild(xmlGyroSmoothing);
            */

            XmlElement xmlGyroControlsSettingsElement = m_Xdoc.CreateElement("GyroControlsSettings");
            XmlNode xmlGyroControlsTriggers = m_Xdoc.CreateNode(XmlNodeType.Element, "Triggers", null); xmlGyroControlsTriggers.InnerText = store.gyroSlots[device].Controls.triggers; xmlGyroControlsSettingsElement.AppendChild(xmlGyroControlsTriggers);
            XmlNode xmlGyroControlsTriggerCond = m_Xdoc.CreateNode(XmlNodeType.Element, "TriggerCond", null); xmlGyroControlsTriggerCond.InnerText = BackingStore.SaTriggerCondString(store.gyroSlots[device].Controls.triggerCond); xmlGyroControlsSettingsElement.AppendChild(xmlGyroControlsTriggerCond);
            XmlNode xmlGyroControlsTriggerTurns = m_Xdoc.CreateNode(XmlNodeType.Element, "TriggerTurns", null); xmlGyroControlsTriggerTurns.InnerText = store.gyroSlots[device].Controls.triggerTurns.ToString(); xmlGyroControlsSettingsElement.AppendChild(xmlGyroControlsTriggerTurns);
            XmlNode xmlGyroControlsToggle = m_Xdoc.CreateNode(XmlNodeType.Element, "Toggle", null); xmlGyroControlsToggle.InnerText = store.gyroSlots[device].Controls.triggerToggle.ToString(); xmlGyroControlsSettingsElement.AppendChild(xmlGyroControlsToggle);
            rootElement.AppendChild(xmlGyroControlsSettingsElement);

            XmlElement xmlGyroSmoothingElement = m_Xdoc.CreateElement("GyroMouseSmoothingSettings");
            XmlNode xmlGyroSmoothing = m_Xdoc.CreateNode(XmlNodeType.Element, "UseSmoothing", null); xmlGyroSmoothing.InnerText = store.gyroSlots[device].Mouse.enableSmoothing.ToString(); xmlGyroSmoothingElement.AppendChild(xmlGyroSmoothing);
            XmlNode xmlGyroSmoothingMethod = m_Xdoc.CreateNode(XmlNodeType.Element, "SmoothingMethod", null); xmlGyroSmoothingMethod.InnerText = store.gyroSlots[device].Mouse.SmoothMethodIdentifier(); xmlGyroSmoothingElement.AppendChild(xmlGyroSmoothingMethod);
            XmlNode xmlGyroSmoothWeight = m_Xdoc.CreateNode(XmlNodeType.Element, "SmoothingWeight", null); xmlGyroSmoothWeight.InnerText = Convert.ToInt32(store.gyroSlots[device].Mouse.smoothingWeight * 100).ToString(); xmlGyroSmoothingElement.AppendChild(xmlGyroSmoothWeight);
            XmlNode xmlGyroSmoothMincutoff = m_Xdoc.CreateNode(XmlNodeType.Element, "SmoothingMinCutoff", null); xmlGyroSmoothMincutoff.InnerText = store.gyroSlots[device].Mouse.minCutoff.ToString(); xmlGyroSmoothingElement.AppendChild(xmlGyroSmoothMincutoff);
            XmlNode xmlGyroSmoothBeta = m_Xdoc.CreateNode(XmlNodeType.Element, "SmoothingBeta", null); xmlGyroSmoothBeta.InnerText = store.gyroSlots[device].Mouse.beta.ToString(); xmlGyroSmoothingElement.AppendChild(xmlGyroSmoothBeta);
            rootElement.AppendChild(xmlGyroSmoothingElement);

            XmlNode xmlGyroMouseHAxis = m_Xdoc.CreateNode(XmlNodeType.Element, "GyroMouseHAxis", null); xmlGyroMouseHAxis.InnerText = store.gyroSlots[device].MouseHorizontalAxis.ToString(); rootElement.AppendChild(xmlGyroMouseHAxis);
            XmlNode xmlGyroMouseDZ = m_Xdoc.CreateNode(XmlNodeType.Element, "GyroMouseDeadZone", null); xmlGyroMouseDZ.InnerText = store.gyroSlots[device].MouseDeadZone.ToString(); rootElement.AppendChild(xmlGyroMouseDZ);
            XmlNode xmlGyroMinThreshold = m_Xdoc.CreateNode(XmlNodeType.Element, "GyroMouseMinThreshold", null); xmlGyroMinThreshold.InnerText = store.gyroSlots[device].Mouse.minThreshold.ToString(); rootElement.AppendChild(xmlGyroMinThreshold);
            XmlNode xmlGyroMouseToggle = m_Xdoc.CreateNode(XmlNodeType.Element, "GyroMouseToggle", null); xmlGyroMouseToggle.InnerText = store.gyroSlots[device].MouseToggle.ToString(); rootElement.AppendChild(xmlGyroMouseToggle);

            XmlNode xmlGyroOutMode = m_Xdoc.CreateNode(XmlNodeType.Element, "GyroOutputMode", null); xmlGyroOutMode.InnerText = store.gyroSlots[device].RemovedSwipe.GyroOutputModeTextForSave(store.gyroSlots[device].OutputMode.ToString()); rootElement.AppendChild(xmlGyroOutMode);
            XmlNode xmlGyroMStickTriggers = m_Xdoc.CreateNode(XmlNodeType.Element, "GyroMouseStickTriggers", null); xmlGyroMStickTriggers.InnerText = store.gyroSlots[device].MouseStickTriggers.ToString(); rootElement.AppendChild(xmlGyroMStickTriggers);
            XmlNode xmlGyroMStickTriggerCond = m_Xdoc.CreateNode(XmlNodeType.Element, "GyroMouseStickTriggerCond", null); xmlGyroMStickTriggerCond.InnerText = BackingStore.SaTriggerCondString(store.gyroSlots[device].MouseStickTriggerCondAnd); rootElement.AppendChild(xmlGyroMStickTriggerCond);
            XmlNode xmlGyroMStickTriggerTurns = m_Xdoc.CreateNode(XmlNodeType.Element, "GyroMouseStickTriggerTurns", null); xmlGyroMStickTriggerTurns.InnerText = store.gyroSlots[device].MouseStickTriggerTurns.ToString(); rootElement.AppendChild(xmlGyroMStickTriggerTurns);
            XmlNode xmlGyroMStickHAxis = m_Xdoc.CreateNode(XmlNodeType.Element, "GyroMouseStickHAxis", null); xmlGyroMStickHAxis.InnerText = store.gyroSlots[device].MouseStickHorizontalAxis.ToString(); rootElement.AppendChild(xmlGyroMStickHAxis);
            XmlNode xmlGyroMStickDZ = m_Xdoc.CreateNode(XmlNodeType.Element, "GyroMouseStickDeadZone", null); xmlGyroMStickDZ.InnerText = store.gyroSlots[device].MouseStick.deadZone.ToString(); rootElement.AppendChild(xmlGyroMStickDZ);
            XmlNode xmlGyroMStickMaxZ = m_Xdoc.CreateNode(XmlNodeType.Element, "GyroMouseStickMaxZone", null); xmlGyroMStickMaxZ.InnerText = store.gyroSlots[device].MouseStick.maxZone.ToString(); rootElement.AppendChild(xmlGyroMStickMaxZ);
            XmlNode xmlGyroMStickOutputStick = m_Xdoc.CreateNode(XmlNodeType.Element, "GyroMouseStickOutputStick", null); xmlGyroMStickOutputStick.InnerText = store.gyroSlots[device].MouseStick.outputStick.ToString(); rootElement.AppendChild(xmlGyroMStickOutputStick);
            XmlNode xmlGyroMStickOutputStickAxes = m_Xdoc.CreateNode(XmlNodeType.Element, "GyroMouseStickOutputStickAxes", null); xmlGyroMStickOutputStickAxes.InnerText = store.gyroSlots[device].MouseStick.outputStickDir.ToString(); rootElement.AppendChild(xmlGyroMStickOutputStickAxes);
            XmlNode xmlGyroMStickAntiDX = m_Xdoc.CreateNode(XmlNodeType.Element, "GyroMouseStickAntiDeadX", null); xmlGyroMStickAntiDX.InnerText = store.gyroSlots[device].MouseStick.antiDeadX.ToString(); rootElement.AppendChild(xmlGyroMStickAntiDX);
            XmlNode xmlGyroMStickAntiDY = m_Xdoc.CreateNode(XmlNodeType.Element, "GyroMouseStickAntiDeadY", null); xmlGyroMStickAntiDY.InnerText = store.gyroSlots[device].MouseStick.antiDeadY.ToString(); rootElement.AppendChild(xmlGyroMStickAntiDY);
            XmlNode xmlGyroMStickInvert = m_Xdoc.CreateNode(XmlNodeType.Element, "GyroMouseStickInvert", null); xmlGyroMStickInvert.InnerText = store.gyroSlots[device].MouseStick.inverted.ToString(); rootElement.AppendChild(xmlGyroMStickInvert);
            XmlNode xmlGyroMStickToggle = m_Xdoc.CreateNode(XmlNodeType.Element, "GyroMouseStickToggle", null); xmlGyroMStickToggle.InnerText = store.gyroSlots[device].MouseStickToggle.ToString(); rootElement.AppendChild(xmlGyroMStickToggle);
            XmlNode xmlGyroMStickMaxOutput = m_Xdoc.CreateNode(XmlNodeType.Element, "GyroMouseStickMaxOutput", null); xmlGyroMStickMaxOutput.InnerText = store.gyroSlots[device].MouseStick.maxOutput.ToString(); rootElement.AppendChild(xmlGyroMStickMaxOutput);
            XmlNode xmlGyroMStickMaxOutputEnabled = m_Xdoc.CreateNode(XmlNodeType.Element, "GyroMouseStickMaxOutputEnabled", null); xmlGyroMStickMaxOutputEnabled.InnerText = store.gyroSlots[device].MouseStick.maxOutputEnabled.ToString(); rootElement.AppendChild(xmlGyroMStickMaxOutputEnabled);
            XmlNode xmlGyroMStickVerticalScale = m_Xdoc.CreateNode(XmlNodeType.Element, "GyroMouseStickVerticalScale", null); xmlGyroMStickVerticalScale.InnerText = store.gyroSlots[device].MouseStick.vertScale.ToString(); rootElement.AppendChild(xmlGyroMStickVerticalScale);


            /*XmlNode xmlGyroMStickSmoothing = m_Xdoc.CreateNode(XmlNodeType.Element, "GyroMouseStickSmoothing", null); xmlGyroMStickSmoothing.InnerText = gyroSlots[device].MouseStick.useSmoothing.ToString(); rootElement.AppendChild(xmlGyroMStickSmoothing);
            XmlNode xmlGyroMStickSmoothWeight = m_Xdoc.CreateNode(XmlNodeType.Element, "GyroMouseStickSmoothingWeight", null); xmlGyroMStickSmoothWeight.InnerText = Convert.ToInt32(gyroSlots[device].MouseStick.smoothWeight * 100).ToString(); rootElement.AppendChild(xmlGyroMStickSmoothWeight);
            */
            XmlElement xmlGyroMStickSmoothingElement = m_Xdoc.CreateElement("GyroMouseStickSmoothingSettings");
            XmlNode xmlGyroMStickSmoothing = m_Xdoc.CreateNode(XmlNodeType.Element, "UseSmoothing", null); xmlGyroMStickSmoothing.InnerText = store.gyroSlots[device].MouseStick.useSmoothing.ToString(); xmlGyroMStickSmoothingElement.AppendChild(xmlGyroMStickSmoothing);
            XmlNode xmlGyroMStickSmoothingMethod = m_Xdoc.CreateNode(XmlNodeType.Element, "SmoothingMethod", null); xmlGyroMStickSmoothingMethod.InnerText = store.gyroSlots[device].MouseStick.SmoothMethodIdentifier(); xmlGyroMStickSmoothingElement.AppendChild(xmlGyroMStickSmoothingMethod);
            XmlNode xmlGyroMStickSmoothWeight = m_Xdoc.CreateNode(XmlNodeType.Element, "SmoothingWeight", null); xmlGyroMStickSmoothWeight.InnerText = Convert.ToInt32(store.gyroSlots[device].MouseStick.smoothWeight * 100).ToString(); xmlGyroMStickSmoothingElement.AppendChild(xmlGyroMStickSmoothWeight);
            XmlNode xmlGyroMStickSmoothMincutoff = m_Xdoc.CreateNode(XmlNodeType.Element, "SmoothingMinCutoff", null); xmlGyroMStickSmoothMincutoff.InnerText = store.gyroSlots[device].MouseStick.minCutoff.ToString(); xmlGyroMStickSmoothingElement.AppendChild(xmlGyroMStickSmoothMincutoff);
            XmlNode xmlGyroMStickSmoothBeta = m_Xdoc.CreateNode(XmlNodeType.Element, "SmoothingBeta", null); xmlGyroMStickSmoothBeta.InnerText = store.gyroSlots[device].MouseStick.beta.ToString(); xmlGyroMStickSmoothingElement.AppendChild(xmlGyroMStickSmoothBeta);
            rootElement.AppendChild(xmlGyroMStickSmoothingElement);

            // Phase8-Step8 8-0: 廃止された「ジャイロスワイプ」の設定は、保持した内容をそのまま書き出す（なければ、従来と同じ既定値の要素）
            store.gyroSlots[device].RemovedSwipe.AppendSettingsTo(m_Xdoc, rootElement);

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
