using SystemTrayProcessManager.Core.Models;

namespace SystemTrayProcessManager.Core.Services
{
    /// <summary>
    /// Provides in-app notification management for displaying process events and system status messages.
    /// Notifications are displayed in the main dashboard notification panel.
    /// </summary>
    public interface INotificationService
    {
        /// <summary>
        /// Gets the currently active (non-dismissed) notifications.
        /// </summary>
        IReadOnlyList<AppNotification> ActiveNotifications { get; }

        /// <summary>
        /// Adds a new notification and raises the <see cref="NotificationAdded"/> event.
        /// </summary>
        /// <param name="notification">The notification to add.</param>
        void AddNotification(AppNotification notification);

        /// <summary>
        /// Dismisses a notification by its unique identifier.
        /// </summary>
        /// <param name="notificationId">The ID of the notification to dismiss.</param>
        void DismissNotification(string notificationId);

        /// <summary>
        /// Dismisses all currently active notifications.
        /// </summary>
        void DismissAll();

        /// <summary>
        /// Gets the total count of active (non-dismissed) notifications.
        /// </summary>
        int ActiveCount { get; }

        /// <summary>
        /// Occurs when a new notification is added.
        /// </summary>
        event EventHandler<AppNotification>? NotificationAdded;

        /// <summary>
        /// Occurs when a notification is dismissed.
        /// </summary>
        event EventHandler<string>? NotificationDismissed;
    }
}
