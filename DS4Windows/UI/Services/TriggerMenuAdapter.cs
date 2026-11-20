#nullable enable

using System.Collections.Generic;
using System.Windows.Controls;
using DS4Windows.UI.ViewModels;

namespace DS4Windows.UI.Services
{
    /// <summary>トリガーを選ぶ WPF のメニューと、ViewModel が扱う <see cref="TriggerMenuEntry"/> の並びを、行き来させる（View 側の部品）。</summary>
    public static class TriggerMenuAdapter
    {
        /// <summary>見出しを持つ項目だけを、順に <see cref="TriggerMenuEntry"/> へ写す。</summary>
        public static List<TriggerMenuEntry> Read(ContextMenu menu)
        {
            var entries = new List<TriggerMenuEntry>();
            foreach (MenuItem item in menu.Items)
                entries.Add(new TriggerMenuEntry(item.Header?.ToString(), item.IsChecked));
            return entries;
        }

        /// <summary><paramref name="entries"/> のチェックを、同じ順番のメニューの項目へ書き戻す。</summary>
        public static void Write(ContextMenu menu, IReadOnlyList<TriggerMenuEntry> entries)
        {
            for (int i = 0; i < entries.Count; i++)
                ((MenuItem)menu.Items[i]).IsChecked = entries[i].IsChecked;
        }

        /// <summary>チェックできる項目を、見出しの並びから作ってメニューへ足す（クリックは <paramref name="onClick"/> へ）。</summary>
        public static void AddItems(ContextMenu menu, IEnumerable<string> headers, System.Windows.RoutedEventHandler onClick)
        {
            foreach (string header in headers)
            {
                var item = new MenuItem { Header = header, IsCheckable = true };
                item.Click += onClick;
                menu.Items.Add(item);
            }
        }
    }
}
