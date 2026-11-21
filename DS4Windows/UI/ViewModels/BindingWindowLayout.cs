#nullable enable
namespace DS4Windows.UI.ViewModels
{
    /// <summary>
    /// 出力ピッカーの表示モードごとの、パネルの出し分け（見える／隠れる）。モードごとの違いはここだけに置き、画面はバインディングで読む。
    /// </summary>
    /// <param name="ShowModePanel">Default／Unbound を選ぶパネル。</param>
    /// <param name="ShowTopOptions">Toggle・Scan Code のチェック（どのモードでも、割り当てエディターの側で設定するため出さない）。</param>
    /// <param name="ShowRecordMacro">Record A Macro のボタン（どのモードでも出さない）。</param>
    /// <param name="ShowMacroOnLabel">マクロの表示のラベル。</param>
    public sealed record BindingWindowLayout(bool ShowModePanel, bool ShowTopOptions, bool ShowRecordMacro, bool ShowMacroOnLabel)
    {
        /// <summary>
        /// Controls の割り当てエディターの Press Key 用。Default／Unbound と Extras を出す。
        /// マクロの表示は、実測どおり出さない（割り当てエディターで設定する）。
        /// </summary>
        public static readonly BindingWindowLayout ForControl = new BindingWindowLayout(true, false, false, false);

        /// <summary>
        /// スペシャルアクションの Press Key 用。Default／Unbound は出さない（SA の Press Key に「Default」はない）。
        /// マクロの表示のラベルは、従来の実行時の値のとおり出る（結びつけ先がなく、既定の Visible のまま）。
        /// </summary>
        public static readonly BindingWindowLayout ForSpecialAction = new BindingWindowLayout(false, false, false, true);

        public static BindingWindowLayout For(bool isControlMode) => isControlMode ? ForControl : ForSpecialAction;
    }
}
