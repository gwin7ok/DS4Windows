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
using System.Xml.Serialization;
using DS4Windows;

namespace DS4WinWPF.DS4Control.DTOXml
{
    [XmlRoot("Programs")]
    public class AutoProfilesDTO : IDTO<AutoProfileHolder>
    {
        [XmlElement("Program")] // Use XmlElement here to skip container element
        public List<AutoProfileEntrySerializer> programEntries;

        /// <summary>Phase10-Step4d W4: 名前 → ID（保存で使う）。省略可（なければ、ID を書かない）。ホルダーが設定する。</summary>
        [XmlIgnore]
        public System.Func<string, string> ProfileIdOf { get; set; }

        /// <summary>Phase10-Step4d W4: ID → 現在の名前（読み込みで使う）。見つからなければ null。省略可。ホルダーが設定する。</summary>
        [XmlIgnore]
        public System.Func<string, string> ProfileNameOf { get; set; }

        public AutoProfilesDTO()
        {
            programEntries = new List<AutoProfileEntrySerializer>();
        }

        public void MapFrom(AutoProfileHolder source)
        {
            foreach(AutoProfileEntity entity in source.AutoProfileColl)
            {
                AutoProfileEntrySerializer temp = new AutoProfileEntrySerializer()
                {
                    Path = entity.Path,
                    Title = entity.Title,
                    Controller1 = entity.ProfileNames[0],
                    Controller2 = entity.ProfileNames[1],
                    Controller3 = entity.ProfileNames[2],
                    TurnOff = entity.Turnoff,
                };

                if (source.UsingMaxControllers)
                {
                    temp.Controller4 = entity.ProfileNames[3];
                    temp.Controller5 = entity.ProfileNames[4];
                    temp.Controller6 = entity.ProfileNames[5];
                    temp.Controller7 = entity.ProfileNames[6];
                    temp.Controller8 = entity.ProfileNames[7];
                }

                // Phase10-Step4d W4: プロファイルの固定 ID（Controller1〜8 と同じ並び。ないものは空）
                if (ProfileIdOf != null)
                {
                    var ids = new string[entity.ProfileNames.Length];
                    bool anyId = false;
                    for (int i = 0; i < ids.Length; i++)
                    {
                        string profileName = entity.ProfileNames[i];
                        string id = !string.IsNullOrEmpty(profileName) && profileName != AutoProfileEntity.NONE_STRING ? ProfileIdOf(profileName) : null;
                        ids[i] = id ?? string.Empty;
                        anyId |= ids[i].Length > 0;
                    }

                    temp.ControllerProfileIds = anyId ? string.Join("/", ids) : string.Empty;
                }

                programEntries.Add(temp);
            }
        }

        public void MapTo(AutoProfileHolder destination)
        {
            foreach(AutoProfileEntrySerializer serializier in programEntries)
            {
                AutoProfileEntity autoprof = new AutoProfileEntity(serializier.Path, serializier.Title);

                autoprof.ProfileNames[0] = serializier.Controller1;
                autoprof.ProfileNames[1] = serializier.Controller2;
                autoprof.ProfileNames[2] = serializier.Controller3;
                autoprof.ProfileNames[3] = serializier.Controller4;

                // Phase7-Step10e W1: 旧 CURRENT_DS4_CONTROLLER_LIMIT >= MAX_DS4_CONTROLLER_COUNT（上限が 8 のとき）。MapFrom と同じ判定を使う
                if (destination.UsingMaxControllers)
                {
                    autoprof.ProfileNames[4] = serializier.Controller5;
                    autoprof.ProfileNames[5] = serializier.Controller6;
                    autoprof.ProfileNames[6] = serializier.Controller7;
                    autoprof.ProfileNames[7] = serializier.Controller8;
                }

                // Phase10-Step4d W4: ID が見つかれば、現在の名前に置き換える（名前変更に追従。ID が勝つ）。ない・見つからないときは、名前のまま
                if (ProfileNameOf != null && !string.IsNullOrEmpty(serializier.ControllerProfileIds))
                {
                    string[] ids = serializier.ControllerProfileIds.Split('/');
                    for (int i = 0; i < ids.Length && i < autoprof.ProfileNames.Length; i++)
                    {
                        if (ids[i].Length == 0) continue;

                        string resolved = ProfileNameOf(ids[i]);
                        if (!string.IsNullOrEmpty(resolved)) autoprof.ProfileNames[i] = resolved;
                    }
                }

                autoprof.Turnoff = serializier.TurnOff;
                destination.AutoProfileColl.Add(autoprof);
            }
        }
    }
}
