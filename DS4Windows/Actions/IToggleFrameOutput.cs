namespace DS4Windows.Actions
{
    /// <summary>
    /// Phase8-Step7 7.8f-2（Phase8-Step7-8f-Plan.md §3.2。Q8＝A）: 1 フレームの出力への、Toggle の書き込み口。
    /// 旧経路の Controls の Toggle が、デバイスの合成状態（<c>currentClicks.toggle</c>／<c>toggleCount</c>）へ書いていたものを、そのまま移した。
    /// <see cref="IFrameOutput"/> とその偽物を変えないために、別のインターフェースにした。デバイスの合成状態を持たない（<c>Bind</c> 前の）ときは、何もしない。
    /// </summary>
    public interface IToggleFrameOutput : IFrameOutput
    {
        /// <summary>Toggle の状態（<c>currentClicks.toggle</c>）を反転する。</summary>
        void FlipToggle();

        /// <summary>このフレームの Toggle の数（<c>currentClicks.toggleCount</c>）を 1 増やす。<c>Commit</c> が、0 から非 0 になる立ち上がりを見る。</summary>
        void CountToggle();
    }
}
