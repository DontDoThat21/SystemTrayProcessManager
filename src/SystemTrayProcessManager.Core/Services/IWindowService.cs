using SystemTrayProcessManager.Core.Enums;

namespace SystemTrayProcessManager.Core.Services
{
    /// <summary>
    /// Provides window manipulation capabilities for Windows applications.
    /// Enables control over window visibility, position, transparency, and z-order.
    /// </summary>
    public interface IWindowService
    {
        /// <summary>
        /// Brings the specified window to the foreground and activates it.
        /// If the window is minimized, it will be restored first.
        /// </summary>
        /// <param name="windowHandle">Handle to the window to bring to front.</param>
        /// <returns>True if the operation succeeded; false otherwise.</returns>
        /// <remarks>
        /// This operation may fail for protected or system windows.
        /// Windows has restrictions on SetForegroundWindow that may prevent
        /// bringing windows to front in certain circumstances.
        /// </remarks>
        Task<bool> BringToFrontAsync(IntPtr windowHandle);

        /// <summary>
        /// Minimizes the specified window to the taskbar.
        /// </summary>
        /// <param name="windowHandle">Handle to the window to minimize.</param>
        /// <returns>True if the operation succeeded; false otherwise.</returns>
        Task<bool> MinimizeAsync(IntPtr windowHandle);

        /// <summary>
        /// Maximizes the specified window to fill the screen.
        /// </summary>
        /// <param name="windowHandle">Handle to the window to maximize.</param>
        /// <returns>True if the operation succeeded; false otherwise.</returns>
        Task<bool> MaximizeAsync(IntPtr windowHandle);

        /// <summary>
        /// Restores the specified window to its normal size and position.
        /// </summary>
        /// <param name="windowHandle">Handle to the window to restore.</param>
        /// <returns>True if the operation succeeded; false otherwise.</returns>
        Task<bool> RestoreAsync(IntPtr windowHandle);

        /// <summary>
        /// Closes the specified window.
        /// </summary>
        /// <param name="windowHandle">Handle to the window to close.</param>
        /// <param name="force">
        /// If true, sends WM_CLOSE without waiting for response.
        /// If false, sends a graceful close request that allows the application to prompt for save.
        /// </param>
        /// <returns>True if the close message was sent; false otherwise.</returns>
        /// <remarks>
        /// Force close may cause data loss if the application has unsaved changes.
        /// The return value indicates the message was sent, not that the window closed.
        /// </remarks>
        Task<bool> CloseAsync(IntPtr windowHandle, bool force = false);

        /// <summary>
        /// Hides the specified window, removing it from view without closing it.
        /// </summary>
        /// <param name="windowHandle">Handle to the window to hide.</param>
        /// <returns>True if the operation succeeded; false otherwise.</returns>
        /// <remarks>
        /// Hidden windows remain in memory and can be restored using <see cref="ShowAsync"/>.
        /// </remarks>
        Task<bool> HideAsync(IntPtr windowHandle);

        /// <summary>
        /// Shows a previously hidden window.
        /// </summary>
        /// <param name="windowHandle">Handle to the window to show.</param>
        /// <returns>True if the operation succeeded; false otherwise.</returns>
        Task<bool> ShowAsync(IntPtr windowHandle);

        /// <summary>
        /// Sets the transparency level of the specified window.
        /// </summary>
        /// <param name="windowHandle">Handle to the window.</param>
        /// <param name="alpha">
        /// Alpha value from 0 (fully transparent) to 255 (fully opaque).
        /// </param>
        /// <returns>True if the operation succeeded; false otherwise.</returns>
        /// <remarks>
        /// This method automatically adds the WS_EX_LAYERED style if not present.
        /// Setting alpha to 255 removes transparency but retains the layered style.
        /// </remarks>
        Task<bool> SetTransparencyAsync(IntPtr windowHandle, byte alpha);

        /// <summary>
        /// Sets or removes the always-on-top flag for the specified window.
        /// </summary>
        /// <param name="windowHandle">Handle to the window.</param>
        /// <param name="enabled">True to make the window always on top; false to remove the flag.</param>
        /// <returns>True if the operation succeeded; false otherwise.</returns>
        Task<bool> SetAlwaysOnTopAsync(IntPtr windowHandle, bool enabled);

        /// <summary>
        /// Gets the current visual state of the specified window.
        /// </summary>
        /// <param name="windowHandle">Handle to the window to check.</param>
        /// <returns>The current <see cref="WindowState"/> of the window.</returns>
        Task<WindowState> GetWindowStateAsync(IntPtr windowHandle);

        /// <summary>
        /// Checks if the specified window is currently visible.
        /// </summary>
        /// <param name="windowHandle">Handle to the window to check.</param>
        /// <returns>True if the window is visible; false otherwise.</returns>
        Task<bool> IsWindowVisibleAsync(IntPtr windowHandle);

        /// <summary>
        /// Checks if the specified window has the always-on-top flag set.
        /// </summary>
        /// <param name="windowHandle">Handle to the window to check.</param>
        /// <returns>True if the window is always on top; false otherwise.</returns>
        Task<bool> IsAlwaysOnTopAsync(IntPtr windowHandle);

        /// <summary>
        /// Gets the current transparency level of the specified window.
        /// </summary>
        /// <param name="windowHandle">Handle to the window to check.</param>
        /// <returns>
        /// Alpha value from 0-255, or 255 if the window is not layered or on error.
        /// </returns>
        Task<byte> GetTransparencyAsync(IntPtr windowHandle);

        /// <summary>
        /// Validates that the specified window handle refers to a valid window.
        /// </summary>
        /// <param name="windowHandle">Handle to validate.</param>
        /// <returns>True if the handle refers to a valid window; false otherwise.</returns>
        bool IsValidWindow(IntPtr windowHandle);
    }
}
