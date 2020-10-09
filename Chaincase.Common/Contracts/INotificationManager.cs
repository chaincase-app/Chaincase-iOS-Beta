using System;

namespace Chaincase.Common
{
    public interface INotificationManager
    {
        event EventHandler NotificationReceived;

        void RequestAuthorization();

        int ScheduleNotification(string title, string message, double timeInterval);

        void ReceiveNotification(string title, string message);

        void RemoveAllPendingNotifications();
    }
    
    public class NotificationEventArgs : EventArgs
    {
        public string Title { get; set; }
        public string Message { get; set; }
    }
}