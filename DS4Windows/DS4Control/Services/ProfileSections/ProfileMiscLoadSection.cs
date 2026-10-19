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
    /// Phase8-Step4b W4（Phase8-Step4b-Plan.md §3.2）: 雑多の設定（ChargingType、MouseAcceleration、ShiftModifier、LaunchProgram、DinputOnly、StartTouchpadOff、UseTPforControls、UseSAforMouse）の読込。ShiftModifier と UseTPforControls の有無は、後の節が使うため文脈に書き戻す。
    /// <c>LegacyProfileXmlSerializer.LoadProfile</c> から移したもの。移動のみで、要素の順序・既定値・例外の扱いは変えない。
    /// </summary>
    internal sealed class ProfileMiscLoadSection : IProfileLoadSection
    {
        public void Load(ProfileLoadContext context)
        {
            BackingStore store = context.Store;
            XmlDocument m_Xdoc = context.Xdoc;
            string rootname = context.RootName;
            int device = context.Device;
            LightbarDS4WinInfo lightInfo = context.LightInfo;
            bool launchprogram = context.LaunchProgram;
            DS4Windows.DI.IProfileSettingsService _profileSettings = context.ProfileSettings;
            ControlService control = context.Control;
            bool missingSetting = context.MissingSetting;
            XmlNode Item;

            try { Item = m_Xdoc.SelectSingleNode("/" + rootname + "/ChargingType"); int.TryParse(Item.InnerText, out lightInfo.chargingType); }
            catch { missingSetting = true; }
            try { Item = m_Xdoc.SelectSingleNode("/" + rootname + "/MouseAcceleration"); bool.TryParse(Item.InnerText, out store.buttonMouseInfos[device].mouseAccel); }
            catch { missingSetting = true; }

            int shiftM = 0;
            if (m_Xdoc.SelectSingleNode("/" + rootname + "/ShiftModifier") != null)
                int.TryParse(m_Xdoc.SelectSingleNode("/" + rootname + "/ShiftModifier").InnerText, out shiftM);

            try
            {
                Item = m_Xdoc.SelectSingleNode("/" + rootname + "/LaunchProgram");
                store.launchProgram[device] = Item.InnerText;
            }
            catch { store.launchProgram[device] = string.Empty; missingSetting = true; }

            if (launchprogram == true && store.launchProgram[device] != string.Empty)
            {
                string programPath = store.launchProgram[device];
                bool procFound = false;
                bool handled = false;
                try
                {
                    var inspector = store.ProcessInspector;
                    if (inspector != null)
                    {
                        procFound = inspector.IsProcessRunning(programPath);
                        handled = true;
                    }
                }
                catch { }

                if (!handled)
                {
                    System.Diagnostics.Process[] localAll = System.Diagnostics.Process.GetProcesses();
                    for (int procInd = 0, procsLen = localAll.Length; !procFound && procInd < procsLen; procInd++)
                    {
                        try
                        {
                            string temp = localAll[procInd].MainModule.FileName;
                            if (temp == programPath)
                            {
                                procFound = true;
                            }
                        }
                        // Ignore any process for which this information
                        // is not exposed
                        catch { }
                    }
                }

                if (!procFound)
                {
                    Task processTask = new Task(() =>
                    {
                        Thread.Sleep(5000);
                        System.Diagnostics.Process tempProcess = new System.Diagnostics.Process();
                        tempProcess.StartInfo.FileName = programPath;
                        tempProcess.StartInfo.WorkingDirectory = new FileInfo(programPath).Directory.ToString();
                        //tempProcess.StartInfo.UseShellExecute = false;
                        try { tempProcess.Start(); }
                        catch { }
                    });

                    processTask.Start();
                }
            }

            try
            {
                Item = m_Xdoc.SelectSingleNode("/" + rootname + "/DinputOnly");
                bool.TryParse(Item.InnerText, out store.dinputOnly[device]);
            }
            catch { store.dinputOnly[device] = false; missingSetting = true; }

            bool oldUseDInputOnly = _profileSettings.UseDInputOnlyArray[device];

            try
            {
                Item = m_Xdoc.SelectSingleNode("/" + rootname + "/StartTouchpadOff");
                bool.TryParse(Item.InnerText, out store.startTouchpadOff[device]);
                if (store.startTouchpadOff[device] == true) control.StartTPOff(device);
            }
            catch { store.startTouchpadOff[device] = false; missingSetting = true; }

            // Fallback lookup if TouchpadOutMode is not set
            bool tpForControlsPresent = false;
            XmlNode xmlUseTPForControlsElement =
                m_Xdoc.SelectSingleNode("/" + rootname + "/UseTPforControls");
            tpForControlsPresent = xmlUseTPForControlsElement != null;
            if (tpForControlsPresent)
            {
                try
                {
                    Item = m_Xdoc.SelectSingleNode("/" + rootname + "/UseTPforControls");
                    if (bool.TryParse(Item?.InnerText ?? "", out bool temp))
                    {
                        if (temp) store.touchOutMode[device] = TouchpadOutMode.Controls;
                    }
                }
                catch { store.touchOutMode[device] = TouchpadOutMode.Mouse; }
            }

            // Fallback lookup if GyroOutMode is not set
            try
            {
                Item = m_Xdoc.SelectSingleNode("/" + rootname + "/UseSAforMouse");
                if (bool.TryParse(Item?.InnerText ?? "", out bool temp))
                {
                    if (temp) store.gyroOutMode[device] = GyroOutMode.Mouse;
                }
            }
            catch { store.gyroOutMode[device] = GyroOutMode.Controls; }

            context.ShiftModifier = shiftM;
            context.TpForControlsPresent = tpForControlsPresent;
            context.MissingSetting = missingSetting;
        }
    }
}
