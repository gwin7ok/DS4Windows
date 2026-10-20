using System.Windows;
using DS4Windows;

namespace DS4WinWPF.DS4Forms
{
    /// <summary>
    /// Phase8-Step8 8-0 W3（Phase8-Step8-Plan.md §5.6。Q9＝A）: 廃止された「ジャイロスワイプ」の設定が残っているプロファイルを、編集画面で開いたときの警告
    /// （<see cref="IRemovedGyroSwipePrompt"/> の画面の実装）。「はい」＝設定を削除する、「いいえ」（またはダイアログを閉じる）＝残す。
    /// 選択は記憶しない（残すなら、開くたびに、この警告を出す）。7.8d の <see cref="MessageBoxRemovedShiftPrompt"/> と同じ作り。
    /// 文言は <c>Properties.Resources.RemovedGyroSwipeWarning</c>（英語・日本語。ほかの言語は英語）。
    /// </summary>
    public sealed class MessageBoxRemovedGyroSwipePrompt : IRemovedGyroSwipePrompt
    {
        public RemovedGyroSwipeChoice Ask(string profileName)
        {
            string message = string.Format(Properties.Resources.RemovedGyroSwipeWarning, profileName);
            MessageBoxResult result = MessageBox.Show(message, "DS4Windows", MessageBoxButton.YesNo,
                MessageBoxImage.Warning, MessageBoxResult.No);
            return result == MessageBoxResult.Yes ? RemovedGyroSwipeChoice.Delete : RemovedGyroSwipeChoice.Keep;
        }
    }
}
