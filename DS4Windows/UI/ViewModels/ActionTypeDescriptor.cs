using DS4Windows;

namespace DS4WinWPF.DS4Forms.ViewModels
{
    /// <summary>
    /// Phase10-Step2 W1（Phase10-Step2-Plan.md §3.1）: 編集画面の種別の一覧の 1 項目。従来は、<c>SpecialActEditorViewModel.typeAssoc</c>（種別の ID の配列）と、
    /// <c>AssignmentEditor.xaml</c> のドロップダウンの項目が、添字だけで対応していた。それを 1 つの記述にまとめる。
    /// </summary>
    public sealed class ActionTypeDescriptor
    {
        public ActionTypeDescriptor(int index, SpecialAction.ActionTypeId typeId, string labelKey,
            ActionEditorModes availableIn, ActionEditorModes savableIn)
        {
            Index = index;
            TypeId = typeId;
            LabelKey = labelKey;
            AvailableIn = availableIn;
            SavableIn = savableIn;
        }

        /// <summary>ドロップダウンでの位置（0 は「種別を選んでください」の項目）。XAML の <c>SelectedIndex</c> と同じ。</summary>
        public int Index { get; }

        /// <summary>種別（Key の項目は、画面上は Press Key のタブで、Button も扱う）。</summary>
        public SpecialAction.ActionTypeId TypeId { get; }

        /// <summary>表示名の言語リソースのキー（<c>Strings.resx</c>）。</summary>
        public string LabelKey { get; }

        /// <summary>この種別を、一覧に出すモード（承認済みの方針: すべての種別を、両方のモードで選べる）。</summary>
        public ActionEditorModes AvailableIn { get; }

        /// <summary>
        /// この種別を、現在、保存できるモード。SA は、すべて保存できる。Controls は、保存形式（<c>DS4ControlSettings</c>）が、
        /// Key・Button・Macro しか持たないため、それだけ（Step3 の保存形式の拡張で、増やす）。
        /// </summary>
        public ActionEditorModes SavableIn { get; }

        public bool IsAvailableIn(ActionEditorModes mode) => (AvailableIn & mode) == mode && mode != ActionEditorModes.None;

        public bool IsSavableIn(ActionEditorModes mode) => (SavableIn & mode) == mode && mode != ActionEditorModes.None;
    }
}
