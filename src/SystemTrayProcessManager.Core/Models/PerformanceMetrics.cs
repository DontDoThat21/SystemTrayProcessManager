namespace SystemTrayProcessManager.Core.Models
{
    /// <summary>
    /// Represents a snapshot of application performance metrics at a specific point in time.
    /// Includes memory, CPU, startup time, and cache statistics for performance monitoring.
    /// </summary>
    public sealed class PerformanceMetrics
    {
        #region Performance Targets

        /// <summary>
        /// Target startup time in milliseconds.
        /// </summary>
        public const long TargetStartupTimeMs = 2000;

        /// <summary>
        /// Target memory usage in megabytes.
        /// </summary>
        public const double TargetMemoryMB = 50.0;

        /// <summary>
        /// Target CPU usage percentage.
        /// </summary>
        public const double TargetCpuPercent = 1.0;

        /// <summary>
        /// Target icon cache hit rate.
        /// </summary>
        public const double TargetCacheHitRate = 0.80;

        #endregion

        #region Startup Metrics

        /// <summary>
        /// Gets the application startup time in milliseconds.
        /// Measured from App.OnStartup to MainWindow.Loaded.
        /// </summary>
        public long StartupTimeMs { get; init; }

        /// <summary>
        /// Gets a value indicating whether startup time is within target (<2 seconds).
        /// </summary>
        public bool StartupTimeWithinTarget => StartupTimeMs < TargetStartupTimeMs;

        #endregion

        #region Memory Metrics

        /// <summary>
        /// Gets the working set memory in bytes (physical memory used).
        /// </summary>
        public long WorkingSetBytes { get; init; }

        /// <summary>
        /// Gets the private memory in bytes (memory that cannot be shared).
        /// </summary>
        public long PrivateMemoryBytes { get; init; }

        /// <summary>
        /// Gets the total managed memory reported by the GC.
        /// </summary>
        public long GCTotalMemoryBytes { get; init; }

        /// <summary>
        /// Gets the working set memory in megabytes.
        /// </summary>
        public double MemoryUsageMB => WorkingSetBytes / (1024.0 * 1024.0);

        /// <summary>
        /// Gets the private memory in megabytes.
        /// </summary>
        public double PrivateMemoryMB => PrivateMemoryBytes / (1024.0 * 1024.0);

        /// <summary>
        /// Gets the GC total memory in megabytes.
        /// </summary>
        public double GCMemoryMB => GCTotalMemoryBytes / (1024.0 * 1024.0);

        /// <summary>
        /// Gets a value indicating whether memory usage is within target (<50 MB).
        /// </summary>
        public bool MemoryWithinTarget => MemoryUsageMB < TargetMemoryMB;

        #endregion

        #region CPU Metrics

        /// <summary>
        /// Gets the CPU usage percentage (0-100).
        /// </summary>
        public double CpuUsagePercent { get; init; }

        /// <summary>
        /// Gets a value indicating whether CPU usage is within target (<1%).
        /// </summary>
        public bool CpuWithinTarget => CpuUsagePercent < TargetCpuPercent;

        #endregion

        #region GC Metrics

        /// <summary>
        /// Gets the number of Generation 0 garbage collections.
        /// </summary>
        public int Gen0Collections { get; init; }

        /// <summary>
        /// Gets the number of Generation 1 garbage collections.
        /// </summary>
        public int Gen1Collections { get; init; }

        /// <summary>
        /// Gets the number of Generation 2 garbage collections.
        /// </summary>
        public int Gen2Collections { get; init; }

        /// <summary>
        /// Gets the total number of garbage collections across all generations.
        /// </summary>
        public int TotalGCCollections => Gen0Collections + Gen1Collections + Gen2Collections;

        #endregion

        #region Cache Metrics

        /// <summary>
        /// Gets the current number of icons in the cache.
        /// </summary>
        public int IconCacheSize { get; init; }

        /// <summary>
        /// Gets the maximum icon cache capacity.
        /// </summary>
        public int IconCacheMaxSize { get; init; }

        /// <summary>
        /// Gets the total number of icon cache hits.
        /// </summary>
        public long IconCacheHits { get; init; }

        /// <summary>
        /// Gets the total number of icon cache misses.
        /// </summary>
        public long IconCacheMisses { get; init; }

        /// <summary>
        /// Gets the icon cache hit rate (0.0 to 1.0).
        /// Returns 0.0 if no cache accesses have been made.
        /// </summary>
        public double IconCacheHitRate
        {
            get
            {
                var totalAccesses = IconCacheHits + IconCacheMisses;
                return totalAccesses > 0 ? (double)IconCacheHits / totalAccesses : 0.0;
            }
        }

        /// <summary>
        /// Gets a value indicating whether cache hit rate is within target (>80%).
        /// </summary>
        public bool CacheHitRateWithinTarget => IconCacheHitRate >= TargetCacheHitRate;

        #endregion

        #region Process Monitoring Metrics

        /// <summary>
        /// Gets the number of processes currently being tracked.
        /// </summary>
        public int TrackedProcessCount { get; init; }

        /// <summary>
        /// Gets the process refresh interval in milliseconds.
        /// </summary>
        public int ProcessRefreshIntervalMs { get; init; }

        #endregion

        #region Runtime Metrics

        /// <summary>
        /// Gets the application uptime.
        /// </summary>
        public TimeSpan Uptime { get; init; }

        /// <summary>
        /// Gets the timestamp when this metrics snapshot was taken.
        /// </summary>
        public DateTime Timestamp { get; init; } = DateTime.UtcNow;

        #endregion

        #region Aggregate Status

        /// <summary>
        /// Gets a value indicating whether all primary performance targets are met.
        /// </summary>
        public bool AllTargetsMet =>
            StartupTimeWithinTarget &&
            MemoryWithinTarget &&
            CpuWithinTarget;

        /// <summary>
        /// Gets a summary status string for the metrics.
        /// </summary>
        public string StatusSummary
        {
            get
            {
                var issues = new List<string>();

                if (!StartupTimeWithinTarget)
                    issues.Add($"Startup: {StartupTimeMs}ms (target: <{TargetStartupTimeMs}ms)");

                if (!MemoryWithinTarget)
                    issues.Add($"Memory: {MemoryUsageMB:F1}MB (target: <{TargetMemoryMB}MB)");

                if (!CpuWithinTarget)
                    issues.Add($"CPU: {CpuUsagePercent:F1}% (target: <{TargetCpuPercent}%)");

                return issues.Count == 0
                    ? "All targets met"
                    : string.Join("; ", issues);
            }
        }

        #endregion

        /// <summary>
        /// Returns a string representation of the performance metrics.
        /// </summary>
        public override string ToString()
        {
            return $"PerformanceMetrics[Startup={StartupTimeMs}ms, Memory={MemoryUsageMB:F1}MB, " +
                   $"CPU={CpuUsagePercent:F2}%, CacheHit={IconCacheHitRate:P0}, " +
                   $"Uptime={Uptime:hh\\:mm\\:ss}, AllTargetsMet={AllTargetsMet}]";
        }
    }
}
