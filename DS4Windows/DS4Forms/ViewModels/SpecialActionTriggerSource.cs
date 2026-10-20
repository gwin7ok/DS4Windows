using System.Collections.Generic;

namespace DS4WinWPF.DS4Forms.ViewModels
{
    /// <summary>
    /// Phase10-Step2 W1（Phase10-Step2-Plan.md §3.1）: SA のモードのトリガーの供給元。従来の <c>SpecialActEditorViewModel</c> の
    /// <c>controlTriggerList</c>／<c>controlUnloadTriggerList</c>（名前のリスト）と、「トリガーが 1 つ以上」の検証を、そのまま持つ。
    /// 画面（チェックボックス）が、リストを直接書き換える従来の経路のために、リストを公開する（W2 でデータ駆動の部品に置き換えるまで）。
    /// </summary>
    public sealed class SpecialActionTriggerSource : IActionTriggerSource
    {
        private readonly List<string> triggers = new List<string>();
        private readonly List<string> unloadTriggers = new List<string>();

        public bool CanChange => true;

        public IReadOnlyList<string> Triggers => triggers;

        public IReadOnlyList<string> UnloadTriggers => unloadTriggers;

        /// <summary>選ばれているトリガーのリスト（画面が、直接書き換える。W2 まで）。</summary>
        public List<string> TriggerList => triggers;

        /// <summary>解除トリガーのリスト（画面が、直接書き換える。W2 まで）。</summary>
        public List<string> UnloadTriggerList => unloadTriggers;

        public bool SetTrigger(string name, bool selected) => Set(triggers, name, selected);

        public bool SetUnloadTrigger(string name, bool selected) => Set(unloadTriggers, name, selected);

        public string TriggersText => string.Join("/", triggers.ToArray());

        public string UnloadTriggersText => unloadTriggers.Count > 0 ? string.Join("/", unloadTriggers.ToArray()) : null;

        public bool Validate(out string error)
        {
            if (triggers.Count == 0)
            {
                error = "No triggers provided";
                return false;
            }

            error = null;
            return true;
        }

        private static bool Set(List<string> list, string name, bool selected)
        {
            if (selected)
            {
                if (!list.Contains(name)) list.Add(name);
            }
            else
            {
                list.Remove(name);
            }

            return true;
        }
    }
}
