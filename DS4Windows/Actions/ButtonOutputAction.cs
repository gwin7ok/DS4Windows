using System;
using System.Collections.Generic;

namespace DS4Windows.Actions
{
    /// <summary>
    /// Phase8-Step4 W2: Button 種別の出力（Phase8-Step2-Design.md §5）。毎フレームの出力を <see cref="IFrameOutputAction.ApplyFrame"/> で行う
    /// （エッジの <c>Execute</c>／<c>Stop</c> は何もしない。現行も Button はエッジの処理を持たず、Stage3 が毎フレームの出力だけを担う）。
    /// 規則は <c>ButtonSpecialActionOutput.ResolveOutputActive</c> と同じ: Press は成立中のみ ON。Toggle は立ち上がりで反転し（Fresh-press ガード中は反転しない）、
    /// ON の間は出力する。状態（<c>IsToggledOn</c>）は、Key の Toggle と同じ <c>ActionInstanceState</c>（<see cref="IManagedActionManager"/>）。
    /// Phase8-Step7 7.1-W2（Q6＝A）: 現行の Stage3 と <c>Mapping.LogSpecialActionTrace</c> が出していた TRACE ログ（立ち上がり・立ち下がり、
    /// 出力の組み合わせ、立ち上がりの詳細）を、同じ文言・レベル・順序で出す（規約 2.3）。ログが無効のときは、文字列を作らない（割り当て 0）。
    /// </summary>
    public sealed class ButtonOutputAction : IFrameOutputAction
    {
        private readonly SpecialAction _sa;
        private readonly IManagedActionManager _manager;
        private readonly bool _hasControl;
        private readonly X360Controls _control;
        private readonly bool _toggle;
        private readonly int _strength;   // Phase10-Step6 後の Step5: アナログな出力（軸・トリガー・マウス移動）の強さ（0〜100%）
        private readonly AnalogOutputKind _analogKind;

        // 前のフレームの成立（旧 Mapping.lastButtonTriggerState 相当。立ち下がりのログ用）。ApplyFrame は毎フレーム、このアクションの所有する束縛で呼ばれる
        private bool _prevActive;

        // TODO(Phase8-Step8。Phase8-Step7-8a-Plan.md Q4＝A): 設定の解釈（details → X360 のボタン、スイッチモード → Toggle）を、この互換のコンストラクターが
        // ActionBindingBuilder.InterpretButton を呼んで行う。ビルダーは解釈済みの値を渡す下のコンストラクターを使う。既存のテストが使うため残す。Step8 で整理する。
        public ButtonOutputAction(SpecialAction sa, IManagedActionManager manager)
            : this(sa ?? throw new ArgumentNullException(nameof(sa)), ActionBindingBuilder.InterpretButton(sa), manager)
        {
        }

        /// <summary>
        /// Phase8-Step7 7.8a（Q9＝A）: 解釈済みの値（<paramref name="values"/>）を受け取る。<paramref name="identity"/> は、Toggle の状態の鍵とログの名前として使う
        /// （Phase8-Step7-8a-Plan.md §2.2）。現行の Stage3 と同じく、ボタンが解釈できなければ、何も出力しない。
        /// </summary>
        public ButtonOutputAction(SpecialAction identity, ButtonBindingValues values, IManagedActionManager manager)
        {
            _sa = identity ?? throw new ArgumentNullException(nameof(identity));
            _manager = manager ?? throw new ArgumentNullException(nameof(manager));
            _hasControl = values.HasControl;
            _control = values.Control;
            _toggle = values.Toggle;
            _strength = values.Strength;
            _analogKind = values.HasControl ? AnalogOutputTargets.KindOf(values.Control) : AnalogOutputKind.None;
        }

        public string Id => string.IsNullOrEmpty(_sa.name) ? "ButtonOutput" : _sa.name;

        public void Execute(IOutputContext ctx) { }

        public void Stop(IOutputContext ctx) { }

        public void ApplyFrame(IOutputContext ctx, bool active, bool risingEdge)
        {
            if (ctx == null) return;

            bool previous = _prevActive;
            _prevActive = active;
            bool trace = AppLogger.IsTraceEnabled;

            bool outputActive = ResolveOutputActive(ctx.Device, active, risingEdge);

            // 現行の Stage3 と同じ: 状態が変わったときだけ、立ち上がり・立ち下がりを記録する
            if (trace && active != previous)
            {
                if (active)
                    AppLogger.LogTrace($"Trigger detected for Button SA '{_sa.name}' on device {ctx.Device}");
                else
                    AppLogger.LogTrace($"Trigger no longer active for Button SA '{_sa.name}' on device {ctx.Device}");
            }

            if (outputActive && _hasControl)
            {
                if (_analogKind == AnalogOutputKind.None) ctx.Frame?.PressX360(_control);
                else ApplyAnalog(ctx);   // Phase10-Step6 後の Step5: 軸・トリガー・マウス移動は、強さの固定値で出す

                // 現行の Stage3 と同じ: 出力しているフレームごとに、出力の組み合わせを記録する
                if (trace) LogOutputCombo(ctx);
            }

            // 現行の LogSpecialActionTrace と同じ: 立ち上がりのときに、対応・トリガーのボタン・出力の組み合わせを記録する
            if (trace && risingEdge) LogRisingEdge(ctx);
        }

        private void LogRisingEdge(IOutputContext ctx)
        {
            try
            {
                AppLogger.LogTrace($"Trigger detected for SA '{_sa.name}' on device {ctx.Device}");

                string mapping = _hasControl
                    ? $"X360Controls {_control.ToString()} ({(int)_control})"
                    : $"Type:{_sa.type} Details:{_sa.details}";
                AppLogger.LogTrace($"SA '{_sa.name}' maps to {mapping}");

                // 現行の LogSpecialActionTrace は、状態を渡さずに getBoolSpecialActionMapping を呼ぶため、例外（握りつぶし）となり、常に "<none>" を記録している
                // （旧経路の既存の挙動。ログの文言を変えない方針［Q6＝A］により、同じ出力にする。成立しているトリガーの一覧を出す改善は、別途ユーザーの承認が要る）
                AppLogger.LogTrace($"Trigger keys for SA '{_sa.name}': <none>");

                try
                {
                    var parts = new List<string>();
                    (ctx.Frame as IFrameOutputTrace)?.AppendOutputCombo(parts);
                    string combo = parts.Count > 0 ? string.Join(", ", parts) : "<none>";
                    AppLogger.LogTrace($"SA '{_sa.name}' output combo: {combo}");
                }
                catch { }
            }
            catch { }
        }

        private void LogOutputCombo(IOutputContext ctx)
        {
            try
            {
                var parts = new List<string>();
                (ctx.Frame as IFrameOutputTrace)?.AppendOutputCombo(parts);
                string combo = parts.Count > 0 ? string.Join(", ", parts) : "<none>";
                AppLogger.LogTrace($"SA '{_sa.name}' output combo: {combo}");
            }
            catch (Exception ex)
            {
                AppLogger.LogTrace($"Failed to build output combo log for SA '{_sa.name}': {ex}");
            }
        }

        // Phase10-Step6 後の Step5（SA 側のアナログ出力）: 成立中（または Toggle が ON の間）、強さ（0〜100%）の固定値で出す。Controls のボタンが元のときの出力を 100% として、比例させる。
        //  - 軸: 中立（128）から、向きの端（正は 255、負は 0）へ、強さの分だけ振る。向きは、出力先の添字が偶数なら正（Controls のパッドへの割り当てと同じ）
        //  - トリガー: 255 × 強さ
        //  - マウス移動: Controls のボタンが元のときと同じ量 × 強さ（IFixedMouseInput）
        private void ApplyAnalog(IOutputContext ctx)
        {
            double fraction = _strength / 100.0;
            switch (_analogKind)
            {
                case AnalogOutputKind.PadAxis:
                    {
                        var frame = ctx.Frame as IPadFrameOutput;
                        if (frame == null) return;

                        DS4Controls output = X360ControlTables.reverseX360ButtonMapping[(int)_control];
                        bool positive = ((int)output) % 2 == 0;
                        byte value = positive
                            ? (byte)Math.Round(128 + (255 - 128) * fraction)
                            : (byte)Math.Round(128 - 128 * fraction);
                        frame.SetAxisPair(output, value);
                        break;
                    }
                case AnalogOutputKind.PadTrigger:
                    {
                        var frame = ctx.Frame as IPadFrameOutput;
                        if (frame == null) return;

                        frame.SetTrigger(X360ControlTables.reverseX360ButtonMapping[(int)_control], (byte)Math.Round(255 * fraction));
                        break;
                    }
                case AnalogOutputKind.MouseMove:
                    {
                        var frame = ctx.Frame as IMouseMoveFrameOutput;
                        var input = (ctx as IOutputContextInput)?.Input as IFixedMouseInput;
                        if (frame == null || input == null) return;

                        int direction = _control == X360Controls.MouseUp ? 0
                            : _control == X360Controls.MouseDown ? 1
                            : _control == X360Controls.MouseLeft ? 2 : 3;
                        double magnitude = Math.Abs(input.GetFixedMouseMovement(direction, fraction));
                        switch (direction)
                        {
                            case 0: frame.SetMouseDeltaY(-magnitude); break;
                            case 1: frame.SetMouseDeltaY(magnitude); break;
                            case 2: frame.SetMouseDeltaX(-magnitude); break;
                            default: frame.SetMouseDeltaX(magnitude); break;
                        }

                        break;
                    }
            }
        }

        private bool ResolveOutputActive(int device, bool triggered, bool risingEdge)
        {
            if (!_toggle) return triggered;

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
