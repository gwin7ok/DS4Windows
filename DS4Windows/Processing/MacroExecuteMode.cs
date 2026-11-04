namespace DS4Windows
{
    /// <summary>
    /// R-2（R2-MacroToggleRepeat-Plan.md §3.1）: マクロの Execute Mode（画面のラベル「Execute Mode」）。保存形式（SA は Actions.xml の
    /// <c>MacroExecuteMode</c> 要素、Controls はプロファイルの <c>Control/MacroExecuteMode</c> ノード）の値であり、実行時は <see cref="DS4KeyType"/> の
    /// <see cref="DS4KeyType.RepeatMacro"/>（<see cref="Held"/>）、<see cref="DS4KeyType.RepeatLatchMacro"/>（<see cref="Toggle"/>）で表す（変換は <see cref="MacroExecuteModes"/>）。
    /// </summary>
    public enum MacroExecuteMode
    {
        /// <summary>1 回だけ再生する（Play Once）。保存では、要素を書かない（既定）。</summary>
        Once,

        /// <summary>押している間、繰り返す（Repeat While Held）。</summary>
        Held,

        /// <summary>押すたびに、繰り返しの開始・停止を切り替える（Repeat (Toggle)）。</summary>
        Toggle,
    }
}
