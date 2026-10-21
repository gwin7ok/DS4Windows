using System;
using DS4Windows.DI;

namespace DS4Windows.Services
{
    /// <summary>
    /// Phase9 9-A(2)-A（Phase9-A2-Plan.md）: <see cref="IGyroControlsProcessor"/> の実装（DI の Singleton。状態は持たない）。
    /// 旧 <c>Mapping.SetCurveAndDeadzone</c> の「ジャイロ → コントロール」の X（SX）の処理を、そのまま移したもの
    /// （Z［SZ］のコードは、名前以外は同じだったため、統合した。出力カーブを適用する条件の違い［Z だけ <c>outputAccelZ != 0</c>］は、呼び出し側に残した。計算結果は変わらない）。
    /// 値の特性化は <c>Phase9A2GyroControlsGoldenTests</c> が守る。
    /// </summary>
    public sealed class GyroControlsProcessor : IGyroControlsProcessor
    {
        // 性能: ローカルは、読む前に必ず代入されるため、0 初期化を省く（SkipLocalsInit）
        [System.Runtime.CompilerServices.SkipLocalsInit]
        public int Correct(int accel, int deadZone, double maxZone, double antiDead, double sens)
        {
            int absAccel = Math.Abs(accel);

            if (deadZone > 0 || maxZone < 1.0 || antiDead > 0)
            {
                int maxValue = (int)(maxZone * 128d);
                if (absAccel > deadZone)
                {
                    double ratio = absAccel < maxValue ? (absAccel - deadZone) / (double)(maxValue - deadZone) : 1.0;
                    return Math.Sign(accel) *
                        (int)Math.Min(128d, sens * 128d * ((1.0 - antiDead) * ratio + antiDead));
                }

                return 0;
            }

            return Math.Sign(accel) *
                (int)Math.Min(128d, sens * 128d * (absAccel / 128d));
        }

        [System.Runtime.CompilerServices.SkipLocalsInit]
        public int ApplyCurve(int mode, BezierCurve bezier, int outputAccel)
        {
            double temp = outputAccel / 128.0;
            double sign = Math.Sign(temp);
            if (mode == 1)
            {
                double output;
                double abs = Math.Abs(temp);

                if (abs <= 0.4)
                    output = 0.55 * abs;
                else if (abs <= 0.75)
                    output = abs - 0.18;
                else // if (abs > 0.75)
                    output = (abs * 1.72) - 0.72;
                return (int)(output * sign * 128.0);
            }
            else if (mode == 2)
            {
                double output = temp * temp;
                return (int)(output * sign * 128.0);
            }
            else if (mode == 3)
            {
                double output = temp * temp * temp;
                return (int)(output * 128.0);
            }
            else if (mode == 4)
            {
                double abs = Math.Abs(temp);
                double output = abs * (abs - 2.0);
                return (int)(-1.0 * output *
                    sign * 128.0);
            }
            else if (mode == 5)
            {
                double inner = Math.Abs(temp) - 1.0;
                double output = inner * inner * inner + 1.0;
                return (int)(output *
                    sign * 128.0);
            }
            else if (mode == 6)
            {
                int signSA = Math.Sign(outputAccel);
                return bezier.arrayBezierLUT[Math.Min(Math.Abs(outputAccel), 128)] * signSA;
            }

            return outputAccel;
        }
    }
}
