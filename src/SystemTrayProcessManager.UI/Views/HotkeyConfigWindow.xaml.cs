using System.ComponentModel;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using Microsoft.Extensions.Logging;
using SystemTrayProcessManager.Core.Models;
using SystemTrayProcessManager.Core.Services;
using SystemTrayProcessManager.UI.ViewModels;

namespace SystemTrayProcessManager.UI.Views
{
    /// <summary>
    /// Interaction logic for HotkeyConfigWindow.xaml.
    /// </summary>
    public partial class HotkeyConfigWindow : Window
    {
        private readonly ILogger<HotkeyConfigWindow> _logger;
        private readonly HotkeyConfigViewModel _viewModel;
        private readonly IHotkeyService _hotkeyService;
        private bool _resumeAfterCapture;

        /// <summary>
        /// Initializes a new instance of the <see cref="HotkeyConfigWindow"/> class.
        /// </summary>
        /// <param name="logger">The logger instance.</param>
        /// <param name="viewModel">The view model for this window.</param>
        /// <param name="hotkeyService">The hotkey service to suspend while recording a shortcut.</param>
        public HotkeyConfigWindow(
            ILogger<HotkeyConfigWindow> logger,
            HotkeyConfigViewModel viewModel,
            IHotkeyService hotkeyService)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _viewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
            _hotkeyService = hotkeyService ?? throw new ArgumentNullException(nameof(hotkeyService));

            InitializeComponent();

            DataContext = _viewModel;

            Loaded += OnLoaded;
            Closing += OnClosing;
            Closed += (_, _) => ResumeAfterCapture();

            _logger.LogDebug("HotkeyConfigWindow initialized");
        }

        private async void OnLoaded(object sender, RoutedEventArgs e)
        {
            _logger.LogDebug("HotkeyConfigWindow loaded, loading configuration");
            await _viewModel.LoadConfigurationAsync().ConfigureAwait(true);
        }

        private async void OnClosing(object? sender, System.ComponentModel.CancelEventArgs e)
        {
            if (_viewModel.IsBusy)
            {
                e.Cancel = true;
                return;
            }

            if (_viewModel.HasUnsavedChanges)
            {
                var result = MessageBox.Show(
                    "You have unsaved changes. Do you want to save before closing?",
                    "Unsaved Changes",
                    MessageBoxButton.YesNoCancel,
                    MessageBoxImage.Question);

                switch (result)
                {
                    case MessageBoxResult.Yes:
                        e.Cancel = true;
                        await _viewModel.SaveAllChangesCommand.ExecuteAsync(null);
                        if (!_viewModel.HasUnsavedChanges)
                        {
                            // Close after the original Closing event has finished.
                            _ = Dispatcher.BeginInvoke(new Action(Close));
                        }
                        return;
                    case MessageBoxResult.Cancel:
                        e.Cancel = true;
                        return;
                    // No - just close without saving
                }
            }

            _logger.LogDebug("HotkeyConfigWindow closing");
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void HotkeyCaptureControl_HotkeyCaptured(object? sender, HotkeyBinding e)
        {
            _viewModel.OnHotkeyCaptured(e);
        }

        private void HotkeyCaptureControl_GotKeyboardFocus(object sender, System.Windows.Input.KeyboardFocusChangedEventArgs e)
        {
            if (!_hotkeyService.IsSuspended)
            {
                _hotkeyService.Suspend();
                _resumeAfterCapture = true;
            }
        }

        private void HotkeyCaptureControl_LostKeyboardFocus(object sender, System.Windows.Input.KeyboardFocusChangedEventArgs e)
        {
            ResumeAfterCapture();
        }

        private void ResumeAfterCapture()
        {
            if (_resumeAfterCapture)
            {
                _hotkeyService.Resume();
                _resumeAfterCapture = false;
            }
        }
    }

    /// <summary>
    /// Converts boolean to "Add New Hotkey" or "Edit Hotkey" text.
    /// </summary>
    public class BoolToAddEditConverter : IValueConverter
    {
        /// <summary>
        /// Gets the singleton instance.
        /// </summary>
        public static BoolToAddEditConverter Instance { get; } = new();

        /// <inheritdoc/>
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value is true ? "Add New Hotkey" : "Edit Hotkey";
        }

        /// <inheritdoc/>
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Converts null/empty string to Collapsed visibility.
    /// </summary>
    public class NullToVisibilityConverter : IValueConverter
    {
        /// <inheritdoc/>
        public object Convert(object? value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string str)
            {
                return string.IsNullOrWhiteSpace(str) ? Visibility.Collapsed : Visibility.Visible;
            }

            return value == null ? Visibility.Collapsed : Visibility.Visible;
        }

        /// <inheritdoc/>
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
        }
    }
