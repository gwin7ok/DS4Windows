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

using DS4Windows.Core.Model;
using System;

namespace DS4Windows.Input
{

    public class SixAxisEventArgs : EventArgs
    {
        public SixAxis sixAxis { get; private set; }
        public DateTime timeStamp { get; private set; }
        public SixAxisEventArgs()
        {
        }

        public SixAxisEventArgs(DateTime utcTimestamp, SixAxis sa)
        {
            Set(utcTimestamp, sa);
        }

        /// <summary>値を入れ替える。毎フレーム生成しないよう、持ち主が 1 つを使い回す。受け取る側は、呼び出しの間だけ読む。</summary>
        public void Set(DateTime utcTimestamp, SixAxis sa)
        {
            sixAxis = sa;
            timeStamp = utcTimestamp;
        }
    }
}