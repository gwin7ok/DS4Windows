using System;

namespace DS4Windows
{
    /// <summary>
    /// Phase7b W6（Q7＝A、Q8＝B）: <see cref="TriggerDeadZoneMode.Cutoff"/> の計算。状態を持たない純粋な関数（ホットパスで使うため、確保しない）。
    /// <list type="bullet">
    /// <item>入力が<b>デッドゾーン以下</b>なら、常に 0。</item>
    /// <item><b>超えたら</b>、押し込み量（最大ゾーンを考慮した割合）を、最大出力で頭打ちにし、アンチデッドゾーンの設定値との<b>大きい方</b>を出力する。デッドゾーンで引き伸ばさない。</item>
    /// </list>
    /// アンチデッドゾーンが最大出力より大きい設定は、<see cref="TriggerDeadZoneMode.Scaled"/> と同様に、アンチデッドゾーンが優先される。
    /// </summary>
    public static class TriggerCutoffCurve
    {
        /// <param name="input">トリガーの入力（0〜255）</param>
        /// <param name="deadZone">デッドゾーン（0〜255。入力がこれ以下なら 0）</param>
        /// <param name="antiDeadZone">アンチデッドゾーン（0〜100 の百分率）</param>
        /// <param name="maxZone">最大ゾーン（0〜100 の百分率）。この割合の入力で、割合が 100% になる</param>
        /// <param name="maxOutput">最大出力（0〜100 の百分率）</param>
        public static byte Apply(byte input, byte deadZone, int antiDeadZone, int maxZone, double maxOutput)
        {
            if (input <= deadZone)
                return 0;

            double maxValue = 255.0 * (maxZone / 100.0);
            double output = maxValue > 0.0 ? Math.Min(input, maxValue) / maxValue : 1.0;

            if (maxOutput != 100.0)
                output = Math.Min(Math.Max(output, 0.0), maxOutput / 100.0);

            if (antiDeadZone > 0)
                output = Math.Max(output, antiDeadZone * 0.01);

            // 割合から 0〜255 へ戻すときは、浮動小数点の誤差で 1 小さくならないよう、四捨五入する
            return output > 0.0 ? (byte)Math.Min(Math.Round(output * 255.0, MidpointRounding.AwayFromZero), 255.0) : (byte)0;
        }
    }
}
