#nullable enable

using System;
using System.Collections.Generic;

namespace DS4Windows.Input
{
    /// <summary>
    /// 他のスレッドから積まれた処理を、読み取りのループのスレッドで実行するための待ち行列。
    /// 積むのは任意のスレッド、実行は読み取りのスレッドの末尾だけ（デバイスの状態へ触る処理を、ループに直列化する）。
    /// </summary>
    public sealed class DeviceEventQueue
    {
        private readonly Queue<Action> _queue = new Queue<Action>();
        private readonly object _lock = new object();
        private volatile bool _hasEvents;

        public void Enqueue(Action act)
        {
            lock (_lock)
            {
                _queue.Enqueue(act);
                _hasEvents = true;
            }
        }

        /// <summary>その時点の件数だけを実行する（実行中に積まれた処理は、次のフレームへ回す）。</summary>
        public void RunPending()
        {
            if (!_hasEvents)
            {
                return;
            }

            lock (_lock)
            {
                for (int actInd = 0, actLen = _queue.Count; actInd < actLen; actInd++)
                {
                    _queue.Dequeue().Invoke();
                }

                _hasEvents = false;
            }
        }
    }
}
