using SystemTrayProcessManager.Core.Models;

namespace SystemTrayProcessManager.Core.Services
{
    /// <summary>
    /// Provides centralized application settings persistence.
    /// Handles loading, saving, backup, restore, and reset operations for <see cref="AppSettings"/>.
    /// </summary>
    public interface IConfigurationService
    {
        /// <summary>
        /// Gets the file path where settings are persisted.
        /// </summary>
        string SettingsFilePath { get; }

        /// <summary>
        /// Gets the currently loaded application settings.
        /// Returns default settings if not yet loaded.
        /// </summary>
        AppSettings CurrentSettings { get; }

        /// <summary>
        /// Loads application settings from the JSON file.
        /// Returns default settings if the file does not exist or is corrupted.
        /// </summary>
        /// <returns>The loaded <see cref="AppSettings"/> instance.</returns>
        Task<AppSettings> LoadSettingsAsync();

        /// <summary>
        /// Saves application settings to the JSON file.
        /// Automatically creates a backup before writing.
        /// </summary>
        /// <param name="settings">The settings to save.</param>
        /// <returns>True if the save succeeded; otherwise, false.</returns>
        Task<bool> SaveSettingsAsync(AppSettings settings);

        /// <summary>
        /// Creates a backup of the current settings file.
        /// </summary>
        /// <returns>True if the backup was created successfully; otherwise, false.</returns>
        Task<bool> BackupAsync();

        /// <summary>
        /// Restores settings from the backup file.
        /// </summary>
        /// <returns>The restored settings, or null if no backup exists or restoration failed.</returns>
        Task<AppSettings?> RestoreFromBackupAsync();

        /// <summary>
        /// Resets all settings to their default values and saves them.
        /// </summary>
        /// <returns>The default <see cref="AppSettings"/> instance after saving.</returns>
        Task<AppSettings> ResetToDefaultsAsync();

        /// <summary>
        /// Gets the default application settings.
        /// </summary>
        /// <returns>A new <see cref="AppSettings"/> with default values.</returns>
        AppSettings GetDefaults();

        /// <summary>
        /// Checks whether a settings file already exists on disk.
        /// </summary>
        /// <returns>True if the settings file exists; otherwise, false.</returns>
        bool SettingsFileExists();

        /// <summary>
        /// Checks whether a backup file exists on disk.
        /// </summary>
        /// <returns>True if the backup file exists; otherwise, false.</returns>
        bool BackupFileExists();

        /// <summary>
        /// Exports the current settings to a specified file path.
        /// </summary>
        /// <param name="filePath">The destination file path.</param>
        /// <param name="settings">The settings to export.</param>
        /// <returns>True if the export succeeded; otherwise, false.</returns>
        Task<bool> ExportAsync(string filePath, AppSettings settings);

        /// <summary>
        /// Imports settings from a specified file path.
        /// </summary>
        /// <param name="filePath">The source file path.</param>
        /// <returns>The imported settings, or null if import failed.</returns>
        Task<AppSettings?> ImportAsync(string filePath);

        /// <summary>
        /// Raised when settings are loaded or changed.
        /// </summary>
        event EventHandler<AppSettings>? SettingsChanged;
    }
}
