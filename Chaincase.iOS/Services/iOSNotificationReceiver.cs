using System;
using Chaincase.Common.Contracts;
using UserNotifications;

namespace Chaincase.iOS.Services
{
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE1006:Naming Styles", Justification = "iOS is iOS")]
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
            completionHandler(UNNotificationPresentationOptions.Sound);
        }
    }
}
