using SystemTrayProcessManager.Core.Enums;

namespace SystemTrayProcessManager.Core.Models
{
    /// <summary>
    /// Represents a saved window position configuration.
    /// Stores position data associated with a specific monitor layout.
    /// </summary>
    public sealed record WindowPosition
    {
        /// <summary>
        /// Gets the unique identifier for this position entry.
        /// </summary>
        public Guid Id { get; init; } = Guid.NewGuid();

        /// <summary>
        /// Gets the name of the process this position is for.
        /// </summary>
        public string ProcessName { get; init; } = string.Empty;

        /// <summary>
        /// Gets the optional window title for more specific matching.
        /// </summary>
        public string? WindowTitle { get; init; }

        /// <summary>
        /// Gets the X coordinate of the window's top-left corner.
        /// </summary>
        public int X { get; init; }

        /// <summary>
        /// Gets the Y coordinate of the window's top-left corner.
        /// </summary>
        public int Y { get; init; }

        /// <summary>
        /// Gets the width of the window in pixels.
        /// </summary>
        public int Width { get; init; }

        /// <summary>
        /// Gets the height of the window in pixels.
        /// </summary>
        public int Height { get; init; }

        /// <summary>
        /// Gets the window state (Normal, Minimized, Maximized).
        /// </summary>
        public WindowState WindowState { get; init; } = WindowState.Normal;

        /// <summary>
        /// Gets the hash of the monitor layout this position was saved for.
        /// </summary>
        public string MonitorLayoutHash { get; init; } = string.Empty;

        /// <summary>
        /// Gets the timestamp when this position was saved.
        /// </summary>
        public DateTime SavedAt { get; init; } = DateTime.UtcNow;

        /// <summary>
        /// Creates a new window position entry.
        /// </summary>
        public static WindowPosition Create(
            string processName,
            int x, int y, int width, int height,
            WindowState windowState,
            string monitorLayoutHash,
            string? windowTitle = null)
        {
            return new WindowPosition
            {
                Id = Guid.NewGuid(),
                ProcessName = processName ?? string.Empty,
                WindowTitle = windowTitle,
                X = x,
                Y = y,
                Width = width,
                Height = height,
                WindowState = windowState,
                MonitorLayoutHash = monitorLayoutHash ?? string.Empty,
                SavedAt = DateTime.UtcNow
            };
        }

        /// <summary>
        /// Determines whether this position matches the specified process and optional title.
        /// </summary>
        public bool Matches(string processName, string? windowTitle = null)
        {
            if (!string.Equals(ProcessName, processName, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            // If we have a window title constraint, check it
            if (!string.IsNullOrEmpty(WindowTitle) && !string.IsNullOrEmpty(windowTitle))
            {
                return windowTitle.Contains(WindowTitle, StringComparison.OrdinalIgnoreCase);
            }

            return true;
        }
    }
}
