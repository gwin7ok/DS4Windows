namespace DS4Windows.Actions
{
    /// <summary>
    /// Phase8-Step3 W1: <see cref="IInputAction.Evaluate"/> の結果（値型。毎フレームの割り当て 0）。
    /// <see cref="Gesture"/> は、タップ・長押し・ダブルタップを返す入力アクション（MultiTapInputAction。Phase8-Step7.7）用の欄で、
    /// <c>ComboTriggerInputAction</c> は常に 0 を返す（契約を後から変えないために、先に用意した。Step3 計画書 Q1＝A）。
    /// </summary>
    public readonly struct TriggerState
    {
        /// <summary>ジェスチャーなし。</summary>
        public const int NoGesture = 0;

        /// <summary>タップ（単押し）。</summary>
        public const int Tap = 1;

        /// <summary>長押し。</summary>
        public const int Hold = 2;

        /// <summary>ダブルタップ。</summary>
        public const int DoubleTap = 3;

        public static readonly TriggerState NotEstablished = new TriggerState(false);

        public static readonly TriggerState Established = new TriggerState(true);

        public TriggerState(bool isEstablished, int gesture = NoGesture)
        {
            IsEstablished = isEstablished;
            Gesture = gesture;
        }

        /// <summary>トリガーが成立しているか。</summary>
        public bool IsEstablished { get; }

        /// <summary>ジェスチャーの種別（<see cref="NoGesture"/>、<see cref="Tap"/>、<see cref="Hold"/>、<see cref="DoubleTap"/>）。</summary>
        public int Gesture { get; }
    }
}
