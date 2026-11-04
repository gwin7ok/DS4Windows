using System;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;

namespace DS4Windows.Composition.Startup
{
    /// <summary>
    /// Phase9 9-F F-1a: 実行フォルダの <c>build_timestamp.txt</c> の内容から、起動時のログの文言を作る。
    /// 旧 <c>App.Application_Startup</c> の中の同じ処理を、そのまま移した（状態なし。ファイルの読み込みと、ログの出力は、呼び出し側が行う）。
    /// </summary>
    public static class BuildStampReader
    {
        /// <summary>
        /// アセンブリ（DS4Windows.exe）に埋め込まれたビルド情報（csproj の EmbedBuildStamp が書く AssemblyMetadata の
        /// BuildTimestamp／BuildCommit）から、<see cref="BuildLogMessage"/> に渡す形の文字列（「日時 commit=ハッシュ」）を返す。
        /// 埋め込みがなければ null。
        /// </summary>
        public static string FromAssembly(Assembly assembly)
        {
            if (assembly == null) return null;
            var metadata = assembly.GetCustomAttributes<AssemblyMetadataAttribute>().ToList();
            string timestamp = metadata.FirstOrDefault(m => m.Key == "BuildTimestamp")?.Value;
            if (string.IsNullOrWhiteSpace(timestamp)) return null;
            string commit = metadata.FirstOrDefault(m => m.Key == "BuildCommit")?.Value;
            // 埋め込まれた値は UTC（例: 2026-11-04T12:54:01Z）。実行している環境のタイムゾーンへ直し、オフセットつきで表示する（例: 2026-11-04 21:54:01 +09:00）
            timestamp = ToLocalTimeText(timestamp, TimeZoneInfo.Local);
            return string.IsNullOrWhiteSpace(commit) ? timestamp : $"{timestamp} commit={commit}";
        }

        /// <summary>UTC の日時文字列を、指定のタイムゾーンの「yyyy-MM-dd HH:mm:ss +09:00」形式にする。読めなければそのまま返す。</summary>
        public static string ToLocalTimeText(string utcText, TimeZoneInfo zone)
        {
            if (!DateTimeOffset.TryParse(utcText, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out DateTimeOffset utc)) return utcText;
            return TimeZoneInfo.ConvertTime(utc, zone).ToString("yyyy-MM-dd HH:mm:ss zzz", CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// 文字列の内容（前後の空白は取り除く）から、ログの文言を返す。日時（ISO 風）が読めれば「This binary was built at 日時 (commit …)」
        /// （commit は、あれば。未コミットの変更があれば「-dirty」つき。古い形式のファイルにはない）、読めなければ「This binary was built: 内容（1 行にしたもの）」。
        /// 解釈中の例外は、旧コードと同じく握りつぶして null を返す（ログを出さない）。
        /// </summary>
        public static string BuildLogMessage(string fileContent)
        {
            try
            {
                string summary = (fileContent ?? string.Empty).Trim();
                string single = summary.Replace(Environment.NewLine, " ").Replace('\n', ' ');

                Match m = Regex.Match(single, @"(?<date>\d{4}-\d{2}-\d{2}[ T]\d{2}:\d{2}:\d{2}(?: ?[+-]\d{2}:\d{2})?)");
                if (m.Success)
                {
                    Match commit = Regex.Match(single, @"commit=(?<hash>\S+)");
                    string commitText = commit.Success ? $" (commit {commit.Groups["hash"].Value})" : string.Empty;
                    return $"This binary was built at {m.Groups["date"].Value}{commitText}";
                }

                return $"This binary was built: {single}";
            }
            catch
            {
                return null;
            }
        }
    }
}
