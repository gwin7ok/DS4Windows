#nullable enable

using System;
using DS4Windows.Core.Logging;
using DS4Windows.Core.Settings;
using DS4Windows.Core.Abstractions;

namespace DS4Windows.UI.Services
{
    public class AppNotificationService : INotificationService
    {
        public event EventHandler<NotificationEventArgs>? NotificationTriggered;

        // INotificationService.NotificationsEnabled／FlashTaskbar は削除した（設定の読み書きは
        // IAppSettingsService.Notifications／FlashWhenLate が担う重複）。通知の有無の判定は、実装の内部判定として残す。
        // 注入された BackingStore の値で判定する（Global 経由をやめた）。
        private readonly BackingStore _config;

        public AppNotificationService(BackingStore config)
        {
            ArgumentNullException.ThrowIfNull(config);
            _config = config;
        }

        private bool NotificationsEnabled => _config.notifications != 0;

        public void SendNotification(string title, string message, bool warning = false, bool temporary = false, bool isToast = true)
        {
            if (!NotificationsEnabled)
                return;

            if (AppLogger.IsTraceEnabled)
                AppLogger.LogTrace($"[DI] AppNotificationService.SendNotification: '{title}' - '{message}' (warning={warning}, temporary={temporary})");

            NotificationTriggered?.Invoke(this, new NotificationEventArgs(title, message, warning, temporary, isToast));
        }
    }
}