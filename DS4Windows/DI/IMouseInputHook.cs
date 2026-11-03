using System;

namespace DS4Windows.Services
{
    /// <summary>
    /// Phase12-Step3 3e（R-3）: 画面全体のマウス入力（移動、ボタンの押下・離上）の監視。マクロの記録で、ほかのウィンドウの上の操作も記録するために使う。
    /// 実装は <c>LowLevelMouseHook</c>（Win32 の低レベルフック）。イベントは、<see cref="Start"/> を呼んだスレッド（UI スレッド）で、同期的に起こる。
    /// ハンドラーは、すぐに戻ること（遅いと、Windows のマウス操作全体が重くなる）。
    /// </summary>
    public interface IMouseInputHook
    {
        /// <summary>ポインターが動いた。引数は、画面の座標（物理ピクセル）。</summary>
        event Action<int, int> PointerMoved;

        /// <summary>
        /// ボタンが押された・離された。引数は、マクロの値（256＝左、257＝右、258＝中、259＝4 番目、260＝5 番目）、押下かどうか、画面の座標。
        /// </summary>
        event Action<int, bool, int, int> ButtonChanged;

        bool IsRunning { get; }

        /// <summary>監視を始める。始められたら true（すでに動いているときも true）。</summary>
        bool Start();

        /// <summary>監視を止める（動いていなくても安全）。</summary>
        void Stop();
    }
}
