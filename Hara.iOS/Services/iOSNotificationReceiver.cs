using System;
using Hara.Abstractions.Contracts;
using UserNotifications;

namespace Hara.iOS.Services.LocalNotifications.iOS
{
    public class iOSNotificationReceiver : UNUserNotificationCenterDelegate
    {
        private readonly INotificationManager _notificationManager;

        public iOSNotificationReceiver(INotificationManager notificationManager)
        {
            _notificationManager = notificationManager;
        }
        public override void WillPresentNotification(UNUserNotificationCenter center, UNNotification notification, Action<UNNotificationPresentationOptions> completionHandler)
        {
            _notificationManager.ReceiveNotification(notification.Request.Content.Title, notification.Request.Content.Body);

            // alerts are always shown for demonstration but this can be set to "None"
            // to avoid showing alerts if the app is in the foreground
            completionHandler(UNNotificationPresentationOptions.Alert);
        }
    }
}