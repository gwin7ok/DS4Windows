
namespace DS4Windows.Core.Abstractions
{
    /// <summary>Phase12-Step2 2b-2: <see cref="IWindowFocuser.Focus"/> の結果。</summary>
    public enum WindowFocusResult
    {
        /// <summary>対象のウィンドウを前面へ出した（すでに前面だった場合を含む）。</summary>
        Focused,

        /// <summary>指定に一致するウィンドウがなかった。</summary>
        NotFound,

        /// <summary>見つかったが、前面へ出せなかった（応答なし、管理者権限のウィンドウ、Windows の制限など）。</summary>
        Failed,

        /// <summary>指定が空（パスもタイトルも省略）で、対象を決められなかった。</summary>
        InvalidTarget
    }
}
