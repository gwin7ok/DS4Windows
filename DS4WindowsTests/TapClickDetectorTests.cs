using System;
using System.Reflection;
using DS4Windows.Input;
using DS4Windows.Processing.Synthetic;
using DS4Windows.Processing.Touchpad;
using Xunit;

namespace DS4WindowsTests
{
    /// <summary>タップ・ダブルタップの判定（<see cref="TapClickDetector"/>）。TouchpadMouse から切り出したときの特性化。</summary>
    public class TapClickDetectorTests
    {
        private const byte Sens = 100;   // タップの感度（ms）。時間の上限は感度 × 2＝200 ms、ダブルタップの待ちは × 1.5＝150 ms

        private static Touch MakeTouch(int x, int y) =>
            (Touch)Activator.CreateInstance(typeof(Touch), BindingFlags.Instance | BindingFlags.NonPublic,
                null, new object[] { x, y, (byte)1, null }, null);

        private static TouchpadEventArgs End(DateTime ts, Touch t, bool buttonPressed = false) =>
            new TouchpadEventArgs(ts, buttonPressed, false, t);

        private static int LeftClicks(SyntheticInputState s) => s.DeviceState[0].currentClicks.leftCount;

        private static int Count(TapClickDetector d, SyntheticInputState s)
        {
            s.DeviceState[0].currentClicks.leftCount = 0;
            d.CountPendingClicks(s, Sens);
            return LeftClicks(s);
        }

        [Fact]
        public void QuickTapWithoutMovement_HoldsTheLeftClickFor50ms()
        {
            var time = new ManualTimeProvider();
            var d = new TapClickDetector(0, time);
            var s = new SyntheticInputState();
            Touch first = MakeTouch(500, 500);
            DateTime t0 = DateTime.UtcNow;

            d.TouchBegan(t0, false, false, Sens);
            d.TouchEnded(End(t0.AddMilliseconds(120), MakeTouch(510, 505)), first, Sens, false);

            Assert.True(d.IsPendingOrHolding);
            Assert.Equal(1, Count(d, s));
            time.Advance(TimeSpan.FromMilliseconds(49));
            Assert.Equal(1, Count(d, s));
            time.Advance(TimeSpan.FromMilliseconds(2));
            Assert.Equal(0, Count(d, s));
            Assert.False(d.IsPendingOrHolding);
        }

        [Fact]
        public void ASlowTouch_IsNotATap()
        {
            var d = new TapClickDetector(0, new ManualTimeProvider());
            Touch first = MakeTouch(500, 500);
            DateTime t0 = DateTime.UtcNow;

            d.TouchBegan(t0, false, false, Sens);
            d.TouchEnded(End(t0.AddMilliseconds(201), MakeTouch(500, 500)), first, Sens, false);

            Assert.False(d.IsPendingOrHolding);
        }

        [Fact]
        public void ATouchThatMovedTooFar_IsNotATap_AndTheLimitIsExclusive()
        {
            var d = new TapClickDetector(0, new ManualTimeProvider());
            Touch first = MakeTouch(500, 500);
            DateTime t0 = DateTime.UtcNow;

            d.TouchBegan(t0, false, false, Sens);
            d.TouchEnded(End(t0.AddMilliseconds(50), MakeTouch(600, 500)), first, Sens, false);   // ずれ 100＝範囲外
            Assert.False(d.IsPendingOrHolding);

            d.TouchBegan(t0, false, false, Sens);
            d.TouchEnded(End(t0.AddMilliseconds(50), MakeTouch(599, 500)), first, Sens, false);   // ずれ 99＝範囲内
            Assert.True(d.IsPendingOrHolding);
        }

        [Fact]
        public void APressedTouchButton_IsNotATap()
        {
            var d = new TapClickDetector(0, new ManualTimeProvider());
            Touch first = MakeTouch(500, 500);
            DateTime t0 = DateTime.UtcNow;

            d.TouchBegan(t0, true, false, Sens);
            d.TouchEnded(End(t0.AddMilliseconds(50), MakeTouch(500, 500), buttonPressed: true), first, Sens, false);

            Assert.False(d.IsPendingOrHolding);
        }

        [Fact]
        public void DoubleTapEnabled_KeepsTheFirstTapPending_UntilTheWaitTimeHasPassed()
        {
            var time = new ManualTimeProvider();
            var d = new TapClickDetector(0, time);
            Touch first = MakeTouch(500, 500);
            DateTime t0 = DateTime.UtcNow;

            d.TouchBegan(t0, false, true, Sens);
            d.TouchEnded(End(t0.AddMilliseconds(80), MakeTouch(500, 500)), first, Sens, true);

            Assert.True(d.TappedOnce);
            Assert.True(d.IsPendingOrHolding);
        }

        [Fact]
        public void ASecondTouchSoonAfterTheFirstTap_IsReportedAsTapAndHold_AndEndsWithTheTouch()
        {
            var d = new TapClickDetector(0, new ManualTimeProvider());
            Touch first = MakeTouch(500, 500);
            DateTime t0 = DateTime.UtcNow;

            d.TouchBegan(t0, false, true, Sens);
            DateTime end1 = t0.AddMilliseconds(80);
            d.TouchEnded(End(end1, MakeTouch(500, 500)), first, Sens, true);

            d.TouchBegan(end1.AddMilliseconds(100), false, true, Sens);   // 150 ms 以内の 2 回目
            Assert.True(d.SecondTouchBegan);

            d.TouchEnded(End(end1.AddMilliseconds(180), MakeTouch(500, 500)), first, Sens, true);
            Assert.False(d.SecondTouchBegan);
            // 2 回目も短い触れなので、元の実装のとおり、また 1 回目として保留される（ダブルタップのクリックは、2 回目の触れの間に出ている）
            Assert.True(d.TappedOnce);
        }

        [Fact]
        public void ATouchAfterTheDoubleTapWait_IsNotASecondTouch()
        {
            var d = new TapClickDetector(0, new ManualTimeProvider());
            Touch first = MakeTouch(500, 500);
            DateTime t0 = DateTime.UtcNow;

            d.TouchBegan(t0, false, true, Sens);
            DateTime end1 = t0.AddMilliseconds(80);
            d.TouchEnded(End(end1, MakeTouch(500, 500)), first, Sens, true);

            d.TouchBegan(end1.AddMilliseconds(151), false, true, Sens);
            Assert.False(d.SecondTouchBegan);
        }
    }
}
