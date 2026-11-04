using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;

namespace DS4WindowsTests
{
    /// <summary>
    /// ソースコードの静的検査（回帰ガード）を行うテストが、リポジトリ内のソースファイルを探すための共通ヘルパー。
    /// ソースが見つからない環境（ソースなしで実行される場合）では null を返す。
    /// </summary>
    internal static class SourceFileLocator
    {
        /// <param name="relativePath">リポジトリのルートからの相対パス（例: DS4Windows/Processing/ControlService.cs）。</param>
        public static string Find(string relativePath, [CallerFilePath] string callerFile = "")
        {
            var starts = new List<string>();
            if (!string.IsNullOrEmpty(callerFile) && Path.IsPathRooted(callerFile))
            {
                starts.Add(Path.GetDirectoryName(callerFile));
            }
            starts.Add(AppContext.BaseDirectory);

            foreach (string start in starts)
            {
                string dir = start;
                while (!string.IsNullOrEmpty(dir))
                {
                    string candidate = Path.Combine(dir, relativePath.Replace('/', Path.DirectorySeparatorChar));
                    if (File.Exists(candidate))
                    {
                        return candidate;
                    }
                    dir = Path.GetDirectoryName(dir);
                }
            }

            return null;
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
    }
}