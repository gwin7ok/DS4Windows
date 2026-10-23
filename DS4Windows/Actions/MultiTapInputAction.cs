using System;
using System.Collections.Generic;
using DS4Windows.DI;

namespace DS4Windows.Actions
{
    /// <summary>
    /// Phase8-Step7 7.7b（Phase8-Step7-7-Plan.md §3.2。Q2＝B）: MultiAction の入力アクション（独立した入力アクション）。
    /// 成立（<see cref="TriggerState.IsEstablished"/>）は、<see cref="ComboTriggerInputAction"/>（トリガーの AND、<c>delayTime</c>）と同じ。トリガーのボタンの抑制・出力の消去を、エンジンの共通の方針で行うため。
    /// それに加えて、**毎フレーム（<see cref="Evaluate"/> を 1 フレームに 1 回呼ぶこと）**、<c>trigger[0]</c> の押下・解放のエッジ（<see cref="ITriggerInput.IsActive"/>／<see cref="ITriggerInput.WasActive"/>）と時刻
    /// （<see cref="ITriggerInput.UtcNow"/>）、<c>DebouncingMs</c> から、旧 <c>MapCustomAction</c> の MultiAction の枝と同じ状態機械を動かし、結果を <see cref="TriggerState.Gesture"/> で返す。
    ///  - 押した: 前回のタップの終わりから間隔（150ms ＋ debounce）以内なら、ダブルタップの開始。そうでなければ最初の接触。
    ///  - 離した: ダブルタップの開始中なら戻す。最初の接触なら、押した時間が間隔以内でタップ成立（時刻を保存）。
    ///  - タップ（終わりから間隔を過ぎたら）→ <see cref="TriggerState.Tap"/>。長押し（最初の接触のまま 500ms ＋ debounce を過ぎたら）→ <see cref="TriggerState.Hold"/>。ダブルタップの開始 → <see cref="TriggerState.DoubleTap"/>。
    /// 状態（旧 <c>SpecialAction</c> の <c>tappedOnce</c>／<c>firstTouch</c>／<c>secondtouchbegin</c>／<c>pastTime</c>／<c>firstTap</c>／<c>TimeofEnd</c>）は、このインスタンスが持つ（Q3＝A）。
    /// </summary>
    public sealed class MultiTapInputAction : IInputAction
    {
        private static readonly TimeSpan Interval = TimeSpan.FromMilliseconds(150);
        private static readonly TimeSpan HoldTime = TimeSpan.FromMilliseconds(500);

        private readonly ComboTriggerInputAction _combo;
        private readonly IProfileSettingsService _profileSettings;

        private bool _tappedOnce;
        private bool _firstTouch;
        private bool _secondTouchBegin;
        private DateTime _pastTime;
        private DateTime _firstTap;
        private DateTime _timeOfEnd;
        private readonly bool _digital;

        /// <param name="digitalJudgement">true（既定。SA の割り当て）なら、閾値なしのデジタルな判定（R-5）。false（Controls の Multi-action）なら、従来の閾値つきの判定。</param>
        public MultiTapInputAction(string name, IReadOnlyList<DS4Controls> triggers, IProfileSettingsService profileSettings, double delaySeconds = 0.0,
            bool digitalJudgement = true)
        {
            _profileSettings = profileSettings ?? throw new ArgumentNullException(nameof(profileSettings));
            _digital = digitalJudgement;
            _combo = new ComboTriggerInputAction(name, triggers, profileSettings, delaySeconds, digitalJudgement: digitalJudgement);
        }

        public string Name => _combo.Name;

        public IReadOnlyList<DS4Controls> Triggers => _combo.Triggers;

        public TriggerState Evaluate(ITriggerInput input)
        {
            if (input == null) throw new ArgumentNullException(nameof(input));

            TriggerState combo = _combo.Evaluate(input);
            int gesture = Advance(input);
            return gesture == TriggerState.NoGesture ? combo : new TriggerState(combo.IsEstablished, gesture);
        }

        // 旧 MapCustomAction の MultiAction の枝と同じ規則（順序も同じ）
        private int Advance(ITriggerInput input)
        {
            DS4Controls first = _combo.Triggers[0];
            bool activeCur = _digital ? input.IsDigitalActive(first) : input.IsActive(first);
            bool activePrev = _digital ? input.WasDigitalActive(first) : input.WasActive(first);
            DateTime now = input.UtcNow;
            TimeSpan debounce = TimeSpan.FromMilliseconds(_profileSettings.DebouncingMs[input.Device]);

            if (activeCur && !activePrev)
            {
                // 押した
                _pastTime = now;
                if (_pastTime <= _firstTap + Interval + debounce)
                {
                    _tappedOnce = false;
                    _secondTouchBegin = true;
                }
                else
                {
                    _firstTouch = true;
                }
            }
            else if (!activeCur && activePrev)
            {
                // 離した
                if (_secondTouchBegin)
                {
                    _firstTouch = false;
                    _secondTouchBegin = false;
                }
                else if (_firstTouch)
                {
                    _firstTouch = false;
                    if (now <= _pastTime + Interval + debounce && !_tappedOnce)
                    {
                        _tappedOnce = true;
                        _firstTap = now;
                        _timeOfEnd = now;
                    }
                }
            }

            if (_tappedOnce)
            {
                // タップ: 終わりから間隔を過ぎたら（ダブルタップの余地を残すための遅延）
                if (now - _timeOfEnd > Interval + debounce)
                {
                    _tappedOnce = false;
                    return TriggerState.Tap;
                }
            }
            else if (_firstTouch && now - _pastTime > HoldTime + debounce)
            {
                _firstTouch = false;
                return TriggerState.Hold;
            }
            else if (_secondTouchBegin)
            {
                _secondTouchBegin = false;
                return TriggerState.DoubleTap;
            }

            return TriggerState.NoGesture;
        }

        public void Reset()
        {
            _combo.Reset();
            _tappedOnce = false;
            _firstTouch = false;
            _secondTouchBegin = false;
        }
    }
}
