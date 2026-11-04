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
                double.TryParse(Item.InnerText, out store.SXDeadzone[device]);
            }
            catch { store.SXDeadzone[device] = BackingStore.DEFAULT_SX_TILT_DEADZONE; missingSetting = true; }

            try
            {
                Item = m_Xdoc.SelectSingleNode("/" + rootname + "/SZDeadZone");
                double.TryParse(Item.InnerText, out store.SZDeadzone[device]);
            }
            catch { store.SZDeadzone[device] = BackingStore.DEFAULT_SX_TILT_DEADZONE; missingSetting = true; }

            try
            {
                Item = m_Xdoc.SelectSingleNode("/" + rootname + "/SXMaxZone");
                int temp = 0;
                int.TryParse(Item.InnerText, out temp);
                store.SXMaxzone[device] = Math.Min(Math.Max(temp * 0.01, 0.0), 1.0);
            }
            catch { store.SXMaxzone[device] = 1.0; missingSetting = true; }

            try
            {
                Item = m_Xdoc.SelectSingleNode("/" + rootname + "/SZMaxZone");
                int temp = 0;
                int.TryParse(Item.InnerText, out temp);
                store.SZMaxzone[device] = Math.Min(Math.Max(temp * 0.01, 0.0), 1.0);
            }
            catch { store.SZMaxzone[device] = 1.0; missingSetting = true; }

            try
            {
                Item = m_Xdoc.SelectSingleNode("/" + rootname + "/SXAntiDeadZone");
                int temp = 0;
                int.TryParse(Item.InnerText, out temp);
                store.SXAntiDeadzone[device] = Math.Min(Math.Max(temp * 0.01, 0.0), 1.0);
            }
            catch { store.SXAntiDeadzone[device] = 0.0; missingSetting = true; }

            try
            {
                Item = m_Xdoc.SelectSingleNode("/" + rootname + "/SZAntiDeadZone");
                int temp = 0;
                int.TryParse(Item.InnerText, out temp);
                store.SZAntiDeadzone[device] = Math.Min(Math.Max(temp * 0.01, 0.0), 1.0);
            }
            catch { store.SZAntiDeadzone[device] = 0.0; missingSetting = true; }

            try
            {
                Item = m_Xdoc.SelectSingleNode("/" + rootname + "/Sensitivity");
                string[] s = Item.InnerText.Split('|');
                if (s.Length == 1)
                    s = Item.InnerText.Split(',');

                if (double.TryParse(s[0], out store.LSSens[device]))
                {
                    store.LSSens[device] = Math.Clamp(store.LSSens[device], 0.1, 5.0);
                }
                else
                {
                    store.LSSens[device] = 1;
                }

                if (double.TryParse(s[1], out store.RSSens[device]))
                {
                    store.RSSens[device] = Math.Clamp(store.RSSens[device], 0.1, 5.0);
                }
                else
                {
                    store.RSSens[device] = 1;
                }

                if (double.TryParse(s[2], out store.l2Sens[device]))
                {
                    store.l2Sens[device] = Math.Clamp(store.l2Sens[device], 0.1, 10.0);
                }
                else
                {
                    store.l2Sens[device] = 1;
                }

                if (double.TryParse(s[3], out store.r2Sens[device]))
                {
                    store.r2Sens[device] = Math.Clamp(store.r2Sens[device], 0.1, 10.0);
                }
                else
                {
                    store.r2Sens[device] = 1;
                }

                if (double.TryParse(s[4], out store.SXSens[device]))
                {
                    store.SXSens[device] = Math.Clamp(store.SXSens[device], 0.0, 5.0);
                }
                else
                {
                    store.SXSens[device] = 1;
                }

                if (double.TryParse(s[5], out store.SZSens[device]))
                {
                    store.SZSens[device] = Math.Clamp(store.SZSens[device], 0.0, 5.0);
                }
                else
                {
                    store.SZSens[device] = 1;
                }
            }
            catch { missingSetting = true; }

            context.MissingSetting = missingSetting;
        }
    }
}
