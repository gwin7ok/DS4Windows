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
    /// Phase8-Step4b W4（Phase8-Step4b-Plan.md §3.2）: SX/SZ（モーションの不感帯）・感度（SXDeadZone〜）の読込。
    /// <c>LegacyProfileXmlSerializer.LoadProfile</c> から移したもの。移動のみで、要素の順序・既定値・例外の扱いは変えない。
    /// </summary>
    internal sealed class ProfileMotionSensitivityLoadSection : IProfileLoadSection
    {
        public void Load(ProfileLoadContext context)
        {
            BackingStore store = context.Store;
            XmlDocument m_Xdoc = context.Xdoc;
            string rootname = context.RootName;
            int device = context.Device;
            bool missingSetting = context.MissingSetting;
            XmlNode Item;

            try
            {
                Item = m_Xdoc.SelectSingleNode("/" + rootname + "/SXDeadZone");
                { double.TryParse(Item.InnerText, out double parsedValue); store.sixAxisSlots[device].SXDeadZone = parsedValue; };
            }
            catch { store.sixAxisSlots[device].SXDeadZone = BackingStore.DEFAULT_SX_TILT_DEADZONE; missingSetting = true; }

            try
            {
                Item = m_Xdoc.SelectSingleNode("/" + rootname + "/SZDeadZone");
                { double.TryParse(Item.InnerText, out double parsedValue); store.sixAxisSlots[device].SZDeadZone = parsedValue; };
            }
            catch { store.sixAxisSlots[device].SZDeadZone = BackingStore.DEFAULT_SX_TILT_DEADZONE; missingSetting = true; }

            try
            {
                Item = m_Xdoc.SelectSingleNode("/" + rootname + "/SXMaxZone");
                int temp = 0;
                int.TryParse(Item.InnerText, out temp);
                store.sixAxisSlots[device].SXMaxZone = Math.Min(Math.Max(temp * 0.01, 0.0), 1.0);
            }
            catch { store.sixAxisSlots[device].SXMaxZone = 1.0; missingSetting = true; }

            try
            {
                Item = m_Xdoc.SelectSingleNode("/" + rootname + "/SZMaxZone");
                int temp = 0;
                int.TryParse(Item.InnerText, out temp);
                store.sixAxisSlots[device].SZMaxZone = Math.Min(Math.Max(temp * 0.01, 0.0), 1.0);
            }
            catch { store.sixAxisSlots[device].SZMaxZone = 1.0; missingSetting = true; }

            try
            {
                Item = m_Xdoc.SelectSingleNode("/" + rootname + "/SXAntiDeadZone");
                int temp = 0;
                int.TryParse(Item.InnerText, out temp);
                store.sixAxisSlots[device].SXAntiDeadZone = Math.Min(Math.Max(temp * 0.01, 0.0), 1.0);
            }
            catch { store.sixAxisSlots[device].SXAntiDeadZone = 0.0; missingSetting = true; }

            try
            {
                Item = m_Xdoc.SelectSingleNode("/" + rootname + "/SZAntiDeadZone");
                int temp = 0;
                int.TryParse(Item.InnerText, out temp);
                store.sixAxisSlots[device].SZAntiDeadZone = Math.Min(Math.Max(temp * 0.01, 0.0), 1.0);
            }
            catch { store.sixAxisSlots[device].SZAntiDeadZone = 0.0; missingSetting = true; }

            try
            {
                Item = m_Xdoc.SelectSingleNode("/" + rootname + "/Sensitivity");
                string[] s = Item.InnerText.Split('|');
                if (s.Length == 1)
                    s = Item.InnerText.Split(',');

                bool parsedOk1 = double.TryParse(s[0], out double parsedValue1); store.stickSlots[device].LSSens = parsedValue1;
                if (parsedOk1)
                {
                    store.stickSlots[device].LSSens = Math.Clamp(store.stickSlots[device].LSSens, 0.1, 5.0);
                }
                else
                {
                    store.stickSlots[device].LSSens = 1;
                }

                bool parsedOk2 = double.TryParse(s[1], out double parsedValue2); store.stickSlots[device].RSSens = parsedValue2;
                if (parsedOk2)
                {
                    store.stickSlots[device].RSSens = Math.Clamp(store.stickSlots[device].RSSens, 0.1, 5.0);
                }
                else
                {
                    store.stickSlots[device].RSSens = 1;
                }

                bool parsedOk3 = double.TryParse(s[2], out double parsedValue3); store.triggerSlots[device].L2Sens = parsedValue3;
                if (parsedOk3)
                {
                    store.triggerSlots[device].L2Sens = Math.Clamp(store.triggerSlots[device].L2Sens, 0.1, 10.0);
                }
                else
                {
                    store.triggerSlots[device].L2Sens = 1;
                }

                bool parsedOk4 = double.TryParse(s[3], out double parsedValue4); store.triggerSlots[device].R2Sens = parsedValue4;
                if (parsedOk4)
                {
                    store.triggerSlots[device].R2Sens = Math.Clamp(store.triggerSlots[device].R2Sens, 0.1, 10.0);
                }
                else
                {
                    store.triggerSlots[device].R2Sens = 1;
                }

                bool parsedOkSx = double.TryParse(s[4], out double parsedValueSx); store.sixAxisSlots[device].SXSens = parsedValueSx;
                if (parsedOkSx)
                {
                    store.sixAxisSlots[device].SXSens = Math.Clamp(store.sixAxisSlots[device].SXSens, 0.0, 5.0);
                }
                else
                {
                    store.sixAxisSlots[device].SXSens = 1;
                }

                bool parsedOkSz = double.TryParse(s[5], out double parsedValueSz); store.sixAxisSlots[device].SZSens = parsedValueSz;
                if (parsedOkSz)
                {
                    store.sixAxisSlots[device].SZSens = Math.Clamp(store.sixAxisSlots[device].SZSens, 0.0, 5.0);
                }
                else
                {
                    store.sixAxisSlots[device].SZSens = 1;
                }
            }
            catch { missingSetting = true; }

            context.MissingSetting = missingSetting;
        }
    }
}
