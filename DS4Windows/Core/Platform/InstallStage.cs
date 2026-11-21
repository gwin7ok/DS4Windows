namespace DS4Windows.Core.Platform
{
    /// <summary>外部コンポーネントの導入の段階。</summary>
    public enum InstallStage
    {
        /// <summary>ダウンロード中（<see cref="InstallProgress.Percent"/> に進捗）。</summary>
        Downloading,

        /// <summary>導入プログラムを起動し、終了を待っている。</summary>
        Installing,

        /// <summary>導入できた。</summary>
        Completed,

        /// <summary>ダウンロードか起動に失敗した、または導入プログラムの終了後も導入が確認できない。</summary>
        Failed,
    }
}
