using SystemTrayProcessManager.Core.Models;

namespace SystemTrayProcessManager.Core.Services
{
    /// <summary>
    /// Provides window position persistence and restoration.
    /// Saves and restores window positions per monitor configuration.
    /// </summary>
    public interface IWindowPositionService
    {
        /// <summary>
        /// Gets the current monitor layout configuration.
        /// </summary>
        /// <returns>The current monitor layout with unique hash.</returns>
        Task<MonitorLayout> GetCurrentLayoutAsync();

        /// <summary>
        /// Saves the current position of a window.
        /// </summary>
        /// <param name="windowHandle">Handle to the window.</param>
        /// <param name="processName">Name of the process.</param>
        /// <param name="windowTitle">Optional window title for specific matching.</param>
        /// <returns>The saved window position, or null if save failed.</returns>
        Task<WindowPosition?> SaveWindowPositionAsync(IntPtr windowHandle, string processName, string? windowTitle = null);

        /// <summary>
        /// Gets the saved position for a process.
        /// </summary>
        /// <param name="processName">Name of the process.</param>
        /// <param name="layoutHash">Optional specific layout hash; uses current layout if null.</param>
        /// <returns>The saved position, or null if not found.</returns>
        Task<WindowPosition?> GetSavedPositionAsync(string processName, string? layoutHash = null);

        /// <summary>
        /// Gets all saved positions for the specified layout.
        /// </summary>
        /// <param name="layoutHash">Optional layout hash; uses current layout if null.</param>
        /// <returns>All saved positions for the layout.</returns>
        Task<IReadOnlyList<WindowPosition>> GetAllSavedPositionsAsync(string? layoutHash = null);

        /// <summary>
        /// Restores the saved position for a window.
        /// </summary>
        /// <param name="windowHandle">Handle to the window.</param>
        /// <param name="processName">Name of the process.</param>
        /// <returns>True if position was restored; false otherwise.</returns>
        Task<bool> RestoreWindowPositionAsync(IntPtr windowHandle, string processName);

        /// <summary>
        /// Restores positions for all windows with saved positions.
        /// </summary>
        /// <returns>The number of windows whose positions were restored.</returns>
        Task<int> RestoreAllWindowPositionsAsync();

        /// <summary>
        /// Clears saved positions for the specified layout.
        /// </summary>
        /// <param name="layoutHash">Optional layout hash; clears current layout if null.</param>
        Task ClearSavedPositionsAsync(string? layoutHash = null);

        /// <summary>
        /// Clears all saved positions for all layouts.
        /// </summary>
        Task ClearAllSavedPositionsAsync();

        /// <summary>
        /// Deletes a specific saved position.
        /// </summary>
        /// <param name="positionId">The ID of the position to delete.</param>
        Task DeleteSavedPositionAsync(Guid positionId);

        /// <summary>
        /// Occurs when the monitor layout changes.
        /// </summary>
        event EventHandler<MonitorLayout>? MonitorLayoutChanged;
    }
}
