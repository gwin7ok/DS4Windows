using System;
using DS4Windows.Core.Model;
using DS4Windows.Input.Abstractions;

namespace DS4Windows.Input.Parsers
{
    /// <summary>
    /// Joy-Con（右）の入力レポートの、ボタン・ZR・サイドボタンの解析。
    /// スティックは、機器ごとに読んだ校正値で換算し、最初のレポートで校正値を補正するため、デバイスの状態に依存してデバイス側に残る。
    /// 毎フレームの経路のため、割り当てを行わない。
    /// </summary>
    public sealed class JoyConRightReportParser : IInputReportParser
    {
        public void Parse(ReadOnlySpan<byte> inputReport, DS4State cState)
        {
            byte tempByte;
            tempByte = inputReport[3];
            cState.Circle = (tempByte & 0x08) != 0;
            cState.Cross = (tempByte & 0x04) != 0;
            cState.Triangle = (tempByte & 0x02) != 0;
            cState.Square = (tempByte & 0x01) != 0;
            cState.R1 = (tempByte & 0x40) != 0;

            cState.R2Btn = (tempByte & 0x80) != 0;
            cState.R2 = (byte)(cState.R2Btn ? 255 : 0);
            cState.R2Raw = cState.R2;

            cState.SideL = (tempByte & 0x20) != 0;
            cState.SideR = (tempByte & 0x10) != 0;

            tempByte = inputReport[4];
            cState.Options = (tempByte & 0x02) != 0;
            cState.R3 = (tempByte & 0x04) != 0;
            cState.PS = (tempByte & 0x10) != 0;
        }
    }
}
