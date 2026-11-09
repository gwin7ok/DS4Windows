using System;
using System.Collections.Concurrent;
using System.Threading;

namespace DS4WindowsTests
{
    /// <summary>
    /// 実物の画面部品（UserControl）を読み込むテストを、プロセス全体で共有する 1 本の STA スレッドで実行する。
    /// WPF の Application とその Dispatcher は、最初に作ったスレッドに結び付く。テストごとに別のスレッドで作ると、
    /// 終わったスレッドの Dispatcher を待って止まることがあるため、スレッドを使い回す。
    /// </summary>
    internal static class WpfPanelTestRunner
    {
        private static readonly BlockingCollection<Action> Queue = new BlockingCollection<Action>();
        private static readonly object Gate = new object();
        private static Thread worker;

        public static void Run(Action body)
        {
            lock (Gate)
            {
                if (worker == null)
                {
                    worker = new Thread(() =>
                    {
                        // Application は、最初に作ったスレッドに結び付く。他のテストが作っていなければ、ここで作る
                        if (System.Windows.Application.Current == null)
                        {
                            new System.Windows.Application { ShutdownMode = System.Windows.ShutdownMode.OnExplicitShutdown };
                        }
                        foreach (Action job in Queue.GetConsumingEnumerable()) job();
                    });
                    worker.SetApartmentState(ApartmentState.STA);
                    worker.IsBackground = true;
                    worker.Start();
                }
            }

            Exception failure = null;
            using (var done = new ManualResetEventSlim(false))
            {
                Queue.Add(() =>
                {
                    try { body(); }
                    catch (Exception ex) { failure = ex; }
                    finally { done.Set(); }
                });
                if (!done.Wait(60000)) throw new TimeoutException("パネルのテストが終わらない（共有の STA スレッドが止まっている）");
            }

            if (failure != null) throw new InvalidOperationException("パネルの読み込みに失敗: " + failure, failure);
        }
    }
}
