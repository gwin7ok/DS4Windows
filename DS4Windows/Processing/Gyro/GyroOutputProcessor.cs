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
    public sealed partial class GyroOutputProcessor
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
                    _cursor.SixaxisMoved(arg);
                else if (!useReverseRatchet && !triggerActive)
                    _cursor.SixaxisMoved(arg);
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

        // ジャイロの出力の種類（Controls／Mouse／MouseJoystick）ごとの、直した結果。入力のスレッドだけが読み書きする
        private readonly ParsedTriggerList[] gyroTriggerCache = new ParsedTriggerList[3];

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
    }
}
