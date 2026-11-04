using System;

namespace DS4Windows.Actions
{
    /// <summary>
    /// Phase8-Step7 7.8f-1（Phase8-Step7-8f-Plan.md §3.2）: Controls タブの Button のうち、パッド（X360 の軸・トリガー・ボタン）への割り当ての出力。
    /// 現行の <c>customMapQueue</c> の処理（全 dcs のあと）と同じ規則で、出力の表へ書く。評価されたフレームだけ、毎フレーム呼ばれる（<see cref="IEvaluatedFrameOutputAction"/>）:
    ///  - 軸: 元の値を <see cref="IAnalogTriggerInput.GetAxisValue"/> で読み（出力先の添字が偶数なら正の向き）、128（中立）でなければ、軸のペアの両方に書く。
    ///  - トリガー: 元の値を <see cref="IAnalogTriggerInput.GetByteValue"/> で読み、0 でなければ書く。
    ///  - ボタン: 元が押されていれば（SA と同じ判定。<see cref="ITriggerInput.IsActive"/>。Q3＝A）、ON にする。
    /// 割り当てたボタンの通常の出力を消すのは、この出力ではなく、束縛の方針（<c>ErasesOutputEveryFrame</c>）。出力先が Unbound／None のときは、この出力を持たない束縛（消すだけ）になる。
    /// </summary>
    public sealed class ControlPadOutputAction : IEvaluatedFrameOutputAction
    {
        private readonly SpecialAction _identity;
        private readonly PadBindingValues _values;

        public ControlPadOutputAction(SpecialAction identity, PadBindingValues values)
        {
            _identity = identity ?? throw new ArgumentNullException(nameof(identity));
            _values = values;
        }

        public string Id => string.IsNullOrEmpty(_identity.name) ? "ControlPad" : _identity.name;

        public void Execute(IOutputContext ctx) { }

        public void Stop(IOutputContext ctx) { }

        public void ApplyEvaluatedFrame(IOutputContext ctx, bool established)
        {
            if (ctx == null) return;

            var frame = ctx.Frame as IPadFrameOutput;
            var input = (ctx as IOutputContextInput)?.Input as IAnalogTriggerInput;
            if (frame == null || input == null) return;

            switch (_values.Kind)
            {
                case PadTargetKind.Axis:
                    {
                        // 旧: alt ＝ 出力先が軸で、添字が偶数（正の向き）
                        byte value = input.GetAxisValue(_values.Source, ((int)_values.Output) % 2 == 0);
                        if (value != 128) frame.SetAxisPair(_values.Output, value);
                        break;
                    }
                case PadTargetKind.Trigger:
                    {
                        byte value = input.GetByteValue(_values.Source);
                        if (value != 0) frame.SetTrigger(_values.Output, value);
                        break;
                    }
                default:
                    {
                        if (input.IsDigitalActive(_values.Source)) frame.SetButton(_values.Output);   // R-5: デジタルな出力先は、閾値なしの判定
                        break;
                    }
            }
        }
    }
}
