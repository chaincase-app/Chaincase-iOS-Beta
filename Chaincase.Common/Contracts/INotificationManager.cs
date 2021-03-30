using System;
using WalletWasabi.Blockchain.TransactionProcessing;
using WalletWasabi.Helpers;
using WalletWasabi.Logging;

namespace Chaincase.Common.Contracts
{
    public interface INotificationManager
    {
        event EventHandler NotificationReceived;

        void RequestAuthorization();

        int ScheduleNotification(string title, string message, double timeInterval);

        void ReceiveNotification(string title, string message);

        void RemoveAllPendingNotifications();

        void NotifyAndLog(string message, string title, NotificationType notificationType, ProcessedResult e)
        {
            message = Guard.Correct(message);
            title = Guard.Correct(title);
            // other types are best left logged for now
            if (notificationType == NotificationType.Success)
            {
                ScheduleNotification(title, message, 1);
            }
            Logger.LogInfo($"Transaction Notification ({notificationType}): {title} - {message} - {e.Transaction.GetHash()}");
        }
    }
    
    public class NotificationEventArgs : EventArgs
    {
        public string Title { get; set; }
        public string Message { get; set; }
    }

    /// <summary>
    /// Enumeration of types for <see cref="T:Avalonia.Controls.Notifications.INotification" />.
    /// </summary>
    public enum NotificationType
    {
        Information,
        Success,
        Warning,
        Error
    }
}