using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;

namespace DS4WindowsTests
{
    /// <summary>
    /// Phase14d-Step1: ソースを文字列として検査するテストの、共通の読み取り部品（旧: 各ファイルが自前で持っていた Read／StripComments の複製）。
    /// 探し方と「見つからないとき」の扱いは <see cref="SourceFileLocator.Find"/> と同じ（ソースなしの環境では null、リポジトリがあるのにファイルがなければ失敗）。
    /// </summary>
    internal static class SourceText
    {
        private static readonly Regex BlockComment = new Regex(@"/\*.*?\*/", RegexOptions.Singleline | RegexOptions.Compiled);
        private static readonly Regex LineComment = new Regex(@"//[^\r\n]*", RegexOptions.Compiled);
        private static readonly Regex Whitespace = new Regex(@"\s+", RegexOptions.Compiled);
        private static readonly object ProductionLock = new object();
        private static IReadOnlyList<string> _productionFiles;

        /// <summary>ファイルの全文。ソースなしの環境では null。</summary>
        public static string Read(string relativePath, [CallerFilePath] string callerFile = "")
        {
            string path = SourceFileLocator.Find(relativePath, callerFile);
            return path == null ? null : File.ReadAllText(path);
        }

        /// <summary>コメント（<c>/* */</c> と <c>//</c>）を除いた全文。ソースなしの環境では null。</summary>
        public static string ReadCode(string relativePath, [CallerFilePath] string callerFile = "")
        {
            string text = Read(relativePath, callerFile);
            return text == null ? null : StripComments(text);
        }

        /// <summary>コメントを除き、空白（改行を含む）の連続を 1 つの空白にした全文（書式に依存しない照合用）。ソースなしの環境では null。</summary>
        public static string ReadNormalized(string relativePath, [CallerFilePath] string callerFile = "")
        {
            string code = ReadCode(relativePath, callerFile);
            return code == null ? null : Whitespace.Replace(code, " ");
        }

        /// <summary>コメント（<c>/* */</c> と <c>//</c>）を除く。文字列の中の <c>//</c> も除く（旧来の各テストの処理と同じ）。</summary>
        public static string StripComments(string text)
        {
            text = BlockComment.Replace(text, string.Empty);
            return LineComment.Replace(text, string.Empty);
        }

        /// <summary>
        /// 本体（DS4Windows/）の .cs の絶対パスの一覧（bin・obj を除く。1 回だけ走査して覚える）。ソースなしの環境では空。
        /// </summary>
        public static IReadOnlyList<string> ProductionFiles([CallerFilePath] string callerFile = "")
        {
            lock (ProductionLock)
            {
                if (_productionFiles != null) return _productionFiles;
                string root = SourceFileLocator.RepoRoot(callerFile);
                _productionFiles = root == null
                    ? new string[0]
                    : Directory.GetFiles(Path.Combine(root, "DS4Windows"), "*.cs", SearchOption.AllDirectories)
                        .Where(f => !IsBuildOutput(f))
                        .OrderBy(f => f, System.StringComparer.Ordinal)
                        .ToArray();
                return _productionFiles;
            }
        }

        /// <summary>
        /// 本体のファイル名（例: <c>JoyConLinker.cs</c>）から、リポジトリのルートからの相対パスを引く（フォルダーの再編でパスが変わっても検査が黙って消えないように）。
        /// ソースなしの環境では null。見つからない・同じ名前が複数あるときは失敗する。
        /// </summary>
        public static string RelativePathOf(string fileName, [CallerFilePath] string callerFile = "")
        {
            string root = SourceFileLocator.RepoRoot(callerFile);
            if (root == null) return null;
            var hits = ProductionFiles(callerFile).Where(f => string.Equals(Path.GetFileName(f), fileName, System.StringComparison.Ordinal)).ToArray();
            if (hits.Length != 1)
            {
                throw new Xunit.Sdk.XunitException($"本体のファイル {fileName} が {hits.Length} 件見つかった（1 件のはず）");
            }
            return Path.GetRelativePath(root, hits[0]).Replace('\\', '/');
        }

        /// <summary>XAML のコメント（<c>&lt;!-- --&gt;</c>）を除いた全文。ソースなしの環境では null。</summary>
        public static string ReadXamlCode(string relativePath, [CallerFilePath] string callerFile = "")
        {
            string text = Read(relativePath, callerFile);
            return text == null ? null : Regex.Replace(text, @"<!--.*?-->", string.Empty, RegexOptions.Singleline);
        }

        /// <summary>
        /// 宣言の先頭（<paramref name="marker"/>）から、対応する閉じ波括弧までのメソッド全体（シグネチャを含む）。
        /// 見つからない・閉じていないときは失敗する。<paramref name="unique"/> が真なら、marker が 2 か所以上にあっても失敗する（オーバーロードの追加に気づくため）。
        /// </summary>
        public static string Method(string text, string marker, bool unique = false)
        {
            int start = text.IndexOf(marker, System.StringComparison.Ordinal);
            Xunit.Assert.True(start >= 0, $"{marker} が見つからない（メソッド名・修飾子の変更？ガードの見直しが必要）");
            if (unique)
            {
                Xunit.Assert.True(text.IndexOf(marker, start + marker.Length, System.StringComparison.Ordinal) < 0,
                    $"{marker} が複数見つかった（オーバーロードの追加？ガードの見直しが必要）");
            }
            return text.Substring(start, MatchingBrace(text, start, marker) + 1 - start);
        }

        /// <summary><see cref="Method"/> の本体だけ（最初の <c>{</c> から対応する <c>}</c> まで。波括弧を含む）。</summary>
        public static string Body(string text, string marker)
        {
            int start = text.IndexOf(marker, System.StringComparison.Ordinal);
            Xunit.Assert.True(start >= 0, $"{marker} が見つからない（メソッド名・修飾子の変更？ガードの見直しが必要）");
            return BodyFrom(text, start, marker);
        }

        /// <summary>正規表現の最初の一致から始まる本体（最初の <c>{</c> から対応する <c>}</c> まで）。</summary>
        public static string BodyByPattern(string text, string pattern)
        {
            Match m = Regex.Match(text, pattern);
            Xunit.Assert.True(m.Success, "見つからない: " + pattern);
            return BodyFrom(text, m.Index, pattern);
        }

        /// <summary>「void／bool 名前(」で始まるメソッドの本体。</summary>
        public static string BodyOfMethodNamed(string text, string methodName)
        {
            return BodyByPattern(text, @"\b(?:void|bool)\s+" + Regex.Escape(methodName) + @"\s*\(");
        }

        private static string BodyFrom(string text, int from, string label)
        {
            int open = text.IndexOf('{', from);
            Xunit.Assert.True(open >= 0, label + " の本体が見つからない");
            return text.Substring(open, MatchingBrace(text, from, label) + 1 - open);
        }

        private static int MatchingBrace(string text, int from, string label)
        {
            int open = text.IndexOf('{', from);
            Xunit.Assert.True(open >= 0, label + " の本体が見つからない");
            int depth = 0;
            for (int i = open; i < text.Length; i++)
            {
                if (text[i] == '{') depth++;
                else if (text[i] == '}' && --depth == 0) return i;
            }
            throw new Xunit.Sdk.XunitException(label + " の閉じ波括弧が見つからない");
        }

        private static bool IsBuildOutput(string path)
        {
            string p = path.Replace('\\', '/');
            return p.Contains("/obj/") || p.Contains("/bin/");
        }
    }
}
