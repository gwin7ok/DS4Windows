#nullable enable

using DS4Windows.Core.Model;

namespace DS4Windows.Processing.Abstractions
{
    /// <summary>パッドの情報（接続状態、MAC アドレス、バッテリーなど）を取り出す関数。UDP サーバーが、問い合わせのたびに呼ぶ。</summary>
    public delegate void UdpPadDetailProvider(int padIdx, ref DualShockPadMeta meta);
}
