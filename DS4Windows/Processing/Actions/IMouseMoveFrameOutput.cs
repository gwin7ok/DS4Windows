namespace DS4Windows.Actions
{
    /// <summary>
    /// Phase8-Step7 7.8f-3（Phase8-Step7-8f-Plan.md §3.2。Q6＝A）: 1 フレームの出力への、マウスの移動量の書き込み口。
    /// 旧経路の <c>MapCustom</c> の局所変数 <c>tempMouseDeltaX／Y</c>（0 のときだけ書く＝先に書いた方が勝つ）を、デバイスごとの加算器に置き換えた。
    /// <c>Mapping</c> がフレームの最後に、局所変数が 0 なら加算器の値を使って最終の移動量を計算する。
    /// </summary>
    public interface IMouseMoveFrameOutput : IFrameOutput
    {
        /// <summary>横の移動量を、まだ書かれていないとき（0 のとき）だけ書く。</summary>
        void SetMouseDeltaX(double value);

        /// <summary>縦の移動量を、まだ書かれていないとき（0 のとき）だけ書く。</summary>
        void SetMouseDeltaY(double value);
    }
}
