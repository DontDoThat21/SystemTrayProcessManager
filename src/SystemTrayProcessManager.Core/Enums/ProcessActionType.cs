namespace SystemTrayProcessManager.Core.Enums
{
    /// <summary>
    /// Defines the types of actions that can be performed on a process or its window.
    /// This enum uses flags to allow combining multiple actions if needed.
    /// </summary>
    [Flags]
    public enum ProcessActionType
    {
        /// <summary>
        /// No action specified.
        /// </summary>
        None = 0,

        /// <summary>
        /// Mutes the audio for the process.
        /// This action is reversible via <see cref="Unmute"/>.
        /// </summary>
        Mute = 1,

        /// <summary>
        /// Unmutes the audio for the process.
        /// This action is reversible via <see cref="Mute"/>.
        /// </summary>
        Unmute = 2,

        /// <summary>
        /// Toggles the mute state of the process audio.
        /// If muted, unmutes; if unmuted, mutes.
        /// </summary>
        ToggleMute = 4,

        /// <summary>
        /// Closes the process window.
        /// This action is NOT reversible.
        /// </summary>
        Close = 8,

        /// <summary>
        /// Minimizes the process window to the taskbar.
        /// This action is reversible via <see cref="Restore"/>.
        /// </summary>
        Minimize = 16,

        /// <summary>
        /// Maximizes the process window to fill the screen.
        /// This action is reversible via <see cref="Restore"/>.
        /// </summary>
        Maximize = 32,

        /// <summary>
        /// Restores the process window to its normal size and position.
        /// Used to reverse <see cref="Minimize"/> or <see cref="Maximize"/>.
        /// </summary>
        Restore = 64,

        /// <summary>
        /// Brings the process window to the foreground and activates it.
        /// This action is NOT reversible.
        /// </summary>
        BringToFront = 128,

        /// <summary>
        /// Hides the process window from view without closing it.
        /// This action is reversible via <see cref="Show"/>.
        /// </summary>
        Hide = 256,

        /// <summary>
        /// Shows a previously hidden process window.
        /// This action is reversible via <see cref="Hide"/>.
        /// </summary>
        Show = 512,

        /// <summary>Toggles whether the application window stays above other windows.</summary>
        ToggleAlwaysOnTop = 1024,

        /// <summary>Sends the previous media track command to the application window.</summary>
        PreviousTrack = 2048,

        /// <summary>Sends the next media track command to the application window.</summary>
        NextTrack = 4096,

        /// <summary>Opens a saved application, or focuses its existing window.</summary>
        Open = 8192,

        /// <summary>Closes a running application gracefully, or opens it when stopped.</summary>
        ToggleOpenClose = 16384
    }
}
