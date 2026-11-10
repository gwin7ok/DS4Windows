using System;
using DS4Windows.Core.Model;
using DS4Windows.Input.Abstractions;

namespace DS4Windows.Input.Parsers
{
    /// <summary>
    /// DualSense の入力レポートの解析（ボタン、スティック、トリガー、方向キー、ミュート・背面ボタン、タッチパッドの生値）。
    /// Bluetooth のレポートは先頭にレポート ID の後の 1 バイトの余りがあるため、<paramref name="reportOffset"/> だけ後ろへずらして読む（USB は 0、Bluetooth は 1）。
    /// 毎フレームの経路のため、割り当てを行わない。
    /// </summary>
    public sealed class DualSenseReportParser : IInputReportParser
    {
        private readonly int reportOffset;

        public DualSenseReportParser(int reportOffset)
        {
            this.reportOffset = reportOffset;
        }

        public void Parse(ReadOnlySpan<byte> inputReport, ControllerState cState)
        {
            int reportOffset = this.reportOffset;
            byte tempByte;
            cState.LX = inputReport[1 + reportOffset];
            cState.LY = inputReport[2 + reportOffset];
            cState.RX = inputReport[3 + reportOffset];
            cState.RY = inputReport[4 + reportOffset];
            cState.L2 = inputReport[5 + reportOffset];
            cState.R2 = inputReport[6 + reportOffset];
            cState.L2Raw = cState.L2;
            cState.R2Raw = cState.R2;

            // DS4 Frame Counter range is [0-127]. DS version range is [0-255]. Convert
            cState.FrameCounter = (byte)(inputReport[7 + reportOffset] % 128);
            tempByte = inputReport[8 + reportOffset];
            cState.Triangle = (tempByte & (1 << 7)) != 0;
            cState.Circle = (tempByte & (1 << 6)) != 0;
            cState.Cross = (tempByte & (1 << 5)) != 0;
            cState.Square = (tempByte & (1 << 4)) != 0;

            // First 4 bits denote dpad state. Clock representation
            // with 8 meaning centered and 0 meaning DpadUp.
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

            tempByte = inputReport[9 + reportOffset];
            cState.R3 = (tempByte & (1 << 7)) != 0;
            cState.L3 = (tempByte & (1 << 6)) != 0;
            cState.Options = (tempByte & (1 << 5)) != 0;
            cState.Share = (tempByte & (1 << 4)) != 0;
            cState.R2Btn = (tempByte & (1 << 3)) != 0;
            cState.L2Btn = (tempByte & (1 << 2)) != 0;
            cState.R1 = (tempByte & (1 << 1)) != 0;
            cState.L1 = (tempByte & (1 << 0)) != 0;

            tempByte = inputReport[10 + reportOffset];
            cState.PS = (tempByte & (1 << 0)) != 0;
            cState.TouchButton = (tempByte & 0x02) != 0;

            cState.OutputTouchButton = cState.TouchButton;
            cState.Mute = (tempByte & (1 << 2)) != 0;
            cState.FnL = (tempByte & (1 << 4)) != 0;
            cState.FnR = (tempByte & (1 << 5)) != 0;
            cState.BLP = (tempByte & (1 << 6)) != 0;
            cState.BRP = (tempByte & (1 << 7)) != 0;

            // タッチパッドの生値（2 本分）
            cState.TrackPadTouch0.RawTrackingNum = inputReport[33+reportOffset];
            cState.TrackPadTouch0.Id = (byte)(inputReport[33+reportOffset] & 0x7f);
            cState.TrackPadTouch0.IsActive = (inputReport[33+reportOffset] & 0x80) == 0;
            cState.TrackPadTouch0.X = (short)(((ushort)(inputReport[35+reportOffset] & 0x0f) << 8) | (ushort)(inputReport[34+reportOffset]));
            cState.TrackPadTouch0.Y = (short)(((ushort)(inputReport[36+reportOffset]) << 4) | ((ushort)(inputReport[35+reportOffset] & 0xf0) >> 4));

            cState.TrackPadTouch1.RawTrackingNum = inputReport[37+reportOffset];
            cState.TrackPadTouch1.Id = (byte)(inputReport[37+reportOffset] & 0x7f);
            cState.TrackPadTouch1.IsActive = (inputReport[37+reportOffset] & 0x80) == 0;
            cState.TrackPadTouch1.X = (short)(((ushort)(inputReport[39+reportOffset] & 0x0f) << 8) | (ushort)(inputReport[38+reportOffset]));
            cState.TrackPadTouch1.Y = (short)(((ushort)(inputReport[40+reportOffset]) << 4) | ((ushort)(inputReport[39+reportOffset] & 0xf0) >> 4));
        }
    }
}
