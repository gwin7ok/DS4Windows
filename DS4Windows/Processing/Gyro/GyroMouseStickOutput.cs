#nullable enable

using System;
using DS4Windows.Core.Abstractions;
using DS4Windows.Core.Model;
using DS4Windows.Core.Settings;
using DS4Windows.Core.Utilities;
using DS4Windows.Input;

namespace DS4Windows.Processing.Gyro
{
    /// <summary>
    /// ジャイロの出力モード MouseJoystick: ジャイロの動きを、スティックの値（<see cref="StickMotionState"/>）へ変える。
    /// 平滑化のバッファーと、1 ユーロフィルターの対を持つ（このモードだけが使う状態）。
    /// </summary>
    internal sealed class GyroMouseStickOutput : IGyroModeOutput
    {
        private const int SMOOTH_BUFFER_LEN = 3;
        private readonly int deviceNum;
        private readonly IProfileSettingsService _profileSettings;
        private readonly StickMotionState _stickMotion;
        private readonly OneEuroFilterPair filterPair = new OneEuroFilterPair();
        private int[] xSmoothBuffer = new int[SMOOTH_BUFFER_LEN];
        private int[] ySmoothBuffer = new int[SMOOTH_BUFFER_LEN];
        private int smoothBufferTail = 0;

        public GyroMouseStickOutput(int deviceNum, IProfileSettingsService profileSettings, StickMotionState stickMotion)
        {
            ArgumentNullException.ThrowIfNull(profileSettings);
            ArgumentNullException.ThrowIfNull(stickMotion);
            this.deviceNum = deviceNum;
            _profileSettings = profileSettings;
            _stickMotion = stickMotion;
            filterPair.axis1Filter.MinCutoff = filterPair.axis2Filter.MinCutoff = GyroMouseStickInfo.DEFAULT_MINCUTOFF;
            filterPair.axis1Filter.Beta = filterPair.axis2Filter.Beta = GyroMouseStickInfo.DEFAULT_BETA;
        }

        public bool IsEnabled => true;

        public void Process(SixAxisEventArgs arg, ControllerState state, bool shouldOutput)
        {
            // 入口で、X と Y の両方を中立へ戻す（動かさない枝や、軸が出力されない設定でも、前の値を残さない）
            _stickMotion.Devices[deviceNum].GyroStickX = _stickMotion.Devices[deviceNum].GyroStickY = 128;
            if (shouldOutput)
                SixMouseStick(arg);
            else
                SixMouseReset(arg);
        }

        /// <summary>プロファイルの適用後の準備（マウススティックの設定の更新のイベントを、フィルターへ結ぶ）。</summary>
        public void PostSetup()
        {
            GyroMouseStickInfo msinfo = _profileSettings.ForSlot(deviceNum).Gyro.MouseStick;
            msinfo.RemoveRefreshEvents();
            msinfo.SetRefreshEvents(filterPair.axis1Filter);
            msinfo.SetRefreshEvents(filterPair.axis2Filter);
        }

        /// <summary>切断・プロファイルの切り替えのときの後始末（フィルターの切り離し）。</summary>
        public void Detach()
        {
            _profileSettings.ForSlot(deviceNum).Gyro.MouseStick.RemoveRefreshEvents();
        }

        public void ReplaceOneEuroFilterPair()
        {
            _profileSettings.ForSlot(deviceNum).Gyro.MouseStick.RemoveRefreshEvents();
            // 新しい実体は作らず、フィルターの状態だけを初期化する
            filterPair.axis1Filter.Reset();
            filterPair.axis2Filter.Reset();
        }

        public void SetupLateOneEuroFilters()
        {
            filterPair.axis1Filter.MinCutoff = filterPair.axis2Filter.MinCutoff = _profileSettings.ForSlot(deviceNum).Gyro.MouseStick.MinCutoff;
            filterPair.axis1Filter.Beta = filterPair.axis2Filter.Beta = _profileSettings.ForSlot(deviceNum).Gyro.MouseStick.Beta;
            _profileSettings.ForSlot(deviceNum).Gyro.MouseStick.SetRefreshEvents(filterPair.axis1Filter);
            _profileSettings.ForSlot(deviceNum).Gyro.MouseStick.SetRefreshEvents(filterPair.axis2Filter);
        }

        private void SixMouseReset(SixAxisEventArgs args)
        {
            int iIndex = smoothBufferTail % SMOOTH_BUFFER_LEN;
            xSmoothBuffer[iIndex] = 0;
            ySmoothBuffer[iIndex] = 0;
            smoothBufferTail = iIndex + 1;

            GyroMouseStickInfo msinfo = _profileSettings.ForSlot(deviceNum).Gyro.MouseStick;
            if (msinfo.smoothingMethod == GyroSmoothingMethod.OneEuro)
            {
                double currentRate = 1.0 / args.sixAxis!.elapsed; // ジャイロのイベントは、6 軸の値を持って来る（null なら従来どおり NullReferenceException）
                filterPair.axis1Filter.Filter(0.0, currentRate);
                filterPair.axis2Filter.Filter(0.0, currentRate);
            }
        }

        private void SixMouseStick(SixAxisEventArgs arg)
        {
            int deltaX = 0, deltaY = 0;
            // 旧は GetGyroMouseStickHorizontalAxis(0) の固定で、2 台目以降も 1 台目のプロファイルの水平軸で動いていた
            // （上流の元からの不具合。1 台だけのときは挙動が変わらない）。この Mouse が受け持つ台の設定を読む。
            deltaX = _profileSettings.ForSlot(deviceNum).Gyro.MouseStickHorizontalAxis == 0 ? arg.sixAxis!.gyroYawFull : // ジャイロのイベントは、6 軸の値を持って来る（null なら従来どおり NullReferenceException）
                arg.sixAxis!.gyroRollFull; // ジャイロのイベントは、6 軸の値を持って来る（null なら従来どおり NullReferenceException）
            deltaY = -arg.sixAxis.gyroPitchFull;
            //int inputX = deltaX, inputY = deltaY;
            int maxDirX = deltaX >= 0 ? 127 : -128;
            int maxDirY = deltaY >= 0 ? 127 : -128;

            GyroMouseStickInfo msinfo = _profileSettings.ForSlot(deviceNum).Gyro.MouseStick;

            double tempDouble = arg.sixAxis.elapsed * 250.0; // Base default speed on 4 ms
            double tempAngle = Math.Atan2(-deltaY, deltaX);
            double normX = Math.Abs(Math.Cos(tempAngle));
            double normY = Math.Abs(Math.Sin(tempAngle));
            int signX = Math.Sign(deltaX);
            int signY = Math.Sign(deltaY);

            int deadzoneX = (int)Math.Abs(normX * msinfo.deadZone);
            int deadzoneY = (int)Math.Abs(normY * msinfo.deadZone);

            int maxValX = signX * msinfo.maxZone;
            int maxValY = signY * msinfo.maxZone;

            double xratio = 0.0, yratio = 0.0;
            double antiX = msinfo.antiDeadX * normX;
            double antiY = msinfo.antiDeadY * normY;

            if (Math.Abs(deltaX) > deadzoneX)
            {
                deltaX -= signX * deadzoneX;
                //deltaX = (int)(deltaX * tempDouble);
                deltaX = (deltaX < 0 && deltaX < maxValX) ? maxValX :
                    (deltaX > 0 && deltaX > maxValX) ? maxValX : deltaX;
                //if (deltaX != maxValX) deltaX -= deltaX % (signX * GyroMouseFuzz);
            }
            else
            {
                deltaX = 0;
            }

            if (Math.Abs(deltaY) > deadzoneY)
            {
                deltaY -= signY * deadzoneY;
                //deltaY = (int)(deltaY * tempDouble);
                deltaY = (deltaY < 0 && deltaY < maxValY) ? maxValY :
                    (deltaY > 0 && deltaY > maxValY) ? maxValY : deltaY;
                //if (deltaY != maxValY) deltaY -= deltaY % (signY * GyroMouseFuzz);
            }
            else
            {
                deltaY = 0;
            }

            if (msinfo.jitterCompensation)
            {
                // Possibly expose threshold later
                const double threshold = 2;
                const float thresholdF = (float)threshold;

                double absX = Math.Abs(deltaX);
                if (absX <= normX * threshold)
                {
                    deltaX = (int)(signX * Math.Pow(absX / thresholdF, 1.408) * threshold);
                }

                double absY = Math.Abs(deltaY);
                if (absY <= normY * threshold)
                {
                    deltaY = (int)(signY * Math.Pow(absY / thresholdF, 1.408) * threshold);
                }
            }

            if (msinfo.useSmoothing)
            {
                if (msinfo.smoothingMethod == GyroSmoothingMethod.OneEuro)
                {
                    double currentRate = 1.0 / arg.sixAxis.elapsed;
                    deltaX = (int)(filterPair.axis1Filter.Filter(deltaX, currentRate));
                    deltaY = (int)(filterPair.axis2Filter.Filter(deltaY, currentRate));
                }
                else if (msinfo.smoothingMethod == GyroSmoothingMethod.WeightedAverage)
                {
                    int iIndex = smoothBufferTail % SMOOTH_BUFFER_LEN;
                    xSmoothBuffer[iIndex] = deltaX;
                    ySmoothBuffer[iIndex] = deltaY;
                    smoothBufferTail = iIndex + 1;

                    double currentWeight = 1.0;
                    double finalWeight = 0.0;
                    double x_out = 0.0, y_out = 0.0;
                    int idx = 0;
                    for (int i = 0; i < SMOOTH_BUFFER_LEN; i++)
                    {
                        idx = (smoothBufferTail - i - 1 + SMOOTH_BUFFER_LEN) % SMOOTH_BUFFER_LEN;
                        x_out += xSmoothBuffer[idx] * currentWeight;
                        y_out += ySmoothBuffer[idx] * currentWeight;
                        finalWeight += currentWeight;
                        currentWeight *= msinfo.smoothWeight;
                    }

                    x_out /= finalWeight;
                    deltaX = (int)x_out;
                    y_out /= finalWeight;
                    deltaY = (int)y_out;
                }

                maxValX = deltaX < 0 ? -msinfo.maxZone : msinfo.maxZone;
                maxValY = deltaY < 0 ? -msinfo.maxZone : msinfo.maxZone;
                maxDirX = deltaX >= 0 ? 127 : -128;
                maxDirY = deltaY >= 0 ? 127 : -128;
            }

            if (msinfo.vertScale != 100)
            {
                double verticalScale = msinfo.vertScale * 0.01;
                deltaY = (int)(deltaY * verticalScale);
                deltaY = (deltaY < 0 && deltaY < maxValY) ? maxValY :
                    (deltaY > 0 && deltaY > maxValY) ? maxValY : deltaY;
            }

            if (deltaX != 0) xratio = deltaX / (double)maxValX;
            if (deltaY != 0) yratio = deltaY / (double)maxValY;

            if (msinfo.maxOutputEnabled)
            {
                double maxOutRatio = msinfo.maxOutput / 100.0;
                // Expand output a bit. Likely not going to get a straight line with Gyro
                double maxOutXRatio = Math.Min(normX / 0.95, 1.0) * maxOutRatio;
                double maxOutYRatio = Math.Min(normY / 0.95, 1.0) * maxOutRatio;

                xratio = Math.Min(Math.Max(xratio, 0.0), maxOutXRatio);
                yratio = Math.Min(Math.Max(yratio, 0.0), maxOutYRatio);
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

            bool outputX = msinfo.OutputHorizontal();
            bool outputY = msinfo.OutputVertical();

            PostMapStickData tempMapStickData = _stickMotion.Devices[deviceNum].MapStickActionData;
            if (outputX)
            {
                if (msinfo.outputStick == GyroMouseStickInfo.OutputStick.LeftStick &&
                    Math.Abs(axisXOut - 128) > Math.Abs(tempMapStickData.LX - 128))
                {
                    tempMapStickData.LX = axisXOut;
                    tempMapStickData.dirty = true;
                }
                else if (msinfo.outputStick == GyroMouseStickInfo.OutputStick.RightStick &&
                    Math.Abs(axisXOut - 128) > Math.Abs(tempMapStickData.RX - 128))
                {
                    tempMapStickData.RX = axisXOut;
                    tempMapStickData.dirty = true;
                }

                _stickMotion.Devices[deviceNum].GyroStickX = axisXOut;
            }

            if (outputY)
            {
                if (msinfo.outputStick == GyroMouseStickInfo.OutputStick.LeftStick &&
                    Math.Abs(axisYOut - 128) > Math.Abs(tempMapStickData.LY - 128))
                {
                    tempMapStickData.LY = axisYOut;
                    tempMapStickData.dirty = true;
                }
                else if (msinfo.outputStick == GyroMouseStickInfo.OutputStick.RightStick &&
                    Math.Abs(axisYOut - 128) > Math.Abs(tempMapStickData.RY - 128))
                {
                    tempMapStickData.RY = axisYOut;
                    tempMapStickData.dirty = true;
                }

                _stickMotion.Devices[deviceNum].GyroStickY = axisYOut;
            }
        }
    }
}
