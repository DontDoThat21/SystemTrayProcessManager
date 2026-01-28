namespace SystemTrayProcessManager.Core.Models
{
    /// <summary>
    /// Represents the fullscreen state of a window.
    /// Used for tracking fullscreen mode transitions.
    /// </summary>
    public sealed record FullscreenState
    {
        /// <summary>
        /// Gets the handle to the fullscreen window.
        /// </summary>
        public IntPtr WindowHandle { get; init; }

        /// <summary>
        /// Gets the process identifier of the fullscreen window.
        /// </summary>
        public int ProcessId { get; init; }

        /// <summary>
        /// Gets the name of the process running in fullscreen.
        /// </summary>
        public string ProcessName { get; init; } = string.Empty;

        /// <summary>
        /// Gets a value indicating whether the window is currently fullscreen.
        /// </summary>
        public bool IsFullscreen { get; init; }

        /// <summary>
        /// Gets the timestamp when the fullscreen state was detected.
        /// </summary>
        public DateTime DetectedAt { get; init; } = DateTime.UtcNow;

        /// <summary>
        /// Creates a new fullscreen state entry.
        /// </summary>
        /// <param name="windowHandle">Handle to the window.</param>
        /// <param name="processId">Process identifier.</param>
        /// <param name="processName">Name of the process.</param>
        /// <param name="isFullscreen">Whether the window is fullscreen.</param>
        /// <returns>A new fullscreen state entry.</returns>
        public static FullscreenState Create(IntPtr windowHandle, int processId, string processName, bool isFullscreen)
        {
            return new FullscreenState
            {
                WindowHandle = windowHandle,
                ProcessId = processId,
                ProcessName = processName ?? string.Empty,
                IsFullscreen = isFullscreen,
                DetectedAt = DateTime.UtcNow
            };
        }

        /// <summary>
        /// Creates an empty state indicating no fullscreen window.
        /// </summary>
        public static FullscreenState None => new()
        {
            WindowHandle = IntPtr.Zero,
            ProcessId = 0,
            ProcessName = string.Empty,
            IsFullscreen = false,
            DetectedAt = DateTime.UtcNow
        };
    }
}
