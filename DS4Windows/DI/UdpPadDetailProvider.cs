namespace DS4Windows.DI
{
    /// <summary>Phase9 9-E E-3: パッドの情報（接続状態、MAC アドレス、バッテリーなど）を取り出す関数。UDP サーバーが、問い合わせのたびに呼ぶ（旧 <c>UdpServer.GetPadDetail</c>）。</summary>
    public delegate void UdpPadDetailProvider(int padIdx, ref DualShockPadMeta meta);
}
