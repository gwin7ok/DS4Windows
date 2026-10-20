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
    /// Phase8-Step4b W5（Phase8-Step4b-Plan.md §3.2）: ボタン割り当て（通常。Control/Button・Macro・Key・Extras・KeyType）の読込。
    /// <c>LegacyProfileXmlSerializer.LoadProfile</c> から移したもの。移動のみで、要素の順序・既定値・例外の扱いは変えない。
    /// </summary>
    internal sealed class ProfileButtonControlLoadSection : IProfileLoadSection
    {
        // 旧版（Phase8-Fix-MacroRepeatUnify 以前）が KeyType に書いた「押している間繰り返す」の名前。読込のみ受け付ける（enum の HoldMacro は削除した）
        private const string LegacyHoldMacroName = "HoldMacro";

        public void Load(ProfileLoadContext context)
        {
            BackingStore store = context.Store;
            XmlDocument m_Xdoc = context.Xdoc;
            string rootname = context.RootName;
            int device = context.Device;
            bool missingSetting = context.MissingSetting;

            DS4KeyType keyType;
            ushort wvk;
            Dictionary<DS4Controls, DS4KeyType> customMapKeyTypes = new Dictionary<DS4Controls, DS4KeyType>();
            Dictionary<DS4Controls, UInt16> customMapKeys = new Dictionary<DS4Controls, UInt16>();
            Dictionary<DS4Controls, X360Controls> customMapButtons = new Dictionary<DS4Controls, X360Controls>();
            Dictionary<DS4Controls, String> customMapMacros = new Dictionary<DS4Controls, String>();
            Dictionary<DS4Controls, String> customMapExtras = new Dictionary<DS4Controls, String>();

            XmlNode ParentItem = m_Xdoc.SelectSingleNode("/" + rootname + "/Control/Button");
            if (ParentItem != null)
            {
                foreach (XmlNode item in ParentItem.ChildNodes)
                {
                    // Phase8-Step8 8-0: 廃止された「ジャイロスワイプ」の割り当ては解釈せず、そのまま保持する
                    if (RemovedGyroSwipeSettings.IsSwipeControlName(item.Name))
                    {
                        store.removedGyroSwipeSettings[device].CaptureControlEntry("Button", item);
                        continue;
                    }

                    if (Enum.TryParse(item.Name, out DS4Controls currentControl))
                    {
                        store.UpdateDS4CSetting(device, item.Name, X360ControlTables.getX360ControlsByName(item.InnerText), "", DS4KeyType.None);
                        customMapButtons.Add(DS4ControlNames.getDS4ControlsByName(item.Name), X360ControlTables.getX360ControlsByName(item.InnerText));
                    }
                }
            }

            ParentItem = m_Xdoc.SelectSingleNode("/" + rootname + "/Control/Macro");
            if (ParentItem != null)
            {
                foreach (XmlNode item in ParentItem.ChildNodes)
                {
                    // Phase8-Step8 8-0: 廃止された「ジャイロスワイプ」の割り当ては解釈せず、そのまま保持する
                    if (RemovedGyroSwipeSettings.IsSwipeControlName(item.Name))
                    {
                        store.removedGyroSwipeSettings[device].CaptureControlEntry("Macro", item);
                        continue;
                    }

                    customMapMacros.Add(DS4ControlNames.getDS4ControlsByName(item.Name), item.InnerText);
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

                    if (Enum.TryParse(item.Name, out DS4Controls currentControl))
                    {
                        store.UpdateDS4CSetting(device, item.Name, keys, "", DS4KeyType.None);
                    }
                }
            }

            ParentItem = m_Xdoc.SelectSingleNode("/" + rootname + "/Control/Key");
            if (ParentItem != null)
            {
                foreach (XmlNode item in ParentItem.ChildNodes)
                {
                    // Phase8-Step8 8-0: 廃止された「ジャイロスワイプ」の割り当ては解釈せず、そのまま保持する
                    if (RemovedGyroSwipeSettings.IsSwipeControlName(item.Name))
                    {
                        store.removedGyroSwipeSettings[device].CaptureControlEntry("Key", item);
                        continue;
                    }

                    if (ushort.TryParse(item.InnerText, out wvk) && Enum.TryParse(item.Name, out DS4Controls currentControl))
                    {
                        store.UpdateDS4CSetting(device, item.Name, wvk, "", DS4KeyType.None);
                        customMapKeys.Add(DS4ControlNames.getDS4ControlsByName(item.Name), wvk);
                    }
                }
            }

            ParentItem = m_Xdoc.SelectSingleNode("/" + rootname + "/Control/Extras");
            if (ParentItem != null)
            {
                foreach (XmlNode item in ParentItem.ChildNodes)
                {
                    // Phase8-Step8 8-0: 廃止された「ジャイロスワイプ」の割り当ては解釈せず、そのまま保持する
                    if (RemovedGyroSwipeSettings.IsSwipeControlName(item.Name))
                    {
                        store.removedGyroSwipeSettings[device].CaptureControlEntry("Extras", item);
                        continue;
                    }

                    if (item.InnerText != string.Empty && Enum.TryParse(item.Name, out DS4Controls currentControl))
                    {
                        store.UpdateDS4CExtra(device, item.Name, item.InnerText);
                        customMapExtras.Add(DS4ControlNames.getDS4ControlsByName(item.Name), item.InnerText);
                    }
                    else
                        ParentItem.RemoveChild(item);
                }
            }

            ParentItem = m_Xdoc.SelectSingleNode("/" + rootname + "/Control/KeyType");
            if (ParentItem != null)
            {
                foreach (XmlNode item in ParentItem.ChildNodes)
                {
                    // Phase8-Step8 8-0: 廃止された「ジャイロスワイプ」の割り当ては解釈せず、そのまま保持する
                    if (RemovedGyroSwipeSettings.IsSwipeControlName(item.Name))
                    {
                        store.removedGyroSwipeSettings[device].CaptureControlEntry("KeyType", item);
                        continue;
                    }

                    if (item != null)
                    {
                        keyType = DS4KeyType.None;
                        if (item.InnerText.Contains(DS4KeyType.ScanCode.ToString()))
                            keyType |= DS4KeyType.ScanCode;
                        if (item.InnerText.Contains(DS4KeyType.Toggle.ToString()))
                            keyType |= DS4KeyType.Toggle;
                        if (item.InnerText.Contains(DS4KeyType.Macro.ToString()))
                            keyType |= DS4KeyType.Macro;
                        // Phase8-Fix-MacroRepeatUnify（D1・D3）: 「押している間繰り返す」は RepeatMacro に統合した。旧版が書いた HoldMacro も、ここでは RepeatMacro として読む
                        // （保存時に RepeatMacro へ置き換わる）。"RepeatMacro"／"HoldMacro" は "Macro" を含むため、上の Macro も同時に立つ（従来と同じ）
                        if (item.InnerText.Contains(DS4KeyType.RepeatMacro.ToString()) || item.InnerText.Contains(LegacyHoldMacroName))
                            keyType |= DS4KeyType.RepeatMacro;
                        if (item.InnerText.Contains(DS4KeyType.Unbound.ToString()))
                            keyType |= DS4KeyType.Unbound;

                        if (keyType != DS4KeyType.None && Enum.TryParse(item.Name, out DS4Controls currentControl))
                        {
                            store.UpdateDS4CKeyType(device, item.Name, keyType);
                            customMapKeyTypes.Add(DS4ControlNames.getDS4ControlsByName(item.Name), keyType);
                        }
                    }
                }
            }

            context.MissingSetting = missingSetting;
        }
    }
}
