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
using DS4Windows.Core.Abstractions;
using DS4Windows.Core.Settings;

namespace DS4Windows.Core.Persistence.ProfileSections
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
            IProfileLoadTarget control = context.Control;
            bool missingSetting = context.MissingSetting;
            XmlNode Item;

            void PortOldGyroSettings(int device)
            {
                if (store.gyroSlots[device].OutputMode == GyroOutMode.None)
                {
                    store.gyroSlots[device].OutputMode = GyroOutMode.Controls;
                }
            }

            try { Item = m_Xdoc.SelectSingleNode("/" + rootname + "/SATriggers"); store.gyroSlots[device].MouseTriggers = Item.InnerText; }
            catch { store.gyroSlots[device].MouseTriggers = "-1"; missingSetting = true; }

            try { Item = m_Xdoc.SelectSingleNode("/" + rootname + "/SATriggerCond"); store.gyroSlots[device].MouseTriggerCondAnd = BackingStore.SaTriggerCondValue(Item.InnerText); }
            catch { store.gyroSlots[device].MouseTriggerCondAnd = true; missingSetting = true; }

            // Phase9 Steering 削除 W4: 旧「SASteeringWheelEmulationAxis／SASteeringWheelEmulationRange／SASteeringWheelFuzz／SASteeringWheelSmoothingOptions」（ステアリングの設定）は、機能ごと削除した。
            // 古いプロファイルに要素が残っていても、読み込まずに無視する（再保存すると、書かれなくなる）。

            try
            {
                Item = m_Xdoc.SelectSingleNode("/" + rootname + "/GyroOutputMode");
                string tempMode = Item.InnerText;
                // Phase8-Step8 8-0: 廃止された DirectionalSwipe のモードは、なかったことにして（None）、保持だけする
                store.gyroSlots[device].RemovedSwipe.CaptureGyroOutputMode(tempMode);
                //gyroSlots[device].OutputMode = GetGyroOutModeType(tempMode);
                { Enum.TryParse(tempMode, out GyroOutMode parsedMode); store.gyroSlots[device].OutputMode = parsedMode; };
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
                        store.gyroSlots[device].Controls.triggers = Item.InnerText;
                    }
                }
                catch { }

                try
                {
                    Item = xmlGyroControlsElement.SelectSingleNode("TriggerCond");
                    if (Item != null)
                    {
                        store.gyroSlots[device].Controls.triggerCond = BackingStore.SaTriggerCondValue(Item.InnerText);
                    }
                }
                catch { }

                try
                {
                    Item = xmlGyroControlsElement.SelectSingleNode("TriggerTurns");
                    if (bool.TryParse(Item?.InnerText ?? "", out bool tempTurns))
                    {
                        store.gyroSlots[device].Controls.triggerTurns = tempTurns;
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

            try { Item = m_Xdoc.SelectSingleNode("/" + rootname + "/GyroMouseStickTriggers"); store.gyroSlots[device].MouseStickTriggers = Item.InnerText; }
            catch { store.gyroSlots[device].MouseStickTriggers = "-1"; missingSetting = true; }

            try { Item = m_Xdoc.SelectSingleNode("/" + rootname + "/GyroMouseStickTriggerCond"); store.gyroSlots[device].MouseStickTriggerCondAnd = BackingStore.SaTriggerCondValue(Item.InnerText); }
            catch { store.gyroSlots[device].MouseStickTriggerCondAnd = true; missingSetting = true; }

            try { Item = m_Xdoc.SelectSingleNode("/" + rootname + "/GyroMouseStickTriggerTurns"); { bool.TryParse(Item.InnerText, out bool parsedValue); store.gyroSlots[device].MouseStickTriggerTurns = parsedValue; }; }
            catch { store.gyroSlots[device].MouseStickTriggerTurns = true; missingSetting = true; }

            try { Item = m_Xdoc.SelectSingleNode("/" + rootname + "/GyroMouseStickHAxis"); int temp = 0; int.TryParse(Item.InnerText, out temp); store.gyroSlots[device].MouseStickHorizontalAxis = Math.Min(Math.Max(0, temp), 1); }
            catch { store.gyroSlots[device].MouseStickHorizontalAxis = 0; missingSetting = true; }

            try
            {
                Item = m_Xdoc.SelectSingleNode("/" + rootname + "/GyroMouseStickDeadZone"); int.TryParse(Item.InnerText, out int temp);
                store.gyroSlots[device].MouseStick.deadZone = temp;
            }
            catch { store.gyroSlots[device].MouseStick.deadZone = 30; missingSetting = true; }

            try
            {
                Item = m_Xdoc.SelectSingleNode("/" + rootname + "/GyroMouseStickMaxZone"); int.TryParse(Item.InnerText, out int temp);
                store.gyroSlots[device].MouseStick.maxZone = Math.Max(temp, 1);
            }
            catch { store.gyroSlots[device].MouseStick.maxZone = 830; missingSetting = true; }

            try
            {
                Item = m_Xdoc.SelectSingleNode("/" + rootname + "/GyroMouseStickOutputStick");
                if (Enum.TryParse(Item?.InnerText ?? "", out GyroMouseStickInfo.OutputStick temp))
                {
                    store.gyroSlots[device].MouseStick.outputStick = temp;
                }
            }
            catch { }

            try
            {
                Item = m_Xdoc.SelectSingleNode("/" + rootname + "/GyroMouseStickOutputStickAxes");
                if (Enum.TryParse(Item?.InnerText ?? "", out GyroMouseStickInfo.OutputStickAxes temp))
                {
                    store.gyroSlots[device].MouseStick.outputStickDir = temp;
                }
            }
            catch { }

            try
            {
                Item = m_Xdoc.SelectSingleNode("/" + rootname + "/GyroMouseStickAntiDeadX"); double.TryParse(Item.InnerText, out double temp);
                store.gyroSlots[device].MouseStick.antiDeadX = temp;
            }
            catch { store.gyroSlots[device].MouseStick.antiDeadX = 0.4; missingSetting = true; }

            try
            {
                Item = m_Xdoc.SelectSingleNode("/" + rootname + "/GyroMouseStickAntiDeadY"); double.TryParse(Item.InnerText, out double temp);
                store.gyroSlots[device].MouseStick.antiDeadY = temp;
            }
            catch { store.gyroSlots[device].MouseStick.antiDeadY = 0.4; missingSetting = true; }

            try { Item = m_Xdoc.SelectSingleNode("/" + rootname + "/GyroMouseStickInvert"); uint.TryParse(Item.InnerText, out store.gyroSlots[device].MouseStick.inverted); }
            catch { store.gyroSlots[device].MouseStick.inverted = 0; missingSetting = true; }

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
                store.gyroSlots[device].MouseStick.maxOutput = Math.Min(Math.Max(temp, 0.0), 100.0);
            }
            catch { store.gyroSlots[device].MouseStick.maxOutput = 100.0; missingSetting = true; }

            try
            {
                Item = m_Xdoc.SelectSingleNode("/" + rootname + "/GyroMouseStickMaxOutputEnabled");
                bool.TryParse(Item.InnerText, out bool temp);
                store.gyroSlots[device].MouseStick.maxOutputEnabled = temp;
            }
            catch { store.gyroSlots[device].MouseStick.maxOutputEnabled = false; missingSetting = true; }

            try { Item = m_Xdoc.SelectSingleNode("/" + rootname + "/GyroMouseStickVerticalScale"); int.TryParse(Item.InnerText, out store.gyroSlots[device].MouseStick.vertScale); }
            catch { store.gyroSlots[device].MouseStick.vertScale = 100; missingSetting = true; }

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
                    store.gyroSlots[device].MouseStick.useSmoothing = tempSmoothing;
                }
                catch { store.gyroSlots[device].MouseStick.useSmoothing = false; missingSetting = true; }

                try
                {
                    Item = xmlGyroMStickSmoothingElement.SelectSingleNode("SmoothingMethod");
                    string temp = Item?.InnerText ?? GyroMouseStickInfo.DEFAULT_SMOOTH_TECHNIQUE;
                    store.gyroSlots[device].MouseStick.DetermineSmoothMethod(temp);
                }
                catch { store.gyroSlots[device].MouseStick.ResetSmoothing(); missingSetting = true; }

                try
                {
                    Item = xmlGyroMStickSmoothingElement.SelectSingleNode("SmoothingWeight");
                    int temp = 0; int.TryParse(Item.InnerText, out temp);
                    store.gyroSlots[device].MouseStick.smoothWeight = Math.Min(Math.Max(0.0, Convert.ToDouble(temp * 0.01)), 1.0);
                }
                catch { store.gyroSlots[device].MouseStick.smoothWeight = 0.5; missingSetting = true; }

                try
                {
                    Item = xmlGyroMStickSmoothingElement.SelectSingleNode("SmoothingMinCutoff");
                    double.TryParse(Item.InnerText, out double temp);
                    store.gyroSlots[device].MouseStick.minCutoff = Math.Min(Math.Max(0.0, temp), 100.0);
                }
                catch { store.gyroSlots[device].MouseStick.minCutoff = GyroMouseStickInfo.DEFAULT_MINCUTOFF; missingSetting = true; }

                try
                {
                    Item = xmlGyroMStickSmoothingElement.SelectSingleNode("SmoothingBeta");
                    double.TryParse(Item.InnerText, out double temp);
                    store.gyroSlots[device].MouseStick.beta = Math.Min(Math.Max(0.0, temp), 1.0);
                }
                catch { store.gyroSlots[device].MouseStick.beta = GyroMouseStickInfo.DEFAULT_BETA; missingSetting = true; }
            }
            else
            {
                missingSetting = true;
            }

            context.MissingSetting = missingSetting;
        }
    }
}
