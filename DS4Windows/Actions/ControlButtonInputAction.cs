using System;
using System.Collections.Generic;
using DS4Windows.DI;

namespace DS4Windows.Actions
{
    /// <summary>
    /// Phase8-Step7 7.8b（Phase8-Step7-8b-Plan.md §3.2）: Controls タブの 1 ボタンの入力アクション（D2＝B1 の「要素数 1」の特殊ケース。ただし押下の判定が
    /// 特殊アクションと違う）。成立 ＝ 入力（<see cref="IControlTriggerInput"/>）が、そのボタンを押していると判定すること。入力が <see cref="IControlTriggerInput"/> でなければ未成立。
    /// 遅延・ジェスチャーはない（旧経路の Controls タブの Key にもない）。割り当て 0。
    /// Phase10-Step4 W3: Macro の「Run on trigger release」のために、離したときに成立する反転（<c>invertOnRelease</c>）を持てる
    /// （<see cref="ComboTriggerInputAction"/> と同じ規則: 押された時点で準備、離された時点で 1 回だけ成立。既定は偽で、従来と同じ）。
    /// </summary>
    public sealed class ControlButtonInputAction : IControlBoundInput
    {
        private readonly DS4Controls[] _triggers;
        private readonly bool _invertOnRelease;
        private readonly double _delaySeconds;
        private readonly IProfileSettingsService _profileSettings;
        private bool _armed;

        /// <param name="kind">所属の区分。束縛の区分（情報用）。</param>
        /// <param name="outputNature">出力の種類（Phase10-Step6 I4a）。デジタルは SA と同じ判定、アナログは Controls の従来の判定。決める箇所は <see cref="ControlOutputNatureTable"/>。</param>
        /// <param name="invertOnRelease">true なら、離したときに 1 回だけ成立する（Macro の Run on trigger release）。</param>
        /// <param name="delaySeconds">0 より大きいと、押し続けてこの秒数が経ったときに成立する（SA の <c>delayTime</c> と同じ規則）。</param>
        /// <param name="profileSettings">遅延の基準時刻の更新に使うデバウンス（<c>DebouncingMs</c>）の取得元。<paramref name="delaySeconds"/> が正のときだけ必要。</param>
        public ControlButtonInputAction(string name, DS4Controls control, ControlBindingKind kind = ControlBindingKind.Key,
            ControlOutputNature outputNature = ControlOutputNature.Analog, bool invertOnRelease = false,
            double delaySeconds = 0.0, IProfileSettingsService profileSettings = null)
        {
            if (delaySeconds > 0.0 && profileSettings == null) throw new ArgumentNullException(nameof(profileSettings));
            _delaySeconds = delaySeconds;
            _profileSettings = profileSettings;
            Name = name ?? string.Empty;
            _triggers = new[] { control };
            Kind = kind;
            OutputNature = outputNature;
            _invertOnRelease = invertOnRelease;
        }

        /// <summary>true なら、離したときに成立する（Macro の Run on trigger release）。</summary>
        public bool InvertsOnRelease => _invertOnRelease;

        /// <summary>所属の区分（7.8c）。</summary>
        public ControlBindingKind Kind { get; }

        /// <summary>出力の種類（Phase10-Step6 I4a。押下の判定はここから決まる）。</summary>
        public ControlOutputNature OutputNature { get; }

        public string Name { get; }

        public IReadOnlyList<DS4Controls> Triggers => _triggers;

        public TriggerState Evaluate(ITriggerInput input)
        {
            if (input == null) throw new ArgumentNullException(nameof(input));

            // Digital: SA のトリガーと同じ判定（Phase8-Step7-8c-Plan.md §0、Phase10-Step4c W4）。Analog: Controls の従来の判定
            bool active = OutputNature == ControlOutputNature.Digital
                ? input.IsActive(_triggers[0])
                : input is IControlTriggerInput control && control.IsControlActive(_triggers[0]);

            // Phase10-Step4 W4: 押し続ける秒数。遅延つきのときは、反転を無視する（ComboTriggerInputAction と同じ）
            if (_delaySeconds > 0.0)
                return EvaluateDelayed(input, active);

            if (!_invertOnRelease)
                return active ? TriggerState.Established : TriggerState.NotEstablished;

            if (_armed)
            {
                if (active) return TriggerState.NotEstablished;   // 押し続けている間は待つ
                _armed = false;
                return TriggerState.Established;                  // 離された: 1 回だけ成立
            }

            if (active) _armed = true;                            // 押された: 準備（成立にはしない）
            return TriggerState.NotEstablished;
        }

        // ComboTriggerInputAction.EvaluateDelayed と同じ更新の規則（基準時刻は、入力の共有の DelayClock）
        private TriggerState EvaluateDelayed(ITriggerInput input, bool active)
        {
            DelayClock clock = input.Delay;
            DateTime utcNow = input.UtcNow;

            if (active)
            {
                clock.Now = utcNow;
                return clock.Now >= clock.Old + TimeSpan.FromSeconds(_delaySeconds)
                    ? TriggerState.Established
                    : TriggerState.NotEstablished;
            }

            if (clock.Now < utcNow - TimeSpan.FromMilliseconds(100) + TimeSpan.FromMilliseconds(_profileSettings.DebouncingMs[input.Device]))
                clock.Old = utcNow;

            return TriggerState.NotEstablished;
        }

        public void Reset()
        {
            _armed = false;
        }
    }
}
