using DS4Windows.Input.Devices;
using Xunit;

namespace DS4WindowsTests
{
    /// <summary>機種のタイムスタンプからの経過時間の計算（巻き戻り・同じ値・初回）。</summary>
    public class TimestampClockTests
    {
        [Fact]
        public void FirstFrame_UsesTheWholeStampAsTheDelta()
        {
            var clock = new TimestampClock(ushort.MaxValue, 16u);
            clock.Advance(300, 0.5, 10, out double elapsed, out ulong total);
            Assert.Equal(1600u, total - 10);
            Assert.Equal(0.0016, elapsed, 9);
        }

        [Fact]
        public void WrapAround_AddsTheDistanceThroughTheMaximum()
        {
            var clock = new TimestampClock(ushort.MaxValue, 16u);
            clock.Advance(65530, 0, 0, out _, out _);
            clock.Advance(5, 0, 0, out _, out ulong total);
            Assert.Equal((6u + 5u) * 16u / 3u, total);
        }

        [Fact]
        public void SameStamp_FallsBackToTheSystemClock()
        {
            var clock = new TimestampClock(uint.MaxValue, 1u);
            clock.Advance(3000, 0, 0, out _, out _);
            clock.Advance(3000, 0.004, 100, out double elapsed, out ulong total);
            Assert.Equal(0.004, elapsed);
            Assert.Equal(100UL + 4000UL, total);
        }
    }
}
