using SystemTrayProcessManager.Core.Models;

namespace SystemTrayProcessManager.Core.Services
{
    /// <summary>
    /// Provides gaming mode functionality for performance optimization.
    /// Manages process priorities, notification suppression, and background app management.
    /// </summary>
    public interface IGamingModeService
    {
        /// <summary>
        /// Gets a value indicating whether gaming mode is currently active.
        /// </summary>
        bool IsGamingModeActive { get; }

        /// <summary>
        /// Gets the current gaming mode configuration.
        /// </summary>
        GamingModeConfig Configuration { get; }

        /// <summary>
        /// Enables gaming mode with optional game process specification.
        /// </summary>
        /// <param name="gameProcessName">Optional name of the game process to prioritize.</param>
        /// <returns>True if gaming mode was enabled successfully; false otherwise.</returns>
        Task<bool> EnableGamingModeAsync(string? gameProcessName = null);

        /// <summary>
        /// Disables gaming mode and restores previous settings.
        /// </summary>
        /// <returns>True if gaming mode was disabled successfully; false otherwise.</returns>
        Task<bool> DisableGamingModeAsync();

        /// <summary>
        /// Toggles gaming mode on or off.
        /// </summary>
        /// <param name="gameProcessName">Optional game process name when enabling.</param>
        /// <returns>True if the operation was successful; the new state can be read from IsGamingModeActive.</returns>
        Task<bool> ToggleGamingModeAsync(string? gameProcessName = null);

        /// <summary>
        /// Updates the gaming mode configuration.
        /// </summary>
        /// <param name="config">The new configuration.</param>
        Task UpdateConfigurationAsync(GamingModeConfig config);

        /// <summary>
        /// Adds a process to the list of processes to close when entering gaming mode.
        /// </summary>
        /// <param name="processName">The process name to add.</param>
        Task AddProcessToCloseAsync(string processName);

        /// <summary>
        /// Adds a process to the list of processes to mute when entering gaming mode.
        /// </summary>
        /// <param name="processName">The process name to add.</param>
        Task AddProcessToMuteAsync(string processName);

        /// <summary>
        /// Adds a process to the list of processes to minimize when entering gaming mode.
        /// </summary>
        /// <param name="processName">The process name to add.</param>
        Task AddProcessToMinimizeAsync(string processName);

        /// <summary>
        /// Removes a process from all gaming mode lists.
        /// </summary>
        /// <param name="processName">The process name to remove.</param>
        Task RemoveProcessFromListsAsync(string processName);

        /// <summary>
        /// Occurs when gaming mode state changes.
        /// Provides true when enabled, false when disabled.
        /// </summary>
        event EventHandler<bool>? GamingModeChanged;
    }
}
