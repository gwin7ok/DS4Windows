using System.Collections.Generic;
using Xunit;
using DS4Windows.Core.Settings;
using DS4Windows.Processing;
using DS4Windows.UI.ViewModels;
using DS4Windows.UI.ViewModels.SpecialActions;

namespace DS4WindowsTests
{
    /// <summary>
    /// Phase12-Step2 2e（R-4）: FocusWindow の編集（<see cref="FocusWindowViewModel"/>）、種別の一覧の項目（添字 9）、割り当ての説明文。
    /// </summary>
    public class Phase12Step2W2eFocusWindowEditorTests
    {
        private static SpecialAction Target() => new SpecialAction("N", "Cross", "FocusWindow", "x") { typeID = SpecialAction.ActionTypeId.FocusWindow };

        [Fact]
        public void Catalog_HasFocusWindowAtIndex9_AndControlOnlyItemsFollow()
        {
            var catalog = new ActionTypeCatalog(includeControlOnlyItems: true);

            Assert.Equal(SpecialAction.ActionTypeId.FocusWindow, catalog.At(9).TypeId);
            Assert.Equal("SpecialActionEdit.FocusWindow", catalog.At(9).LabelKey);
            Assert.Equal(9, catalog.IndexOf(SpecialAction.ActionTypeId.FocusWindow));
            Assert.Equal(10, catalog.DefaultIndex);
            Assert.Equal(11, catalog.UnboundIndex);
            Assert.True(catalog.At(9).IsSavableIn(ActionEditorModes.SpecialAction));
            Assert.True(catalog.At(9).IsSavableIn(ActionEditorModes.Control));
        }

        [Fact]
        public void SaveAction_UsesMode11_WithPathAsDetails_AndTitleAsExtras()
        {
            var repo = new RecordingSpecialActionRepository();
            var vm = new FocusWindowViewModel(repo) { WindowPath = "^c:\\games", WindowTitle = "*Test", Delay = 0.5 };
            var set = new ActionTypeEditorSet(new Dictionary<SpecialAction.ActionTypeId, IActionTypeEditor> { [SpecialAction.ActionTypeId.FocusWindow] = vm });

            set.Save(SpecialAction.ActionTypeId.FocusWindow, Target(), edit: true);

            Assert.Equal(1, repo.SaveCount);
            Assert.Equal(("N", "Cross", 11, "^c:\\games", true, 0.5, "*Test"),
                (repo.SavedName, repo.SavedControls, repo.SavedMode, repo.SavedDetails, repo.SavedEdit, repo.SavedDelay, repo.SavedExtras));
        }

        [Fact]
        public void LoadAction_And_LoadControlItem_FillTheFields()
        {
            var vm = new FocusWindowViewModel(new RecordingSpecialActionRepository());

            vm.LoadAction(new SpecialAction("N", "Cross", "FocusWindow", "game.exe$", 1.5, "Window$"));
            Assert.Equal(("game.exe$", "Window$", 1.5), (vm.WindowPath, vm.WindowTitle, vm.Delay));

            vm.LoadControlItem(new ControlSpecialItem { Type = SpecialAction.ActionTypeId.FocusWindow, WindowPath = "a", WindowTitle = "b", HoldSeconds = 2 });
            Assert.Equal(("a", "b", 2.0), (vm.WindowPath, vm.WindowTitle, vm.Delay));
        }

        [Fact]
        public void ToControlItem_CarriesPathTitleAndHold()
        {
            var vm = new FocusWindowViewModel(new RecordingSpecialActionRepository()) { WindowPath = "p", WindowTitle = "t", Delay = 0.25 };

            ControlSpecialItem item = vm.ToControlItem(SpecialAction.ActionTypeId.FocusWindow);

            Assert.Equal(SpecialAction.ActionTypeId.FocusWindow, item.Type);
            Assert.Equal(("p", "t", 0.25), (item.WindowPath, item.WindowTitle, item.HoldSeconds));
        }

        [Theory]
        [InlineData("", "", false)]
        [InlineData("  ", " ", false)]
        [InlineData(null, null, false)]
        [InlineData("game.exe$", "", true)]
        [InlineData("", "*Notes", true)]
        [InlineData("game.exe$", "*Notes", true)]
        public void IsValid_RequiresPathOrTitle_AndReportsErrorsOnBothFields(string path, string title, bool expected)
        {
            var vm = new FocusWindowViewModel(new RecordingSpecialActionRepository()) { WindowPath = path, WindowTitle = title };

            Assert.Equal(expected, vm.IsValid(Target()));
            Assert.Equal(!expected, vm.HasErrors);

            // 入力し直して再検証すると、前のエラーは消える
            vm.WindowPath = "x";
            Assert.True(vm.IsValid(Target()));
            Assert.False(vm.HasErrors);
        }

        [Fact]
        public void Describer_ShowsTheTarget_ForControlsAndSpecialActions()
        {
            var item = new ControlSpecialItem { Type = SpecialAction.ActionTypeId.FocusWindow, WindowPath = "game.exe$", WindowTitle = "*Test", HoldSeconds = 0.5 };
            Assert.Equal("Focus Window: game.exe$ / *Test (hold 0.5s)", AssignmentDescriber.ForSpecialItem(item));

            item.WindowTitle = string.Empty;
            Assert.Equal("Focus Window: game.exe$ (hold 0.5s)", AssignmentDescriber.ForSpecialItem(item));

            var sa = new SpecialAction("n", "Cross", "FocusWindow", string.Empty, 0, "*Notes");
            Assert.Equal("Focus Window: *Notes", AssignmentDescriber.ForSpecialAction(sa, c => c.ToString()));
        }
    }
}
