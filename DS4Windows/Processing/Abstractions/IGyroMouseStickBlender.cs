namespace DS4Windows.DI
{
    /// <summary>
    /// Phase9 9-B（Phase9-B-Plan.md）: ジャイロのマウススティック出力（<c>GyroOutMode.MouseJoystick</c>）の合成。
    /// 出力スティックの X／Y それぞれで、ジャイロ由来の値と通常のスティックの値のうち、中心（128）から遠い方を採る。
    /// 以前は <c>Mapping.TempMouseJoystick</c> の内側だった。状態は持たない。
    /// </summary>
    public interface IGyroMouseStickBlender
    {
        /// <summary>
        /// <paramref name="MappedState"/> の出力スティックを更新し、<paramref name="mapStickData"/> に採った値を書いて dirty にする。
        /// 出力スティックが <c>None</c> でないこと、ジャイロの出力モードがマウススティックであることは、呼び出し側が確認する。
        /// </summary>
        /// <param name="gyroX">ジャイロ由来の X（中心 128。128 のときは何もしない）。</param>
        /// <param name="gyroY">ジャイロ由来の Y（同上）。</param>
        void Blend(GyroMouseStickInfo msinfo, byte gyroX, byte gyroY, DS4State MappedState, PostMapStickData mapStickData);
    }
}
