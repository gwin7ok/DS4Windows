using System;
using DS4Windows.DI;

namespace DS4Windows.Services
{
    /// <summary>
    /// Phase9 9-A(2)-C（Phase9-A2-Plan.md）: <see cref="IFlickStickProcessor"/> の実装（DI の Singleton。状態は持たない）。
    /// 旧 <c>Mapping.ProcessFlickStick</c>／<c>HandleFlickStickAngle</c>／<c>WarpEaseOut</c> を、そのまま移したもの
    /// （<c>ProcessFlickStick</c> の未使用の引数［<c>device</c>、<c>ControlService ctrl</c>］は外した。<c>DS4State</c> は <c>elapsedTime</c> だけ使うため、値で受ける。計算結果は変わらない）。
    /// 値の特性化は <c>Phase9A2FlickStickGoldenTests</c> が守る。
    /// </summary>
    public sealed class FlickStickProcessor : IFlickStickProcessor
    {
        public void Process(FlickStickMappingData data, FlickStickSettings settings, double elapsedTime,
            byte stickX, byte stickY, byte prevStickX, byte prevStickY, ref double mouseDeltaX)
        {
            FlickStickMappingData tempFlickData = data;
            double angleChange = HandleFlickStickAngle(elapsedTime, stickX, stickY, prevStickX, prevStickY,
                tempFlickData, settings);
            //angleChange = flickFilter.Filter(angleChange, cState.elapsedTime);
            //Console.WriteLine(angleChange);
            //if (angleChange != 0.0)
            double lsangle = angleChange * 180.0 / Math.PI;
            if (lsangle == 0.0)
            {
                tempFlickData.flickAngleRemainder = 0.0;
            }
            else if (lsangle >= 0.0 && tempFlickData.flickAngleRemainder >= 0.0)
            {
                lsangle += tempFlickData.flickAngleRemainder;
            }

            tempFlickData.flickAngleRemainder = 0.0;
            //Console.WriteLine(lsangle);
            //if (angleChange != 0.0)
            if (settings.minAngleThreshold == 0.0 && lsangle != 0.0)
            //if (Math.Abs(lsangle) >= 0.5)
            {
                tempFlickData.flickAngleRemainder = 0.0;
                //flickAngleRemainder = lsangle - (int)lsangle;
                //lsangle = (int)lsangle;
                mouseDeltaX += lsangle * settings.realWorldCalibration;
            }
            else if (Math.Abs(lsangle) >= settings.minAngleThreshold)
            {
                tempFlickData.flickAngleRemainder = 0.0;
                //flickAngleRemainder = lsangle - (int)lsangle;
                //lsangle = (int)lsangle;
                mouseDeltaX += lsangle * settings.realWorldCalibration;
            }
            else
            {
                tempFlickData.flickAngleRemainder = lsangle;
            }
        }

        private static double HandleFlickStickAngle(double elapsedTime, byte stickX, byte stickY, byte prevStickX, byte prevStickY,
            FlickStickMappingData flickData, FlickStickSettings flickSettings)
        {
            double result = 0.0;

            double lastXMax = prevStickX >= 128 ? 127.0 : -128.0;
            double lastTestX = (prevStickX - 128) / lastXMax;
            double lastYMax = prevStickY >= 128 ? 127.0 : -128.0;
            double lastTestY = (prevStickY - 128) / lastYMax;

            double currentXMax = stickX >= 128 ? 127.0 : -128.0;
            double currentTestX = (stickX - 128) / currentXMax;
            double currentYMax = stickY >= 128 ? 127.0 : -128.0;
            double currentTestY = (stickY - 128) / currentYMax;

            double lastLength = (lastTestX * lastTestX) + (lastTestY * lastTestY);
            double length = (currentTestX * currentTestX) + (currentTestY * currentTestY);
            double testLength = flickSettings.flickThreshold * flickSettings.flickThreshold;

            if (length >= testLength)
            {
                if (lastLength < testLength)
                {
                    // Start new Flick
                    flickData.flickProgress = 0.0; // Reset Flick progress
                    flickData.flickSize = Math.Atan2((stickX - 128), -(stickY - 128));
                    //flickData.flickFilter.Filter(0.0, cState.elapsedTime);
                }
                else
                {
                    // Turn camera
                    double stickAngle = Math.Atan2((stickX - 128), -(stickY - 128));
                    double lastStickAngle = Math.Atan2((prevStickX - 128), -(prevStickY - 128));
                    double angleChange = (stickAngle - lastStickAngle);
                    double rawAngleChange = angleChange;
                    angleChange = (angleChange + Math.PI) % (2 * Math.PI);
                    if (angleChange < 0)
                    {
                        angleChange += 2 * Math.PI;
                    }
                    angleChange -= Math.PI;
                    //Console.WriteLine("ANGLE CHANGE: {0} {1} {2}", stickAngle, lastStickAngle, rawAngleChange);
                    //Console.WriteLine("{0} {1} | {2} {3}", cState.RX, pState.RX, cState.RY, pState.RY);
                    //angleChange = flickData.flickFilter.Filter(angleChange, cState.elapsedTime);
                    result += angleChange;
                }
            }
            else
            {
                // Cleanup
                //flickData.flickFilter.Filter(0.0, cState.elapsedTime);
                result = 0.0;
            }

            // Continue Flick motion
            double lastFlickProgress = flickData.flickProgress;
            double flickTime = flickSettings.flickTime;
            if (lastFlickProgress < flickTime)
            {
                flickData.flickProgress = Math.Min(flickData.flickProgress + elapsedTime, flickTime);

                double lastPerOne = lastFlickProgress / flickTime;
                double thisPerOne = flickData.flickProgress / flickTime;

                double warpedLastPerOne = WarpEaseOut(lastPerOne);
                double warpedThisPerone = WarpEaseOut(thisPerOne);
                //Console.WriteLine("{0} {1}", warpedThisPerone, warpedLastPerOne);

                result += (warpedThisPerone - warpedLastPerOne) * flickData.flickSize;
            }

            return result;
        }

        private static double WarpEaseOut(double input)
        {
            double flipped = 1.0 - input;
            return 1.0 - flipped * flipped;
        }
    }
}
