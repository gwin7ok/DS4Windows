using System;
using DS4Windows.Core.Model;
using DS4Windows.Input.Abstractions;

namespace DS4Windows.Input.Parsers
{
    /// <summary>
    /// Joy-Con（左）の入力レポートの、ボタン・方向キー・ZL・サイドボタンの解析。
    /// スティックは、機器ごとに読んだ校正値で換算し、最初のレポートで校正値を補正するため、デバイスの状態に依存してデバイス側に残る。
    /// 毎フレームの経路のため、割り当てを行わない。
    /// </summary>
    public sealed class JoyConLeftReportParser : IInputReportParser
    {
        public void Parse(ReadOnlySpan<byte> inputReport, DS4State cState)
        {
            byte tempByte;
            tempByte = inputReport[4];
            cState.Share = (tempByte & 0x01) != 0;
            cState.L3 = (tempByte & 0x08) != 0;

            // Capture Button
            //cState.PS = (tempByte & 0x20) != 0;
            cState.Capture = (tempByte & 0x20) != 0;

            tempByte = inputReport[5];
            cState.DpadUp = (tempByte & 0x02) != 0;
            cState.DpadDown = (tempByte & 0x01) != 0;
            cState.DpadLeft = (tempByte & 0x08) != 0;
            cState.DpadRight = (tempByte & 0x04) != 0;
            cState.L1 = (tempByte & 0x40) != 0;
            cState.L2Btn = (tempByte & 0x80) != 0;

            cState.L2 = (byte)(cState.L2Btn ? 255 : 0);
            cState.L2Raw = cState.L2;

            cState.SideL = (tempByte & 0x20) != 0;
            cState.SideR = (tempByte & 0x10) != 0;
        }
    }
}
