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
using DS4Windows.Core.Utilities;
using DS4Windows.Input;
using DS4Windows.Processing.Abstractions;
using DS4Windows.Processing.Gyro;

namespace DS4Windows.Processing.Touchpad
{
    public class TouchpadMouse : ITouchpadBehaviour, ITouchPadGyroToggles
    {
        protected DateTime pastTime, firstTap, TimeofEnd;
        protected Touch firstTouch, secondTouch;
        private ControllerState s = new ControllerState();
        protected int deviceNum;
        private InputDeviceCore dev = null;
        private readonly MouseCursor cursor;
        private readonly MouseWheel wheel;
        private readonly GyroOutputProcessor _gyro;
        // Phase6-Step4-3: Global 直参照を廃し、コンストラクタ注入（Pure DI、フォールバックなし）で受け取る
        private readonly IProfileSettingsService _profileSettings;
        private readonly IVirtualKBM _virtualKBM;
        // Phase7-Step8 W1: 旧 StickMotionState.Shared／Mapping.MapClick（静的な窓口）。コンストラクターで受ける。
        private readonly StickMotionState _stickMotion;
        private readonly SyntheticInputState _syntheticInput;
        private bool tappedOnce = false, secondtouchbegin = false;
        // タップ判定で許容する始点と終点のずれ（タッチパッド座標。上流は 10）
        private const int TapMaxMoveHw = 100;
        // タップによる左クリックを押したままにする時間（ms）。
        // 上流は 1 フレームだけの瞬間的なクリックで、フォーカスの切り替えが効かない環境があったため保持する（Phase6 変更、ユーザー承認 2026-10-15）
        private const double TapClickHoldMs = 50.0;
        private DateTime tapClickHoldUntil = DateTime.MinValue;
        private readonly TimeProvider _time;

        private void StartTapClickHold()
        {
            tapClickHoldUntil = _time.GetUtcNow().UtcDateTime + TimeSpan.FromMilliseconds(TapClickHoldMs);
        }
        public bool swipeLeft, swipeRight, swipeUp, swipeDown;
        public bool priorSwipeLeft, priorSwipeRight, priorSwipeUp, priorSwipeDown;
        public byte swipeLeftB, swipeRightB, swipeUpB, swipeDownB, swipedB;
        public byte priorSwipeLeftB, priorSwipeRightB, priorSwipeUpB, priorSwipeDownB, priorSwipedB;
        public bool slideleft, slideright;
        public bool priorSlideLeft, priorSlideright;
        // touch area stuff
        public bool leftDown, rightDown, upperDown, multiDown;
        public bool priorLeftDown, priorRightDown, priorUpperDown, priorMultiDown;
        private bool touchStarted = false;
        private bool touchEnded = false;
        protected DS4Controls pushed = DS4Controls.None;
        protected Click clicked = Click.None;
        public int CursorGyroDead { get => cursor.GyroCursorDeadZone; set => cursor.GyroCursorDeadZone = value; }


        private readonly TouchMouseTrackball _trackball = new TouchMouseTrackball();

        private bool trackballTouchStickActive = false;


        // Phase14b-Step2（R-04）: 旧ネスト型 Mouse.TouchButtonActivationMode は、プロファイルの設定値の型として
        // Core/Settings/TouchButtonActivationMode.cs（トップレベル）へ移した（値と基底型は同じ）

        private enum TouchButtonModeCandidate : ushort
        {
            None,
            Left,
            Right,
            Multi,
            Upper,
        }

        private TouchButtonModeCandidate touchButtonCurrentCandidate;

        private bool wasTouched; // Needed to know when to check for Release activation
        private bool wasTouchButtonClicked = false; // Needed for Upper Touch on Release
        private bool releaseButtonActive; // Flag to now if Release button mode is active
        private DateTime onReleaseTime; // Time Release button mode was first activated

        private readonly TouchStickOutput _touchStick;
        private FakeTrackball touchStickTrackball;

        public TouchpadMouse(int deviceID, InputDeviceCore d, IProfileSettingsService profileSettings, IVirtualKBM virtualKBM,
            StickMotionState stickMotion, SyntheticInputState syntheticInput, TimeProvider timeProvider)
        {
            _time = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
            _stickMotion = stickMotion ?? throw new ArgumentNullException(nameof(stickMotion));
            _syntheticInput = syntheticInput ?? throw new ArgumentNullException(nameof(syntheticInput));
            _profileSettings = profileSettings ?? throw new ArgumentNullException(nameof(profileSettings));
            _virtualKBM = virtualKBM ?? throw new ArgumentNullException(nameof(virtualKBM));
            deviceNum = deviceID;
            dev = d;
            cursor = new MouseCursor(deviceNum, d.GyroMouseSensSettings, _profileSettings, _virtualKBM);
            wheel = new MouseWheel(deviceNum, _profileSettings, _virtualKBM);
            firstTouch = new Touch(0, 0, 0, null);

            _gyro = new GyroOutputProcessor(deviceNum, d, cursor, _profileSettings, _stickMotion);
            //Global.ForSlot(deviceNum).Gyro.MouseStick.SetRefreshEvents(filterPair.axis1Filter);
            //Global.ForSlot(deviceNum).Gyro.MouseStick.SetRefreshEvents(filterPair.axis2Filter);

            _touchStick = new TouchStickOutput(deviceNum, _profileSettings, _stickMotion);
            touchStickTrackball = new FakeTrackball();
        }

        public void ResetTrackAccel(double friction)
        {
            _trackball.ResetAccel(friction);
        }

        public void ResetTouchStickAccel(double friction)
        {
            touchStickTrackball.ResetAccel(friction);
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


        bool previousUnchangedTouchJoyFrame = false;
        int previousTouchDX = 0;
        int previousTouchDY = 0;

        public virtual void sixaxisMoved(DS4SixAxis sender, SixAxisEventArgs arg)
        {
            _gyro.SixaxisMoved(arg);
        }

        public bool IsGyroTriggerActive(GyroOutMode mode) => _gyro.IsTriggerActive(mode);

        public void ReplaceOneEuroFilterPair() => _gyro.ReplaceOneEuroFilterPair();

        public void SetupLateOneEuroFilters() => _gyro.SetupLateOneEuroFilters();

        public void PostSetup()
        {
            TouchpadOutMode tempMode = _profileSettings.ForSlot(deviceNum).Touchpad.OutputMode;
            if (tempMode == TouchpadOutMode.MouseJoystick)
            {
                TouchMouseStickInfo msinfo = _profileSettings.ForSlot(deviceNum).Touchpad.MouseStick;
                msinfo.RemoveRefreshEvents();
                msinfo.SetRefreshEvents(_touchStick.Filter.axis1Filter);
                msinfo.SetRefreshEvents(_touchStick.Filter.axis2Filter);

                touchStickTrackball.ResetBuffers();
                touchStickTrackball.ResetAccel(msinfo.trackballFriction);
                trackballTouchStickActive = false;
            }

            _gyro.PostSetup();
        }

        public void Reset()
        {
            TouchpadOutMode tempMode = _profileSettings.ForSlot(deviceNum).Touchpad.OutputMode;
            if (tempMode == TouchpadOutMode.Mouse)
            {
                _trackball.Reset();
            }
            else if (tempMode == TouchpadOutMode.MouseJoystick)
            {
                touchStickTrackball.Reset();
                trackballTouchStickActive = false;
            }

            _gyro.Reset();
        }


        private void TouchpadMouseStick(int dx, int dy)
        {
            s = dev.getCurrentStateRef();
            _touchStick.Apply(dx, dy, s.elapsedTime);
        }

        private bool getDS4ControlsByName(int key) => TriggerButtonReader.IsPressed(s, key);

        private bool tempBool = false;
        public virtual void touchesMoved(DS4Touchpad sender, TouchpadEventArgs arg)
        {
            s = dev.getCurrentStateRef();

            //Trace.WriteLine("TOUCHES_MOVED");
            TouchpadOutMode tempMode = _profileSettings.ForSlot(deviceNum).Touchpad.OutputMode;
            if (tempMode == TouchpadOutMode.Mouse)
            {
                if (_profileSettings.GetTouchpadActive(deviceNum))
                {
                    int[] disArray = _profileSettings.ForSlot(deviceNum).Touchpad.DisInvertTriggers;
                    tempBool = true;
                    for (int i = 0, arlen = disArray.Length; tempBool && i < arlen; i++)
                    {
                        if (getDS4ControlsByName(disArray[i]) == false)
                            tempBool = false;
                    }

                    if (_profileSettings.ForSlot(deviceNum).Touchpad.Trackball)
                    {
                        _trackball.AddSample(arg.Touches[0].DeltaX, arg.Touches[0].DeltaY);
                    }

                    cursor.touchesMoved(arg, dragging || dragging2, tempBool);
                    wheel.touchesMoved(arg, dragging || dragging2);
                }
                else
                {
                    if (_profileSettings.ForSlot(deviceNum).Touchpad.Trackball)
                    {
                        _trackball.AddEmptySample();
                    }
                }
            }
            else if (tempMode == TouchpadOutMode.Controls)
            {
                if (!(swipeUp || swipeDown || swipeLeft || swipeRight) && arg.Touches.Length == 1)
                {
                    if (arg.Touches[0].HwX - firstTouch.HwX > 300) swipeRight = true;
                    if (arg.Touches[0].HwX - firstTouch.HwX < -300) swipeLeft = true;
                    if (arg.Touches[0].HwY - firstTouch.HwY > 300) swipeDown = true;
                    if (arg.Touches[0].HwY - firstTouch.HwY < -300) swipeUp = true;
                }

                swipeUpB = (byte)Math.Min(255, Math.Max(0, (firstTouch.HwY - arg.Touches[0].HwY) * 1.5f));
                swipeDownB = (byte)Math.Min(255, Math.Max(0, (arg.Touches[0].HwY - firstTouch.HwY) * 1.5f));
                swipeLeftB = (byte)Math.Min(255, Math.Max(0, firstTouch.HwX - arg.Touches[0].HwX));
                swipeRightB = (byte)Math.Min(255, Math.Max(0, arg.Touches[0].HwX - firstTouch.HwX));
            }
            else if (tempMode == TouchpadOutMode.MouseJoystick)
            {
                previousUnchangedTouchJoyFrame = false;

                TouchMouseStickInfo msinfo = _profileSettings.ForSlot(deviceNum).Touchpad.MouseStick;
                if (_profileSettings.GetTouchpadActive(deviceNum))
                {
                    if (msinfo.trackballMode)
                    //if (true)
                    {
                        //int iIndex = trackballBufferTail;
                        //// Establish 4 ms as the base
                        //trackballXBuffer[iIndex] = (arg.touches[0].deltaX * TRACKBALL_SCALE) / 0.004; // dev.getCurrentStateRef().elapsedTime;
                        //trackballYBuffer[iIndex] = (arg.touches[0].deltaY * TRACKBALL_SCALE) / 0.004; // dev.getCurrentStateRef().elapsedTime;
                        //trackballBufferTail = (iIndex + 1) % TRACKBALL_BUFFER_LEN;
                        //if (trackballBufferHead == trackballBufferTail)
                        //    trackballBufferHead = (trackballBufferHead + 1) % TRACKBALL_BUFFER_LEN;

                        touchStickTrackball.AddData(arg.Touches[0].DeltaX, arg.Touches[0].DeltaY);
                    }

                    TouchpadMouseStick(arg.Touches[0].DeltaX, arg.Touches[0].DeltaY);
                    previousTouchDX = arg.Touches[0].DeltaX;
                    previousTouchDY = arg.Touches[0].DeltaY;
                }
                else
                {
                    //if (Global.getTrackballMode(deviceNum))
                    //if (true)
                    if (msinfo.trackballMode)
                    {
                        touchStickTrackball.AddEmptyTrackballEntry();
                        //AddEmptyTrackballEntry();
                    }
                }
            }

            // Slide flags needed for possible profile switching from Touchpad swipes
            if (Math.Abs(firstTouch.HwY - arg.Touches[0].HwY) < 50 && arg.Touches.Length == 2)
            {
                if (arg.Touches[0].HwX - firstTouch.HwX > 200 && !slideleft)
                    slideright = true;
                else if (firstTouch.HwX - arg.Touches[0].HwX > 200 && !slideright)
                    slideleft = true;
            }

            TouchButtonCheckProcess(arg);
            synthesizeMouseButtons();
        }

        public virtual void touchesBegan(DS4Touchpad sender, TouchpadEventArgs arg)
        {
            TouchpadOutMode tempMode = _profileSettings.ForSlot(deviceNum).Touchpad.OutputMode;
            bool mouseMode = tempMode == TouchpadOutMode.Mouse;
            if (mouseMode)
            {
                _trackball.Reset();

                cursor.touchesBegan(arg);
                wheel.touchesBegan(arg);
            }
            else if (tempMode == TouchpadOutMode.MouseJoystick)
            {
                touchStickTrackball.ResetBuffers();
                trackballTouchStickActive = false;
            }

            previousTouchDX = previousTouchDY = 0;
            previousUnchangedTouchJoyFrame = false;
            pastTime = arg.TimeStamp;
            // 調査用 Trace（動作は変えない）: タッチ開始の検出
            if (AppLogger.IsTraceEnabled) AppLogger.LogTrace($"[TAP] device={deviceNum} touchesBegan touches={arg.Touches?.Length} buttonPressed={arg.TouchButtonPressed}");
            firstTouch.populate(arg.Touches[0].HwX, arg.Touches[0].HwY, arg.Touches[0].TouchID,
                arg.Touches[0].PreviousTouch);

            if (mouseMode && _profileSettings.ForSlot(deviceNum).Touchpad.DoubleTap)
            {
                DateTime test = arg.TimeStamp;
                if (test <= (firstTap + TimeSpan.FromMilliseconds((double)_profileSettings.ForSlot(deviceNum).Touchpad.TapSensitivity * 1.5)) && !arg.TouchButtonPressed)
                    secondtouchbegin = true;
            }

            wasTouched = false;
            wasTouchButtonClicked = false;
            releaseButtonActive = false;
            touchButtonCurrentCandidate = TouchButtonModeCandidate.None;
            TouchButtonUpFlags();

            s = dev.getCurrentStateRef();
            TouchButtonCheckProcess(arg);
            synthesizeMouseButtons();
        }

        public virtual void touchesEnded(DS4Touchpad sender, TouchpadEventArgs arg)
        {
            //Trace.WriteLine("TOUCHES ENDED");

            s = dev.getCurrentStateRef();
            wasTouched = true;
            slideright = slideleft = false;
            swipeUp = swipeDown = swipeLeft = swipeRight = false;
            swipeUpB = swipeDownB = swipeLeftB = swipeRightB = 0;
            previousTouchDX = previousTouchDY = 0;
            previousUnchangedTouchJoyFrame = false;
            byte tapSensitivity = _profileSettings.ForSlot(deviceNum).Touchpad.TapSensitivity;
            if (AppLogger.IsTraceEnabled) AppLogger.LogTrace($"[TAP] device={deviceNum} touchesEnded outMode={_profileSettings.ForSlot(deviceNum).Touchpad.OutputMode} sens={tapSensitivity}");
            if (tapSensitivity != 0 && _profileSettings.ForSlot(deviceNum).Touchpad.OutputMode == TouchpadOutMode.Mouse)
            {
                if (secondtouchbegin)
                {
                    tappedOnce = false;
                    secondtouchbegin = false;
                }

                DateTime test = arg.TimeStamp;
                // 調査用 Trace（動作は変えない）: タップ判定の入力値と成否
                if (AppLogger.IsTraceEnabled) AppLogger.LogTrace($"[TAP] device={deviceNum} ended sens={tapSensitivity} doubleTap={_profileSettings.ForSlot(deviceNum).Touchpad.DoubleTap} " +
                    $"elapsedMs={(test - pastTime).TotalMilliseconds:F0} limitMs={tapSensitivity * 2} buttonPressed={arg.TouchButtonPressed} tappedOnce={tappedOnce} " +
                    $"dx={Math.Abs(firstTouch.HwX - arg.Touches[0].HwX)} dy={Math.Abs(firstTouch.HwY - arg.Touches[0].HwY)}");
                if (test <= (pastTime + TimeSpan.FromMilliseconds((double)tapSensitivity * 2)) && !arg.TouchButtonPressed && !tappedOnce)
                {
                    // Phase6 変更（ユーザー承認 2026-10-15）: 上流の許容 10 は実機で厳しすぎ、タップが成立しなかったため 100 に拡大
                    if (Math.Abs(firstTouch.HwX - arg.Touches[0].HwX) < TapMaxMoveHw && Math.Abs(firstTouch.HwY - arg.Touches[0].HwY) < TapMaxMoveHw)
                    {
                        if (AppLogger.IsTraceEnabled) AppLogger.LogTrace($"[TAP] device={deviceNum} tap accepted (doubleTap={_profileSettings.ForSlot(deviceNum).Touchpad.DoubleTap})");
                        if (_profileSettings.ForSlot(deviceNum).Touchpad.DoubleTap)
                        {
                            tappedOnce = true;
                            firstTap = arg.TimeStamp;
                            TimeofEnd = DateTime.Now; //since arg can't be used in synthesizeMouseButtons
                        }
                        else
                            StartTapClickHold(); //this way no delay if disabled (クリックは synthesizeMouseButtons が保持時間の間出し続ける)
                    }
                }
            }
            else
            {
                TouchpadOutMode tempMode = _profileSettings.ForSlot(deviceNum).Touchpad.OutputMode;
                if (tempMode == TouchpadOutMode.Mouse)
                {
                    int[] disArray = _profileSettings.ForSlot(deviceNum).Touchpad.DisInvertTriggers;
                    tempBool = true;
                    for (int i = 0, arlen = disArray.Length; tempBool && i < arlen; i++)
                    {
                        if (getDS4ControlsByName(disArray[i]) == false)
                            tempBool = false;
                    }

                    if (_profileSettings.ForSlot(deviceNum).Touchpad.Trackball)
                    {
                        _trackball.BeginCoast();
                        if (_trackball.Step(s.elapsedTime, out int dx, out int dy))
                        {
                            cursor.TouchMoveCursor(dx, dy, tempBool);
                        }
                    }
                }
                else if (tempMode == TouchpadOutMode.MouseJoystick)
                {
                    TouchMouseStickInfo msinfo = _profileSettings.ForSlot(deviceNum).Touchpad.MouseStick;
                    //if (Global.getTrackballMode(deviceNum))
                    //if (true)
                    if (msinfo.trackballMode)
                    {
                        if (!trackballTouchStickActive)
                        {
                            //    double currentWeight = 1.0;
                            //    double finalWeight = 0.0;
                            //    double x_out = 0.0, y_out = 0.0;
                            //    int idx = -1;
                            //    for (int i = 0; i < TRACKBALL_BUFFER_LEN && idx != trackballBufferHead; i++)
                            //    {
                            //        idx = (trackballBufferTail - i - 1 + TRACKBALL_BUFFER_LEN) % TRACKBALL_BUFFER_LEN;
                            //        x_out += trackballXBuffer[idx] * currentWeight;
                            //        y_out += trackballYBuffer[idx] * currentWeight;
                            //        finalWeight += currentWeight;
                            //        currentWeight *= 1.0;
                            //    }

                            //    x_out /= finalWeight;
                            //    trackballXVel = x_out;
                            //    y_out /= finalWeight;
                            //    trackballYVel = y_out;

                            //    trackballActive = true;
                            //}

                            //double tempAngle = Math.Atan2(-trackballYVel, trackballXVel);
                            //double normX = Math.Abs(Math.Cos(tempAngle));
                            //double normY = Math.Abs(Math.Sin(tempAngle));
                            //int signX = Math.Sign(trackballXVel);
                            //int signY = Math.Sign(trackballYVel);

                            //double trackXvDecay = Math.Min(Math.Abs(trackballXVel), trackballAccel * s.elapsedTime * normX);
                            //double trackYvDecay = Math.Min(Math.Abs(trackballYVel), trackballAccel * s.elapsedTime * normY);
                            //double xVNew = trackballXVel - (trackXvDecay * signX);
                            //double yVNew = trackballYVel - (trackYvDecay * signY);
                            //double xMotion = (xVNew * s.elapsedTime) / TRACKBALL_SCALE;
                            //double yMotion = (yVNew * s.elapsedTime) / TRACKBALL_SCALE;
                            //if (xMotion != 0.0)
                            //{
                            //    xMotion += trackballDXRemain;
                            //}
                            //else
                            //{
                            //    trackballDXRemain = 0.0;
                            //}

                            //int dx = (int)xMotion;
                            //trackballDXRemain = xMotion - dx;

                            //if (yMotion != 0.0)
                            //{
                            //    yMotion += trackballDYRemain;
                            //}
                            //else
                            //{
                            //    trackballDYRemain = 0.0;
                            //}

                            //int dy = (int)yMotion;
                            //trackballDYRemain = yMotion - dy;

                            //trackballXVel = xVNew;
                            //trackballYVel = yVNew;

                            //if (dx == 0 && dy == 0)
                            //{
                            //    trackballActive = false;
                            //}
                            //else
                            //{
                            //    TouchpadMouseStick(dx, dy);
                            //}

                            touchStickTrackball.CalcTrackballInitVelocities();
                            trackballTouchStickActive = true;
                            touchStickTrackball.Process(s.elapsedTime, out int dx, out int dy);
                            if (dx == 0 && dy == 0)
                            {
                                trackballTouchStickActive = false;
                            }
                            else
                            {
                                TouchpadMouseStick(dx, dy);
                            }
                        }
                    }
                }
            }

            TouchButtonCheckProcess(arg);
            synthesizeMouseButtons();
        }
        public void TouchStartedOrEnded(DS4Touchpad sender, TouchpadEventArgs arg)
        {
            if (arg.Touches.Length < 2)
            {

                if (arg.TouchActive)
                {
                    touchStarted = true;
                }
                else
                {
                    touchEnded = true;
                }
            }
        }

        private bool isLeft(Touch t)
        {
            return t.HwX < 1920 * 2 / 5;
        }

        private bool isRight(Touch t)
        {
            return t.HwX >= 1920 * 2 / 5;
        }

        public virtual void touchUnchanged(DS4Touchpad sender, EventArgs unused)
        {
            //Trace.WriteLine("TOUCHES UNCHANGED");

            s = dev.getCurrentStateRef();
            if (s.Touch1Finger)
            {
                wasTouched = false;
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
                        int[] disArray = _profileSettings.ForSlot(deviceNum).Touchpad.DisInvertTriggers;
                        tempBool = true;
                        for (int i = 0, arlen = disArray.Length; tempBool && i < arlen; i++)
                        {
                            if (getDS4ControlsByName(disArray[i]) == false)
                                tempBool = false;
                        }

                        if (_trackball.Step(s.elapsedTime, out int dx, out int dy))
                        {
                            cursor.TouchMoveCursor(dx, dy, tempBool);
                        }
                    }
                }
            }
            else if (touchMode == TouchpadOutMode.MouseJoystick)
            {
                if (trackballTouchStickActive)
                {
                    touchStickTrackball.Process(s.elapsedTime, out int dx, out int dy);
                    TouchpadMouseStick(dx, dy);
                }
                else if (s.Touch1 && !previousUnchangedTouchJoyFrame)
                {
                    previousUnchangedTouchJoyFrame = true;
                    //Trace.WriteLine($"PREVIOUS {previousTouchDX} {previousTouchDY}");
                    TouchpadMouseStick(previousTouchDX, previousTouchDY);
                }
                else if (!s.Touch1)
                {
                    double currentRate = 1.0 / s.elapsedTime;
                    _touchStick.Filter.axis1Filter.Filter(0, currentRate);
                    _touchStick.Filter.axis2Filter.Filter(0, currentRate);
                }
            }

            bool releaseButtonChanged = false;
            if (releaseButtonActive)
            {
                DateTime current = DateTime.UtcNow;
                // Set keydown time to 100 ms for now
                // Phase6 fix: 上流の比較が逆（'<'）で、離した直後のフレームでフラグが下りて Mapping に届かなかった。
                // 100ms 経過後にフラグを下ろす（'>='）。
                if (current >= (onReleaseTime + TimeSpan.FromMilliseconds(100.0)))
                {
                    releaseButtonActive = false;
                    releaseButtonChanged = true;
                    TouchButtonUpFlags();
                }
            }

            // tappedOnce（Double Tap の保留クリック）と tapClickHoldUntil（タップクリックの保持中）は、
            // 指を離した後も処理が必要（Phase6 変更、ユーザー承認 2026-10-15）
            if (s.Touch1Finger || s.TouchButton ||
                releaseButtonActive || releaseButtonChanged ||
                tappedOnce || _time.GetUtcNow().UtcDateTime < tapClickHoldUntil)
            {
                synthesizeMouseButtons();
            }
        }

        private void TouchButtonUpFlags()
        {
            upperDown = leftDown = rightDown = multiDown = false;
            touchButtonCurrentCandidate = TouchButtonModeCandidate.None;
        }

        public bool dragging, dragging2;

        private void TouchButtonCheckProcess(TouchpadEventArgs arg)
        {
            bool activateTouchButton = false;
            TouchButtonActivationMode touchButtonMode = _profileSettings.ForSlot(deviceNum).Touchpad.ButtonMode;
            //TouchButtonActivationMode touchButtonMode = TouchButtonActivationMode.Release;
            if (touchButtonMode == TouchButtonActivationMode.Click &&
                arg.TouchButtonPressed)
            {
                if (arg.Touches == null)
                {
                    touchButtonCurrentCandidate = TouchButtonModeCandidate.Upper;
                }
                else if (arg.Touches.Length > 1)
                {
                    touchButtonCurrentCandidate = TouchButtonModeCandidate.Multi;
                }
                else
                {
                    if (isLeft(arg.Touches[0]))
                    {
                        touchButtonCurrentCandidate = TouchButtonModeCandidate.Left;
                    }
                    else if (isRight(arg.Touches[0]))
                    {
                        touchButtonCurrentCandidate = TouchButtonModeCandidate.Right;
                    }
                }

                activateTouchButton = true;
            }
            else if (touchButtonMode == TouchButtonActivationMode.Touch)
            {
                if (arg.TouchActive || arg.TouchButtonPressed)
                {
                    if (arg.Touches == null)
                    {
                        touchButtonCurrentCandidate = TouchButtonModeCandidate.Upper;
                    }
                    else if (arg.Touches.Length > 1)
                    {
                        touchButtonCurrentCandidate = TouchButtonModeCandidate.Multi;
                    }
                    else
                    {
                        if (isLeft(arg.Touches[0]))
                        {
                            touchButtonCurrentCandidate = TouchButtonModeCandidate.Left;
                        }
                        else if (isRight(arg.Touches[0]))
                        {
                            touchButtonCurrentCandidate = TouchButtonModeCandidate.Right;
                        }
                    }

                    activateTouchButton = true;
                }
                else
                {
                    upperDown = leftDown = rightDown = multiDown = false;
                }
            }
            else if (touchButtonMode == TouchButtonActivationMode.Release)
            {
                if (touchButtonCurrentCandidate == TouchButtonModeCandidate.None)
                {
                    // Top region of Touchpad was clicked. Out of range of Touchpad sensors
                    if (!arg.TouchActive && !arg.TouchButtonPressed && wasTouchButtonClicked)
                    {
                        touchButtonCurrentCandidate = TouchButtonModeCandidate.Upper;
                    }
                    else if (arg.TouchActive)
                    {
                        if (arg.Touches.Length > 1)
                        {
                            touchButtonCurrentCandidate = TouchButtonModeCandidate.Multi;
                        }
                    }
                    else if (!arg.TouchActive && arg.Touches != null)
                    {
                        if (isLeft(arg.Touches[0]))
                        {
                            touchButtonCurrentCandidate = TouchButtonModeCandidate.Left;
                        }
                        else if (isRight(arg.Touches[0]))
                        {
                            touchButtonCurrentCandidate = TouchButtonModeCandidate.Right;
                        }
                    }
                }

                if (touchButtonCurrentCandidate != TouchButtonModeCandidate.None)
                {
                    if (touchButtonCurrentCandidate == TouchButtonModeCandidate.Multi)
                    {
                        // Check that no finger is touching Touchpad
                        if (wasTouched && !arg.TouchActive)
                        {
                            activateTouchButton = true;
                            releaseButtonActive = true;
                            onReleaseTime = DateTime.UtcNow;
                        }
                    }
                    else if ((wasTouched && !arg.TouchActive) ||
                        (wasTouchButtonClicked && !arg.TouchButtonPressed && !arg.TouchActive))
                    {
                        activateTouchButton = true;
                        releaseButtonActive = true;
                        onReleaseTime = DateTime.UtcNow;
                    }
                }
            }

            if (activateTouchButton)
            {
                switch (touchButtonCurrentCandidate)
                {
                    case TouchButtonModeCandidate.Left:
                        leftDown = true;
                        break;
                    case TouchButtonModeCandidate.Right:
                        rightDown = true;
                        break;
                    case TouchButtonModeCandidate.Multi:
                        multiDown = true;
                        break;
                    case TouchButtonModeCandidate.Upper:
                        upperDown = true;
                        break;
                    default:
                        break;
                }
            }
        }

        private void synthesizeMouseButtons()
        {
            TouchpadOutMode tempMode = _profileSettings.ForSlot(deviceNum).Touchpad.OutputMode;
            if (tempMode != TouchpadOutMode.Passthru)
            {
                bool touchClickPass = _profileSettings.ForSlot(deviceNum).Touchpad.ClickPassthru;
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

            if (_profileSettings.GetDS4CSetting(deviceNum, DS4Controls.TouchLeft).IsDefault &&
                leftDown)
            {
                _syntheticInput.CountClick(deviceNum, Click.Left);
                dragging2 = true;
            }
            else
            {
                dragging2 = false;
            }

            if (_profileSettings.GetDS4CSetting(deviceNum, DS4Controls.TouchUpper).IsDefault &&
                upperDown)
            {
                _syntheticInput.CountClick(deviceNum, Click.Middle);
            }

            if (_profileSettings.GetDS4CSetting(deviceNum, DS4Controls.TouchRight).IsDefault &&
                rightDown)
            {
                _syntheticInput.CountClick(deviceNum, Click.Left);
            }

            if (_profileSettings.GetDS4CSetting(deviceNum, DS4Controls.TouchMulti).IsDefault &&
                multiDown)
            {
                _syntheticInput.CountClick(deviceNum, Click.Right);
            }

            if (_profileSettings.ForSlot(deviceNum).Touchpad.OutputMode == TouchpadOutMode.Mouse)
            {
                if (tappedOnce)
                {
                    DateTime tester = DateTime.Now;
                    if (tester > (TimeofEnd + TimeSpan.FromMilliseconds((double)(_profileSettings.ForSlot(deviceNum).Touchpad.TapSensitivity) * 1.5)))
                    {
                        StartTapClickHold();
                        tappedOnce = false;
                    }
                    //if it fails the method resets, and tries again with a new tester value (gives tap a delay so tap and hold can work)
                }

                // タップのクリックを保持時間の間、押したままにする（毎フレーム MapClick。state は毎フレーム消去されるため）
                if (_time.GetUtcNow().UtcDateTime < tapClickHoldUntil)
                {
                    _syntheticInput.CountClick(deviceNum, Click.Left);
                }

                if (secondtouchbegin) //if tap and hold (also works as double tap)
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

        public virtual void touchButtonUp(DS4Touchpad sender, TouchpadEventArgs arg)
        {
            pushed = DS4Controls.None;
            upperDown = leftDown = rightDown = multiDown = false;
            wasTouchButtonClicked = true;

            s = dev.getCurrentStateRef();
            TouchButtonActivationMode touchButtonMode = _profileSettings.ForSlot(deviceNum).Touchpad.ButtonMode;
            if (s.Touch1 || s.Touch2 ||
                touchButtonMode == TouchButtonActivationMode.Release)
            {
                TouchButtonCheckProcess(arg);
                synthesizeMouseButtons();
            }
        }

        public virtual void touchButtonDown(DS4Touchpad sender, TouchpadEventArgs arg)
        {
            if (arg.Touches != null &&
                arg.Touches.Length == 1 &&
                (_profileSettings.ForSlot(deviceNum).Touchpad.LowerRightAsRightClick && arg.Touches[0].HwX > (1920 * 3) / 4 && arg.Touches[0].HwY > (960 * 3) / 4))
            {
                _syntheticInput.CountClick(deviceNum, Click.Right);
            }
            else if (arg.Touches == null)
            {
                if (touchButtonCurrentCandidate != TouchButtonModeCandidate.None)
                {
                    touchButtonCurrentCandidate = TouchButtonModeCandidate.None;
                }
            }

            wasTouchButtonClicked = false;
            s = dev.getCurrentStateRef();
            TouchButtonCheckProcess(arg);
            synthesizeMouseButtons();
        }

        public void populatePriorButtonStates()
        {
            priorUpperDown = upperDown;
            priorLeftDown = leftDown;
            priorRightDown = rightDown;
            priorMultiDown = multiDown;

            priorSwipeLeft = swipeLeft; priorSwipeRight = swipeRight;
            priorSwipeUp = swipeUp; priorSwipeDown = swipeDown;
            priorSwipeLeftB = swipeLeftB; priorSwipeRightB = swipeRightB; priorSwipeUpB = swipeUpB;
            priorSwipeDownB = swipeDownB; priorSwipedB = swipedB;
        }

        public ControllerState getDS4State()
        {
            return s;
        }
    }
}