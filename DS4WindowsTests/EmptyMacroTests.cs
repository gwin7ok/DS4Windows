using Xunit;
using DS4Windows.Core.Settings;
using DS4Windows.UI.ViewModels.SpecialActions;

namespace DS4WindowsTests
{
    /// <summary>
    /// 手順が空のマクロも、SA として保存できる（従来は「No macro defined」で保存できなかった）。
    /// 保存できること、保存した値を読み込み直しても空のマクロになること、空のマクロの繰り返しの待ちは、MacroPlaybackEmptyRepeatTests が挙動で確かめる。
    /// </summary>
    public class EmptyMacroTests
    {
        [Fact]
        public void AnEmptyMacro_IsValid_AndRecordsNoError()
        {
            var vm = new MacroViewModel(new RecordingSpecialActionRepository());

            Assert.True(vm.IsValid(new SpecialAction("N", "Cross", "Macro", "")));
            Assert.False(vm.HasErrors);
        }

        [Fact]
        public void AnEmptyMacro_IsValid_AfterAnEarlierError_WasCleared()
        {
            var vm = new MacroViewModel(new RecordingSpecialActionRepository());
            vm.Macro.Add(350);
            vm.Macro.Clear();

            Assert.True(vm.IsValid(new SpecialAction("N", "Cross", "Macro", "")));
        }

        [Fact]
        public void AnEmptyMacro_IsSavedWithEmptyDetails()
        {
            var repo = new RecordingSpecialActionRepository();
            var vm = new MacroViewModel(repo);
            var action = new SpecialAction("N", "Cross", "Macro", "");

            vm.SaveAction(action);

            Assert.Equal(1, repo.SaveCount);
            Assert.Equal(string.Empty, repo.SavedDetails);
        }

        [Fact]
        public void AnEmptyMacro_IsLoadedBackAsAnEmptyMacro()
        {
            var action = new SpecialAction("N", "Cross", "Macro", "");

            Assert.Equal(SpecialAction.ActionTypeId.Macro, action.typeID);
            Assert.Empty(action.macro);
        }
    }
}
