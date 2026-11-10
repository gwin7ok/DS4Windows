namespace DS4Windows.Output.Backend
{
    /// <summary>仮想 DS4 の十字キーの向き。値はドライバー（<c>DualShock4DPadDirection</c>）と同じ（差分テストで固定）。</summary>
    public enum Ds4DPadDirection : byte
    {
        North = 0,
        Northeast = 1,
        East = 2,
        Southeast = 3,
        South = 4,
        Southwest = 5,
        West = 6,
        Northwest = 7,
        None = 8,
    }
}
