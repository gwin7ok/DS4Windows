using System;

namespace DS4Windows.Processing.Abstractions
{
    /// <summary>
    /// Phase13-Step3: 専用のイベントスレッド（旧 <c>ControlService</c> が所有していた "ControlService Events"）の窓口。
    /// <see cref="Queue"/> は非同期（旧 <c>Dispatcher.BeginInvoke</c>）、<see cref="Invoke"/> は同期（旧 <c>Dispatcher.Invoke</c>）。
    /// <see cref="Shutdown"/> の後の <see cref="Queue"/>／<see cref="Invoke"/> は何もしない。
    /// </summary>
    public interface IEventThreadDispatcher
    {
        /// <summary>イベントスレッドで非同期に実行する。</summary>
        void Queue(Action action);

        /// <summary>イベントスレッドで同期的に実行する。</summary>
        void Invoke(Action action);

        /// <summary>イベントスレッドを止め、終了を待つ（旧 <c>InvokeShutdown</c> → <c>Join</c>）。</summary>
        void Shutdown();
    }
}
