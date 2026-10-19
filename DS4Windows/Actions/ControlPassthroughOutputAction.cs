using System;

namespace DS4Windows.Actions
{
    /// <summary>
    /// Phase8-Step7 7.8f-1（Phase8-Step7-8f-Plan.md §3.2。Q2＝A）: Default の軸の素通しの出力。
    /// 割り当てたボタンの通常の出力の消去は、軸のペアの両方の向きを中立（128）にする。その反対側の向きが Default のとき、旧経路は、Default 側の値を
    /// 出力へ書き直していた（<c>customMapQueue</c> に <c>(control, control)</c> を積む）。この出力が同じことをする: 軸の値を読み、128 でなければ、軸のペアの両方に書く。
    /// ビルダーは、反対側の向きが割り当て済みの軸にだけ、この束縛を作る（割り当てのないペアでは、入力と同じ値を書き直すだけで、結果が変わらないため）。
    /// </summary>
    public sealed class ControlPassthroughOutputAction : IEvaluatedFrameOutputAction
    {
        private readonly SpecialAction _identity;
        private readonly DS4Controls _control;

        public ControlPassthroughOutputAction(SpecialAction identity, DS4Controls control)
        {
            _identity = identity ?? throw new ArgumentNullException(nameof(identity));
            _control = control;
        }

        public string Id => string.IsNullOrEmpty(_identity.name) ? "ControlPassthrough" : _identity.name;

        public void Execute(IOutputContext ctx) { }

        public void Stop(IOutputContext ctx) { }

        public void ApplyEvaluatedFrame(IOutputContext ctx, bool established)
        {
            if (ctx == null) return;

            var frame = ctx.Frame as IPadFrameOutput;
            var input = (ctx as IOutputContextInput)?.Input as IAnalogTriggerInput;
            if (frame == null || input == null) return;

            byte value = input.GetAxisValue(_control, ((int)_control) % 2 == 0);
            if (value != 128) frame.SetAxisPair(_control, value);
        }
    }
}
