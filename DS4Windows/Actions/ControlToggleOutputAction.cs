using System;

namespace DS4Windows.Actions
{
    /// <summary>
    /// Phase8-Step7 7.8f-2（Phase8-Step7-8f-Plan.md §3.2。Q8＝A）: Controls タブの Button の Toggle つきの割り当ての、Toggle の部分。
    /// 旧経路は、押している間、<c>GetIsToggledOn(keyvalue, null, device)</c> が常に偽を返すため、毎フレーム <c>currentClicks.toggle</c> を反転していた
    /// （予測できない動き）。Q8＝A により、SA の Button の Toggle と同じく、<b>立ち上がりで 1 回だけ</b>反転する（<see cref="Execute"/>）。
    /// <c>toggleCount</c> は、旧経路と同じく、押している間、毎フレーム数える（<see cref="ApplyFrame"/>。<c>Commit</c> が 0 から非 0 になる立ち上がりで、
    /// <c>toggle</c> の値により、押す・離すを決める）。割り当て先の出力（マウスのカウンターやパッド）は、同じ束縛の別の出力が行う。
    /// </summary>
    public sealed class ControlToggleOutputAction : IFrameOutputAction
    {
        private readonly SpecialAction _identity;

        public ControlToggleOutputAction(SpecialAction identity)
        {
            _identity = identity ?? throw new ArgumentNullException(nameof(identity));
        }

        public string Id => string.IsNullOrEmpty(_identity.name) ? "ControlToggle" : _identity.name;

        public void Execute(IOutputContext ctx)
        {
            (ctx?.Frame as IToggleFrameOutput)?.FlipToggle();
        }

        public void Stop(IOutputContext ctx) { }

        public void ApplyFrame(IOutputContext ctx, bool active, bool risingEdge)
        {
            if (!active || ctx == null) return;

            (ctx.Frame as IToggleFrameOutput)?.CountToggle();
        }
    }
}
