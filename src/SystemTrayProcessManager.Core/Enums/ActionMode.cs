namespace SystemTrayProcessManager.Core.Enums
{
    /// <summary>
    /// Defines the execution mode for action mappings.
    /// Determines how the target process is identified when an action is triggered.
    /// </summary>
    public enum ActionMode
    {
        /// <summary>
        /// Quick Action mode targets the currently focused/foreground window.
        /// The action is applied to whatever window has focus when the hotkey is pressed.
        /// </summary>
        QuickAction = 0,

        /// <summary>
        /// Pinned Process mode targets a specific process by name.
        /// The target process is specified in the hotkey configuration.
        /// </summary>
        PinnedProcess = 1
    }
}
