using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace DS4WinWPF.DS4Forms
{
    /// <summary>
    /// R-6（Release-Backlog.md）: マウスホイールの挙動を、Windows の設定「マウスホイールで 1 回にスクロールする量」に合わせる。
    /// WPF の ScrollViewer は、この設定が「1 画面ずつ」（<c>WHEEL_PAGESCROLL</c>）でも、数行ずつしかスクロールしない。
    /// 設定が「1 画面ずつ」のときだけ、ホイール 1 ノッチごとに、1 ページ分（<see cref="ScrollViewer.PageUp"/>／<see cref="ScrollViewer.PageDown"/>）スクロールする。
    /// 「行数」の設定のときは、何もしない（WPF の標準が、行数の設定に従う）。設定は、ホイールを回すたびに読む（設定の変更が、すぐ反映される）。
    /// </summary>
    public static class PageScrollWheelHandler
    {
        // SystemParametersInfo の SPI_GETWHEELSCROLLLINES。値が WHEEL_PAGESCROLL（uint.MaxValue）なら、1 画面ずつ
        private const uint SPI_GETWHEELSCROLLLINES = 0x0068;
        private const uint WHEEL_PAGESCROLL = uint.MaxValue;

        /// <summary>ホイール 1 ノッチの値（<c>WHEEL_DELTA</c>）。</summary>
        public const int WheelDelta = 120;

        private static bool registered;
        private static int remainder;

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool SystemParametersInfo(uint uiAction, uint uiParam, ref uint pvParam, uint fWinIni);

        /// <summary>
        /// すべての <see cref="ScrollViewer"/> に、ホイールの処理を付ける（クラスのハンドラー）。最初の画面ができる前に、1 回だけ呼ぶ。
        /// 内側の ScrollViewer が先に受け取り（バブル）、処理したら外側へは伝えない（WPF の標準と同じ）。
        /// </summary>
        public static void Register()
        {
            if (registered) return;
            registered = true;
            EventManager.RegisterClassHandler(typeof(ScrollViewer), UIElement.MouseWheelEvent, new MouseWheelEventHandler(OnMouseWheel));
        }

        /// <summary>Windows の設定が「マウスホイールで 1 画面ずつスクロール」か。</summary>
        public static bool IsPageScrollEnabled()
        {
            uint lines = 3;
            return SystemParametersInfo(SPI_GETWHEELSCROLLLINES, 0, ref lines, 0) && lines == WHEEL_PAGESCROLL;
        }

        /// <summary>
        /// ホイールの値（<paramref name="delta"/>）を、ノッチの数に直す。端数は <paramref name="remainder"/> に持ち越す
        /// （細かい値を返すホイールで、複数回に分けて回しても、1 ノッチ分たまれば 1 ページ進む）。向きが変わったときは、端数を捨てる。
        /// </summary>
        /// <returns>正＝上へ、負＝下へのノッチ数</returns>
        public static int ConsumeNotches(ref int remainder, int delta)
        {
            if (delta == 0) return 0;
            if ((remainder > 0 && delta < 0) || (remainder < 0 && delta > 0)) remainder = 0;

            remainder += delta;
            int notches = remainder / WheelDelta;
            remainder -= notches * WheelDelta;
            return notches;
        }

        private static void OnMouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (e.Handled) return;

            // Ctrl（拡大・縮小）や Shift（横スクロール）を押しているときは、標準のまま
            if (Keyboard.Modifiers != ModifierKeys.None) return;

            ScrollViewer viewer = (ScrollViewer)sender;
            if (viewer.VerticalScrollBarVisibility == ScrollBarVisibility.Disabled || viewer.ScrollableHeight <= 0) return;

            if (!IsPageScrollEnabled())
            {
                remainder = 0;
                return;
            }

            int notches = ConsumeNotches(ref remainder, e.Delta);
            for (int i = 0; i < System.Math.Abs(notches); i++)
            {
                if (notches > 0) viewer.PageUp();
                else viewer.PageDown();
            }

            e.Handled = true;
        }
    }
}
