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
    /// Phase8-Step4b W4（Phase8-Step4b-Plan.md §3.2）: SA トリガー・SA ステアリング・ジャイロの制御・ジャイロのマウススティック（SATriggers〜GyroMouseStickSmoothingSettings）の読込。
    /// <c>LegacyProfileXmlSerializer.LoadProfile</c> から移したもの。移動のみで、要素の順序・既定値・例外の扱いは変えない。
    /// </summary>
    internal sealed class ProfileGyroControlsLoadSection : IProfileLoadSection
    {
        public void Load(ProfileLoadContext context)
        {
            BackingStore store = context.Store;
            XmlDocument m_Xdoc = context.Xdoc;
            string rootname = context.RootName;
            int device = context.Device;
            ControlService control = context.Control;
            bool missingSetting = context.MissingSetting;
            XmlNode Item;

            void PortOldGyroSettings(int device)
            {
                if (store.gyroOutMode[device] == GyroOutMode.None)
                {
                    store.gyroOutMode[device] = GyroOutMode.Controls;
                }
            }

            try { Item = m_Xdoc.SelectSingleNode("/" + rootname + "/SATriggers"); store.sATriggers[device] = Item.InnerText; }
            catch { store.sATriggers[device] = "-1"; missingSetting = true; }

            try { Item = m_Xdoc.SelectSingleNode("/" + rootname + "/SATriggerCond"); store.sATriggerCond[device] = BackingStore.SaTriggerCondValue(Item.InnerText); }
            catch { store.sATriggerCond[device] = true; missingSetting = true; }

            try { Item = m_Xdoc.SelectSingleNode("/" + rootname + "/SASteeringWheelEmulationAxis"); SASteeringWheelEmulationAxisType.TryParse(Item.InnerText, out store.sASteeringWheelEmulationAxis[device]); }
            catch { store.sASteeringWheelEmulationAxis[device] = SASteeringWheelEmulationAxisType.None; missingSetting = true; }

            try { Item = m_Xdoc.SelectSingleNode("/" + rootname + "/SASteeringWheelEmulationRange"); int.TryParse(Item.InnerText, out store.sASteeringWheelEmulationRange[device]); }
            catch { store.sASteeringWheelEmulationRange[device] = 360; missingSetting = true; }

            bool sASteeringWheelSmoothingGroup = false;
            XmlNode xmlSASteeringWheelSmoothElement =
                m_Xdoc.SelectSingleNode("/" + rootname + "/SASteeringWheelSmoothingOptions");
            sASteeringWheelSmoothingGroup = xmlSASteeringWheelSmoothElement != null;

            if (sASteeringWheelSmoothingGroup)
            {
                try
                {
                    Item = xmlSASteeringWheelSmoothElement.SelectSingleNode("SASteeringWheelUseSmoothing");
                    //Item = m_Xdoc.SelectSingleNode("/" + rootname + "/SASteeringWheelUseSmoothing");
                    bool.TryParse(Item.InnerText, out bool temp);
                    store.wheelSmoothInfo[device].Enabled = temp;
                }
                catch { store.wheelSmoothInfo[device].Reset(); missingSetting = true; }

                try
                {
                    Item = xmlSASteeringWheelSmoothElement.SelectSingleNode("SASteeringWheelSmoothMinCutoff");
                    //Item = m_Xdoc.SelectSingleNode("/" + rootname + "/SASteeringWheelSmoothMinCutoff");
                    double.TryParse(Item.InnerText, out double temp);
                    store.wheelSmoothInfo[device].MinCutoff = temp;
                }
                catch { store.wheelSmoothInfo[device].MinCutoff = OneEuroFilterPair.DEFAULT_WHEEL_CUTOFF; missingSetting = true; }

                try
                {
                    Item = xmlSASteeringWheelSmoothElement.SelectSingleNode("SASteeringWheelSmoothBeta");
                    //Item = m_Xdoc.SelectSingleNode("/" + rootname + "/SASteeringWheelSmoothBeta");
                    double.TryParse(Item.InnerText, out double temp);
                    store.wheelSmoothInfo[device].Beta = temp;
                }
                catch { store.wheelSmoothInfo[device].Beta = OneEuroFilterPair.DEFAULT_WHEEL_BETA; missingSetting = true; }
            }

            try
            {
                Item = m_Xdoc.SelectSingleNode("/" + rootname + "/SASteeringWheelFuzz");
                int.TryParse(Item.InnerText, out int temp);
                store.saWheelFuzzValues[device] = temp >= 0 && temp <= 100 ? temp : 0;
            }
            catch { store.saWheelFuzzValues[device] = 0; missingSetting = true; }

            try
            {
                Item = m_Xdoc.SelectSingleNode("/" + rootname + "/GyroOutputMode");
                string tempMode = Item.InnerText;
                //gyroOutMode[device] = GetGyroOutModeType(tempMode);
                Enum.TryParse(tempMode, out store.gyroOutMode[device]);
            }
            catch { PortOldGyroSettings(device); missingSetting = true; }

            XmlNode xmlGyroControlsElement =
                m_Xdoc.SelectSingleNode("/" + rootname + "/GyroControlsSettings");
            if (xmlGyroControlsElement != null)
            {
                try
                {
                    Item = xmlGyroControlsElement.SelectSingleNode("Triggers");
                    if (Item != null)
                    {
                        store.gyroControlsInf[device].triggers = Item.InnerText;
                    }
                }
                catch { }

                try
                {
                    Item = xmlGyroControlsElement.SelectSingleNode("TriggerCond");
                    if (Item != null)
                    {
                        store.gyroControlsInf[device].triggerCond = BackingStore.SaTriggerCondValue(Item.InnerText);
                    }
                }
                catch { }

                try
                {
                    Item = xmlGyroControlsElement.SelectSingleNode("TriggerTurns");
                    if (bool.TryParse(Item?.InnerText ?? "", out bool tempTurns))
                    {
                        store.gyroControlsInf[device].triggerTurns = tempTurns;
                    }
                }
                catch { }

                try
                {
                    Item = xmlGyroControlsElement.SelectSingleNode("Toggle");
                    if (bool.TryParse(Item?.InnerText ?? "", out bool tempToggle))
                    {
                        store.SetGyroControlsToggle(device, tempToggle, control);
                    }
                }
                catch { }
            }

            try { Item = m_Xdoc.SelectSingleNode("/" + rootname + "/GyroMouseStickTriggers"); store.sAMouseStickTriggers[device] = Item.InnerText; }
            catch { store.sAMouseStickTriggers[device] = "-1"; missingSetting = true; }

            try { Item = m_Xdoc.SelectSingleNode("/" + rootname + "/GyroMouseStickTriggerCond"); store.sAMouseStickTriggerCond[device] = BackingStore.SaTriggerCondValue(Item.InnerText); }
            catch { store.sAMouseStickTriggerCond[device] = true; missingSetting = true; }

            try { Item = m_Xdoc.SelectSingleNode("/" + rootname + "/GyroMouseStickTriggerTurns"); bool.TryParse(Item.InnerText, out store.gyroMouseStickTriggerTurns[device]); }
            catch { store.gyroMouseStickTriggerTurns[device] = true; missingSetting = true; }

            try { Item = m_Xdoc.SelectSingleNode("/" + rootname + "/GyroMouseStickHAxis"); int temp = 0; int.TryParse(Item.InnerText, out temp); store.gyroMouseStickHorizontalAxis[device] = Math.Min(Math.Max(0, temp), 1); }
            catch { store.gyroMouseStickHorizontalAxis[device] = 0; missingSetting = true; }

            try
            {
                Item = m_Xdoc.SelectSingleNode("/" + rootname + "/GyroMouseStickDeadZone"); int.TryParse(Item.InnerText, out int temp);
                store.gyroMStickInfo[device].deadZone = temp;
            }
            catch { store.gyroMStickInfo[device].deadZone = 30; missingSetting = true; }

            try
            {
                Item = m_Xdoc.SelectSingleNode("/" + rootname + "/GyroMouseStickMaxZone"); int.TryParse(Item.InnerText, out int temp);
                store.gyroMStickInfo[device].maxZone = Math.Max(temp, 1);
            }
            catch { store.gyroMStickInfo[device].maxZone = 830; missingSetting = true; }

            try
            {
                Item = m_Xdoc.SelectSingleNode("/" + rootname + "/GyroMouseStickOutputStick");
                if (Enum.TryParse(Item?.InnerText ?? "", out GyroMouseStickInfo.OutputStick temp))
                {
                    store.gyroMStickInfo[device].outputStick = temp;
                }
            }
            catch { }

            try
            {
                Item = m_Xdoc.SelectSingleNode("/" + rootname + "/GyroMouseStickOutputStickAxes");
                if (Enum.TryParse(Item?.InnerText ?? "", out GyroMouseStickInfo.OutputStickAxes temp))
                {
                    store.gyroMStickInfo[device].outputStickDir = temp;
                }
            }
            catch { }

            try
            {
                Item = m_Xdoc.SelectSingleNode("/" + rootname + "/GyroMouseStickAntiDeadX"); double.TryParse(Item.InnerText, out double temp);
                store.gyroMStickInfo[device].antiDeadX = temp;
            }
            catch { store.gyroMStickInfo[device].antiDeadX = 0.4; missingSetting = true; }

            try
            {
                Item = m_Xdoc.SelectSingleNode("/" + rootname + "/GyroMouseStickAntiDeadY"); double.TryParse(Item.InnerText, out double temp);
                store.gyroMStickInfo[device].antiDeadY = temp;
            }
            catch { store.gyroMStickInfo[device].antiDeadY = 0.4; missingSetting = true; }

            try { Item = m_Xdoc.SelectSingleNode("/" + rootname + "/GyroMouseStickInvert"); uint.TryParse(Item.InnerText, out store.gyroMStickInfo[device].inverted); }
            catch { store.gyroMStickInfo[device].inverted = 0; missingSetting = true; }

            try
            {
                Item = m_Xdoc.SelectSingleNode("/" + rootname + "/GyroMouseStickToggle");
                bool.TryParse(Item.InnerText, out bool temp);
                store.SetGyroMouseStickToggle(device, temp, control);
            }
            catch
            {
                store.SetGyroMouseStickToggle(device, false, control);
                missingSetting = true;
            }

            try
            {
                Item = m_Xdoc.SelectSingleNode("/" + rootname + "/GyroMouseStickMaxOutput"); double temp = 100.0;
                temp = double.Parse(Item.InnerText);
                store.gyroMStickInfo[device].maxOutput = Math.Min(Math.Max(temp, 0.0), 100.0);
            }
            catch { store.gyroMStickInfo[device].maxOutput = 100.0; missingSetting = true; }

            try
            {
                Item = m_Xdoc.SelectSingleNode("/" + rootname + "/GyroMouseStickMaxOutputEnabled");
                bool.TryParse(Item.InnerText, out bool temp);
                store.gyroMStickInfo[device].maxOutputEnabled = temp;
            }
            catch { store.gyroMStickInfo[device].maxOutputEnabled = false; missingSetting = true; }

            try { Item = m_Xdoc.SelectSingleNode("/" + rootname + "/GyroMouseStickVerticalScale"); int.TryParse(Item.InnerText, out store.gyroMStickInfo[device].vertScale); }
            catch { store.gyroMStickInfo[device].vertScale = 100; missingSetting = true; }

            bool gyroMStickSmoothingGroup = false;
            XmlNode xmlGyroMStickSmoothingElement =
                m_Xdoc.SelectSingleNode("/" + rootname + "/GyroMouseStickSmoothingSettings");
            gyroMStickSmoothingGroup = xmlGyroMStickSmoothingElement != null;

            if (gyroMStickSmoothingGroup)
            {
                try
                {
                    Item = xmlGyroMStickSmoothingElement.SelectSingleNode("UseSmoothing");
                    bool.TryParse(Item.InnerText, out bool tempSmoothing);
                    store.gyroMStickInfo[device].useSmoothing = tempSmoothing;
                }
                catch { store.gyroMStickInfo[device].useSmoothing = false; missingSetting = true; }

                try
                {
                    Item = xmlGyroMStickSmoothingElement.SelectSingleNode("SmoothingMethod");
                    string temp = Item?.InnerText ?? GyroMouseStickInfo.DEFAULT_SMOOTH_TECHNIQUE;
                    store.gyroMStickInfo[device].DetermineSmoothMethod(temp);
                }
                catch { store.gyroMStickInfo[device].ResetSmoothing(); missingSetting = true; }

                try
                {
                    Item = xmlGyroMStickSmoothingElement.SelectSingleNode("SmoothingWeight");
                    int temp = 0; int.TryParse(Item.InnerText, out temp);
                    store.gyroMStickInfo[device].smoothWeight = Math.Min(Math.Max(0.0, Convert.ToDouble(temp * 0.01)), 1.0);
                }
                catch { store.gyroMStickInfo[device].smoothWeight = 0.5; missingSetting = true; }

                try
                {
                    Item = xmlGyroMStickSmoothingElement.SelectSingleNode("SmoothingMinCutoff");
                    double.TryParse(Item.InnerText, out double temp);
                    store.gyroMStickInfo[device].minCutoff = Math.Min(Math.Max(0.0, temp), 100.0);
                }
                catch { store.gyroMStickInfo[device].minCutoff = GyroMouseStickInfo.DEFAULT_MINCUTOFF; missingSetting = true; }

                try
                {
                    Item = xmlGyroMStickSmoothingElement.SelectSingleNode("SmoothingBeta");
                    double.TryParse(Item.InnerText, out double temp);
                    store.gyroMStickInfo[device].beta = Math.Min(Math.Max(0.0, temp), 1.0);
                }
                catch { store.gyroMStickInfo[device].beta = GyroMouseStickInfo.DEFAULT_BETA; missingSetting = true; }
            }
            else
            {
                missingSetting = true;
            }

            context.MissingSetting = missingSetting;
        }
    }
}
