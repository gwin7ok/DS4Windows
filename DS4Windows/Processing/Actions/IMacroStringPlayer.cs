namespace DS4Windows.Actions
{
    /// <summary>
    /// Phase8-Step7 7.7b（Phase8-Step7-7-Plan.md Q4＝A。W2b で <c>IMultiActionMacroPlayer</c> から改名）: マクロを**文字列で直接指定して**実行する窓口。
    /// 出力層の汎用の出力（<see cref="MacroStringAction"/>）が使う。<c>SpecialAction</c> を介さない点が <c>IMacroPlayer</c> と違う（MultiAction の旧経路の呼び出し方）。
    /// 実装は <c>Mapping</c>（旧 <c>PlayMacro(…, 文字列, …)</c> を呼ぶだけ）。<c>IMapping</c> のメンバーではない（<c>IMapping</c> は 10 メンバーのまま）。
    /// </summary>
    public interface IMacroStringPlayer
    {
        /// <summary>マクロの文字列（「/」区切りの整数の並び）を、背景のタスクで再生する。空なら何もしない。</summary>
        void PlayMacroString(int deviceIndex, string macro);
    }
}
