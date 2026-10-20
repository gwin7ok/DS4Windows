using System;

namespace DS4Windows
{
    /// <summary>
    /// Phase8-Step8 8-0（Q9＝A）: 編集画面を開いたときの、廃止された「ジャイロスワイプ」の設定の警告の判定（画面から独立。状態を持たない）。
    /// 保持した内容があれば、警告を出し（<see cref="IRemovedGyroSwipePrompt.Ask"/>）、選択に従う。<b>選択は記憶しない</b>: 「残す」なら内容はそのままで、
    /// 次に編集画面を開いたときも、また警告を出す（7.8d の <see cref="RemovedShiftNotice"/> と同じ）。
    /// </summary>
    public static class RemovedGyroSwipeNotice
    {
        public static RemovedGyroSwipeOutcome Apply(RemovedGyroSwipeSettings settings, string profileName, IRemovedGyroSwipePrompt prompt)
        {
            if (settings == null || !settings.HasContent) return RemovedGyroSwipeOutcome.NoNotice;
            if (prompt == null) throw new ArgumentNullException(nameof(prompt));

            RemovedGyroSwipeChoice choice = prompt.Ask(profileName);
            if (choice == RemovedGyroSwipeChoice.Delete)
            {
                settings.Clear();
                return RemovedGyroSwipeOutcome.Deleted;
            }

            return RemovedGyroSwipeOutcome.Kept;
        }
    }
}
