using DS4Windows.Processing.Touchpad;
using DS4Windows.Core.Model;
using System;
using DS4Windows.Input;

namespace DS4Windows.Processing.Abstractions
{
    /// <summary>
    /// Phase9 9-E E-5（Phase9-E5-Plan.md）: タッチパッドの動作の切り替え（PS ボタンを押しながらタッチ）、起動時の OFF、スライドの判定。
    /// 以前は <c>ControlService.CheckForTouchToggle</c>／<c>StartTPOff</c>／<c>TouchpadSlide</c> と、状態 <c>touchreleased</c>／<c>touchslid</c> だった。
    /// <c>ControlService</c> の同名のメソッドは、薄い委譲として残す（画面と読み込み処理が使う）。
    /// </summary>
    public interface ITouchpadToggleService
    {
        /// <summary>PS ボタンを押しながらタッチされたら、タッチパッドの動作を ON／OFF する（レポートごとに呼ばれる）。</summary>
        void CheckForToggle(int deviceID, ControllerState cState, ControllerState pState);

        /// <summary>プロファイルの「タッチパッドを OFF で開始」。スロットの範囲内のときだけ OFF にする。</summary>
        void StartTouchpadOff(int deviceID);

        /// <summary>スライドの向き（"right"／"left"／""／"none"）を返す。<paramref name="connected"/> はそのスロットにデバイスがあるか。</summary>
        string Slide(int ind, ControllerState cState, TouchpadMouse touchPad, bool connected);
    }
}
