using System;
using DS4Windows.Core.Model;
using DS4Windows.Input.Abstractions;
using DS4Windows.Input.Xbox;

namespace DS4Windows.Input.Parsers
{
    /// <summary>
    /// XInput の合成レポート（<see cref="XInputReportLayout"/>）の解析。標準の Xbox コントローラー（USB）は HID の子を持たず、XInput でしか読めないため、
    /// XInput の状態を HID のレポートに見立てて、他の機種と同じ流れ（<c>ProtocolDevice</c>）に載せている。
    /// ボタンの名前は、位置が対応する DS4 の名前へ揃える（A＝Cross、B＝Circle、X＝Square、Y＝Triangle、LB／RB＝L1／R1、Back＝Share、Start＝Options、押し込み＝L3／R3、Guide＝PS）。
    /// <c>L2Btn</c>／<c>R2Btn</c> は書かない（後段が <c>L2 &gt; 0</c> で決める）。毎フレームの経路のため、割り当てを行わない（状態を持たない）。
    /// </summary>
    public sealed class XInputReportParser : IInputReportParser
    {
        public void Parse(ReadOnlySpan<byte> inputReport, ControllerState cState)
        {
            if (inputReport.Length < XInputReportLayout.ReportLength) return;   // 想定外の短いレポートは読み捨てる（状態は前のまま）

            ushort buttons = (ushort)(inputReport[XInputReportLayout.ButtonsOffset] | (inputReport[XInputReportLayout.ButtonsOffset + 1] << 8));

            cState.Cross = (buttons & XInputReportLayout.A) != 0;
            cState.Circle = (buttons & XInputReportLayout.B) != 0;
            cState.Square = (buttons & XInputReportLayout.X) != 0;
            cState.Triangle = (buttons & XInputReportLayout.Y) != 0;
            cState.L1 = (buttons & XInputReportLayout.LeftShoulder) != 0;
            cState.R1 = (buttons & XInputReportLayout.RightShoulder) != 0;
            cState.Share = (buttons & XInputReportLayout.Back) != 0;
            cState.Options = (buttons & XInputReportLayout.Start) != 0;
            cState.L3 = (buttons & XInputReportLayout.LeftThumb) != 0;
            cState.R3 = (buttons & XInputReportLayout.RightThumb) != 0;
            cState.PS = (buttons & XInputReportLayout.Guide) != 0;
            cState.DpadUp = (buttons & XInputReportLayout.DpadUp) != 0;
            cState.DpadDown = (buttons & XInputReportLayout.DpadDown) != 0;
            cState.DpadLeft = (buttons & XInputReportLayout.DpadLeft) != 0;
            cState.DpadRight = (buttons & XInputReportLayout.DpadRight) != 0;

            cState.L2 = inputReport[XInputReportLayout.LeftTriggerOffset];
            cState.R2 = inputReport[XInputReportLayout.RightTriggerOffset];
            cState.L2Raw = cState.L2;
            cState.R2Raw = cState.R2;

            cState.LX = StickToByte(ReadS16(inputReport, XInputReportLayout.LeftStickXOffset));
            cState.LY = StickToByteInverted(ReadS16(inputReport, XInputReportLayout.LeftStickYOffset));
            cState.RX = StickToByte(ReadS16(inputReport, XInputReportLayout.RightStickXOffset));
            cState.RY = StickToByteInverted(ReadS16(inputReport, XInputReportLayout.RightStickYOffset));
        }

        private static int ReadS16(ReadOnlySpan<byte> report, int offset) => (short)(report[offset] | (report[offset + 1] << 8));

        // 符号付き 16 ビット（中立 0）→ 0〜255（中立 128）
        private static byte StickToByte(int value) => (byte)((value + 0x8000) >> 8);

        // Y は XInput が上を正にするため、DS4 の並び（上が 0）へ反転する。中立は 128
        private static byte StickToByteInverted(int value) => (byte)Math.Min((0x8000 - value) >> 8, 255);
    }
}
