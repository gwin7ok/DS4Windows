namespace DS4Windows.UI.ViewModels
{
    /// <summary>
    /// トリガーを選ぶメニューの 1 項目（見出しとチェックの状態）。画面の部品に依存しない値の入れ物。
    /// View がメニューの項目をこの形で ViewModel へ渡し、ViewModel が計算したチェックを書き戻す。
    /// </summary>
    public sealed class TriggerMenuEntry
    {
        public TriggerMenuEntry(string header, bool isChecked)
        {
            Header = header;
            IsChecked = isChecked;
        }

        public string Header { get; }

        public bool IsChecked { get; set; }
    }
}
