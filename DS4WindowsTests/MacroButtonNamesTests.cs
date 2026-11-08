using Xunit;
using DS4Windows.Core.Model;
using DS4Windows.Processing;

namespace DS4WindowsTests
{
    /// <summary>
    /// Phase11 W6（S9 の追加修正）: マクロの記録画面のボタン名は、Emulated Controller に合わせる（Xbox 360 は Xbox 名、DS4 は DS4 名）。
    /// </summary>
    public class MacroButtonNamesTests
    {
        [Theory]
        [InlineData(261, "A Button", "Cross")]
        [InlineData(262, "B Button", "Circle")]
        [InlineData(263, "X Button", "Square")]
        [InlineData(264, "Y Button", "Triangle")]
        [InlineData(265, "Start", "Options")]
        [InlineData(266, "Back", "Share")]
        [InlineData(267, "Up Button", "Dpad Up")]
        [InlineData(271, "Guide", "PS")]
        [InlineData(272, "Left Bumper", "L1")]
        [InlineData(274, "Left Trigger", "L2")]
        [InlineData(277, "Right Stick", "R3")]
        public void ButtonNames_FollowTheEmulatedController(int value, string xbox, string ds4)
        {
            Assert.Equal(xbox, MacroParser.InputName(value, OutContType.X360));
            Assert.Equal(xbox, MacroParser.InputName(value));
            Assert.Equal(ds4, MacroParser.InputName(value, OutContType.DS4));
        }

        [Theory]
        [InlineData(256)]
        [InlineData(260)]
        [InlineData(278)]
        [InlineData(285)]
        [InlineData(287)]
        [InlineData(288)]
        public void MouseStickAndTouchNames_DoNotDependOnTheType(int value)
        {
            Assert.Equal(MacroParser.macroInputNames[value], MacroParser.InputName(value, OutContType.DS4));
        }

        [Fact]
        public void TheParser_NamesLoadedSteps_ByTheGivenType()
        {
            var ds4 = new MacroParser(new[] { 261, 261 }, OutContType.DS4);
            Assert.Equal(new[] { "Cross", "Cross" }, ds4.GetMacroStrings());

            var xbox = new MacroParser(new[] { 261, 261 });
            Assert.Equal(new[] { "A Button", "A Button" }, xbox.GetMacroStrings());
        }
    }
}
