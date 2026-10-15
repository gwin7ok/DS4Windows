using System;

namespace DS4Windows.DI
{

    public interface INotificationService
    {
        bool NotificationsEnabled { get; set; }
        bool FlashTaskbar { get; set; }

        void SendNotification(string title, string message, bool warning = false, bool temporary = false, bool isToast = true);

        event EventHandler<NotificationEventArgs> NotificationTriggered;
    }
}