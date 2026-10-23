using System;

namespace DS4Windows.Actions
{
    /// <summary>
    /// Phase8-Step7 7.8f-2（Phase8-Step7-8f-Plan.md §3.2）: Controls タブの Button のうち、タッチパッドクリックへの割り当ての出力。
    /// 旧経路は、他のマウスのボタンと違い、Controls の判定（<c>GetBoolActionMapping</c>）ではなく、<c>GetBoolMapping</c>（スティック ±55、トリガー 100 超の SA 風の判定）で押下を見た。
    /// 同じく、<see cref="ITriggerInput.IsActive"/>（SA と同じ判定。Q3＝A の考え方。差はジャイロの閾値だけ）で見る。
    /// 評価されたフレームだけ呼ばれる（<see cref="IEvaluatedFrameOutputAction"/>）。
    /// </summary>
    public sealed class ControlTouchpadClickOutputAction : IEvaluatedFrameOutputAction
    {
        private readonly SpecialAction _identity;
        private readonly DS4Controls _source;

        public ControlTouchpadClickOutputAction(SpecialAction identity, DS4Controls source)
        {
            _identity = identity ?? throw new ArgumentNullException(nameof(identity));
            _source = source;
        }

        public string Id => string.IsNullOrEmpty(_identity.name) ? "ControlTouchpadClick" : _identity.name;

        public void Execute(IOutputContext ctx) { }

        public void Stop(IOutputContext ctx) { }

        public void ApplyEvaluatedFrame(IOutputContext ctx, bool established)
        {
            if (ctx == null) return;

            var input = (ctx as IOutputContextInput)?.Input as IAnalogTriggerInput;
            if (input == null) return;

            if (input.IsDigitalActive(_source)) ctx.Frame?.PressX360(X360Controls.TouchpadClick);   // R-5: デジタルな出力先は、閾値なしの判定
        }
    }
}
