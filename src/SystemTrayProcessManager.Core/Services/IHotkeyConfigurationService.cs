using SystemTrayProcessManager.Core.Models;

namespace SystemTrayProcessManager.Core.Services
{
    /// <summary>
    /// Provides hotkey configuration persistence and management operations.
    /// Handles loading, saving, importing, and exporting hotkey configurations.
    /// </summary>
    public interface IHotkeyConfigurationService
    {
        /// <summary>
        /// Gets the path where the configuration file is stored.
        /// </summary>
        string ConfigurationFilePath { get; }

        /// <summary>
        /// Loads the hotkey configuration from the default location.
        /// If no configuration exists, returns the default configuration.
        /// </summary>
        /// <returns>The loaded hotkey configuration.</returns>
        Task<HotkeyConfiguration> LoadConfigurationAsync();

        /// <summary>
        /// Saves the hotkey configuration to the default location.
        /// Creates a backup of the existing configuration before saving.
        /// </summary>
        /// <param name="configuration">The configuration to save.</param>
        /// <returns>True if save was successful; otherwise, false.</returns>
        Task<bool> SaveConfigurationAsync(HotkeyConfiguration configuration);

        /// <summary>
        /// Imports a hotkey configuration from the specified file path.
        /// </summary>
        /// <param name="filePath">The path to the configuration file to import.</param>
        /// <returns>The imported configuration, or null if import failed.</returns>
        Task<HotkeyConfiguration?> ImportAsync(string filePath);

        /// <summary>
        /// Exports the hotkey configuration to the specified file path.
        /// </summary>
        /// <param name="filePath">The path where the configuration should be saved.</param>
        /// <param name="configuration">The configuration to export.</param>
        /// <returns>True if export was successful; otherwise, false.</returns>
        Task<bool> ExportAsync(string filePath, HotkeyConfiguration configuration);

        /// <summary>
        /// Gets the default hotkey configuration with preset bindings.
        /// </summary>
        /// <returns>The default hotkey configuration.</returns>
        HotkeyConfiguration GetDefaultConfiguration();

        /// <summary>
        /// Validates a hotkey configuration and returns any validation errors.
        /// </summary>
        /// <param name="configuration">The configuration to validate.</param>
        /// <returns>A collection of validation error messages, empty if valid.</returns>
        IEnumerable<string> ValidateConfiguration(HotkeyConfiguration configuration);

        /// <summary>
        /// Determines whether a hotkey binding conflicts with existing configuration items.
        /// </summary>
        /// <param name="binding">The binding to check.</param>
        /// <param name="existingItems">The existing configuration items to check against.</param>
        /// <param name="excludeId">Optional ID to exclude from conflict checking (for editing).</param>
        /// <returns>True if a conflict exists; otherwise, false.</returns>
        bool HasConflict(HotkeyBinding binding, IEnumerable<HotkeyConfigItem> existingItems, Guid? excludeId = null);

        /// <summary>
        /// Determines whether a hotkey binding conflicts with known system shortcuts.
        /// </summary>
        /// <param name="binding">The binding to check.</param>
        /// <returns>A warning message if conflict detected, or null if no conflict.</returns>
        string? GetSystemShortcutConflict(HotkeyBinding binding);

        /// <summary>
        /// Creates a backup of the current configuration.
        /// </summary>
        /// <returns>True if backup was successful; otherwise, false.</returns>
        Task<bool> CreateBackupAsync();

        /// <summary>
        /// Restores the configuration from the most recent backup.
        /// </summary>
        /// <returns>The restored configuration, or null if restore failed.</returns>
        Task<HotkeyConfiguration?> RestoreFromBackupAsync();

        /// <summary>
        /// Checks if a configuration file exists.
        /// </summary>
        /// <returns>True if a configuration file exists; otherwise, false.</returns>
        bool ConfigurationExists();
    }
}
