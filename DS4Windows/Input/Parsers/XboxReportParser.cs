using System;
using DS4Windows.Core.Model;
using DS4Windows.Input.Abstractions;

namespace DS4Windows.Input.Parsers
{
    /// <summary>
    /// USB の Xbox 360 コントローラー（Windows の <c>xinputhid</c> が見せる、15 バイトの HID 入力レポート。先頭はレポート ID の 0）の解析。
    /// 並びは実機の記録で決めた（<c>docs-forDIMG/MadeByAgent/Phase22-Plan.md</c> のデコード表）:
    /// 軸は 16 ビットの符号なし（リトルエンディアン）、トリガーは 1 本の軸に合成（LT が大きい側、RT が小さい側）、
    /// ボタンは 16 ビット、方向キーはハット値。ボタンの名前は、位置が対応する DS4 の名前へ揃える
    /// （A＝Cross、B＝Circle、X＝Square、Y＝Triangle、LB／RB＝L1／R1、Back＝Share、Start＝Options、押し込み＝L3／R3）。
    /// Guide は HID の子に出ないため扱わない。毎フレームの経路のため、割り当てを行わない（状態を持たない）。
    /// <para>
    /// TODO（Phase22-22-1b〜22-2）: 現在は、どこからも使われていない。標準の Xbox（USB）は HID の子を持たず、
    /// HID の読み取りでは読めないと分かったため、Phase22 の入力は XInput を主経路にした（Phase22-Plan.md 決定 2）。
    /// これは、HID の子も出す機器（PS4 と Xbox の両対応機など）の記録から作った解析器で、記録を台本にした単体テストがある。
    /// XInput の経路の実機の確認が済んだ後（22-2）に、温存するか削除するかを決める（規則 2.4）。
    /// </para>
    /// </summary>
    public sealed class XboxReportParser : IInputReportParser
    {
        /// <summary>入力レポートの長さ（レポート ID を含む）。</summary>
        public const int ReportLength = 15;

        private const int AxisCenter = 0x8000;

        public void Parse(ReadOnlySpan<byte> inputReport, ControllerState cState)
        {
            if (inputReport.Length < ReportLength) return;   // 想定外の短いレポートは読み捨てる（状態は前のまま）

            // 16 ビットの軸を 0〜255 へ。四捨五入して、LX の中立（0x8000）と LY の中立（0x7FFF）をどちらも 128 にそろえる
            cState.LX = ToByte(ReadU16(inputReport, 1));
            cState.LY = ToByte(ReadU16(inputReport, 3));
            cState.RX = ToByte(ReadU16(inputReport, 5));
            cState.RY = ToByte(ReadU16(inputReport, 7));

            // トリガーは 1 本の軸に合成されている。両方を同時に押すと打ち消し合う（機器の仕様）
            int z = ReadU16(inputReport, 9);
            cState.L2 = z > AxisCenter ? TriggerByte(z - AxisCenter) : (byte)0;
            cState.R2 = z < AxisCenter ? TriggerByte(AxisCenter - z) : (byte)0;
            cState.L2Raw = cState.L2;
            cState.R2Raw = cState.R2;

            byte b = inputReport[11];
            cState.Cross = (b & 0x01) != 0;      // A
            cState.Circle = (b & 0x02) != 0;     // B
            cState.Square = (b & 0x04) != 0;     // X
            cState.Triangle = (b & 0x08) != 0;   // Y
            cState.L1 = (b & 0x10) != 0;         // LB
            cState.R1 = (b & 0x20) != 0;         // RB
            cState.Share = (b & 0x40) != 0;      // Back
            cState.Options = (b & 0x80) != 0;    // Start

            b = inputReport[12];
            cState.L3 = (b & 0x01) != 0;
            cState.R3 = (b & 0x02) != 0;

            // 方向キー: bit2〜5 のハット値。0＝なし、1＝上から時計回りに 8＝左上
            int hat = (b >> 2) & 0x0F;
            cState.DpadUp = hat == 1 || hat == 2 || hat == 8;
            cState.DpadRight = hat >= 2 && hat <= 4;
            cState.DpadDown = hat >= 4 && hat <= 6;
            cState.DpadLeft = hat >= 6 && hat <= 8;
        }

        private static int ReadU16(ReadOnlySpan<byte> report, int offset) => report[offset] | (report[offset + 1] << 8);

        private static byte ToByte(int value) => (byte)Math.Min((value + 0x80) >> 8, 255);

        // 0〜0x7FFF を 0〜255 へ（0x7F80 以上は 255）
        private static byte TriggerByte(int value) => (byte)Math.Min(value >> 7, 255);
    }
}
