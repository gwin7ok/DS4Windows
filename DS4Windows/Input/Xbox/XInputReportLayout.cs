namespace DS4Windows.Input.Xbox
{
    /// <summary>
    /// XInput の状態を、HID の入力レポートのふりをして運ぶ「合成レポート」の並び（<c>XInputTransport</c> が作り、<c>XInputReportParser</c> が読む）。
    /// 15 バイト、リトルエンディアン: [0] レポート ID（0）、[1..2] ボタン（<c>XINPUT_GAMEPAD.wButtons</c>。Guide は 0x0400）、
    /// [3] 左トリガー、[4] 右トリガー、[5..6] 左スティック X、[7..8] 左スティック Y、[9..10] 右スティック X、[11..12] 右スティック Y
    /// （スティックは符号付き 16 ビット。XInput のとおり、Y は上が正）、[13] 電池の種類、[14] 電池の残量
    /// （<see cref="DS4Windows.Input.Abstractions.XInputBattery"/> の値。電池を読めなかったときは、種類が 0xFF）。
    /// </summary>
    public static class XInputReportLayout
    {
        public const int ReportLength = 15;

        public const int ButtonsOffset = 1;
        public const int LeftTriggerOffset = 3;
        public const int RightTriggerOffset = 4;
        public const int LeftStickXOffset = 5;
        public const int LeftStickYOffset = 7;
        public const int RightStickXOffset = 9;
        public const int RightStickYOffset = 11;
        public const int BatteryTypeOffset = 13;
        public const int BatteryLevelOffset = 14;

        // wButtons のビット（XInput の定義どおり）
        public const ushort DpadUp = 0x0001;
        public const ushort DpadDown = 0x0002;
        public const ushort DpadLeft = 0x0004;
        public const ushort DpadRight = 0x0008;
        public const ushort Start = 0x0010;
        public const ushort Back = 0x0020;
        public const ushort LeftThumb = 0x0040;
        public const ushort RightThumb = 0x0080;
        public const ushort LeftShoulder = 0x0100;
        public const ushort RightShoulder = 0x0200;
        /// <summary>Guide（Xbox ボタン）。文書化されていない <c>XInputGetStateEx</c> だけが返す。</summary>
        public const ushort Guide = 0x0400;
        public const ushort A = 0x1000;
        public const ushort B = 0x2000;
        public const ushort X = 0x4000;
        public const ushort Y = 0x8000;
    }
}
