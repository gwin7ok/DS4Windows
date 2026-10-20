using System;
using System.Collections.Generic;

namespace DS4WinWPF.DS4Forms.ViewModels
{
    /// <summary>
    /// Phase10-Step2 W3（Phase10-Step2-Plan.md §3.3）: Controls のモードのトリガーの供給元。割り当てを編集する対象のボタン 1 つに固定で、
    /// ユーザーは、選んで変えられない（<see cref="CanChange"/> は偽）。SA のモード（<see cref="SpecialActionTriggerSource"/>）と違い、
    /// 解除トリガーはない。
    /// </summary>
    public sealed class ControlTriggerSource : IActionTriggerSource
    {
        private static readonly string[] NoUnloadTriggers = new string[0];
        private readonly string[] triggers;

        /// <param name="triggerName">対象のボタンの名前（<see cref="TriggerCatalog"/> の項目の <c>Name</c> と同じ）。</param>
        public ControlTriggerSource(string triggerName)
        {
            if (string.IsNullOrEmpty(triggerName)) throw new ArgumentException("対象のボタンの名前が必要", nameof(triggerName));
            triggers = new[] { triggerName };
        }

        public bool CanChange => false;

        public IReadOnlyList<string> Triggers => triggers;

        public IReadOnlyList<string> UnloadTriggers => NoUnloadTriggers;

        public bool SetTrigger(string name, bool selected) => false;

        public bool SetUnloadTrigger(string name, bool selected) => false;

        public string TriggersText => triggers[0];

        public string UnloadTriggersText => null;

        public bool Validate(out string error)
        {
            error = null;
            return true;
        }
    }
}
