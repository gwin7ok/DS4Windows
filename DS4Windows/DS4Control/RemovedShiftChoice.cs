namespace DS4Windows
{
    /// <summary>
    /// Phase8-Step7 7.8d: 廃止された「シフト」機能の設定がプロファイルに残っているとき、編集画面でユーザーが選ぶ内容。
    /// </summary>
    public enum RemovedShiftChoice
    {
        /// <summary>設定項目自体を削除する（プロファイルを保存すると、ファイルから消える）。</summary>
        Delete,

        /// <summary>残す（動作はしない。編集画面を開くたびに、警告を出す）。</summary>
        Keep,
    }
}
