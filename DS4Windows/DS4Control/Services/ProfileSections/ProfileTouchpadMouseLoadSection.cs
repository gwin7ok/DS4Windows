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
    /// Phase8-Step4b W5（Phase8-Step4b-Plan.md §3.2）: タッチパッドのマウス（トラックボール、相対マウス）・タッチパッドのスティック・タッチパッドのボタンモード（TrackballMode〜）の読込。
    /// <c>LegacyProfileXmlSerializer.LoadProfile</c> から移したもの。移動のみで、要素の順序・既定値・例外の扱いは変えない。
    /// </summary>
    internal sealed class ProfileTouchpadMouseLoadSection : IProfileLoadSection
    {
        public void Load(ProfileLoadContext context)
        {
            BackingStore store = context.Store;
            XmlDocument m_Xdoc = context.Xdoc;
            string rootname = context.RootName;
            int device = context.Device;
            bool missingSetting = context.MissingSetting;
            XmlNode Item;

            try { Item = m_Xdoc.SelectSingleNode("/" + rootname + "/TrackballMode"); bool.TryParse(Item.InnerText, out store.trackballMode[device]); }
            catch { store.trackballMode[device] = false; missingSetting = true; }

            try { Item = m_Xdoc.SelectSingleNode("/" + rootname + "/TrackballFriction"); double.TryParse(Item.InnerText, out store.trackballFriction[device]); }
            catch { store.trackballFriction[device] = 10.0; missingSetting = true; }

            try
            {
                Item = m_Xdoc.SelectSingleNode("/" + rootname + "/TouchRelMouseRotation");
                int.TryParse(Item.InnerText, out int temp);
                temp = Math.Min(Math.Max(temp, -180), 180);
                store.touchpadRelMouse[device].rotation = temp * Math.PI / 180.0;
            }
            catch { store.touchpadRelMouse[device].rotation = 0.0; missingSetting = true; }

            try
            {
                Item = m_Xdoc.SelectSingleNode("/" + rootname + "/TouchRelMouseMinThreshold");
                double.TryParse(Item.InnerText, out double temp);
                temp = Math.Min(Math.Max(temp, 1.0), 40.0);
                store.touchpadRelMouse[device].minThreshold = temp;
            }
            catch { store.touchpadRelMouse[device].minThreshold = TouchpadRelMouseSettings.DEFAULT_MIN_THRESHOLD; missingSetting = true; }


            bool touchMStickGroup = false;
            XmlNode xmlTouchMStickSmoothingElement =
                m_Xdoc.SelectSingleNode("/" + rootname + "/TouchpadMouseStick");
            touchMStickGroup = xmlTouchMStickSmoothingElement != null;

            if (touchMStickGroup && xmlTouchMStickSmoothingElement.HasChildNodes)
            {
                //try
                //{
                //    Item = xmlGyroMStickSmoothingElement.SelectSingleNode("UseSmoothing");
                //    if (bool.TryParse(Item?.InnerText ?? "", out bool tempSmoothing))
                //    {
                //        gyroMStickInfo[device].useSmoothing = tempSmoothing;
                //    }
                //}
                //catch { }

                try
                {
                    Item = xmlTouchMStickSmoothingElement.SelectSingleNode("DeadZone");
                    if (int.TryParse(Item?.InnerText ?? "", out int temp))
                    {
                        store.touchMStickInfo[device].deadZone = temp;
                    }
                }
                catch { }

                try
                {
                    Item = xmlTouchMStickSmoothingElement.SelectSingleNode("MaxZone");
                    if (int.TryParse(Item?.InnerText ?? "", out int temp))
                    {
                        store.touchMStickInfo[device].maxZone = Math.Max(temp, 1);
                    }
                }
                catch { }

                try
                {
                    Item = xmlTouchMStickSmoothingElement.SelectSingleNode("OutputStick");
                    if (Enum.TryParse(Item?.InnerText ?? "",
                        out TouchMouseStickInfo.OutputStick temp))
                    {
                        store.touchMStickInfo[device].outputStick = temp;
                    }
                }
                catch { }

                try
                {
                    Item = xmlTouchMStickSmoothingElement.SelectSingleNode("OutputStickAxes");
                    if (Item == null)
                    {
                        // Phase6-Step12: 互換。以前の保存（上流の 2022-10-20 以来の不具合）は、軸を 2 つ目の <OutputStick> として書いていた
                        XmlNodeList legacyOutputStickNodes = xmlTouchMStickSmoothingElement.SelectNodes("OutputStick");
                        if (legacyOutputStickNodes != null && legacyOutputStickNodes.Count > 1)
                            Item = legacyOutputStickNodes[1];
                    }
                    if (Enum.TryParse(Item?.InnerText ?? "",
                        out TouchMouseStickInfo.OutputStickAxes temp))
                    {
                        store.touchMStickInfo[device].outputStickDir = temp;
                    }
                }
                catch { }

                try
                {
                    Item = xmlTouchMStickSmoothingElement.SelectSingleNode("AntiDeadX");
                    if (double.TryParse(Item?.InnerText ?? "", out double temp))
                    {
                        store.touchMStickInfo[device].antiDeadX = temp;
                    }
                }
                catch { }

                try
                {
                    Item = xmlTouchMStickSmoothingElement.SelectSingleNode("AntiDeadY");
                    if (double.TryParse(Item?.InnerText ?? "", out double temp))
                    {
                        store.touchMStickInfo[device].antiDeadY = temp;
                    }
                }
                catch { }

                try
                {
                    Item = xmlTouchMStickSmoothingElement.SelectSingleNode("Invert");
                    if (uint.TryParse(Item?.InnerText ?? "",
                        out uint temp))
                    {
                        store.touchMStickInfo[device].inverted = temp;
                    }
                }
                catch { }

                try
                {
                    Item = xmlTouchMStickSmoothingElement.SelectSingleNode("MaxOutput");
                    if (double.TryParse(Item?.InnerText ?? "", out double temp))
                    {
                        store.touchMStickInfo[device].maxOutput = Math.Clamp(temp, 0.0, 100.0);
                    }
                }
                catch { }

                try
                {
                    Item = xmlTouchMStickSmoothingElement.SelectSingleNode("MaxOutputEnabled");
                    if (bool.TryParse(Item?.InnerText ?? "", out bool temp))
                    {
                        store.touchMStickInfo[device].maxOutputEnabled = temp;
                    }
                }
                catch { }

                try
                {
                    Item = xmlTouchMStickSmoothingElement.SelectSingleNode("VerticalScale");
                    if (int.TryParse(Item?.InnerText ?? "", out int temp))
                    {
                        store.touchMStickInfo[device].vertScale = temp;
                    }
                }
                catch { }

                try
                {
                    Item = xmlTouchMStickSmoothingElement.SelectSingleNode("OutputCurve");
                    if (Enum.TryParse(Item?.InnerText ?? "", out StickOutCurve.Curve temp))
                    {
                        store.touchMStickInfo[device].outputCurve = temp;
                    }
                }
                catch { }

                try
                {
                    Item = xmlTouchMStickSmoothingElement.SelectSingleNode("Rotation");
                    if (int.TryParse(Item?.InnerText ?? "", out int temp))
                    {
                        temp = Math.Clamp(temp, -180, 180);
                        store.touchMStickInfo[device].rotationRad = temp * Math.PI / 180.0;
                    }
                }
                catch { }


                bool stickSmoothingGroup = false;
                XmlNode xmlStickSmoothingElement =
                    xmlTouchMStickSmoothingElement.SelectSingleNode("SmoothingSettings");
                stickSmoothingGroup = xmlStickSmoothingElement != null;

                if (stickSmoothingGroup && xmlStickSmoothingElement.HasChildNodes)
                {
                    //try
                    //{
                    //    Item = xmlGyroMStickSmoothingElement.SelectSingleNode("UseSmoothing");
                    //    bool.TryParse(Item.InnerText, out bool tempSmoothing);
                    //    gyroMStickInfo[device].useSmoothing = tempSmoothing;
                    //}
                    //catch { gyroMStickInfo[device].useSmoothing = false; missingSetting = true; }

                    try
                    {
                        Item = xmlTouchMStickSmoothingElement.SelectSingleNode("SmoothingMethod");
                        if (Enum.TryParse(Item?.InnerText ?? "",
                            out TouchMouseStickInfo.SmoothingMethod temp))
                        {
                            store.touchMStickInfo[device].smoothingMethod = temp;
                        }
                    }
                    catch { }

                    try
                    {
                        Item = xmlTouchMStickSmoothingElement.SelectSingleNode("SmoothingMinCutoff");
                        if (double.TryParse(Item?.InnerText ?? "", out double temp))
                        {
                            store.touchMStickInfo[device].minCutoff = Math.Clamp(temp, 0.0, 100.0);
                        }
                    }
                    catch { }

                    try
                    {
                        Item = xmlTouchMStickSmoothingElement.SelectSingleNode("SmoothingBeta");
                        if (double.TryParse(Item?.InnerText ?? "", out double temp))
                        {
                            store.touchMStickInfo[device].beta = Math.Clamp(temp, 0.0, 1.0);
                        }
                    }
                    catch { }
                }
            }


            try
            {
                Item = m_Xdoc.SelectSingleNode($"/{rootname}/TouchpadButtonMode");
                if (Enum.TryParse(Item?.InnerText ?? "", out TouchButtonActivationMode tempMode))
                {
                    store.touchpadButtonMode[device] = tempMode;
                }
            }
            catch { store.touchpadButtonMode[device] = TouchButtonActivationMode.Click; missingSetting = true; }

            context.MissingSetting = missingSetting;
        }
    }
}
