#nullable enable

namespace DS4Windows.Processing.Touchpad
{
    /// <summary>
    /// タッチパッドのスワイプ・スライドの現在の状態と、前回のフレームの状態（スワイプ、タッチ領域）。
    /// 読み取りは誰でもできる。書き込みは <see cref="TouchpadMouse"/>（同じアセンブリ）だけ。
    /// 前回の状態の更新は <see cref="SavePrior"/> の 1 か所に集める（書き漏れを防ぐ）。
    /// </summary>
    public sealed class TouchpadGestureState
    {
        public bool SwipeLeft { get; internal set; }
        public bool SwipeRight { get; internal set; }
        public bool SwipeUp { get; internal set; }
        public bool SwipeDown { get; internal set; }

        public bool PriorSwipeLeft { get; private set; }
        public bool PriorSwipeRight { get; private set; }
        public bool PriorSwipeUp { get; private set; }
        public bool PriorSwipeDown { get; private set; }

        public byte SwipeLeftB { get; internal set; }
        public byte SwipeRightB { get; internal set; }
        public byte SwipeUpB { get; internal set; }
        public byte SwipeDownB { get; internal set; }

        public byte PriorSwipeLeftB { get; private set; }
        public byte PriorSwipeRightB { get; private set; }
        public byte PriorSwipeUpB { get; private set; }
        public byte PriorSwipeDownB { get; private set; }

        public bool SlideLeft { get; internal set; }
        public bool SlideRight { get; internal set; }

        public bool PriorLeftDown { get; private set; }
        public bool PriorRightDown { get; private set; }
        public bool PriorUpperDown { get; private set; }
        public bool PriorMultiDown { get; private set; }

        /// <summary>今のフレームの状態を、前回の状態へ写す（フレームの最後に 1 回だけ呼ぶ）。</summary>
        internal void SavePrior(bool upperDown, bool leftDown, bool rightDown, bool multiDown)
        {
            PriorUpperDown = upperDown;
            PriorLeftDown = leftDown;
            PriorRightDown = rightDown;
            PriorMultiDown = multiDown;

            PriorSwipeLeft = SwipeLeft;
            PriorSwipeRight = SwipeRight;
            PriorSwipeUp = SwipeUp;
            PriorSwipeDown = SwipeDown;
            PriorSwipeLeftB = SwipeLeftB;
            PriorSwipeRightB = SwipeRightB;
            PriorSwipeUpB = SwipeUpB;
            PriorSwipeDownB = SwipeDownB;
        }
    }
}
