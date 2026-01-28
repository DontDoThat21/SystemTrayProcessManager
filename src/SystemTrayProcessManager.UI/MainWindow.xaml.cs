using Microsoft.Extensions.Logging;
using System.ComponentModel;

namespace SystemTrayProcessManager.UI
{
    /// <summary>
    /// Main application window with minimize-to-tray support.
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly ILogger<MainWindow> _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="MainWindow"/> class.
        /// </summary>
        /// <param name="logger">Logger instance for this window.</param>
        public MainWindow(ILogger<MainWindow> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            InitializeComponent();

            // Subscribe to window state changes for minimize-to-tray
            StateChanged += OnWindowStateChanged;

            _logger.LogInformation("MainWindow initialized");
        }

        /// <summary>
        /// Handles window state changes to implement minimize-to-tray behavior.
        /// </summary>
        private void OnWindowStateChanged(object? sender, EventArgs e)
        {
            if (WindowState == WindowState.Minimized)
            {
                _logger.LogDebug("Window minimized - hiding to system tray");
                Hide();
            }
        }

        /// <summary>
        /// Handles the window closing event. Hides to tray instead of closing unless app is shutting down.
        /// </summary>
        /// <param name="e">Cancel event arguments.</param>
        protected override void OnClosing(CancelEventArgs e)
        {
            // Check if the application is shutting down
            if (Application.Current.ShutdownMode == ShutdownMode.OnExplicitShutdown ||
                !Application.Current.Windows.OfType<Window>().Any())
            {
                // App is shutting down - allow close
                _logger.LogDebug("Application shutting down - allowing window close");
                base.OnClosing(e);
                return;
            }

            // Hide to tray instead of closing
            _logger.LogDebug("Window close requested - hiding to system tray");
            e.Cancel = true;
            Hide();
        }
    }
}
