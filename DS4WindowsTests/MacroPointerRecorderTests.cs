using Xunit;
using DS4Windows.Core.Platform;
using DS4Windows.Processing;

namespace DS4WindowsTests
{
    /// <summary>
    /// Phase12-Step3 3e（R-3）: マクロの記録で、マウスの移動・ボタンの位置を移動ステップの値へ変えるロジック（<see cref="MacroPointerRecorder"/>）と、
    /// フックのメッセージからボタンへの変換。フックそのものと、記録画面の操作は、実機で確認する。
    /// </summary>
    public class Phase12Step3W3eMacroPointerRecorderTests
    {
        private const int OwnProcessId = 4242;

        private static MacroPointerRecorder NewRecorder(MacroPointerMoveMode mode, RecordingWindowSystem window)
            => new MacroPointerRecorder(mode, window, OwnProcessId);

        private static MacroPointerMove Decode(int value)
        {
            Assert.True(MacroPointerMove.TryFromMacroValue(value, out MacroPointerMove move));
            return move;
        }

        [Fact]
        public void TheFirstMove_IsRecorded_AsAbsoluteScreenCoordinates()
        {
            MacroPointerRecorder recorder = NewRecorder(MacroPointerMoveMode.Absolute, new RecordingWindowSystem());

            Assert.True(recorder.TryRecordMove(800, 600, 1000, out int value));

            Assert.Equal(new MacroPointerMove(MacroPointerMoveMode.Absolute, 800, 600), Decode(value));
            Assert.Equal(1, recorder.MoveCount);
        }

        [Fact]
        public void SmallOrQuickMoves_AreSkipped()
        {
            MacroPointerRecorder recorder = NewRecorder(MacroPointerMoveMode.Absolute, new RecordingWindowSystem());
            Assert.True(recorder.TryRecordMove(100, 100, 1000, out _));

            Assert.False(recorder.TryRecordMove(103, 102, 1100, out _));     // 5 px 未満
            Assert.False(recorder.TryRecordMove(200, 200, 1010, out _));     // 20 ms 未満
            Assert.True(recorder.TryRecordMove(105, 100, 1020, out _));      // 5 px 以上、20 ms 以上
            Assert.Equal(2, recorder.MoveCount);
        }

        [Fact]
        public void TheDistanceIsMeasuredFromTheLastRecordedPosition_NotFromTheLastEvent()
        {
            MacroPointerRecorder recorder = NewRecorder(MacroPointerMoveMode.Absolute, new RecordingWindowSystem());
            Assert.True(recorder.TryRecordMove(0, 0, 1000, out _));

            Assert.False(recorder.TryRecordMove(3, 0, 1050, out _));
            Assert.False(recorder.TryRecordMove(4, 0, 1100, out _));
            Assert.True(recorder.TryRecordMove(5, 0, 1150, out _));
        }

        [Fact]
        public void MovesOverOurOwnWindow_AreNotRecorded()
        {
            var window = new RecordingWindowSystem { WindowAt = (x, y) => (true, OwnProcessId, 0, 0) };
            MacroPointerRecorder recorder = NewRecorder(MacroPointerMoveMode.Absolute, window);

            Assert.False(recorder.TryRecordMove(500, 500, 1000, out _));
            Assert.Equal(0, recorder.MoveCount);
        }

        [Fact]
        public void WindowRelative_SubtractsTheOriginOfTheWindowUnderThePointer()
        {
            var window = new RecordingWindowSystem { WindowAt = (x, y) => (true, 99, 300, 120) };
            MacroPointerRecorder recorder = NewRecorder(MacroPointerMoveMode.WindowRelative, window);

            Assert.True(recorder.TryRecordMove(340, 115, 1000, out int value));

            Assert.Equal(new MacroPointerMove(MacroPointerMoveMode.WindowRelative, 40, -5), Decode(value));
        }

        [Fact]
        public void WindowRelative_WithNoWindowUnderThePointer_IsNotRecorded_ButAbsoluteIs()
        {
            var window = new RecordingWindowSystem { WindowAt = (x, y) => (false, 0, 0, 0) };

            Assert.False(NewRecorder(MacroPointerMoveMode.WindowRelative, window).TryRecordMove(10, 10, 1000, out _));
            Assert.True(NewRecorder(MacroPointerMoveMode.Absolute, window).TryRecordMove(10, 10, 1000, out _));
        }

        [Fact]
        public void ABoxOutsideOurWindow_ButtonRecordsThePositionFirst_WhenItIsNotTheLastRecordedOne()
        {
            MacroPointerRecorder recorder = NewRecorder(MacroPointerMoveMode.Absolute, new RecordingWindowSystem());
            Assert.True(recorder.TryRecordMove(100, 100, 1000, out _));

            Assert.True(recorder.TryBeginButton(103, 101, 1005, out bool hasMove, out int moveValue));

            Assert.True(hasMove);
            Assert.Equal(new MacroPointerMove(MacroPointerMoveMode.Absolute, 103, 101), Decode(moveValue));
        }

        [Fact]
        public void AButtonAtTheLastRecordedPosition_AddsNoMove_SoPressAndReleaseDoNotRepeatIt()
        {
            MacroPointerRecorder recorder = NewRecorder(MacroPointerMoveMode.Absolute, new RecordingWindowSystem());

            Assert.True(recorder.TryBeginButton(50, 60, 1000, out bool downMove, out _));    // 押下: 位置を入れる
            Assert.True(recorder.TryBeginButton(50, 60, 1050, out bool upMove, out _));      // 離上: 同じ位置

            Assert.True(downMove);
            Assert.False(upMove);
            Assert.Equal(1, recorder.MoveCount);
        }

        [Fact]
        public void AButtonOverOurOwnWindow_IsNotRecorded()
        {
            var window = new RecordingWindowSystem { WindowAt = (x, y) => (true, OwnProcessId, 0, 0) };
            MacroPointerRecorder recorder = NewRecorder(MacroPointerMoveMode.Absolute, window);

            Assert.False(recorder.TryBeginButton(10, 10, 1000, out bool hasMove, out _));
            Assert.False(hasMove);
        }

        [Fact]
        public void TheMoveLimit_StopsRecordingMoves_ButNotTheButtons()
        {
            MacroPointerRecorder recorder = NewRecorder(MacroPointerMoveMode.Absolute, new RecordingWindowSystem());

            for (int i = 0; i < MacroPointerRecorder.MaxMoveSteps; i++)
            {
                Assert.True(recorder.TryRecordMove(i * 10, 0, 1000 + (i * 100L), out _), "i=" + i);
            }

            Assert.True(recorder.LimitReached);
            Assert.False(recorder.TryRecordMove(99999, 0, 9_999_999, out _));
            // ボタンは記録する（位置の移動だけ、入れない）
            Assert.True(recorder.TryBeginButton(1, 1, 9_999_999, out bool hasMove, out _));
            Assert.False(hasMove);
        }

        [Theory]
        [InlineData(0x0201, 0u, 256, true)]
        [InlineData(0x0202, 0u, 256, false)]
        [InlineData(0x0204, 0u, 257, true)]
        [InlineData(0x0205, 0u, 257, false)]
        [InlineData(0x0207, 0u, 258, true)]
        [InlineData(0x0208, 0u, 258, false)]
        [InlineData(0x020B, 0x00010000u, 259, true)]
        [InlineData(0x020C, 0x00010000u, 259, false)]
        [InlineData(0x020B, 0x00020000u, 260, true)]
        [InlineData(0x020C, 0x00020000u, 260, false)]
        public void TheHookMessages_AreTranslatedToTheMacroButtonValues(int message, uint mouseData, int expectedValue, bool expectedDown)
        {
            Assert.True(LowLevelMouseHook.TryTranslateButton(message, mouseData, out int value, out bool isDown));
            Assert.Equal(expectedValue, value);
            Assert.Equal(expectedDown, isDown);
        }

        [Theory]
        [InlineData(0x0200, 0u)]          // 移動
        [InlineData(0x020A, 0u)]          // ホイール
        [InlineData(0x020B, 0x00030000u)] // 知らない X ボタン
        public void MessagesThatAreNotButtons_AreNotTranslated(int message, uint mouseData)
        {
            Assert.False(LowLevelMouseHook.TryTranslateButton(message, mouseData, out _, out _));
        }
    }
}
