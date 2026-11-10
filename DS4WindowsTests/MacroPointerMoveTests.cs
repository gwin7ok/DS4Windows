using DS4Windows.Processing.Macro;
using Xunit;
using DS4Windows.Processing;

namespace DS4WindowsTests
{
    /// <summary>
    /// Phase12-Step3 3b（R-3）: マクロの「マウスポインターの移動」ステップの値の体系（整数 1 個との相互変換）と、リストの表示名。
    /// </summary>
    public class MacroPointerMoveTests
    {
        [Theory]
        [InlineData(MacroPointerMoveMode.Absolute, 0, 0)]
        [InlineData(MacroPointerMoveMode.Absolute, 1920, 1080)]
        [InlineData(MacroPointerMoveMode.Absolute, -1920, -300)]
        [InlineData(MacroPointerMoveMode.Absolute, 11519, 2160)]
        [InlineData(MacroPointerMoveMode.Absolute, MacroPointerMove.AbsoluteMin, MacroPointerMove.AbsoluteMin)]
        [InlineData(MacroPointerMoveMode.Absolute, MacroPointerMove.AbsoluteMax, MacroPointerMove.AbsoluteMax)]
        [InlineData(MacroPointerMoveMode.WindowRelative, 0, 0)]
        [InlineData(MacroPointerMoveMode.WindowRelative, 640, 360)]
        [InlineData(MacroPointerMoveMode.WindowRelative, -50, -8)]
        [InlineData(MacroPointerMoveMode.WindowRelative, MacroPointerMove.WindowRelativeMin, MacroPointerMove.WindowRelativeMax)]
        [InlineData(MacroPointerMoveMode.WindowRelative, MacroPointerMove.WindowRelativeMax, MacroPointerMove.WindowRelativeMin)]
        public void ToMacroValue_ThenBack_ReturnsTheSameMove(MacroPointerMoveMode mode, int x, int y)
        {
            var move = new MacroPointerMove(mode, x, y);

            int value = move.ToMacroValue();

            Assert.True(MacroPointerMove.TryFromMacroValue(value, out MacroPointerMove back));
            Assert.Equal(move, back);
            Assert.Equal(mode, back.Mode);
            Assert.Equal(x, back.X);
            Assert.Equal(y, back.Y);
        }

        [Fact]
        public void EveryValueIsInsideTheMoveRange_AndAboveTheExistingLightbarRange()
        {
            const int maxLightbarValue = 1_255_255_255;
            int smallest = new MacroPointerMove(MacroPointerMoveMode.Absolute, MacroPointerMove.AbsoluteMin, MacroPointerMove.AbsoluteMin).ToMacroValue();
            int largest = new MacroPointerMove(MacroPointerMoveMode.WindowRelative, MacroPointerMove.WindowRelativeMax, MacroPointerMove.WindowRelativeMax).ToMacroValue();

            Assert.Equal(MacroPointerMove.BaseValue, smallest);
            Assert.True(smallest > maxLightbarValue);
            Assert.True(largest > 0 && largest < int.MaxValue);
            Assert.True(MacroPointerMove.IsPointerMoveValue(smallest));
            Assert.True(MacroPointerMove.IsPointerMoveValue(largest));
        }

        [Fact]
        public void TheModesDoNotOverlap()
        {
            int absoluteLargest = new MacroPointerMove(MacroPointerMoveMode.Absolute, MacroPointerMove.AbsoluteMax, MacroPointerMove.AbsoluteMax).ToMacroValue();
            int windowSmallest = new MacroPointerMove(MacroPointerMoveMode.WindowRelative, MacroPointerMove.WindowRelativeMin, MacroPointerMove.WindowRelativeMin).ToMacroValue();

            Assert.True(absoluteLargest < windowSmallest);
            Assert.Equal(absoluteLargest + 1, windowSmallest);
        }

        [Theory]
        [InlineData(MacroPointerMoveMode.Absolute, 100000, -100000, MacroPointerMove.AbsoluteMax, MacroPointerMove.AbsoluteMin)]
        [InlineData(MacroPointerMoveMode.WindowRelative, -100000, 100000, MacroPointerMove.WindowRelativeMin, MacroPointerMove.WindowRelativeMax)]
        public void OutOfRangeCoordinates_AreClampedToTheEdges(MacroPointerMoveMode mode, int x, int y, int expectedX, int expectedY)
        {
            var move = new MacroPointerMove(mode, x, y);

            Assert.Equal(expectedX, move.X);
            Assert.Equal(expectedY, move.Y);
            Assert.True(MacroPointerMove.TryFromMacroValue(move.ToMacroValue(), out MacroPointerMove back));
            Assert.Equal(move, back);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(65)]
        [InlineData(256)]
        [InlineData(288)]
        [InlineData(300)]
        [InlineData(999_999)]
        [InlineData(1_000_000)]
        [InlineData(1_255_255)]
        [InlineData(1_000_000_000)]
        [InlineData(1_255_255_255)]
        [InlineData(1_299_999_999)]
        public void TheExistingMacroValues_AreNotPointerMoves(int value)
        {
            Assert.False(MacroPointerMove.IsPointerMoveValue(value));
            Assert.False(MacroPointerMove.TryFromMacroValue(value, out _));
        }

        [Fact]
        public void TheReservedModeAndBeyond_AreInTheRangeButCannotBeDecoded()
        {
            int reserved = MacroPointerMove.BaseValue + (2 * 268_435_456);

            Assert.True(MacroPointerMove.IsPointerMoveValue(reserved));
            Assert.False(MacroPointerMove.TryFromMacroValue(reserved, out _));
            Assert.False(MacroPointerMove.TryFromMacroValue(int.MaxValue, out _));
        }

        [Fact]
        public void DisplayName_ShowsTheModeAndTheCoordinates()
        {
            Assert.Equal("Move to (100, -200)", new MacroPointerMove(MacroPointerMoveMode.Absolute, 100, -200).DisplayName);
            Assert.Equal("Move in window (10, 5)", new MacroPointerMove(MacroPointerMoveMode.WindowRelative, 10, 5).DisplayName);
        }

        [Fact]
        public void MacroParser_ListsAPointerMoveStep_AndKeepsTheOtherStepsAsBefore()
        {
            int abs = new MacroPointerMove(MacroPointerMoveMode.Absolute, 800, 600).ToMacroValue();
            int win = new MacroPointerMove(MacroPointerMoveMode.WindowRelative, 12, 34).ToMacroValue();
            var parser = new MacroParser(new[] { abs, 256, 400, 256, win, 1_000_000_000 });

            parser.LoadMacro();

            var steps = parser.MacroSteps;
            Assert.Equal(MacroStep.StepOutput.PointerMove, steps[0].OutputType);
            Assert.Equal(MacroStep.StepType.ActDown, steps[0].ActType);
            Assert.Equal("Move to (800, 600)", steps[0].Name);
            Assert.Equal(abs, steps[0].Value);

            Assert.Equal(MacroStep.StepOutput.Button, steps[1].OutputType);
            Assert.Equal(MacroStep.StepType.Wait, steps[2].ActType);
            Assert.Equal(MacroStep.StepType.ActUp, steps[3].ActType);

            Assert.Equal(MacroStep.StepOutput.PointerMove, steps[4].OutputType);
            Assert.Equal("Move in window (12, 34)", steps[4].Name);

            Assert.Equal(MacroStep.StepOutput.Lightbar, steps[5].OutputType);
            Assert.Equal("Reset Lightbar", steps[5].Name);
        }

        [Fact]
        public void MacroParser_NamesAReservedModeValueAsUnsupported_WithoutThrowing()
        {
            var parser = new MacroParser(new[] { MacroPointerMove.BaseValue + (2 * 268_435_456) });

            parser.LoadMacro();

            Assert.Equal(MacroStep.StepOutput.PointerMove, parser.MacroSteps[0].OutputType);
            Assert.Equal("Move (unsupported)", parser.MacroSteps[0].Name);
        }

        [Fact]
        public void ChangingAPointerMoveStepValue_UpdatesItsName()
        {
            int first = new MacroPointerMove(MacroPointerMoveMode.Absolute, 1, 2).ToMacroValue();
            var step = new MacroStep(first, "x", MacroStep.StepType.ActDown, MacroStep.StepOutput.PointerMove);

            step.Value = new MacroPointerMove(MacroPointerMoveMode.WindowRelative, -3, 4).ToMacroValue();

            Assert.Equal("Move in window (-3, 4)", step.Name);
        }
    }
}
