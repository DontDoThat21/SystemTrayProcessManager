using Microsoft.Extensions.Logging;
using SystemTrayProcessManager.Core.Services;

namespace SystemTrayProcessManager.Infrastructure.Services;

/// <summary>
/// Tooltip service providing application-wide tooltip text management.
/// Uses a dictionary-based approach for easy maintenance and potential localization.
/// </summary>
public class TooltipService : ITooltipService
{
    private readonly ILogger<TooltipService> _logger;
    private readonly Dictionary<string, string> _tooltips;

    /// <summary>
    /// Initializes a new instance of the <see cref="TooltipService"/> class.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    public TooltipService(ILogger<TooltipService> logger)
    {
        ArgumentNullException.ThrowIfNull(logger);
        _logger = logger;
        _tooltips = InitializeTooltips();
        _logger.LogDebug("TooltipService initialized with {Count} tooltips", _tooltips.Count);
    }

    /// <summary>
    /// Initializes the tooltip dictionary with all application tooltips.
    /// </summary>
    private static Dictionary<string, string> InitializeTooltips()
    {
        return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            // Process Card actions
            ["ProcessCard.BringToFront"] = "Bring this window to the foreground and activate it",
            ["ProcessCard.Minimize"] = "Minimize this window to the taskbar",
            ["ProcessCard.Maximize"] = "Maximize this window to fill the screen",
            ["ProcessCard.Restore"] = "Restore this window to its previous size",
            ["ProcessCard.Close"] = "Close this application",
            ["ProcessCard.Mute"] = "Toggle mute for this application's audio",
            ["ProcessCard.Hide"] = "Hide this window (it will remain running)",
            ["ProcessCard.Show"] = "Show this hidden window",
            ["ProcessCard.AlwaysOnTop"] = "Toggle always-on-top for this window",
            ["ProcessCard.SetTransparency"] = "Adjust the transparency of this window",

            // Hotkey Configuration
            ["Hotkey.Capture"] = "Click here, then press your desired key combination",
            ["Hotkey.Conflict"] = "This hotkey is already in use by another binding",
            ["Hotkey.SystemConflict"] = "This hotkey may conflict with a Windows system shortcut",
            ["Hotkey.Import"] = "Import hotkey configuration from a JSON file",
            ["Hotkey.Export"] = "Export your hotkey configuration to a JSON file",
            ["Hotkey.Add"] = "Add a new hotkey binding",
            ["Hotkey.Edit"] = "Edit this hotkey binding",
            ["Hotkey.Delete"] = "Delete this hotkey binding",
            ["Hotkey.ResetDefaults"] = "Reset all hotkeys to their default configuration",
            ["Hotkey.ActionType"] = "The action to perform when this hotkey is pressed",
            ["Hotkey.ActionMode"] = "Quick Action affects the focused window; Pinned Process targets a specific application",
            ["Hotkey.ProcessName"] = "The name of the process to target (e.g., 'chrome' or 'spotify')",

            // Settings - General
            ["Settings.StartWithWindows"] = "Automatically launch this application when Windows starts",
            ["Settings.StartMinimized"] = "Start the application minimized to the system tray",
            ["Settings.MinimizeToTray"] = "Minimize to the system tray instead of the taskbar",
            ["Settings.EnableNotifications"] = "Show balloon notifications for application events",
            ["Settings.ProcessRefresh"] = "How often to refresh the process list (in milliseconds)",

            // Settings - Hotkeys
            ["Settings.HotkeysEnabled"] = "Enable or disable all global hotkeys",

            // Settings - Audio
            ["Settings.DefaultVolume"] = "Default volume level for new audio sessions (0-100%)",
            ["Settings.MuteOnMinimize"] = "Automatically mute applications when their windows are minimized",

            // Settings - Appearance
            ["Settings.Theme"] = "Select the application color theme",
            ["Settings.ShowProcessIcons"] = "Display application icons in the process list",
            ["Settings.AnimationsEnabled"] = "Enable smooth animations and transitions",

            // Settings - Actions
            ["Settings.Import"] = "Import settings from a previously exported file",
            ["Settings.Export"] = "Export your current settings to a file for backup",
            ["Settings.ResetDefaults"] = "Reset all settings to their default values",
            ["Settings.RestoreBackup"] = "Restore settings from the last automatic backup",
            ["Settings.Save"] = "Save your changes",
            ["Settings.Cancel"] = "Discard changes and close",

            // Main Window
            ["Main.Search"] = "Search by process name, window title, or PID",
            ["Main.Refresh"] = "Refresh the process list immediately",
            ["Main.Notifications"] = "View recent application notifications",
            ["Main.NotificationCount"] = "You have {0} unread notification(s)",
            ["Main.ProcessCount"] = "{0} processes running",

            // Tray Icon
            ["Tray.ShowWindow"] = "Show the main window",
            ["Tray.HotkeyConfig"] = "Configure global hotkeys",
            ["Tray.Settings"] = "Open application settings",
            ["Tray.Exit"] = "Exit the application completely",

            // Gaming Mode
            ["GamingMode.Enable"] = "Enable Gaming Mode to optimize performance and suppress notifications",
            ["GamingMode.Disable"] = "Disable Gaming Mode and restore normal operation",
            ["GamingMode.Status"] = "Gaming Mode is {0}",

            // Smart Features
            ["SmartFeatures.FocusHistory"] = "View and switch to recently focused windows",
            ["SmartFeatures.AutoMuteFullscreen"] = "Automatically mute background apps when a fullscreen application is detected",
            ["SmartFeatures.WindowPositionMemory"] = "Save and restore window positions per monitor configuration",

            // Error Messages
            ["Error.ProcessTerminated"] = "The process was terminated before the operation could complete",
            ["Error.InvalidHandle"] = "The window handle is no longer valid",
            ["Error.PermissionDenied"] = "Administrator privileges are required for this operation",
            ["Error.AudioNotAvailable"] = "No audio session exists for this process",

            // General
            ["General.Loading"] = "Loading...",
            ["General.Saving"] = "Saving...",
            ["General.Exit"] = "Exit the application",
            ["General.Cancel"] = "Cancel this operation",
            ["General.OK"] = "Confirm and close",
            ["General.Apply"] = "Apply changes without closing",
            ["General.Help"] = "Open the help documentation"
        };
    }

    /// <inheritdoc/>
    public string GetTooltip(string key)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            _logger.LogWarning("GetTooltip called with null or empty key");
            return string.Empty;
        }

        if (_tooltips.TryGetValue(key, out var tooltip))
        {
            return tooltip;
        }

        _logger.LogWarning("Tooltip not found for key: {Key}", key);
        return string.Empty;
    }

    /// <inheritdoc/>
    public string GetTooltip(string key, params object[] args)
    {
        var template = GetTooltip(key);
        if (string.IsNullOrEmpty(template))
        {
            return string.Empty;
        }

        if (args == null || args.Length == 0)
        {
            return template;
        }

        try
        {
            return string.Format(template, args);
        }
        catch (FormatException ex)
        {
            _logger.LogError(ex, "Failed to format tooltip for key {Key} with {ArgCount} arguments", key, args.Length);
            return template;
        }
    }

    /// <inheritdoc/>
    public bool HasTooltip(string key)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            return false;
        }

        return _tooltips.ContainsKey(key);
    }

    /// <inheritdoc/>
    public IReadOnlyCollection<string> GetAllKeys()
    {
        return _tooltips.Keys.ToList().AsReadOnly();
    }
}
