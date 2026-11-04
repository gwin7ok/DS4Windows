using System;

namespace DS4Windows.Processing.Abstractions
{

    public interface INotificationService
    {

        void SendNotification(string title, string message, bool warning = false, bool temporary = false, bool isToast = true);

        event EventHandler<NotificationEventArgs> NotificationTriggered;
    }
}