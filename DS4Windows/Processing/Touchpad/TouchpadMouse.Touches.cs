using System;
using DS4Windows.Core.Logging;
using DS4Windows.Core.Settings;
using DS4Windows.Input;

namespace DS4Windows.Processing.Touchpad
{
    /// <summary>タッチの開始・移動・終了の処理。</summary>
    public partial class TouchpadMouse
    {
        private void ControlsModeMoved(TouchpadEventArgs arg)
        {
                if (!(Gestures.SwipeUp || Gestures.SwipeDown || Gestures.SwipeLeft || Gestures.SwipeRight) && arg.Touches!.Length == 1) // タッチパッドのイベントは、接触の配列を持って来る（null なら従来どおり NullReferenceException）
                {
                    if (arg.Touches[0].HwX - firstTouch.HwX > 300) Gestures.SwipeRight = true;
                    if (arg.Touches[0].HwX - firstTouch.HwX < -300) Gestures.SwipeLeft = true;
                    if (arg.Touches[0].HwY - firstTouch.HwY > 300) Gestures.SwipeDown = true;
                    if (arg.Touches[0].HwY - firstTouch.HwY < -300) Gestures.SwipeUp = true;
                }

                Gestures.SwipeUpB = (byte)Math.Min(255, Math.Max(0, (firstTouch.HwY - arg.Touches![0].HwY) * 1.5f)); // タッチパッドのイベントは、接触の配列を持って来る（null なら従来どおり NullReferenceException）
                Gestures.SwipeDownB = (byte)Math.Min(255, Math.Max(0, (arg.Touches[0].HwY - firstTouch.HwY) * 1.5f));
                Gestures.SwipeLeftB = (byte)Math.Min(255, Math.Max(0, firstTouch.HwX - arg.Touches[0].HwX));
                Gestures.SwipeRightB = (byte)Math.Min(255, Math.Max(0, arg.Touches[0].HwX - firstTouch.HwX));
        }


        public void TouchesMoved(DS4Touchpad sender, TouchpadEventArgs arg)
        {
            TouchpadSlotSettings touchpad = _profileSettings.ForSlot(deviceNum).Touchpad;
            s = dev!.GetCurrentStateRef(); // 接続中の機器から呼ばれる（null なら従来どおり NullReferenceException）

            //Trace.WriteLine("TOUCHES_MOVED");
            TouchpadOutMode tempMode = touchpad.OutputMode;
            if (tempMode == TouchpadOutMode.Mouse)
                MouseModeMoved(arg);
            else if (tempMode == TouchpadOutMode.Controls)
                ControlsModeMoved(arg);

            // Slide flags needed for possible profile switching from Touchpad swipes
            if (Math.Abs(firstTouch.HwY - arg.Touches![0].HwY) < 50 && arg.Touches.Length == 2) // タッチパッドのイベントは、接触の配列を持って来る（null なら従来どおり NullReferenceException）
            {
                if (arg.Touches[0].HwX - firstTouch.HwX > 200 && !Gestures.SlideLeft)
                    Gestures.SlideRight = true;
                else if (firstTouch.HwX - arg.Touches[0].HwX > 200 && !Gestures.SlideRight)
                    Gestures.SlideLeft = true;
            }

            _buttons.Check(arg, touchpad.ButtonMode);
            synthesizeMouseButtons();
        }

        public void TouchesBegan(DS4Touchpad sender, TouchpadEventArgs arg)
        {
            TouchpadSlotSettings touchpad = _profileSettings.ForSlot(deviceNum).Touchpad;
            TouchpadOutMode tempMode = touchpad.OutputMode;
            bool mouseMode = tempMode == TouchpadOutMode.Mouse;
            if (mouseMode)
            {
                _trackball.Reset();

                cursor.TouchesBegan(arg);
                wheel.TouchesBegan(arg);
            }

            // 調査用 Trace（動作は変えない）: タッチ開始の検出
            if (AppLogger.IsTraceEnabled) AppLogger.LogTrace($"[TAP] device={deviceNum} touchesBegan touches={arg.Touches?.Length} buttonPressed={arg.TouchButtonPressed}");
            firstTouch.Populate(arg.Touches![0].HwX, arg.Touches[0].HwY, arg.Touches[0].TouchID, // タッチパッドのイベントは、接触の配列を持って来る（null なら従来どおり NullReferenceException）
                arg.Touches[0].PreviousTouch);

            _tap.TouchBegan(arg.TimeStamp, arg.TouchButtonPressed,
                mouseMode && touchpad.DoubleTap,
                touchpad.TapSensitivity);

            _buttons.ResetForNewTouch();

            s = dev!.GetCurrentStateRef(); // 接続中の機器から呼ばれる（null なら従来どおり NullReferenceException）
            _buttons.Check(arg, touchpad.ButtonMode);
            synthesizeMouseButtons();
        }

        public void TouchesEnded(DS4Touchpad sender, TouchpadEventArgs arg)
        {
            TouchpadSlotSettings touchpad = _profileSettings.ForSlot(deviceNum).Touchpad;
            //Trace.WriteLine("TOUCHES ENDED");

            s = dev!.GetCurrentStateRef(); // 接続中の機器から呼ばれる（null なら従来どおり NullReferenceException）
            _buttons.WasTouched = true;
            Gestures.SlideRight = Gestures.SlideLeft = false;
            Gestures.SwipeUp = Gestures.SwipeDown = Gestures.SwipeLeft = Gestures.SwipeRight = false;
            Gestures.SwipeUpB = Gestures.SwipeDownB = Gestures.SwipeLeftB = Gestures.SwipeRightB = 0;
            byte tapSensitivity = touchpad.TapSensitivity;
            if (AppLogger.IsTraceEnabled) AppLogger.LogTrace($"[TAP] device={deviceNum} touchesEnded outMode={touchpad.OutputMode} sens={tapSensitivity}");
            if (tapSensitivity != 0 && touchpad.OutputMode == TouchpadOutMode.Mouse)
            {
                _tap.TouchEnded(arg, firstTouch, tapSensitivity, touchpad.DoubleTap);
            }
            else
            {
                TouchpadOutMode tempMode = touchpad.OutputMode;
                if (tempMode == TouchpadOutMode.Mouse)
                {
                    tempBool = AreDisInvertTriggersHeld();

                    if (touchpad.Trackball)
                    {
                        _trackball.BeginCoast();
                        if (_trackball.Step(s.elapsedTime, out int dx, out int dy))
                        {
                            cursor.TouchMoveCursor(dx, dy, tempBool);
                        }
                    }
                }

            }

            _buttons.Check(arg, touchpad.ButtonMode);
            synthesizeMouseButtons();
        }
        public void TouchStartedOrEnded(DS4Touchpad sender, TouchpadEventArgs arg)
        {
            if (arg.Touches!.Length < 2) // タッチパッドのイベントは、接触の配列を持って来る（null なら従来どおり NullReferenceException）
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
    }
}
