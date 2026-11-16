namespace DS4WindowsTests
{
    /// <summary>
    /// 規則 V2（画面［View。<c>DS4Windows.UI</c> 直下の型］は、業務のサービス・設定の型を直接参照せず、ViewModel を通す）の、今ある違反の許可リスト。
    /// "参照元|参照先" の形（序数順）。<see cref="ViewDirectServiceReferenceRatchetTests"/> が実数と一致することを検査する。
    /// 是正で減ったら、同じコミットでここからも消す。新しい違反は追加しない（是正する）。
    /// </summary>
    public static class ViewDirectServiceReferenceBaseline
    {
        public static readonly string[] Entries =
        {
        };
    }
}
