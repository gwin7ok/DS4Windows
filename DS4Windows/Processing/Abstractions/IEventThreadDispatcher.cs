using System;

namespace DS4Windows.Processing.Abstractions
{
    /// <summary>
    /// 専用のイベントスレッドの窓口。
    /// <see cref="Queue"/> は非同期、<see cref="Invoke"/> は同期。
    /// <see cref="Shutdown"/> の後の <see cref="Queue"/>／<see cref="Invoke"/> は何もしない。
    /// </summary>
    public interface IEventThreadDispatcher
    {
        /// <summary>イベントスレッドで非同期に実行する。</summary>
        void Queue(Action action);

        /// <summary>イベントスレッドで同期的に実行する。</summary>
        void Invoke(Action action);

        /// <summary>イベントスレッドを止め、終了を待つ。</summary>
        void Shutdown();
    }
}
