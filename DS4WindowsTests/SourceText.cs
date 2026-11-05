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

        private static bool IsBuildOutput(string path)
        {
            string p = path.Replace('\\', '/');
            return p.Contains("/obj/") || p.Contains("/bin/");
        }
    }
}
