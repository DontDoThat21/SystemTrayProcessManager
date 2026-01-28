namespace SystemTrayProcessManager.Core.Models
{
    /// <summary>
    /// Represents information about a single display monitor.
    /// </summary>
    public sealed record MonitorInfo
    {
        /// <summary>
        /// Gets the device name of the monitor (e.g., "\\.\DISPLAY1").
        /// </summary>
        public string DeviceName { get; init; } = string.Empty;

        /// <summary>
        /// Gets the X coordinate of the monitor's top-left corner.
        /// </summary>
        public int X { get; init; }

        /// <summary>
        /// Gets the Y coordinate of the monitor's top-left corner.
        /// </summary>
        public int Y { get; init; }

        /// <summary>
        /// Gets the width of the monitor in pixels.
        /// </summary>
        public int Width { get; init; }

        /// <summary>
        /// Gets the height of the monitor in pixels.
        /// </summary>
        public int Height { get; init; }

        /// <summary>
        /// Gets a value indicating whether this is the primary monitor.
        /// </summary>
        public bool IsPrimary { get; init; }

        /// <summary>
        /// Creates a new monitor info with the specified parameters.
        /// </summary>
        public static MonitorInfo Create(string deviceName, int x, int y, int width, int height, bool isPrimary)
        {
            return new MonitorInfo
            {
                DeviceName = deviceName ?? string.Empty,
                X = x,
                Y = y,
                Width = width,
                Height = height,
                IsPrimary = isPrimary
            };
        }

        /// <summary>
        /// Gets a string representation suitable for hashing.
        /// </summary>
        public string ToHashString()
        {
            return $"{DeviceName}:{X},{Y},{Width},{Height},{IsPrimary}";
        }
    }
}
