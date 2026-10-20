using System;

namespace DS4Windows.Actions
{
    /// <summary>
    /// Phase8-Step7 7.8f-3（Phase8-Step7-8f-Plan.md §3.2。Q6＝A）: Controls タブの Button のうち、マウスの移動（MouseUp／Down／Left／Right）への割り当ての出力。
    /// 旧経路は、dcs を処理するごとに <c>getMouseMapping</c>（感度・加速・デッドゾーン・経過時間を使うアナログの量。押下の判定なし）を呼び、
    /// 局所変数 <c>tempMouseDeltaX／Y</c> が 0 のときだけ、符号をそろえた値を書いた。同じく、量は <see cref="IAnalogTriggerInput.GetMouseMovement"/> で借り、
    /// 加算器（<see cref="IMouseMoveFrameOutput"/>）へ「0 のときだけ」書く。評価されたフレームだけ呼ばれる（<see cref="IEvaluatedFrameOutputAction"/>）。
    /// </summary>
    public sealed class ControlMouseMoveOutputAction : IEvaluatedFrameOutputAction
    {
        private readonly SpecialAction _identity;
        private readonly DS4Controls _source;
        private readonly int _direction;   // 0＝Up、1＝Down、2＝Left、3＝Right（旧 getMouseMapping の mnum）

        public ControlMouseMoveOutputAction(SpecialAction identity, DS4Controls source, int direction)
        {
            _identity = identity ?? throw new ArgumentNullException(nameof(identity));
            _source = source;
            _direction = direction;
        }

        public string Id => string.IsNullOrEmpty(_identity.name) ? "ControlMouseMove" : _identity.name;

        public void Execute(IOutputContext ctx) { }

        public void Stop(IOutputContext ctx) { }

        public void ApplyEvaluatedFrame(IOutputContext ctx, bool established)
        {
            if (ctx == null) return;

            var frame = ctx.Frame as IMouseMoveFrameOutput;
            var input = (ctx as IOutputContextInput)?.Input as IAnalogTriggerInput;
            if (frame == null || input == null) return;

            double raw = input.GetMouseMovement(_source, _direction);
            // 旧: Math.Abs((value == -2147483648 ? 0 : value)) に、向きの符号を付ける
            double magnitude = Math.Abs(raw == -2147483648 ? 0 : raw);
            switch (_direction)
            {
                case 0: frame.SetMouseDeltaY(-magnitude); break;
                case 1: frame.SetMouseDeltaY(magnitude); break;
                case 2: frame.SetMouseDeltaX(-magnitude); break;
                case 3: frame.SetMouseDeltaX(magnitude); break;
                default: break;
            }
        }
    }
}
