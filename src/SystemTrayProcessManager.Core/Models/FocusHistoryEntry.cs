namespace SystemTrayProcessManager.Core.Models
{
    /// <summary>
    /// Represents a focus change event entry in the focus history.
    /// Tracks which window gained focus and when.
    /// </summary>
    public sealed record FocusHistoryEntry
    {
        /// <summary>
        /// Gets the handle to the window that gained focus.
        /// </summary>
        public IntPtr WindowHandle { get; init; }

        /// <summary>
        /// Gets the process identifier of the window.
        /// </summary>
        public int ProcessId { get; init; }

        /// <summary>
        /// Gets the name of the process.
        /// </summary>
        public string ProcessName { get; init; } = string.Empty;

        /// <summary>
        /// Gets the title of the window at the time of focus.
        /// </summary>
        public string WindowTitle { get; init; } = string.Empty;

        /// <summary>
        /// Gets the timestamp when the window gained focus.
        /// </summary>
        public DateTime FocusedAt { get; init; } = DateTime.UtcNow;

        /// <summary>
        /// Creates a new focus history entry with the specified parameters.
        /// </summary>
        /// <param name="windowHandle">Handle to the window.</param>
        /// <param name="processId">Process identifier.</param>
        /// <param name="processName">Name of the process.</param>
        /// <param name="windowTitle">Title of the window.</param>
        /// <returns>A new focus history entry.</returns>
        public static FocusHistoryEntry Create(IntPtr windowHandle, int processId, string processName, string windowTitle)
        {
            return new FocusHistoryEntry
            {
                WindowHandle = windowHandle,
                ProcessId = processId,
                ProcessName = processName ?? string.Empty,
                WindowTitle = windowTitle ?? string.Empty,
                FocusedAt = DateTime.UtcNow
            };
        }

        /// <summary>
        /// Determines whether the entry is still valid (window still exists).
        /// </summary>
        public bool IsValid => WindowHandle != IntPtr.Zero && ProcessId > 0;
    }
}
