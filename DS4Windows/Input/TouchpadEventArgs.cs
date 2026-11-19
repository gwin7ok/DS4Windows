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

namespace DS4Windows.Input
{
    /// <summary>
    /// タッチパッドのイベントの引数。毎フレーム生成しないよう、<see cref="DS4Touchpad"/> が 1 つを使い回す（<see cref="Set"/>）。
    /// 受け取る側は、呼び出しの間だけ読む。値を持ち続けるときは、値を写す（参照を保持しない）。
    /// </summary>
    public class TouchpadEventArgs : EventArgs
    {
        private readonly Touch[] oneTouch = new Touch[1];
        private readonly Touch[] twoTouches = new Touch[2];
        private Touch[]? touches = null;
        private DateTime timeStamp;
        private bool touchButtonPressed;
        private bool touchActive;

        public Touch[]? Touches => touches;

        public DateTime TimeStamp => timeStamp;

        public bool TouchButtonPressed => touchButtonPressed;

        public bool TouchActive => touchActive;

        public TouchpadEventArgs()
        {
        }

        public TouchpadEventArgs(DateTime utcTimestamp, bool tButtonDown, bool touchActive, Touch? t0, Touch? t1 = null)
        {
            Set(utcTimestamp, tButtonDown, touchActive, t0, t1);
        }

        /// <summary>値を入れ替える。t0 も t1 も null なら <see cref="Touches"/> は null、t1 だけ null なら長さ 1、両方あれば長さ 2。</summary>
        public void Set(DateTime utcTimestamp, bool tButtonDown, bool touchActive, Touch? t0, Touch? t1 = null)
        {
            if (t1 != null)
            {
                twoTouches[0] = t0!; // t1 があるときは t0 もある（Set の規約）
                twoTouches[1] = t1;
                touches = twoTouches;
            }
            else if (t0 != null)
            {
                oneTouch[0] = t0;
                touches = oneTouch;
            }
            else
            {
                touches = null;
            }

            touchButtonPressed = tButtonDown;
            this.touchActive = touchActive;
            timeStamp = utcTimestamp;
        }
    }
}
