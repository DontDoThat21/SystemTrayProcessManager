using CommunityToolkit.Mvvm.ComponentModel;
using SystemTrayProcessManager.Core.Models;

namespace SystemTrayProcessManager.UI.ViewModels
{
    /// <summary>
    /// ViewModel wrapping an <see cref="AppNotification"/> for display in the notification panel.
    /// </summary>
    public partial class NotificationViewModel : ObservableObject
    {
        /// <summary>
        /// Gets the notification ID.
        /// </summary>
        public string Id { get; }

        /// <summary>
        /// Gets the notification title.
        /// </summary>
        public string Title { get; }

        /// <summary>
        /// Gets the notification message body.
        /// </summary>
        public string Message { get; }

        /// <summary>
        /// Gets the notification severity level.
        /// </summary>
        public NotificationLevel Level { get; }

        /// <summary>
        /// Gets the timestamp of the notification.
        /// </summary>
        public DateTime Timestamp { get; }

        /// <summary>
        /// Gets the display-friendly time string.
        /// </summary>
        public string TimeAgo => _notification.TimeAgo;

        /// <summary>
        /// Gets the icon text based on notification level.
        /// </summary>
        public string LevelIcon => Level switch
        {
            NotificationLevel.Info => "\u2139",      // i symbol
            NotificationLevel.Success => "\u2713",   // checkmark
            NotificationLevel.Warning => "\u26A0",   // warning triangle
            NotificationLevel.Error => "\u2717",     // X mark
            _ => "\u2139"
        };

        private readonly AppNotification _notification;

        /// <summary>
        /// Initializes a new instance of the <see cref="NotificationViewModel"/> class.
        /// </summary>
        /// <param name="notification">The notification model to wrap.</param>
        public NotificationViewModel(AppNotification notification)
        {
            _notification = notification ?? throw new ArgumentNullException(nameof(notification));

            Id = notification.Id;
            Title = notification.Title;
            Message = notification.Message;
            Level = notification.Level;
            Timestamp = notification.Timestamp;
        }

        /// <inheritdoc/>
        public override string ToString() => $"[{Level}] {Title}: {Message}";
    }
}
