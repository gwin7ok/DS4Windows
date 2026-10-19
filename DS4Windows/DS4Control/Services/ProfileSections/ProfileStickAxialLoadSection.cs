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
using LightbarMacro = DS4WinWPF.DS4Forms.ViewModels.LightbarMacro;

namespace DS4Windows.Services
{
    /// <summary>
    /// Phase8-Step4b W3（Phase8-Step4b-Plan.md §3.2）: スティックの軸別の不感帯・デルタ加速（LSAxialDeadOptions〜RSDeltaAccelSettings）の読込。右スティックのデルタ加速の条件は、旧コードのまま左スティックの要素を見る（挙動を変えない）。
    /// <c>LegacyProfileXmlSerializer.LoadProfile</c> から移したもの。移動のみで、要素の順序・既定値・例外の扱いは変えない。
    /// </summary>
    internal sealed class ProfileStickAxialLoadSection : IProfileLoadSection
    {
        public void Load(ProfileLoadContext context)
        {
            BackingStore store = context.Store;
            XmlDocument m_Xdoc = context.Xdoc;
            string rootname = context.RootName;
            int device = context.Device;
            bool missingSetting = context.MissingSetting;
            XmlNode Item;

            bool lsAxialDeadGroup = false;
            XmlNode lsAxialDeadElement =
                m_Xdoc.SelectSingleNode("/" + rootname + "/LSAxialDeadOptions");
            lsAxialDeadGroup = lsAxialDeadElement != null;

            if (lsAxialDeadGroup)
            {
                try
                {
                    Item = lsAxialDeadElement.SelectSingleNode("DeadZoneX");
                    int.TryParse(Item.InnerText, out int temp);
                    temp = Math.Min(Math.Max(temp, 0), 127);
                    store.lsModInfo[device].xAxisDeadInfo.deadZone = temp;
                }
                catch { }

                try
                {
                    Item = lsAxialDeadElement.SelectSingleNode("DeadZoneY");
                    int.TryParse(Item.InnerText, out int temp);
                    temp = Math.Min(Math.Max(temp, 0), 127);
                    store.lsModInfo[device].yAxisDeadInfo.deadZone = temp;
                }
                catch { }

                try
                {
                    Item = lsAxialDeadElement.SelectSingleNode("MaxZoneX");
                    int.TryParse(Item.InnerText, out int temp);
                    store.lsModInfo[device].xAxisDeadInfo.maxZone = Math.Min(Math.Max(temp, 0), 100);
                }
                catch { }

                try
                {
                    Item = lsAxialDeadElement.SelectSingleNode("MaxZoneY");
                    int.TryParse(Item.InnerText, out int temp);
                    store.lsModInfo[device].yAxisDeadInfo.maxZone = Math.Min(Math.Max(temp, 0), 100);
                }
                catch { }

                try
                {
                    Item = lsAxialDeadElement.SelectSingleNode("AntiDeadZoneX");
                    int.TryParse(Item.InnerText, out int temp);
                    store.lsModInfo[device].xAxisDeadInfo.antiDeadZone = Math.Min(Math.Max(temp, 0), 100);
                }
                catch { }

                try
                {
                    Item = lsAxialDeadElement.SelectSingleNode("AntiDeadZoneY");
                    int.TryParse(Item.InnerText, out int temp);
                    store.lsModInfo[device].yAxisDeadInfo.antiDeadZone = Math.Min(Math.Max(temp, 0), 100);
                }
                catch { }

                try
                {
                    Item = lsAxialDeadElement.SelectSingleNode("MaxOutputX");
                    double.TryParse(Item.InnerText, out double temp);
                    store.lsModInfo[device].xAxisDeadInfo.maxOutput = Math.Min(Math.Max(temp, 0.0), 100.0);
                }
                catch { }

                try
                {
                    Item = lsAxialDeadElement.SelectSingleNode("MaxOutputY");
                    double.TryParse(Item.InnerText, out double temp);
                    store.lsModInfo[device].yAxisDeadInfo.maxOutput = Math.Min(Math.Max(temp, 0.0), 100.0);
                }
                catch { }
            }

            XmlNode lsDeltaAccelElement =
                m_Xdoc.SelectSingleNode($"/{rootname}/LSDeltaAccelSettings");
            if (lsDeltaAccelElement != null)
            {
                try
                {
                    Item = lsDeltaAccelElement.SelectSingleNode("Enabled");
                    if (bool.TryParse(Item.InnerText ?? "", out bool temp))
                    {
                        store.lsOutputSettings[device].outputSettings.controlSettings.deltaAccelSettings.enabled
                            = temp;
                    }
                }
                catch { }

                try
                {
                    Item = lsDeltaAccelElement.SelectSingleNode("Multiplier");
                    if (double.TryParse(Item?.InnerText ?? "", out double temp))
                    {
                        store.lsOutputSettings[device].outputSettings.controlSettings.deltaAccelSettings.multiplier =
                            Math.Clamp(temp, 0.0, 10.0);
                    }
                }
                catch { }

                try
                {
                    Item = lsDeltaAccelElement.SelectSingleNode("MaxTravel");
                    if (double.TryParse(Item?.InnerText ?? "", out double temp))
                    {
                        store.lsOutputSettings[device].outputSettings.controlSettings.deltaAccelSettings.maxTravel =
                            Math.Clamp(temp, 0.0, 1.0);
                    }
                }
                catch { }

                try
                {
                    Item = lsDeltaAccelElement.SelectSingleNode("MinTravel");
                    if (double.TryParse(Item?.InnerText ?? "", out double temp))
                    {
                        store.lsOutputSettings[device].outputSettings.controlSettings.deltaAccelSettings.minTravel =
                            Math.Clamp(temp, 0.0, 1.0);
                    }
                }
                catch { }

                try
                {
                    Item = lsDeltaAccelElement.SelectSingleNode("EasingDuration");
                    if (double.TryParse(Item?.InnerText ?? "", out double temp))
                    {
                        store.lsOutputSettings[device].outputSettings.controlSettings.deltaAccelSettings.easingDuration =
                            Math.Clamp(temp, 0.0, 600.0);
                    }
                }
                catch { }

                try
                {
                    Item = lsDeltaAccelElement.SelectSingleNode("MinFactor");
                    if (double.TryParse(Item?.InnerText ?? "", out double temp))
                    {
                        store.lsOutputSettings[device].outputSettings.controlSettings.deltaAccelSettings.minfactor =
                            Math.Clamp(temp, 1.0, 10.0);
                    }
                }
                catch { }
            }

            try
            {
                Item = m_Xdoc.SelectSingleNode("/" + rootname + "/RSDeadZoneType");
                if (Enum.TryParse(Item?.InnerText ?? "", out StickDeadZoneInfo.DeadZoneType temp))
                {
                    store.rsModInfo[device].deadzoneType = temp;
                }
            }
            catch { }

            bool rsAxialDeadGroup = false;
            XmlNode rsAxialDeadElement =
                m_Xdoc.SelectSingleNode("/" + rootname + "/RSAxialDeadOptions");
            rsAxialDeadGroup = rsAxialDeadElement != null;

            if (rsAxialDeadGroup)
            {
                try
                {
                    Item = rsAxialDeadElement.SelectSingleNode("DeadZoneX");
                    int.TryParse(Item.InnerText, out int temp);
                    temp = Math.Min(Math.Max(temp, 0), 127);
                    store.rsModInfo[device].xAxisDeadInfo.deadZone = temp;
                }
                catch { }

                try
                {
                    Item = rsAxialDeadElement.SelectSingleNode("DeadZoneY");
                    int.TryParse(Item.InnerText, out int temp);
                    temp = Math.Min(Math.Max(temp, 0), 127);
                    store.rsModInfo[device].yAxisDeadInfo.deadZone = temp;
                }
                catch { }

                try
                {
                    Item = rsAxialDeadElement.SelectSingleNode("MaxZoneX");
                    int.TryParse(Item.InnerText, out int temp);
                    store.rsModInfo[device].xAxisDeadInfo.maxZone = Math.Min(Math.Max(temp, 0), 100);
                }
                catch { }

                try
                {
                    Item = rsAxialDeadElement.SelectSingleNode("MaxZoneY");
                    int.TryParse(Item.InnerText, out int temp);
                    store.rsModInfo[device].yAxisDeadInfo.maxZone = Math.Min(Math.Max(temp, 0), 100);
                }
                catch { }

                try
                {
                    Item = rsAxialDeadElement.SelectSingleNode("AntiDeadZoneX");
                    int.TryParse(Item.InnerText, out int temp);
                    store.rsModInfo[device].xAxisDeadInfo.antiDeadZone = Math.Min(Math.Max(temp, 0), 100);
                }
                catch { }

                try
                {
                    Item = rsAxialDeadElement.SelectSingleNode("AntiDeadZoneY");
                    int.TryParse(Item.InnerText, out int temp);
                    store.rsModInfo[device].yAxisDeadInfo.antiDeadZone = Math.Min(Math.Max(temp, 0), 100);
                }
                catch { }

                try
                {
                    Item = rsAxialDeadElement.SelectSingleNode("MaxOutputX");
                    double.TryParse(Item.InnerText, out double temp);
                    store.rsModInfo[device].xAxisDeadInfo.maxOutput = Math.Min(Math.Max(temp, 0.0), 100.0);
                }
                catch { }

                try
                {
                    Item = rsAxialDeadElement.SelectSingleNode("MaxOutputY");
                    double.TryParse(Item.InnerText, out double temp);
                    store.rsModInfo[device].yAxisDeadInfo.maxOutput = Math.Min(Math.Max(temp, 0.0), 100.0);
                }
                catch { }
            }

            XmlNode rsDeltaAccelElement =
                m_Xdoc.SelectSingleNode($"/{rootname}/RSDeltaAccelSettings");
            if (lsDeltaAccelElement != null)
            {
                try
                {
                    Item = rsDeltaAccelElement.SelectSingleNode("Enabled");
                    if (bool.TryParse(Item.InnerText ?? "", out bool temp))
                    {
                        store.rsOutputSettings[device].outputSettings.controlSettings.deltaAccelSettings.enabled
                            = temp;
                    }
                }
                catch { }

                try
                {
                    Item = rsDeltaAccelElement.SelectSingleNode("Multiplier");
                    if (double.TryParse(Item?.InnerText ?? "", out double temp))
                    {
                        store.rsOutputSettings[device].outputSettings.controlSettings.deltaAccelSettings.multiplier =
                            Math.Clamp(temp, 0.0, 10.0);
                    }
                }
                catch { }

                try
                {
                    Item = rsDeltaAccelElement.SelectSingleNode("MaxTravel");
                    if (double.TryParse(Item?.InnerText ?? "", out double temp))
                    {
                        store.rsOutputSettings[device].outputSettings.controlSettings.deltaAccelSettings.maxTravel =
                            Math.Clamp(temp, 0.0, 1.0);
                    }
                }
                catch { }

                try
                {
                    Item = rsDeltaAccelElement.SelectSingleNode("MinTravel");
                    if (double.TryParse(Item?.InnerText ?? "", out double temp))
                    {
                        store.rsOutputSettings[device].outputSettings.controlSettings.deltaAccelSettings.minTravel =
                            Math.Clamp(temp, 0.0, 1.0);
                    }
                }
                catch { }

                try
                {
                    Item = rsDeltaAccelElement.SelectSingleNode("EasingDuration");
                    if (double.TryParse(Item?.InnerText ?? "", out double temp))
                    {
                        store.rsOutputSettings[device].outputSettings.controlSettings.deltaAccelSettings.easingDuration =
                            Math.Clamp(temp, 0.0, 600.0);
                    }
                }
                catch { }

                try
                {
                    Item = rsDeltaAccelElement.SelectSingleNode("MinFactor");
                    if (double.TryParse(Item?.InnerText ?? "", out double temp))
                    {
                        store.rsOutputSettings[device].outputSettings.controlSettings.deltaAccelSettings.minfactor =
                            Math.Clamp(temp, 1.0, 10.0);
                    }
                }
                catch { }
            }

            context.MissingSetting = missingSetting;
        }
    }
}
