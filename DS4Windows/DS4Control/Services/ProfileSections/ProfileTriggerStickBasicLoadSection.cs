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
    /// Phase8-Step4b W3（Phase8-Step4b-Plan.md §3.2）: トリガー（L2/R2）の基本設定、スティックの回転・不感帯・最大域・外周・ボタンのマウス・レインボー（LeftTriggerMiddle〜）の読込。
    /// <c>LegacyProfileXmlSerializer.LoadProfile</c> から移したもの。移動のみで、要素の順序・既定値・例外の扱いは変えない。
    /// </summary>
    internal sealed class ProfileTriggerStickBasicLoadSection : IProfileLoadSection
    {
        public void Load(ProfileLoadContext context)
        {
            BackingStore store = context.Store;
            XmlDocument m_Xdoc = context.Xdoc;
            string rootname = context.RootName;
            int device = context.Device;
            LightbarDS4WinInfo lightInfo = context.LightInfo;
            LightbarSettingInfo lightbarSettings = context.LightbarSettings;
            bool missingSetting = context.MissingSetting;
            XmlNode Item;

            try { Item = m_Xdoc.SelectSingleNode("/" + rootname + "/LeftTriggerMiddle"); byte.TryParse(Item.InnerText, out store.l2ModInfo[device].deadZone); }
            catch { missingSetting = true; }
            try { Item = m_Xdoc.SelectSingleNode("/" + rootname + "/RightTriggerMiddle"); byte.TryParse(Item.InnerText, out store.r2ModInfo[device].deadZone); }
            catch { missingSetting = true; }

            try { Item = m_Xdoc.SelectSingleNode("/" + rootname + "/L2AntiDeadZone"); int.TryParse(Item.InnerText, out store.l2ModInfo[device].antiDeadZone); }
            catch { store.l2ModInfo[device].antiDeadZone = 0; missingSetting = true; }

            try { Item = m_Xdoc.SelectSingleNode("/" + rootname + "/R2AntiDeadZone"); int.TryParse(Item.InnerText, out store.r2ModInfo[device].antiDeadZone); }
            catch { store.r2ModInfo[device].antiDeadZone = 0; missingSetting = true; }

            try
            {
                Item = m_Xdoc.SelectSingleNode("/" + rootname + "/L2MaxZone"); int temp = 100;
                int.TryParse(Item.InnerText, out temp);
                store.l2ModInfo[device].maxZone = Math.Min(Math.Max(temp, 0), 100);
            }
            catch { store.l2ModInfo[device].maxZone = 100; missingSetting = true; }

            try
            {
                Item = m_Xdoc.SelectSingleNode("/" + rootname + "/R2MaxZone"); int temp = 100;
                int.TryParse(Item.InnerText, out temp);
                store.r2ModInfo[device].maxZone = Math.Min(Math.Max(temp, 0), 100);
            }
            catch { store.r2ModInfo[device].maxZone = 100; missingSetting = true; }

            try
            {
                Item = m_Xdoc.SelectSingleNode("/" + rootname + "/L2MaxOutput"); double temp = 100.0;
                temp = double.Parse(Item.InnerText);
                store.l2ModInfo[device].maxOutput = Math.Min(Math.Max(temp, 0.0), 100.0);
            }
            catch { store.l2ModInfo[device].maxOutput = 100.0; missingSetting = true; }

            try
            {
                Item = m_Xdoc.SelectSingleNode("/" + rootname + "/R2MaxOutput"); double temp = 100.0;
                temp = double.Parse(Item.InnerText);
                store.r2ModInfo[device].maxOutput = Math.Min(Math.Max(temp, 0.0), 100.0);
            }
            catch { store.r2ModInfo[device].maxOutput = 100.0; missingSetting = true; }

            try
            {
                // Take a rotation angle (degrees) and convert to radians
                Item = m_Xdoc.SelectSingleNode("/" + rootname + "/LSRotation"); int tempDegrees = 0;
                int.TryParse(Item.InnerText, out tempDegrees);
                tempDegrees = Math.Min(Math.Max(tempDegrees, -180), 180);
                store.LSRotation[device] = tempDegrees * Math.PI / 180.0;
            }
            catch { store.LSRotation[device] = 0.0; missingSetting = true; }

            try
            {
                // Take a rotation angle (degrees) and convert to radians
                Item = m_Xdoc.SelectSingleNode("/" + rootname + "/RSRotation"); int tempDegrees = 0;
                int.TryParse(Item.InnerText, out tempDegrees);
                tempDegrees = Math.Min(Math.Max(tempDegrees, -180), 180);
                store.RSRotation[device] = tempDegrees * Math.PI / 180.0;
            }
            catch { store.RSRotation[device] = 0.0; missingSetting = true; }

            try
            {
                Item = m_Xdoc.SelectSingleNode("/" + rootname + "/LSFuzz"); int temp = 0;
                int.TryParse(Item.InnerText, out temp);
                temp = Math.Min(Math.Max(temp, 0), 100);
                store.lsModInfo[device].fuzz = temp;
            }
            catch { store.lsModInfo[device].fuzz = StickDeadZoneInfo.DEFAULT_FUZZ; missingSetting = true; }

            try
            {
                Item = m_Xdoc.SelectSingleNode("/" + rootname + "/RSFuzz"); int temp = 0;
                int.TryParse(Item.InnerText, out temp);
                temp = Math.Min(Math.Max(temp, 0), 100);
                store.rsModInfo[device].fuzz = temp;
            }
            catch { store.rsModInfo[device].fuzz = StickDeadZoneInfo.DEFAULT_FUZZ; missingSetting = true; }

            try { Item = m_Xdoc.SelectSingleNode("/" + rootname + "/ButtonMouseSensitivity"); int.TryParse(Item.InnerText, out store.buttonMouseInfos[device].buttonSensitivity); }
            catch { missingSetting = true; }

            try { Item = m_Xdoc.SelectSingleNode("/" + rootname + "/ButtonMouseOffset"); double.TryParse(Item.InnerText, out store.buttonMouseInfos[device].mouseVelocityOffset); }
            catch { missingSetting = true; }

            try
            {
                Item = m_Xdoc.SelectSingleNode("/" + rootname + "/ButtonMouseVerticalScale");
                int.TryParse(Item.InnerText, out int temp);
                store.buttonMouseInfos[device].buttonVerticalScale = Math.Min(Math.Max(temp, 0), 500) * 0.01;
            }
            catch { missingSetting = true; }

            try { Item = m_Xdoc.SelectSingleNode("/" + rootname + "/Rainbow"); double.TryParse(Item.InnerText, out lightInfo.rainbow); }
            catch { lightInfo.rainbow = 0; missingSetting = true; }
            try
            {
                Item = m_Xdoc.SelectSingleNode("/" + rootname + "/MaxSatRainbow");
                int.TryParse(Item.InnerText, out int temp);
                lightInfo.maxRainbowSat = Math.Max(0, Math.Min(100, temp)) / 100.0;
            }
            catch { lightInfo.maxRainbowSat = 1.0; missingSetting = true; }

            try
            {
                Item = m_Xdoc.SelectSingleNode("/" + rootname + "/LSDeadZone");
                int.TryParse(Item.InnerText, out int temp);
                temp = Math.Min(Math.Max(temp, 0), 127);
                store.lsModInfo[device].deadZone = temp;
            }
            catch
            {
                store.lsModInfo[device].deadZone = 10; missingSetting = true;
            }

            try
            {
                Item = m_Xdoc.SelectSingleNode("/" + rootname + "/RSDeadZone");
                int.TryParse(Item.InnerText, out int temp);
                temp = Math.Min(Math.Max(temp, 0), 127);
                store.rsModInfo[device].deadZone = temp;
            }
            catch
            {
                store.rsModInfo[device].deadZone = 10; missingSetting = true;
            }

            try { Item = m_Xdoc.SelectSingleNode("/" + rootname + "/LSAntiDeadZone"); int.TryParse(Item.InnerText, out store.lsModInfo[device].antiDeadZone); }
            catch { store.lsModInfo[device].antiDeadZone = 20; missingSetting = true; }
            try { Item = m_Xdoc.SelectSingleNode("/" + rootname + "/RSAntiDeadZone"); int.TryParse(Item.InnerText, out store.rsModInfo[device].antiDeadZone); }
            catch { store.rsModInfo[device].antiDeadZone = 20; missingSetting = true; }

            try
            {
                Item = m_Xdoc.SelectSingleNode("/" + rootname + "/LSMaxZone"); int temp = 100;
                int.TryParse(Item.InnerText, out temp);
                store.lsModInfo[device].maxZone = Math.Min(Math.Max(temp, 0), 100);
            }
            catch { store.lsModInfo[device].maxZone = 100; missingSetting = true; }

            try
            {
                Item = m_Xdoc.SelectSingleNode("/" + rootname + "/RSMaxZone"); int temp = 100;
                int.TryParse(Item.InnerText, out temp);
                store.rsModInfo[device].maxZone = Math.Min(Math.Max(temp, 0), 100);
            }
            catch { store.rsModInfo[device].maxZone = 100; missingSetting = true; }

            try
            {
                Item = m_Xdoc.SelectSingleNode("/" + rootname + "/LSVerticalScale");
                if (double.TryParse(Item?.InnerText ?? "", out double temp))
                {
                    store.lsModInfo[device].verticalScale = Math.Min(Math.Max(temp, 0.0), 200.0);
                }
            }
            catch { store.lsModInfo[device].verticalScale = StickDeadZoneInfo.DEFAULT_VERTICAL_SCALE; }

            try
            {
                Item = m_Xdoc.SelectSingleNode("/" + rootname + "/LSMaxOutput"); double temp = 100.0;
                temp = double.Parse(Item.InnerText);
                store.lsModInfo[device].maxOutput = Math.Min(Math.Max(temp, 0.0), 100.0);
            }
            catch { store.lsModInfo[device].maxOutput = 100.0; missingSetting = true; }

            try
            {
                Item = m_Xdoc.SelectSingleNode("/" + rootname + "/LSMaxOutputForce");
                if (bool.TryParse(Item?.InnerText ?? "", out bool temp))
                {
                    store.lsModInfo[device].maxOutputForce = temp;
                }
            }
            catch { store.lsModInfo[device].maxOutputForce = StickDeadZoneInfo.DEFAULT_MAXOUTPUT_FORCE; missingSetting = true; }

            try
            {
                Item = m_Xdoc.SelectSingleNode("/" + rootname + "/RSMaxOutput"); double temp = 100.0;
                temp = double.Parse(Item.InnerText);
                store.rsModInfo[device].maxOutput = Math.Min(Math.Max(temp, 0.0), 100.0);
            }
            catch { store.rsModInfo[device].maxOutput = 100; missingSetting = true; }

            try
            {
                Item = m_Xdoc.SelectSingleNode("/" + rootname + "/RSMaxOutputForce");
                if (bool.TryParse(Item?.InnerText ?? "", out bool temp))
                {
                    store.rsModInfo[device].maxOutputForce = temp;
                }
            }
            catch { store.rsModInfo[device].maxOutputForce = StickDeadZoneInfo.DEFAULT_MAXOUTPUT_FORCE; missingSetting = true; }

            try
            {
                Item = m_Xdoc.SelectSingleNode("/" + rootname + "/LSOuterBindDead");
                if (int.TryParse(Item?.InnerText ?? "", out int temp))
                {
                    temp = Math.Min(Math.Max(temp, 0), 100);
                    store.lsModInfo[device].outerBindDeadZone = temp;
                }
            }
            catch { store.lsModInfo[device].outerBindDeadZone = StickDeadZoneInfo.DEFAULT_OUTER_BIND_DEAD; }

            try
            {
                Item = m_Xdoc.SelectSingleNode("/" + rootname + "/RSOuterBindDead");
                if (int.TryParse(Item?.InnerText ?? "", out int temp))
                {
                    temp = Math.Min(Math.Max(temp, 0), 100);
                    store.rsModInfo[device].outerBindDeadZone = temp;
                }
            }
            catch { store.rsModInfo[device].outerBindDeadZone = StickDeadZoneInfo.DEFAULT_OUTER_BIND_DEAD; }

            try
            {
                Item = m_Xdoc.SelectSingleNode("/" + rootname + "/LSOuterBindInvert");
                if (bool.TryParse(Item?.InnerText ?? "", out bool temp))
                {
                    store.lsModInfo[device].outerBindInvert = temp;
                }
            }
            catch { store.lsModInfo[device].outerBindInvert = StickDeadZoneInfo.DEFAULT_OUTER_BIND_INVERT; }

            try
            {
                Item = m_Xdoc.SelectSingleNode("/" + rootname + "/RSOuterBindInvert");
                if (bool.TryParse(Item?.InnerText ?? "", out bool temp))
                {
                    store.rsModInfo[device].outerBindInvert = temp;
                }
            }
            catch { store.rsModInfo[device].outerBindInvert = StickDeadZoneInfo.DEFAULT_OUTER_BIND_INVERT; }

            try
            {
                Item = m_Xdoc.SelectSingleNode("/" + rootname + "/RSVerticalScale");
                if (double.TryParse(Item?.InnerText ?? "", out double temp))
                {
                    store.rsModInfo[device].verticalScale = Math.Min(Math.Max(temp, 0.0), 200.0);
                }
            }
            catch { store.rsModInfo[device].verticalScale = StickDeadZoneInfo.DEFAULT_VERTICAL_SCALE; }

            try
            {
                Item = m_Xdoc.SelectSingleNode("/" + rootname + "/LSDeadZoneType");
                if (Enum.TryParse(Item?.InnerText ?? "", out StickDeadZoneInfo.DeadZoneType temp))
                {
                    store.lsModInfo[device].deadzoneType = temp;
                }
            }
            catch { }

            context.MissingSetting = missingSetting;
        }
    }
}
