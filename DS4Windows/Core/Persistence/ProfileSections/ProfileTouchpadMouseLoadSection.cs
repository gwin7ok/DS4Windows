using System;
using System.Xml;
using DS4Windows.Core.Settings;

namespace DS4Windows.Core.Persistence.ProfileSections
{
    /// <summary>
    /// タッチパッドのマウス（トラックボール、相対マウス）・タッチパッドのスティック・タッチパッドのボタンモード（TrackballMode〜）の読込。
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
            XmlNode? Item;

            try { Item = m_Xdoc.SelectSingleNode("/" + rootname + "/TrackballMode"); { bool.TryParse(Item.Required().InnerText, out bool parsedValue); store.touchpadSlots[device].Trackball = parsedValue; }; }
            catch { store.touchpadSlots[device].Trackball = false; missingSetting = true; }

            try { Item = m_Xdoc.SelectSingleNode("/" + rootname + "/TrackballFriction"); { double.TryParse(Item.Required().InnerText, out double parsedValue); store.touchpadSlots[device].TrackballFriction = parsedValue; }; }
            catch { store.touchpadSlots[device].TrackballFriction = 10.0; missingSetting = true; }

            try
            {
                Item = m_Xdoc.SelectSingleNode("/" + rootname + "/TouchRelMouseRotation");
                int.TryParse(Item.Required().InnerText, out int temp);
                temp = Math.Min(Math.Max(temp, -180), 180);
                store.touchpadSlots[device].RelMouse.rotation = temp * Math.PI / 180.0;
            }
            catch { store.touchpadSlots[device].RelMouse.rotation = 0.0; missingSetting = true; }

            try
            {
                Item = m_Xdoc.SelectSingleNode("/" + rootname + "/TouchRelMouseMinThreshold");
                double.TryParse(Item.Required().InnerText, out double temp);
                temp = Math.Min(Math.Max(temp, 1.0), 40.0);
                store.touchpadSlots[device].RelMouse.minThreshold = temp;
            }
            catch { store.touchpadSlots[device].RelMouse.minThreshold = TouchpadRelMouseSettings.DEFAULT_MIN_THRESHOLD; missingSetting = true; }


            bool touchMStickGroup = false;
            XmlNode? xmlTouchMStickSmoothingElement =
                m_Xdoc.SelectSingleNode("/" + rootname + "/TouchpadMouseStick");
            touchMStickGroup = xmlTouchMStickSmoothingElement != null;

            if (touchMStickGroup && xmlTouchMStickSmoothingElement.Required().HasChildNodes)
            {
                //try
                //{
                //    Item = xmlGyroMStickSmoothingElement.SelectSingleNode("UseSmoothing");
                //    if (bool.TryParse(Item?.InnerText ?? "", out bool tempSmoothing))
                //    {
                //        gyroSlots[device].MouseStick.useSmoothing = tempSmoothing;
                //    }
                //}
                // （旧: 要素がない・値が不正な古い保存ファイルは、既定値のまま続ける）

                context.ReadInt(xmlTouchMStickSmoothingElement, "DeadZone", int.MinValue, int.MaxValue, v => store.touchpadSlots[device].MouseStick.deadZone = v);

                context.ReadInt(xmlTouchMStickSmoothingElement, "MaxZone", 1, int.MaxValue, v => store.touchpadSlots[device].MouseStick.maxZone = v);

                context.ReadEnum<TouchMouseStickInfo.OutputStick>(xmlTouchMStickSmoothingElement, "OutputStick", v => store.touchpadSlots[device].MouseStick.outputStick = v);

                Item = xmlTouchMStickSmoothingElement?.SelectSingleNode("OutputStickAxes");
                if (Item == null)
                {
                    // 互換。以前の保存（上流の 2022-10-20 以来の不具合）は、軸を 2 つ目の <OutputStick> として書いていた
                    XmlNodeList? legacyOutputStickNodes = xmlTouchMStickSmoothingElement?.SelectNodes("OutputStick");
                    if (legacyOutputStickNodes != null && legacyOutputStickNodes.Count > 1)
                        Item = legacyOutputStickNodes[1];
                }
                if (Enum.TryParse(Item?.InnerText ?? "",
                    out TouchMouseStickInfo.OutputStickAxes tempAxes))
                {
                    store.touchpadSlots[device].MouseStick.outputStickDir = tempAxes;
                }

                context.ReadDouble(xmlTouchMStickSmoothingElement, "AntiDeadX", double.MinValue, double.MaxValue, v => store.touchpadSlots[device].MouseStick.antiDeadX = v);

                context.ReadDouble(xmlTouchMStickSmoothingElement, "AntiDeadY", double.MinValue, double.MaxValue, v => store.touchpadSlots[device].MouseStick.antiDeadY = v);

                Item = xmlTouchMStickSmoothingElement?.SelectSingleNode("Invert");
                if (uint.TryParse(Item?.InnerText ?? "",
                    out uint tempInvert))
                {
                    store.touchpadSlots[device].MouseStick.inverted = tempInvert;
                }

                context.ReadDouble(xmlTouchMStickSmoothingElement, "MaxOutput", 0.0, 100.0, v => store.touchpadSlots[device].MouseStick.maxOutput = v);

                context.ReadBool(xmlTouchMStickSmoothingElement, "MaxOutputEnabled", v => store.touchpadSlots[device].MouseStick.maxOutputEnabled = v);

                context.ReadInt(xmlTouchMStickSmoothingElement, "VerticalScale", int.MinValue, int.MaxValue, v => store.touchpadSlots[device].MouseStick.vertScale = v);

                context.ReadEnum<StickOutCurve.Curve>(xmlTouchMStickSmoothingElement, "OutputCurve", v => store.touchpadSlots[device].MouseStick.outputCurve = v);

                Item = xmlTouchMStickSmoothingElement?.SelectSingleNode("Rotation");
                if (int.TryParse(Item?.InnerText ?? "", out int tempRot))
                {
                    tempRot = Math.Clamp(tempRot, -180, 180);
                    store.touchpadSlots[device].MouseStick.rotationRad = tempRot * Math.PI / 180.0;
                }


                bool stickSmoothingGroup = false;
                XmlNode? xmlStickSmoothingElement =
                    xmlTouchMStickSmoothingElement.Required().SelectSingleNode("SmoothingSettings");
                stickSmoothingGroup = xmlStickSmoothingElement != null;

                if (stickSmoothingGroup && xmlStickSmoothingElement.Required().HasChildNodes)
                {
                    //try
                    //{
                    //    Item = xmlGyroMStickSmoothingElement.SelectSingleNode("UseSmoothing");
                    //    bool.TryParse(Item.InnerText, out bool tempSmoothing);
                    //    gyroSlots[device].MouseStick.useSmoothing = tempSmoothing;
                    //}
                    //catch { gyroSlots[device].MouseStick.useSmoothing = false; missingSetting = true; }

                    context.ReadEnum<TouchMouseStickInfo.SmoothingMethod>(xmlTouchMStickSmoothingElement, "SmoothingMethod", v => store.touchpadSlots[device].MouseStick.smoothingMethod = v);

                    context.ReadDouble(xmlTouchMStickSmoothingElement, "SmoothingMinCutoff", 0.0, 100.0, v => store.touchpadSlots[device].MouseStick.minCutoff = v);

                    context.ReadDouble(xmlTouchMStickSmoothingElement, "SmoothingBeta", 0.0, 1.0, v => store.touchpadSlots[device].MouseStick.beta = v);
                }
            }


            try
            {
                Item = m_Xdoc.SelectSingleNode($"/{rootname}/TouchpadButtonMode");
                if (Enum.TryParse(Item?.InnerText ?? "", out TouchButtonActivationMode tempMode))
                {
                    store.touchpadSlots[device].ButtonMode = tempMode;
                }
            }
            catch { store.touchpadSlots[device].ButtonMode = TouchButtonActivationMode.Click; missingSetting = true; }

            context.MissingSetting = missingSetting;
        }
    }
}
