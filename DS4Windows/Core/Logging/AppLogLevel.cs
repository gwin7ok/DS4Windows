namespace DS4Windows
{
    /// <summary>
    /// ログのレベル（Phase13b-Step1。Log-Policy.md §2）。値の大小がそのまま重大度の順。
    /// </summary>
    public enum AppLogLevel
    {
        Trace = 0,
        Debug = 1,
        Info = 2,
        Warn = 3,
        Error = 4,
    }
}
