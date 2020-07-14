using System;
namespace Chaincase.Notifications
{
    // https://docs.microsoft.com/en-us/xamarin/xamarin-forms/app-fundamentals/local-notifications#create-the-ios-interface-implementation
    public interface INotificationManager
    {
        event EventHandler NotificationReceived;

        void Initialize();

        int ScheduleNotification(string title, string message, double timeInterval);

        void ReceiveNotification(string title, string message);

        void RemoveAllPendingNotifications();
    }
}
