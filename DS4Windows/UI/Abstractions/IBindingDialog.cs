#nullable enable
namespace DS4Windows.UI.Abstractions
{
    /// <summary>
    /// 出力ピッカー（<c>BindingWindow</c>）の、呼び出し側が使う部分。割り当て編集の手順（<c>AssignmentEditorSession</c>）が
    /// 具象の WPF の窓に依存せず、テストでは偽物へ差し替えられるようにする（結果は、渡した <c>DS4ControlSettings</c> へ書き戻される）。
    /// </summary>
    public interface IBindingDialog
    {
        /// <summary>利用者が「Default」を選んだか（× で閉じたときは false）。</summary>
        bool DefaultPicked { get; }

        /// <summary>メイン画面を親にして、閉じるまで表示する。</summary>
        void ShowModal();
    }
}
