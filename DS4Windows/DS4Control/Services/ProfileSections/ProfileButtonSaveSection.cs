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
    /// Phase8-Step4b W6（Phase8-Step4b-Plan.md §3.3）: ボタン割り当て（Control と ShiftControl。通常とシフトを 1 つのループで集めて保存する）の保存。
    /// <c>LegacyProfileXmlSerializer.SaveProfile</c> から移したもの。移動のみで、要素の順序・値の書式は変えない（XML の順序はゴールデンが固定している）。
    /// </summary>
    internal sealed class ProfileButtonSaveSection : IProfileSaveSection
    {
        public void Save(ProfileSaveContext context)
        {
            BackingStore store = context.Store;
            XmlDocument m_Xdoc = context.Xdoc;
            XmlElement rootElement = context.RootElement;
            int device = context.Device;

            XmlNode NodeControl = m_Xdoc.CreateNode(XmlNodeType.Element, "Control", null);
            XmlNode Key = m_Xdoc.CreateNode(XmlNodeType.Element, "Key", null);
            XmlNode Macro = m_Xdoc.CreateNode(XmlNodeType.Element, "Macro", null);
            XmlNode KeyType = m_Xdoc.CreateNode(XmlNodeType.Element, "KeyType", null);
            XmlNode Button = m_Xdoc.CreateNode(XmlNodeType.Element, "Button", null);
            XmlNode Extras = m_Xdoc.CreateNode(XmlNodeType.Element, "Extras", null);

            XmlNode NodeShiftControl = m_Xdoc.CreateNode(XmlNodeType.Element, "ShiftControl", null);

            XmlNode ShiftKey = m_Xdoc.CreateNode(XmlNodeType.Element, "Key", null);
            XmlNode ShiftMacro = m_Xdoc.CreateNode(XmlNodeType.Element, "Macro", null);
            XmlNode ShiftKeyType = m_Xdoc.CreateNode(XmlNodeType.Element, "KeyType", null);
            XmlNode ShiftButton = m_Xdoc.CreateNode(XmlNodeType.Element, "Button", null);
            XmlNode ShiftExtras = m_Xdoc.CreateNode(XmlNodeType.Element, "Extras", null);

            foreach (DS4ControlSettings dcs in store.ds4settings[device])
            {
                if (dcs.actionType != DS4ControlSettings.ActionType.Default)
                {
                    XmlNode buttonNode;
                    string keyType = string.Empty;

                    if (dcs.actionType == DS4ControlSettings.ActionType.Button &&
                        dcs.action.actionBtn == X360Controls.Unbound)
                    {
                        keyType += DS4KeyType.Unbound;
                    }

                    if (dcs.keyType.HasFlag(DS4KeyType.HoldMacro))
                        keyType += DS4KeyType.HoldMacro;
                    else if (dcs.keyType.HasFlag(DS4KeyType.Macro))
                        keyType += DS4KeyType.Macro;

                    if (dcs.keyType.HasFlag(DS4KeyType.Toggle))
                        keyType += DS4KeyType.Toggle;
                    if (dcs.keyType.HasFlag(DS4KeyType.ScanCode))
                        keyType += DS4KeyType.ScanCode;

                    if (keyType != string.Empty)
                    {
                        buttonNode = m_Xdoc.CreateNode(XmlNodeType.Element, dcs.control.ToString(), null);
                        buttonNode.InnerText = keyType;
                        KeyType.AppendChild(buttonNode);
                    }

                    buttonNode = m_Xdoc.CreateNode(XmlNodeType.Element, dcs.control.ToString(), null);
                    if (dcs.actionType == DS4ControlSettings.ActionType.Macro)
                    {
                        int[] ii = dcs.action.actionMacro;
                        buttonNode.InnerText = string.Join("/", ii);
                        Macro.AppendChild(buttonNode);
                    }
                    else if (dcs.actionType == DS4ControlSettings.ActionType.Key)
                    {
                        buttonNode.InnerText = dcs.action.actionKey.ToString();
                        Key.AppendChild(buttonNode);
                    }
                    else if (dcs.actionType == DS4ControlSettings.ActionType.Button)
                    {
                        buttonNode.InnerText = X360ControlTables.getX360ControlString((X360Controls)dcs.action.actionBtn);
                        Button.AppendChild(buttonNode);
                    }
                }

                bool hasvalue = false;
                if (!string.IsNullOrEmpty(dcs.extras))
                {
                    foreach (string s in dcs.extras.Split(','))
                    {
                        if (s != "0")
                        {
                            hasvalue = true;
                            break;
                        }
                    }
                }

                if (hasvalue)
                {
                    XmlNode extraNode = m_Xdoc.CreateNode(XmlNodeType.Element, dcs.control.ToString(), null);
                    extraNode.InnerText = dcs.extras;
                    Extras.AppendChild(extraNode);
                }

                if (dcs.shiftActionType != DS4ControlSettings.ActionType.Default && dcs.shiftTrigger > 0)
                {
                    XmlElement buttonNode;
                    string keyType = string.Empty;

                    if (dcs.shiftActionType == DS4ControlSettings.ActionType.Button &&
                        dcs.shiftAction.actionBtn == X360Controls.Unbound)
                    {
                        keyType += DS4KeyType.Unbound;
                    }

                    if (dcs.shiftKeyType.HasFlag(DS4KeyType.HoldMacro))
                        keyType += DS4KeyType.HoldMacro;
                    if (dcs.shiftKeyType.HasFlag(DS4KeyType.Macro))
                        keyType += DS4KeyType.Macro;
                    if (dcs.shiftKeyType.HasFlag(DS4KeyType.Toggle))
                        keyType += DS4KeyType.Toggle;
                    if (dcs.shiftKeyType.HasFlag(DS4KeyType.ScanCode))
                        keyType += DS4KeyType.ScanCode;

                    if (keyType != string.Empty)
                    {
                        buttonNode = m_Xdoc.CreateElement(dcs.control.ToString());
                        buttonNode.InnerText = keyType;
                        ShiftKeyType.AppendChild(buttonNode);
                    }

                    buttonNode = m_Xdoc.CreateElement(dcs.control.ToString());
                    buttonNode.SetAttribute("Trigger", dcs.shiftTrigger.ToString());
                    if (dcs.shiftActionType == DS4ControlSettings.ActionType.Macro)
                    {
                        int[] ii = dcs.shiftAction.actionMacro;
                        buttonNode.InnerText = string.Join("/", ii);
                        ShiftMacro.AppendChild(buttonNode);
                    }
                    else if (dcs.shiftActionType == DS4ControlSettings.ActionType.Key)
                    {
                        buttonNode.InnerText = dcs.shiftAction.actionKey.ToString();
                        ShiftKey.AppendChild(buttonNode);
                    }
                    else if (dcs.shiftActionType == DS4ControlSettings.ActionType.Button)
                    {
                        buttonNode.InnerText = dcs.shiftAction.actionBtn.ToString();
                        ShiftButton.AppendChild(buttonNode);
                    }
                }

                hasvalue = false;
                if (!string.IsNullOrEmpty(dcs.shiftExtras))
                {
                    foreach (string s in dcs.shiftExtras.Split(','))
                    {
                        if (s != "0")
                        {
                            hasvalue = true;
                            break;
                        }
                    }
                }

                if (hasvalue)
                {
                    XmlNode extraNode = m_Xdoc.CreateNode(XmlNodeType.Element, dcs.control.ToString(), null);
                    extraNode.InnerText = dcs.shiftExtras;
                    ShiftExtras.AppendChild(extraNode);
                }
            }

            rootElement.AppendChild(NodeControl);
            if (Button.HasChildNodes)
                NodeControl.AppendChild(Button);
            if (Macro.HasChildNodes)
                NodeControl.AppendChild(Macro);
            if (Key.HasChildNodes)
                NodeControl.AppendChild(Key);
            if (Extras.HasChildNodes)
                NodeControl.AppendChild(Extras);
            if (KeyType.HasChildNodes)
                NodeControl.AppendChild(KeyType);

            if (NodeControl.HasChildNodes)
                rootElement.AppendChild(NodeControl);

            rootElement.AppendChild(NodeShiftControl);
            if (ShiftButton.HasChildNodes)
                NodeShiftControl.AppendChild(ShiftButton);
            if (ShiftMacro.HasChildNodes)
                NodeShiftControl.AppendChild(ShiftMacro);
            if (ShiftKey.HasChildNodes)
                NodeShiftControl.AppendChild(ShiftKey);
            if (ShiftKeyType.HasChildNodes)
                NodeShiftControl.AppendChild(ShiftKeyType);
            if (ShiftExtras.HasChildNodes)
                NodeShiftControl.AppendChild(ShiftExtras);
        }
    }
}
