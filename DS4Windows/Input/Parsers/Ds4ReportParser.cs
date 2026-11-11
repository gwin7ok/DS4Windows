using System;
using DS4Windows.Core.Model;
using DS4Windows.Input.Abstractions;

namespace DS4Windows.Input.Parsers
{
    /// <summary>
    /// DS4（USB 形式に揃えた 64 バイトの入力レポート。Bluetooth は先頭 2 バイトを除いて同じ並びにしたもの）の解析。
    /// 毎フレームの経路のため、割り当てを行わない（状態を持たず、既存の <see cref="ControllerState"/> へ書くだけ）。
    /// </summary>
    public sealed class Ds4ReportParser : IInputReportParser
    {
        public void Parse(ReadOnlySpan<byte> inputReport, ControllerState cState)
        {
            cState.LX = inputReport[1];
            cState.LY = inputReport[2];
            cState.RX = inputReport[3];
            cState.RY = inputReport[4];
            cState.L2 = inputReport[8];
            cState.R2 = inputReport[9];
            cState.L2Raw = cState.L2;
            cState.R2Raw = cState.R2;

            byte tempByte = inputReport[5];
            cState.Triangle = (tempByte & (1 << 7)) != 0;
            cState.Circle = (tempByte & (1 << 6)) != 0;
            cState.Cross = (tempByte & (1 << 5)) != 0;
            cState.Square = (tempByte & (1 << 4)) != 0;

            // 下位 4 ビットは方向キー。時計の表現で、8 が中立、0 が上
            byte dpad_state = (byte)(tempByte & 0x0F);

            switch (dpad_state)
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

            tempByte = inputReport[6];
            cState.R3 = (tempByte & (1 << 7)) != 0;
            cState.L3 = (tempByte & (1 << 6)) != 0;
            cState.Options = (tempByte & (1 << 5)) != 0;
            cState.Share = (tempByte & (1 << 4)) != 0;
            cState.R2Btn = (inputReport[6] & (1 << 3)) != 0;
            cState.L2Btn = (inputReport[6] & (1 << 2)) != 0;
            cState.R1 = (tempByte & (1 << 1)) != 0;
            cState.L1 = (tempByte & (1 << 0)) != 0;

            tempByte = inputReport[7];
            cState.PS = (tempByte & (1 << 0)) != 0;
            cState.TouchButton = (tempByte & 0x02) != 0;

            cState.OutputTouchButton = cState.TouchButton;
            cState.FrameCounter = (byte)(tempByte >> 2);

            // タッチパッドの生値（2 本分）
            cState.TrackPadTouch0.RawTrackingNum = inputReport[35];
            cState.TrackPadTouch0.Id = (byte)(inputReport[35] & 0x7f);
            cState.TrackPadTouch0.IsActive = (inputReport[35] & 0x80) == 0;
            cState.TrackPadTouch0.X = (short)(((ushort)(inputReport[37] & 0x0f) << 8) | (ushort)(inputReport[36]));
            cState.TrackPadTouch0.Y = (short)(((ushort)(inputReport[38]) << 4) | ((ushort)(inputReport[37] & 0xf0) >> 4));

            cState.TrackPadTouch1.RawTrackingNum = inputReport[39];
            cState.TrackPadTouch1.Id = (byte)(inputReport[39] & 0x7f);
            cState.TrackPadTouch1.IsActive = (inputReport[39] & 0x80) == 0;
            cState.TrackPadTouch1.X = (short)(((ushort)(inputReport[41] & 0x0f) << 8) | (ushort)(inputReport[40]));
            cState.TrackPadTouch1.Y = (short)(((ushort)(inputReport[42]) << 4) | ((ushort)(inputReport[41] & 0xf0) >> 4));
        }
    }
}
