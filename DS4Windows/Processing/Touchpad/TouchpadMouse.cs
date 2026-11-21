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

//using System.Diagnostics;

using DS4Windows.Processing.Synthetic;
using System;
using DS4Windows.Core.Abstractions;
using DS4Windows.Core.Logging;
using DS4Windows.Core.Model;
using DS4Windows.Core.Settings;
using DS4Windows.Input;
using DS4Windows.Processing.Abstractions;
using DS4Windows.Processing.Gyro;

namespace DS4Windows.Processing.Touchpad
{
    public partial class TouchpadMouse : ITouchPadGyroToggles
    {
        private Touch firstTouch;
        private ControllerState s = new ControllerState();
        private int deviceNum;
        private InputDeviceCore? dev = null;
        private readonly MouseCursor cursor;
        private readonly MouseWheel wheel;
        private readonly GyroOutputProcessor _gyro;
        // Global 直参照を廃し、コンストラクタ注入（Pure DI、フォールバックなし）で受け取る
        private readonly IProfileSettingsService _profileSettings;
        private readonly IVirtualKBM _virtualKBM;
        // 旧 StickMotionState.Shared／Mapping.MapClick（静的な窓口）。コンストラクターで受ける。
        private readonly StickMotionState _stickMotion;
        private readonly SyntheticInputState _syntheticInput;
        private readonly TapClickDetector _tap;
        /// <summary>スワイプ・スライドと、前回のタッチ領域の状態（読み取りは外へ公開。書き込みはこの型だけ）。</summary>
        public TouchpadGestureState Gestures { get; } = new TouchpadGestureState();
        // touch area stuff
        public bool leftDown => _buttons.LeftDown;
        public bool rightDown => _buttons.RightDown;
        public bool upperDown => _buttons.UpperDown;
        public bool multiDown => _buttons.MultiDown;
        private bool touchStarted = false;
        private bool touchEnded = false;
        public int CursorGyroDead { get => cursor.GyroCursorDeadZone; set => cursor.GyroCursorDeadZone = value; }

        private readonly TouchMouseTrackball _trackball = new TouchMouseTrackball();

        // 旧ネスト型 Mouse.TouchButtonActivationMode は、プロファイルの設定値の型として
        // Core/Settings/TouchButtonActivationMode.cs（トップレベル）へ移した（値と基底型は同じ）

        private readonly TouchButtonDetector _buttons;

        /// <summary>
        /// 持ち物（タップ・カーソル・ホイール・ジャイロ）は、<c>TouchpadMouseFactory</c> が組み立てて渡す
        /// （デバイスの設定の読み書きや時刻の供給元を、この型が知らなくてよいようにする）。
        /// </summary>
        public TouchpadMouse(int deviceID, InputDeviceCore d, IProfileSettingsService profileSettings, IVirtualKBM virtualKBM,
            StickMotionState stickMotion, SyntheticInputState syntheticInput, TapClickDetector tap, MouseCursor mouseCursor,
            MouseWheel mouseWheel, GyroOutputProcessor gyro, TouchButtonDetector buttons)
        {
            ArgumentNullException.ThrowIfNull(buttons);
            _buttons = buttons;
            ArgumentNullException.ThrowIfNull(stickMotion);
            ArgumentNullException.ThrowIfNull(syntheticInput);
            ArgumentNullException.ThrowIfNull(profileSettings);
            ArgumentNullException.ThrowIfNull(virtualKBM);
            ArgumentNullException.ThrowIfNull(tap);
            ArgumentNullException.ThrowIfNull(mouseCursor);
            ArgumentNullException.ThrowIfNull(mouseWheel);
            ArgumentNullException.ThrowIfNull(gyro);
            _stickMotion = stickMotion;
            _syntheticInput = syntheticInput;
            _profileSettings = profileSettings;
            _virtualKBM = virtualKBM;
            _tap = tap;
            cursor = mouseCursor;
            wheel = mouseWheel;
            _gyro = gyro;
            deviceNum = deviceID;
            dev = d;
            firstTouch = new Touch(0, 0, 0, null);
        }

        public void ResetTrackAccel(double friction)
        {
            _trackball.ResetAccel(friction);
        }

        public void ResetToggleGyroModes() => _gyro.ResetToggleGyroModes();

        public bool ToggleGyroControls
        {
            get => _gyro.ToggleGyroControls;
            set => _gyro.ToggleGyroControls = value;
        }

        public bool ToggleGyroMouse
        {
            get => _gyro.ToggleGyroMouse;
            set => _gyro.ToggleGyroMouse = value;
        }

        public bool ToggleGyroStick
        {
            get => _gyro.ToggleGyroStick;
            set => _gyro.ToggleGyroStick = value;
        }

        public MouseCursor Cursor => cursor;

        public bool TouchStarted
        {
            get
            {
                var temp = touchStarted;
                touchStarted = false;
                return temp;
            }
            private set => touchStarted = value;
        }
        public bool TouchEnded
        {
            get
            {
                var temp = touchEnded;
                touchEnded = false;
                return temp;
            }
            private set => touchEnded = value;
        }


        public void SixaxisMoved(DS4SixAxis sender, SixAxisEventArgs arg)
        {
            _gyro.SixaxisMoved(arg);
        }

        public bool IsGyroTriggerActive(GyroOutMode mode, ControllerState state) => _gyro.IsTriggerActive(mode, state);

        public void PostSetup()
        {
            _gyro.PostSetup();
        }

        public void Reset()
        {
            TouchpadOutMode tempMode = _profileSettings.ForSlot(deviceNum).Touchpad.OutputMode;
            if (tempMode == TouchpadOutMode.Mouse)
            {
                _trackball.Reset();
            }

            _gyro.Reset();
        }

        private bool GetDS4ControlsByName(int key) => TriggerButtonReader.IsPressed(s, key);

        private bool tempBool = false;

        // 設定の「反転を無効にするトリガー」が、すべて押されているか（空なら真）
        private bool AreDisInvertTriggersHeld()
        {
            int[] disArray = _profileSettings.ForSlot(deviceNum).Touchpad.DisInvertTriggers;
            for (int i = 0, arlen = disArray.Length; i < arlen; i++)
            {
                if (GetDS4ControlsByName(disArray[i]) == false)
                    return false;
            }

            return true;
        }
        // 出力の種類ごとの、動いたときの処理（タッチ → マウス／Controls のスワイプ／スティック）
        private void MouseModeMoved(TouchpadEventArgs arg)
        {
            TouchpadSlotSettings touchpad = _profileSettings.ForSlot(deviceNum).Touchpad;
                if (_profileSettings.GetTouchpadActive(deviceNum))
                {
                    tempBool = AreDisInvertTriggersHeld();

                    if (touchpad.Trackball)
                    {
                        _trackball.AddSample(arg.Touches![0].DeltaX, arg.Touches[0].DeltaY); // タッチパッドのイベントは、接触の配列を持って来る（null なら従来どおり NullReferenceException）
                    }

                    cursor.TouchesMoved(arg, dragging || dragging2, tempBool);
                    wheel.TouchesMoved(arg, dragging || dragging2);
                }
                else
                {
                    if (touchpad.Trackball)
                    {
                        _trackball.AddEmptySample();
                    }
                }
        }

        public void TouchUnchanged(DS4Touchpad sender, EventArgs unused)
        {
            //Trace.WriteLine("TOUCHES UNCHANGED");

            s = dev!.GetCurrentStateRef(); // 接続中の機器から呼ばれる（null なら従来どおり NullReferenceException）
            if (s.Touch1Finger)
            {
                _buttons.WasTouched = false;
            }

            TouchpadOutMode touchMode = _profileSettings.ForSlot(deviceNum).Touchpad.OutputMode;
            if (touchMode == TouchpadOutMode.Mouse)
            {
                if (_trackball.Active)
                {
                    //if (touchMode == TouchpadOutMode.Mouse ||
                    //    touchMode == TouchpadOutMode.MouseJoystick)
                    if (touchMode == TouchpadOutMode.Mouse)
                    {
                        tempBool = AreDisInvertTriggersHeld();

                        if (_trackball.Step(s.elapsedTime, out int dx, out int dy))
                        {
                            cursor.TouchMoveCursor(dx, dy, tempBool);
                        }
                    }
                }
            }

            // Release 方式の押下は、一定時間（TouchButtonDetector.ReleaseHoldMs）保持してから下ろす
            bool releaseButtonChanged = _buttons.TryExpireRelease();

            // Double Tap の保留クリックと、タップクリックの保持中は、
            // 指を離した後も処理が必要
            if (s.Touch1Finger || s.TouchButton ||
                _buttons.ReleaseButtonActive || releaseButtonChanged ||
                _tap.IsPendingOrHolding)
            {
                synthesizeMouseButtons();
            }
        }

        public bool dragging, dragging2;

        private void synthesizeMouseButtons()
        {
            TouchpadSlotSettings touchpad = _profileSettings.ForSlot(deviceNum).Touchpad;
            TouchpadOutMode tempMode = touchpad.OutputMode;
            if (tempMode != TouchpadOutMode.Passthru)
            {
                bool touchClickPass = touchpad.ClickPassthru;
                if (!touchClickPass)
                {
                    // Reset output Touchpad click button
                    s.OutputTouchButton = false;
                }
            }
            else
            {
                // Don't allow virtual buttons for Passthru mode
                return;
            }

            // 実在するコントロール（DS4Controls.TouchLeft）の設定は、必ずある
            if (_profileSettings.GetDS4CSetting(deviceNum, DS4Controls.TouchLeft)!.IsDefault &&
                leftDown)
            {
                _syntheticInput.CountClick(deviceNum, Click.Left);
                dragging2 = true;
            }
            else
            {
                dragging2 = false;
            }

            // 実在するコントロール（DS4Controls.TouchUpper）の設定は、必ずある
            if (_profileSettings.GetDS4CSetting(deviceNum, DS4Controls.TouchUpper)!.IsDefault &&
                upperDown)
            {
                _syntheticInput.CountClick(deviceNum, Click.Middle);
            }

            // 実在するコントロール（DS4Controls.TouchRight）の設定は、必ずある
            if (_profileSettings.GetDS4CSetting(deviceNum, DS4Controls.TouchRight)!.IsDefault &&
                rightDown)
            {
                _syntheticInput.CountClick(deviceNum, Click.Left);
            }

            // 実在するコントロール（DS4Controls.TouchMulti）の設定は、必ずある
            if (_profileSettings.GetDS4CSetting(deviceNum, DS4Controls.TouchMulti)!.IsDefault &&
                multiDown)
            {
                _syntheticInput.CountClick(deviceNum, Click.Right);
            }

            if (touchpad.OutputMode == TouchpadOutMode.Mouse)
            {
                _tap.CountPendingClicks(_syntheticInput, touchpad.TapSensitivity);

                if (_tap.SecondTouchBegan) //if tap and hold (also works as double tap)
                {
                    _syntheticInput.CountClick(deviceNum, Click.Left);
                    dragging = true;
                }
                else
                {
                    dragging = false;
                }
            }
        }

        public void TouchButtonUp(DS4Touchpad sender, TouchpadEventArgs arg)
        {
            TouchpadSlotSettings touchpad = _profileSettings.ForSlot(deviceNum).Touchpad;
            _buttons.ClearDownFlagsKeepCandidate();
            _buttons.WasTouchButtonClicked = true;

            s = dev!.GetCurrentStateRef(); // 接続中の機器から呼ばれる（null なら従来どおり NullReferenceException）
            TouchButtonActivationMode touchButtonMode = touchpad.ButtonMode;
            bool checkedOnUp = s.Touch1 || s.Touch2 ||
                touchButtonMode == TouchButtonActivationMode.Release;
            if (checkedOnUp)
            {
                _buttons.Check(arg, touchpad.ButtonMode);
                synthesizeMouseButtons();
            }

            // 上部タッチ（タッチパッドの上端の押し込み）が、方式によって実行されない件の診断（KI-17）
            if (AppLogger.IsTraceEnabled) AppLogger.LogTrace($"[TOUCHBTN] device={deviceNum} up mode={touchButtonMode} touches={arg.Touches?.Length} active={arg.TouchActive} pressed={arg.TouchButtonPressed} checked={checkedOnUp} L={leftDown} R={rightDown} U={upperDown} M={multiDown}");
        }

        public void TouchButtonDown(DS4Touchpad sender, TouchpadEventArgs arg)
        {
            TouchpadSlotSettings touchpad = _profileSettings.ForSlot(deviceNum).Touchpad;
            if (arg.Touches != null &&
                arg.Touches.Length == 1 &&
                (touchpad.LowerRightAsRightClick && arg.Touches[0].HwX > (1920 * 3) / 4 && arg.Touches[0].HwY > (960 * 3) / 4))
            {
                _syntheticInput.CountClick(deviceNum, Click.Right);
            }
            else if (arg.Touches == null)
            {
                _buttons.ClearCandidate();
            }

            _buttons.WasTouchButtonClicked = false;
            s = dev!.GetCurrentStateRef(); // 接続中の機器から呼ばれる（null なら従来どおり NullReferenceException）
            _buttons.Check(arg, touchpad.ButtonMode);
            synthesizeMouseButtons();

            if (AppLogger.IsTraceEnabled) AppLogger.LogTrace($"[TOUCHBTN] device={deviceNum} down mode={touchpad.ButtonMode} touches={arg.Touches?.Length} active={arg.TouchActive} pressed={arg.TouchButtonPressed} L={leftDown} R={rightDown} U={upperDown} M={multiDown}");
        }

        public void PopulatePriorButtonStates()
        {
            Gestures.SavePrior(upperDown, leftDown, rightDown, multiDown);
        }

        public ControllerState GetDS4State()
        {
            return s;
        }
    }
}