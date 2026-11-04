using System;

namespace DS4Windows.Processing.Abstractions
{
    /// <summary>
    /// Phase12-Step2 2i: マウスの位置にあるトップレベルのウィンドウ 1 つ（ターゲットマークのドラッグ＆ドロップで取得する）。
    /// <see cref="WindowCandidate"/>（照合用に小文字化）と違い、画面の入力欄へ入れるため、パス・タイトルとも元の大文字小文字のまま持つ。
    /// </summary>
    public class CapturedWindow
    {
        public CapturedWindow(IntPtr handle, int processId, string processPath, string title)
        {
            Handle = handle;
            ProcessId = processId;
            ProcessPath = processPath ?? string.Empty;
            Title = title ?? string.Empty;
        }

        public IntPtr Handle { get; }

        public int ProcessId { get; }

        /// <summary>プロセスの実行ファイルのフルパス（元の大文字小文字、バックスラッシュ区切り）。取得できなければ空。</summary>
        public string ProcessPath { get; }

        /// <summary>ウィンドウタイトル（元の大文字小文字）。</summary>
        public string Title { get; }
    }
}
