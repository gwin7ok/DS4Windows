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

        public ControlButtonInputAction(string name, DS4Controls control)
        {
            Name = name ?? string.Empty;
            _triggers = new[] { control };
        }

        public string Name { get; }

        public IReadOnlyList<DS4Controls> Triggers => _triggers;

        public TriggerState Evaluate(ITriggerInput input)
        {
            if (input == null) throw new ArgumentNullException(nameof(input));

            return input is IControlTriggerInput control && control.IsControlActive(_triggers[0])
                ? TriggerState.Established
                : TriggerState.NotEstablished;
        }

        public void Reset()
        {
        }
    }
}
