using System;
using System.Windows;
using System.Windows.Threading;
using DS4Windows.UI.Abstractions;

namespace DS4Windows.UI.Services
{
    /// <summary>
    /// <see cref="IUiDispatcher"/> の実装。WPF アプリケーションの Dispatcher を使う。
    /// アプリケーションがない（終了後・テスト）ときは、呼び出したスレッドでそのまま実行する。
    /// </summary>
    public sealed class WpfUiDispatcher : IUiDispatcher
    {
        private static Dispatcher Current => Application.Current?.Dispatcher;

        public bool CheckAccess() => Current?.CheckAccess() ?? true;

        public void Invoke(Action action)
        {
            if (action == null) throw new ArgumentNullException(nameof(action));
            Dispatcher dispatcher = Current;
            if (dispatcher == null || dispatcher.CheckAccess()) action();
            else dispatcher.Invoke(action);
        }

        public void BeginInvoke(Action action)
        {
            if (action == null) throw new ArgumentNullException(nameof(action));
            Dispatcher dispatcher = Current;
            if (dispatcher == null) action();
            else dispatcher.BeginInvoke(action);
        }
    }
}
