using DS4Windows;
using DS4WinWPF.DS4Forms.ViewModels;
using Xunit;

namespace DS4WindowsTests
{
    /// <summary>
    /// Phase10-Step2 W3（Phase10-Step2-Plan.md §3.3・§5）: Controls のモードの編集の対象（<see cref="ControlEditTarget"/>）。
    /// 名前は、対象のボタンの表示名で固定（編集不可）。名前の検査はない。
    /// </summary>
    public class Phase10Step2W3ControlEditTargetTests
    {
        [Theory]
        [InlineData(DS4Controls.Circle)]
        [InlineData(DS4Controls.L2)]
        [InlineData(DS4Controls.LXNeg)]
        public void TheName_IsTheButtonsDisplayName(DS4Controls control)
        {
            var target = new ControlEditTarget(control);

            Assert.Equal(control, target.Control);
            Assert.Equal(DS4ControlNames.ds4inputNames[control], target.Name);
        }

        [Fact]
        public void TheName_IsNotEditable_AndAWriteIsIgnored()
        {
            var target = new ControlEditTarget(DS4Controls.Circle);
            string original = target.Name;

            target.Name = "Something else";

            Assert.False(target.NameEditable);
            Assert.Equal(original, target.Name);
        }

        [Fact]
        public void ItEditsAnExistingAssignment_WithoutASavedSpecialAction()
        {
            var target = new ControlEditTarget(DS4Controls.Cross);

            Assert.True(target.EditMode);
            Assert.Null(target.SavedAction);
        }

        [Fact]
        public void ValidateName_HasNoErrors_AndNeverReportsADuplicate()
        {
            var target = new ControlEditTarget(DS4Controls.Cross);

            Assert.Empty(target.ValidateName(out bool alreadyExists));
            Assert.False(alreadyExists);
        }
    }
}
