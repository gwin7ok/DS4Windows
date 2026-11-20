#nullable enable

using System;
using System.Collections;
using System.Runtime.CompilerServices;
using System.Windows.Data;
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
        private static Dispatcher? Current => Application.Current?.Dispatcher;

        public bool CheckAccess() => Current?.CheckAccess() ?? true;

        public void Invoke(Action action)
        {
            if (action == null) throw new ArgumentNullException(nameof(action));
            Dispatcher? dispatcher = Current;
            if (dispatcher == null || dispatcher.CheckAccess()) action();
            else dispatcher.Invoke(action);
        }

        public void BeginInvoke(Action action)
        {
            if (action == null) throw new ArgumentNullException(nameof(action));
            Dispatcher? dispatcher = Current;
            if (dispatcher == null) action();
            else dispatcher.BeginInvoke(action);
        }

        public void BeginInvokeIdle(Action action)
        {
            if (action == null) throw new ArgumentNullException(nameof(action));
            Dispatcher? dispatcher = Current;
            if (dispatcher == null) action();
            else dispatcher.BeginInvoke(action, DispatcherPriority.ContextIdle);
        }

        public void EnableCollectionSynchronization(IEnumerable collection, object lockObject)
            => BindingOperations.EnableCollectionSynchronization(collection, lockObject);

        // WPF は同期のコールバックを弱い参照でしか持たない（匿名メソッドは回収され、「同期コールバックは使用できなくなりました」の例外になる）。
        // 包んだコールバックは、コレクションが生きている間だけ、ここで強く持つ。
        private static readonly ConditionalWeakTable<IEnumerable, CollectionSynchronizationCallback> KeepAlive = new();

        public void EnableCollectionSynchronization(IEnumerable collection, object context, Action<Action, bool> accessCallback)
        {
            CollectionSynchronizationCallback wrapped = (c, ctx, accessMethod, writeAccess) => accessCallback(accessMethod, writeAccess);
            KeepAlive.AddOrUpdate(collection, wrapped);
            BindingOperations.EnableCollectionSynchronization(collection, context, wrapped);
        }
    }
}
