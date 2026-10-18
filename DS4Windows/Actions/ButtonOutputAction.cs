using System;

namespace DS4Windows.Actions
{
    /// <summary>
    /// Phase8-Step4 W2: Button 種別の出力（Phase8-Step2-Design.md §5）。毎フレームの出力を <see cref="IFrameOutputAction.ApplyFrame"/> で行う
    /// （エッジの <c>Execute</c>／<c>Stop</c> は何もしない。現行も Button はエッジの処理を持たず、Stage3 が毎フレームの出力だけを担う）。
    /// 規則は <c>ButtonSpecialActionOutput.ResolveOutputActive</c> と同じ: Press は成立中のみ ON。Toggle は立ち上がりで反転し（Fresh-press ガード中は反転しない）、
    /// ON の間は出力する。状態（<c>IsToggledOn</c>）は、Key の Toggle と同じ <c>ActionInstanceState</c>（<see cref="IManagedActionManager"/>）。
    /// まだ <c>Mapping</c> には配線していない（W4 の空配線、Step7.1 で切替）。
    /// 注意（Step7.1 へ）: 現行の Stage3 は、立ち上がり・立ち下がりと出力の組み合わせの TRACE ログ（<c>LogSpecialActionTrace</c>）を出す。
    /// ログは維持する必要がある（規約 2.3）ため、7.1 の PR でエンジンの側に引き継ぐ。
    /// </summary>
    public sealed class ButtonOutputAction : IFrameOutputAction
    {
        private readonly SpecialAction _sa;
        private readonly IManagedActionManager _manager;
        private readonly bool _hasControl;
        private readonly X360Controls _control;

        public ButtonOutputAction(SpecialAction sa, IManagedActionManager manager)
        {
            _sa = sa ?? throw new ArgumentNullException(nameof(sa));
            _manager = manager ?? throw new ArgumentNullException(nameof(manager));

            // 現行の Stage3 と同じ: details が整数として読めなければ、何も出力しない
            if (int.TryParse(sa.details, out int value))
            {
                _control = (X360Controls)value;
                _hasControl = true;
            }
        }

        public string Id => string.IsNullOrEmpty(_sa.name) ? "ButtonOutput" : _sa.name;

        public void Execute(IOutputContext ctx) { }

        public void Stop(IOutputContext ctx) { }

        public void ApplyFrame(IOutputContext ctx, bool active, bool risingEdge)
        {
            if (ctx == null) return;

            bool outputActive = ResolveOutputActive(ctx.Device, active, risingEdge);
            if (!outputActive || !_hasControl) return;

            ctx.Frame?.PressX360(_control);
        }

        private bool ResolveOutputActive(int device, bool triggered, bool risingEdge)
        {
            bool isToggle = _sa.KeyButtonSwitchMode.HasValue &&
                _sa.KeyButtonSwitchMode.Value == SpecialAction.KeyButtonSwitchModeEnum.Toggle;
            if (!isToggle) return triggered;

            ActionInstanceState state = _manager.GetStateFor(_sa, device);
            if (state == null) return triggered;

            // プロファイル適用・接続の直後の押しっぱなしでは反転しない（Fresh-press ガード）
            if (risingEdge && !state.RequiresFreshPressAfterReset)
            {
                bool next = !state.IsToggledOn;
                try
                {
                    _manager.SetToggledOn(_sa, device, next);
                }
                catch
                {
                    // 現行と同じ: 管理役が状態を更新できないときは、状態を直接更新する
                    state.IsToggledOn = next;
                }
            }

            return state.IsToggledOn;
        }
    }
}
