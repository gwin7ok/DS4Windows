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
    /// Phase8-Step4b W4（Phase8-Step4b-Plan.md §3.2）: ジャイロのスワイプ・タッチパッドの出力モード・タッチパッドの反転の無効トリガー・ジャイロの感度とマウス（GyroSwipeSettings〜GyroMouseToggle）の読込。TouchpadOutputMode は UseTPforControls が無いときだけ読む。
    /// <c>LegacyProfileXmlSerializer.LoadProfile</c> から移したもの。移動のみで、要素の順序・既定値・例外の扱いは変えない。
    /// </summary>
    internal sealed class ProfileGyroSwipeLoadSection : IProfileLoadSection
    {
        public void Load(ProfileLoadContext context)
        {
            BackingStore store = context.Store;
            XmlDocument m_Xdoc = context.Xdoc;
            string rootname = context.RootName;
            int device = context.Device;
            ControlService control = context.Control;
            bool tpForControlsPresent = context.TpForControlsPresent;
            bool missingSetting = context.MissingSetting;
            XmlNode Item;

            XmlNode xmlGyroSwipeElement =
                m_Xdoc.SelectSingleNode("/" + rootname + "/GyroSwipeSettings");
            if (xmlGyroSwipeElement != null)
            {
                try
                {
                    Item = xmlGyroSwipeElement.SelectSingleNode("DeadZoneX");
                    if (int.TryParse(Item?.InnerText ?? "", out int tempDead))
                    {
                        store.gyroSwipeInfo[device].deadzoneX = tempDead;
                    }
                }
                catch { }

                try
                {
                    Item = xmlGyroSwipeElement.SelectSingleNode("DeadZoneY");
                    if (int.TryParse(Item?.InnerText ?? "", out int tempDead))
                    {
                        store.gyroSwipeInfo[device].deadzoneY = tempDead;
                    }
                }
                catch { }

                try
                {
                    Item = xmlGyroSwipeElement.SelectSingleNode("Triggers");
                    if (Item != null)
                    {
                        store.gyroSwipeInfo[device].triggers = Item.InnerText;
                    }
                }
                catch { }

                try
                {
                    Item = xmlGyroSwipeElement.SelectSingleNode("TriggerCond");
                    if (Item != null)
                    {
                        store.gyroSwipeInfo[device].triggerCond = BackingStore.SaTriggerCondValue(Item.InnerText);
                    }
                }
                catch { }

                try
                {
                    Item = xmlGyroSwipeElement.SelectSingleNode("TriggerTurns");
                    if (bool.TryParse(Item?.InnerText ?? "", out bool tempTurns))
                    {
                        store.gyroSwipeInfo[device].triggerTurns = tempTurns;
                    }
                }
                catch { }

                try
                {
                    Item = xmlGyroSwipeElement.SelectSingleNode("XAxis");
                    if (Enum.TryParse(Item?.InnerText ?? "", out GyroDirectionalSwipeInfo.XAxisSwipe tempX))
                    {
                        store.gyroSwipeInfo[device].xAxis = tempX;
                    }
                }
                catch { }

                try
                {
                    Item = xmlGyroSwipeElement.SelectSingleNode("DelayTime");
                    if (int.TryParse(Item?.InnerText ?? "", out int tempDelay))
                    {
                        store.gyroSwipeInfo[device].delayTime = tempDelay;
                    }
                }
                catch { }
            }

            // Check for TouchpadOutputMode if UseTPforControls is not present in profile
            if (!tpForControlsPresent)
            {
                try
                {
                    Item = m_Xdoc.SelectSingleNode("/" + rootname + "/TouchpadOutputMode");
                    string tempMode = Item.InnerText;
                    Enum.TryParse(tempMode, out store.touchOutMode[device]);
                }
                catch { store.touchOutMode[device] = TouchpadOutMode.Mouse; missingSetting = true; }
            }

            try
            {
                Item = m_Xdoc.SelectSingleNode("/" + rootname + "/TouchDisInvTriggers");
                string[] triggers = Item.InnerText.Split(',');
                int temp = -1;
                List<int> tempIntList = new List<int>();
                for (int i = 0, arlen = triggers.Length; i < arlen; i++)
                {
                    if (int.TryParse(triggers[i], out temp))
                        tempIntList.Add(temp);
                }

                store.touchDisInvertTriggers[device] = tempIntList.ToArray();
            }
            catch { store.touchDisInvertTriggers[device] = new int[1] { -1 }; missingSetting = true; }

            try { Item = m_Xdoc.SelectSingleNode("/" + rootname + "/GyroSensitivity"); int.TryParse(Item.InnerText, out store.gyroSensitivity[device]); }
            catch { store.gyroSensitivity[device] = 100; missingSetting = true; }

            try { Item = m_Xdoc.SelectSingleNode("/" + rootname + "/GyroSensVerticalScale"); int.TryParse(Item.InnerText, out store.gyroSensVerticalScale[device]); }
            catch { store.gyroSensVerticalScale[device] = 100; missingSetting = true; }

            try { Item = m_Xdoc.SelectSingleNode("/" + rootname + "/GyroInvert"); int.TryParse(Item.InnerText, out store.gyroInvert[device]); }
            catch { store.gyroInvert[device] = 0; missingSetting = true; }

            try { Item = m_Xdoc.SelectSingleNode("/" + rootname + "/GyroTriggerTurns"); bool.TryParse(Item.InnerText, out store.gyroTriggerTurns[device]); }
            catch { store.gyroTriggerTurns[device] = true; missingSetting = true; }

            /*try { Item = m_Xdoc.SelectSingleNode("/" + rootname + "/GyroSmoothing"); bool.TryParse(Item.InnerText, out gyroSmoothing[device]); }
            catch { gyroSmoothing[device] = false; missingSetting = true; }

            try { Item = m_Xdoc.SelectSingleNode("/" + rootname + "/GyroSmoothingWeight"); int temp = 0; int.TryParse(Item.InnerText, out temp); gyroSmoothWeight[device] = Math.Min(Math.Max(0.0, Convert.ToDouble(temp * 0.01)), 1.0); }
            catch { gyroSmoothWeight[device] = 0.5; missingSetting = true; }
            */

            bool gyroSmoothingGroup = false;
            XmlNode xmlGyroSmoothingElement =
                m_Xdoc.SelectSingleNode("/" + rootname + "/GyroMouseSmoothingSettings");
            gyroSmoothingGroup = xmlGyroSmoothingElement != null;

            if (gyroSmoothingGroup)
            {
                try
                {
                    Item = xmlGyroSmoothingElement.SelectSingleNode("UseSmoothing");
                    bool.TryParse(Item.InnerText, out bool temp);
                    store.gyroMouseInfo[device].enableSmoothing = temp;
                }
                catch { store.gyroMouseInfo[device].enableSmoothing = false; missingSetting = true; }

                try
                {
                    Item = xmlGyroSmoothingElement.SelectSingleNode("SmoothingMethod");
                    string temp = Item?.InnerText ?? GyroMouseInfo.DEFAULT_SMOOTH_TECHNIQUE;
                    store.gyroMouseInfo[device].DetermineSmoothMethod(temp);
                }
                catch { store.gyroMouseInfo[device].ResetSmoothing(); missingSetting = true; }

                try
                {
                    Item = xmlGyroSmoothingElement.SelectSingleNode("SmoothingWeight");
                    int.TryParse(Item.InnerText, out int temp);
                    store.gyroMouseInfo[device].smoothingWeight = Math.Min(Math.Max(0.0, Convert.ToDouble(temp * 0.01)), 1.0);
                }
                catch { store.gyroMouseInfo[device].smoothingWeight = 0.5; missingSetting = true; }

                try
                {
                    Item = xmlGyroSmoothingElement.SelectSingleNode("SmoothingMinCutoff");
                    double.TryParse(Item.InnerText, out double temp);
                    store.gyroMouseInfo[device].minCutoff = Math.Min(Math.Max(0.0, temp), 100.0);
                }
                catch { store.gyroMouseInfo[device].minCutoff = GyroMouseInfo.DEFAULT_MINCUTOFF; missingSetting = true; }

                try
                {
                    Item = xmlGyroSmoothingElement.SelectSingleNode("SmoothingBeta");
                    double.TryParse(Item.InnerText, out double temp);
                    store.gyroMouseInfo[device].beta = Math.Min(Math.Max(0.0, temp), 1.0);
                }
                catch { store.gyroMouseInfo[device].beta = GyroMouseInfo.DEFAULT_BETA; missingSetting = true; }
            }
            else
            {
                missingSetting = true;
            }

            try { Item = m_Xdoc.SelectSingleNode("/" + rootname + "/GyroMouseHAxis"); int temp = 0; int.TryParse(Item.InnerText, out temp); store.gyroMouseHorizontalAxis[device] = Math.Min(Math.Max(0, temp), 1); }
            catch { store.gyroMouseHorizontalAxis[device] = 0; missingSetting = true; }

            try
            {
                Item = m_Xdoc.SelectSingleNode("/" + rootname + "/GyroMouseDeadZone"); int.TryParse(Item.InnerText, out int temp);
                store.SetGyroMouseDZ(device, temp, control);
            }
            catch { store.SetGyroMouseDZ(device, MouseCursor.GYRO_MOUSE_DEADZONE, control); missingSetting = true; }

            try
            {
                Item = m_Xdoc.SelectSingleNode("/" + rootname + "/GyroMouseMinThreshold");
                double.TryParse(Item.InnerText, out double temp);
                temp = Math.Min(Math.Max(temp, 1.0), 40.0);
                store.gyroMouseInfo[device].minThreshold = temp;
            }
            catch { store.gyroMouseInfo[device].minThreshold = GyroMouseInfo.DEFAULT_MIN_THRESHOLD; missingSetting = true; }

            try
            {
                Item = m_Xdoc.SelectSingleNode("/" + rootname + "/GyroMouseToggle");
                bool.TryParse(Item.InnerText, out bool temp);
                store.SetGyroMouseToggle(device, temp, control);
            }
            catch
            {
                store.SetGyroMouseToggle(device, false, control);
                missingSetting = true;
            }

            context.MissingSetting = missingSetting;
        }
    }
}
