using Microsoft.Extensions.Logging;
using System.Drawing;
using System.Windows.Forms;
using SystemTrayProcessManager.Core.Enums;
using SystemTrayProcessManager.Core.Services;

namespace SystemTrayProcessManager.UI.Services
{
    /// <summary>
    /// Implements system tray functionality using Windows Forms NotifyIcon.
    /// Provides tray icon display, context menu, balloon notifications, and user interaction handling.
    /// </summary>
    public sealed class TrayIconService : ITrayIconService
    {
        private readonly ILogger<TrayIconService> _logger;
        private NotifyIcon? _notifyIcon;
        private ContextMenuStrip? _contextMenu;
        private bool _disposed;
        private bool _initialized;

        /// <inheritdoc/>
        public event EventHandler? TrayIconClicked;

        /// <inheritdoc/>
        public event EventHandler? ExitRequested;

        /// <inheritdoc/>
        public bool IsVisible => _notifyIcon?.Visible ?? false;

        /// <summary>
        /// Initializes a new instance of the <see cref="TrayIconService"/> class.
        /// </summary>
        /// <param name="logger">Logger instance for diagnostic output.</param>
        /// <exception cref="ArgumentNullException">Thrown when logger is null.</exception>
        public TrayIconService(ILogger<TrayIconService> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <inheritdoc/>
        public void Initialize()
        {
            if (_initialized)
            {
                _logger.LogWarning("TrayIconService already initialized. Ignoring duplicate initialization call.");
                return;
            }

            try
            {
                _logger.LogDebug("Initializing TrayIconService...");

                // Create context menu
                _contextMenu = CreateContextMenu();
                _logger.LogDebug("Context menu created with {ItemCount} items", _contextMenu.Items.Count);

                // Create and configure the NotifyIcon
                _notifyIcon = new NotifyIcon
                {
                    Icon = CreateDefaultIcon(),
                    Text = "SystemTray Process Manager",
                    Visible = true,
                    ContextMenuStrip = _contextMenu
                };

                // Subscribe to events
                _notifyIcon.MouseClick += OnNotifyIconMouseClick;
                _notifyIcon.MouseDoubleClick += OnNotifyIconMouseDoubleClick;
                _notifyIcon.BalloonTipClicked += OnBalloonTipClicked;

                _initialized = true;
                _logger.LogInformation("TrayIconService initialized successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to initialize TrayIconService");
                throw;
            }
        }

        /// <inheritdoc/>
        public void ShowBalloonTip(string title, string message, BalloonIcon icon = BalloonIcon.Info, int timeoutMs = 3000)
        {
            if (!_initialized || _notifyIcon == null)
            {
                _logger.LogWarning("Cannot show balloon tip - TrayIconService not initialized");
                return;
            }

            if (string.IsNullOrWhiteSpace(title))
            {
                _logger.LogWarning("Balloon tip title is empty");
                title = "SystemTray Process Manager";
            }

            try
            {
                _logger.LogDebug("Showing balloon tip: {Title} - {Message}", title, message);

                var toolTipIcon = icon switch
                {
                    BalloonIcon.Info => ToolTipIcon.Info,
                    BalloonIcon.Warning => ToolTipIcon.Warning,
                    BalloonIcon.Error => ToolTipIcon.Error,
                    _ => ToolTipIcon.None
                };

                _notifyIcon.ShowBalloonTip(timeoutMs, title, message ?? string.Empty, toolTipIcon);
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Failed to show balloon tip (non-critical)");
            }
        }

        /// <inheritdoc/>
        public void SetTooltip(string tooltip)
        {
            if (!_initialized || _notifyIcon == null)
            {
                _logger.LogWarning("Cannot set tooltip - TrayIconService not initialized");
                return;
            }

            try
            {
                // NotifyIcon.Text is limited to 127 characters
                if (!string.IsNullOrEmpty(tooltip) && tooltip.Length > 127)
                {
                    _logger.LogDebug("Tooltip truncated from {OriginalLength} to 127 characters", tooltip.Length);
                    tooltip = tooltip[..127];
                }

                _notifyIcon.Text = tooltip ?? "SystemTray Process Manager";
                _logger.LogDebug("Tooltip set to: {Tooltip}", _notifyIcon.Text);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to set tooltip");
            }
        }

        /// <inheritdoc/>
        public void SetVisible(bool visible)
        {
            if (!_initialized || _notifyIcon == null)
            {
                _logger.LogWarning("Cannot set visibility - TrayIconService not initialized");
                return;
            }

            try
            {
                _notifyIcon.Visible = visible;
                _logger.LogDebug("Tray icon visibility set to: {Visible}", visible);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to set tray icon visibility");
            }
        }

        /// <summary>
        /// Creates the context menu for the tray icon.
        /// </summary>
        /// <returns>Configured context menu strip.</returns>
        private ContextMenuStrip CreateContextMenu()
        {
            var menu = new ContextMenuStrip();

            // Header (disabled, just for display)
            var headerItem = new ToolStripMenuItem("SystemTray Process Manager")
            {
                Enabled = false,
                Font = new Font(menu.Font, FontStyle.Bold)
            };
            menu.Items.Add(headerItem);

            // Separator
            menu.Items.Add(new ToolStripSeparator());

            // Show Window
            var showWindowItem = new ToolStripMenuItem("Show Window");
            showWindowItem.Click += OnShowWindowClicked;
            showWindowItem.Font = new Font(menu.Font, FontStyle.Bold); // Default action
            menu.Items.Add(showWindowItem);

            // Separator for future process list
            menu.Items.Add(new ToolStripSeparator());

            // Placeholder for process list (future task)
            var processPlaceholder = new ToolStripMenuItem("Processes...")
            {
                Enabled = false
            };
            menu.Items.Add(processPlaceholder);

            // Separator
            menu.Items.Add(new ToolStripSeparator());

            // Exit
            var exitItem = new ToolStripMenuItem("Exit");
            exitItem.Click += OnExitClicked;
            menu.Items.Add(exitItem);

            return menu;
        }

        /// <summary>
        /// Creates a default application icon programmatically.
        /// Uses a simple colored square as a placeholder.
        /// </summary>
        /// <returns>Generated icon.</returns>
        private Icon CreateDefaultIcon()
        {
            try
            {
                // Try to load from embedded resource first
                var resourceIcon = LoadIconFromResource();
                if (resourceIcon != null)
                {
                    _logger.LogDebug("Loaded tray icon from embedded resource");
                    return resourceIcon;
                }
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Could not load icon from resource, using generated icon");
            }

            // Generate a simple icon programmatically
            _logger.LogDebug("Generating default tray icon");
            return GenerateDefaultIcon();
        }

        /// <summary>
        /// Attempts to load the tray icon from embedded resources.
        /// </summary>
        /// <returns>The loaded icon, or null if not found.</returns>
        private Icon? LoadIconFromResource()
        {
            try
            {
                var assembly = System.Reflection.Assembly.GetExecutingAssembly();
                var resourceNames = assembly.GetManifestResourceNames();

                // Look for the icon resource
                var iconResourceName = resourceNames.FirstOrDefault(n => 
                    n.EndsWith("tray-icon.ico", StringComparison.OrdinalIgnoreCase));

                if (iconResourceName != null)
                {
                    using var stream = assembly.GetManifestResourceStream(iconResourceName);
                    if (stream != null)
                    {
                        return new Icon(stream);
                    }
                }

                // Try WPF resource approach
                var uri = new Uri("pack://application:,,,/Resources/Icons/tray-icon.ico", UriKind.Absolute);
                var resourceStream = System.Windows.Application.GetResourceStream(uri);
                if (resourceStream?.Stream != null)
                {
                    return new Icon(resourceStream.Stream);
                }
            }
            catch
            {
                // Silently fall back to generated icon
            }

            return null;
        }

        /// <summary>
        /// Generates a simple default icon programmatically.
        /// Creates a blue square with "PM" text as a visual identifier.
        /// </summary>
        /// <returns>Generated icon.</returns>
        private static Icon GenerateDefaultIcon()
        {
            const int size = 32;
            using var bitmap = new Bitmap(size, size);
            using var graphics = Graphics.FromImage(bitmap);

            // Fill with a nice blue color
            using var brush = new SolidBrush(Color.FromArgb(0, 120, 215));
            graphics.FillRectangle(brush, 0, 0, size, size);

            // Add "PM" text
            using var font = new Font("Segoe UI", 10, FontStyle.Bold);
            using var textBrush = new SolidBrush(Color.White);
            var textSize = graphics.MeasureString("PM", font);
            var x = (size - textSize.Width) / 2;
            var y = (size - textSize.Height) / 2;
            graphics.DrawString("PM", font, textBrush, x, y);

            // Convert bitmap to icon
            var hIcon = bitmap.GetHicon();
            return Icon.FromHandle(hIcon);
        }

        /// <summary>
        /// Handles mouse click events on the tray icon.
        /// </summary>
        private void OnNotifyIconMouseClick(object? sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                _logger.LogDebug("Tray icon left-clicked");
                TrayIconClicked?.Invoke(this, EventArgs.Empty);
            }
        }

        /// <summary>
        /// Handles double-click events on the tray icon.
        /// </summary>
        private void OnNotifyIconMouseDoubleClick(object? sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                _logger.LogDebug("Tray icon double-clicked");
                TrayIconClicked?.Invoke(this, EventArgs.Empty);
            }
        }

        /// <summary>
        /// Handles balloon tip click events.
        /// </summary>
        private void OnBalloonTipClicked(object? sender, EventArgs e)
        {
            _logger.LogDebug("Balloon tip clicked");
            TrayIconClicked?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// Handles the Show Window menu item click.
        /// </summary>
        private void OnShowWindowClicked(object? sender, EventArgs e)
        {
            _logger.LogDebug("Show Window menu item clicked");
            TrayIconClicked?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// Handles the Exit menu item click.
        /// </summary>
        private void OnExitClicked(object? sender, EventArgs e)
        {
            _logger.LogDebug("Exit menu item clicked");
            ExitRequested?.Invoke(this, EventArgs.Empty);
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _logger.LogDebug("Disposing TrayIconService...");

            try
            {
                if (_notifyIcon != null)
                {
                    _notifyIcon.MouseClick -= OnNotifyIconMouseClick;
                    _notifyIcon.MouseDoubleClick -= OnNotifyIconMouseDoubleClick;
                    _notifyIcon.BalloonTipClicked -= OnBalloonTipClicked;

                    _notifyIcon.Visible = false;
                    _notifyIcon.Icon?.Dispose();
                    _notifyIcon.Dispose();
                    _notifyIcon = null;
                }

                if (_contextMenu != null)
                {
                    foreach (var item in _contextMenu.Items.OfType<ToolStripMenuItem>())
                    {
                        item.Click -= OnShowWindowClicked;
                        item.Click -= OnExitClicked;
                    }

                    _contextMenu.Dispose();
                    _contextMenu = null;
                }

                _disposed = true;
                _logger.LogInformation("TrayIconService disposed successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during TrayIconService disposal");
            }
        }
    }
}
