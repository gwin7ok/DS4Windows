using System;

namespace DS4Windows.Actions
{
    /// <summary>
    /// Phase8-Step7 7.8f-2（Phase8-Step7-8f-Plan.md §3.2）: Controls タブの Button のうち、マウスのボタン（左・右・中・4・5）とデジタルのホイール（元がボタン）の出力。
    /// 旧経路は、押されているフレームごとに、デバイスの合成状態のクリックのカウンターを増やしていた。同じく、成立している間、毎フレーム
    /// <see cref="IFrameOutput.PressX360"/> でカウンターを増やす（<c>Commit</c> が立ち上がり・立ち下がりを見る）。
    /// 割り当てたボタンの通常の出力を消すのは、束縛の方針（<c>ErasesOutputEveryFrame</c>）。
    /// </summary>
    public sealed class ControlMouseOutputAction : IFrameOutputAction
    {
        private readonly SpecialAction _identity;
        private readonly X360Controls _target;

        public ControlMouseOutputAction(SpecialAction identity, X360Controls target)
        {
            _identity = identity ?? throw new ArgumentNullException(nameof(identity));
            _target = target;
        }

        public string Id => string.IsNullOrEmpty(_identity.name) ? "ControlMouse" : _identity.name;

        public void Execute(IOutputContext ctx) { }

        public void Stop(IOutputContext ctx) { }

        public void ApplyFrame(IOutputContext ctx, bool active, bool risingEdge)
        {
            if (!active || ctx == null) return;

            ctx.Frame?.PressX360(_target);
        }
    }
}
