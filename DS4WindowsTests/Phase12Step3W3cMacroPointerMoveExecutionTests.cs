using Xunit;
using DS4Windows.Core.Abstractions;
using DS4Windows.Core.Settings;
using DS4Windows.Input;
using DS4Windows.Processing;
using DS4Windows.Processing.Abstractions;
using DS4Windows.Processing.Services;

namespace DS4WindowsTests
{
    /// <summary>
    /// Phase12-Step3 3c（R-3）: マクロのマウスポインターの移動ステップの実行（絶対／前面ウィンドウ基準）。
    /// 実際のカーソルの移動（<c>Win32WindowSystem</c>）と、ゲームでの効き方は、実機で確認する。
    /// </summary>
    public class Phase12Step3W3cMacroPointerMoveExecutionTests
    {
        private static MacroStepExecutor NewExecutor(RecordingWindowSystem windowSystem)
            => new MacroStepExecutor(TestHost.GetService<IVirtualKBM>(), TestHost.GetService<IProfileSettingsService>(),
                new MacroRuntimeState(), new DS4Windows.Processing.Services.DeviceSlotState(), windowSystem);

        private static int Value(MacroPointerMoveMode mode, int x, int y) => new MacroPointerMove(mode, x, y).ToMacroValue();

        [Theory]
        [InlineData(1920, 1080)]
        [InlineData(-1920, 200)]
        [InlineData(0, 0)]
        public void AbsoluteMove_MovesToTheScreenCoordinates_AndNeedsNoDelay(int x, int y)
        {
            var window = new RecordingWindowSystem { ForegroundOrigin = (500, 500) };

            bool delay = NewExecutor(window).Execute(0, DS4KeyType.None, Value(MacroPointerMoveMode.Absolute, x, y), new bool[512]);

            Assert.False(delay);
            Assert.Equal(new[] { (x, y) }, window.MovedTo);
        }

        [Fact]
        public void WindowRelativeMove_AddsTheForegroundWindowOrigin()
        {
            var window = new RecordingWindowSystem { ForegroundOrigin = (300, 120) };

            NewExecutor(window).Execute(0, DS4KeyType.None, Value(MacroPointerMoveMode.WindowRelative, 40, -5), new bool[512]);

            Assert.Equal(new[] { (340, 115) }, window.MovedTo);
        }

        [Fact]
        public void WindowRelativeMove_WithNoForegroundWindow_DoesNotMove_AndDoesNotThrow()
        {
            var window = new RecordingWindowSystem { HasForegroundWindow = false };

            bool delay = NewExecutor(window).Execute(0, DS4KeyType.None, Value(MacroPointerMoveMode.WindowRelative, 10, 10), new bool[512]);

            Assert.False(delay);
            Assert.Empty(window.MovedTo);
        }

        [Fact]
        public void AbsoluteMove_DoesNotLookAtTheForegroundWindow()
        {
            var window = new RecordingWindowSystem { HasForegroundWindow = false };

            NewExecutor(window).Execute(0, DS4KeyType.None, Value(MacroPointerMoveMode.Absolute, 7, 8), new bool[512]);

            Assert.Equal(new[] { (7, 8) }, window.MovedTo);
        }

        [Fact]
        public void AMoveThatFailsToMove_DoesNotThrow()
        {
            var window = new RecordingWindowSystem { MoveResult = false };

            var exception = Record.Exception(() => NewExecutor(window).Execute(0, DS4KeyType.None, Value(MacroPointerMoveMode.Absolute, 1, 1), new bool[512]));

            Assert.Null(exception);
        }

        [Fact]
        public void TheReservedModeValue_IsIgnored_WithoutMovingOrThrowing()
        {
            var window = new RecordingWindowSystem();

            bool delay = NewExecutor(window).Execute(0, DS4KeyType.None, MacroPointerMove.BaseValue + (2 * 268_435_456), new bool[512]);

            Assert.False(delay);
            Assert.Empty(window.MovedTo);
        }

        [Fact]
        public void APointerMove_LeavesTheKeyStateAndTheLightbarAlone()
        {
            var window = new RecordingWindowSystem();
            var keydown = new bool[512];
            bool forcelightBefore = DS4LightBar.forcelight[0];

            NewExecutor(window).Execute(0, DS4KeyType.None, Value(MacroPointerMoveMode.Absolute, 100, 100), keydown);

            Assert.DoesNotContain(true, keydown);
            Assert.Equal(forcelightBefore, DS4LightBar.forcelight[0]);
        }

        [Fact]
        public void TheExistingValues_StillWork_NextToAMove()
        {
            var window = new RecordingWindowSystem();
            MacroStepExecutor executor = NewExecutor(window);
            var keydown = new bool[512];

            Assert.True(executor.Execute(0, DS4KeyType.None, 400, keydown));            // 待ち 100 ms
            Assert.Empty(window.MovedTo);
        }
    }
}
