using System;
using System.Collections.Generic;

namespace DS4Windows.Actions
{
    /// <summary>
    /// Phase8-Step3 W1: 統一トリガーディスパッチの束縛（不変）。プロファイルの読込・適用の完了後に <c>IActionBindingBuilder</c>（Step5）が組み立て、
    /// <c>IActionRegistry</c> に不変のリストとして登録される。実行時の状態は持たない（<c>ActionInstanceState</c> に残す。Q2＝A）。
    /// </summary>
    public sealed class TriggerBinding : IActionBinding
    {
        private readonly IOutputAction[] _outputs;

        public TriggerBinding(IInputAction input, IReadOnlyList<IOutputAction> outputs, BindingPolicy policy, SpecialAction source, int index = -1)
        {
            Input = input ?? throw new ArgumentNullException(nameof(input));
            if (outputs == null) throw new ArgumentNullException(nameof(outputs));
            Policy = policy ?? throw new ArgumentNullException(nameof(policy));
            Source = source;
            Index = index;

            _outputs = new IOutputAction[outputs.Count];
            for (int i = 0; i < _outputs.Length; i++)
                _outputs[i] = outputs[i] ?? throw new ArgumentException("outputs に null を含められない", nameof(outputs));
        }

        public IInputAction Input { get; }

        public IReadOnlyList<IOutputAction> Outputs => _outputs;

        public BindingPolicy Policy { get; }

        public SpecialAction Source { get; }

        public int Index { get; }
    }
}
