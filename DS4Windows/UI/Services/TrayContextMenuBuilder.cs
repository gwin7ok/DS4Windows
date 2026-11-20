#nullable enable

using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using DS4Windows.UI.ViewModels;

namespace DS4Windows.UI.Services
{
    /// <summary>
    /// <see cref="TrayMenuItemModel"/> の並びを WPF のメニューへ変換する（トレイアイコンの右クリックメニュー用。View 側の部品）。
    /// </summary>
    public static class TrayContextMenuBuilder
    {
        /// <summary><paramref name="menu"/> の中身を、<paramref name="models"/> の内容へ置き換える。</summary>
        public static void Rebuild(ContextMenu menu, IReadOnlyList<TrayMenuItemModel> models)
        {
            menu.Items.Clear();
            Fill(menu.Items, models);
        }

        private static void Fill(ItemCollection target, IReadOnlyList<TrayMenuItemModel> models)
        {
            foreach (TrayMenuItemModel model in models)
            {
                if (model.IsSeparator)
                {
                    target.Add(new Separator());
                    continue;
                }

                // 見出しの _ はアクセスキーの印になるため、二重にして文字として表示させる
                var item = new MenuItem
                {
                    Header = Regex.Replace(model.Header ?? string.Empty, "_{1}", "__"),
                    IsEnabled = model.IsEnabled,
                    IsChecked = model.IsChecked,
                };
                if (model.IsBold)
                    item.FontWeight = FontWeights.Bold;
                if (model.Click != null)
                {
                    Action click = model.Click;
                    item.Click += (s, e) => click();
                }

                Fill(item.Items, model.Children);
                target.Add(item);
            }
        }
    }
}
