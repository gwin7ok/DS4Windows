using System;
using DS4Windows.DI;
using DS4Windows.DS4Control;

namespace DS4Windows.Actions
{
    /// <summary>
    /// Phase8-Step7 7.8f-3（Phase8-Step7-8f-Plan.md §3.2。Q5＝B）: Controls タブの Button のうち、元がスティック・トリガー・ジャイロのアナログのホイール（WUP／WDOWN）の出力。
    /// 旧経路の <c>GetMouseWheelMapping</c> と同じ計算: 10ms ごとに、元のバイト値から量（0.05〜1.0 を 3 で割った値）を求め、端数を持ち越し、1 以上になったら整数分だけホイールを送る。
    /// <b>Q5＝B（承認済みの動作変更）</b>: 旧経路は、端数・向き・間隔の基準時刻が Mapping 全体で 1 つ（<c>stickWheelRemainder</c>、<c>stickWheelDownDir</c>、<c>oldnow</c>。
    /// デジタルのホイールの繰り返し［<c>Commit</c> の 150ms］とも共有）だった。新経路は、束縛ごとに持つ。向きは束縛ごとに固定のため、向きの変更で端数を捨てる処理はない。
    /// 成立している間だけ進む（<see cref="IFrameOutputAction"/>）。離している間も、持ち越した端数は残る（旧経路と同じ）。
    /// </summary>
    public sealed class ControlAnalogWheelOutputAction : IFrameOutputAction
    {
        private const double IntervalMs = 10.0;

        private readonly SpecialAction _identity;
        private readonly DS4Controls _source;
        private readonly bool _down;
        private readonly IProfileSettingsService _profileSettings;

        private DateTime _last = DateTime.MinValue;
        private double _remainder;

        public ControlAnalogWheelOutputAction(SpecialAction identity, DS4Controls source, bool down, IProfileSettingsService profileSettings)
        {
            _identity = identity ?? throw new ArgumentNullException(nameof(identity));
            _source = source;
            _down = down;
            _profileSettings = profileSettings ?? throw new ArgumentNullException(nameof(profileSettings));
        }

        public string Id => string.IsNullOrEmpty(_identity.name) ? "ControlAnalogWheel" : _identity.name;

        public void Execute(IOutputContext ctx) { }

        public void Stop(IOutputContext ctx) { }

        public void ApplyFrame(IOutputContext ctx, bool active, bool risingEdge)
        {
            if (!active || ctx == null) return;

            var input = (ctx as IOutputContextInput)?.Input as IAnalogTriggerInput;
            if (input == null) return;

            DateTime now = input.UtcNow;
            if (now < _last + TimeSpan.FromMilliseconds(IntervalMs)) return;
            _last = now;

            byte value = input.GetByteValue(_source);
            VirtualKBMMapping mapping = _profileSettings.OutputKBMMapping;
            int wheelDir = _down ? mapping.WHEEL_TICK_DOWN : mapping.WHEEL_TICK_UP;
            double ratio = (1.0 - 0.05) * (value / 255.0) + 0.05;

            // 3 回で 1 目盛り（端数は持ち越す）
            double wheel = ratio / 3.0 + _remainder;
            if (wheel >= 1.0)
            {
                int wheelTravel = (int)wheel * wheelDir;
                ctx.OutputHandler?.PerformMouseWheelEvent(wheelTravel, 0);
                _remainder = wheel - (int)wheel;
            }
            else
            {
                _remainder = wheel;
            }
        }
    }
}
