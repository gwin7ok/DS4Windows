using System;
using System.Collections.Generic;

namespace DS4WinWPF.DS4Forms.ViewModels
{
    /// <summary>
    /// Phase10-Step2 W2（Phase10-Step2-Plan.md §3.2）: トリガーの一覧。引数なしの構築は、SA のモードの全トリガー（49 項目）。並び・名前・表示名は、
    /// 従来の <c>SpecialActionEditor.xaml</c> の、名前つきのチェックボックス 49 個（と、その <c>Tag</c>／<c>Content</c>）と同じ。
    /// Controls のモード（W3）は、対象のボタン 1 つだけの一覧を、引数つきで作る。状態を持たない（不変）。
    /// </summary>
    public sealed class TriggerCatalog
    {
        private readonly TriggerItem[] items;
        private readonly HashSet<string> names;

        /// <summary>SA のモードの全トリガー。</summary>
        public TriggerCatalog()
            : this(new[]
            {
                new TriggerItem("Cross", "Cross"), new TriggerItem("Circle", "Circle"), new TriggerItem("Square", "Square"),
                new TriggerItem("Triangle", "Triangle"), new TriggerItem("Options", "Options"), new TriggerItem("Share", "Share"),
                new TriggerItem("Up", "Up"), new TriggerItem("Down", "Down"), new TriggerItem("Left", "Left"),
                new TriggerItem("Right", "Right"), new TriggerItem("PS", "PS"), new TriggerItem("Mute", "Mute"),
                new TriggerItem("Capture", "Capture"), new TriggerItem("SideL", "Side L"), new TriggerItem("SideR", "Side R"),
                new TriggerItem("L1", "L1"), new TriggerItem("R1", "R1"), new TriggerItem("L2", "L2"),
                new TriggerItem("L2 Full Pull", "L2 Full Pull"), new TriggerItem("R2", "R2"), new TriggerItem("R2 Full Pull", "R2 Full Pull"),
                new TriggerItem("L3", "L3"), new TriggerItem("R3", "R3"),
                // DualSense Edge の追加ボタン
                new TriggerItem("Function Left", "Function L"), new TriggerItem("Function Right", "Function R"),
                new TriggerItem("Bottom Left Paddle", "Paddle L"), new TriggerItem("Bottom Right Paddle", "Paddle R"),
                new TriggerItem("Left Touch", "Left Touch"), new TriggerItem("Upper Touch", "Upper Touch"),
                new TriggerItem("Multitouch", "Multitouch"), new TriggerItem("Right Touch", "Right Touch"),
                new TriggerItem("Left Stick Up", "Left Stick Up"), new TriggerItem("Left Stick Down", "Left Stick Down"),
                new TriggerItem("Left Stick Left", "Left Stick Left"), new TriggerItem("Left Stick Right", "Left Stick Right"),
                new TriggerItem("Right Stick Up", "Right Stick Up"), new TriggerItem("Right Stick Down", "Right Stick Down"),
                new TriggerItem("Right Stick Left", "Right Stick Left"), new TriggerItem("Right Stick Right", "Right Stick Right"),
                new TriggerItem("Swipe Up", "Swipe Up"), new TriggerItem("Swipe Down", "Swipe Down"),
                new TriggerItem("Swipe Left", "Swipe Left"), new TriggerItem("Swipe Right", "Swipe Right"),
                new TriggerItem("Tilt Up", "Tilt Up"), new TriggerItem("Tilt Down", "Tilt Down"),
                new TriggerItem("Tilt Left", "Tilt Left"), new TriggerItem("Tilt Right", "Tilt Right"),
                new TriggerItem("Touch Started", "Touch Started"), new TriggerItem("Touch Ended", "Touch Ended"),
            })
        {
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
