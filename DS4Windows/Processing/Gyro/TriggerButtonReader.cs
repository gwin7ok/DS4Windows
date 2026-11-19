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

using DS4Windows.Core.Model;

namespace DS4Windows.Processing.Gyro
{
    /// <summary>
    /// 設定の「トリガー」の番号（-1 は常に真、0〜26 はボタン）から、ボタンが押されているかを読む。
    /// ジャイロのトリガーと、タッチパッドのスワイプの無効化のボタンの指定が使う。
    /// </summary>
    public static class TriggerButtonReader
    {
        public static bool IsPressed(ControllerState s, int key)
        {
            switch (key)
            {
                case -1: return true;
                case 0: return s.Cross;
                case 1: return s.Circle;
                case 2: return s.Square;
                case 3: return s.Triangle;
                case 4: return s.L1;
                case 5: return s.L2 > 128;
                case 6: return s.R1;
                case 7: return s.R2 > 128;
                case 8: return s.DpadUp;
                case 9: return s.DpadDown;
                case 10: return s.DpadLeft;
                case 11: return s.DpadRight;
                case 12: return s.L3;
                case 13: return s.R3;
                case 14: return s.Touch1Finger;
                case 15: return s.Touch2Fingers;
                case 16: return s.Options;
                case 17: return s.Share;
                case 18: return s.PS;
                case 19: return s.TouchButton;
                case 20: return s.Mute;
                case 21: return s.SideL;
                case 22: return s.SideR;
                case 23: return s.FnL;
                case 24: return s.FnR;
                case 25: return s.BLP;
                case 26: return s.BRP;
                default: break;
            }

            return false;
        }
    }
}
