using System;
using System.Collections.Generic;

namespace DS4Windows.Actions
{
    /// <summary>
    /// Phase8-Step7 7.8b（Phase8-Step7-8b-Plan.md §3.2）: Controls タブの 1 ボタンの入力アクション（D2＝B1 の「要素数 1」の特殊ケース。ただし押下の判定が
    /// 特殊アクションと違う）。成立 ＝ 入力（<see cref="IControlTriggerInput"/>）が、そのボタンを押していると判定すること。入力が <see cref="IControlTriggerInput"/> でなければ未成立。
    /// 遅延・反転・ジェスチャーはない（旧経路の Controls タブの Key にもない）。割り当て 0。
    /// </summary>
    public sealed class ControlButtonInputAction : IInputAction
    {
        private readonly DS4Controls[] _triggers;

        /// <param name="kind">所属の区分。束縛の区分（情報用）。</param>
        /// <param name="judgement">押下の判定の種類（Key は Assignment、Macro は SA と同じ Trigger）。</param>
        public ControlButtonInputAction(string name, DS4Controls control, ControlBindingKind kind = ControlBindingKind.Key,
            ControlJudgement judgement = ControlJudgement.Assignment)
        {
            Name = name ?? string.Empty;
            _triggers = new[] { control };
            Kind = kind;
            Judgement = judgement;
        }

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
            return active ? TriggerState.Established : TriggerState.NotEstablished;
        }

        public void Reset()
        {
        }
    }
}
