namespace DS4Windows.UI.ViewModels.SpecialActions
{
    /// <summary>
    /// Phase12-Step2 2h（R-4）: FocusWindow の編集画面で選ぶ、ウインドウ名の一致方法。順番は、一覧（ComboBox）の項目の並びと同じ（添字で対応づける）。
    /// 保存の形式は変えず、<see cref="WindowTargetText"/> が、従来の印（<c>^</c> 前方、<c>$</c> 後方、<c>*</c> 部分、印なし＝完全）と相互に変換する。
    /// </summary>
    public enum WindowTitleMatchMode
    {
        Exact = 0,
        StartsWith = 1,
        EndsWith = 2,
        Contains = 3,
    }
}
