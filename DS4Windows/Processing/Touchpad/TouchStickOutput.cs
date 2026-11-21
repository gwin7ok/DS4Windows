/*
DS4Windows
Copyright (C) 2023  Travis Nickles

This program is free software: you can redistribute it and/or modify
it under the terms of the GNU General Public License as published by
the Free Software Foundation, either version 3 of the License, or
(at your option) any later version.

This program is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
GNU General Public License for more details.

You should have received a copy of the GNU General Public License
along with this program.  If not, see <https://www.gnu.org/licenses/>.
*/

using System;
using DS4Windows.Core.Abstractions;
using DS4Windows.Core.Settings;
using DS4Windows.Core.Utilities;
using DS4Windows.Input;

namespace DS4Windows.Processing.Touchpad
{
    /// <summary>
    /// タッチパッドの移動量を、スティックの出力（左または右）へ変える（タッチ → スティック）。回転、デッドゾーン、スムージング、出力カーブ、
    /// 反転を適用して、<see cref="StickMotionState.MapStickActionData"/> へ重ねる。タッチの間のフレームごとに呼ばれる。
    /// </summary>
    public sealed class TouchStickOutput
    {
        private readonly int deviceNum;
        private readonly IProfileSettingsService _profileSettings;
        private readonly StickMotionState _stickMotion;

        public TouchStickOutput(int deviceNum, IProfileSettingsService profileSettings, StickMotionState stickMotion)
        {
            this.deviceNum = deviceNum;
            ArgumentNullException.ThrowIfNull(profileSettings);
            _profileSettings = profileSettings;
            ArgumentNullException.ThrowIfNull(stickMotion);
            _stickMotion = stickMotion;
        }

        /// <summary>スムージングの OneEuro フィルター（設定の変更の通知を受ける側）。</summary>
        public OneEuroFilterPair Filter { get; } = new OneEuroFilterPair(1.0, 1.0);

        /// <summary>タッチの移動量（dx, dy）を、スティックの出力へ重ねる。<paramref name="elapsedTime"/> は、直前のフレームからの秒数。</summary>
        public void Apply(int dx, int dy, double elapsedTime)
        {
            //Trace.WriteLine($"DX {dx}");

            int deltaX = 0, deltaY = 0;
            deltaX = dx;
            deltaY = dy;
            //int inputX = deltaX, inputY = deltaY;
            int maxDirX = deltaX >= 0 ? 127 : -128;
            int maxDirY = deltaY >= 0 ? 127 : -128;

            //GyroMouseStickInfo msinfo = Global.ForSlot(deviceNum).Gyro.MouseStick;
            TouchMouseStickInfo msinfo = _profileSettings.ForSlot(deviceNum).Touchpad.MouseStick;
            if (msinfo.rotationRad != TouchMouseStickInfo.ANG_RAD_DEFAULT)
            {
                //double rotation = 5.0 * Math.PI / 180.0;
                double rotation = msinfo.rotationRad;
                double sinAngle = Math.Sin(rotation), cosAngle = Math.Cos(rotation);
                int tempX = deltaX, tempY = deltaY;
                deltaX = (int)NumericUtil.Clamp(-DS4Touchpad.RESOLUTION_X_MAX, tempX * cosAngle - tempY * sinAngle, DS4Touchpad.RESOLUTION_X_MAX);
                deltaY = (int)NumericUtil.Clamp(-DS4Touchpad.RESOLUTION_Y_MAX, tempX * sinAngle + tempY * cosAngle, DS4Touchpad.RESOLUTION_Y_MAX);
            }

            //double tempDouble = s.elapsedTime * 250.0; // Base default speed on 4 ms
            //double tempDouble = 1.0;
            double tempAngle = Math.Atan2(-deltaY, deltaX);
            double normX = Math.Abs(Math.Cos(tempAngle));
            double normY = Math.Abs(Math.Sin(tempAngle));
            int signX = Math.Sign(deltaX);
            int signY = Math.Sign(deltaY);

            int deadzoneX = (int)Math.Abs(normX * msinfo.deadZone);
            //int deadzoneY = (int)Math.Abs(normY * msinfo.deadZone);
            int radialDeadZoneY = (int)(Math.Abs(normY * msinfo.deadZone));

            int maxZone = msinfo.maxZone;
            double maxZone_d = maxZone;
            int maxValX = signX * maxZone;
            int maxValY = signY * maxZone;
            // Possibly tweak limits later
            int maxDeadZoneAxial = (int)(maxZone * 0.30);
            int minDeadZoneAxial = (int)(maxZone * 0.06);

            int absDX = Math.Abs(deltaX);
            int absDY = Math.Abs(deltaY);

            double xratio = 0.0, yratio = 0.0;
            double antiX = msinfo.antiDeadX * normX;
            double antiY = msinfo.antiDeadY * normY;

            // Check for radial dead zone first
            double mag = (deltaX * deltaX) + (deltaY * deltaY);
            if (mag <= (0 * 0))
            {
                deltaX = 0;
                deltaY = 0;
            }
            // Past radial. Check for bowtie
            else
            {
                double tempRangeRatioX = absDX / Math.Abs((double)maxValX);
                double tempRangeRatioY = absDY / Math.Abs((double)maxValY);

                int axialDeadX = (int)((maxDeadZoneAxial - minDeadZoneAxial) *
                    Math.Min(1.0, tempRangeRatioY) + minDeadZoneAxial);
                int deadzoneY = (int)((maxDeadZoneAxial - minDeadZoneAxial) *
                    Math.Min(1.0, tempRangeRatioX) + minDeadZoneAxial);

                if (Math.Abs(deltaX) > axialDeadX)
                {
                    int tempUseDeadX = deadzoneX > axialDeadX ? deadzoneX : axialDeadX;
                    deltaX -= signX * tempUseDeadX;
                    double newMaxValX = Math.Abs(maxValX) - tempUseDeadX;
                    double scaleX = Math.Abs(deltaX) / (double)(newMaxValX);
                    deltaX = (int)(maxValX * scaleX);

                    deltaX = (deltaX < 0 && deltaX < maxValX) ? maxValX :
                        (deltaX > 0 && deltaX > maxValX) ? maxValX : deltaX;
                }
                else
                {
                    //Trace.WriteLine("IN DEAD");
                    deltaX = 0;
                }

                if (Math.Abs(deltaY) > deadzoneY)
                {
                    int tempUseDeadY = radialDeadZoneY > deadzoneY ? radialDeadZoneY : deadzoneY;
                    deltaY -= signY * tempUseDeadY;
                    double newMaxValY = Math.Abs(maxValY) - tempUseDeadY;
                    double scaleY = Math.Abs(deltaY) / (double)(newMaxValY);
                    deltaY = (int)(maxValY * scaleY);

                    deltaY = (deltaY < 0 && deltaY < maxValY) ? maxValY :
                        (deltaY > 0 && deltaY > maxValY) ? maxValY : deltaY;
                }
                else
                {
                    deltaY = 0;
                }

                //if (Math.Abs(deltaX) > deadzoneX)
                //{
                //    deltaX -= signX * deadzoneX;
                //    //deltaX = (int)(deltaX * tempDouble);
                //    deltaX = (deltaX < 0 && deltaX < maxValX) ? maxValX :
                //        (deltaX > 0 && deltaX > maxValX) ? maxValX : deltaX;
                //    //if (deltaX != maxValX) deltaX -= deltaX % (signX * GyroMouseFuzz);
                //}
                //else
                //{
                //    Trace.WriteLine("IN DEAD");
                //    deltaX = 0;
                //}

                //if (Math.Abs(deltaY) > deadzoneY)
                //{
                //    deltaY -= signY * deadzoneY;
                //    //deltaY = (int)(deltaY * tempDouble);
                //    deltaY = (deltaY < 0 && deltaY < maxValY) ? maxValY :
                //        (deltaY > 0 && deltaY > maxValY) ? maxValY : deltaY;
                //    //if (deltaY != maxValY) deltaY -= deltaY % (signY * GyroMouseFuzz);
                //}
                //else
                //{
                //    deltaY = 0;
                //}
            }

            //if (true)
            if (msinfo.UseSmoothing)
            {
                double currentRate = 1.0 / elapsedTime;
                double oldDeltaX = deltaX, oldDeltaY = deltaY;
                // Adjust sensitivity to work around rounding in filter method
                double tempDeltaX = Filter.axis1Filter.Filter(deltaX * 1.0005, currentRate);
                double tempDeltaY = Filter.axis2Filter.Filter(deltaY * 1.0005, currentRate);

                // Filter does not go back to absolute zero for reasons. Check
                // for low number and reset to zero
                if (Math.Abs(tempDeltaX) < 0.0001) tempDeltaX = 0;
                if (Math.Abs(tempDeltaY) < 0.0001) tempDeltaY = 0;

                // Need to check bounds again
                deltaX = (int)(Math.Clamp(tempDeltaX, -maxZone_d, maxZone_d));
                deltaY = (int)(Math.Clamp(tempDeltaY, -maxZone_d, maxZone_d));

                //Trace.WriteLine($"{tempDeltaX} {tempDeltaY} {oldDeltaX} {oldDeltaY}");

                maxValX = deltaX < 0 ? -maxZone : maxZone;
                maxValY = deltaY < 0 ? -maxZone : maxZone;
                maxDirX = deltaX >= 0 ? 127 : -128;
                maxDirY = deltaY >= 0 ? 127 : -128;
            }
            //if (msinfo.useSmoothing)
            //{
            //    if (msinfo.smoothingMethod == GyroSmoothingMethod.OneEuro)
            //    {
            //        double currentRate = 1.0 / arg.sixAxis.elapsed;
            //        deltaX = (int)(filterPair.axis1Filter.Filter(deltaX, currentRate));
            //        deltaY = (int)(filterPair.axis2Filter.Filter(deltaY, currentRate));
            //    }
            //    else if (msinfo.smoothingMethod == GyroSmoothingMethod.WeightedAverage)
            //    {
            //        int iIndex = smoothBufferTail % SMOOTH_BUFFER_LEN;
            //        xSmoothBuffer[iIndex] = deltaX;
            //        ySmoothBuffer[iIndex] = deltaY;
            //        smoothBufferTail = iIndex + 1;

            //        double currentWeight = 1.0;
            //        double finalWeight = 0.0;
            //        double x_out = 0.0, y_out = 0.0;
            //        int idx = 0;
            //        for (int i = 0; i < SMOOTH_BUFFER_LEN; i++)
            //        {
            //            idx = (smoothBufferTail - i - 1 + SMOOTH_BUFFER_LEN) % SMOOTH_BUFFER_LEN;
            //            x_out += xSmoothBuffer[idx] * currentWeight;
            //            y_out += ySmoothBuffer[idx] * currentWeight;
            //            finalWeight += currentWeight;
            //            currentWeight *= msinfo.smoothWeight;
            //        }

            //        x_out /= finalWeight;
            //        deltaX = (int)x_out;
            //        y_out /= finalWeight;
            //        deltaY = (int)y_out;
            //    }

            //    maxValX = deltaX < 0 ? -msinfo.maxZone : msinfo.maxZone;
            //    maxValY = deltaY < 0 ? -msinfo.maxZone : msinfo.maxZone;
            //    maxDirX = deltaX >= 0 ? 127 : -128;
            //    maxDirY = deltaY >= 0 ? 127 : -128;
            //}

            if (deltaX != 0) xratio = deltaX / (double)maxValX;
            if (deltaY != 0) yratio = deltaY / (double)maxValY;

            if (msinfo.vertScale != 100)
            {
                double verticalScale = msinfo.vertScale * 0.01;
                deltaY = (int)(deltaY * verticalScale);
                deltaY = (deltaY < 0 && deltaY < maxValY) ? maxValY :
                    (deltaY > 0 && deltaY > maxValY) ? maxValY : deltaY;
            }

            if (msinfo.maxOutputEnabled)
            {
                double maxOutRatio = msinfo.maxOutput / 100.0;
                // Expand output a bit. Likely not going to get a straight line with Gyro
                double maxOutXRatio = Math.Min(normX / 0.95, 1.0) * maxOutRatio;
                double maxOutYRatio = Math.Min(normY / 0.95, 1.0) * maxOutRatio;

                xratio = Math.Min(Math.Max(xratio, 0.0), maxOutXRatio);
                yratio = Math.Min(Math.Max(yratio, 0.0), maxOutYRatio);
            }

            if (msinfo.outputCurve != StickOutCurve.Curve.Linear)
            {
                StickOutCurve.CalcOutValue(msinfo.outputCurve, xratio, yratio,
                    out xratio, out yratio);
            }

            double xNorm = 0.0, yNorm = 0.0;
            if (xratio != 0.0)
            {
                xNorm = (1.0 - antiX) * xratio + antiX;
            }

            if (yratio != 0.0)
            {
                yNorm = (1.0 - antiY) * yratio + antiY;
            }

            if (msinfo.inverted != 0)
            {
                if ((msinfo.inverted & 1) == 1)
                {
                    // Invert max dir value
                    maxDirX = deltaX >= 0 ? -128 : 127;
                }

                if ((msinfo.inverted & 2) == 2)
                {
                    // Invert max dir value
                    maxDirY = deltaY >= 0 ? -128 : 127;
                }
            }

            byte axisXOut = (byte)(xNorm * maxDirX + 128.0);
            byte axisYOut = (byte)(yNorm * maxDirY + 128.0);
            //Trace.WriteLine($"{deltaX} {axisXOut}");

            bool outputX = msinfo.OutputHorizontal();
            bool outputY = msinfo.OutputVertical();

            //Mapping.touchStickX[deviceNum] = axisXOut;
            //Mapping.touchStickY[deviceNum] = axisYOut;
            PostMapStickData tempMapStickData = _stickMotion.Devices[deviceNum].MapStickActionData;
            if (outputX)
            {
                if (msinfo.outputStick == TouchMouseStickInfo.OutputStick.LeftStick &&
                    Math.Abs(axisXOut - 128) > Math.Abs(tempMapStickData.LX - 128))
                {
                    tempMapStickData.LX = axisXOut;
                    tempMapStickData.dirty = true;
                }
                else if (msinfo.outputStick == TouchMouseStickInfo.OutputStick.RightStick &&
                    Math.Abs(axisXOut - 128) > Math.Abs(tempMapStickData.RX - 128))
                {
                    tempMapStickData.RX = axisXOut;
                    tempMapStickData.dirty = true;
                }

                //Mapping.touchStickX[deviceNum] = axisXOut;
            }

            if (outputY)
            {
                if (msinfo.outputStick == TouchMouseStickInfo.OutputStick.LeftStick &&
                    Math.Abs(axisYOut - 128) > Math.Abs(tempMapStickData.LY - 128))
                {
                    tempMapStickData.LY = axisYOut;
                    tempMapStickData.dirty = true;
                }
                else if (msinfo.outputStick == TouchMouseStickInfo.OutputStick.RightStick &&
                    Math.Abs(axisYOut - 128) > Math.Abs(tempMapStickData.RY - 128))
                {
                    tempMapStickData.RY = axisYOut;
                    tempMapStickData.dirty = true;
                }

                //Mapping.touchStickY[deviceNum] = axisYOut;
            }
        }
    }
}
