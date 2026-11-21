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
        private readonly IProfileSettingsService _profileSettings;
        private readonly GyroTriggerEvaluator _triggers;
        private readonly GyroControlsOutput _controls = new GyroControlsOutput();
        private readonly GyroMouseOutput _mouse;
        private readonly GyroMouseStickOutput _mouseStick;

        public GyroOutputProcessor(int deviceNum, InputDeviceCore device, MouseCursor cursor,
            IProfileSettingsService profileSettings, StickMotionState stickMotion)
        {
            this.deviceNum = deviceNum;
            _device = device;
            ArgumentNullException.ThrowIfNull(profileSettings);
            _profileSettings = profileSettings;
            _triggers = new GyroTriggerEvaluator(deviceNum, profileSettings);
            _mouse = new GyroMouseOutput(deviceNum, cursor, profileSettings);
            _mouseStick = new GyroMouseStickOutput(deviceNum, profileSettings, stickMotion);
        }

        /// <summary>プロファイルの切り替え・接続の入れ替えのときに、トグルの状態を初期化する。</summary>
        public void ResetToggleGyroModes()
        {
            _triggers.Reset();
        }

        /// <summary>プロファイルの適用後の準備（マウススティックのフィルターの設定の結び）。</summary>
        public void PostSetup()
        {
            if (_profileSettings.ForSlot(deviceNum).Gyro.OutputMode == GyroOutMode.MouseJoystick)
                _mouseStick.PostSetup();
        }

        /// <summary>切断・プロファイルの切り替えのときの後始末（トグルの初期化と、マウススティックのフィルターの切り離し）。</summary>
        public void Reset()
        {
            ResetToggleGyroModes();
            if (_profileSettings.ForSlot(deviceNum).Gyro.OutputMode == GyroOutMode.MouseJoystick)
                _mouseStick.Detach();
        }

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

        /// <summary>トリガーの判定（トグルを反映）。<paramref name="state"/> は、判定が読むボタンの状態。</summary>
        public bool IsTriggerActive(GyroOutMode mode, ControllerState state) => _triggers.Evaluate(mode, state, toggleGyroControls).Active;

        private IGyroModeOutput? SelectOutput(GyroOutMode mode) => mode switch
        {
            GyroOutMode.Controls => _controls,
            GyroOutMode.Mouse => _mouse,
            GyroOutMode.MouseJoystick => _mouseStick,
            _ => null,
        };

        public void SixaxisMoved(SixAxisEventArgs arg)
        {
            GyroOutMode outMode = _profileSettings.ForSlot(deviceNum).Gyro.OutputMode;
            IGyroModeOutput? output = SelectOutput(outMode);
            if (output == null || !output.IsEnabled) return;

            ControllerState state = _device.GetCurrentStateRef();
            output.Process(arg, state, _triggers.Evaluate(outMode, state, toggleGyroControls).ShouldOutput);
        }

        public void ReplaceOneEuroFilterPair() => _mouseStick.ReplaceOneEuroFilterPair();

        public void SetupLateOneEuroFilters() => _mouseStick.SetupLateOneEuroFilters();
    }
}
