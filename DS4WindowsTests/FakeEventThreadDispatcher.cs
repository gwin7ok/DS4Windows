using System;
using DS4Windows.Processing.Abstractions;

namespace DS4WindowsTests
{
    /// <summary>イベントスレッドの代わりに、その場で（同期して）実行し、回数を数える偽実装。</summary>
    public sealed class FakeEventThreadDispatcher : IEventThreadDispatcher
    {
        public int Queued;

        public void Queue(Action action)
        {
            Queued++;
            action?.Invoke();
        }

        public void Invoke(Action action) => action?.Invoke();

        public void Shutdown() { }
    }
}
