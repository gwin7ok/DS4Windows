using System;
using DS4Windows.UI.Services;
using Xunit;

namespace DS4WindowsTests
{
    /// <summary>
    /// <see cref="GuardedTimerTick"/>（「止めて、処理して、始め直す」タイマーの 1 回ぶん）のテスト。
    /// 処理が例外を出しても始め直されること、終了のときは始め直さないことを守る。
    /// </summary>
    public class GuardedTimerTickTests
    {
        [Fact]
        public void WhenTheBodyThrows_TheTimerIsStillRestarted_AndTheErrorIsReported()
        {
            int restarts = 0;
            Exception reported = null;

            GuardedTimerTick.Run(
                () => throw new InvalidOperationException("boom"),
                () => true,
                () => restarts++,
                ex => reported = ex);

            Assert.Equal(1, restarts);
            Assert.IsType<InvalidOperationException>(reported);
        }

        [Fact]
        public void WhenTheBodySucceeds_TheTimerIsRestartedOnce_AndNoErrorIsReported()
        {
            int restarts = 0;
            int errors = 0;

            GuardedTimerTick.Run(() => { }, () => true, () => restarts++, ex => errors++);

            Assert.Equal(1, restarts);
            Assert.Equal(0, errors);
        }

        [Fact]
        public void WhenRestartIsNotAllowed_TheTimerStaysStopped_EvenIfTheBodyThrows()
        {
            int restarts = 0;

            GuardedTimerTick.Run(
                () => throw new InvalidOperationException("boom"),
                () => false,
                () => restarts++,
                ex => { });

            Assert.Equal(0, restarts);
        }

        [Fact]
        public void WhenTheErrorReportItselfThrows_TheTimerIsStillRestarted()
        {
            int restarts = 0;

            GuardedTimerTick.Run(
                () => throw new InvalidOperationException("boom"),
                () => true,
                () => restarts++,
                ex => throw new InvalidOperationException("log failed"));

            Assert.Equal(1, restarts);
        }
    }
}
