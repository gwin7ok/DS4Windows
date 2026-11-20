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

#nullable enable

using System;
namespace DS4Windows.Core.Logging
{

    public class DebugEventArgs : EventArgs
    {
        protected DateTime m_Time = DateTime.Now;
        protected string m_Data = string.Empty;
        protected bool warning = false;
        protected bool temporary = false;
        protected AppLogLevel level = AppLogLevel.Info;
        public DebugEventArgs(string Data, bool warn, bool temporary = false)
        {
            m_Data = Data;
            warning = warn;
            this.temporary = temporary;
            level = warn ? AppLogLevel.Warn : AppLogLevel.Info;
        }

        // 新 API（AppLogger.Log）が、利用者向けのログのレベルを運ぶために追加。Warning は Warn 以上で真。
        public DebugEventArgs(string Data, AppLogLevel level, bool temporary = false)
        {
            m_Data = Data;
            this.level = level;
            warning = level >= AppLogLevel.Warn;
            this.temporary = temporary;
        }

        public AppLogLevel Level => level;

        /// <summary>
        /// ファイルへ書く文字列。画面（<see cref="Data"/>）は表示言語で出し、ファイルは不具合報告を読みやすくするため英語で書くので、
        /// 両者が異なるときだけ設定する（<see cref="AppLogger.LogUser"/>）。未設定のときは <see cref="Data"/> と同じ。
        /// </summary>
        public string? FileData { get; set; }

        public DateTime Time => m_Time;
        public string Data => m_Data;
        public bool Warning => warning;
        public bool Temporary => temporary;
    }
}

