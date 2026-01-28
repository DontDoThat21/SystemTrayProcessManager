namespace SystemTrayProcessManager.Core.Models
{
    /// <summary>
    /// Represents the severity level of an in-app notification.
    /// </summary>
    public enum NotificationLevel
    {
        /// <summary>Informational message.</summary>
        Info,

        /// <summary>Success message.</summary>
        Success,

        /// <summary>Warning message.</summary>
        Warning,

        /// <summary>Error message.</summary>
        Error
    }

    /// <summary>
    /// Represents an in-app notification displayed in the notification panel.
    /// Provides information about process events and system status changes.
    /// </summary>
    public sealed class AppNotification
    {
        /// <summary>
        /// Gets the unique identifier for this notification.
        /// </summary>
        public string Id { get; init; } = Guid.NewGuid().ToString("N");

        /// <summary>
        /// Gets the notification title.
        /// </summary>
        public string Title { get; init; } = string.Empty;

        /// <summary>
        /// Gets the notification message body.
        /// </summary>
        public string Message { get; init; } = string.Empty;

        /// <summary>
        /// Gets the severity level of the notification.
        /// </summary>
        public NotificationLevel Level { get; init; } = NotificationLevel.Info;

        /// <summary>
        /// Gets the timestamp when the notification was created.
        /// </summary>
        public DateTime Timestamp { get; init; } = DateTime.Now;

        /// <summary>
        /// Gets or sets a value indicating whether the notification has been dismissed.
        /// </summary>
        public bool IsDismissed { get; set; }

        /// <summary>
        /// Gets the auto-dismiss duration in milliseconds. 0 means no auto-dismiss.
        /// </summary>
        public int AutoDismissMs { get; init; } = 3000;

        /// <summary>
        /// Gets a display-friendly time string (e.g., "just now", "2m ago").
        /// </summary>
        public string TimeAgo
        {
            get
            {
                var elapsed = DateTime.Now - Timestamp;
                if (elapsed.TotalSeconds < 30) return "just now";
                if (elapsed.TotalMinutes < 1) return $"{(int)elapsed.TotalSeconds}s ago";
                if (elapsed.TotalHours < 1) return $"{(int)elapsed.TotalMinutes}m ago";
                if (elapsed.TotalDays < 1) return $"{(int)elapsed.TotalHours}h ago";
                return Timestamp.ToString("MMM d, HH:mm");
            }
        }

        /// <summary>
        /// Creates an informational notification.
        /// </summary>
        /// <param name="title">The notification title.</param>
        /// <param name="message">The notification message.</param>
        /// <returns>A new info notification instance.</returns>
        public static AppNotification Info(string title, string message) =>
            new() { Title = title, Message = message, Level = NotificationLevel.Info };

        /// <summary>
        /// Creates a success notification.
        /// </summary>
        /// <param name="title">The notification title.</param>
        /// <param name="message">The notification message.</param>
        /// <returns>A new success notification instance.</returns>
        public static AppNotification Success(string title, string message) =>
            new() { Title = title, Message = message, Level = NotificationLevel.Success };

        /// <summary>
        /// Creates a warning notification.
        /// </summary>
        /// <param name="title">The notification title.</param>
        /// <param name="message">The notification message.</param>
        /// <returns>A new warning notification instance.</returns>
        public static AppNotification Warn(string title, string message) =>
            new() { Title = title, Message = message, Level = NotificationLevel.Warning };

        /// <summary>
        /// Creates an error notification.
        /// </summary>
        /// <param name="title">The notification title.</param>
        /// <param name="message">The notification message.</param>
        /// <returns>A new error notification instance.</returns>
        public static AppNotification Error(string title, string message) =>
            new() { Title = title, Message = message, Level = NotificationLevel.Error };

        /// <inheritdoc/>
        public override string ToString() =>
            $"[{Level}] {Title}: {Message} ({TimeAgo})";
    }
}
