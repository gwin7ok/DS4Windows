using System;
using System.Collections.ObjectModel;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Windows.Data;
using System.Windows.Threading;
using DS4Windows.UI.Services;
using Xunit;

namespace DS4WindowsTests
{
    /// <summary>
    /// <see cref="WpfUiDispatcher.EnableCollectionSynchronization(System.Collections.IEnumerable, object, Action{Action, bool})"/> が、
    /// 包んだ同期コールバックを、コレクションが生きている間は保持することを守る。
    /// WPF はコールバックを弱い参照でしか持たないため、保持しないと、ガベージコレクションのあとの更新で
    /// 「同期コールバックは使用できなくなりました」の例外になり、アプリが落ちる（トレイの Stop で再現した）。
    /// </summary>
    public class WpfUiDispatcherCollectionSyncTests
    {
        [Fact]
        public void TheWrappedCallback_SurvivesGarbageCollection()
        {
            int calls = 0;
            Exception failure = null;

            var thread = new Thread(() =>
            {
                try
                {
                    // BindingOperations は、有効にしたスレッドの Dispatcher に結びつく
                    _ = Dispatcher.CurrentDispatcher;
                    var collection = new ObservableCollection<int>();
                    Enable(collection, () => Interlocked.Increment(ref calls));

                    for (int i = 0; i < 3; i++)
                    {
                        GC.Collect();
                        GC.WaitForPendingFinalizers();
                    }

                    BindingOperations.AccessCollection(collection, () => { }, true);
                }
                catch (Exception ex)
                {
                    failure = ex;
                }
            });
            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
            thread.Join();

            Assert.Null(failure);
            Assert.Equal(1, calls);
        }

        // 呼び出し元に、コールバックへの強い参照を残さないための分離
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void Enable(ObservableCollection<int> collection, Action onAccess)
        {
            new WpfUiDispatcher().EnableCollectionSynchronization(collection, new object(), (access, write) =>
            {
                onAccess();
                access?.Invoke();
            });
        }
    }
}
