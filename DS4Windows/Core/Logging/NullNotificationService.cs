using System;
using System.Threading;
using DS4Windows.Core.Abstractions;

namespace DS4Windows.Core.Logging
{
    /// <summary>
    /// Phase14b-Step3（R-03）: 通知を何もしない <see cref="INotificationService"/>。<see cref="AppLogger.NotificationService"/> が
    /// まだ設定されていないとき（App の <c>InitializePostHostServices</c> より前、テスト）に使う。
    /// 旧フォールバックは <c>new AppNotificationService(new BackingStore(new EnvironmentService()))</c> で、設定の唯一の実体とは別の
    /// <c>BackingStore</c> を作っていた。その通知には購読者がいなかった（画面は DI の Singleton を購読する）ため、表示の挙動は同じ。
    /// 落とした通知があったことは、最初の 1 回だけ Debug のログに残す。
    /// </summary>
    public sealed class NullNotificationService : INotificationService
    {
        private int _droppedLogged;

        public void SendNotification(string title, string message, bool warning = false, bool temporary = false, bool isToast = true)
        {
            if (Interlocked.Exchange(ref _droppedLogged, 1) == 0)
                AppLogger.Log(AppLogLevel.Debug, "[DI] AppLogger.NotificationService: not set yet; notification dropped");
        }

        // 発火しない（購読しても何も届かない）
        public event EventHandler<NotificationEventArgs> NotificationTriggered
        {
            add { }
            remove { }
        }
    }
}
