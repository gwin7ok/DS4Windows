namespace DS4Windows.Services
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
        WindowFocusResult Focus(WindowTargetMatcher target);
    }
}
