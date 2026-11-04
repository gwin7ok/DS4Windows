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
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Xml;
using System.Xml.Serialization;
using DS4Windows.Core.Platform;
using DS4Windows.Core.Settings;

namespace DS4Windows.Core.Profiles
{

    public class AutoProfileEntity
    {
        // Phase12-Step2 2b-1: 対象の指定（Path／Title）と照合の規則は、WindowTargetMatcher へ取り出した（動作は変えない）。
        // 旧 public フィールド path／title は、ログ出力が読むため、読み取り専用のプロパティとして残す。
        private readonly WindowTargetMatcher matcher = new WindowTargetMatcher(string.Empty, string.Empty);
        public string path => matcher.Path;
        public string title => matcher.Title;
        private bool turnoff;
        // Phase10-Step4d W5d: 各コントローラーのプロファイルの保管は ID（名前を読み書きする窓口）。登録簿は、ルールがホルダーに入るとき（AutoProfileHolder）に結ぶ
        private ProfileSlotNames profileNames = new ProfileSlotNames(ControllerLimits.MaxControllerCount);
        public const string NONE_STRING = "(none)";

        public string Path { get => matcher.Path; set => matcher.Path = value; }
        public string Title { get => matcher.Title; set => matcher.Title = value; }
        public bool Turnoff { get => turnoff; set => turnoff = value; }
        public ProfileSlotNames ProfileNames { get => profileNames; set => profileNames = value; }

        /// <summary>Phase10-Step4d W5d: 登録簿を結ぶ（ID ↔ 名前を引く）。null なら、結びを外す。</summary>
        public void AttachProfileIdentity(DS4Windows.Core.Abstractions.IProfileIdentityService identity) => profileNames.AttachIdentity(identity);

        public AutoProfileEntity(string pathStr, string titleStr)
        {
            // Initialize autoprofile search keywords(xxx_tolower).To improve performance the search keyword is pre - calculated in xxx_tolower variables,
            // so autoprofile timer thread doesn't have to create substrings/replace/tolower string instances every second over and over again.
            matcher.Path = pathStr;
            matcher.Title = titleStr;
        }

        public bool IsMatch(string searchPath, string searchTitle) => matcher.IsMatch(searchPath, searchTitle);
    }
}
