using System;
using DS4Windows.DI;

namespace DS4Windows.Services
{
    /// <summary>
    /// Phase9 9-A(1) (b)（Phase9-A1-Plan.md §2）: <see cref="IStickDeadZoneProcessor"/> の実装（DI の Singleton。状態は持たない）。
    /// 旧 <c>Mapping.SetCurveAndDeadzone</c> の左スティックのデッドゾーンの処理（Radial と Axial、Radial の外側の出力）を、そのまま移したもの
    /// （右スティックのコードは、名前と数値の書き方［<c>128f</c> と <c>128.0</c>］以外は同じだったため、このメソッドに統合した。計算結果は変わらない）。
    /// 値の特性化は <c>Phase9A1SetCurveAndDeadzoneGoldenTests</c> が守る。
    /// </summary>
    public sealed class StickDeadZoneProcessor : IStickDeadZoneProcessor
    {
        // 性能: 毎フレーム 2 回（左右）呼ばれる。C# の確定代入で、ローカルは、読む前に必ず代入されるため、ローカルの 0 初期化を省く（SkipLocalsInit）
        [System.Runtime.CompilerServices.SkipLocalsInit]
        public void Process(StickDeadZoneInfo mod, byte rawX, byte rawY, ref byte x, ref byte y, ref byte outer)
        {
            if (mod.deadzoneType == StickDeadZoneInfo.DeadZoneType.Radial)
            {
                int stickDeadzone = mod.deadZone;
                int stickAntiDead = mod.antiDeadZone;
                int stickMaxZone = mod.maxZone;
                double stickMaxOutput = mod.maxOutput;
                double stickVerticalScale = mod.verticalScale;
                bool interpret = stickAntiDead > 0 || stickMaxZone != 100 || stickMaxOutput != 100.0 || mod.maxOutputForce || stickVerticalScale != StickDeadZoneInfo.DEFAULT_VERTICAL_SCALE;

                if (stickDeadzone > 0 || interpret)
                {
                    double stickSquared = Math.Pow(rawX - 128f, 2) + Math.Pow(rawY - 128f, 2);
                    double stickDeadzoneSquared = Math.Pow(stickDeadzone, 2);
                    if (stickDeadzone > 0 && stickSquared <= stickDeadzoneSquared)
                    {
                        x = 128;
                        y = 128;
                    }
                    else if ((stickDeadzone > 0 && stickSquared > stickDeadzoneSquared) || interpret)
                    {
                        double r = Math.Atan2(-(y - 128.0), (x - 128.0));
                        double maxXValue = x >= 128.0 ? 127.0 : -128;
                        double maxYValue = y >= 128.0 ? 127.0 : -128;
                        double ratio = stickMaxZone / 100.0;
                        double maxOutRatio = stickMaxOutput / 100.0;
                        double verticalScale = stickVerticalScale / 100.0;

                        double maxZoneXNegValue = (ratio * -128) + 128;
                        double maxZoneXPosValue = (ratio * 127) + 128;
                        double maxZoneYNegValue = maxZoneXNegValue;
                        double maxZoneYPosValue = maxZoneXPosValue;
                        double maxZoneX = x >= 128.0 ? (maxZoneXPosValue - 128.0) : (maxZoneXNegValue - 128.0);
                        double maxZoneY = y >= 128.0 ? (maxZoneYPosValue - 128.0) : (maxZoneYNegValue - 128.0);

                        double tempStickXDead = 0.0, tempStickYDead = 0.0;
                        double tempOutputX = 0.0, tempOutputY = 0.0;
                        if (stickDeadzone > 0)
                        {
                            tempStickXDead = Math.Abs(Math.Cos(r)) * (stickDeadzone / 127.0) * maxXValue;
                            tempStickYDead = Math.Abs(Math.Sin(r)) * (stickDeadzone / 127.0) * maxYValue;

                            if (stickSquared > stickDeadzoneSquared)
                            {
                                double currentX = NumericUtil.Clamp(maxZoneXNegValue, x, maxZoneXPosValue);
                                double currentY = NumericUtil.Clamp(maxZoneYNegValue, y, maxZoneYPosValue);
                                tempOutputX = ((currentX - 128.0 - tempStickXDead) / (maxZoneX - tempStickXDead));
                                tempOutputY = ((currentY - 128.0 - tempStickYDead) / (maxZoneY - tempStickYDead));
                            }
                        }
                        else
                        {
                            double currentX = NumericUtil.Clamp(maxZoneXNegValue, x, maxZoneXPosValue);
                            double currentY = NumericUtil.Clamp(maxZoneYNegValue, y, maxZoneYPosValue);
                            tempOutputX = (currentX - 128.0) / maxZoneX;
                            tempOutputY = (currentY - 128.0) / maxZoneY;
                        }

                        if (stickVerticalScale != StickDeadZoneInfo.DEFAULT_VERTICAL_SCALE)
                        {
                            tempOutputY = Math.Min(Math.Max(tempOutputY * verticalScale, 0.0), 1.0);
                        }

                        if (stickMaxOutput != 100.0 || mod.maxOutputForce)
                        {
                            double maxOutXRatio = Math.Abs(Math.Cos(r)) * maxOutRatio;
                            // Expand output a bit
                            maxOutXRatio = Math.Min(maxOutXRatio / 0.99, 1.0);

                            double maxOutYRatio = Math.Abs(Math.Sin(r)) * maxOutRatio;
                            // Expand output a bit
                            maxOutYRatio = Math.Min(maxOutYRatio / 0.99, 1.0);

                            tempOutputX = Math.Min(Math.Max(tempOutputX, 0.0), maxOutXRatio);
                            tempOutputY = Math.Min(Math.Max(tempOutputY, 0.0), maxOutYRatio);
                        }

                        double tempStickXAntiDeadPercent = 0.0, tempStickYAntiDeadPercent = 0.0;
                        if (stickAntiDead > 0)
                        {
                            tempStickXAntiDeadPercent = (stickAntiDead * 0.01) * Math.Abs(Math.Cos(r));
                            tempStickYAntiDeadPercent = (stickAntiDead * 0.01) * Math.Abs(Math.Sin(r));
                        }

                        if (tempOutputX > 0.0)
                        {
                            x = (byte)((((1.0 - tempStickXAntiDeadPercent) * tempOutputX + tempStickXAntiDeadPercent)) * maxXValue + 128.0);
                        }
                        else
                        {
                            x = 128;
                        }

                        if (tempOutputY > 0.0)
                        {
                            y = (byte)((((1.0 - tempStickYAntiDeadPercent) * tempOutputY + tempStickYAntiDeadPercent)) * maxYValue + 128.0);
                        }
                        else
                        {
                            y = 128;
                        }
                    }
                }

                // Process Outer Binding
                outer = 0;
                if (x != 128 || y != 128)
                {
                    int adjustX = x - 128;
                    int adjustY = y - 128;
                    double r = Math.Atan2(-adjustY, adjustX);
                    //double r = Math.Atan2(-(dState.RY - 128.0), (dState.RX - 128.0));
                    //double maxXValue = dState.RX >= 128.0 ? 127.0 : -128;
                    //double maxYValue = dState.RY >= 128.0 ? 127.0 : -128;
                    double hyp = Math.Sqrt((adjustX * adjustX) + (adjustY * adjustY));

                    if (hyp != 0.0)
                    {
                        int tempX = (int)(Math.Abs(Math.Cos(r)) * (x >= 128 ? 127 : 128));
                        int tempY = (int)(Math.Abs(Math.Sin(r)) * (y >= 128 ? 127 : 128));
                        double maxValue = Math.Sqrt((tempX * tempX) + (tempY * tempY));
                        double ratio = hyp / maxValue;
                        if (ratio > 1.0) ratio = 1.0;
                        double currentValue = ratio * 255.0;
                        double deadValue = mod.outerBindDeadZone * 0.01 * 255.0;
                        if (!mod.outerBindInvert && currentValue > deadValue)
                        {
                            double outputRatio = (currentValue - deadValue) / (double)(255.0 - deadValue);
                            outer = (byte)(outputRatio * 255);
                        }
                        else if (mod.outerBindInvert && currentValue < deadValue)
                        {
                            double outputRatio = (deadValue - currentValue) / (double)deadValue;
                            outer = (byte)(outputRatio * 255);
                        }
                    }
                }
            }
            else if (mod.deadzoneType == StickDeadZoneInfo.DeadZoneType.Axial)
            {
                ref StickDeadZoneInfo.AxisDeadZoneInfo xAxisDeadInfo = ref mod.xAxisDeadInfo;
                if (xAxisDeadInfo.deadZone > 0 || xAxisDeadInfo.antiDeadZone > 0 || xAxisDeadInfo.maxZone != 100 || xAxisDeadInfo.maxOutput != 100)
                {
                    int distVal = Math.Abs(rawX - 128);
                    if (xAxisDeadInfo.deadZone > 0 && distVal <= xAxisDeadInfo.deadZone)
                    {
                        x = 128;
                    }
                    else if ((xAxisDeadInfo.deadZone > 0 && distVal > xAxisDeadInfo.deadZone) || xAxisDeadInfo.antiDeadZone > 0 || xAxisDeadInfo.maxZone != 100 || xAxisDeadInfo.maxOutput != 100)
                    {
                        double maxAxisValue = x >= 128.0 ? 127.0 : -128.0;
                        double ratio = xAxisDeadInfo.maxZone / 100.0;
                        double maxOutRatio = xAxisDeadInfo.maxOutput / 100.0;

                        double maxZoneNegValue = (ratio * -128.0) + 128.0;
                        double maxZonePosValue = (ratio * 127.0) + 128.0;
                        double maxZone = x >= 128.0 ? (maxZonePosValue - 128.0) : (maxZoneNegValue - 128.0);

                        double tempDead = (xAxisDeadInfo.deadZone > 0) ? ((xAxisDeadInfo.deadZone / 127.0) * maxAxisValue) : 0.0;
                        double currentVal = NumericUtil.Clamp(maxZoneNegValue, x, maxZonePosValue);
                        double tempOutput = (currentVal - 128.0 - tempDead) / (maxZone - tempDead);

                        if (xAxisDeadInfo.maxOutput != 100.0)
                        {
                            // Expand output a bit
                            maxOutRatio = Math.Min(maxOutRatio / 0.99, 1.0);
                            tempOutput = Math.Min(Math.Max(tempOutput, 0.0), maxOutRatio);
                        }

                        double tempAntiDeadPercent = 0.0;
                        if (xAxisDeadInfo.antiDeadZone > 0)
                        {
                            tempAntiDeadPercent = xAxisDeadInfo.antiDeadZone * 0.01;
                        }

                        if (tempOutput > 0.0)
                        {
                            x = (byte)((((1.0 - tempAntiDeadPercent) * tempOutput + tempAntiDeadPercent)) * maxAxisValue + 128.0);
                        }
                        else
                        {
                            x = 128;
                        }
                    }
                }

                ref StickDeadZoneInfo.AxisDeadZoneInfo yAxisDeadInfo = ref mod.yAxisDeadInfo;
                if (yAxisDeadInfo.deadZone > 0 || yAxisDeadInfo.antiDeadZone > 0 || yAxisDeadInfo.maxZone != 100 || yAxisDeadInfo.maxOutput != 100)
                {
                    int distVal = Math.Abs(rawY - 128);
                    if (yAxisDeadInfo.deadZone > 0 && distVal <= yAxisDeadInfo.deadZone)
                    {
                        y = 128;
                    }
                    else if ((yAxisDeadInfo.deadZone > 0 && distVal > yAxisDeadInfo.deadZone) || yAxisDeadInfo.antiDeadZone > 0 || yAxisDeadInfo.maxZone != 100 || yAxisDeadInfo.maxOutput != 100)
                    {
                        double maxAxisValue = y >= 128.0 ? 127.0 : -128.0;
                        double ratio = yAxisDeadInfo.maxZone / 100.0;
                        double maxOutRatio = yAxisDeadInfo.maxOutput / 100.0;

                        double maxZoneNegValue = (ratio * -128.0) + 128.0;
                        double maxZonePosValue = (ratio * 127.0) + 128.0;
                        double maxZone = y >= 128.0 ? (maxZonePosValue - 128.0) : (maxZoneNegValue - 128.0);

                        double tempDead = (yAxisDeadInfo.deadZone > 0) ? ((yAxisDeadInfo.deadZone / 127.0) * maxAxisValue) : 0.0;
                        double currentVal = NumericUtil.Clamp(maxZoneNegValue, y, maxZonePosValue);
                        double tempOutput = (currentVal - 128.0 - tempDead) / (maxZone - tempDead);

                        if (yAxisDeadInfo.maxOutput != 100.0)
                        {
                            // Expand output a bit
                            maxOutRatio = Math.Min(maxOutRatio / 0.99, 1.0);
                            tempOutput = Math.Min(Math.Max(tempOutput, 0.0), maxOutRatio);
                        }

                        double tempAntiDeadPercent = 0.0;
                        if (yAxisDeadInfo.antiDeadZone > 0)
                        {
                            tempAntiDeadPercent = yAxisDeadInfo.antiDeadZone * 0.01;
                        }

                        if (tempOutput > 0.0)
                        {
                            y = (byte)((((1.0 - tempAntiDeadPercent) * tempOutput + tempAntiDeadPercent)) * maxAxisValue + 128.0);
                        }
                        else
                        {
                            y = 128;
                        }
                    }
                }
            }


        }
    }
}
