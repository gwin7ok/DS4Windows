using DS4Windows.Processing;
using DS4Windows.Processing.Abstractions;

namespace DS4Windows.Core.Abstractions
{
    /// <summary>
    /// Phase12-Step2 2b-2（R-4）: 指定に一致するウィンドウを探して、前面へ出す（フォーカスする）サービス。
    /// 指定は、Auto-Profile と同じ規則の <see cref="WindowTargetMatcher"/>（パスとタイトル。<c>^</c>・<c>$</c>・<c>*</c> の記号）。
    /// 一致するウィンドウが複数あるときは、Z 順で最初の 1 つを対象にする。最小化されていれば復元する。
    /// <para>
    /// 同期的に動き、前面化が確認できるまで数十〜数百ミリ秒かかることがある。入力処理のスレッドから呼ぶときは、呼び出し側が別スレッドへ逃がす
    /// （Phase12-Step2 2d の <c>FocusWindowAction</c> が行う）。
    /// </para>
    /// </summary>
    public interface IWindowFocuser
    {
        /// <param name="centerCursor">
        /// Phase12-Step2 2j: 前面化に成功したとき（すでに前面だった場合も含む）、そのウィンドウの中央へマウスポインターを移動する。
        /// 見つからない・失敗のときは動かさない。
        /// </param>
        WindowFocusResult Focus(WindowTargetMatcher target, bool centerCursor = false);
    }
}
