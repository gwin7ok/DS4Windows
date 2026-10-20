using System;

namespace DS4WinWPF.DS4Forms.ViewModels
{
    /// <summary>
    /// Phase10-Step2 W1（Phase10-Step2-Plan.md §3.1）: 共通の編集画面のモード。SA のトリガー（複数のボタンの組み合わせ）を編集するか、
    /// Controls のボタン 1 つの割り当てを編集するか。種別の一覧（<see cref="ActionTypeDescriptor"/>）の、使えるモードの指定にも使う。
    /// </summary>
    [Flags]
    public enum ActionEditorModes
    {
        None = 0,
        SpecialAction = 1,
        Control = 2,
        Both = SpecialAction | Control,
    }
}
