using System;
using System.IO;
using System.Runtime.CompilerServices;
using Xunit.Sdk;

namespace DS4WindowsTests
{
    /// <summary>
    /// ソースコードの静的検査（回帰ガード）を行うテストが、リポジトリ内のソースファイルを探すための共通ヘルパー。
    /// ソースが見つからない環境（リポジトリのルート［DS4Windows/DS4WinWPF.csproj］が見つからない。配布物だけで実行する場合）では null を返し、検査を省略する。
    /// Phase14d-Step1: ルートが見つかるのにファイルがないときは、null を返さずに失敗する。ファイルを移す・消すと、検査が黙って消えていた
    /// （存在しないファイルを読み、何も検査せずに合格するテストが 9 件あった）。ファイルが「ないこと」を確かめるときは <see cref="Exists"/> を使う。
    /// </summary>
    internal static class SourceFileLocator
    {
        /// <param name="relativePath">リポジトリのルートからの相対パス（例: DS4Windows/Processing/ControlService.cs）。</param>
        public static string Find(string relativePath, [CallerFilePath] string callerFile = "")
        {
            string root = RepoRoot(callerFile);
            if (root == null)
            {
                return null;
            }

            string candidate = Combine(root, relativePath);
            if (File.Exists(candidate))
            {
                return candidate;
            }

            throw new XunitException(
                $"ソースファイルが見つからない: {relativePath}（リポジトリ: {root}）。ファイルを移した・消したなら、テストのパスを直すか、検査そのものを見直すこと。");
        }

        /// <summary>
        /// Phase14d-Step1: ファイルがあるか（ないことを確かめる検査に使う）。ソースが見つからない環境では false。
        /// </summary>
        public static bool Exists(string relativePath, [CallerFilePath] string callerFile = "")
        {
            string root = RepoRoot(callerFile);
            return root != null && File.Exists(Combine(root, relativePath));
        }

        /// <summary>Phase14d-Step1: リポジトリのルート（DS4Windows/DS4WinWPF.csproj を持つフォルダー）。見つからなければ null。</summary>
        public static string RepoRoot([CallerFilePath] string callerFile = "")
        {
            if (!string.IsNullOrEmpty(callerFile) && Path.IsPathRooted(callerFile))
            {
                string fromCaller = RootFromAnchor(callerFile);
                if (fromCaller != null)
                {
                    return fromCaller;
                }
            }

            return RootFromAnchor(Path.Combine(AppContext.BaseDirectory, "anchor"));
        }

        /// <summary>Phase14: アンカー（DS4Windows 配下のソースファイル）から、リポジトリのルートを深さに依存せず求める。</summary>
        public static string ProjectDirFromAnchor(string anchor)
        {
            string root = RootFromAnchor(anchor);
            return root == null ? null : Path.Combine(root, "DS4Windows");
        }

        public static string RootFromAnchor(string anchor)
        {
            string dir = Path.GetDirectoryName(anchor);
            while (!string.IsNullOrEmpty(dir))
            {
                if (File.Exists(Path.Combine(dir, "DS4Windows", "DS4WinWPF.csproj")))
                {
                    return dir;
                }
                dir = Path.GetDirectoryName(dir);
            }
            return null;
        }

        private static string Combine(string root, string relativePath)
        {
            return Path.Combine(root, relativePath.Replace('/', Path.DirectorySeparatorChar));
        }
    }
}
