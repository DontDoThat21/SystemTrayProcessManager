using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using SystemTrayProcessManager.Core.Enums;
using SystemTrayProcessManager.Core.Models;
using SystemTrayProcessManager.Core.Services;

namespace SystemTrayProcessManager.Infrastructure.Services
{
    /// <summary>
    /// Provides hotkey configuration persistence using JSON file storage.
    /// Handles loading, saving, importing, exporting, and validating hotkey configurations.
    /// </summary>
    public sealed class HotkeyConfigurationService : IHotkeyConfigurationService
    {
        private readonly ILogger<HotkeyConfigurationService> _logger;
        private readonly string _configDirectory;
        private readonly string _configFilePath;
        private readonly string _backupFilePath;

        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
        };

        // Known system shortcuts to warn about
        private static readonly Dictionary<(int VkCode, HotkeyModifier Modifiers), string> SystemShortcuts = new()
        {
            // Windows shortcuts
            { (0x44, HotkeyModifier.Win), "Win+D: Show Desktop" },
            { (0x45, HotkeyModifier.Win), "Win+E: Open File Explorer" },
            { (0x49, HotkeyModifier.Win), "Win+I: Open Settings" },
            { (0x4C, HotkeyModifier.Win), "Win+L: Lock Computer" },
            { (0x52, HotkeyModifier.Win), "Win+R: Open Run Dialog" },
            { (0x53, HotkeyModifier.Win), "Win+S: Open Search" },
            { (0x56, HotkeyModifier.Win), "Win+V: Open Clipboard History" },
            { (0x58, HotkeyModifier.Win), "Win+X: Quick Link Menu" },
            { (0x09, HotkeyModifier.Alt), "Alt+Tab: Switch Windows" },
            { (0x73, HotkeyModifier.Alt), "Alt+F4: Close Application" },
            { (0x1B, HotkeyModifier.Ctrl), "Ctrl+Escape: Open Start Menu" },
            { (0x1B, HotkeyModifier.Alt | HotkeyModifier.Ctrl), "Ctrl+Alt+Delete: Security Options" },
        };

        /// <inheritdoc/>
        public string ConfigurationFilePath => _configFilePath;

        /// <summary>
        /// Initializes a new instance of the <see cref="HotkeyConfigurationService"/> class.
        /// </summary>
        /// <param name="logger">The logger instance.</param>
        /// <exception cref="ArgumentNullException">Thrown when logger is null.</exception>
        public HotkeyConfigurationService(ILogger<HotkeyConfigurationService> logger)
            : this(logger, Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "SystemTrayProcessManager"))
        {
        }

        internal HotkeyConfigurationService(ILogger<HotkeyConfigurationService> logger, string configDirectory)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _configDirectory = configDirectory ?? throw new ArgumentNullException(nameof(configDirectory));

            _configFilePath = Path.Combine(_configDirectory, "hotkeys.json");
            _backupFilePath = Path.Combine(_configDirectory, "hotkeys.backup.json");

            _logger.LogDebug("HotkeyConfigurationService initialized. Config path: {Path}", _configFilePath);
        }

        /// <inheritdoc/>
        public async Task<HotkeyConfiguration> LoadConfigurationAsync()
        {
            try
            {
                _logger.LogDebug("Loading hotkey configuration from {Path}", _configFilePath);

                if (!File.Exists(_configFilePath))
                {
                    _logger.LogInformation("No existing configuration found, returning defaults");
                    return GetDefaultConfiguration();
                }

                var json = await File.ReadAllTextAsync(_configFilePath).ConfigureAwait(false);
                var configuration = JsonSerializer.Deserialize<HotkeyConfiguration>(json, JsonOptions);

                if (configuration == null)
                {
                    _logger.LogWarning("Failed to deserialize configuration, returning defaults");
                    return GetDefaultConfiguration();
                }

                // Validate loaded configuration
                var errors = ValidateConfiguration(configuration).ToList();
                if (errors.Count > 0)
                {
                    foreach (var error in errors)
                    {
                        _logger.LogWarning("Configuration validation warning: {Error}", error);
                    }
                }

                _logger.LogInformation(
                    "Loaded hotkey configuration: {Count} items ({Enabled} enabled)",
                    configuration.Count,
                    configuration.EnabledCount);

                return configuration;
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "Failed to parse hotkey configuration file");
                return GetDefaultConfiguration();
            }
            catch (IOException ex)
            {
                _logger.LogError(ex, "Failed to read hotkey configuration file");
                return GetDefaultConfiguration();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error loading hotkey configuration");
                return GetDefaultConfiguration();
            }
        }

        private async Task RememberApplicationsAsync(HotkeyConfiguration configuration)
        {
            // Merge the durable application catalogue before editors replace the hotkey list.
            var previous = await LoadConfigurationAsync().ConfigureAwait(false);
            var applications = new Dictionary<string, string?>(previous.Applications, StringComparer.OrdinalIgnoreCase);
            foreach (var app in configuration.Applications) applications[app.Key] = app.Value;
            foreach (var item in previous.Items.Concat(configuration.Items))
            {
                if (string.IsNullOrWhiteSpace(item.TargetProcessName)) continue;
                var name = HotkeyConfiguration.ApplicationName(item.TargetProcessName);
                applications.TryAdd(name, null);
                if (string.IsNullOrWhiteSpace(applications[name]) && Path.IsPathFullyQualified(item.TargetProcessName))
                    applications[name] = item.TargetProcessName;
            }
            foreach (var name in applications.Keys.ToList())
            {
                if (!string.IsNullOrWhiteSpace(applications[name])) continue;
                var processes = System.Diagnostics.Process.GetProcessesByName(name);
                try
                {
                    foreach (var process in processes)
                    {
                        try
                        {
                            var path = process.MainModule?.FileName;
                            if (!string.IsNullOrWhiteSpace(path)) { applications[name] = path; break; }
                        }
                        catch (Exception ex) { _logger.LogDebug(ex, "Cannot read executable for {Name}", name); }
                    }
                }
                finally { foreach (var process in processes) process.Dispose(); }
            }
            configuration.Applications = applications;
        }

        /// <inheritdoc/>
        public async Task<bool> SaveConfigurationAsync(HotkeyConfiguration configuration)
        {
            ArgumentNullException.ThrowIfNull(configuration);

            try
            {
                _logger.LogDebug("Saving hotkey configuration to {Path}", _configFilePath);

                await RememberApplicationsAsync(configuration).ConfigureAwait(false);

                // Ensure directory exists
                Directory.CreateDirectory(_configDirectory);

                // Create backup of existing file
                if (File.Exists(_configFilePath))
                {
                    await CreateBackupAsync().ConfigureAwait(false);
                }

                // Update last modified timestamp
                configuration.LastModified = DateTime.UtcNow;

                var json = JsonSerializer.Serialize(configuration, JsonOptions);
                await File.WriteAllTextAsync(_configFilePath, json).ConfigureAwait(false);

                _logger.LogInformation(
                    "Saved hotkey configuration: {Count} items",
                    configuration.Count);

                return true;
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogError(ex, "Access denied saving hotkey configuration");
                return false;
            }
            catch (IOException ex)
            {
                _logger.LogError(ex, "Failed to write hotkey configuration file");
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error saving hotkey configuration");
                return false;
            }
        }

        /// <inheritdoc/>
        public async Task<HotkeyConfiguration?> ImportAsync(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath))
            {
                _logger.LogWarning("Import called with empty file path");
                return null;
            }

            try
            {
                _logger.LogDebug("Importing hotkey configuration from {Path}", filePath);

                if (!File.Exists(filePath))
                {
                    _logger.LogWarning("Import file not found: {Path}", filePath);
                    return null;
                }

                var json = await File.ReadAllTextAsync(filePath).ConfigureAwait(false);
                var configuration = JsonSerializer.Deserialize<HotkeyConfiguration>(json, JsonOptions);

                if (configuration == null)
                {
                    _logger.LogWarning("Failed to deserialize imported configuration");
                    return null;
                }

                // Assign new IDs to avoid conflicts
                foreach (var item in configuration.Items)
                {
                    item.Id = Guid.NewGuid();
                }

                _logger.LogInformation(
                    "Imported hotkey configuration: {Count} items from {Path}",
                    configuration.Count,
                    filePath);

                return configuration;
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "Failed to parse imported configuration file");
                return null;
            }
            catch (IOException ex)
            {
                _logger.LogError(ex, "Failed to read import file");
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error importing configuration");
                return null;
            }
        }

        /// <inheritdoc/>
        public async Task<bool> ExportAsync(string filePath, HotkeyConfiguration configuration)
        {
            if (string.IsNullOrWhiteSpace(filePath))
            {
                _logger.LogWarning("Export called with empty file path");
                return false;
            }

            ArgumentNullException.ThrowIfNull(configuration);

            try
            {
                _logger.LogDebug("Exporting hotkey configuration to {Path}", filePath);

                await RememberApplicationsAsync(configuration).ConfigureAwait(false);

                // Ensure directory exists
                var directory = Path.GetDirectoryName(filePath);
                if (!string.IsNullOrEmpty(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                var json = JsonSerializer.Serialize(configuration, JsonOptions);
                await File.WriteAllTextAsync(filePath, json).ConfigureAwait(false);

                _logger.LogInformation(
                    "Exported hotkey configuration: {Count} items to {Path}",
                    configuration.Count,
                    filePath);

                return true;
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogError(ex, "Access denied exporting configuration to {Path}", filePath);
                return false;
            }
            catch (IOException ex)
            {
                _logger.LogError(ex, "Failed to write export file");
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error exporting configuration");
                return false;
            }
        }

        /// <inheritdoc/>
        public HotkeyConfiguration GetDefaultConfiguration()
        {
            _logger.LogDebug("Creating default hotkey configuration");

            var configuration = new HotkeyConfiguration
            {
                Version = HotkeyConfiguration.CurrentVersion,
                Items =
                [
                    new HotkeyConfigItem(
                        "Toggle Mute (Focused)",
                        0x4D, // M key
                        HotkeyModifier.Ctrl | HotkeyModifier.Alt,
                        "ToggleMute")
                    {
                        Description = "Toggles mute for the currently focused application"
                    },

                    new HotkeyConfigItem(
                        "Minimize Window",
                        0x4E, // N key
                        HotkeyModifier.Ctrl | HotkeyModifier.Alt,
                        "Minimize")
                    {
                        Description = "Minimizes the currently focused window"
                    },

                    new HotkeyConfigItem(
                        "Bring to Front",
                        0x42, // B key
                        HotkeyModifier.Ctrl | HotkeyModifier.Alt,
                        "BringToFront")
                    {
                        Description = "Brings the selected process window to the front"
                    },

                    new HotkeyConfigItem(
                        "Close Window",
                        0x57, // W key
                        HotkeyModifier.Ctrl | HotkeyModifier.Alt | HotkeyModifier.Shift,
                        "Close")
                    {
                        Description = "Closes the currently focused window (Shift required for safety)"
                    },

                    new HotkeyConfigItem(
                        "Toggle Always on Top",
                        0x54, // T key
                        HotkeyModifier.Ctrl | HotkeyModifier.Alt,
                        "ToggleAlwaysOnTop")
                    {
                        Description = "Toggles always-on-top for the currently focused window"
                    }
                ]
            };

            _logger.LogInformation("Created default configuration with {Count} hotkeys", configuration.Count);

            return configuration;
        }

        /// <inheritdoc/>
        public IEnumerable<string> ValidateConfiguration(HotkeyConfiguration configuration)
        {
            if (configuration == null)
            {
                yield return "Configuration is null";
                yield break;
            }

            foreach (var error in configuration.Validate())
            {
                yield return error;
            }
        }

        /// <inheritdoc/>
        public bool HasConflict(HotkeyBinding binding, IEnumerable<HotkeyConfigItem> existingItems, Guid? excludeId = null)
        {
            if (existingItems == null) return false;

            return existingItems.Any(item =>
                item.VirtualKeyCode == binding.VirtualKeyCode &&
                item.Modifiers == binding.Modifiers &&
                (!excludeId.HasValue || item.Id != excludeId.Value));
        }

        /// <inheritdoc/>
        public string? GetSystemShortcutConflict(HotkeyBinding binding)
        {
            var key = (binding.VirtualKeyCode, binding.Modifiers);

            if (SystemShortcuts.TryGetValue(key, out var description))
            {
                return description;
            }

            return null;
        }

        /// <inheritdoc/>
        public async Task<bool> CreateBackupAsync()
        {
            try
            {
                if (!File.Exists(_configFilePath))
                {
                    _logger.LogDebug("No configuration file to backup");
                    return true;
                }

                _logger.LogDebug("Creating backup of configuration");

                var content = await File.ReadAllTextAsync(_configFilePath).ConfigureAwait(false);
                await File.WriteAllTextAsync(_backupFilePath, content).ConfigureAwait(false);

                _logger.LogInformation("Configuration backup created at {Path}", _backupFilePath);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create configuration backup");
                return false;
            }
        }

        /// <inheritdoc/>
        public async Task<HotkeyConfiguration?> RestoreFromBackupAsync()
        {
            try
            {
                if (!File.Exists(_backupFilePath))
                {
                    _logger.LogWarning("No backup file found to restore");
                    return null;
                }

                _logger.LogDebug("Restoring configuration from backup");

                var json = await File.ReadAllTextAsync(_backupFilePath).ConfigureAwait(false);
                var configuration = JsonSerializer.Deserialize<HotkeyConfiguration>(json, JsonOptions);

                if (configuration != null)
                {
                    _logger.LogInformation("Configuration restored from backup: {Count} items", configuration.Count);
                }

                return configuration;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to restore configuration from backup");
                return null;
            }
        }

        /// <inheritdoc/>
        public bool ConfigurationExists()
        {
            return File.Exists(_configFilePath);
        }
    }
}
