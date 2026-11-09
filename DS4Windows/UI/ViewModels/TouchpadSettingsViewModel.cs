using System;
using System.Collections.Generic;
using System.Windows.Controls;
using DS4Windows.Core.Abstractions;
using DS4Windows.Core.Settings;
using DS4Windows.UI.ViewModels.Util;

namespace DS4Windows.UI.ViewModels
{
    /// <summary>
    /// プロファイル編集画面の「タッチパッド」タブの ViewModel。1 つのスロットのタッチパッドの設定（<see cref="TouchpadSlotSettings"/>）だけを扱う。
    /// プロパティ名は、タブの Binding と同じ名前のまま（画面の部品は、この ViewModel を DataContext にして、そのまま使える）。
    /// 値は設定サービスの配列にあり、保存・読込の経路は変わらない。
    /// </summary>
    public class TouchpadSettingsViewModel : ObservableObject
    {
        private readonly TouchpadSlotSettings touchpad;
        private readonly IEnvironmentService environmentService;

        public TouchpadSettingsViewModel(TouchpadSlotSettings touchpad, IEnvironmentService environmentService)
        {
            this.touchpad = touchpad ?? throw new ArgumentNullException(nameof(touchpad));
            this.environmentService = environmentService ?? throw new ArgumentNullException(nameof(environmentService));
        }

        public bool UsingMinViGEm173333 => environmentService.IsUsingMinViGEm117333();

        public int TouchpadOutputIndex
        {
            get
            {
                int index = 0;
                switch (touchpad.OutputMode)
                {
                    case TouchpadOutMode.Mouse:
                        index = 0; break;
                    case TouchpadOutMode.Controls:
                        index = 1; break;
                    case TouchpadOutMode.MouseJoystick:
                        index = 2; break;
                    case TouchpadOutMode.Passthru:
                        index = 3; break;
                    default: break;
                }
                return index;
            }
            set
            {
                TouchpadOutMode temp = TouchpadOutMode.Mouse;
                switch (value)
                {
                    case 0: break;
                    case 1:
                        temp = TouchpadOutMode.Controls; break;
                    case 2:
                        temp = TouchpadOutMode.MouseJoystick; break;
                    case 3:
                        temp = TouchpadOutMode.Passthru; break;
                    default: break;
                }

                TouchpadOutMode current = touchpad.OutputMode;
                if (temp == current) return;
                TouchpadOutputIndexChanging?.Invoke(this, current, temp);
                touchpad.OutputMode = temp;
                OnPropertyChanged(nameof(TouchpadOutputIndex));
            }
        }
        public event PropertyChangingHandler<TouchpadOutMode> TouchpadOutputIndexChanging;
        public bool TouchSenExists
        {
            get => touchpad.TouchSensitivity != 0;
            set
            {
                touchpad.TouchSensitivity = value ? (byte)100 : (byte)0;
                OnPropertyChanged(nameof(TouchSenExists));
                OnPropertyChanged(nameof(TouchSens));
            }
        }
        public int TouchSens
        {
            get => touchpad.TouchSensitivity;
            set
            {
                int temp = touchpad.TouchSensitivity;
                if (temp == value) return;
                touchpad.TouchSensitivity = (byte)value;
                if (value == 0) OnPropertyChanged(nameof(TouchSenExists));
                OnPropertyChanged(nameof(TouchSens));
            }
        }
        public bool TouchScrollExists
        {
            get => touchpad.ScrollSensitivity != 0;
            set
            {
                touchpad.ScrollSensitivity = value ? 100 : 0;
                OnPropertyChanged(nameof(TouchScrollExists));
                OnPropertyChanged(nameof(TouchScroll));
            }
        }
        public int TouchScroll
        {
            get => touchpad.ScrollSensitivity;
            set
            {
                int temp = touchpad.ScrollSensitivity;
                if (temp == value) return;
                touchpad.ScrollSensitivity = value;
                if (value == 0) OnPropertyChanged(nameof(TouchScrollExists));
                OnPropertyChanged(nameof(TouchScroll));
            }
        }
        public bool TouchTapExists
        {
            get => touchpad.TapSensitivity != 0;
            set
            {
                touchpad.TapSensitivity = value ? (byte)100 : (byte)0;
                OnPropertyChanged(nameof(TouchTapExists));
                OnPropertyChanged(nameof(TouchTap));
            }
        }
        public int TouchTap
        {
            get => touchpad.TapSensitivity;
            set
            {
                int temp = touchpad.TapSensitivity;
                if (temp == value) return;
                touchpad.TapSensitivity = (byte)value;
                if (value == 0) OnPropertyChanged(nameof(TouchTapExists));
                OnPropertyChanged(nameof(TouchTap));
            }
        }
        public bool TouchDoubleTap
        {
            get => touchpad.DoubleTap;
            set
            {
                touchpad.DoubleTap = value;
            }
        }

        public bool TouchJitter
        {
            get => touchpad.JitterCompensation;
            set => touchpad.JitterCompensation = value;
        }

        private int[] touchpadInvertToValue = new int[4] { 0, 2, 1, 3 };
        public int TouchInvertIndex
        {
            get
            {
                int invert = touchpad.Invert;
                int index = Array.IndexOf(touchpadInvertToValue, invert);
                return index;
            }
            set
            {
                int invert = touchpadInvertToValue[value];
                touchpad.Invert = invert;
            }
        }

        public bool LowerRightTouchRMB
        {
            get => touchpad.LowerRightAsRightClick;
            set
            {
                touchpad.LowerRightAsRightClick = value;
            }
        }

        public bool TouchpadClickPassthru
        {
            get => touchpad.ClickPassthru;
            set
            {
                touchpad.ClickPassthru = value;
            }
        }

        public bool StartTouchpadOff
        {
            get => touchpad.StartOff;
            set
            {
                touchpad.StartOff = value;
            }
        }

        public double TouchRelMouseRotation
        {
            get => touchpad.RelMouse.rotation * 180.0 / Math.PI;
            set => touchpad.RelMouse.rotation = value * Math.PI / 180.0;
        }

        public double TouchRelMouseMinThreshold
        {
            get => touchpad.RelMouse.minThreshold;
            set
            {
                double temp = touchpad.RelMouse.minThreshold;
                if (temp == value) return;
                touchpad.RelMouse.minThreshold = value;
            }
        }

        public bool TouchTrackball
        {
            get => touchpad.Trackball;
            set => touchpad.Trackball = value;
        }

        public double TouchTrackballFriction
        {
            get => touchpad.TrackballFriction;
            set => touchpad.TrackballFriction = value;
        }

        public int TouchMouseStickDeadZone
        {
            get => touchpad.MouseStick.deadZone;
            set => touchpad.MouseStick.deadZone = value;
        }

        public int TouchMouseStickMaxZone
        {
            get => touchpad.MouseStick.maxZone;
            set => touchpad.MouseStick.maxZone = value;
        }

        public int TouchMouseStickOutputStick
        {
            get => (int)touchpad.MouseStick.outputStick;
            set
            {
                touchpad.MouseStick.outputStick =
                    (TouchMouseStickInfo.OutputStick)value;
            }
        }

        public int TouchMouseStickOutputAxes
        {
            get => (int)touchpad.MouseStick.outputStickDir;
            set
            {
                touchpad.MouseStick.outputStickDir =
                    (TouchMouseStickInfo.OutputStickAxes)value;
            }
        }

        public double TouchMouseStickAntiDeadX
        {
            get => touchpad.MouseStick.antiDeadX * 100.0;
            set => touchpad.MouseStick.antiDeadX = value * 0.01;
        }

        public double TouchMouseStickAntiDeadY
        {
            get => touchpad.MouseStick.antiDeadY * 100.0;
            set => touchpad.MouseStick.antiDeadY = value * 0.01;
        }

        public int TouchMouseStickVertScale
        {
            get => touchpad.MouseStick.vertScale;
            set => touchpad.MouseStick.vertScale = value;
        }

        public bool TouchMouseStickMaxOutputEnabled
        {
            get => touchpad.MouseStick.maxOutputEnabled;
            set
            {
                bool temp = touchpad.MouseStick.maxOutputEnabled;
                if (temp == value) return;
                touchpad.MouseStick.maxOutputEnabled = value;
                OnPropertyChanged(nameof(TouchMouseStickMaxOutput));
            }
        }
        public double TouchMouseStickMaxOutput
        {
            get => touchpad.MouseStick.maxOutput;
            set => touchpad.MouseStick.maxOutput = value;
        }

        public bool TouchMouseStickInvertX
        {
            get => (touchpad.MouseStick.inverted & 1) == 1;
            set
            {
                if (value)
                {
                    touchpad.MouseStick.inverted |= 1;
                }
                else
                {
                    uint temp = touchpad.MouseStick.inverted;
                    touchpad.MouseStick.inverted = (uint)(temp & ~1);
                }
            }
        }

        public bool TouchMouseStickInvertY
        {
            get => (touchpad.MouseStick.inverted & 2) == 2;
            set
            {
                if (value)
                {
                    touchpad.MouseStick.inverted |= 2;
                }
                else
                {
                    uint temp = touchpad.MouseStick.inverted;
                    touchpad.MouseStick.inverted = (uint)(temp & ~2);
                }
            }
        }

        public bool TouchMouseStickSmooth
        {
            get => touchpad.MouseStick.UseSmoothing;
            set => touchpad.MouseStick.UseSmoothing = value;
        }

        public double TouchMouseStickOneEuroMinCutoff
        {
            get => touchpad.MouseStick.MinCutoff;
            set => touchpad.MouseStick.MinCutoff = value;
        }

        public double TouchMouseStickOneEuroBeta
        {
            get => touchpad.MouseStick.Beta;
            set => touchpad.MouseStick.Beta = value;
        }

        private List<EnumChoiceSelection<StickOutCurve.Curve>> touchMStickOutputCurveOptions =
            new List<EnumChoiceSelection<StickOutCurve.Curve>>
            {
                new EnumChoiceSelection<StickOutCurve.Curve>("Linear", StickOutCurve.Curve.Linear),
                new EnumChoiceSelection<StickOutCurve.Curve>("Easeout Quad", StickOutCurve.Curve.EaseoutQuad),
                new EnumChoiceSelection<StickOutCurve.Curve>("Easeout Cubic", StickOutCurve.Curve.EaseoutCubic),
                new EnumChoiceSelection<StickOutCurve.Curve>("Enhanced Precision", StickOutCurve.Curve.EnhancedPrecision),
                new EnumChoiceSelection<StickOutCurve.Curve>("Quadratic", StickOutCurve.Curve.Quadratic),
                new EnumChoiceSelection<StickOutCurve.Curve>("Cubic", StickOutCurve.Curve.Cubic),
            };

        public List<EnumChoiceSelection<StickOutCurve.Curve>> TouchMouseStickOutputCurveOptions => touchMStickOutputCurveOptions;

        public StickOutCurve.Curve TouchMouseStickOutputCurve
        {
            get => touchpad.MouseStick.outputCurve;
            set => touchpad.MouseStick.outputCurve = value;
        }

        public bool TouchMouseStickTrackball
        {
            get => touchpad.MouseStick.trackballMode;
            set => touchpad.MouseStick.trackballMode = value;
        }

        public double TouchMouseStickTrackballFriction
        {
            get => touchpad.MouseStick.trackballFriction;
            set
            {
                double temp = touchpad.MouseStick.trackballFriction;
                if (temp == value) return;
                touchpad.MouseStick.trackballFriction = value;
                OnPropertyChanged(nameof(TouchMouseStickTrackballFriction));
            }
        }
        public double TouchMouseStickRotation
        {
            get => touchpad.MouseStick.rotationRad * 180.0 / Math.PI;
            set => touchpad.MouseStick.rotationRad = value * Math.PI / 180.0;
        }

        private string touchDisInvertString = "None";
        public string TouchDisInvertString
        {
            get => touchDisInvertString;
            set
            {
                touchDisInvertString = value;
                OnPropertyChanged(nameof(TouchDisInvertString));
            }
        }

        public void UpdateTouchDisInvert(ContextMenu menu)
        {
            int index = 0;
            List<int> triggerList = new List<int>();
            List<string> triggerName = new List<string>();

            foreach (MenuItem item in menu.Items)
            {
                if (item.IsChecked)
                {
                    triggerList.Add(index);
                    triggerName.Add(item.Header.ToString());
                }

                index++;
            }

            if (triggerList.Count == 0)
            {
                triggerList.Add(-1);
                triggerName.Add("None");
            }

            touchpad.DisInvertTriggers = triggerList.ToArray();
            TouchDisInvertString = string.Join(", ", triggerName.ToArray());
        }

        public void PopulateTouchDisInver(ContextMenu menu)
        {
            int[] triggers = touchpad.DisInvertTriggers;
            int itemCount = menu.Items.Count;
            List<string> triggerName = new List<string>();
            foreach (int trigid in triggers)
            {
                if (trigid >= 0 && trigid < itemCount - 1)
                {
                    MenuItem current = menu.Items[trigid] as MenuItem;
                    current.IsChecked = true;
                    triggerName.Add(current.Header.ToString());
                }
                else if (trigid == -1)
                {
                    triggerName.Add("None");
                    break;
                }
            }

            if (triggerName.Count == 0)
            {
                triggerName.Add("None");
            }

            TouchDisInvertString = string.Join(", ", triggerName.ToArray());
        }
    }
}
