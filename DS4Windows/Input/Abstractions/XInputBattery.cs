namespace DS4Windows.Input.Abstractions
{
    /// <summary>
    /// <c>XInputGetBatteryInformation</c> の結果。種類は XInput の定義どおり（0: 接続なし、1: 有線、2: アルカリ、3: ニッケル水素、0xFF: 不明）、
    /// 残量は 4 段階（0: 空、1: 低、2: 中、3: 満）。
    /// </summary>
    public readonly struct XInputBattery
    {
        public const byte TypeWired = 1;
        public const byte TypeAlkaline = 2;
        public const byte TypeNiMh = 3;
        public const byte TypeUnknown = 0xFF;

        /// <summary>残量の最大の段階（満）。</summary>
        public const int MaxLevel = 3;

        public XInputBattery(byte type, byte level)
        {
            Type = type;
            Level = level;
        }

        public byte Type { get; }

        public byte Level { get; }
    }
}
