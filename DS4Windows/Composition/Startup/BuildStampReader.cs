using System;
using System.Text.RegularExpressions;

namespace DS4WinWPF
{
    /// <summary>
    /// Phase9 9-F F-1a: 実行フォルダの <c>build_timestamp.txt</c> の内容から、起動時のログの文言を作る。
    /// 旧 <c>App.Application_Startup</c> の中の同じ処理を、そのまま移した（状態なし。ファイルの読み込みと、ログの出力は、呼び出し側が行う）。
    /// </summary>
    public static class BuildStampReader
    {
        /// <summary>
        /// ファイルの内容（前後の空白は取り除く）から、ログの文言を返す。日時（ISO 風）が読めれば「This binary was built at 日時 (commit …)」
        /// （commit は、あれば。未コミットの変更があれば「-dirty」つき。古い形式のファイルにはない）、読めなければ「This binary was built: 内容（1 行にしたもの）」。
        /// 解釈中の例外は、旧コードと同じく握りつぶして null を返す（ログを出さない）。
        /// </summary>
        public static string BuildLogMessage(string fileContent)
        {
            try
            {
                string summary = (fileContent ?? string.Empty).Trim();
                string single = summary.Replace(Environment.NewLine, " ").Replace('\n', ' ');

                Match m = Regex.Match(single, @"(?<date>\d{4}-\d{2}-\d{2}[ T]\d{2}:\d{2}:\d{2})");
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
