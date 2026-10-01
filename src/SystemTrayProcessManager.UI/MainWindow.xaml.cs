using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using System.ComponentModel;
using SystemTrayProcessManager.UI.ViewModels;

namespace SystemTrayProcessManager.UI
{
    /// <summary>
    /// Main application window with dark-themed process dashboard and minimize-to-tray support.
    /// Displays running processes in a card grid with search, filter, and quick action capabilities.
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly ILogger<MainWindow> _logger;
        private readonly MainViewModel _viewModel;

        /// <summary>
        /// Initializes a new instance of the <see cref="MainWindow"/> class.
        /// </summary>
        /// <param name="viewModel">The main dashboard ViewModel.</param>
        /// <param name="logger">Logger instance for this window.</param>
        public MainWindow(MainViewModel viewModel, ILogger<MainWindow> logger)
        {
            _viewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            InitializeComponent();

            DataContext = _viewModel;

            // Subscribe to window state changes for minimize-to-tray
            StateChanged += OnWindowStateChanged;

            // Load processes on window shown
            Loaded += OnWindowLoaded;

            _logger.LogInformation("MainWindow initialized with MainViewModel");
        }

        /// <summary>
        /// Loads processes when the window is first shown.
        /// </summary>
        private async void OnWindowLoaded(object sender, System.Windows.RoutedEventArgs e)
        {
            try
            {
                _logger.LogDebug("MainWindow loaded - initiating process refresh");
                await _viewModel.RefreshProcessesCommand.ExecuteAsync(null);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading processes on window startup");
            }
        }

        private void ConfigureHotkeys_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            try
            {
                var window = ((App)Application.Current).Services.GetRequiredService<Views.HotkeyConfigWindow>();
                window.Owner = this;
                window.ShowDialog();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to open hotkey configuration");
                MessageBox.Show("Unable to open hotkey configuration. Please check the application log.", "Hotkey Configuration");
            }
        }

        private async void ProcessHotkeys_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            if (sender is not System.Windows.FrameworkElement { DataContext: ProcessCardViewModel process }) return;
            try
            {
                var window = ((App)Application.Current).Services.GetRequiredService<Views.ProcessHotkeysWindow>();
                window.Owner = this;
                await window.LoadAsync(process.Name);
                window.ShowDialog();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to configure shortcuts for {ProcessName}", process.Name);
                MessageBox.Show("Unable to open application shortcuts. Please check the application log.", "Application Hotkeys");
            }
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
                // App is shutting down - allow close and dispose ViewModel
                _logger.LogDebug("Application shutting down - allowing window close");
                _viewModel.Dispose();
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
