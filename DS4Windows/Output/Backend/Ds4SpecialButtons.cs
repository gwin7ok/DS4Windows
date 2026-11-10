namespace DS4Windows.Output.Backend
{
    /// <summary>仮想 DS4 の特殊ボタン（PS、タッチパッド）のビット。値はドライバー（<c>DualShock4SpecialButton</c>）と同じ（差分テストで固定）。</summary>
    public static class Ds4SpecialButtons
    {
        public const ushort Ps = 1 << 0;
        public const ushort Touchpad = 1 << 1;
    }
}
