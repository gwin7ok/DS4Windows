using DS4Windows.Core.Abstractions;
using DS4Windows.Core.Model;

namespace DS4Windows.Core.Settings
{
    /// <summary>
    /// 1 つのスロットのTouchpadの設定の持ち主（値そのもの）。<see cref="BackingStore"/> がスロットごとに 1 つ持ち、
    /// 保存・読み込み（プロファイルの XML）、実行時の入力処理、設定画面の ViewModel が、同じ 1 つのオブジェクトを読み書きする。
    /// </summary>
    public class TouchpadSlotSettings
    {
        public TouchpadSlotSettings(int slot)
        {
            Slot = slot;
            RelMouse = new TouchpadRelMouseSettings();
            MouseStick = new TouchMouseStickInfo();
            OutputMode = BackingStore.DEFAULT_TOUCH_OUT_MODE;
            ButtonMode = new TouchButtonActivationMode();
            TouchSensitivity = BackingStore.DEFAULT_TOUCHPAD_SENS;
            ScrollSensitivity = 0;
            TapSensitivity = 0;
            DoubleTap = false;
            JitterCompensation = BackingStore.DEFAULT_TOUCHPAD_JITTER_COMP;
            Invert = 0;
            LowerRightAsRightClick = false;
            ClickPassthru = false;
            StartOff = false;
            Trackball = BackingStore.DEFAULT_TRACKBALL_MODE;
            TrackballFriction = BackingStore.DEFAULT_TRACKBALL_FRICTION;
            DisInvertTriggers = new int[1] { BackingStore.DEFAULT_TOUCH_DIS_INVERT_TRIGGER};
        }

        public int Slot { get; }

        public TouchpadOutMode OutputMode { get; set; }
        public TouchButtonActivationMode ButtonMode { get; set; }
        public byte TouchSensitivity { get; set; }
        public int ScrollSensitivity { get; set; }
        public byte TapSensitivity { get; set; }
        public bool DoubleTap { get; set; }
        public bool JitterCompensation { get; set; }
        public int Invert { get; set; }
        public bool LowerRightAsRightClick { get; set; }
        public bool ClickPassthru { get; set; }
        public bool StartOff { get; set; }
        public bool Trackball { get; set; }
        public double TrackballFriction { get; set; }
        public int[] DisInvertTriggers { get; set; }
        public TouchpadRelMouseSettings RelMouse { get; }
        public TouchMouseStickInfo MouseStick { get; }
    }
}
