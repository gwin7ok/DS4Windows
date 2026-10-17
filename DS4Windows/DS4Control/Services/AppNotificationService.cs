using System;
using DS4Windows.DI;

namespace DS4Windows
{
    public class AppNotificationService : INotificationService
    {
        public event EventHandler<NotificationEventArgs> NotificationTriggered;

        // Phase7-Step2 W4: INotificationService.NotificationsEnabled／FlashTaskbar は削除した（設定の読み書きは
        // IAppSettingsService.Notifications／FlashWhenLate が担う重複）。通知の有無の判定は、実装の内部判定として残す。
        private static bool NotificationsEnabled => Global.Notifications != 0;

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