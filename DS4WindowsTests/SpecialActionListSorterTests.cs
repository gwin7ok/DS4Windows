#nullable enable
using System.ComponentModel;
using System.Linq;
using DS4Windows.UI.ViewModels;
using Xunit;

namespace DS4WindowsTests
{
    /// <summary>
    /// スペシャルアクションの一覧の並べ替え部品を、ViewModel を作らずに単体で検査する
    /// （ViewModel 越しの振る舞いは <see cref="SpecialActionsListViewModelSortTests"/> が守る）。
    /// </summary>
    public class SpecialActionListSorterTests
    {
        private static SpecialActionItem[] Items(params (string name, bool active)[] items) =>
            items.Select((x, i) => new SpecialActionItem(null!, x.name, i) { Active = x.active }).ToArray();

        [Fact]
        public void DefaultState_IsNameAscending()
        {
            var sorter = new SpecialActionListSorter();

            Assert.Equal("Name", sorter.Column);
            Assert.Equal(ListSortDirection.Ascending, sorter.Direction);
            Assert.True(sorter.Ascending);
        }

        [Fact]
        public void Sort_RecordsTheColumnAndDirection_AndUsesThePreviousColumnAsTheSecondKey()
        {
            var sorter = new SpecialActionListSorter();

            var result = sorter.Sort(Items(("Bravo", true), ("Alpha", true), ("Delta", false), ("Charlie", false)), "Active", ListSortDirection.Ascending);

            Assert.Equal(new[] { "Alpha", "Bravo", "Charlie", "Delta" }, result.Select(i => i.ActionName));
            Assert.Equal("Active", sorter.Column);
            Assert.Equal(ListSortDirection.Ascending, sorter.Direction);
        }

        [Fact]
        public void Sort_DoesNotChangeTheSourceOrder()
        {
            var sorter = new SpecialActionListSorter();
            var source = Items(("B", false), ("A", false));

            sorter.Sort(source, "Name", ListSortDirection.Ascending);

            Assert.Equal(new[] { "B", "A" }, source.Select(i => i.ActionName));
        }

        [Fact]
        public void UnknownColumn_FallsBackToTheNameOrder()
        {
            var sorter = new SpecialActionListSorter();

            var result = sorter.Sort(Items(("B", false), ("A", false)), "Nope", ListSortDirection.Descending);

            Assert.Null(SpecialActionListSorter.GetColumnComparison("Nope", ListSortDirection.Ascending));
            Assert.Equal(new[] { "A", "B" }, result.Select(i => i.ActionName));
        }
    }
}
