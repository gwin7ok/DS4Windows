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
using DS4Windows.Core.Abstractions;
using DS4Windows.Core.Model;
using DS4Windows.Core.Settings;

namespace DS4Windows.Core.Persistence.ProfileSections
{
    /// <summary>
    /// Phase8-Step4b W5（Phase8-Step4b-Plan.md §3.2）: 出力コントローラーの種別（OutputContDevice）の読込と、必要なときの出力デバイスの状態確認。確認の結果（xinputPlug／xinputStatus）は文脈に書き戻し、読込の最後の PostLoadSnippet が使う。
    /// <c>LegacyProfileXmlSerializer.LoadProfile</c> から移したもの。移動のみで、要素の順序・既定値・例外の扱いは変えない。
    /// </summary>
    internal sealed class ProfileOutputControllerLoadSection : IProfileLoadSection
    {
        public void Load(ProfileLoadContext context)
        {
            BackingStore store = context.Store;
            XmlDocument m_Xdoc = context.Xdoc;
            string rootname = context.RootName;
            int device = context.Device;
            IProfileLoadTarget control = context.Control;
            bool missingSetting = context.MissingSetting;
            XmlNode Item;

            DS4Windows.Core.Abstractions.IEnvironmentService _environment = context.Environment;
            bool xinputChange = context.XinputChange;
            OutContType oldContType = context.OldContType;
            bool xinputPlug = false;
            bool xinputStatus = false;

            OutContType OutContDeviceId(string name)
            {
                OutContType id = OutContType.X360;
                switch (name)
                {
                    case "None":
                    case "X360": id = OutContType.X360; break;
                    case "DS4": id = OutContType.DS4; break;
                    default: break;
                }

                return id;
            }

            try { Item = m_Xdoc.SelectSingleNode("/" + rootname + "/OutputContDevice"); store.outputDevType[device] = OutContDeviceId(Item.InnerText); }
            catch { store.outputDevType[device] = OutContType.X360; missingSetting = true; }

            // Only change xinput devices under certain conditions. Avoid
            // performing this upon program startup before loading devices.
            if (xinputChange && device < _environment.ControllerSlotLimit)
            {
                store.CheckOldDevicestatus(device, control, oldContType,
                    out xinputPlug, out xinputStatus);
            }

            context.XinputPlug = xinputPlug;
            context.XinputStatus = xinputStatus;
            context.MissingSetting = missingSetting;
        }
    }
}
