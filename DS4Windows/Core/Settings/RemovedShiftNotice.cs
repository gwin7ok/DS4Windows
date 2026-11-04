using System;

namespace DS4Windows
{
    /// <summary>
    /// Phase8-Step7 7.8d（Phase8-Step7-8d-Plan.md §4.2）: 編集画面を開いたときの、廃止された「シフト」の設定の警告の判定（画面から独立。状態を持たない）。
    /// 保持した内容があれば、警告を出し（<see cref="IRemovedShiftPrompt.Ask"/>）、選択に従う。<b>選択は記憶しない</b>: 「残す」なら内容はそのままで、
    /// 次に編集画面を開いたときも、また警告を出す。
    /// </summary>
    public static class RemovedShiftNotice
    {
        public static RemovedShiftOutcome Apply(RemovedShiftSettings settings, string profileName, IRemovedShiftPrompt prompt)
        {
            if (settings == null || !settings.HasContent) return RemovedShiftOutcome.NoNotice;
            if (prompt == null) throw new ArgumentNullException(nameof(prompt));

            RemovedShiftChoice choice = prompt.Ask(profileName);
            if (choice == RemovedShiftChoice.Delete)
            {
                settings.Clear();
                return RemovedShiftOutcome.Deleted;
            }

            return RemovedShiftOutcome.Kept;
        }
    }
}
