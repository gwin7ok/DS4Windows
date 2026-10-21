using System;
using DS4Windows.DI;

namespace DS4Windows.Services
{
    /// <summary>
    /// Phase9 9-A(1) (c)（Phase9-A1-Plan.md §2）: <see cref="IOutputCurveProcessor"/> の実装（DI の Singleton。状態は持たない）。
    /// 旧 <c>Mapping.SetCurveAndDeadzone</c> の左スティックの出力カーブとスクエアスティック、L2 の出力カーブを、そのまま移したもの
    /// （右スティックと R2 のコードは、名前以外は同じだったため、統合した。計算結果は変わらない）。
    /// スクエアスティックは、旧 <c>Mapping.DS4SquareStick</c>（デバイスごとの作業用オブジェクト）の計算を、ローカル変数で行う（割り当て 0。結果は同じ）。
    /// 値の特性化は <c>Phase9A1SetCurveAndDeadzoneGoldenTests</c> が守る。
    /// </summary>
    public sealed class OutputCurveProcessor : IOutputCurveProcessor
    {
        // 性能: 毎フレーム呼ばれうる。ローカルは、読む前に必ず代入されるため、0 初期化を省く（SkipLocalsInit）
        [System.Runtime.CompilerServices.SkipLocalsInit]
        public void ProcessSquareStick(double roundness, ref byte x, ref byte y)
        {
            double capX = x >= 128 ? 127.0 : 128.0;
            double capY = y >= 128 ? 127.0 : 128.0;
            double tempX = (x - 128.0) / capX;
            double tempY = (y - 128.0) / capY;
            CircleToSquare(ref tempX, ref tempY, roundness);
            tempX = tempX < -1.0 ? -1.0 : tempX > 1.0
                ? 1.0 : tempX;
            tempY = tempY < -1.0 ? -1.0 : tempY > 1.0
                ? 1.0 : tempY;
            x = (byte)(tempX * capX + 128.0);
            y = (byte)(tempY * capY + 128.0);
        }

        // Modification of squared stick routine documented
        // at http://theinstructionlimit.com/squaring-the-thumbsticks
        // （旧 Mapping.DS4SquareStick.CircleToSquare。current＝(curX, curY)、squared＝(sqX, sqY)）
        private static void CircleToSquare(ref double curX, ref double curY, double roundness)
        {
            const double PiOverFour = Math.PI / 4.0;
            double sqX, sqY;

            // Determine the theta angle
            double angle = Math.Atan2(curY, -curX);
            angle += Math.PI;
            double cosAng = Math.Cos(angle);
            // Scale according to which wall we're clamping to
            // X+ wall
            if (angle <= PiOverFour || angle > 7.0 * PiOverFour)
            {
                double tempVal = 1.0 / cosAng;
                sqX = curX * tempVal;
                sqY = curY * tempVal;
            }
            // Y+ wall
            else if (angle > PiOverFour && angle <= 3.0 * PiOverFour)
            {
                double tempVal = 1.0 / Math.Sin(angle);
                sqX = curX * tempVal;
                sqY = curY * tempVal;
            }
            // X- wall
            else if (angle > 3.0 * PiOverFour && angle <= 5.0 * PiOverFour)
            {
                double tempVal = -1.0 / cosAng;
                sqX = curX * tempVal;
                sqY = curY * tempVal;
            }
            // Y- wall
            else if (angle > 5.0 * PiOverFour && angle <= 7.0 * PiOverFour)
            {
                double tempVal = -1.0 / Math.Sin(angle);
                sqX = curX * tempVal;
                sqY = curY * tempVal;
            }
            else return;

            double length = curX / cosAng;
            double factor = Math.Pow(length, roundness);
            curX += (sqX - curX) * factor;
            curY += (sqY - curY) * factor;
        }

        [System.Runtime.CompilerServices.SkipLocalsInit]
        public void ProcessStickCurve(int mode, StickDeadZoneInfo.DeadZoneType deadzoneType, BezierCurve bezier, ref byte x, ref byte y)
        {
            double tempRatioX = 0.0, tempRatioY = 0.0;
            double capX = 0.0, capY = 0.0;
            if (deadzoneType == StickDeadZoneInfo.DeadZoneType.Radial)
            {
                double r = Math.Atan2(-(y - 128.0), (x - 128.0));
                double maxOutXRatio = Math.Abs(Math.Cos(r));
                double maxOutYRatio = Math.Abs(Math.Sin(r));
                double sideX = x - 128; double sideY = y - 128.0;
                capX = x >= 128 ? maxOutXRatio * 127.0 : maxOutXRatio * 128.0;
                capY = y >= 128 ? maxOutYRatio * 127.0 : maxOutYRatio * 128.0;
                double absSideX = Math.Abs(sideX); double absSideY = Math.Abs(sideY);
                if (absSideX > capX) capX = absSideX;
                if (absSideY > capY) capY = absSideY;
                tempRatioX = capX > 0 ? (x - 128.0) / capX : 0;
                tempRatioY = capY > 0 ? (y - 128.0) / capY : 0;
            }
            else if (deadzoneType == StickDeadZoneInfo.DeadZoneType.Axial)
            {
                capX = x >= 128 ? 127.0 : 128.0;
                capY = y >= 128 ? 127.0 : 128.0;
                tempRatioX = (x - 128.0) / capX;
                tempRatioY = (y - 128.0) / capY;
            }

            double signX = tempRatioX >= 0.0 ? 1.0 : -1.0;
            double signY = tempRatioY >= 0.0 ? 1.0 : -1.0;
            if (mode == 1)
            {
                double absX = Math.Abs(tempRatioX);
                double absY = Math.Abs(tempRatioY);
                double outputX = 0.0;
                double outputY = 0.0;

                if (absX <= 0.4)
                {
                    outputX = 0.8 * absX;
                }
                else if (absX <= 0.75)
                {
                    outputX = absX - 0.08;
                }
                else if (absX > 0.75)
                {
                    outputX = (absX * 1.32) - 0.32;
                }

                if (absY <= 0.4)
                {
                    outputY = 0.8 * absY;
                }
                else if (absY <= 0.75)
                {
                    outputY = absY - 0.08;
                }
                else if (absY > 0.75)
                {
                    outputY = (absY * 1.32) - 0.32;
                }

                x = (byte)(outputX * signX * capX + 128.0);
                y = (byte)(outputY * signY * capY + 128.0);
            }
            else if (mode == 2)
            {
                double outputX = tempRatioX * tempRatioX;
                double outputY = tempRatioY * tempRatioY;
                x = (byte)(outputX * signX * capX + 128.0);
                y = (byte)(outputY * signY * capY + 128.0);
            }
            else if (mode == 3)
            {
                double outputX = tempRatioX * tempRatioX * tempRatioX;
                double outputY = tempRatioY * tempRatioY * tempRatioY;
                x = (byte)(outputX * capX + 128.0);
                y = (byte)(outputY * capY + 128.0);
            }
            else if (mode == 4)
            {
                double absX = Math.Abs(tempRatioX);
                double absY = Math.Abs(tempRatioY);
                double outputX = absX * (absX - 2.0);
                double outputY = absY * (absY - 2.0);
                x = (byte)(-1.0 * outputX * signX * capX + 128.0);
                y = (byte)(-1.0 * outputY * signY * capY + 128.0);
            }
            else if (mode == 5)
            {
                double innerX = Math.Abs(tempRatioX) - 1.0;
                double innerY = Math.Abs(tempRatioY) - 1.0;
                double outputX = innerX * innerX * innerX + 1.0;
                double outputY = innerY * innerY * innerY + 1.0;
                x = (byte)(1.0 * outputX * signX * capX + 128.0);
                y = (byte)(1.0 * outputY * signY * capY + 128.0);
            }
            else if (mode == 6)
            {
                if (deadzoneType == StickDeadZoneInfo.DeadZoneType.Radial)
                {
                    // Get max values and circular distance of axes
                    double maxX = (x >= 128 ? 127 : 128);
                    double maxY = (y >= 128 ? 127 : 128);
                    byte tempOutX = (byte)(tempRatioX * maxX + 128.0);
                    byte tempOutY = (byte)(tempRatioY * maxY + 128.0);

                    // Perform curve based on byte values from vector
                    byte tempX = bezier.arrayBezierLUT[tempOutX];
                    byte tempY = bezier.arrayBezierLUT[tempOutY];

                    // Calculate new ratio
                    double tempRatioOutX = (tempX - 128.0) / maxX;
                    double tempRatioOutY = (tempY - 128.0) / maxY;

                    // Map back to stick coordinates
                    x = (byte)(tempRatioOutX * capX + 128);
                    y = (byte)(tempRatioOutY * capY + 128);
                }
                else if (deadzoneType == StickDeadZoneInfo.DeadZoneType.Axial)
                {
                    x = bezier.arrayBezierLUT[x];
                    y = bezier.arrayBezierLUT[y];
                }
            }
        }

        [System.Runtime.CompilerServices.SkipLocalsInit]
        public void ProcessTriggerCurve(int mode, BezierCurve bezier, ref byte value)
        {
            double temp = value / 255.0;
            if (mode == 1)
            {
                double output;

                if (temp <= 0.4)
                    output = 0.55 * temp;
                else if (temp <= 0.75)
                    output = temp - 0.18;
                else // if (temp > 0.75)
                    output = (temp * 1.72) - 0.72;
                value = (byte)(output * 255.0);
            }
            else if (mode == 2)
            {
                double output = temp * temp;
                value = (byte)(output * 255.0);
            }
            else if (mode == 3)
            {
                double output = temp * temp * temp;
                value = (byte)(output * 255.0);
            }
            else if (mode == 4)
            {
                double output = temp * (temp - 2.0);
                value = (byte)(-1.0 * output * 255.0);
            }
            else if (mode == 5)
            {
                double inner = Math.Abs(temp) - 1.0;
                double output = inner * inner * inner + 1.0;
                value = (byte)(-1.0 * output * 255.0);
            }
            else if (mode == 6)
            {
                value = bezier.arrayBezierLUT[value];
            }
        }
    }
}
