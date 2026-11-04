using System;
using System.Globalization;
using System.IO;

namespace DS4WinWPF
{
    /// <summary>
    /// Phase9 9-F F-1a: 起動時の診断ログの文言。旧 <c>App.Application_Startup</c> の中の同じ文言を、そのまま移した（ログの出力は、呼び出し側が行う。文言とレベルは変えない）。
    /// </summary>
    public static class StartupDiagnostics
    {
        public static string CultureMessage()
            => $"Startup culture: CurrentCulture={CultureInfo.CurrentCulture}, CurrentUICulture={CultureInfo.CurrentUICulture}";

        public static string DefaultCultureMessage()
            => $"DefaultThreadCurrentCulture={CultureInfo.DefaultThreadCurrentCulture}, DefaultThreadCurrentUICulture={CultureInfo.DefaultThreadCurrentUICulture}";

        public static string ArgsMessage(string[] args)
            => $"Startup args: {string.Join(' ', args ?? Array.Empty<string>())}";

        public static string LangFolderMessage(string langDir)
            => $"Lang folder exists: {Directory.Exists(langDir)} at {langDir}";

        /// <summary>言語フォルダの中のサブフォルダの名前の一覧のログ。</summary>
        public static string LangSubdirsMessage(string langDir)
        {
            string[] dirs = Directory.GetDirectories(langDir);
            return $"Lang subdirs: {string.Join(',', Array.ConvertAll(dirs, d => Path.GetFileName(d)))}";
        }

        /// <summary>ログの設定の後に出す、環境の情報（アーキテクチャ、OS）。<paramref name="osProductName"/>／<paramref name="osReleaseId"/> は、呼び出し側が取得して渡す。</summary>
        public static string[] EnvironmentMessages(string version, string executablePath, string osProductName, string osReleaseId)
            => new[]
            {
                $"DS4Windows version {version}",
                $"DS4Windows exe file: {Path.GetFileName(executablePath)}",
                $"DS4Windows Assembly Architecture: {(Environment.Is64BitProcess ? "x64" : "x86")}",
                $"OS Version: {Environment.OSVersion}",
                $"OS Product Name: {osProductName}",
                $"OS Release ID: {osReleaseId}",
                $"System Architecture: {(Environment.Is64BitOperatingSystem ? "x64" : "x86")}",
            };
    }
}
