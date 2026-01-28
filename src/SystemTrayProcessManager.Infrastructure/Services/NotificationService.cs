using Microsoft.Extensions.Logging;
using SystemTrayProcessManager.Core.Models;
using SystemTrayProcessManager.Core.Services;

namespace SystemTrayProcessManager.Infrastructure.Services
{
    /// <summary>
    /// Manages in-app notifications for process events and system status messages.
    /// Maintains a bounded list of active notifications with auto-dismiss support.
    /// </summary>
    public class NotificationService : INotificationService
    {
        private readonly ILogger<NotificationService> _logger;
        private readonly List<AppNotification> _notifications = new();
        private readonly object _lock = new();
        private const int MaxNotifications = 50;

        /// <inheritdoc/>
        public event EventHandler<AppNotification>? NotificationAdded;

        /// <inheritdoc/>
        public event EventHandler<string>? NotificationDismissed;

        /// <summary>
        /// Initializes a new instance of the <see cref="NotificationService"/> class.
        /// </summary>
        /// <param name="logger">Logger instance for this service.</param>
        public NotificationService(ILogger<NotificationService> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _logger.LogDebug("NotificationService initialized");
        }

        /// <inheritdoc/>
        public IReadOnlyList<AppNotification> ActiveNotifications
        {
            get
            {
                lock (_lock)
                {
                    return _notifications
                        .Where(n => !n.IsDismissed)
                        .OrderByDescending(n => n.Timestamp)
                        .ToList()
                        .AsReadOnly();
                }
            }
        }

        /// <inheritdoc/>
        public int ActiveCount
        {
            get
            {
                lock (_lock)
                {
                    return _notifications.Count(n => !n.IsDismissed);
                }
            }
        }

        /// <inheritdoc/>
        public void AddNotification(AppNotification notification)
        {
            try
            {
                if (notification == null)
                {
                    _logger.LogWarning("Attempted to add null notification");
                    return;
                }

                lock (_lock)
                {
                    _notifications.Add(notification);

                    // Trim old notifications if exceeding limit
                    while (_notifications.Count > MaxNotifications)
                    {
                        _notifications.RemoveAt(0);
                    }
                }

                _logger.LogDebug("Notification added: [{Level}] {Title}", notification.Level, notification.Title);
                NotificationAdded?.Invoke(this, notification);

                // Schedule auto-dismiss if applicable
                if (notification.AutoDismissMs > 0)
                {
                    _ = AutoDismissAsync(notification.Id, notification.AutoDismissMs);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to add notification");
            }
        }

        /// <inheritdoc/>
        public void DismissNotification(string notificationId)
        {
            try
            {
                if (string.IsNullOrEmpty(notificationId))
                {
                    _logger.LogWarning("Attempted to dismiss notification with null/empty ID");
                    return;
                }

                lock (_lock)
                {
                    var notification = _notifications.FirstOrDefault(n => n.Id == notificationId);
                    if (notification != null)
                    {
                        notification.IsDismissed = true;
                        _logger.LogDebug("Notification dismissed: {Id}", notificationId);
                        NotificationDismissed?.Invoke(this, notificationId);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to dismiss notification {Id}", notificationId);
            }
        }

        /// <inheritdoc/>
        public void DismissAll()
        {
            try
            {
                lock (_lock)
                {
                    foreach (var notification in _notifications)
                    {
                        notification.IsDismissed = true;
                    }
                }

                _logger.LogDebug("All notifications dismissed");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to dismiss all notifications");
            }
        }

        /// <summary>
        /// Automatically dismisses a notification after the specified delay.
        /// </summary>
        private async Task AutoDismissAsync(string notificationId, int delayMs)
        {
            try
            {
                await Task.Delay(delayMs);
                DismissNotification(notificationId);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Auto-dismiss failed for notification {Id}", notificationId);
            }
        }
    }
}
