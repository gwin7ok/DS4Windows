namespace DS4Windows
{
    /// <summary>
    /// Phase7b W6（Q7＝A）: L2／R2 のデッドゾーンの計算方式。
    /// </summary>
    public enum TriggerDeadZoneMode : byte
    {
        /// <summary>現行（上流の元から）。デッドゾーン以下は 0、超えた点を 0% として、残りを 0〜100% に引き伸ばす。既定。</summary>
        Scaled = 0,

        /// <summary>
        /// デッドゾーン以下は常に 0。超えたら、押し込み量そのまま（最大ゾーン・最大出力は考慮）と、アンチデッドゾーンの設定値の、大きい方を出力する
        /// （<see cref="TriggerCutoffCurve"/>）。
        /// </summary>
        Cutoff = 1,
    }
}
