using System;
using DS4Windows.Core.Model;
using DS4Windows.Input.Abstractions;

namespace DS4Windows.Input.Parsers
{
    /// <summary>
    /// Switch Pro コントローラーの入力レポート（標準モード）の、ボタン・方向キー・ZL／ZR（押した／離したの 2 値）の解析。
    /// スティックは、機器ごとに読んだ校正値（最小・最大・オフセット）で換算するため、デバイスの状態に依存してデバイス側に残る。
    /// 毎フレームの経路のため、割り当てを行わない。
    /// </summary>
    public sealed class SwitchProReportParser : IInputReportParser
    {
        public void Parse(ReadOnlySpan<byte> inputReport, ControllerState cState)
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

            tempByte = inputReport[4];
            cState.Share = (tempByte & 0x01) != 0;
            cState.Options = (tempByte & 0x02) != 0;
            cState.PS = (tempByte & 0x10) != 0;
            cState.Capture = (tempByte & 0x20) != 0;
            cState.L3 = (tempByte & 0x08) != 0;
            cState.R3 = (tempByte & 0x04) != 0;

            tempByte = inputReport[5];
            cState.DpadUp = (tempByte & 0x02) != 0;
            cState.DpadDown = (tempByte & 0x01) != 0;
            cState.DpadLeft = (tempByte & 0x08) != 0;
            cState.DpadRight = (tempByte & 0x04) != 0;
            cState.L1 = (tempByte & 0x40) != 0;
            cState.L2Btn = (tempByte & 0x80) != 0;
            cState.L2 = (byte)(cState.L2Btn ? 255 : 0);
            cState.L2Raw = cState.L2;
        }
    }
}
