using System;
using DS4Windows.Core.Model;
using DS4Windows.Input.Abstractions;

namespace DS4Windows.Input.Parsers
{
    /// <summary>
    /// DS3 の特徴レポート（機器から読むバイト列。先頭にレポート ID の 1 バイトがある）の、ボタン、感圧のある方向キー・面ボタン、トリガー、スティックの解析。
    /// 方向キーと面ボタンは、ビットが立っていて、かつ圧力の値が 0 より大きいときだけ押した扱いにする。毎フレームの経路のため、割り当てを行わない。
    /// </summary>
    public sealed class Ds3ReportParser : IInputReportParser
    {
        public void Parse(ReadOnlySpan<byte> inputReport, DS4State cState)
        {
            const int reportOffset = 1;
            cState.Share = (inputReport[2 + reportOffset] & 0x01) > 0;
            cState.L3 = (inputReport[2 + reportOffset] & 0x02) > 0;
            cState.R3 = (inputReport[2 + reportOffset] & 0x04) > 0;
            cState.Options = (inputReport[2 + reportOffset] & 0x08) > 0;
            cState.DpadUp = ((inputReport[2 + reportOffset] & 0x10) > 0) && inputReport[14 + reportOffset] > 0;
            cState.DpadRight = ((inputReport[2 + reportOffset] & 0x20) > 0) && inputReport[15 + reportOffset] > 0;
            cState.DpadDown = ((inputReport[2 + reportOffset] & 0x40) > 0) && inputReport[16 + reportOffset] > 0;
            cState.DpadLeft = ((inputReport[2 + reportOffset] & 0x80) > 0) && inputReport[17 + reportOffset] > 0;

            cState.L2 = ((inputReport[3 + reportOffset] & 0x01) > 0) ? inputReport[18 + reportOffset] : (byte)0x00;
            cState.R2 = ((inputReport[3 + reportOffset] & 0x02) > 0) ? inputReport[19 + reportOffset] : (byte)0x00;

            cState.L1 = ((inputReport[3 + reportOffset] & 0x04) > 0) && inputReport[20 + reportOffset] > 0;
            cState.R1 = ((inputReport[3 + reportOffset] & 0x08) > 0) && inputReport[21 + reportOffset] > 0;
            cState.Triangle = ((inputReport[3 + reportOffset] & 0x10) > 0) && inputReport[22 + reportOffset] > 0;
            cState.Circle = ((inputReport[3 + reportOffset] & 0x20) > 0) && inputReport[23 + reportOffset] > 0;
            cState.Cross = ((inputReport[3 + reportOffset] & 0x40) > 0) && inputReport[24 + reportOffset] > 0;
            cState.Square = ((inputReport[3 + reportOffset] & 0x80) > 0) && inputReport[25 + reportOffset] > 0;
            cState.PS = (inputReport[4 + reportOffset] & 0x01) > 0;

            cState.L2Btn = cState.L2 > 0;
            cState.R2Btn = cState.R2 > 0;

            cState.L2Raw = cState.L2;
            cState.R2Raw = cState.R2;

            cState.LX = inputReport[6 + reportOffset];
            cState.LY = inputReport[7 + reportOffset];
            cState.RX = inputReport[8 + reportOffset];
            cState.RY = inputReport[9 + reportOffset];
        }
    }
}
