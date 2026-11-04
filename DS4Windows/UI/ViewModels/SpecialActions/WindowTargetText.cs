
namespace DS4Windows.UI.ViewModels.SpecialActions
{
    /// <summary>
    /// Phase12-Step2 2h（R-4）: FocusWindow の対象の指定（保存用の文字列）と、画面の入力（本文＋一致方法）の相互変換。
    /// 保存用の文字列は、<see cref="DS4Windows.Core.Platform.WindowTargetMatcher"/> の規則（先頭 <c>^</c>＝前方、末尾 <c>$</c>＝後方、先頭 <c>*</c>＝部分、印なし＝完全。2 文字以上のときだけ印とみなす）のまま。
    /// 印と同じ文字で始まる・終わる完全一致の本文は、照合の規則が表せないため、扱わない。
    /// </summary>
    public static class WindowTargetText
    {
        /// <summary>保存用の文字列を、印を除いた本文と一致方法に分ける。照合の規則と同じ優先順位（<c>^</c>、<c>$</c>、<c>*</c>）。</summary>
        public static string ParseTitle(string stored, out WindowTitleMatchMode mode)
        {
            mode = WindowTitleMatchMode.Exact;
            if (string.IsNullOrEmpty(stored)) return string.Empty;

            if (stored.Length >= 2)
            {
                if (stored[0] == '^')
                {
                    mode = WindowTitleMatchMode.StartsWith;
                    return stored.Substring(1);
                }

                if (stored[stored.Length - 1] == '$')
                {
                    mode = WindowTitleMatchMode.EndsWith;
                    return stored.Substring(0, stored.Length - 1);
                }

                if (stored[0] == '*')
                {
                    mode = WindowTitleMatchMode.Contains;
                    return stored.Substring(1);
                }
            }

            return stored;
        }

        /// <summary>本文と一致方法から、保存用の文字列を作る。本文が空なら空（タイトルを指定しない）。</summary>
        public static string ComposeTitle(string text, WindowTitleMatchMode mode)
        {
            if (string.IsNullOrEmpty(text)) return string.Empty;

            switch (mode)
            {
                case WindowTitleMatchMode.StartsWith: return "^" + text;
                case WindowTitleMatchMode.EndsWith: return text + "$";
                case WindowTitleMatchMode.Contains: return "*" + text;
                default: return text;
            }
        }

        /// <summary>
        /// パスの指定を、実行ファイル名だけで判定する形（<c>\ファイル名$</c>。後方一致）に変える。印（先頭の <c>^</c>・<c>*</c>、末尾の <c>$</c>）を外し、最後の <c>\</c> か <c>/</c> より後ろを使う。
        /// 先頭に <c>\</c> を付けるのは、<c>xnotepad.exe</c> のような別のファイルに一致しないため。空、またはファイル名が取れないとき（末尾が区切り）は、そのまま返す。
        /// </summary>
        public static string ToExecutableNameOnly(string path)
        {
            if (string.IsNullOrWhiteSpace(path)) return path;

            string body = path.Trim();
            if (body.Length >= 2 && (body[0] == '^' || body[0] == '*')) body = body.Substring(1);
            if (body.Length >= 2 && body[body.Length - 1] == '$') body = body.Substring(0, body.Length - 1);

            int separator = body.LastIndexOfAny(new[] { '\\', '/' });
            string name = separator >= 0 ? body.Substring(separator + 1) : body;
            if (name.Length == 0) return path;

            return "\\" + name + "$";
        }
    }
}
