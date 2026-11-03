namespace DS4Windows
{
    /// <summary>
    /// Phase12-Step2 2b-1: ウィンドウの「対象の指定」（実行ファイルのパスとウィンドウタイトル）と、その照合の規則。
    /// Auto-Profile の <c>AutoProfileEntity</c> が持っていた規則を、そのまま取り出した部品（動作は変えない）。
    /// プロファイルの自動切替（前面ウィンドウとの照合）と、ウィンドウをフォーカスする割り当て（全ウィンドウからの探索）が、同じ規則で対象を特定するために共有する。
    /// <para>
    /// 規則: <c>Path</c>・<c>Title</c> はどちらも省略可。両方あれば AND。完全一致のほか、先頭 <c>^</c>＝前方一致、末尾 <c>$</c>＝後方一致、
    /// 先頭 <c>*</c>＝部分一致。パスは小文字化し <c>/</c> を <c>\</c> にそろえる。照合の入力（<c>searchPath</c>・<c>searchTitle</c>）は、
    /// 呼び出し側が小文字にそろえて渡す（<c>IProcessInspector.GetForegroundProcessInfo</c> と同じ約束）。
    /// </para>
    /// 検索語は、毎秒の照合で文字列を作り直さないよう、設定時に小文字化・記号の除去を済ませておく（旧実装から継承）。
    /// </summary>
    public class WindowTargetMatcher
    {
        private string path = string.Empty;
        private string title = string.Empty;
        private string path_lowercase;
        private string title_lowercase;

        public WindowTargetMatcher(string pathStr, string titleStr)
        {
            SetSearchPath(pathStr);
            SetSearchTitle(titleStr);
        }

        /// <summary>設定された指定（記号を含む、入力のまま）。</summary>
        public string Path { get => path; set => SetSearchPath(value); }

        /// <summary>設定された指定（記号を含む、入力のまま）。</summary>
        public string Title { get => title; set => SetSearchTitle(value); }

        public bool IsMatch(string searchPath, string searchTitle)
        {
            bool bPathMatched = true;
            bool bTitleMwatched = true;

            if (!string.IsNullOrEmpty(path_lowercase))
            {
                bPathMatched = (path_lowercase == searchPath
                    || (path[0] == '^' && searchPath.StartsWith(path_lowercase))
                    || (path[path.Length - 1] == '$' && searchPath.EndsWith(path_lowercase))
                    || (path[0] == '*' && searchPath.Contains(path_lowercase))
                   );
            }

            if (bPathMatched && !string.IsNullOrEmpty(title_lowercase))
            {
                bTitleMwatched = (title_lowercase == searchTitle
                    || (title[0] == '^' && searchTitle.StartsWith(title_lowercase))
                    || (title[title.Length - 1] == '$' && searchTitle.EndsWith(title_lowercase))
                    || (title[0] == '*' && searchTitle.Contains(title_lowercase))
                   );
            }

            // If both path and title defined in autoprofile entry then do AND condition (ie. both path and title should match)
            return bPathMatched && bTitleMwatched;
        }

        private void SetSearchPath(string pathStr)
        {
            if (!string.IsNullOrEmpty(pathStr))
            {
                path = pathStr;
                path_lowercase = path.ToLower().Replace('/', '\\');

                if (path.Length >= 2)
                {
                    if (path[0] == '^') path_lowercase = path_lowercase.Substring(1);
                    else if (path[path.Length - 1] == '$') path_lowercase = path_lowercase.Substring(0, path_lowercase.Length - 1);
                    else if (path[0] == '*') path_lowercase = path_lowercase.Substring(1);
                }
            }
            else path = path_lowercase = string.Empty;
        }

        private void SetSearchTitle(string titleStr)
        {
            if (!string.IsNullOrEmpty(titleStr))
            {
                title = titleStr;
                title_lowercase = title.ToLower();

                if (title.Length >= 2)
                {
                    if (title[0] == '^') title_lowercase = title_lowercase.Substring(1);
                    else if (title[title.Length - 1] == '$') title_lowercase = title_lowercase.Substring(0, title_lowercase.Length - 1);
                    else if (title[0] == '*') title_lowercase = title_lowercase.Substring(1);
                }
            }
            else title = title_lowercase = string.Empty;
        }
    }
}
