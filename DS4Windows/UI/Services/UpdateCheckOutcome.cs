namespace DS4Windows.UI.Services
{
    /// <summary>更新の確認の結果。画面が何を出すかを決める。</summary>
    public enum UpdateCheckOutcome
    {
        /// <summary>新しい版がある。更新の画面を出す。</summary>
        UpdateAvailable,

        /// <summary>最新である（または確認に失敗した）。画面は、利用者が確認を求めたときだけ「最新です」と知らせる。</summary>
        UpToDate,

        /// <summary>版の記録ファイルの内容が版として読めない。何も知らせない。</summary>
        Silent,
    }
}
