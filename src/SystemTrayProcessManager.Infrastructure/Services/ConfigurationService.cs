using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using SystemTrayProcessManager.Core.Models;
using SystemTrayProcessManager.Core.Services;

namespace SystemTrayProcessManager.Infrastructure.Services
{
    /// <summary>
    /// Provides centralized application settings persistence using JSON file storage.
    /// Handles loading, saving, backup, restore, import, export, and reset operations.
    /// </summary>
    public sealed class ConfigurationService : IConfigurationService
    {
        private readonly ILogger<ConfigurationService> _logger;
        private readonly string _configDirectory;
        private readonly string _settingsFilePath;
        private readonly string _backupFilePath;
        private AppSettings _currentSettings;

        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
        };

        /// <inheritdoc/>
        public string SettingsFilePath => _settingsFilePath;

        /// <inheritdoc/>
        public AppSettings CurrentSettings => _currentSettings;

        /// <inheritdoc/>
        public event EventHandler<AppSettings>? SettingsChanged;

        /// <summary>
        /// Initializes a new instance of the <see cref="ConfigurationService"/> class.
        /// </summary>
        /// <param name="logger">The logger instance.</param>
        /// <exception cref="ArgumentNullException">Thrown when logger is null.</exception>
        public ConfigurationService(ILogger<ConfigurationService> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            _configDirectory = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "SystemTrayProcessManager");

            _settingsFilePath = Path.Combine(_configDirectory, "settings.json");
            _backupFilePath = Path.Combine(_configDirectory, "settings.backup.json");
            _currentSettings = AppSettings.Default;

            _logger.LogDebug("ConfigurationService initialized. Settings path: {Path}", _settingsFilePath);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ConfigurationService"/> class
        /// with a custom configuration directory. Used for testing.
        /// </summary>
        /// <param name="logger">The logger instance.</param>
        /// <param name="configDirectory">The configuration directory path.</param>
        internal ConfigurationService(ILogger<ConfigurationService> logger, string configDirectory)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            if (string.IsNullOrWhiteSpace(configDirectory))
            {
                throw new ArgumentException("Config directory cannot be null or empty.", nameof(configDirectory));
            }

            _configDirectory = configDirectory;
            _settingsFilePath = Path.Combine(_configDirectory, "settings.json");
            _backupFilePath = Path.Combine(_configDirectory, "settings.backup.json");
            _currentSettings = AppSettings.Default;

            _logger.LogDebug("ConfigurationService initialized with custom path: {Path}", _settingsFilePath);
        }

        /// <inheritdoc/>
        public async Task<AppSettings> LoadSettingsAsync()
        {
            try
            {
                _logger.LogDebug("Loading settings from {Path}", _settingsFilePath);

                if (!File.Exists(_settingsFilePath))
                {
                    _logger.LogInformation("No existing settings file found, returning defaults");
                    _currentSettings = GetDefaults();
                    return _currentSettings;
                }

                var json = await File.ReadAllTextAsync(_settingsFilePath).ConfigureAwait(false);
                var settings = JsonSerializer.Deserialize<AppSettings>(json, JsonOptions);

                if (settings == null)
                {
                    _logger.LogWarning("Failed to deserialize settings, attempting backup restore");
                    return await TryRestoreOrDefaultAsync().ConfigureAwait(false);
                }

                // Sanitize and validate
                settings = settings.Sanitize();
                var errors = settings.Validate().ToList();
                if (errors.Count > 0)
                {
                    foreach (var error in errors)
                    {
                        _logger.LogWarning("Settings validation warning: {Error}", error);
                    }
                }

                _currentSettings = settings;
                _logger.LogInformation("Settings loaded successfully: {Settings}", settings);
                OnSettingsChanged(settings);

                return settings;
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "Failed to parse settings file, attempting recovery");
                return await TryRestoreOrDefaultAsync().ConfigureAwait(false);
            }
            catch (IOException ex)
            {
                _logger.LogError(ex, "Failed to read settings file");
                return await TryRestoreOrDefaultAsync().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error loading settings");
                _currentSettings = GetDefaults();
                return _currentSettings;
            }
        }

        /// <inheritdoc/>
        public async Task<bool> SaveSettingsAsync(AppSettings settings)
        {
            ArgumentNullException.ThrowIfNull(settings);

            try
            {
                _logger.LogDebug("Saving settings to {Path}", _settingsFilePath);

                // Ensure directory exists
                Directory.CreateDirectory(_configDirectory);

                // Create backup of existing file before writing
                if (File.Exists(_settingsFilePath))
                {
                    await BackupAsync().ConfigureAwait(false);
                }

                // Update metadata
                settings.LastModified = DateTime.UtcNow;

                var json = JsonSerializer.Serialize(settings, JsonOptions);
                await File.WriteAllTextAsync(_settingsFilePath, json).ConfigureAwait(false);

                _currentSettings = settings;
                _logger.LogInformation("Settings saved successfully: {Settings}", settings);
                OnSettingsChanged(settings);

                return true;
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogError(ex, "Access denied saving settings to {Path}", _settingsFilePath);
                return false;
            }
            catch (IOException ex)
            {
                _logger.LogError(ex, "Failed to write settings file");
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error saving settings");
                return false;
            }
        }

        /// <inheritdoc/>
        public async Task<bool> BackupAsync()
        {
            try
            {
                if (!File.Exists(_settingsFilePath))
                {
                    _logger.LogDebug("No settings file to backup");
                    return true;
                }

                _logger.LogDebug("Creating settings backup");

                var content = await File.ReadAllTextAsync(_settingsFilePath).ConfigureAwait(false);
                await File.WriteAllTextAsync(_backupFilePath, content).ConfigureAwait(false);

                _logger.LogInformation("Settings backup created at {Path}", _backupFilePath);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create settings backup");
                return false;
            }
        }

        /// <inheritdoc/>
        public async Task<AppSettings?> RestoreFromBackupAsync()
        {
            try
            {
                if (!File.Exists(_backupFilePath))
                {
                    _logger.LogWarning("No backup file found to restore");
                    return null;
                }

                _logger.LogDebug("Restoring settings from backup");

                var json = await File.ReadAllTextAsync(_backupFilePath).ConfigureAwait(false);
                var settings = JsonSerializer.Deserialize<AppSettings>(json, JsonOptions);

                if (settings != null)
                {
                    settings = settings.Sanitize();
                    _currentSettings = settings;
                    _logger.LogInformation("Settings restored from backup: {Settings}", settings);
                    OnSettingsChanged(settings);
                }

                return settings;
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "Failed to parse backup settings file");
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to restore settings from backup");
                return null;
            }
        }

        /// <inheritdoc/>
        public async Task<AppSettings> ResetToDefaultsAsync()
        {
            try
            {
                _logger.LogInformation("Resetting settings to defaults");

                var defaults = GetDefaults();
                defaults.IsFirstRun = false; // Not first run if user explicitly resets

                await SaveSettingsAsync(defaults).ConfigureAwait(false);

                return defaults;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to reset settings to defaults");
                var defaults = GetDefaults();
                _currentSettings = defaults;
                return defaults;
            }
        }

        /// <inheritdoc/>
        public AppSettings GetDefaults()
        {
            _logger.LogDebug("Creating default settings");
            return AppSettings.Default;
        }

        /// <inheritdoc/>
        public bool SettingsFileExists()
        {
            return File.Exists(_settingsFilePath);
        }

        /// <inheritdoc/>
        public bool BackupFileExists()
        {
            return File.Exists(_backupFilePath);
        }

        /// <inheritdoc/>
        public async Task<bool> ExportAsync(string filePath, AppSettings settings)
        {
            if (string.IsNullOrWhiteSpace(filePath))
            {
                _logger.LogWarning("Export called with empty file path");
                return false;
            }

            ArgumentNullException.ThrowIfNull(settings);

            try
            {
                _logger.LogDebug("Exporting settings to {Path}", filePath);

                var directory = Path.GetDirectoryName(filePath);
                if (!string.IsNullOrEmpty(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                var json = JsonSerializer.Serialize(settings, JsonOptions);
                await File.WriteAllTextAsync(filePath, json).ConfigureAwait(false);

                _logger.LogInformation("Settings exported to {Path}", filePath);
                return true;
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogError(ex, "Access denied exporting settings to {Path}", filePath);
                return false;
            }
            catch (IOException ex)
            {
                _logger.LogError(ex, "Failed to write export file");
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error exporting settings");
                return false;
            }
        }

        /// <inheritdoc/>
        public async Task<AppSettings?> ImportAsync(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath))
            {
                _logger.LogWarning("Import called with empty file path");
                return null;
            }

            try
            {
                _logger.LogDebug("Importing settings from {Path}", filePath);

                if (!File.Exists(filePath))
                {
                    _logger.LogWarning("Import file not found: {Path}", filePath);
                    return null;
                }

                var json = await File.ReadAllTextAsync(filePath).ConfigureAwait(false);
                var settings = JsonSerializer.Deserialize<AppSettings>(json, JsonOptions);

                if (settings == null)
                {
                    _logger.LogWarning("Failed to deserialize imported settings");
                    return null;
                }

                settings = settings.Sanitize();
                _logger.LogInformation("Settings imported from {Path}: {Settings}", filePath, settings);

                return settings;
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "Failed to parse imported settings file");
                return null;
            }
            catch (IOException ex)
            {
                _logger.LogError(ex, "Failed to read import file");
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error importing settings");
                return null;
            }
        }

        /// <summary>
        /// Attempts to restore from backup; if that fails, returns defaults.
        /// </summary>
        private async Task<AppSettings> TryRestoreOrDefaultAsync()
        {
            var restored = await RestoreFromBackupAsync().ConfigureAwait(false);
            if (restored != null)
            {
                _logger.LogInformation("Successfully recovered settings from backup");
                return restored;
            }

            _logger.LogWarning("Backup restoration failed, returning defaults");
            _currentSettings = GetDefaults();
            return _currentSettings;
        }

        /// <summary>
        /// Raises the <see cref="SettingsChanged"/> event.
        /// </summary>
        /// <param name="settings">The updated settings.</param>
        private void OnSettingsChanged(AppSettings settings)
        {
            SettingsChanged?.Invoke(this, settings);
        }
    }
}
