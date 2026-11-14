using System;
using DS4Windows.Core.Model;

namespace DS4Windows.Input.Parsers
{
    /// <summary>
    /// DS4 と DualSense の入力レポートで、位置だけが違い、形が同じ部分の読み取り（方向キー、タッチ 1 本）。
    /// 状態を持たず、割り当てを行わない。解析器の全体は機種ごとに分けたまま、この 2 つだけを共有する。
    /// </summary>
    public static class SonyReportDecoding
    {
        /// <summary>
        /// 下位 4 ビットの方向キー（時計の表現で、0 が上、8 が中立）を、4 つの bool へ書く。範囲外は中立。
        /// </summary>
        public static void ApplyDpad(ControllerState cState, byte dpadState)
        {
            switch (dpadState)
            {
                case 0: cState.DpadUp = true; cState.DpadDown = false; cState.DpadLeft = false; cState.DpadRight = false; break;
                case 1: cState.DpadUp = true; cState.DpadDown = false; cState.DpadLeft = false; cState.DpadRight = true; break;
                case 2: cState.DpadUp = false; cState.DpadDown = false; cState.DpadLeft = false; cState.DpadRight = true; break;
                case 3: cState.DpadUp = false; cState.DpadDown = true; cState.DpadLeft = false; cState.DpadRight = true; break;
                case 4: cState.DpadUp = false; cState.DpadDown = true; cState.DpadLeft = false; cState.DpadRight = false; break;
                case 5: cState.DpadUp = false; cState.DpadDown = true; cState.DpadLeft = true; cState.DpadRight = false; break;
                case 6: cState.DpadUp = false; cState.DpadDown = false; cState.DpadLeft = true; cState.DpadRight = false; break;
                case 7: cState.DpadUp = true; cState.DpadDown = false; cState.DpadLeft = true; cState.DpadRight = false; break;
                case 8:
                default: cState.DpadUp = false; cState.DpadDown = false; cState.DpadLeft = false; cState.DpadRight = false; break;
            }
        }

        /// <summary>
        /// タッチ 1 本の 4 バイト（<paramref name="pos"/> から: 追跡番号と非接触の印、X の下位 8 ビット、X の上位 4 ビットと Y の下位 4 ビット、Y の上位 8 ビット）を読む。
        /// </summary>
        public static void ReadTouch(ReadOnlySpan<byte> report, int pos, ref ControllerState.TrackPadTouch touch)
        {
            touch.RawTrackingNum = report[pos];
            touch.Id = (byte)(report[pos] & 0x7f);
            touch.IsActive = (report[pos] & 0x80) == 0;
            touch.X = (short)(((ushort)(report[pos + 2] & 0x0f) << 8) | (ushort)(report[pos + 1]));
            touch.Y = (short)(((ushort)(report[pos + 3]) << 4) | ((ushort)(report[pos + 2] & 0xf0) >> 4));
        }
    }
}
