using System;
using DS4Windows.UI.Abstractions;

namespace DS4WindowsTests
{
    /// <summary><see cref="IUiDispatcher"/> の偽物。呼び出したスレッドで、その場で実行する。</summary>
    public sealed class ImmediateUiDispatcher : IUiDispatcher
    {
        public bool CheckAccess() => true;
        public void Invoke(Action action) => action();
        public void BeginInvoke(Action action) => action();
    }
}
