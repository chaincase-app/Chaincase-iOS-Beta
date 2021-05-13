using System;
using Chaincase.Common.Contracts;

namespace Chaincase.Common.Services.Mock
{
    public class MockNotificationManager : INotificationManager
    {
        public event EventHandler NotificationReceived;

        public void RequestAuthorization()
        {
        }

        public int ScheduleNotification(string title, string message, double timeInterval)
        {
            return new Random().Next();
        }

        public void ReceiveNotification(string title, string message)
        {
        }

        public void RemoveAllPendingNotifications()
        {
        }
    }
}
