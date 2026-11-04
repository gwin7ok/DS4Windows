using System;

namespace DS4Windows.Core.Abstractions
{
    /// <summary>
    /// Phase12-Step2 2b-2: 列挙されたトップレベルのウィンドウ 1 つ。照合に使う値は、<see cref="IProcessInspector.GetForegroundProcessInfo"/> と
    /// 同じ約束にそろえる（パスは小文字・バックスラッシュ正規化、タイトルは小文字）。そのため <see cref="WindowTargetMatcher.IsMatch"/> へそのまま渡せる。
    /// </summary>
    public class WindowCandidate
    {
        public WindowCandidate(IntPtr handle, string processPath, string title)
        {
            Handle = handle;
            ProcessPath = processPath ?? string.Empty;
            Title = title ?? string.Empty;
        }

        public IntPtr Handle { get; }

        /// <summary>プロセスの実行ファイルのパス（小文字・バックスラッシュ正規化）。取得できなければ空。</summary>
        public string ProcessPath { get; }

        /// <summary>ウィンドウタイトル（小文字）。</summary>
        public string Title { get; }
    }
}
