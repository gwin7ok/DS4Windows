/*
DS4Windows
Copyright (C) 2023  Travis Nickles

This program is free software: you can redistribute it and/or modify
it under the terms of the GNU General Public License as published by
the Free Software Foundation, either version 3 of the License, or
(at your option) any later version.

This program is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
GNU General Public License for more details.

You should have received a copy of the GNU General Public License
along with this program.  If not, see <https://www.gnu.org/licenses/>.
*/

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
namespace DS4Windows
{

    public class DS4ControlSettings
    {
        public const int MAX_MACRO_VALUE = 288;
        public const byte LAST_DS4_ACTION = (byte)DS4Controls.TouchEnded;


        public DS4Controls control;
        public string extras = null;
        public DS4KeyType keyType = DS4KeyType.None;
        // Phase10-Step4 W1: Special は、SA 専用の種別（Program、Profile、DisconnectBT、BatteryCheck、MultiAction、較正）の割り当て。内容は special（型のある項目）が持つ
        public enum ActionType : byte { Default, Key, Button, Macro, Special };
        public ActionType actionType = ActionType.Default;
        public ControlActionData action = new ControlActionData();

        /// <summary>
        /// Phase10-Step4 W1（Phase10-Step4-Plan.md §3.1）: SA 専用の種別と項目（型のある項目）。<see cref="actionType"/> が <c>Special</c> のとき、その割り当ての内容。
        /// <c>Macro</c> のときは、Macro の SA 専用の 3 項目（Run on release／Synchronized／Keep key state）の入れ物。それ以外は null。
        /// </summary>
        public ControlSpecialItem special;

        // Phase8-Step7 7.8e: 「Lightbar マクロ」機能は廃止した（Phase8-Step7-8e-Plan.md Q8＝B。プロファイルに保存されず、実質使えなかった）。

        // Phase8-Step7 7.8d: 「シフト」機能は廃止した（Phase8-Step7-8d-Plan.md）。プロファイルに残っているシフトの設定は、解釈せず、RemovedShiftSettings に保持する。

        public bool IsDefault { get => actionType == ActionType.Default; }

        public DS4ControlSettings(DS4Controls ctrl)
        {
            control = ctrl;
        }

        public void Reset()
        {
            extras = null;
            keyType = DS4KeyType.None;
            actionType = ActionType.Default;
            action = new ControlActionData();
            special = null;
            action.actionAlias = 0;
            //actionAlias = 0;
        }

        public bool IsExtrasEmpty(string extraStr)
        {
            return string.IsNullOrEmpty(extraStr) || extraStr == "0,0,0,0,0,0,0,0,0";
        }

        internal void UpdateSettings(object act, string exts, DS4KeyType kt)
        {
            if (act is int || act is ushort)
            {
                actionType = ActionType.Key;
                action.actionKey = Convert.ToInt32(act);
            }
            else if (act is string || act is X360Controls)
            {
                actionType = ActionType.Button;
                if (act is X360Controls)
                {
                    action.actionBtn = (X360Controls)act;
                }
                else
                {
                    Enum.TryParse(act.ToString(), out action.actionBtn);
                }
            }
            else if (act is int[])
            {
                actionType = ActionType.Macro;
                action.actionMacro = (int[])act;
            }
            else
            {
                actionType = ActionType.Default;
                action.actionKey = 0;
            }

            extras = exts;
            keyType = kt;
        }
    }
}

