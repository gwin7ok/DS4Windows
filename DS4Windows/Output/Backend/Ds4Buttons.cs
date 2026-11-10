namespace DS4Windows.Output.Backend
{
    /// <summary>仮想 DS4 のボタンのビット。値はドライバー（<c>DualShock4Button</c>）と同じ（差分テストで固定）。</summary>
    public static class Ds4Buttons
    {
        public const ushort ThumbRight = 1 << 15;
        public const ushort ThumbLeft = 1 << 14;
        public const ushort Options = 1 << 13;
        public const ushort Share = 1 << 12;
        public const ushort TriggerRight = 1 << 11;
        public const ushort TriggerLeft = 1 << 10;
        public const ushort ShoulderRight = 1 << 9;
        public const ushort ShoulderLeft = 1 << 8;
        public const ushort Triangle = 1 << 7;
        public const ushort Circle = 1 << 6;
        public const ushort Cross = 1 << 5;
        public const ushort Square = 1 << 4;
    }
}
