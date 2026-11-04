using System;

namespace DS4Windows.DI
{
    /// <summary>
    /// Phase9 9-E E-5（Phase9-E5-Plan.md）: いま押されている（倒されている）入力を 1 つ探す。プロファイル編集画面の「どのボタンを押した?」が使う。
    /// 以前は <c>ControlService.GetActiveInputControl</c>。状態は持たない。<c>ControlService</c> の同名のメソッドは、薄い委譲として残す。
    /// </summary>
    public interface IActiveInputProbe
    {
        /// <summary>
        /// 押されている入力を返す（なければ <see cref="DS4Controls.None"/>）。
        /// <paramref name="connected"/> はそのスロットにデバイスがあるか（なければ常に None）。<paramref name="tp"/> は connected のときだけ読む。
        /// </summary>
        DS4Controls GetActive(DS4State cState, Mouse tp, bool connected);
    }
}
