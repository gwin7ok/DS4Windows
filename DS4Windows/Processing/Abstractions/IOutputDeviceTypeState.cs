using DS4Windows.Output;

namespace DS4Windows.Processing.Abstractions
{
    /// <summary>
    /// Phase13-Step7 7-b（Phase13-Step7-Plan.md §3.1）: 出力デバイス種別の 2 本の配列の持ち主（依存なし）。
    /// 以前は <c>OutputSlotService</c> が持っていたため、<c>VirtualPadService</c> などが配列を取るためだけに <c>Func&lt;IOutputSlotService&gt;</c> を使っていた
    /// （<c>VirtualPadService</c> ⇄ <c>OutputSlotService</c> の相互参照）。持ち主を分けて、依存を一方向にする。
    /// 配列の参照は再代入しない（利用者はコンストラクターで保持してよい）。要素の読み書きにロックはない（従来どおり）。
    /// </summary>
    public interface IOutputDeviceTypeState
    {
        /// <summary>UI 一時状態（Profile Editor で選択中の未確定の種別）。既定は全スロット X360。永続設定とは別物。</summary>
        OutContType[] OutDevTypeTemp { get; }

        /// <summary>実行時接続状態（仮想バスに接続中の種別）。未接続は None。永続設定とは別物。</summary>
        OutContType[] ActiveOutDevType { get; }
    }
}
