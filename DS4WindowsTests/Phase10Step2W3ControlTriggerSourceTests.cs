using System;
using DS4WinWPF.DS4Forms.ViewModels;
using Xunit;

namespace DS4WindowsTests
{
    /// <summary>
    /// Phase10-Step2 W3（Phase10-Step2-Plan.md §3.3・§5）: Controls のモードのトリガーの供給元（<see cref="ControlTriggerSource"/>）。
    /// 対象のボタン 1 つに固定で、変更できない。
    /// </summary>
    public class Phase10Step2W3ControlTriggerSourceTests
    {
        [Fact]
        public void HoldsOnlyTheTargetButton_AndCannotBeChanged()
        {
            var source = new ControlTriggerSource("Circle");

            Assert.False(source.CanChange);
            Assert.Equal(new[] { "Circle" }, source.Triggers);
            Assert.Equal("Circle", source.TriggersText);
            Assert.Empty(source.UnloadTriggers);
            Assert.Null(source.UnloadTriggersText);
        }

        [Fact]
        public void SettingOrUnsettingATrigger_IsRefused_AndChangesNothing()
        {
            var source = new ControlTriggerSource("Circle");

            Assert.False(source.SetTrigger("Cross", true));
            Assert.False(source.SetTrigger("Circle", false));
            Assert.False(source.SetUnloadTrigger("R1", true));

            Assert.Equal(new[] { "Circle" }, source.Triggers);
            Assert.Empty(source.UnloadTriggers);
        }

        [Fact]
        public void Validate_IsAlwaysValid_BecauseTheTriggerIsFixed()
        {
            var source = new ControlTriggerSource("Circle");

            Assert.True(source.Validate(out string error));
            Assert.Null(error);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        public void Constructor_RequiresTheButtonName(string name)
        {
            Assert.Throws<ArgumentException>(() => new ControlTriggerSource(name));
        }
    }
}
