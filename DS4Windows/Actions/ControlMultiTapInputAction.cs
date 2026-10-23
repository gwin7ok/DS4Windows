using System;
using System.Collections.Generic;
using DS4Windows.DI;

namespace DS4Windows.Actions
{
    /// <summary>
    /// Phase10-Step6 W8 相当: Controls タブの Multi-action（ボタン 1 つの、タップ・長押し・ダブルタップ）の入力アクション。
    /// 判定は SA の <see cref="MultiTapInputAction"/>（押下の判定は SA と同じ、閾値なしのデジタルな判定［R-5］、タップの間隔、長押しの時間）にそのまま任せる。Controls の束縛としてエンジンの
    /// ボタンごとの処理の表に載せるための印（<see cref="IControlBoundInput"/>）を持つだけ。
    /// </summary>
    public sealed class ControlMultiTapInputAction : IControlBoundInput
    {
        private readonly MultiTapInputAction _inner;

        public ControlMultiTapInputAction(string name, DS4Controls control, IProfileSettingsService profileSettings, double delaySeconds = 0.0)
        {
            // R-5: SA の MultiAction と同じ、閾値なしのデジタルな判定（既定）
            _inner = new MultiTapInputAction(name, new[] { control }, profileSettings, delaySeconds);
        }

        public string Name => _inner.Name;

        public IReadOnlyList<DS4Controls> Triggers => _inner.Triggers;

        public TriggerState Evaluate(ITriggerInput input) => _inner.Evaluate(input);

        public void Reset() => _inner.Reset();
    }
}
