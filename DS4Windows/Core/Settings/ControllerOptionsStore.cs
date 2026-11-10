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
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Serialization;
using DS4Windows.Core.Model;
using DS4Windows.Core.Persistence.Dto;

namespace DS4Windows.Core.Settings
{

    public abstract class ControllerOptionsStore
    {
        protected InputDeviceType deviceType;
        public InputDeviceType DeviceType { get => deviceType; }

        public ControllerOptionsStore(InputDeviceType deviceType)
        {
            this.deviceType = deviceType;
        }

        /// <summary>XML の節の名前。null のとき（保存する設定がない機種）は、保存も読み込みも何もしない。</summary>
        protected virtual string SectionName => null;

        /// <summary>保存する設定項目の定義（保存する順）。</summary>
        protected virtual IReadOnlyList<ControllerSetting> Schema => Array.Empty<ControllerSetting>();

        // 定義の並びを、親の節の下へ 1 つの節として置く（同名の節があれば置き換える）。保存形式は機種に関わらず同じ
        public void PersistSettings(XmlDocument xmlDoc, XmlNode node)
        {
            string name = SectionName;
            if (name == null)
                return;

            XmlElement section = xmlDoc.CreateElement(name);
            foreach (ControllerSetting setting in Schema)
            {
                XmlElement item = xmlDoc.CreateElement(setting.XmlName);
                item.InnerText = setting.Read();
                section.AppendChild(item);
            }

            XmlNode existing = node.SelectSingleNode(name);
            if (existing != null)
            {
                node.RemoveChild(existing);
            }

            node.AppendChild(section);
        }

        // 親の節の下の該当の節を読み、適用する。節がない場合と、列挙型の値が解析できない場合は、何も適用しない（解析を終えてからまとめて適用する）
        public void LoadSettings(XmlDocument xmlDoc, XmlNode node)
        {
            string name = SectionName;
            XmlNode section = name == null ? null : node.SelectSingleNode(name);
            if (section == null)
                return;

            var applies = new List<Action>();
            foreach (ControllerSetting setting in Schema)
            {
                Action apply = setting.Parse(section.SelectSingleNode(setting.XmlName)?.InnerText);
                if (apply == null)
                    return;

                applies.Add(apply);
            }

            foreach (Action apply in applies)
            {
                apply();
            }
        }
    }
}
