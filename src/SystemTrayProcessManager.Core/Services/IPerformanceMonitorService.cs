using SystemTrayProcessManager.Core.Models;

namespace SystemTrayProcessManager.Core.Services
{
    /// <summary>
    /// Service for monitoring and reporting application performance metrics.
    /// Tracks startup time, memory usage, CPU usage, and cache statistics.
    /// </summary>
    public interface IPerformanceMonitorService : IDisposable
    {
        /// <summary>
        /// Gets the recorded application startup time in milliseconds.
        /// Returns -1 if startup timing has not been recorded.
        /// </summary>
        long StartupTimeMs { get; }

        /// <summary>
        /// Gets a value indicating whether startup timing is currently in progress.
        /// </summary>
        bool IsTimingStartup { get; }

        /// <summary>
        /// Gets a value indicating whether periodic monitoring is active.
        /// </summary>
        bool IsMonitoring { get; }

        /// <summary>
        /// Gets the total number of icon cache hits recorded.
        /// </summary>
        long IconCacheHits { get; }

        /// <summary>
        /// Gets the total number of icon cache misses recorded.
        /// </summary>
        long IconCacheMisses { get; }

        /// <summary>
        /// Starts timing the application startup.
        /// Call at the beginning of App.OnStartup.
        /// </summary>
        void StartStartupTimer();

        /// <summary>
        /// Records the application startup completion time.
        /// Call after MainWindow.Loaded or when startup is considered complete.
        /// </summary>
        void RecordStartupComplete();

        /// <summary>
        /// Gets the current performance metrics snapshot.
        /// </summary>
        /// <returns>A snapshot of current performance metrics.</returns>
        PerformanceMetrics GetCurrentMetrics();

        /// <summary>
        /// Gets the icon cache hit rate (0.0 to 1.0).
        /// Returns 0.0 if no cache accesses have been recorded.
        /// </summary>
        /// <returns>The cache hit rate as a decimal.</returns>
        double GetIconCacheHitRate();

        /// <summary>
        /// Records an icon cache access (hit or miss).
        /// </summary>
        /// <param name="isHit">True if the icon was found in cache; false otherwise.</param>
        void RecordIconCacheAccess(bool isHit);

        /// <summary>
        /// Forces a garbage collection and returns the memory freed in bytes.
        /// </summary>
        /// <returns>The approximate number of bytes freed.</returns>
        long ForceGarbageCollection();

        /// <summary>
        /// Starts periodic metrics collection at the specified interval.
        /// </summary>
        /// <param name="interval">The collection interval.</param>
        void StartMonitoring(TimeSpan interval);

        /// <summary>
        /// Stops periodic metrics collection.
        /// </summary>
        void StopMonitoring();

        /// <summary>
        /// Updates the icon cache size and max size for metrics reporting.
        /// </summary>
        /// <param name="currentSize">The current number of cached icons.</param>
        /// <param name="maxSize">The maximum cache capacity.</param>
        void UpdateIconCacheStats(int currentSize, int maxSize);

        /// <summary>
        /// Updates the tracked process count for metrics reporting.
        /// </summary>
        /// <param name="count">The current number of tracked processes.</param>
        void UpdateTrackedProcessCount(int count);

        /// <summary>
        /// Updates the process refresh interval for metrics reporting.
        /// </summary>
        /// <param name="intervalMs">The refresh interval in milliseconds.</param>
        void UpdateProcessRefreshInterval(int intervalMs);

        /// <summary>
        /// Event raised when metrics are updated during periodic monitoring.
        /// </summary>
        event EventHandler<PerformanceMetrics>? MetricsUpdated;
    }
}
