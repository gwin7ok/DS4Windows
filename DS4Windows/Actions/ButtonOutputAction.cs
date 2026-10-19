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

        // 前のフレームの成立（旧 Mapping.lastButtonTriggerState 相当。立ち下がりのログ用）。ApplyFrame は毎フレーム、このアクションの所有する束縛で呼ばれる
        private bool _prevActive;

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
                ctx.Frame?.PressX360(_control);

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
