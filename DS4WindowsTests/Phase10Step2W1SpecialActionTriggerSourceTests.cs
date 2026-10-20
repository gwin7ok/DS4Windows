using DS4WinWPF.DS4Forms.ViewModels;
using Xunit;

namespace DS4WindowsTests
{
    /// <summary>
    /// Phase10-Step2 W1（Phase10-Step2-Plan.md §3.1・§5）: SA のモードのトリガーの供給元（<see cref="SpecialActionTriggerSource"/>）。
    /// 従来の <c>SpecialActEditorViewModel</c> の、トリガーのリストと「トリガーが 1 つ以上」の検証と同じ。
    /// </summary>
    public class Phase10Step2W1SpecialActionTriggerSourceTests
    {
        [Fact]
        public void SelectingAndUnselecting_UpdatesTheList_WithoutDuplicates()
        {
            var source = new SpecialActionTriggerSource();

            Assert.True(source.CanChange);
            Assert.True(source.SetTrigger("Cross", true));
            Assert.True(source.SetTrigger("Cross", true));   // 重複して足さない
            Assert.True(source.SetTrigger("L2 Full Pull", true));
            Assert.Equal(new[] { "Cross", "L2 Full Pull" }, source.Triggers);

            Assert.True(source.SetTrigger("Cross", false));
            Assert.Equal(new[] { "L2 Full Pull" }, source.Triggers);
            Assert.True(source.SetTrigger("Square", false));   // 選んでいないものを外しても、何も起きない
            Assert.Equal(new[] { "L2 Full Pull" }, source.Triggers);
        }

        [Fact]
        public void TriggersText_JoinsWithASlash_LikeTheControlsOfASpecialAction()
        {
            var source = new SpecialActionTriggerSource();
            source.SetTrigger("Cross", true);
            source.SetTrigger("Circle", true);
            source.SetTrigger("Left Stick Up", true);

            Assert.Equal("Cross/Circle/Left Stick Up", source.TriggersText);
        }

        [Fact]
        public void UnloadTriggersText_IsNullWhenThereAreNone_SoTheSavedValueIsNotOverwritten()
        {
            var source = new SpecialActionTriggerSource();
            Assert.Null(source.UnloadTriggersText);

            source.SetUnloadTrigger("R1", true);
            source.SetUnloadTrigger("L1", true);
            Assert.Equal("R1/L1", source.UnloadTriggersText);
            Assert.Equal(new[] { "R1", "L1" }, source.UnloadTriggers);

            source.SetUnloadTrigger("R1", false);
            source.SetUnloadTrigger("L1", false);
            Assert.Null(source.UnloadTriggersText);
        }

        [Fact]
        public void Validate_RequiresAtLeastOneTrigger()
        {
            var source = new SpecialActionTriggerSource();

            Assert.False(source.Validate(out string error));
            Assert.Equal("No triggers provided", error);

            source.SetTrigger("Cross", true);
            Assert.True(source.Validate(out string noError));
            Assert.Null(noError);
        }

        [Fact]
        public void TheExposedLists_AreTheSameOnesTheSourceReads()
        {
            // 画面（チェックボックスのクリック）が、リストを直接書き換える従来の経路（W2 まで）
            var source = new SpecialActionTriggerSource();

            source.TriggerList.Add("Cross");
            source.UnloadTriggerList.Add("R1");

            Assert.Equal(new[] { "Cross" }, source.Triggers);
            Assert.Equal("Cross", source.TriggersText);
            Assert.Equal("R1", source.UnloadTriggersText);
        }
    }
}
