namespace DS4Windows
{
    /// <summary>
    /// <see cref="AppLogger.Log(AppLogLevel, string, LogOptions)"/> の付加情報（Phase13b-Step1。Log-Policy.md §3）。
    /// 「利用者向け」の印が付いたログだけが画面（ログタブ・ステータス）に出る。Temporary は一時表示で、ファイルには出さない。
    /// 旧 <c>LogToGui(data, warning, temporary)</c> の対応: warning → Level（Warn／Info）、temporary → <see cref="Temporary"/>。
    /// </summary>
    public sealed class LogOptions
    {
        /// <summary>画面に出さない（ファイルのみ）。<c>null</c> と同じ意味。</summary>
        public static readonly LogOptions FileOnly = new LogOptions(userFacing: false, temporary: false);

        /// <summary>利用者向け（画面とファイル）。</summary>
        public static readonly LogOptions UserFacing = new LogOptions(userFacing: true, temporary: false);

        /// <summary>利用者向けの一時表示（画面のみ。ファイルには出さない）。</summary>
        public static readonly LogOptions Temporary = new LogOptions(userFacing: true, temporary: true);

        private LogOptions(bool userFacing, bool temporary)
        {
            IsUserFacing = userFacing;
            IsTemporary = temporary;
        }

        /// <summary>画面に出す印。</summary>
        public bool IsUserFacing { get; }

        /// <summary>一時表示の印（<see cref="IsUserFacing"/> が真のときだけ意味を持つ）。</summary>
        public bool IsTemporary { get; }
    }
}
