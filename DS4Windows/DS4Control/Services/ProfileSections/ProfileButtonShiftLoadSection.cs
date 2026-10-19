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
    /// Phase8-Step4b W5（Phase8-Step4b-Plan.md §3.2）: ボタン割り当て（シフト。ShiftControl/Button・Macro・Key・Extras・KeyType）の読込。ShiftControl の個別の Trigger がないときは、旧形式の ShiftModifier（文脈）を使う。
    /// <c>LegacyProfileXmlSerializer.LoadProfile</c> から移したもの。移動のみで、要素の順序・既定値・例外の扱いは変えない。
    /// </summary>
    internal sealed class ProfileButtonShiftLoadSection : IProfileLoadSection
    {
        public void Load(ProfileLoadContext context)
        {
            BackingStore store = context.Store;
            XmlDocument m_Xdoc = context.Xdoc;
            string rootname = context.RootName;
            int device = context.Device;
            bool missingSetting = context.MissingSetting;

            int shiftM = context.ShiftModifier;
            XmlNode ParentItem;
            DS4KeyType keyType;
            ushort wvk;
            Dictionary<DS4Controls, DS4KeyType> shiftCustomMapKeyTypes = new Dictionary<DS4Controls, DS4KeyType>();
            Dictionary<DS4Controls, UInt16> shiftCustomMapKeys = new Dictionary<DS4Controls, UInt16>();
            Dictionary<DS4Controls, X360Controls> shiftCustomMapButtons = new Dictionary<DS4Controls, X360Controls>();
            Dictionary<DS4Controls, String> shiftCustomMapMacros = new Dictionary<DS4Controls, String>();
            Dictionary<DS4Controls, String> shiftCustomMapExtras = new Dictionary<DS4Controls, String>();

            ParentItem = m_Xdoc.SelectSingleNode("/" + rootname + "/ShiftControl/Button");
            if (ParentItem != null)
            {
                foreach (XmlElement item in ParentItem.ChildNodes)
                {
                    int shiftT = shiftM;
                    if (item.HasAttribute("Trigger"))
                        int.TryParse(item.Attributes["Trigger"].Value, out shiftT);

                    if (Enum.TryParse(item.Name, out DS4Controls currentControl))
                    {
                        store.UpdateDS4CSetting(device, item.Name, true, store.getX360ControlsByName(item.InnerText), "", DS4KeyType.None, shiftT);
                        shiftCustomMapButtons.Add(store.getDS4ControlsByName(item.Name), store.getX360ControlsByName(item.InnerText));
                    }
                }
            }

            ParentItem = m_Xdoc.SelectSingleNode("/" + rootname + "/ShiftControl/Macro");
            if (ParentItem != null)
            {
                foreach (XmlElement item in ParentItem.ChildNodes)
                {
                    shiftCustomMapMacros.Add(store.getDS4ControlsByName(item.Name), item.InnerText);
                    string[] skeys;
                    int[] keys;
                    if (!string.IsNullOrEmpty(item.InnerText))
                    {
                        skeys = item.InnerText.Split('/');
                        keys = new int[skeys.Length];
                    }
                    else
                    {
                        skeys = new string[0];
                        keys = new int[0];
                    }

                    for (int i = 0, keylen = keys.Length; i < keylen; i++)
                        keys[i] = int.Parse(skeys[i]);

                    int shiftT = shiftM;
                    if (item.HasAttribute("Trigger"))
                        int.TryParse(item.Attributes["Trigger"].Value, out shiftT);

                    if (Enum.TryParse(item.Name, out DS4Controls currentControl))
                    {
                        store.UpdateDS4CSetting(device, item.Name, true, keys, "", DS4KeyType.None, shiftT);
                    }
                }
            }

            ParentItem = m_Xdoc.SelectSingleNode("/" + rootname + "/ShiftControl/Key");
            if (ParentItem != null)
            {
                foreach (XmlElement item in ParentItem.ChildNodes)
                {
                    if (ushort.TryParse(item.InnerText, out wvk))
                    {
                        int shiftT = shiftM;
                        if (item.HasAttribute("Trigger"))
                            int.TryParse(item.Attributes["Trigger"].Value, out shiftT);

                        if (Enum.TryParse(item.Name, out DS4Controls currentControl))
                        {
                            store.UpdateDS4CSetting(device, item.Name, true, wvk, "", DS4KeyType.None, shiftT);
                            shiftCustomMapKeys.Add(store.getDS4ControlsByName(item.Name), wvk);
                        }
                    }
                }
            }

            ParentItem = m_Xdoc.SelectSingleNode("/" + rootname + "/ShiftControl/Extras");
            if (ParentItem != null)
            {
                foreach (XmlElement item in ParentItem.ChildNodes)
                {
                    if (item.InnerText != string.Empty)
                    {
                        if (Enum.TryParse(item.Name, out DS4Controls currentControl))
                        {
                            store.UpdateDS4CExtra(device, item.Name, true, item.InnerText);
                            shiftCustomMapExtras.Add(store.getDS4ControlsByName(item.Name), item.InnerText);
                        }
                    }
                    else
                        ParentItem.RemoveChild(item);
                }
            }

            ParentItem = m_Xdoc.SelectSingleNode("/" + rootname + "/ShiftControl/KeyType");
            if (ParentItem != null)
            {
                foreach (XmlElement item in ParentItem.ChildNodes)
                {
                    if (item != null)
                    {
                        keyType = DS4KeyType.None;
                        if (item.InnerText.Contains(DS4KeyType.ScanCode.ToString()))
                            keyType |= DS4KeyType.ScanCode;
                        if (item.InnerText.Contains(DS4KeyType.Toggle.ToString()))
                            keyType |= DS4KeyType.Toggle;
                        if (item.InnerText.Contains(DS4KeyType.Macro.ToString()))
                            keyType |= DS4KeyType.Macro;
                        if (item.InnerText.Contains(DS4KeyType.HoldMacro.ToString()))
                            keyType |= DS4KeyType.HoldMacro;
                        if (item.InnerText.Contains(DS4KeyType.Unbound.ToString()))
                            keyType |= DS4KeyType.Unbound;

                        if (keyType != DS4KeyType.None && Enum.TryParse(item.Name, out DS4Controls currentControl))
                        {
                            store.UpdateDS4CKeyType(device, item.Name, true, keyType);
                            shiftCustomMapKeyTypes.Add(store.getDS4ControlsByName(item.Name), keyType);
                        }
                    }
                }
            }

            context.MissingSetting = missingSetting;
        }
    }
}
