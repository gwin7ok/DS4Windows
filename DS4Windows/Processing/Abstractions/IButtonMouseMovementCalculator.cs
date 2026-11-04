namespace DS4Windows.DI
{
    /// <summary>
    /// Phase9 9-B（Phase9-B-Plan.md）: ボタン・スティック・トリガー・ジャイロ方向のコントロールを「マウスの移動」に割り当てたときの、生の移動量（符号の処理の前）の計算。
    /// 以前は <c>Mapping.getMouseMapping</c>（private。約 318 行）だった。感度・縦倍率・マウス加速・デッドゾーン・経過時間を使う。
    /// マウス加速の状態はデバイスごとに持つ（旧: <c>Mapping</c> が全コントローラーで 1 つを共有。ユーザー判断 Q2）。
    /// </summary>
    public interface IButtonMouseMovementCalculator
    {
        /// <param name="device">入力スロット（加速の状態の添字）。</param>
        /// <param name="control">マウス移動に割り当てたコントロール。</param>
        /// <param name="mnum">方向。0＝Up、1＝Down、2＝Left、3＝Right。</param>
        /// <param name="cState">今のフレームの入力。</param>
        /// <param name="fieldMapping">今のフレームの入力の添字つき参照（<c>DS4StateFieldMapping</c>）。</param>
        /// <param name="buttonMouseInfo">そのデバイスのボタン → マウスの設定。</param>
        /// <param name="lsDeadZone">左スティックのデッドゾーン設定値（0 のとき、小さな固定のデッドゾーンを使う）。</param>
        /// <param name="rsDeadZone">右スティックのデッドゾーン設定値（同上）。</param>
        /// <param name="deltaAccelProcessorGroup">そのデバイスのデルタ加速の処理。</param>
        /// <param name="timeElapsed">前のフレームからの経過時間（ミリ秒。<c>DS4Device.lastTimeElapsedDouble</c>）。</param>
        double Calculate(int device, DS4Controls control, int mnum, DS4State cState,
            DS4StateFieldMapping fieldMapping, ButtonMouseInfo buttonMouseInfo, int lsDeadZone, int rsDeadZone,
            DeltaSettingsProcessorGroup deltaAccelProcessorGroup, double timeElapsed);
    }
}
