using SystemTrayProcessManager.UI.ViewModels;
using WinOpenFileDialog = Microsoft.Win32.OpenFileDialog;
using WinSaveFileDialog = Microsoft.Win32.SaveFileDialog;

namespace SystemTrayProcessManager.UI.Views
{
    /// <summary>
    /// Interaction logic for SettingsWindow.xaml.
    /// Handles file dialogs for import/export and close-on-save behavior.
    /// </summary>
    public partial class SettingsWindow : Window
    {
        private readonly SettingsViewModel _viewModel;

        /// <summary>
        /// Initializes a new instance of the <see cref="SettingsWindow"/> class.
        /// </summary>
        /// <param name="viewModel">The settings view model injected via DI.</param>
        public SettingsWindow(SettingsViewModel viewModel)
        {
            _viewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));

            InitializeComponent();
            DataContext = _viewModel;

            // Wire up file dialog delegates
            _viewModel.RequestImportFilePath = OnRequestImportFilePath;
            _viewModel.RequestExportFilePath = OnRequestExportFilePath;

            // Wire up close request
            _viewModel.RequestClose += OnRequestClose;

            Loaded += OnLoaded;
            Closed += OnClosed;
        }

        /// <summary>
        /// Loads settings when the window is shown.
        /// </summary>
        private async void OnLoaded(object sender, RoutedEventArgs e)
        {
            await _viewModel.LoadSettingsAsync();
        }

        /// <summary>
        /// Cleans up event handlers when the window is closed.
        /// </summary>
        private void OnClosed(object? sender, EventArgs e)
        {
            _viewModel.RequestImportFilePath = null;
            _viewModel.RequestExportFilePath = null;
            _viewModel.RequestClose -= OnRequestClose;
        }

        /// <summary>
        /// Shows an open-file dialog for importing settings and returns the selected path.
        /// </summary>
        /// <returns>The selected file path, or null if the user cancelled.</returns>
        private string? OnRequestImportFilePath()
        {
            var dialog = new WinOpenFileDialog
            {
                Title = "Import Settings",
                Filter = "JSON Files (*.json)|*.json|All Files (*.*)|*.*",
                DefaultExt = ".json",
                CheckFileExists = true
            };

            return dialog.ShowDialog(this) == true ? dialog.FileName : null;
        }

        /// <summary>
        /// Shows a save-file dialog for exporting settings and returns the selected path.
        /// </summary>
        /// <returns>The selected file path, or null if the user cancelled.</returns>
        private string? OnRequestExportFilePath()
        {
            var dialog = new WinSaveFileDialog
            {
                Title = "Export Settings",
                Filter = "JSON Files (*.json)|*.json|All Files (*.*)|*.*",
                DefaultExt = ".json",
                FileName = "SystemTrayProcessManager_Settings.json"
            };

            return dialog.ShowDialog(this) == true ? dialog.FileName : null;
        }

        /// <summary>
        /// Handles the close request from the ViewModel (after successful save).
        /// </summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="dialogResult">The dialog result to set.</param>
        private void OnRequestClose(object? sender, bool dialogResult)
        {
            DialogResult = dialogResult;
            Close();
        }
    }
}
