using System.Collections.Generic;

namespace DS4Windows.Actions
{
    /// <summary>
    /// Phase8-Step7 7.1-W2（Q6＝A）: <see cref="IFrameOutput"/> の実装が、TRACE ログ用に、このフレームの出力の組み合わせを読み出すための口。
    /// 現行の Stage3 が出していた「出力の組み合わせ」のログ（タッチパッドクリック、マウスのカウンター、ボタン配列）を、新経路で同じ文言にするために使う。
    /// ログが有効なときだけ呼ぶ（無効のときは呼ばず、割り当てを増やさない）。
    /// </summary>
    public interface IFrameOutputTrace
    {
        /// <summary>
        /// 出力の組み合わせを、現行の Stage3 と同じ順序（TouchpadClick、マウスのカウンター、ボタン）で <paramref name="parts"/> に足す。
        /// </summary>
        void AppendOutputCombo(List<string> parts);
    }
}
