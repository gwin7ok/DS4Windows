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

using System;
using DS4Windows.Core.Model;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Serialization;

namespace DS4Windows.Core.Settings
{
    public class ControlServiceDeviceOptions
    {
        // 機種の系統ごとの設定。系統を足すときは、ここへ 1 行足す（保存・画面・接続フックは系統で引く）
        private readonly Dictionary<InputDeviceFamily, DeviceOptionsBase> byFamily = new Dictionary<InputDeviceFamily, DeviceOptionsBase>
        {
            [InputDeviceFamily.DS4] = new DS4DeviceOptions(),
            [InputDeviceFamily.DualSense] = new DualSenseDeviceOptions(),
            [InputDeviceFamily.SwitchPro] = new SwitchProDeviceOptions(),
            [InputDeviceFamily.JoyCon] = new JoyConDeviceOptions(),
            [InputDeviceFamily.DS3] = new DS3DeviceOptions(),
        };

        /// <summary>系統の設定（有効・無効と変更の通知）。</summary>
        public DeviceOptionsBase this[InputDeviceFamily family] => byFamily[family];

        /// <summary>系統の設定を、機種固有の型で受け取る（例: Joy-Con の結合の設定）。</summary>
        public T Get<T>(InputDeviceFamily family) where T : DeviceOptionsBase => (T)byFamily[family];

        /// <summary>設定を持つ系統の一覧。</summary>
        public IEnumerable<InputDeviceFamily> Families => byFamily.Keys;

        private bool verboseLogMessages;
        public bool VerboseLogMessages { get => verboseLogMessages; set => verboseLogMessages = value; }

        public ControlServiceDeviceOptions()
        {
            // If enabled then DS4Windows shows additional log messages when a gamepad is connected (may be useful to diagnose connection problems).
            // This option is not persistent (ie. not saved into config files), so if enabled then it is reset back to FALSE when DS4Windows is restarted.
            verboseLogMessages = false;
        }
    }
}
