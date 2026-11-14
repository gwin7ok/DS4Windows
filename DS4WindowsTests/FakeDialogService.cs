using System.Collections.Generic;
using DS4Windows.UI.Abstractions;

namespace DS4WindowsTests
{
    /// <summary>
    /// <see cref="IDialogService"/> の偽物。窓を出さず、呼び出しを記録し、答えは設定した値を返す。
    /// ViewModel のテストで、確認・通知・ファイルの選択を、WPF なしで確かめるために使う。
    /// </summary>
    public sealed class FakeDialogService : IDialogService
    {
        public readonly List<string> Messages = new List<string>();
        public readonly List<string> Confirmations = new List<string>();

        /// <summary>確認の窓への答え（既定は「はい」）。</summary>
        public bool ConfirmAnswer { get; set; } = true;

        /// <summary>ファイルの選択の答え（取り消しを表すときは null）。</summary>
        public string OpenFileAnswer { get; set; }
        public string SaveFileAnswer { get; set; }

        public FileDialogOptions LastOpenOptions { get; private set; }
        public FileDialogOptions LastSaveOptions { get; private set; }

        public void ShowMessage(string message, string title = null, DialogIcon icon = DialogIcon.None) => Messages.Add(message);

        public bool Confirm(string message, string title = null, DialogIcon icon = DialogIcon.Question)
        {
            Confirmations.Add(message);
            return ConfirmAnswer;
        }

        public string PickOpenFile(FileDialogOptions options)
        {
            LastOpenOptions = options;
            return OpenFileAnswer;
        }

        public string PickSaveFile(FileDialogOptions options)
        {
            LastSaveOptions = options;
            return SaveFileAnswer;
        }
    }
}
