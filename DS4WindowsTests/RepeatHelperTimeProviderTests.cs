using System;
using DS4Windows.Processing;
using Xunit;

namespace DS4WindowsTests
{
    /// <summary>
    /// キーの繰り返し（<see cref="RepeatHelper"/>）が、渡された <see cref="TimeProvider"/> のタイマーで動くことを、
    /// 実時間を待たずに確かめる。
    /// </summary>
    public class RepeatHelperTimeProviderTests
    {
        private static RepeatHelper Create(ManualTimeProvider time, bool sendFirstImmediate)
            => new RepeatHelper(TestActions.Dispatcher, 0, 65, 65, false, new MockVirtualKBM(), time, intervalMillis: 50, sendFirstImmediate: sendFirstImmediate);

        [Fact]
        public void Start_ArmsAnIntervalTimer_OnTheInjectedTimeProvider()
        {
            var time = new ManualTimeProvider();
            using (var helper = Create(time, sendFirstImmediate: false))
            {
                Assert.Equal(0, time.ActiveTimerCount);
                helper.Start();
                Assert.Equal(1, time.ActiveTimerCount);
                time.Advance(TimeSpan.FromMilliseconds(200));
                Assert.Equal(1, time.ActiveTimerCount);
                helper.Stop();
                Assert.Equal(0, time.ActiveTimerCount);
            }
        }

        [Fact]
        public void Dispose_DisarmsTheTimer()
        {
            var time = new ManualTimeProvider();
            var helper = Create(time, sendFirstImmediate: true);
            Assert.Equal(1, time.ActiveTimerCount);
            helper.Dispose();
            Assert.Equal(0, time.ActiveTimerCount);
        }

        [Fact]
        public void Constructor_RejectsANullTimeProvider()
        {
            Assert.Throws<ArgumentNullException>(() => new RepeatHelper(TestActions.Dispatcher, 0, 65, 65, false, new MockVirtualKBM(), null, 50, true));
        }
    }
}
