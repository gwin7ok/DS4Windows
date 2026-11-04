using System;
using DS4Windows.DI;

namespace DS4Windows.Services
{
    /// <summary>
    /// Phase9 9-A(1) (d)（Phase9-A1-Plan.md §6）: <see cref="ITriggerDeadZoneProcessor"/> の実装（DI の Singleton。状態は持たない）。
    /// 旧 <c>Mapping.SetCurveAndDeadzone</c> の L2 のデッドゾーンの処理を、そのまま移したもの
    /// （R2 のコードは、名前と数値の書き方［<c>255.0 * ratio</c> と <c>255 * ratio</c>］以外は同じだったため、統合した。計算結果は変わらない）。
    /// 値の特性化は <c>Phase9A1SetCurveAndDeadzoneGoldenTests</c> と <c>Phase7bTriggerDeadZoneRoundingTests</c> が守る。
    /// </summary>
    public sealed class TriggerDeadZoneProcessor : ITriggerDeadZoneProcessor
    {
        // 性能: 設定が既定でないときだけ、毎フレーム呼ばれる。ローカルは、読む前に必ず代入されるため、0 初期化を省く（SkipLocalsInit）
        [System.Runtime.CompilerServices.SkipLocalsInit]
        public void Process(TriggerDeadZoneZInfo mod, byte rawValue, ref byte value)
        {
            byte deadzone = mod.deadZone;
            int antiDeadzone = mod.antiDeadZone;
            int maxzone = mod.maxZone;
            double maxOutput = mod.maxOutput;

            double tempOutput = rawValue / 255.0;
            double tempAntiDead = 0.0;
            double ratio = maxzone / 100.0;
            double maxValue = 255.0 * ratio;

            if (deadzone > 0)
            {
                if (rawValue > deadzone)
                {
                    double current = NumericUtil.Clamp(0, value, maxValue);
                    tempOutput = (current - deadzone) / (maxValue - deadzone);
                }
                else
                {
                    tempOutput = 0.0;
                }
            }
            else
            {
                double current = NumericUtil.Clamp(0, value, maxValue);
                tempOutput = current / maxValue;
            }

            if (maxOutput != 100.0)
            {
                double maxOutRatio = maxOutput / 100.0;
                tempOutput = Math.Min(Math.Max(tempOutput, 0.0), maxOutRatio);
            }

            if (antiDeadzone > 0)
            {
                tempAntiDead = antiDeadzone * 0.01;
            }

            if (tempOutput > 0.0)
            {
                value = (byte)(((1.0 - tempAntiDead) * tempOutput + tempAntiDead) * 255.0);
            }
            else
            {
                value = 0;
            }
        }
    }
}
