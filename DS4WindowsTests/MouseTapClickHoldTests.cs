using System;
using Xunit;

namespace DS4WindowsTests
{
    /// <summary>
    /// タップによる左クリックが、瞬間的ではなく一定時間（50ms）保持されること（2026-10-15 承認）。
    /// 判定は <c>TapClickDetector</c>、呼び出しは <c>TouchpadMouse</c> にある。
    /// </summary>
    public class MouseTapClickHoldTests
    {
        private static DS4Windows.Input.Touch MakeTouch(int x, int y) =>
            (DS4Windows.Input.Touch)System.Activator.CreateInstance(typeof(DS4Windows.Input.Touch),
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic,
                null, new object[] { x, y, (byte)1, null }, null);

        private static int LeftClicks(DS4Windows.Processing.Touchpad.TapClickDetector detector, DS4Windows.Processing.Synthetic.SyntheticInputState state)
        {
            state.DeviceState[0].currentClicks.leftCount = 0;
            detector.CountPendingClicks(state, 10);
            return state.DeviceState[0].currentClicks.leftCount;
        }

        [Fact]
        public void TapClick_UsesHoldInsteadOfSingleFramePulse()
        {
            Assert.Equal(50.0, ConstantValueReader.Get<double>(typeof(DS4Windows.Processing.Touchpad.TapClickDetector), "TapClickHoldMs"));

            // 即時のタップ（ダブルタップ無効）: 指を離した時点から 50ms の間、毎フレーム左クリックを出し続ける
            var time = new ManualTimeProvider();
            var detector = new DS4Windows.Processing.Touchpad.TapClickDetector(0, time);
            var state = new DS4Windows.Processing.Synthetic.SyntheticInputState();
            DateTime t0 = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            var touch = MakeTouch(100, 100);

            detector.TouchBegan(t0, false, false, 10);
            detector.TouchEnded(new DS4Windows.Input.TouchpadEventArgs(t0.AddMilliseconds(10), false, true, touch), touch, 10, false);

            Assert.True(detector.IsPendingOrHolding);
            Assert.Equal(1, LeftClicks(detector, state));
            time.Advance(TimeSpan.FromMilliseconds(49));
            Assert.Equal(1, LeftClicks(detector, state));
            time.Advance(TimeSpan.FromMilliseconds(2));
            Assert.Equal(0, LeftClicks(detector, state));
            Assert.False(detector.IsPendingOrHolding);
        }

        [Fact]
        public void TapClick_WithDoubleTapEnabled_StartsTheHoldWhenThePendingTapIsConfirmed()
        {
            // ダブルタップ有効: 指を離したあとは保留（IsPendingOrHolding）。待ち時間が過ぎて確定したら、同じ 50ms の保持になる
            var time = new ManualTimeProvider();
            var detector = new DS4Windows.Processing.Touchpad.TapClickDetector(0, time);
            var state = new DS4Windows.Processing.Synthetic.SyntheticInputState();
            DateTime t0 = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            var touch = MakeTouch(100, 100);

            detector.TouchBegan(t0, false, true, 10);
            detector.TouchEnded(new DS4Windows.Input.TouchpadEventArgs(t0.AddMilliseconds(10), false, true, touch), touch, 10, true);
            Assert.True(detector.TappedOnce);
            Assert.True(detector.IsPendingOrHolding);

            // 確定の時刻は、渡した時計で測る（待ち時間 = 感度 10 × 1.5 = 15ms が過ぎるまでは、確定しない）
            time.Advance(TimeSpan.FromMilliseconds(14));
            LeftClicks(detector, state);
            Assert.True(detector.TappedOnce);
            time.Advance(TimeSpan.FromMilliseconds(2));
            LeftClicks(detector, state);

            Assert.False(detector.TappedOnce);
            Assert.Equal(1, LeftClicks(detector, state));
            time.Advance(TimeSpan.FromMilliseconds(51));
            Assert.Equal(0, LeftClicks(detector, state));
        }

        [Fact]
        public void TouchUnchanged_ContinuesSynthesis_WhileTapPendingOrHeld()
        {
            string text = SourceText.ReadCodeWithPartials("DS4Windows/Processing/Touchpad/TouchpadMouse.cs");
            if (text == null) return;

            Assert.Matches(@"_buttons\.ReleaseButtonActive\s*\|\|\s*releaseButtonChanged\s*\|\|\s*_tap\.IsPendingOrHolding", text);

            string tap = SourceText.ReadCode("DS4Windows/Processing/Touchpad/TapClickDetector.cs");
            if (tap == null) return;
            Assert.Matches(@"tappedOnce\s*\|\|\s*_time\.GetUtcNow\(\)\.UtcDateTime\s*<\s*tapClickHoldUntil", tap);
        }
    }
}
