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

#nullable enable

using System;
using DS4Windows.Core.Abstractions;
using DS4Windows.Core.Model;
using DS4Windows.Core.Settings;
using DS4Windows.Core.Utilities;
using DS4Windows.Input;
using DS4Windows.Processing.Touchpad;

namespace DS4Windows.Processing.Gyro
{
    /// <summary>
    /// ジャイロの動きの出力（Controls のトリガー判定、マウス、マウススティック）と、そのトリガーの切り替えの状態を受け持つ。
    /// 動きのイベントごとに呼ばれる（毎フレームの経路）。<see cref="TouchpadMouse"/> が、スロットごとに 1 つ持つ。
    /// カーソル（<see cref="MouseCursor"/>）は、タッチパッドのマウスと共有するため、持ち主から受け取る。
    /// </summary>
    public sealed class GyroOutputProcessor
    {
        private readonly int deviceNum;
        private readonly InputDeviceCore _device;
        private readonly MouseCursor _cursor;
        private readonly IProfileSettingsService _profileSettings;
        private readonly StickMotionState _stickMotion;
        private ControllerState s = new ControllerState();

        public GyroOutputProcessor(int deviceNum, InputDeviceCore device, MouseCursor cursor,
            IProfileSettingsService profileSettings, StickMotionState stickMotion)
        {
            this.deviceNum = deviceNum;
            _device = device;
            ArgumentNullException.ThrowIfNull(cursor);
            _cursor = cursor;
            ArgumentNullException.ThrowIfNull(profileSettings);
            _profileSettings = profileSettings;
            ArgumentNullException.ThrowIfNull(stickMotion);
            _stickMotion = stickMotion;

            filterPair.axis1Filter.MinCutoff = filterPair.axis2Filter.MinCutoff = GyroMouseStickInfo.DEFAULT_MINCUTOFF;
            filterPair.axis1Filter.Beta = filterPair.axis2Filter.Beta = GyroMouseStickInfo.DEFAULT_BETA;
        }

        /// <summary>トリガーの判定が読むボタンの状態（動きのイベントのたびに、機器の現在の状態へ差し替える）。</summary>
        public ControllerState State
        {
            get => s;
            set => s = value;
        }

        /// <summary>プロファイルの切り替え・接続の入れ替えのときに、トグルの状態を初期化する。</summary>
        public void ResetToggleGyroModes()
        {
            currentToggleGyroControls = false;
            currentToggleGyroMouse = false;
            currentToggleGyroStick = false;

            previousTriggerActivated = false;
            triggeractivated = false;
        }

        /// <summary>プロファイルの適用後の準備（マウススティックのフィルターの設定の結び）。</summary>
        public void PostSetup()
        {
            GyroOutMode gyroOutMode = _profileSettings.ForSlot(deviceNum).Gyro.OutputMode;
            if (gyroOutMode == GyroOutMode.MouseJoystick)
            {
                GyroMouseStickInfo msinfo = _profileSettings.ForSlot(deviceNum).Gyro.MouseStick;
                msinfo.RemoveRefreshEvents();
                msinfo.SetRefreshEvents(filterPair.axis1Filter);
                msinfo.SetRefreshEvents(filterPair.axis2Filter);
            }
        }

        /// <summary>切断・プロファイルの切り替えのときの後始末（トグルの初期化と、マウススティックのフィルターの切り離し）。</summary>
        public void Reset()
        {
            ResetToggleGyroModes();
            GyroOutMode gyroOutMode = _profileSettings.ForSlot(deviceNum).Gyro.OutputMode;
            if (gyroOutMode == GyroOutMode.MouseJoystick)
            {
                _profileSettings.ForSlot(deviceNum).Gyro.MouseStick.RemoveRefreshEvents();
            }
        }

        bool triggeractivated = false;
        bool previousTriggerActivated = false;
        bool useReverseRatchet = false;

        private bool toggleGyroControls = true;
        public bool ToggleGyroControls
        {
            get => toggleGyroControls;
            set
            {
                toggleGyroControls = value;
                ResetToggleGyroModes();
            }
        }

        private bool toggleGyroMouse = true;
        public bool ToggleGyroMouse
        {
            get => toggleGyroMouse;
            set
            {
                toggleGyroMouse = value;
                ResetToggleGyroModes();
            }
        }

        private bool toggleGyroStick = true;
        public bool ToggleGyroStick
        {
            get => toggleGyroStick;
            set
            {
                toggleGyroStick = value;
                ResetToggleGyroModes();
            }
        }

#pragma warning disable CS0414 // toggle flags may be assigned/used in runtime branches
        bool currentToggleGyroControls = false;
        bool currentToggleGyroMouse = false;
        bool currentToggleGyroStick = false;
#pragma warning restore CS0414

        public void SixaxisMoved(SixAxisEventArgs arg)
        {
            GyroOutMode outMode = _profileSettings.ForSlot(deviceNum).Gyro.OutputMode;
            if (outMode == GyroOutMode.Controls)
            {
                s = _device.GetCurrentStateRef();

                var triggerActive = IsTriggerActive(outMode);

                if (useReverseRatchet && triggerActive)
                {
                    s.Motion.outputGyroControls = true;
                }
                else if (!useReverseRatchet && !triggerActive)
                {
                    s.Motion.outputGyroControls = true;
                }
                else
                {
                    s.Motion.outputGyroControls = false;
                }
            }
            else if (outMode == GyroOutMode.Mouse && _profileSettings.ForSlot(deviceNum).Gyro.Sensitivity > 0)
            {
                s = _device.GetCurrentStateRef();

                var triggerActive = IsTriggerActive(outMode);
                if (useReverseRatchet && triggerActive)
                    _cursor.sixaxisMoved(arg);
                else if (!useReverseRatchet && !triggerActive)
                    _cursor.sixaxisMoved(arg);
                else
                    _cursor.MouseRemainderReset(arg);

            }
            else if (outMode == GyroOutMode.MouseJoystick)
            {
                s = _device.GetCurrentStateRef();
                _stickMotion.GyroStickX[deviceNum] = _stickMotion.GyroStickX[deviceNum] = 128;

                var triggerActive = IsTriggerActive(outMode);

                if (useReverseRatchet && triggerActive)
                    SixMouseStick(arg);
                else if (!useReverseRatchet && !triggerActive)
                    SixMouseStick(arg);
                else
                    SixMouseReset(arg);
            }
        }

        /// <summary>
        /// トリガーの設定の文字列（「4,5」のような、コンマ区切りのボタンの番号）を、数の配列へ直したもの。
        /// 判定は毎フレーム行われるため、同じ文字列のあいだは直した結果を使い回し、Split と TryParse の割り当てを避ける。
        /// 数でない要素は、判定では「押されていない」として扱う（Valid が偽）。
        /// </summary>
        private sealed class ParsedTriggerList
        {
            public readonly string Source;
            public readonly bool IsEmpty;
            public readonly int[] Values;
            public readonly bool[] Valid;

            public ParsedTriggerList(string source)
            {
                Source = source;
                string[] parts = (source ?? string.Empty).Split(',');
                IsEmpty = string.IsNullOrEmpty(parts[0]);
                Values = new int[parts.Length];
                Valid = new bool[parts.Length];
                for (int k = 0; k < parts.Length; k++)
                {
                    Valid[k] = int.TryParse(parts[k], out Values[k]);
                }
            }
        }

        // ジャイロの出力の種類（Controls／Mouse／MouseJoystick）ごとの、直した結果。入力のスレッドだけが読み書きする
        private readonly ParsedTriggerList[] gyroTriggerCache = new ParsedTriggerList[3];

        private ParsedTriggerList GetParsedTriggers(int cacheIndex, string source)
        {
            ParsedTriggerList cached = gyroTriggerCache[cacheIndex];
            if (cached == null || !string.Equals(cached.Source, source))
            {
                cached = new ParsedTriggerList(source);
                gyroTriggerCache[cacheIndex] = cached;
            }

            return cached;
        }

        public bool IsTriggerActive(GyroOutMode mode)
        {
            ParsedTriggerList? triggers = null;
            var andCond = false;
            useReverseRatchet = _profileSettings.ForSlot(deviceNum).Gyro.TriggerTurns;
            if (mode == GyroOutMode.Controls)
            {
                GyroControlsInfo controlsMapInfo = _profileSettings.ForSlot(deviceNum).Gyro.Controls;
                triggers = GetParsedTriggers(0, controlsMapInfo.triggers);
                andCond = controlsMapInfo.triggerCond;
            }
            else if (mode == GyroOutMode.Mouse)
            {
                triggers = GetParsedTriggers(1, _profileSettings.ForSlot(deviceNum).Gyro.MouseTriggers);
                andCond = _profileSettings.ForSlot(deviceNum).Gyro.MouseTriggerCondAnd;
            }
            else if (mode == GyroOutMode.MouseJoystick)
            {
                useReverseRatchet = _profileSettings.ForSlot(deviceNum).Gyro.MouseStickTriggerTurns;
                triggers = GetParsedTriggers(2, _profileSettings.ForSlot(deviceNum).Gyro.MouseStickTriggers);
                andCond = _profileSettings.ForSlot(deviceNum).Gyro.MouseStickTriggerCondAnd;
            }
            triggeractivated = andCond;
            if (triggers != null && !triggers.IsEmpty)
            {
                for (int index = 0, arlen = triggers.Values.Length; index < arlen; index++)
                {
                    bool pressed = triggers.Valid[index] && TriggerButtonReader.IsPressed(s, triggers.Values[index]);
                    if (andCond && !pressed)
                    {
                        triggeractivated = false;
                        break;
                    }
                    else if (!andCond && pressed)
                    {
                        triggeractivated = true;
                        break;
                    }
                }
            }
            if (toggleGyroControls)
            {
                if (triggeractivated && triggeractivated != previousTriggerActivated)
                {
                    currentToggleGyroControls = !currentToggleGyroControls;
                }

                previousTriggerActivated = triggeractivated;
                triggeractivated = currentToggleGyroControls;
            }
            else
            {
                previousTriggerActivated = triggeractivated;
            }

            return triggeractivated;
        }

        private OneEuroFilterPair filterPair = new OneEuroFilterPair();

        public void ReplaceOneEuroFilterPair()
        {
            _profileSettings.ForSlot(deviceNum).Gyro.MouseStick.RemoveRefreshEvents();
            //filterPair = new OneEuroFilterPair();
            // No longer need to make new instances. Just reset filters
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

        private const int SMOOTH_BUFFER_LEN = 3;
        private int[] xSmoothBuffer = new int[SMOOTH_BUFFER_LEN];
        private int[] ySmoothBuffer = new int[SMOOTH_BUFFER_LEN];
        private int smoothBufferTail = 0;

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
            // Phase7b W2（K4-1）: 旧は GetGyroMouseStickHorizontalAxis(0) の固定で、2 台目以降も 1 台目のプロファイルの水平軸で動いていた
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

            PostMapStickData tempMapStickData = _stickMotion.MapStickActionData[deviceNum];
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

                _stickMotion.GyroStickX[deviceNum] = axisXOut;
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

                _stickMotion.GyroStickY[deviceNum] = axisYOut;
            }
        }
    }
}
