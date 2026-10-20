namespace DS4WinWPF.DS4Forms.ViewModels
{
    /// <summary>
    /// Phase10-Step2 W2（Phase10-Step2-Plan.md §3.2）: トリガーの一覧の 1 項目。<see cref="Name"/> は、Actions.xml の <c>Trigger</c> に書く名前
    /// （従来の <c>SpecialActionEditor.xaml</c> のチェックボックスの <c>Tag</c>。例: <c>SideL</c>、<c>Function Left</c>）。
    /// <see cref="Label"/> は、画面に出す表示名（従来のチェックボックスの <c>Content</c>。例: <c>Side L</c>、<c>Function L</c>）。
    /// </summary>
    public sealed class TriggerItem
    {
        public TriggerItem(string name, string label)
        {
            Name = name;
            Label = label;
        }

        /// <summary>保存する名前（<c>SpecialAction.controls</c> の要素）。</summary>
        public string Name { get; }

        /// <summary>画面に出す表示名。</summary>
        public string Label { get; }
    }
}
