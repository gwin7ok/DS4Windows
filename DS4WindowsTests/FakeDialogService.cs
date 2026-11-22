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
        public string FolderAnswer { get; set; }

        public FileDialogOptions LastOpenOptions { get; private set; }
        public FileDialogOptions LastSaveOptions { get; private set; }

        public void ShowMessage(string message, string title = null, DialogIcon icon = DialogIcon.None) => Messages.Add(message);

        public bool Confirm(string message, string title = null, DialogIcon icon = DialogIcon.Question)
        {
            Confirmations.Add(message);
            return ConfirmAnswer;
        }

        /// <summary>3 択の窓への答え（既定は「最初のボタン」）。</summary>
        public DialogChoice ThreeWayAnswer { get; set; } = DialogChoice.First;
        public readonly List<string> ThreeWayMessages = new List<string>();

        /// <summary>文字列の入力への答え。順に返し、尽きたら null（取り消し）。</summary>
        public readonly Queue<string> PromptAnswers = new Queue<string>();
        public readonly List<string> PromptMessages = new List<string>();

        public DialogChoice ChooseOneOfThree(string message, string title, string firstText, string secondText, string thirdText, DialogIcon icon = DialogIcon.Question)
        {
            ThreeWayMessages.Add(message);
            return ThreeWayAnswer;
        }

        public string PromptText(string message, string title, string initialText = "")
        {
            PromptMessages.Add(message);
            return PromptAnswers.Count > 0 ? PromptAnswers.Dequeue() : null;
        }

        public string PickOpenFile(FileDialogOptions options)
        {
            LastOpenOptions = options;
            return OpenFileAnswer;
        }

        public string PickFolder() => FolderAnswer;

        public string PickSaveFile(FileDialogOptions options)
        {
            LastSaveOptions = options;
            return SaveFileAnswer;
        }
    }
}
