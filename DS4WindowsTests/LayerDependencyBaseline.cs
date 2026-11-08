namespace DS4WindowsTests
{
    /// <summary>
    /// Phase14b-Step1（R-01）: 層の依存の規則（L1・L3〜L6）に今ある違反の許可リスト。"規則|参照元の型|参照先の型" の形（序数順）。
    /// <see cref="LayerDependencyRuleTests"/> が実数と一致することを検査する（多くても少なくても失敗。規約 §3.1 のラチェットの一致検査と同じ運用）。
    /// 是正で減ったら、同じ Step でここからも消す。新しい違反は追加しない（是正する）。
    /// 更新の手順: 環境変数 DS4W_LAYER_BASELINE_OUT にファイルのパスを入れてテストを実行すると、実測から作った本ファイルの内容をそのパスへ書き出す。
    /// </summary>
    public static class LayerDependencyBaseline
    {
        /// <summary>記録のみの規則 L2（Core → 第1層・第3層）の件数の基準値。<see cref="LayerDependencyRuleTests"/> が実数との一致を検査する。</summary>
        public const int RecordOnlyL2Count = 29;

        /// <summary>記録のみの規則 L7（UI → 第1層・第3層）の件数の基準値。</summary>
        public const int RecordOnlyL7Count = 13;

        public static readonly string[] Entries =
        {
        };
    }
}
