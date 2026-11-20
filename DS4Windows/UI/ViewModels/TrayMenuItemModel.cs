#nullable enable

using System;
using System.Collections.Generic;

namespace DS4Windows.UI.ViewModels
{
    /// <summary>
    /// トレイアイコンのメニューの 1 項目を表す、画面の部品に依存しない値の入れ物。
    /// ViewModel が項目の並びと状態を決め、View（<c>TrayContextMenuBuilder</c>）が WPF のメニューへ変換する。
    /// 見出しは、アクセスキーのためのエスケープ（_ → __）をしない、利用者に見せる文字列のまま持つ。
    /// </summary>
    public sealed class TrayMenuItemModel
    {
        public string Header { get; set; }

        public bool IsSeparator { get; set; }

        public bool IsEnabled { get; set; } = true;

        public bool IsChecked { get; set; }

        public bool IsBold { get; set; }

        /// <summary>選ばれたときの処理。サブメニューだけの項目は null。</summary>
        public Action Click { get; set; }

        public List<TrayMenuItemModel> Children { get; } = new List<TrayMenuItemModel>();

        public static TrayMenuItemModel Separator() => new TrayMenuItemModel { IsSeparator = true };
    }
}
