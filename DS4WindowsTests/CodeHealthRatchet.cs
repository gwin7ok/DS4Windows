namespace DS4WindowsTests
{
    /// <summary>
    /// 数字で守る検査（ラチェット）の判定。実数が基準を超えても、基準より減っても失敗にする
    /// （超えたら是正、減ったら基準も同じコミットで更新させるため。既存の <c>LayerDependencyBaseline</c> などと同じ考え方）。
    /// </summary>
    internal static class CodeHealthRatchet
    {
        /// <summary>基準と実数が一致すれば null。そうでなければ、何をすべきかを書いた失敗の文言。</summary>
        public static string Evaluate(string name, int actual, int baseline)
        {
            if (actual == baseline) return null;
            if (actual > baseline)
            {
                return $"{name}: 実数 {actual} が基準 {baseline} を超えた。増えた分を是正すること（基準へは足さない）。";
            }

            return $"{name}: 実数 {actual} が基準 {baseline} より少ない。CodeHealthBaseline の値を {actual} に更新すること。";
        }
    }
}
