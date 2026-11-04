using System;
using System.Collections.Generic;

namespace DS4WinWPF.DS4Forms.ViewModels
{
    /// <summary>
    /// Phase10-Step2 W2（Phase10-Step2-Plan.md §3.2）: トリガーの一覧。引数なしの構築は、SA のモードの全トリガー（51 項目）。並び・名前・表示名は、
    /// 従来の <c>AssignmentEditor.xaml</c> の、名前つきのチェックボックス 49 個（と、その <c>Tag</c>／<c>Content</c>）と同じ。
    /// Phase10-Step4c W5（項目 9、ユーザー承認 2026-10-21）: 末尾に LS Outer／RS Outer を足した（Controls の一覧と入力の集合を同じにするため。既存の並びは変えない）。
    /// Controls のモード（W3）は、対象のボタン 1 つだけの一覧を、引数つきで作る。状態を持たない（不変）。
    /// </summary>
    public sealed class TriggerCatalog
    {
        private readonly TriggerItem[] items;
        private readonly HashSet<string> names;

        /// <summary>SA のモードの全トリガー。</summary>
        public TriggerCatalog()
            : this(FromInputControls())
        {
        }

        // Phase9 UI 変更: 並びと表示名は、InputControlCatalog（Controls の一覧と共通）。保存する名前（Name）は、従来のまま
        private static TriggerItem[] FromInputControls()
        {
            var result = new TriggerItem[InputControlCatalog.Entries.Count];
            for (int i = 0; i < result.Length; i++)
            {
                result[i] = new TriggerItem(InputControlCatalog.Entries[i].Name, InputControlCatalog.Entries[i].Label);
            }

            return result;
        }

        /// <summary>指定の項目の一覧（Controls のモードの、対象のボタン 1 つだけ、など）。</summary>
        public TriggerCatalog(IReadOnlyList<TriggerItem> items)
        {
            if (items == null) throw new ArgumentNullException(nameof(items));

            this.items = new TriggerItem[items.Count];
            names = new HashSet<string>(StringComparer.Ordinal);
            for (int i = 0; i < items.Count; i++)
            {
                this.items[i] = items[i] ?? throw new ArgumentException("null の項目は指定できない", nameof(items));
                names.Add(items[i].Name);
            }
        }

        /// <summary>全項目（画面に出す順）。</summary>
        public IReadOnlyList<TriggerItem> Items => items;

        /// <summary>この名前が、一覧にあるか（大文字・小文字を区別する）。</summary>
        public bool Contains(string name) => name != null && names.Contains(name);
    }
}
