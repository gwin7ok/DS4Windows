namespace DS4Windows.Processing.Actions
{
    /// <summary>パッド（X360）への割り当ての出力先の種類。</summary>
    public enum PadTargetKind
    {
        /// <summary>軸（<c>LXNeg</c>〜<c>RYPos</c>）。値を、軸のペアの両方へ書く。</summary>
        Axis,
        /// <summary>トリガー（<c>L2</c>／<c>R2</c>）。</summary>
        Trigger,
        /// <summary>ボタン（上記以外。<c>Start</c> までの X360 のボタン）。</summary>
        Button,
    }
}
