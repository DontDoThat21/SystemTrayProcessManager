using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using SystemTrayProcessManager.Core.Models;
using SystemTrayProcessManager.Core.Services;

namespace SystemTrayProcessManager.UI.ViewModels
{
    /// <summary>
    /// ViewModel for the Settings window. Provides two-way binding for all application settings
    /// and commands for save, reset, import, and export operations.
    /// </summary>
    public partial class SettingsViewModel : ObservableObject
    {
        private readonly IConfigurationService _configurationService;
        private readonly ILogger<SettingsViewModel> _logger;
        private AppSettings? _originalSettings;

        #region Observable Properties

        /// <summary>
        /// Gets or sets a value indicating whether global hotkeys are enabled.
        /// </summary>
        [ObservableProperty]
        private bool _hotkeysEnabled = true;

        /// <summary>
        /// Gets or sets the default audio volume (0.0-1.0).
        /// </summary>
        [ObservableProperty]
        private float _defaultVolume = 1.0f;

        /// <summary>
        /// Gets or sets a value indicating whether to mute on minimize.
        /// </summary>
        [ObservableProperty]
        private bool _muteOnMinimize;

        /// <summary>
        /// Gets or sets a value indicating whether the app starts with Windows.
        /// </summary>
        [ObservableProperty]
        private bool _startWithWindows;

        /// <summary>
        /// Gets or sets a value indicating whether the app starts minimized.
        /// </summary>
        [ObservableProperty]
        private bool _startMinimized = true;

        /// <summary>
        /// Gets or sets a value indicating whether notifications are enabled.
        /// </summary>
        [ObservableProperty]
        private bool _enableNotifications = true;

        /// <summary>
        /// Gets or sets a value indicating whether minimize goes to tray.
        /// </summary>
        [ObservableProperty]
        private bool _minimizeToTray = true;

        /// <summary>
        /// Gets or sets the process refresh interval in milliseconds.
        /// </summary>
        [ObservableProperty]
        private int _processRefreshIntervalMs = 5000;

        /// <summary>
        /// Gets or sets the selected theme name.
        /// </summary>
        [ObservableProperty]
        private string _theme = "Dark";

        /// <summary>
        /// Gets or sets a value indicating whether process icons are shown.
        /// </summary>
        [ObservableProperty]
        private bool _showProcessIcons = true;

        /// <summary>
        /// Gets or sets a value indicating whether animations are enabled.
        /// </summary>
        [ObservableProperty]
        private bool _animationsEnabled = true;

        /// <summary>
        /// Gets or sets the status message displayed to the user.
        /// </summary>
        [ObservableProperty]
        private string _statusMessage = string.Empty;

        /// <summary>
        /// Gets or sets a value indicating whether an operation is in progress.
        /// </summary>
        [ObservableProperty]
        private bool _isBusy;

        /// <summary>
        /// Gets or sets a value indicating whether settings have been modified.
        /// </summary>
        [ObservableProperty]
        private bool _hasChanges;

        #endregion

        /// <summary>
        /// Gets the available themes.
        /// </summary>
        public IReadOnlyList<string> AvailableThemes { get; } = new[] { "Dark", "Light" };

        /// <summary>
        /// Delegate to request a file path for importing settings. Set by the View.
        /// Returns the selected file path, or null if the user cancelled.
        /// </summary>
        public Func<string?>? RequestImportFilePath { get; set; }

        /// <summary>
        /// Delegate to request a file path for exporting settings. Set by the View.
        /// Returns the selected file path, or null if the user cancelled.
        /// </summary>
        public Func<string?>? RequestExportFilePath { get; set; }

        /// <summary>
        /// Raised when the ViewModel requests the View to close (e.g., after successful save).
        /// </summary>
        public event EventHandler<bool>? RequestClose;

        /// <summary>
        /// Initializes a new instance of the <see cref="SettingsViewModel"/> class.
        /// </summary>
        /// <param name="configurationService">The configuration service.</param>
        /// <param name="logger">The logger instance.</param>
        public SettingsViewModel(
            IConfigurationService configurationService,
            ILogger<SettingsViewModel> logger)
        {
            _configurationService = configurationService ?? throw new ArgumentNullException(nameof(configurationService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Loads current settings into the ViewModel properties.
        /// </summary>
        [RelayCommand]
        public async Task LoadSettingsAsync()
        {
            try
            {
                IsBusy = true;
                StatusMessage = "Loading settings...";
                _logger.LogDebug("Loading settings into ViewModel");

                var settings = await _configurationService.LoadSettingsAsync().ConfigureAwait(true);
                ApplySettingsToProperties(settings);
                _originalSettings = settings.Clone();
                HasChanges = false;

                StatusMessage = "Settings loaded.";
                _logger.LogInformation("Settings loaded into ViewModel");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load settings");
                StatusMessage = "Failed to load settings.";
            }
            finally
            {
                IsBusy = false;
            }
        }

        /// <summary>
        /// Saves the current ViewModel properties to the settings file.
        /// </summary>
        [RelayCommand]
        public async Task SaveSettingsAsync()
        {
            try
            {
                IsBusy = true;
                StatusMessage = "Saving settings...";
                _logger.LogDebug("Saving settings from ViewModel");

                var settings = BuildSettingsFromProperties();
                var success = await _configurationService.SaveSettingsAsync(settings).ConfigureAwait(true);

                if (success)
                {
                    _originalSettings = settings.Clone();
                    HasChanges = false;
                    StatusMessage = "Settings saved successfully.";
                    _logger.LogInformation("Settings saved from ViewModel");
                }
                else
                {
                    StatusMessage = "Failed to save settings.";
                    _logger.LogWarning("Failed to save settings");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to save settings");
                StatusMessage = "Error saving settings.";
            }
            finally
            {
                IsBusy = false;
            }
        }

        /// <summary>
        /// Resets all settings to their default values.
        /// </summary>
        [RelayCommand]
        public async Task ResetToDefaultsAsync()
        {
            try
            {
                IsBusy = true;
                StatusMessage = "Resetting to defaults...";
                _logger.LogDebug("Resetting settings to defaults");

                var defaults = await _configurationService.ResetToDefaultsAsync().ConfigureAwait(true);
                ApplySettingsToProperties(defaults);
                _originalSettings = defaults.Clone();
                HasChanges = false;

                StatusMessage = "Settings reset to defaults.";
                _logger.LogInformation("Settings reset to defaults via ViewModel");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to reset settings");
                StatusMessage = "Error resetting settings.";
            }
            finally
            {
                IsBusy = false;
            }
        }

        /// <summary>
        /// Restores settings from the backup file.
        /// </summary>
        [RelayCommand]
        public async Task RestoreFromBackupAsync()
        {
            try
            {
                IsBusy = true;
                StatusMessage = "Restoring from backup...";
                _logger.LogDebug("Restoring settings from backup");

                var restored = await _configurationService.RestoreFromBackupAsync().ConfigureAwait(true);
                if (restored != null)
                {
                    ApplySettingsToProperties(restored);
                    _originalSettings = restored.Clone();
                    HasChanges = false;
                    StatusMessage = "Settings restored from backup.";
                    _logger.LogInformation("Settings restored from backup via ViewModel");
                }
                else
                {
                    StatusMessage = "No backup available to restore.";
                    _logger.LogWarning("No backup available for restore");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to restore settings from backup");
                StatusMessage = "Error restoring from backup.";
            }
            finally
            {
                IsBusy = false;
            }
        }

        /// <summary>
        /// Cancels changes and reverts to the original settings.
        /// </summary>
        [RelayCommand]
        public void CancelChanges()
        {
            try
            {
                _logger.LogDebug("Cancelling settings changes");

                if (_originalSettings != null)
                {
                    ApplySettingsToProperties(_originalSettings);
                    HasChanges = false;
                    StatusMessage = "Changes cancelled.";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to cancel settings changes");
                StatusMessage = "Error cancelling changes.";
            }
        }

        /// <summary>
        /// Imports settings from an external JSON file selected by the user.
        /// </summary>
        [RelayCommand]
        public async Task ImportSettingsAsync()
        {
            try
            {
                _logger.LogDebug("Import settings requested");

                var filePath = RequestImportFilePath?.Invoke();
                if (string.IsNullOrWhiteSpace(filePath))
                {
                    _logger.LogDebug("Import cancelled by user");
                    return;
                }

                IsBusy = true;
                StatusMessage = "Importing settings...";

                var imported = await _configurationService.ImportAsync(filePath).ConfigureAwait(true);
                if (imported != null)
                {
                    ApplySettingsToProperties(imported);
                    HasChanges = true;
                    StatusMessage = $"Settings imported from {System.IO.Path.GetFileName(filePath)}. Save to apply.";
                    _logger.LogInformation("Settings imported from {Path}", filePath);
                }
                else
                {
                    StatusMessage = "Failed to import settings. File may be invalid.";
                    _logger.LogWarning("Failed to import settings from {Path}", filePath);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error importing settings");
                StatusMessage = "Error importing settings.";
            }
            finally
            {
                IsBusy = false;
            }
        }

        /// <summary>
        /// Exports the current settings to a JSON file chosen by the user.
        /// </summary>
        [RelayCommand]
        public async Task ExportSettingsAsync()
        {
            try
            {
                _logger.LogDebug("Export settings requested");

                var filePath = RequestExportFilePath?.Invoke();
                if (string.IsNullOrWhiteSpace(filePath))
                {
                    _logger.LogDebug("Export cancelled by user");
                    return;
                }

                IsBusy = true;
                StatusMessage = "Exporting settings...";

                var settings = BuildSettingsFromProperties();
                var success = await _configurationService.ExportAsync(filePath, settings).ConfigureAwait(true);

                if (success)
                {
                    StatusMessage = $"Settings exported to {System.IO.Path.GetFileName(filePath)}.";
                    _logger.LogInformation("Settings exported to {Path}", filePath);
                }
                else
                {
                    StatusMessage = "Failed to export settings.";
                    _logger.LogWarning("Failed to export settings to {Path}", filePath);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exporting settings");
                StatusMessage = "Error exporting settings.";
            }
            finally
            {
                IsBusy = false;
            }
        }

        /// <summary>
        /// Saves settings and requests the view to close.
        /// </summary>
        [RelayCommand]
        public async Task SaveAndCloseAsync()
        {
            await SaveSettingsAsync().ConfigureAwait(true);

            if (StatusMessage == "Settings saved successfully.")
            {
                _logger.LogDebug("Save succeeded, requesting view close");
                RequestClose?.Invoke(this, true);
            }
        }

        /// <summary>
        /// Applies an <see cref="AppSettings"/> object to the ViewModel's observable properties.
        /// </summary>
        /// <param name="settings">The settings to apply.</param>
        private void ApplySettingsToProperties(AppSettings settings)
        {
            HotkeysEnabled = settings.HotkeysEnabled;
            DefaultVolume = settings.DefaultVolume;
            MuteOnMinimize = settings.MuteOnMinimize;
            StartWithWindows = settings.StartWithWindows;
            StartMinimized = settings.StartMinimized;
            EnableNotifications = settings.EnableNotifications;
            MinimizeToTray = settings.MinimizeToTray;
            ProcessRefreshIntervalMs = settings.ProcessRefreshIntervalMs;
            Theme = settings.Theme;
            ShowProcessIcons = settings.ShowProcessIcons;
            AnimationsEnabled = settings.AnimationsEnabled;
        }

        /// <summary>
        /// Builds an <see cref="AppSettings"/> from the ViewModel's current property values.
        /// </summary>
        /// <returns>A new settings object reflecting the ViewModel state.</returns>
        internal AppSettings BuildSettingsFromProperties()
        {
            return new AppSettings
            {
                Version = AppSettings.CurrentVersion,
                IsFirstRun = false,
                HotkeysEnabled = HotkeysEnabled,
                DefaultVolume = DefaultVolume,
                MuteOnMinimize = MuteOnMinimize,
                StartWithWindows = StartWithWindows,
                StartMinimized = StartMinimized,
                EnableNotifications = EnableNotifications,
                MinimizeToTray = MinimizeToTray,
                ProcessRefreshIntervalMs = ProcessRefreshIntervalMs,
                Theme = Theme,
                ShowProcessIcons = ShowProcessIcons,
                AnimationsEnabled = AnimationsEnabled
            };
        }

        /// <summary>
        /// Called when any observable property changes to track modifications.
        /// </summary>
        protected override void OnPropertyChanged(System.ComponentModel.PropertyChangedEventArgs e)
        {
            base.OnPropertyChanged(e);

            // Track changes for any settings property (not status/busy properties)
            if (e.PropertyName != nameof(StatusMessage) &&
                e.PropertyName != nameof(IsBusy) &&
                e.PropertyName != nameof(HasChanges) &&
                _originalSettings != null)
            {
                var current = BuildSettingsFromProperties();
                HasChanges = !current.Equals(_originalSettings);
            }
        }
    }
}
