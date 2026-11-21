#nullable enable
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;

namespace DS4Windows.UI.ViewModels
{
    /// <summary>
    /// スペシャルアクションの一覧の並べ替え。直前に並べた列・方向を持ち（唯一の実体）、
    /// 次に別の列で並べるときは、直前の列を第 2 のキーに使う。
    /// 例: 名前の昇順の状態から Active 列を押すと、Active（押した方向）→ 名前の昇順の順になる。
    /// </summary>
    public sealed class SpecialActionListSorter
    {
        /// <summary>直近に適用した列名。</summary>
        public string Column { get; private set; } = "Name";

        /// <summary>直近に適用した方向。</summary>
        public ListSortDirection Direction { get; private set; } = ListSortDirection.Ascending;

        public bool Ascending => Direction == ListSortDirection.Ascending;

        /// <summary>並べ替えた新しい並びを返し、列・方向を記録する。</summary>
        public List<SpecialActionItem> Sort(IEnumerable<SpecialActionItem> source, string propertyName, ListSortDirection direction)
        {
            List<SpecialActionItem> items = source.ToList();

            Comparison<SpecialActionItem>? primary = GetColumnComparison(propertyName, direction);
            Comparison<SpecialActionItem>? secondary =
                (Column != propertyName) ? GetColumnComparison(Column, Direction) : null;

            items.Sort((x, y) =>
            {
                int cmp = primary?.Invoke(x, y) ?? 0;
                if (cmp != 0) return cmp;

                if (secondary != null)
                {
                    cmp = secondary(x, y);
                    if (cmp != 0) return cmp;
                }

                // 最終のタイブレーク: アクション名（プロファイル内で一意）で順序を確定させる。
                // List<T>.Sort は不安定なため、これがないと、主・第 2 のキーが同値の要素の順序が実行ごとに変わりうる。
                return string.Compare(x.ActionName, y.ActionName, StringComparison.CurrentCultureIgnoreCase);
            });

            Column = propertyName;
            Direction = direction;
            return items;
        }

        /// <summary>指定した列・方向の比較関数。未知の列名は null（同値扱い）。</summary>
        public static Comparison<SpecialActionItem>? GetColumnComparison(string propertyName, ListSortDirection direction)
        {
            bool asc = direction == ListSortDirection.Ascending;
            switch (propertyName)
            {
                case "Name":
                    if (asc) return (x, y) => string.Compare(x.ActionName, y.ActionName, StringComparison.CurrentCultureIgnoreCase);
                    return (x, y) => string.Compare(y.ActionName, x.ActionName, StringComparison.CurrentCultureIgnoreCase);
                case "Active":
                    // 昇順ではチェック有り(true)を上に、降順ではチェック無し(false)を上にする
                    if (asc) return (x, y) => y.Active.CompareTo(x.Active);
                    return (x, y) => x.Active.CompareTo(y.Active);
                case "Trigger":
                    // 画面のヘッダーボタンの Tag="Trigger" に対応。実データは Controls
                    if (asc) return (x, y) => string.Compare(x.Controls, y.Controls, StringComparison.CurrentCultureIgnoreCase);
                    return (x, y) => string.Compare(y.Controls, x.Controls, StringComparison.CurrentCultureIgnoreCase);
                case "Action":
                    // 画面のヘッダーボタンの Tag="Action" に対応。実データは TypeName
                    if (asc) return (x, y) => string.Compare(x.TypeName, y.TypeName, StringComparison.CurrentCultureIgnoreCase);
                    return (x, y) => string.Compare(y.TypeName, x.TypeName, StringComparison.CurrentCultureIgnoreCase);
                default:
                    return null;
            }
        }
    }
}
