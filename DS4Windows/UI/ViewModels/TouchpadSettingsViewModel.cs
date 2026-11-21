using System;
using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.Generic;
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
            ArgumentNullException.ThrowIfNull(touchpad);
            this.touchpad = touchpad;
            ArgumentNullException.ThrowIfNull(environmentService);
            this.environmentService = environmentService;
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
                    case TouchpadOutMode.Passthru:
                        index = 2; break;
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
        public event PropertyChangingHandler<TouchpadOutMode>? TouchpadOutputIndexChanging;
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
            set
            {
                if (touchpad.Trackball == value) return;
                touchpad.Trackball = value;
                OnPropertyChanged(nameof(TouchTrackball));
            }
        }

        public double TouchTrackballFriction
        {
            get => touchpad.TrackballFriction;
            set => touchpad.TrackballFriction = value;
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

        public void UpdateTouchDisInvert(IReadOnlyList<TriggerMenuEntry> entries)
        {
            int index = 0;
            List<int> triggerList = new List<int>();
            List<string> triggerName = new List<string>();

            foreach (TriggerMenuEntry item in entries)
            {
                if (item.IsChecked)
                {
                    triggerList.Add(index);
                    triggerName.Add(item.Header);
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

        /// <summary>保存されている設定をメニューのチェックへ反映する（<paramref name="entries"/> を書き換える）。</summary>
        public void PopulateTouchDisInver(IReadOnlyList<TriggerMenuEntry> entries)
        {
            int[] triggers = touchpad.DisInvertTriggers;
            int itemCount = entries.Count;
            List<string> triggerName = new List<string>();
            foreach (int trigid in triggers)
            {
                if (trigid >= 0 && trigid < itemCount - 1)
                {
                    TriggerMenuEntry current = entries[trigid];
                    current.IsChecked = true;
                    triggerName.Add(current.Header);
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
        }    }
}
