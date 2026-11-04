namespace DS4Windows.DI
{
    /// <summary>
    /// Phase9 9-B（Phase9-B-Plan.md）: マウスの移動量（小数）を、整数のピクセル数にそろえる。切り捨てた端数は次のフレームへ持ち越す。
    /// 以前は <c>Mapping.calculateFinalMouseMovement</c>（private）で、端数（<c>horizontalRemainder</c>／<c>verticalRemainder</c>）は <c>Mapping</c> が全コントローラーで 1 つを共有していた。
    /// ユーザー判断（Q2）により、端数はデバイスごとに持つ。時刻には依存しない。
    /// </summary>
    public interface IMouseMotionQuantizer
    {
        /// <summary>
        /// 生の移動量 <paramref name="rawMouseX"/>／<paramref name="rawMouseY"/> に、前回の端数を足し（同じ向きのときだけ）、整数部を <paramref name="mouseX"/>／<paramref name="mouseY"/> に返す。
        /// 端数が足された場合、<paramref name="rawMouseX"/>／<paramref name="rawMouseY"/> は書き換わる（旧実装と同じ）。
        /// </summary>
        void Quantize(int device, ref double rawMouseX, ref double rawMouseY, out int mouseX, out int mouseY);
    }
}
