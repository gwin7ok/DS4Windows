using System;
using System.Threading;

namespace DS4Windows.Composition.Startup
{
    /// <summary>
    /// 単一起動の仕組み。2 つ目のインスタンスは、名前つきイベントを通して 1 つ目へ知らせて終了し、
    /// 1 つ目は、知らせを受けたら（別スレッドで待っている）、画面を前面へ出す処理を呼ぶ。
    /// </summary>
    public sealed class SingleInstanceGuard
    {
        private const string SingleAppComEventName = "{a52b5b20-d9ee-4f32-8518-307fa14aa0c6}";

        private EventWaitHandle _comEvent;
        private Thread _listenerThread;
        private volatile bool _exit;
        private Action _onSecondLaunch;

        /// <summary>別のインスタンスが動いていれば、そちらへ知らせて true を返す（このインスタンスは終了する）。</summary>
        public bool TryNotifyExistingInstance()
        {
            // another instance is already running if TryOpenExisting returns true.
            try
            {
                if (EventWaitHandleAcl.TryOpenExisting(SingleAppComEventName,
                    System.Security.AccessControl.EventWaitHandleRights.Synchronize |
                    System.Security.AccessControl.EventWaitHandleRights.Modify,
                    out EventWaitHandle tempComEvent))
                {
                    tempComEvent.Set();  // signal the other instance.
                    tempComEvent.Close();
                    return true;
                }
            }
            catch (System.UnauthorizedAccessException)
            {
                // Ignore exception
            }
            return false;
        }

        /// <summary>
        /// 知らせの待ち受けを始める。<paramref name="onSecondLaunch"/> は待ち受けのスレッドから呼ばれる
        /// （画面の操作は、呼び出し側が UI スレッドへ渡すこと）。
        /// </summary>
        public void StartListening(Action onSecondLaunch)
        {
            _onSecondLaunch = onSecondLaunch ?? throw new ArgumentNullException(nameof(onSecondLaunch));

            // Create the Event handle
            _comEvent = new EventWaitHandle(false, EventResetMode.ManualReset, SingleAppComEventName);

            _listenerThread = new Thread(ListenerWork);
            _listenerThread.Priority = ThreadPriority.Lowest;
            _listenerThread.IsBackground = true;
            _listenerThread.Start();
        }

        private void ListenerWork()
        {
            while (!_exit)
            {
                // check for a signal.
                if (_comEvent.WaitOne())
                {
                    _comEvent.Reset();
                    // The user tried to start another instance. We can't allow that,
                    // so bring the other instance back into view and enable that one.
                    if (!_exit)
                    {
                        _onSecondLaunch();
                    }
                }
            }
        }

        /// <summary>待ち受けを止め、イベントを閉じる（終了処理）。待ち受けを始めていなければ何もしない。</summary>
        public void Stop()
        {
            _exit = true;
            if (_comEvent != null)
            {
                _comEvent.Set();  // wake the listener so that it can exit
                while (_listenerThread.IsAlive)
                    Thread.SpinWait(500);
                _comEvent.Close();
            }
        }
    }
}
