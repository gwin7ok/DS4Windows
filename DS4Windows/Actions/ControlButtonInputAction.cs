using System;
using System.Collections.Generic;

namespace DS4Windows.Actions
{
    /// <summary>
    /// Phase8-Step7 7.8b（Phase8-Step7-8b-Plan.md §3.2）: Controls タブの 1 ボタンの入力アクション（D2＝B1 の「要素数 1」の特殊ケース。ただし押下の判定が
    /// 特殊アクションと違う）。成立 ＝ 入力（<see cref="IControlTriggerInput"/>）が、そのボタンを押していると判定すること。入力が <see cref="IControlTriggerInput"/> でなければ未成立。
    /// 遅延・ジェスチャーはない（旧経路の Controls タブの Key にもない）。割り当て 0。
    /// Phase10-Step4 W3: Macro の「Run on trigger release」のために、離したときに成立する反転（<c>invertOnRelease</c>）を持てる
    /// （<see cref="ComboTriggerInputAction"/> と同じ規則: 押された時点で準備、離された時点で 1 回だけ成立。既定は偽で、従来と同じ）。
    /// </summary>
    public sealed class ControlButtonInputAction : IInputAction
    {
        private readonly DS4Controls[] _triggers;
        private readonly bool _invertOnRelease;
        private bool _armed;

        /// <param name="kind">所属の区分。束縛の区分（情報用）。</param>
        /// <param name="judgement">押下の判定の種類（Key は Assignment、Macro は SA と同じ Trigger）。</param>
        /// <param name="invertOnRelease">true なら、離したときに 1 回だけ成立する（Macro の Run on trigger release）。</param>
        public ControlButtonInputAction(string name, DS4Controls control, ControlBindingKind kind = ControlBindingKind.Key,
            ControlJudgement judgement = ControlJudgement.Assignment, bool invertOnRelease = false)
        {
            Name = name ?? string.Empty;
            _triggers = new[] { control };
            Kind = kind;
            Judgement = judgement;
            _invertOnRelease = invertOnRelease;
        }

        /// <summary>true なら、離したときに成立する（Macro の Run on trigger release）。</summary>
        public bool InvertsOnRelease => _invertOnRelease;

        /// <summary>所属の区分（7.8c）。</summary>
        public ControlBindingKind Kind { get; }

        /// <summary>押下の判定の種類（7.8c）。</summary>
        public ControlJudgement Judgement { get; }

        public string Name { get; }

        public IReadOnlyList<DS4Controls> Triggers => _triggers;

        public TriggerState Evaluate(ITriggerInput input)
        {
            if (input == null) throw new ArgumentNullException(nameof(input));

            // Trigger: SA のトリガーと同じ判定（Controls の Macro。Phase8-Step7-8c-Plan.md §0）。Assignment: Controls の Key／Button の判定
            bool active = Judgement == ControlJudgement.Trigger
                ? input.IsActive(_triggers[0])
                : input is IControlTriggerInput control && control.IsControlActive(_triggers[0]);

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

        public void Reset()
        {
            _armed = false;
        }
    }
}
