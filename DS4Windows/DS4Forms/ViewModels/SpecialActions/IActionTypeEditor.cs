using DS4Windows;

namespace DS4WinWPF.DS4Forms.ViewModels.SpecialActions
{
    /// <summary>
    /// Phase10-Step2 W1（Phase10-Step2-Plan.md §3.1）: 編集画面の、種別ごとの入力（Macro、Press Key など）の共通の窓口。
    /// 種別ごとの ViewModel（<c>MacroViewModel</c>、<c>PressKeyViewModel</c> ほか）が、すでに持っている 3 つのメソッドを、そのまま契約にした
    /// （挙動は変えない）。<see cref="ActionTypeEditorSet"/> が、種別から対応する入力を選んで、読み込み・検証・保存を中継する
    /// （従来は、<c>SpecialActionEditor</c> のコードビハインドの 3 つの <c>switch</c> が行っていた）。
    /// </summary>
    public interface IActionTypeEditor
    {
        /// <summary>保存済みの SA から、この種別の入力へ読み込む。</summary>
        void LoadAction(SpecialAction action);

        /// <summary>この種別の入力を、SA として保存する（<c>ISpecialActionRepository.SaveAction</c>）。</summary>
        void SaveAction(SpecialAction action, bool edit = false);

        /// <summary>この種別の入力を検証する（エラーは、各 ViewModel が <c>INotifyDataErrorInfo</c> で通知する）。</summary>
        bool IsValid(SpecialAction action);
    }
}
