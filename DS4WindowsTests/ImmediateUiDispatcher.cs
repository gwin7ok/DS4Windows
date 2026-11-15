using System;
using System.Collections;
using DS4Windows.UI.Abstractions;

namespace DS4WindowsTests
{
    /// <summary><see cref="IUiDispatcher"/> の偽物。呼び出したスレッドで、その場で実行する。</summary>
    public sealed class ImmediateUiDispatcher : IUiDispatcher
    {
        public bool CheckAccess() => true;
        public void Invoke(Action action) => action();
        public void BeginInvoke(Action action) => action();
        public void BeginInvokeIdle(Action action) => action();
        public void EnableCollectionSynchronization(IEnumerable collection, object lockObject) { }
        public void EnableCollectionSynchronization(IEnumerable collection, object context, Action<Action, bool> accessCallback) { }
    }
}
