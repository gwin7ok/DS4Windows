namespace DS4Windows.DI
{
    /// <summary>
    /// Phase9 9-A(1) (b)（Phase9-A1-Plan.md §2）: スティック 1 本のデッドゾーンの補正（Radial と Axial。Radial のときは外側の出力［Outer Binding］も）。
    /// 以前は <c>Mapping.SetCurveAndDeadzone</c> の中に、左右のスティックで、ほぼ同じコードが 2 回あった。左右とも、このメソッドを呼ぶ。状態は持たない。
    /// </summary>
    public interface IStickDeadZoneProcessor
    {
        /// <summary>
        /// 補正を行う。<paramref name="rawX"/>・<paramref name="rawY"/> は補正前の入力（キャリブレーション後）、<paramref name="x"/>・<paramref name="y"/> は、入力の値で、
        /// 補正後の値を書き戻す。<paramref name="outer"/> は、外側の出力（Radial のときだけ更新する）。フィールドを直接読み書きするため、割り当て 0（<c>ref</c> 渡し）。
        /// </summary>
        /// <param name="mod">スティックのデッドゾーンの設定（<c>LSModInfo</c>／<c>RSModInfo</c>）。</param>
        void Process(StickDeadZoneInfo mod, byte rawX, byte rawY, ref byte x, ref byte y, ref byte outer);
    }
}
