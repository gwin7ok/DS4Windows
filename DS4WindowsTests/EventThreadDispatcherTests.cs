using System;
using System.Threading;
using Xunit;
using DS4Windows.Processing.Services;

namespace DS4WindowsTests
{
    /// <summary>Phase13-Step3: 旧 <c>ControlService</c> のイベントスレッド（生成・実行・破棄）の特性化。</summary>
    public class EventThreadDispatcherTests
    {
        [Fact]
        public void Thread_HasTheSameNameAndSettings_AsTheOldControlServiceThread()
        {
            var d = new EventThreadDispatcher();
            try
            {
                Thread t = null;
                d.Invoke(() => t = Thread.CurrentThread);
                Assert.NotNull(t);
                Assert.Equal("ControlService Events", t.Name);
                Assert.Equal(ThreadPriority.BelowNormal, t.Priority);
                Assert.True(t.IsBackground);
                Assert.NotEqual(Thread.CurrentThread.ManagedThreadId, t.ManagedThreadId);
            }
            finally { d.Shutdown(); }
        }

        [Fact]
        public void Queue_RunsOnTheEventThread_AndInvoke_IsSynchronous()
        {
            var d = new EventThreadDispatcher();
            try
            {
                using var done = new ManualResetEventSlim(false);
                int queuedThread = -1;
                d.Queue(() => { queuedThread = Thread.CurrentThread.ManagedThreadId; done.Set(); });   // 生成直後でも失われない
                Assert.True(done.Wait(5000));
                Assert.NotEqual(Thread.CurrentThread.ManagedThreadId, queuedThread);

                int ran = 0;
                d.Invoke(() => ran = 1);
                Assert.Equal(1, ran);
            }
            finally { d.Shutdown(); }
        }

        [Fact]
        public void AfterShutdown_QueueAndInvokeDoNothing_AndShutdownIsIdempotent()
        {
            var d = new EventThreadDispatcher();
            d.Shutdown();
            int ran = 0;
            Assert.Null(Record.Exception(() => d.Queue(() => ran++)));
            Assert.Null(Record.Exception(() => d.Invoke(() => ran++)));
            Assert.Null(Record.Exception(() => d.Shutdown()));
            Assert.Equal(0, ran);
        }
    }
}
