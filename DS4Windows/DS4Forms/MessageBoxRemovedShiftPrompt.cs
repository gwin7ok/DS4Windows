using System.Windows;
using DS4Windows;

namespace DS4WinWPF.DS4Forms
{
    /// <summary>
    /// Phase8-Step7 7.8d W3（Phase8-Step7-8d-Plan.md §4.2）: 廃止された「シフト」の設定が残っているプロファイルを、編集画面で開いたときの警告（<see cref="IRemovedShiftPrompt"/> の画面の実装）。
    /// 「はい」＝設定項目自体を削除する、「いいえ」（またはダイアログを閉じる）＝残す。選択は記憶しない（残すなら、開くたびに、この警告を出す）。
    /// 文言は <c>Properties.Resources.RemovedShiftWarning</c>（英語・日本語。ほかの言語は英語）。
    /// </summary>
    public sealed class MessageBoxRemovedShiftPrompt : IRemovedShiftPrompt
    {
        public RemovedShiftChoice Ask(string profileName)
        {
            string message = string.Format(Properties.Resources.RemovedShiftWarning, profileName);
            MessageBoxResult result = MessageBox.Show(message, "DS4Windows", MessageBoxButton.YesNo,
                MessageBoxImage.Warning, MessageBoxResult.No);
            return result == MessageBoxResult.Yes ? RemovedShiftChoice.Delete : RemovedShiftChoice.Keep;
        }
    }
}
