namespace SystemTrayProcessManager.Core.Enums
{
    /// <summary>
    /// Represents the visual state of a window.
    /// </summary>
    public enum WindowState
    {
        /// <summary>
        /// The window is in its normal, restored state.
        /// </summary>
        Normal = 0,

        /// <summary>
        /// The window is minimized to the taskbar.
        /// </summary>
        Minimized = 1,

        /// <summary>
        /// The window is maximized to fill the screen.
        /// </summary>
        Maximized = 2,

        /// <summary>
        /// The window is hidden and not visible.
        /// </summary>
        Hidden = 3,

        /// <summary>
        /// The window handle is invalid or the window no longer exists.
        /// </summary>
        Invalid = -1
    }
}
