using SystemTrayProcessManager.Core.Enums;

namespace SystemTrayProcessManager.Core.Services
{
    /// <summary>
    /// Provides system tray icon functionality including notifications, context menus, and user interactions.
    /// </summary>
    public interface ITrayIconService : IDisposable
    {
        /// <summary>
        /// Initializes the tray icon with the application icon and default context menu.
        /// Must be called before any other operations.
        /// </summary>
        void Initialize();

        /// <summary>
        /// Displays a balloon notification near the system tray icon.
        /// </summary>
        /// <param name="title">The title of the balloon notification.</param>
        /// <param name="message">The message text to display.</param>
        /// <param name="icon">The icon to display in the notification. Defaults to Info.</param>
        /// <param name="timeoutMs">The timeout in milliseconds before the balloon auto-dismisses. Defaults to 3000.</param>
        void ShowBalloonTip(string title, string message, BalloonIcon icon = BalloonIcon.Info, int timeoutMs = 3000);

        /// <summary>
        /// Sets the tooltip text displayed when hovering over the tray icon.
        /// </summary>
        /// <param name="tooltip">The tooltip text. Limited to 127 characters.</param>
        void SetTooltip(string tooltip);

        /// <summary>
        /// Shows or hides the tray icon.
        /// </summary>
        /// <param name="visible">True to show the icon; false to hide it.</param>
        void SetVisible(bool visible);

        /// <summary>
        /// Gets a value indicating whether the tray icon is currently visible.
        /// </summary>
        bool IsVisible { get; }

        /// <summary>
        /// Occurs when the tray icon is left-clicked or double-clicked.
        /// </summary>
        event EventHandler? TrayIconClicked;

        /// <summary>
        /// Occurs when the user selects the Exit option from the context menu.
        /// </summary>
        event EventHandler? ExitRequested;

        /// <summary>
        /// Occurs when the user selects the Hotkey Configuration option from the context menu.
        /// </summary>
        event EventHandler? HotkeyConfigRequested;

        /// <summary>
        /// Occurs when the user selects the Settings option from the context menu.
        /// </summary>
        event EventHandler? SettingsRequested;
    }
}
