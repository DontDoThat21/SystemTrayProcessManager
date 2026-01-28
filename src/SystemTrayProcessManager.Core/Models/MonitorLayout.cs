using System.Security.Cryptography;
using System.Text;

namespace SystemTrayProcessManager.Core.Models
{
    /// <summary>
    /// Represents a monitor configuration layout with a unique identifying hash.
    /// Used to track different multi-monitor setups.
    /// </summary>
    public sealed record MonitorLayout
    {
        /// <summary>
        /// Gets the unique hash identifying this monitor configuration.
        /// </summary>
        public string LayoutHash { get; init; } = string.Empty;

        /// <summary>
        /// Gets the number of monitors in this layout.
        /// </summary>
        public int MonitorCount { get; init; }

        /// <summary>
        /// Gets the list of monitors in this layout.
        /// </summary>
        public IReadOnlyList<MonitorInfo> Monitors { get; init; } = Array.Empty<MonitorInfo>();

        /// <summary>
        /// Creates a new monitor layout from a list of monitors.
        /// Automatically computes the layout hash.
        /// </summary>
        /// <param name="monitors">The list of monitors.</param>
        /// <returns>A new monitor layout.</returns>
        public static MonitorLayout Create(IEnumerable<MonitorInfo> monitors)
        {
            var monitorList = monitors?.ToList() ?? new List<MonitorInfo>();
            var hash = ComputeLayoutHash(monitorList);

            return new MonitorLayout
            {
                LayoutHash = hash,
                MonitorCount = monitorList.Count,
                Monitors = monitorList.AsReadOnly()
            };
        }

        /// <summary>
        /// Computes a unique hash for the monitor configuration.
        /// </summary>
        private static string ComputeLayoutHash(IReadOnlyList<MonitorInfo> monitors)
        {
            if (monitors == null || monitors.Count == 0)
            {
                return "empty";
            }

            // Sort monitors by device name for consistent hashing
            var sorted = monitors.OrderBy(m => m.DeviceName).ToList();
            var combined = string.Join("|", sorted.Select(m => m.ToHashString()));

            // Compute SHA256 hash and return first 16 characters
            var bytes = Encoding.UTF8.GetBytes(combined);
            var hashBytes = SHA256.HashData(bytes);
            return Convert.ToHexString(hashBytes)[..16].ToLowerInvariant();
        }

        /// <summary>
        /// Gets the primary monitor in this layout, or null if none.
        /// </summary>
        public MonitorInfo? PrimaryMonitor => Monitors.FirstOrDefault(m => m.IsPrimary);

        /// <summary>
        /// Determines whether two layouts have the same configuration.
        /// </summary>
        public bool IsSameConfiguration(MonitorLayout? other)
        {
            return other != null && LayoutHash == other.LayoutHash;
        }
    }
}
