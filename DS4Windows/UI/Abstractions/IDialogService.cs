namespace DS4Windows.UI.Abstractions
{
    /// <summary>
    /// 確認・通知・ファイルの選択の窓の窓口。ViewModel が <c>MessageBox</c>・<c>OpenFileDialog</c> を直接作らず、
    /// WPF なしでテストできるようにするための契約（テストでは、答えを決めておく偽物を渡す）。
    /// 文言は呼び出し側が表示言語に合わせて決めて渡す（この窓口は文言を持たない）。
    /// </summary>
    public interface IDialogService
    {
        /// <summary>OK だけの通知の窓を出す。</summary>
        void ShowMessage(string message, string? title = null, DialogIcon icon = DialogIcon.None);

        /// <summary>はい／いいえの確認の窓を出す。はい なら true。</summary>
        bool Confirm(string message, string? title = null, DialogIcon icon = DialogIcon.Question);

        /// <summary>開くファイルを選ばせる。取り消したら null。</summary>
        string? PickOpenFile(FileDialogOptions options);

        /// <summary>フォルダーを選ばせる。取り消したら null。</summary>
        string? PickFolder();

        /// <summary>保存先のファイルを選ばせる。取り消したら null。</summary>
        string? PickSaveFile(FileDialogOptions options);
    }
}
