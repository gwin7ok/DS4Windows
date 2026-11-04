using DS4Windows;

namespace DS4WinWPF.DS4Forms.ViewModels.SpecialActions
{
    /// <summary>
    /// Phase10-Step2 W1（Phase10-Step2-Plan.md §3.1）: 割り当て編集画面の、種別ごとの入力（Macro、Press Key など）の共通の窓口。
    /// 種別ごとの ViewModel（<c>MacroViewModel</c>、<c>PressKeyViewModel</c> ほか）が、すでに持っている 3 つのメソッドを、そのまま契約にした
    /// （挙動は変えない）。<see cref="ActionTypeEditorSet"/> が、種別から対応する入力を選んで、読み込み・検証・保存を中継する
    /// （従来は、<c>AssignmentEditor</c> のコードビハインドの 3 つの <c>switch</c> が行っていた）。
    /// </summary>
    public interface IActionTypeEditor
    {
        /// <summary>保存済みの SA から、この種別の入力へ読み込む。</summary>
        void LoadAction(SpecialAction action);

        /// <summary>この種別の入力を、SA として保存する（<c>ISpecialActionRepository.SaveAction</c>）。</summary>
        void SaveAction(SpecialAction action, bool edit = false);

        /// <summary>この種別の入力を検証する（エラーは、各 ViewModel が <c>INotifyDataErrorInfo</c> で通知する）。</summary>
        bool IsValid(SpecialAction action);

        /// <summary>
        /// Phase10-Step6 I6b: Controls のモードで、保存済みの割り当ての型のある項目（<see cref="ControlSpecialItem"/>）から、この種別の入力へ読み込む。
        /// 既定は何もしない（Controls の SA 専用の種別を持たない入力。Key、Macro ほか）。以前は <c>ControlSpecialEditors</c> を通して、
        /// <c>ControlSettingsEditTarget</c> が種別ごとに書いていた。
        /// </summary>
        void LoadControlItem(ControlSpecialItem item)
        {
        }

        /// <summary>
        /// Phase10-Step6 I6b: Controls のモードで、この種別の入力から、型のある項目を作る。既定は、種別だけを持つ項目（<paramref name="type"/>、押し続ける秒数は 0）。
        /// </summary>
        ControlSpecialItem ToControlItem(SpecialAction.ActionTypeId type) => new ControlSpecialItem { Type = type };
    }
}
