using System;

namespace DS4Windows.DI
{
    /// <summary>
    /// Phase9 9-E E-5（Phase9-E5-Plan.md）: タッチパッドの動作の切り替え（PS ボタンを押しながらタッチ）、起動時の OFF、スライドの判定。
    /// 以前は <c>ControlService.CheckForTouchToggle</c>／<c>StartTPOff</c>／<c>TouchpadSlide</c> と、状態 <c>touchreleased</c>／<c>touchslid</c> だった。
    /// <c>ControlService</c> の同名のメソッドは、薄い委譲として残す（画面と読み込み処理が使う）。
    /// </summary>
    public interface ITouchpadToggleService
    {
        /// <summary>ログ（これまでの <c>ControlService.LogDebug</c>）。<c>ControlService</c> が購読して Debug イベントへ流す。</summary>
        event Action<string, bool> DebugMessage;

        /// <summary>PS ボタンを押しながらタッチされたら、タッチパッドの動作を ON／OFF する（レポートごとに呼ばれる）。</summary>
        void CheckForToggle(int deviceID, DS4State cState, DS4State pState);

        /// <summary>プロファイルの「タッチパッドを OFF で開始」。スロットの範囲内のときだけ OFF にする。</summary>
        void StartTouchpadOff(int deviceID);

        /// <summary>スライドの向き（"right"／"left"／""／"none"）を返す。<paramref name="connected"/> はそのスロットにデバイスがあるか。</summary>
        string Slide(int ind, DS4State cState, Mouse touchPad, bool connected);
    }
}
