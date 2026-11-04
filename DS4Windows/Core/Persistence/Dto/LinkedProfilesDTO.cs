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
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;
using DS4Windows;

namespace DS4WinWPF.DS4Control.DTOXml
{
    [XmlRoot("LinkedControllers")]
    public class LinkedProfilesDTO : IDTO<BackingStore>, IXmlSerializable
    {
        private List<LinkedProfileItem> linkedProfileItems;

        /// <summary>Phase10-Step4d W4: 名前 → ID（保存で使う）。省略可（なければ、ID を書かない）。シリアライザーが設定する。</summary>
        public Func<string, string> ProfileIdOf { get; set; }

        /// <summary>Phase10-Step4d W4: ID → 現在の名前（読み込みで使う）。見つからなければ null。省略可。シリアライザーが設定する。</summary>
        public Func<string, string> ProfileNameOf { get; set; }

        public LinkedProfilesDTO()
        {
            linkedProfileItems = new List<LinkedProfileItem>();
        }

        public void MapFrom(BackingStore source)
        {
            foreach(KeyValuePair<string, string> pair in source.linkedProfiles)
            {
                LinkedProfileItem tempItem = new LinkedProfileItem()
                {
                    Serial = pair.Key.Replace("MAC", string.Empty),
                    Profile = pair.Value,
                    ProfileId = !string.IsNullOrEmpty(pair.Value) ? ProfileIdOf?.Invoke(pair.Value) : null,   // Phase10-Step4d W4
                };

                linkedProfileItems.Add(tempItem);
            }
        }

        public void MapTo(BackingStore destination)
        {
            foreach(LinkedProfileItem item in linkedProfileItems)
            {
                // Phase10-Step4d W4: ID が見つかれば、現在の名前に置き換える（名前変更に追従。ID が勝つ）。ない・見つからないときは、名前のまま
                string profile = item.Profile;
                if (!string.IsNullOrEmpty(item.ProfileId))
                {
                    string resolved = ProfileNameOf?.Invoke(item.ProfileId);
                    if (!string.IsNullOrEmpty(resolved)) profile = resolved;
                }

                destination.linkedProfiles[item.Serial] = profile;
            }
        }

        public XmlSchema GetSchema()
        {
            return null;
        }

        public void ReadXml(XmlReader reader)
        {
            XmlDocument tempDoc = new XmlDocument();
            tempDoc.Load(reader);

            XmlNode node = tempDoc.SelectSingleNode("/LinkedControllers");
            if (node == null)
            {
                return;
            }

            XmlNodeList links = node.ChildNodes;
            for (int i = 0, listLen = links.Count; i < listLen; i++)
            {
                XmlNode current = links[i];
                if (current.Name.Contains("MAC"))
                {
                    string serial = current.Name.Replace("MAC", string.Empty);
                    string profile = current.InnerText;
                    LinkedProfileItem tempItem = new LinkedProfileItem()
                    {
                        Serial = serial,
                        Profile = profile,
                        ProfileId = current.Attributes?["Id"]?.Value,   // Phase10-Step4d W4
                    };

                    linkedProfileItems.Add(tempItem);
                }
            }
        }

        public void WriteXml(XmlWriter writer)
        {
            int itemCount = linkedProfileItems.Count;
            if (itemCount == 0)
                return;

            foreach (LinkedProfileItem item in linkedProfileItems)
            {
                // 要素の中身（名前）は従来のまま。Phase10-Step4d W4 で、ID があれば Id 属性を足す（古いバージョンは中身だけを読む）
                writer.WriteStartElement($"MAC{item.Serial}");
                if (!string.IsNullOrEmpty(item.ProfileId)) writer.WriteAttributeString("Id", item.ProfileId);
                writer.WriteString(item.Profile);
                writer.WriteEndElement();
            }
        }
    }
}
