namespace DS4Windows.Actions
{
    /// <summary>
    /// Phase8-Step7 7.8f-1（Phase8-Step7-8f-Plan.md §3.2。Q4＝A）: Controls タブの Button（パッドの軸・トリガー）が、元のコントロールの「値」（押した／押さないではなく、倒し具合・押し込み）を読むための入力。
    /// 現行の <c>Mapping.GetXYAxisMapping</c>／<c>GetByteMapping</c> と同じ値を返す（<c>TriggerInputTests</c> と同じ方式で、全コントロールを旧と比べる）。
    /// 既存の <see cref="IControlTriggerInput"/> とその偽物を変えないために、別のインターフェースにした。
    /// </summary>
    public interface IAnalogTriggerInput : IControlTriggerInput
    {
        /// <summary>
        /// 軸の値（128 が中立）。<paramref name="positive"/> は、出力先が「正の向き」の軸か（旧 <c>GetXYAxisMapping</c> の <c>alt</c>）。
        /// 元がボタンなら、押していれば 255（正）または 0（負）、押していなければ 128。
        /// </summary>
        byte GetAxisValue(DS4Controls control, bool positive);

        /// <summary>バイト値（0〜255）。元がボタンなら 255 か 0、軸なら向きの倒し具合を 0〜255 に伸ばした値、トリガーならその値（旧 <c>GetByteMapping</c>）。</summary>
        byte GetByteValue(DS4Controls control);

        /// <summary>
        /// Phase8-Step7 7.8f-3: マウスの移動の生の量（旧 <c>Mapping.getMouseMapping</c> の戻り値。符号の処理の前）。<paramref name="direction"/> は 0＝Up、1＝Down、2＝Left、3＝Right。
        /// 感度・加速・デッドゾーン・経過時間を使うため、計算は <c>Mapping</c> が持ち、入力の側から借りる（<see cref="TriggerInput.MouseSampler"/>）。借り先がなければ 0。
        /// </summary>
        double GetMouseMovement(DS4Controls control, int direction);
    }
}
