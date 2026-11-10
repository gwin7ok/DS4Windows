using DS4Windows.Processing.Touchpad;
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
using DS4Windows.Processing;

namespace DS4Windows.Core.Persistence.ProfileSections
{
    /// <summary>
    /// Phase8-Step4b W4（Phase8-Step4b-Plan.md §3.2）: ジャイロのスワイプ・タッチパッドの出力モード・タッチパッドの反転の無効トリガー・ジャイロの感度とマウス（GyroSwipeSettings〜GyroMouseToggle）の読込。TouchpadOutputMode は UseTPforControls が無いときだけ読む。
    /// Phase8-Step8 8-0: ジャイロスワイプの機能廃止に伴い、GyroSwipeSettings は保持だけ（<see cref="RemovedGyroSwipeSettings"/>）。名前を Swipe から Mouse に改めた。
    /// <c>LegacyProfileXmlSerializer.LoadProfile</c> から移したもの。移動のみで、要素の順序・既定値・例外の扱いは変えない。
    /// </summary>
    internal sealed class ProfileGyroMouseLoadSection : IProfileLoadSection
    {
        public void Load(ProfileLoadContext context)
        {
            BackingStore store = context.Store;
            XmlDocument m_Xdoc = context.Xdoc;
            string rootname = context.RootName;
            int device = context.Device;
            IProfileLoadTarget control = context.Control;
            bool tpForControlsPresent = context.TpForControlsPresent;
            bool missingSetting = context.MissingSetting;
            XmlNode Item;

            // Phase8-Step8 8-0: 廃止された「ジャイロスワイプ」の設定は解釈せず、既定値と異なるときだけ、そのまま保持する（編集画面で「残す」を選んだとき、保存で書き戻す）
            store.gyroSlots[device].RemovedSwipe.CaptureSettings(m_Xdoc.SelectSingleNode("/" + rootname + "/GyroSwipeSettings"));

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

            try { Item = m_Xdoc.SelectSingleNode("/" + rootname + "/GyroSensitivity"); { int.TryParse(Item.InnerText, out int parsedValue); store.gyroSlots[device].Sensitivity = parsedValue; }; }
            catch { store.gyroSlots[device].Sensitivity = 100; missingSetting = true; }

            try { Item = m_Xdoc.SelectSingleNode("/" + rootname + "/GyroSensVerticalScale"); { int.TryParse(Item.InnerText, out int parsedValue); store.gyroSlots[device].SensVerticalScale = parsedValue; }; }
            catch { store.gyroSlots[device].SensVerticalScale = 100; missingSetting = true; }

            try { Item = m_Xdoc.SelectSingleNode("/" + rootname + "/GyroInvert"); { int.TryParse(Item.InnerText, out int parsedValue); store.gyroSlots[device].Invert = parsedValue; }; }
            catch { store.gyroSlots[device].Invert = 0; missingSetting = true; }

            try { Item = m_Xdoc.SelectSingleNode("/" + rootname + "/GyroTriggerTurns"); { bool.TryParse(Item.InnerText, out bool parsedValue); store.gyroSlots[device].TriggerTurns = parsedValue; }; }
            catch { store.gyroSlots[device].TriggerTurns = true; missingSetting = true; }

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
                    store.gyroSlots[device].Mouse.enableSmoothing = temp;
                }
                catch { store.gyroSlots[device].Mouse.enableSmoothing = false; missingSetting = true; }

                try
                {
                    Item = xmlGyroSmoothingElement.SelectSingleNode("SmoothingMethod");
                    string temp = Item?.InnerText ?? GyroMouseInfo.DEFAULT_SMOOTH_TECHNIQUE;
                    store.gyroSlots[device].Mouse.DetermineSmoothMethod(temp);
                }
                catch { store.gyroSlots[device].Mouse.ResetSmoothing(); missingSetting = true; }

                try
                {
                    Item = xmlGyroSmoothingElement.SelectSingleNode("SmoothingWeight");
                    int.TryParse(Item.InnerText, out int temp);
                    store.gyroSlots[device].Mouse.smoothingWeight = Math.Min(Math.Max(0.0, Convert.ToDouble(temp * 0.01)), 1.0);
                }
                catch { store.gyroSlots[device].Mouse.smoothingWeight = 0.5; missingSetting = true; }

                try
                {
                    Item = xmlGyroSmoothingElement.SelectSingleNode("SmoothingMinCutoff");
                    double.TryParse(Item.InnerText, out double temp);
                    store.gyroSlots[device].Mouse.minCutoff = Math.Min(Math.Max(0.0, temp), 100.0);
                }
                catch { store.gyroSlots[device].Mouse.minCutoff = GyroMouseInfo.DEFAULT_MINCUTOFF; missingSetting = true; }

                try
                {
                    Item = xmlGyroSmoothingElement.SelectSingleNode("SmoothingBeta");
                    double.TryParse(Item.InnerText, out double temp);
                    store.gyroSlots[device].Mouse.beta = Math.Min(Math.Max(0.0, temp), 1.0);
                }
                catch { store.gyroSlots[device].Mouse.beta = GyroMouseInfo.DEFAULT_BETA; missingSetting = true; }
            }
            else
            {
                missingSetting = true;
            }

            try { Item = m_Xdoc.SelectSingleNode("/" + rootname + "/GyroMouseHAxis"); int temp = 0; int.TryParse(Item.InnerText, out temp); store.gyroSlots[device].MouseHorizontalAxis = Math.Min(Math.Max(0, temp), 1); }
            catch { store.gyroSlots[device].MouseHorizontalAxis = 0; missingSetting = true; }

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
                store.gyroSlots[device].Mouse.minThreshold = temp;
            }
            catch { store.gyroSlots[device].Mouse.minThreshold = GyroMouseInfo.DEFAULT_MIN_THRESHOLD; missingSetting = true; }

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
