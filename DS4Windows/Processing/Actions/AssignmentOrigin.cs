namespace DS4Windows.Actions
{
    /// <summary>
    /// Phase10-Step6 I1（Phase10-Step6-Plan.md §4）: 割り当て定義（<see cref="AssignmentDefinition"/>）の由来。
    /// 「Controls タブか SpecialActions タブか」を、この 1 つの項目に集約する（暫定の <c>forControls</c> 引数の置き換え先。I4 で方針の表を、種別 × 由来で引く）。
    /// </summary>
    public enum AssignmentOrigin
    {
        /// <summary>Controls タブ（ボタン 1 つの割り当て。トリガーは常に 1 つ）。</summary>
        Controls,

        /// <summary>SpecialActions タブ（トリガーは 1 つ以上のボタンの組み合わせ）。</summary>
        SpecialAction,
    }
}
