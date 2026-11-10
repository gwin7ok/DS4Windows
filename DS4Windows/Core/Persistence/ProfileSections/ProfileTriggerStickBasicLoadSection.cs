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

            try { Item = m_Xdoc.SelectSingleNode("/" + rootname + "/LeftTriggerMiddle"); byte.TryParse(Item.InnerText, out store.triggerSlots[device].L2Mod.deadZone); }
            catch { missingSetting = true; }
            try { Item = m_Xdoc.SelectSingleNode("/" + rootname + "/RightTriggerMiddle"); byte.TryParse(Item.InnerText, out store.triggerSlots[device].R2Mod.deadZone); }
            catch { missingSetting = true; }

            try { Item = m_Xdoc.SelectSingleNode("/" + rootname + "/L2AntiDeadZone"); int.TryParse(Item.InnerText, out store.triggerSlots[device].L2Mod.antiDeadZone); }
            catch { store.triggerSlots[device].L2Mod.antiDeadZone = 0; missingSetting = true; }

            try { Item = m_Xdoc.SelectSingleNode("/" + rootname + "/R2AntiDeadZone"); int.TryParse(Item.InnerText, out store.triggerSlots[device].R2Mod.antiDeadZone); }
            catch { store.triggerSlots[device].R2Mod.antiDeadZone = 0; missingSetting = true; }

            try
            {
                Item = m_Xdoc.SelectSingleNode("/" + rootname + "/L2MaxZone"); int temp = 100;
                int.TryParse(Item.InnerText, out temp);
                store.triggerSlots[device].L2Mod.maxZone = Math.Min(Math.Max(temp, 0), 100);
            }
            catch { store.triggerSlots[device].L2Mod.maxZone = 100; missingSetting = true; }

            try
            {
                Item = m_Xdoc.SelectSingleNode("/" + rootname + "/R2MaxZone"); int temp = 100;
                int.TryParse(Item.InnerText, out temp);
                store.triggerSlots[device].R2Mod.maxZone = Math.Min(Math.Max(temp, 0), 100);
            }
            catch { store.triggerSlots[device].R2Mod.maxZone = 100; missingSetting = true; }

            try
            {
                Item = m_Xdoc.SelectSingleNode("/" + rootname + "/L2MaxOutput"); double temp = 100.0;
                temp = double.Parse(Item.InnerText);
                store.triggerSlots[device].L2Mod.maxOutput = Math.Min(Math.Max(temp, 0.0), 100.0);
            }
            catch { store.triggerSlots[device].L2Mod.maxOutput = 100.0; missingSetting = true; }

            try
            {
                Item = m_Xdoc.SelectSingleNode("/" + rootname + "/R2MaxOutput"); double temp = 100.0;
                temp = double.Parse(Item.InnerText);
                store.triggerSlots[device].R2Mod.maxOutput = Math.Min(Math.Max(temp, 0.0), 100.0);
            }
            catch { store.triggerSlots[device].R2Mod.maxOutput = 100.0; missingSetting = true; }

            try
            {
                // Take a rotation angle (degrees) and convert to radians
                Item = m_Xdoc.SelectSingleNode("/" + rootname + "/LSRotation"); int tempDegrees = 0;
                int.TryParse(Item.InnerText, out tempDegrees);
                tempDegrees = Math.Min(Math.Max(tempDegrees, -180), 180);
                store.stickSlots[device].LSRotation = tempDegrees * Math.PI / 180.0;
            }
            catch { store.stickSlots[device].LSRotation = 0.0; missingSetting = true; }

            try
            {
                // Take a rotation angle (degrees) and convert to radians
                Item = m_Xdoc.SelectSingleNode("/" + rootname + "/RSRotation"); int tempDegrees = 0;
                int.TryParse(Item.InnerText, out tempDegrees);
                tempDegrees = Math.Min(Math.Max(tempDegrees, -180), 180);
                store.stickSlots[device].RSRotation = tempDegrees * Math.PI / 180.0;
            }
            catch { store.stickSlots[device].RSRotation = 0.0; missingSetting = true; }

            try
            {
                Item = m_Xdoc.SelectSingleNode("/" + rootname + "/LSFuzz"); int temp = 0;
                int.TryParse(Item.InnerText, out temp);
                temp = Math.Min(Math.Max(temp, 0), 100);
                store.stickSlots[device].LSMod.fuzz = temp;
            }
            catch { store.stickSlots[device].LSMod.fuzz = StickDeadZoneInfo.DEFAULT_FUZZ; missingSetting = true; }

            try
            {
                Item = m_Xdoc.SelectSingleNode("/" + rootname + "/RSFuzz"); int temp = 0;
                int.TryParse(Item.InnerText, out temp);
                temp = Math.Min(Math.Max(temp, 0), 100);
                store.stickSlots[device].RSMod.fuzz = temp;
            }
            catch { store.stickSlots[device].RSMod.fuzz = StickDeadZoneInfo.DEFAULT_FUZZ; missingSetting = true; }

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
                store.stickSlots[device].LSMod.deadZone = temp;
            }
            catch
            {
                store.stickSlots[device].LSMod.deadZone = 10; missingSetting = true;
            }

            try
            {
                Item = m_Xdoc.SelectSingleNode("/" + rootname + "/RSDeadZone");
                int.TryParse(Item.InnerText, out int temp);
                temp = Math.Min(Math.Max(temp, 0), 127);
                store.stickSlots[device].RSMod.deadZone = temp;
            }
            catch
            {
                store.stickSlots[device].RSMod.deadZone = 10; missingSetting = true;
            }

            try { Item = m_Xdoc.SelectSingleNode("/" + rootname + "/LSAntiDeadZone"); int.TryParse(Item.InnerText, out store.stickSlots[device].LSMod.antiDeadZone); }
            catch { store.stickSlots[device].LSMod.antiDeadZone = 20; missingSetting = true; }
            try { Item = m_Xdoc.SelectSingleNode("/" + rootname + "/RSAntiDeadZone"); int.TryParse(Item.InnerText, out store.stickSlots[device].RSMod.antiDeadZone); }
            catch { store.stickSlots[device].RSMod.antiDeadZone = 20; missingSetting = true; }

            try
            {
                Item = m_Xdoc.SelectSingleNode("/" + rootname + "/LSMaxZone"); int temp = 100;
                int.TryParse(Item.InnerText, out temp);
                store.stickSlots[device].LSMod.maxZone = Math.Min(Math.Max(temp, 0), 100);
            }
            catch { store.stickSlots[device].LSMod.maxZone = 100; missingSetting = true; }

            try
            {
                Item = m_Xdoc.SelectSingleNode("/" + rootname + "/RSMaxZone"); int temp = 100;
                int.TryParse(Item.InnerText, out temp);
                store.stickSlots[device].RSMod.maxZone = Math.Min(Math.Max(temp, 0), 100);
            }
            catch { store.stickSlots[device].RSMod.maxZone = 100; missingSetting = true; }

            try
            {
                Item = m_Xdoc.SelectSingleNode("/" + rootname + "/LSVerticalScale");
                if (double.TryParse(Item?.InnerText ?? "", out double temp))
                {
                    store.stickSlots[device].LSMod.verticalScale = Math.Min(Math.Max(temp, 0.0), 200.0);
                }
            }
            catch { store.stickSlots[device].LSMod.verticalScale = StickDeadZoneInfo.DEFAULT_VERTICAL_SCALE; }

            try
            {
                Item = m_Xdoc.SelectSingleNode("/" + rootname + "/LSMaxOutput"); double temp = 100.0;
                temp = double.Parse(Item.InnerText);
                store.stickSlots[device].LSMod.maxOutput = Math.Min(Math.Max(temp, 0.0), 100.0);
            }
            catch { store.stickSlots[device].LSMod.maxOutput = 100.0; missingSetting = true; }

            try
            {
                Item = m_Xdoc.SelectSingleNode("/" + rootname + "/LSMaxOutputForce");
                if (bool.TryParse(Item?.InnerText ?? "", out bool temp))
                {
                    store.stickSlots[device].LSMod.maxOutputForce = temp;
                }
            }
            catch { store.stickSlots[device].LSMod.maxOutputForce = StickDeadZoneInfo.DEFAULT_MAXOUTPUT_FORCE; missingSetting = true; }

            try
            {
                Item = m_Xdoc.SelectSingleNode("/" + rootname + "/RSMaxOutput"); double temp = 100.0;
                temp = double.Parse(Item.InnerText);
                store.stickSlots[device].RSMod.maxOutput = Math.Min(Math.Max(temp, 0.0), 100.0);
            }
            catch { store.stickSlots[device].RSMod.maxOutput = 100; missingSetting = true; }

            try
            {
                Item = m_Xdoc.SelectSingleNode("/" + rootname + "/RSMaxOutputForce");
                if (bool.TryParse(Item?.InnerText ?? "", out bool temp))
                {
                    store.stickSlots[device].RSMod.maxOutputForce = temp;
                }
            }
            catch { store.stickSlots[device].RSMod.maxOutputForce = StickDeadZoneInfo.DEFAULT_MAXOUTPUT_FORCE; missingSetting = true; }

            try
            {
                Item = m_Xdoc.SelectSingleNode("/" + rootname + "/LSOuterBindDead");
                if (int.TryParse(Item?.InnerText ?? "", out int temp))
                {
                    temp = Math.Min(Math.Max(temp, 0), 100);
                    store.stickSlots[device].LSMod.outerBindDeadZone = temp;
                }
            }
            catch { store.stickSlots[device].LSMod.outerBindDeadZone = StickDeadZoneInfo.DEFAULT_OUTER_BIND_DEAD; }

            try
            {
                Item = m_Xdoc.SelectSingleNode("/" + rootname + "/RSOuterBindDead");
                if (int.TryParse(Item?.InnerText ?? "", out int temp))
                {
                    temp = Math.Min(Math.Max(temp, 0), 100);
                    store.stickSlots[device].RSMod.outerBindDeadZone = temp;
                }
            }
            catch { store.stickSlots[device].RSMod.outerBindDeadZone = StickDeadZoneInfo.DEFAULT_OUTER_BIND_DEAD; }

            try
            {
                Item = m_Xdoc.SelectSingleNode("/" + rootname + "/LSOuterBindInvert");
                if (bool.TryParse(Item?.InnerText ?? "", out bool temp))
                {
                    store.stickSlots[device].LSMod.outerBindInvert = temp;
                }
            }
            catch { store.stickSlots[device].LSMod.outerBindInvert = StickDeadZoneInfo.DEFAULT_OUTER_BIND_INVERT; }

            try
            {
                Item = m_Xdoc.SelectSingleNode("/" + rootname + "/RSOuterBindInvert");
                if (bool.TryParse(Item?.InnerText ?? "", out bool temp))
                {
                    store.stickSlots[device].RSMod.outerBindInvert = temp;
                }
            }
            catch { store.stickSlots[device].RSMod.outerBindInvert = StickDeadZoneInfo.DEFAULT_OUTER_BIND_INVERT; }

            try
            {
                Item = m_Xdoc.SelectSingleNode("/" + rootname + "/RSVerticalScale");
                if (double.TryParse(Item?.InnerText ?? "", out double temp))
                {
                    store.stickSlots[device].RSMod.verticalScale = Math.Min(Math.Max(temp, 0.0), 200.0);
                }
            }
            catch { store.stickSlots[device].RSMod.verticalScale = StickDeadZoneInfo.DEFAULT_VERTICAL_SCALE; }

            try
            {
                Item = m_Xdoc.SelectSingleNode("/" + rootname + "/LSDeadZoneType");
                if (Enum.TryParse(Item?.InnerText ?? "", out StickDeadZoneInfo.DeadZoneType temp))
                {
                    store.stickSlots[device].LSMod.deadzoneType = temp;
                }
            }
            catch { }

            context.MissingSetting = missingSetting;
        }
    }
}
