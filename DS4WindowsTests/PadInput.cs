#nullable enable
namespace DS4WindowsTests
{
    /// <summary>
    /// 仮想コントローラー（<see cref="VirtualPad"/>）へ与える、機種に依存しない入力の指定。
    /// 機種ごとのレポートのバイト列への変換は、各 <see cref="VirtualPad"/> が持つ。
    /// スティックは 0〜255（128 が中立）。トリガーは、アナログのない機種（Switch Pro）でも比べられるよう、押した・押さないの 2 値で与える。
    /// </summary>
    internal sealed class PadInput
    {
        public bool Cross { get; init; }
        public bool Circle { get; init; }
        public bool Square { get; init; }
        public bool Triangle { get; init; }
        public bool L1 { get; init; }
        public bool R1 { get; init; }
        public bool DpadUp { get; init; }
        public bool L2Pressed { get; init; }
        public bool R2Pressed { get; init; }
        public byte LX { get; init; } = 128;
        public byte LY { get; init; } = 128;
        public byte RX { get; init; } = 128;
        public byte RY { get; init; } = 128;
    }
}
