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
    /// Phase8-Step7 7.8d（Phase8-Step7-8d-Plan.md §4.1）: 廃止された「シフト」機能の設定（<c>ShiftControl</c>）の読込。
    /// シフトの設定は解釈せず、実行にも使わない。<c>&lt;ShiftControl&gt;</c> ノードの内容を、そのまま <see cref="RemovedShiftSettings"/> に保持するだけ
    /// （編集画面でユーザーが「残す」を選んだとき、保存で、そのまま書き出す）。項目がなければ、保持を空にする。
    /// </summary>
    internal sealed class ProfileButtonShiftLoadSection : IProfileLoadSection
    {
        public void Load(ProfileLoadContext context)
        {
            XmlNode shiftControl = context.Xdoc.SelectSingleNode("/" + context.RootName + "/ShiftControl");
            context.Store.removedShiftSettings[context.Device].Capture(shiftControl);
        }
    }
}
