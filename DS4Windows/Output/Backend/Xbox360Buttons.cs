namespace DS4Windows.Output.Backend
{
    /// <summary>仮想 Xbox 360 のボタンのビット。値はドライバー（<c>Xbox360Button</c>）と同じ（差分テストで固定）。</summary>
    public static class Xbox360Buttons
    {
        public const ushort Up = 0x0001;
        public const ushort Down = 0x0002;
        public const ushort Left = 0x0004;
        public const ushort Right = 0x0008;
        public const ushort Start = 0x0010;
        public const ushort Back = 0x0020;
        public const ushort LeftThumb = 0x0040;
        public const ushort RightThumb = 0x0080;
        public const ushort LeftShoulder = 0x0100;
        public const ushort RightShoulder = 0x0200;
        public const ushort Guide = 0x0400;
        public const ushort A = 0x1000;
        public const ushort B = 0x2000;
        public const ushort X = 0x4000;
        public const ushort Y = 0x8000;
    }
}
