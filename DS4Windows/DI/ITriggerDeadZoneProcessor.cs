namespace DS4Windows.DI
{
    /// <summary>
    /// Phase9 9-A(1) (d)（Phase9-A1-Plan.md §6）: トリガー 1 本のデッドゾーンの補正（デッドゾーン、アンチデッドゾーン、最大ゾーン、最大出力）。
    /// 以前は <c>Mapping.SetCurveAndDeadzone</c> の中に、L2 と R2 で同じコードが 2 回あった。L2／R2 とも、このメソッドを呼ぶ。状態は持たない。
    /// 呼び出し側は、「補正の設定が既定でない」ときだけ呼ぶ（既定のフレームには、呼び出しの費用がかからない）。
    /// </summary>
    public interface ITriggerDeadZoneProcessor
    {
        /// <summary>
        /// 補正を行う。<paramref name="rawValue"/> は補正前の入力（デッドゾーンを超えたかの判定に使う）、<paramref name="value"/> は、入力の値で、補正後の値を書き戻す。
        /// フィールドを直接読み書きするため、割り当て 0（<c>ref</c> 渡し）。
        /// </summary>
        /// <param name="mod">トリガーのデッドゾーンの設定（<c>L2ModInfo</c>／<c>R2ModInfo</c>）。</param>
        void Process(TriggerDeadZoneZInfo mod, byte rawValue, ref byte value);
    }
}
