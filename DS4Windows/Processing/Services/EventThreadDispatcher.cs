using System;
using System.Threading;
using System.Windows.Threading;
using DS4Windows.Processing.Abstractions;

namespace DS4Windows.Processing.Services
{
    /// <summary>
    /// Phase13-Step3: <see cref="IEventThreadDispatcher"/> の実装。旧 <c>ControlService</c> のコンストラクターにあったスレッドの生成
    /// （名前 "ControlService Events"、<c>BelowNormal</c>、<c>IsBackground</c>、<c>Dispatcher.Run</c>）と、<c>ShutDown</c> の
    /// <c>InvokeShutdown</c> → <c>Join</c> を、そのまま移した。生成直後に使っても失われないよう、スレッド内の Dispatcher の確定を待つ。
    /// </summary>
    public sealed class EventThreadDispatcher : IEventThreadDispatcher
    {
        private readonly object _lock = new object();
        private readonly ManualResetEventSlim _ready = new ManualResetEventSlim(false);
        private Thread _thread;
        private Dispatcher _dispatcher;

        public EventThreadDispatcher()
        {
            _thread = new Thread(() =>
            {
                Dispatcher current = Dispatcher.CurrentDispatcher;
                _dispatcher = current;
                _ready.Set();
                Dispatcher.Run();
            });
            _thread.IsBackground = true;
            _thread.Priority = ThreadPriority.BelowNormal;
            _thread.Name = "ControlService Events";
            _thread.Start();
        }

        private Dispatcher Current()
        {
            Thread thread = _thread;
            if (thread == null) return null;
            _ready.Wait();
            return _dispatcher;
        }

        public void Queue(Action action)
        {
            Dispatcher d = Current();
            d?.BeginInvoke(action);
        }

        public void Invoke(Action action)
        {
            Dispatcher d = Current();
            d?.Invoke(action);
        }

        public void Shutdown()
        {
            Thread thread;
            Dispatcher dispatcher;
            lock (_lock)
            {
                thread = _thread;
                if (thread == null) return;
                _ready.Wait();
                dispatcher = _dispatcher;
                _thread = null;
                _dispatcher = null;
            }

            dispatcher.InvokeShutdown();
            thread.Join();
        }
    }
}
