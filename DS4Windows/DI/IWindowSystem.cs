using System;
using System.Collections.Generic;

namespace DS4Windows.Services
{
    /// <summary>
    /// Phase12-Step2 2b-2: ウィンドウの列挙と前面化に使う Win32 の操作の抽象。<see cref="IWindowFocuser"/> の手順（復元→素の前面化→
    /// 入力の結び付け→Alt の擬似入力）を、実際の Windows なしで単体テストするために分けた。実装は <c>Win32WindowSystem</c>。
    /// </summary>
    public interface IWindowSystem
    {
        /// <summary>可視で、タイトルを持ち、cloaked でない、ツールウィンドウでないトップレベルのウィンドウ（Z 順）。</summary>
        IReadOnlyList<WindowCandidate> EnumerateWindows();

        bool IsMinimized(IntPtr window);

        /// <summary>最小化を元に戻す（<c>SW_RESTORE</c>）。</summary>
        void Restore(IntPtr window);

        /// <summary>別スレッドのウィンドウが応答なしか。</summary>
        bool IsHung(IntPtr window);

        /// <summary>前面のウィンドウが、<paramref name="window"/> 自身か、それが所有するウィンドウか。</summary>
        bool IsForeground(IntPtr window);

        /// <summary>前面化を 1 回試し、短く待ってから、<see cref="IsForeground"/> の結果を返す。</summary>
        bool TrySetForeground(IntPtr window);

        /// <summary>
        /// 自スレッドを前面のスレッドへ、前面のスレッドを対象のスレッドへ、入力の処理を結び付ける。戻り値を Dispose すると、結びを外す。
        /// </summary>
        IDisposable AttachInput(IntPtr window);

        /// <summary>Alt キーの解放だけを送る（前面化の許可を得るための擬似入力。1 回目の再試行用）。</summary>
        void SendAltUp();

        /// <summary>Alt キーの押下と解放を送る（最後の手段）。</summary>
        void SendAltTap();

        /// <summary>Z 順の最上位へ出す。</summary>
        void BringToTop(IntPtr window);

        /// <summary>
        /// Phase12-Step2 2i: 今のマウスカーソルの位置にあるウィンドウの、最上位の親（トップレベル）。ウィンドウがない・取得できないときは null。
        /// 位置は画面の座標（物理ピクセル）で、カーソルの取得と同じ座標系のため、画面の拡大率の変換は要らない。
        /// </summary>
        CapturedWindow GetWindowUnderCursor();
    }
}
