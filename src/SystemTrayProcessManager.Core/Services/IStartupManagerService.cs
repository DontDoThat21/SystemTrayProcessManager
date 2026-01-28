namespace SystemTrayProcessManager.Core.Services
{
    /// <summary>
    /// Provides Windows startup management functionality.
    /// Manages automatic application startup via registry.
    /// </summary>
    public interface IStartupManagerService
    {
        /// <summary>
        /// Gets a value indicating whether the application is configured to start with Windows.
        /// </summary>
        bool IsStartupEnabled { get; }

        /// <summary>
        /// Enables automatic startup with Windows.
        /// </summary>
        /// <param name="startMinimized">Whether to start the application minimized.</param>
        /// <returns>True if startup was enabled successfully; false otherwise.</returns>
        Task<bool> EnableStartupAsync(bool startMinimized = true);

        /// <summary>
        /// Disables automatic startup with Windows.
        /// </summary>
        /// <returns>True if startup was disabled successfully; false otherwise.</returns>
        Task<bool> DisableStartupAsync();

        /// <summary>
        /// Toggles the startup setting.
        /// </summary>
        /// <param name="startMinimized">Whether to start minimized (when enabling).</param>
        /// <returns>True if the operation was successful; the new state can be read from IsStartupEnabled.</returns>
        Task<bool> ToggleStartupAsync(bool startMinimized = true);

        /// <summary>
        /// Refreshes the startup status from the registry.
        /// </summary>
        /// <returns>True if refresh was successful; false otherwise.</returns>
        Task<bool> RefreshStartupStatusAsync();

        /// <summary>
        /// Gets the path to the executable that would be started.
        /// </summary>
        string ExecutablePath { get; }

        /// <summary>
        /// Occurs when the startup state changes.
        /// Provides true when enabled, false when disabled.
        /// </summary>
        event EventHandler<bool>? StartupStateChanged;
    }
}
