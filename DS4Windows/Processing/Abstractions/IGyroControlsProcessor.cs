namespace DS4Windows.DI
{
    /// <summary>
    /// Phase9 9-A(2)-A（Phase9-A2-Plan.md）: 「ジャイロ → コントロール」（<c>GyroOutMode.Controls</c>）の、ジャイロの加速度 1 軸分の補正（デッドゾーン、最大ゾーン、アンチデッドゾーン、感度、出力カーブ）。
    /// 以前は <c>Mapping.SetCurveAndDeadzone</c> の中に、X（SX）と Z（SZ）で同じコードが 2 回あった。X／Z とも、同じメソッドを呼ぶ。状態は持たない。
    /// 出力カーブを適用する条件（X と Z で違う）は、呼び出し側が持つ。
    /// </summary>
    public interface IGyroControlsProcessor
    {
        /// <summary>
        /// デッドゾーン・最大ゾーン・アンチデッドゾーン・感度の補正。<paramref name="accel"/> は補正前の加速度（<c>Motion.accelX／accelZ</c>）、
        /// <paramref name="deadZone"/> は <c>(int)(128 * デッドゾーン)</c>、<paramref name="maxZone"/>・<paramref name="antiDead"/>・<paramref name="sens"/> は設定の値。
        /// 返す値が <c>Motion.outputAccelX／outputAccelZ</c>（範囲は -128〜128）。
        /// </summary>
        int Correct(int accel, int deadZone, double maxZone, double antiDead, double sens);

        /// <summary>出力カーブ（モード 1〜6）。<paramref name="outputAccel"/> は <see cref="Correct"/> の戻り値。<paramref name="bezier"/> は、モード 6 のときだけ使う。</summary>
        int ApplyCurve(int mode, BezierCurve bezier, int outputAccel);
    }
}
