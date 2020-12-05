using System;
using System.Threading.Tasks;
using Hara.Abstractions.Contracts;
using UserNotifications;

namespace Hara.iOS.Services
{
    namespace LocalNotifications.iOS
    {
        public class iOSNotificationManager : INotificationManager
        {
            int messageId = -1;

            bool hasNotificationsPermission;
            public bool Initialized { get; private set; }
            public event EventHandler<NotificationEventArgs> NotificationReceived;

            public Task<bool> Initialize()
            {
                // request the permission to use local notifications
                UNUserNotificationCenter.Current.RequestAuthorization(UNAuthorizationOptions.Alert, (approved, err) =>
                {
                    hasNotificationsPermission = approved;
                });
                Initialized = true;
                return Task.FromResult(true);
            }

            public Task<string> ScheduleNotification(string title, string message)
            {
                // EARLY OUT: app doesn't have permissions
                if(!hasNotificationsPermission)
                {
                    return Task.FromResult("");
                }

                messageId++;

                var content = new UNMutableNotificationContent()
                {
                    Title = title,
                    Subtitle = "",
                    Body = message,
                    Badge = 1
                };

                // Local notifications can be time or location based
                // Create a time-based trigger, interval is in seconds and must be greater than 0
                var trigger = UNTimeIntervalNotificationTrigger.CreateTrigger(0.25, false);

                var request = UNNotificationRequest.FromIdentifier(messageId.ToString(), content, trigger);
                UNUserNotificationCenter.Current.AddNotificationRequest(request, (err) =>
                {
                    if (err != null)
                    {
                        throw new Exception($"Failed to schedule notification: {err}");
                    }
                });

                return Task.FromResult(messageId.ToString());
            }

            public void ReceiveNotification(string title, string message)
            {
                var args = new NotificationEventArgs()
                {
                    Title = title,
                    Message = message
                };
                NotificationReceived?.Invoke(null, args);
            }
        }
    }
}