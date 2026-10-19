using System;

namespace DS4Windows.Actions
{
    /// <summary>
    /// Phase8-Step7 7.8e（Phase8-Step7-8e-Plan.md §3.2）: Controls タブの Extras（振動、ライトバーの強制色と点滅、ボタン感度の一時変更）の出力。
    /// 旧 <c>Mapping.ProcessControlSettingAction</c> の Extras の枝と同じ流れ:
    ///  - 立ち上がり（<see cref="Execute"/>）と押している間の毎フレーム（<see cref="WhileHeld"/>）: 同じ「適用」。振動が両方 0 でなければ振動を出す。
    ///    ライトバーが有効なら強制色と点滅を設定する。感度が有効なら、一時の感度がまだないときだけ設定する（1 回だけ）。全体を try で囲み、例外は握りつぶす（旧経路と同じ）。
    ///  - 離したとき（<see cref="Stop"/>）: ライトバーの強制を解除、一時の感度を戻す、振動を出していたら 0,0 に戻す（ライトバーが無効の設定でも、強制を解除する。旧経路と同じ）。
    /// 旧経路との違い（承認済み）: 旧 <c>held</c>（デバイスに 1 つ）は持たず、ボタンごとに自分の分を元に戻す（Q3＝B）。押したままのプロファイル適用・切断では、
    /// <see cref="OnReset"/> で元に戻す（Q6＝B）。値は解釈済み（<see cref="ExtrasBindingValues"/>）で受け取る。実機・静的な状態の操作は <see cref="IControlExtrasPort"/> 越し。
    /// </summary>
    public sealed class ControlExtrasOutputAction : IHeldOutputAction, IResettableOutputAction
    {
        private readonly SpecialAction _identity;
        private readonly ExtrasBindingValues _values;
        private readonly IControlExtrasPort _port;

        private bool _applied;
        private bool _rumbleActive;

        public ControlExtrasOutputAction(SpecialAction identity, ExtrasBindingValues values, IControlExtrasPort port)
        {
            _identity = identity ?? throw new ArgumentNullException(nameof(identity));
            _values = values;
            _port = port ?? throw new ArgumentNullException(nameof(port));
        }

        public string Id => string.IsNullOrEmpty(_identity.name) ? "ControlExtras" : _identity.name;

        public void Execute(IOutputContext ctx)
        {
            if (ctx == null) return;
            Apply(ctx.Device);
        }

        public void WhileHeld(IOutputContext ctx)
        {
            if (ctx == null) return;
            Apply(ctx.Device);
        }

        public void Stop(IOutputContext ctx)
        {
            if (ctx == null) return;
            Restore(ctx.Device);
        }

        public void OnReset(IOutputContext ctx)
        {
            if (ctx == null || !_applied) return;
            Restore(ctx.Device);
        }

        private void Apply(int device)
        {
            _applied = true;

            try
            {
                if (!(_values.HeavyRumble == _values.LightRumble && _values.LightRumble == 0))
                {
                    _port.SetRumble(device, (byte)_values.HeavyRumble, (byte)_values.LightRumble);
                    _rumbleActive = true;
                }

                if (_values.LightbarEnabled)
                {
                    var color = new DS4Color { red = (byte)_values.Red, green = (byte)_values.Green, blue = (byte)_values.Blue };
                    _port.ForceLightbar(device, color, (byte)_values.Flash);
                }

                if (_values.SensitivityEnabled)
                    _port.TrySetTemporarySensitivity(device, _values.Sensitivity);
            }
            catch
            {
            }
        }

        private void Restore(int device)
        {
            _applied = false;

            _port.ClearForcedLightbar(device);
            _port.RestoreSensitivity(device);

            if (_rumbleActive)
            {
                _port.SetRumble(device, 0, 0);
                _rumbleActive = false;
            }
        }
    }
}
