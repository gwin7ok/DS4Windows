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
    /// Phase8-Step4b W5（Phase8-Step4b-Plan.md §3.2）: 特殊アクションの割り当て（ProfileActions）の読込と、アクションの辞書の作り直し。Step5 が組み立ての入力に使う節のため、単独のクラスにする。
    /// <c>LegacyProfileXmlSerializer.LoadProfile</c> から移したもの。移動のみで、要素の順序・既定値・例外の扱いは変えない。
    /// </summary>
    internal sealed class ProfileActionsLoadSection : IProfileLoadSection
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
                Item = m_Xdoc.SelectSingleNode("/" + rootname + "/ProfileActions");
                store.profileActions[device].Clear();
                if (!string.IsNullOrEmpty(Item.InnerText))
                {
                    string[] actionNames = Item.InnerText.Split('/');
                    for (int actIndex = 0, actLen = actionNames.Length; actIndex < actLen; actIndex++)
                    {
                        string tempActionName = actionNames[actIndex];
                        if (!store.profileActions[device].Contains(tempActionName))
                        {
                            store.profileActions[device].Add(tempActionName);
                        }
                    }
                }
            }
            catch { store.profileActions[device].Clear(); missingSetting = true; }

            foreach (DS4ControlSettings dcs in store.ds4settings[device])
                dcs.Reset();

            store.containsCustomAction[device] = false;
            store.containsCustomExtras[device] = false;
            store.profileActionCount[device] = store.profileActions[device].Count;
            store.profileActionDict[device].Clear();
            store.profileActionIndexDict[device].Clear();
            foreach (string actionname in store.profileActions[device])
            {
                store.profileActionDict[device][actionname] = store.GetAction(actionname);
                store.profileActionIndexDict[device][actionname] = store.GetActionIndexOf(actionname);
            }

            context.MissingSetting = missingSetting;
        }
    }
}
