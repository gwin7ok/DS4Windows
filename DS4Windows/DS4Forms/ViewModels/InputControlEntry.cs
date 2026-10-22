using DS4Windows;

namespace DS4WinWPF.DS4Forms.ViewModels
{
    /// <summary>
    /// Phase9 UI 変更: 入力ボタンのカタログ（<see cref="InputControlCatalog"/>）の 1 項目。
    /// <see cref="Name"/> は、Actions.xml の <c>Trigger</c> に書く名前（変えない）。<see cref="Label"/> は、Controls の一覧と SA のトリガーの一覧が共通で使う表示名。
    /// </summary>
    public sealed class InputControlEntry
    {
        public InputControlEntry(DS4Controls control, string name, string label)
        {
            Control = control;
            Name = name;
            Label = label;
        }

        public DS4Controls Control { get; }

        public string Name { get; }

        public string Label { get; }
    }
}
