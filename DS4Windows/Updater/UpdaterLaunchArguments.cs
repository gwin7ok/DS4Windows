namespace DS4WinWPF
{
    /// <summary>
    /// DS4Updater.exe を起動するときのコマンドライン引数を組み立てる（起動の 3 か所で共通）。
    /// DS4Updater は、DS4Windows の取得先を <c>--ds4windows-repo</c> で受け取る。引数がなければ、DS4Updater の既定値
    /// （利用者の手元に残っている古い DS4Updater では、移行前のリポジトリ）を使うため、<see cref="ReleaseSource"/> のリポジトリを必ず渡す。
    /// </summary>
    public static class UpdaterLaunchArguments
    {
        /// <param name="ds4WindowsDir">DS4Windows のフォルダー。</param>
        /// <param name="ds4UpdaterDir">DS4Updater のフォルダー。</param>
        /// <param name="ds4WindowsExe">更新の後に起動する DS4Windows.exe。</param>
        /// <param name="launchMode">"admin" または "user"。null のときは、<c>--launch-mode</c> を付けない。</param>
        public static string Build(string ds4WindowsDir, string ds4UpdaterDir, string ds4WindowsExe, string launchMode = null)
        {
            string args = $"--ds4windows-path \"{ds4WindowsDir}\" --ds4updater-path \"{ds4UpdaterDir}\" " +
                          $"--ds4windows-repo \"{ReleaseSource.RepositoryPageUri}\" " +
                          $"-autolaunch --launchExe \"{ds4WindowsExe}\"";
            if (!string.IsNullOrEmpty(launchMode))
            {
                args += $" --launch-mode={launchMode}";
            }

            return args;
        }
    }
}
