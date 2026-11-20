using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace DS4WindowsTests
{
    /// <summary>
    /// <see cref="CodeHealthRatchetTests"/> が使う、ソースの文字列の数え方。
    /// 入力（相対パスと全文）を引数で受けるので、偽の入力で数え方そのものを検査できる（<c>CodeHealthScannerTests</c>）。
    /// </summary>
    internal static class CodeHealthScanner
    {
        private static readonly Regex UpperLayerUsing = new Regex(
            @"^using DS4Windows\.(Processing|UI|Output|Composition|Input)(\.\w+)*;", RegexOptions.Multiline | RegexOptions.Compiled);

        private static readonly Regex WindowsUsing = new Regex(
            @"^\s*using\s+(static\s+)?(\w+\s*=\s*)?System\.Windows(\.\w+)*\s*;", RegexOptions.Multiline | RegexOptions.Compiled);

        private static readonly Regex NonFormTimerAlias = new Regex(
            @"^[ \t]*using\s+NonFormTimer\s*=\s*System\.Timers\.Timer\s*;[ \t]*\r?$", RegexOptions.Multiline | RegexOptions.Compiled);

        /// <summary>数える対象の 1 ファイル。<see cref="RelativePath"/> は、本体の <c>DS4Windows/</c> からの相対パス（区切りは / でも \ でもよい）。</summary>
        public sealed class SourceFile
        {
            public SourceFile(string relativePath, string text)
            {
                RelativePath = relativePath;
                Text = text;
            }

            public string RelativePath { get; }

            public string Text { get; }
        }

        /// <summary>相対パスの最初のフォルダ名（<c>Core/Model/X.cs</c> なら <c>Core</c>）。ルート直下のファイルは空文字。</summary>
        public static string LayerOf(string relativePath)
        {
            string normalized = relativePath.Replace('\\', '/');
            int slash = normalized.IndexOf('/');
            return slash < 0 ? string.Empty : normalized.Substring(0, slash);
        }

        /// <summary><paramref name="layer"/> の中で、上位層の名前空間を <c>using</c> するファイルの数。</summary>
        public static int CountUpperLayerUsingFiles(IEnumerable<SourceFile> files, string layer)
        {
            return InLayer(files, layer).Count(f => UpperLayerUsing.IsMatch(f.Text));
        }

        /// <summary><paramref name="layer"/> の中で、<c>System.Windows*</c> を <c>using</c> するファイルの数。</summary>
        public static int CountWindowsUsingFiles(IEnumerable<SourceFile> files, string layer)
        {
            return InLayer(files, layer).Count(f => WindowsUsing.IsMatch(f.Text));
        }

        /// <summary>別名 <c>NonFormTimer</c> を宣言して、コメントを除いたファイルの中でほかに使っていない行の数。</summary>
        public static int CountUnusedNonFormTimerAliasLines(IEnumerable<SourceFile> files)
        {
            int unused = 0;
            foreach (SourceFile file in files)
            {
                int aliasLines = NonFormTimerAlias.Matches(file.Text).Count;
                if (aliasLines == 0) continue;
                string code = NonFormTimerAlias.Replace(SourceText.StripComments(file.Text), string.Empty);
                if (!Regex.IsMatch(code, @"\bNonFormTimer\b")) unused += aliasLines;
            }
            return unused;
        }

        private static readonly Regex NullableWarningPragma = new Regex(
            @"^[ \t]*#pragma[ \t]+warning[ \t]+disable\b[^\r\n]*\b(CS86\d\d|CS87\d\d|nullable)\b", RegexOptions.Multiline | RegexOptions.Compiled);

        // 後置の `!`（null 許容の抑止）。`!=` と、前置の論理否定 `!x` は含まない。
        private static readonly Regex NullForgiving = new Regex(@"[\w\)\]]!(?!=)", RegexOptions.Compiled);

        private static readonly Regex StringLiteral = new Regex("@?\"(?:[^\"\\\\]|\\\\.|\"\")*\"|'(?:[^'\\\\]|\\\\.)'", RegexOptions.Compiled);

        /// <summary>
        /// 全ファイルの中で、同じ行か直前の行に <c>//</c> の理由コメントがない、後置の <c>!</c>（null 許容の抑止）の数。
        /// </summary>
        public static int CountNullForgivingWithoutReason(IEnumerable<SourceFile> files)
        {
            int count = 0;
            foreach (SourceFile file in files)
            {
                string[] lines = file.Text.Split('\n');
                for (int i = 0; i < lines.Length; i++)
                {
                    string line = lines[i].TrimEnd('\r');
                    int comment = LineCommentStart(line);
                    string code = comment < 0 ? line : line.Substring(0, comment);
                    if (code.TrimStart().StartsWith("#", StringComparison.Ordinal)) continue;
                    code = StringLiteral.Replace(code, "\"\"");
                    if (!NullForgiving.IsMatch(code)) continue;
                    if (comment >= 0) continue;
                    if (i > 0 && lines[i - 1].TrimStart().StartsWith("//", StringComparison.Ordinal)) continue;
                    count++;
                }
            }
            return count;
        }

        /// <summary><c>#pragma warning disable</c> で、null 許容の警告（CS86xx・CS87xx・nullable）を消している行の数。</summary>
        public static int CountNullableWarningPragmas(IEnumerable<SourceFile> files)
        {
            return files.Sum(f => NullableWarningPragma.Matches(f.Text).Count);
        }

        private static int LineCommentStart(string line)
        {
            bool inString = false;
            for (int i = 0; i < line.Length - 1; i++)
            {
                char c = line[i];
                if (c == '"' && (i == 0 || line[i - 1] != '\\')) inString = !inString;
                else if (!inString && c == '/' && line[i + 1] == '/') return i;
            }
            return -1;
        }

        // 空の catch（本体が空白だけ）・コメントだけの catch・ログを包む try・イベントハンドラー以外の async void。
        private static readonly Regex EmptyCatchBlank = new Regex(
            @"\bcatch\b(?:\s*\([^)]*\))?(?:\s*when\s*\([^)]*\))?\s*\{\s*\}", RegexOptions.Compiled);

        private static readonly Regex EmptyCatchAny = new Regex(
            @"\bcatch\b(?:\s*\([^)]*\))?(?:\s*when\s*\([^)]*\))?\s*\{(?<body>(?:\s|//[^\n]*|/\*.*?\*/)*)\}", RegexOptions.Compiled | RegexOptions.Singleline);

        private static readonly Regex AsyncVoid = new Regex(@"\basync\s+void\s+\w+\s*\(([^)]*)\)", RegexOptions.Compiled);

        private static readonly Regex EventHandlerParameters = new Regex(@"^\s*object\??\s+\w+\s*,", RegexOptions.Compiled);

        /// <summary>何も書かない空の <c>catch</c>（本体が空白だけ）。<paramref name="excludeFolderPrefix"/> に属するファイルは数えない。</summary>
        public static int CountBlankEmptyCatches(IEnumerable<SourceFile> files, string excludeFolderPrefix = null)
        {
            return files
                .Where(f => excludeFolderPrefix == null
                    || !f.RelativePath.Replace('\\', '/').StartsWith(excludeFolderPrefix, StringComparison.Ordinal))
                .Sum(f => EmptyCatchBlank.Matches(f.Text).Count);
        }

        /// <summary>本体がコメントだけの <c>catch</c>（理由の質は人が見る。件数だけ凍結する）。</summary>
        public static int CountCommentOnlyEmptyCatches(IEnumerable<SourceFile> files)
        {
            return files.Sum(f => EmptyCatchAny.Matches(f.Text).Cast<Match>().Count(m => m.Groups["body"].Value.Trim().Length > 0));
        }

        /// <summary><c>try { …AppLogger.… } catch …</c> と同じ行で、ログ呼び出しを包んでいる行。</summary>
        public static int CountLogWrappingTryLines(IEnumerable<SourceFile> files)
        {
            int count = 0;
            foreach (SourceFile file in files)
            {
                foreach (string line in file.Text.Split('\n'))
                {
                    if (line.Contains("AppLogger.") && line.Contains("catch") && Regex.IsMatch(line, @"\btry\s*\{")) count++;
                }
            }

            return count;
        }

        /// <summary>イベントハンドラー（第 1 引数が <c>object</c>）の形ではない <c>async void</c>。コメントの中は数えない。</summary>
        public static int CountNonHandlerAsyncVoid(IEnumerable<SourceFile> files)
        {
            int count = 0;
            foreach (SourceFile file in files)
            {
                foreach (Match m in AsyncVoid.Matches(SourceText.StripComments(file.Text)))
                {
                    if (!EventHandlerParameters.IsMatch(m.Groups[1].Value)) count++;
                }
            }

            return count;
        }

        /// <summary>
        /// 経緯の参照（<c>Phase</c> に数字が続く語。例: <c>Phase9</c>）を含む行。経緯は git の履歴と <c>docs-forDIMG/</c> にあり、コメントへ書き足さない（規約 3.3 原則5）。
        /// 実在する型名（例: テストのクラス名）を指す行も数える。
        /// </summary>
        public static int CountPhaseReferenceLines(IEnumerable<SourceFile> files)
        {
            return files.Sum(f => f.Text.Split('\n').Count(line => Regex.IsMatch(line, @"Phase\d")));
        }

        private static bool IsUnder(string relativePath, string prefix)
        {
            string path = relativePath.Replace('\\', '/');
            return prefix.EndsWith("/", StringComparison.Ordinal)
                ? path.StartsWith(prefix, StringComparison.Ordinal)
                : string.Equals(path, prefix, StringComparison.Ordinal);
        }

        private static IEnumerable<SourceFile> InLayer(IEnumerable<SourceFile> files, string layer)
        {
            return files.Where(f => string.Equals(LayerOf(f.RelativePath), layer, StringComparison.Ordinal));
        }
    }
}
