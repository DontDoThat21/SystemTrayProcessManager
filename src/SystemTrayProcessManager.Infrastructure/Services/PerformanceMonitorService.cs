using Microsoft.Extensions.Logging;
using System.Diagnostics;
using SystemTrayProcessManager.Core.Models;
using SystemTrayProcessManager.Core.Services;
using Timer = System.Threading.Timer;

namespace SystemTrayProcessManager.Infrastructure.Services
{
    /// <summary>
    /// Service for monitoring and reporting application performance metrics.
    /// Tracks startup time, memory usage, CPU usage, and cache statistics.
    /// </summary>
    public sealed class PerformanceMonitorService : IPerformanceMonitorService
    {
        private readonly ILogger<PerformanceMonitorService> _logger;
        private readonly Stopwatch _startupStopwatch;
        private readonly Stopwatch _uptimeStopwatch;
        private readonly object _lockObject = new();

        private Timer? _monitoringTimer;
        private bool _disposed;

        // Startup timing
        private long _startupTimeMs = -1;
        private bool _isTimingStartup;

        // CPU tracking
        private TimeSpan _lastCpuTime;
        private DateTime _lastCpuCheck;
        private double _lastCpuPercent;

        // Cache statistics
        private long _iconCacheHits;
        private long _iconCacheMisses;
        private int _iconCacheSize;
        private int _iconCacheMaxSize = 100;

        // Process monitoring stats
        private int _trackedProcessCount;
        private int _processRefreshIntervalMs = 5000;

        /// <inheritdoc/>
        public long StartupTimeMs => _startupTimeMs;

        /// <inheritdoc/>
        public bool IsTimingStartup => _isTimingStartup;

        /// <inheritdoc/>
        public bool IsMonitoring { get; private set; }

        /// <inheritdoc/>
        public long IconCacheHits => Interlocked.Read(ref _iconCacheHits);

        /// <inheritdoc/>
        public long IconCacheMisses => Interlocked.Read(ref _iconCacheMisses);

        /// <inheritdoc/>
        public event EventHandler<PerformanceMetrics>? MetricsUpdated;

        /// <summary>
        /// Initializes a new instance of the <see cref="PerformanceMonitorService"/> class.
        /// </summary>
        /// <param name="logger">Logger instance for diagnostic output.</param>
        /// <exception cref="ArgumentNullException">Thrown when logger is null.</exception>
        public PerformanceMonitorService(ILogger<PerformanceMonitorService> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _startupStopwatch = new Stopwatch();
            _uptimeStopwatch = Stopwatch.StartNew();
            _lastCpuCheck = DateTime.UtcNow;
            _lastCpuTime = Process.GetCurrentProcess().TotalProcessorTime;

            _logger.LogDebug("PerformanceMonitorService initialized");
        }

        /// <inheritdoc/>
        public void StartStartupTimer()
        {
            if (_disposed)
            {
                return;
            }

            lock (_lockObject)
            {
                if (_isTimingStartup)
                {
                    _logger.LogWarning("Startup timer already running");
                    return;
                }

                _startupStopwatch.Restart();
                _isTimingStartup = true;
                _logger.LogDebug("Startup timer started");
            }
        }

        /// <inheritdoc/>
        public void RecordStartupComplete()
        {
            if (_disposed)
            {
                return;
            }

            lock (_lockObject)
            {
                if (!_isTimingStartup)
                {
                    _logger.LogWarning("Startup timer was not running");
                    return;
                }

                _startupStopwatch.Stop();
                _startupTimeMs = _startupStopwatch.ElapsedMilliseconds;
                _isTimingStartup = false;

                var withinTarget = _startupTimeMs < PerformanceMetrics.TargetStartupTimeMs;
                _logger.LogInformation(
                    "Application startup completed in {StartupTime}ms (target: <{Target}ms, {Status})",
                    _startupTimeMs,
                    PerformanceMetrics.TargetStartupTimeMs,
                    withinTarget ? "PASSED" : "EXCEEDED");
            }
        }

        /// <inheritdoc/>
        public PerformanceMetrics GetCurrentMetrics()
        {
            if (_disposed)
            {
                return new PerformanceMetrics();
            }

            try
            {
                using var process = Process.GetCurrentProcess();

                // Calculate CPU usage
                var cpuPercent = CalculateCpuUsage(process);

                return new PerformanceMetrics
                {
                    // Startup
                    StartupTimeMs = _startupTimeMs >= 0 ? _startupTimeMs : 0,

                    // Memory
                    WorkingSetBytes = process.WorkingSet64,
                    PrivateMemoryBytes = process.PrivateMemorySize64,
                    GCTotalMemoryBytes = GC.GetTotalMemory(false),

                    // CPU
                    CpuUsagePercent = cpuPercent,

                    // GC
                    Gen0Collections = GC.CollectionCount(0),
                    Gen1Collections = GC.CollectionCount(1),
                    Gen2Collections = GC.CollectionCount(2),

                    // Cache
                    IconCacheSize = _iconCacheSize,
                    IconCacheMaxSize = _iconCacheMaxSize,
                    IconCacheHits = IconCacheHits,
                    IconCacheMisses = IconCacheMisses,

                    // Process monitoring
                    TrackedProcessCount = _trackedProcessCount,
                    ProcessRefreshIntervalMs = _processRefreshIntervalMs,

                    // Runtime
                    Uptime = _uptimeStopwatch.Elapsed,
                    Timestamp = DateTime.UtcNow
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error collecting performance metrics");
                return new PerformanceMetrics { Timestamp = DateTime.UtcNow };
            }
        }

        /// <inheritdoc/>
        public double GetIconCacheHitRate()
        {
            var hits = IconCacheHits;
            var misses = IconCacheMisses;
            var total = hits + misses;

            return total > 0 ? (double)hits / total : 0.0;
        }

        /// <inheritdoc/>
        public void RecordIconCacheAccess(bool isHit)
        {
            if (_disposed)
            {
                return;
            }

            if (isHit)
            {
                Interlocked.Increment(ref _iconCacheHits);
            }
            else
            {
                Interlocked.Increment(ref _iconCacheMisses);
            }
        }

        /// <inheritdoc/>
        public long ForceGarbageCollection()
        {
            if (_disposed)
            {
                return 0;
            }

            try
            {
                var beforeMemory = GC.GetTotalMemory(false);

                _logger.LogDebug("Forcing garbage collection...");

                GC.Collect();
                GC.WaitForPendingFinalizers();
                GC.Collect();

                var afterMemory = GC.GetTotalMemory(true);
                var freedBytes = beforeMemory - afterMemory;

                _logger.LogInformation(
                    "Garbage collection completed. Memory freed: {FreedMB:F2}MB (before: {BeforeMB:F2}MB, after: {AfterMB:F2}MB)",
                    freedBytes / (1024.0 * 1024.0),
                    beforeMemory / (1024.0 * 1024.0),
                    afterMemory / (1024.0 * 1024.0));

                return freedBytes > 0 ? freedBytes : 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during forced garbage collection");
                return 0;
            }
        }

        /// <inheritdoc/>
        public void StartMonitoring(TimeSpan interval)
        {
            if (_disposed)
            {
                return;
            }

            lock (_lockObject)
            {
                if (IsMonitoring)
                {
                    _logger.LogWarning("Performance monitoring already active");
                    return;
                }

                if (interval < TimeSpan.FromSeconds(1))
                {
                    interval = TimeSpan.FromSeconds(1);
                    _logger.LogWarning("Monitoring interval adjusted to minimum of 1 second");
                }

                _monitoringTimer = new Timer(
                    MonitoringCallback,
                    null,
                    interval,
                    interval);

                IsMonitoring = true;
                _logger.LogInformation("Performance monitoring started with {Interval}s interval", interval.TotalSeconds);
            }
        }

        /// <inheritdoc/>
        public void StopMonitoring()
        {
            if (_disposed)
            {
                return;
            }

            lock (_lockObject)
            {
                if (!IsMonitoring)
                {
                    return;
                }

                _monitoringTimer?.Dispose();
                _monitoringTimer = null;
                IsMonitoring = false;

                _logger.LogInformation("Performance monitoring stopped");
            }
        }

        /// <inheritdoc/>
        public void UpdateIconCacheStats(int currentSize, int maxSize)
        {
            _iconCacheSize = currentSize;
            _iconCacheMaxSize = maxSize;
        }

        /// <inheritdoc/>
        public void UpdateTrackedProcessCount(int count)
        {
            _trackedProcessCount = count;
        }

        /// <inheritdoc/>
        public void UpdateProcessRefreshInterval(int intervalMs)
        {
            _processRefreshIntervalMs = intervalMs;
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _logger.LogDebug("Disposing PerformanceMonitorService...");

            StopMonitoring();
            _startupStopwatch.Stop();
            _uptimeStopwatch.Stop();

            _disposed = true;
            _logger.LogDebug("PerformanceMonitorService disposed");
        }

        /// <summary>
        /// Timer callback for periodic metrics collection.
        /// </summary>
        private void MonitoringCallback(object? state)
        {
            if (_disposed || !IsMonitoring)
            {
                return;
            }

            try
            {
                var metrics = GetCurrentMetrics();
                MetricsUpdated?.Invoke(this, metrics);

                // Log if any targets are exceeded
                if (!metrics.AllTargetsMet)
                {
                    _logger.LogWarning("Performance targets exceeded: {Status}", metrics.StatusSummary);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in performance monitoring callback");
            }
        }

        /// <summary>
        /// Calculates the current CPU usage percentage.
        /// </summary>
        private double CalculateCpuUsage(Process process)
        {
            try
            {
                var currentCpuTime = process.TotalProcessorTime;
                var currentTime = DateTime.UtcNow;

                var cpuTimeDelta = currentCpuTime - _lastCpuTime;
                var timeDelta = currentTime - _lastCpuCheck;

                if (timeDelta.TotalMilliseconds > 0)
                {
                    // CPU usage = (CPU time used / elapsed time) / processor count * 100
                    _lastCpuPercent = (cpuTimeDelta.TotalMilliseconds / timeDelta.TotalMilliseconds)
                                      / Environment.ProcessorCount * 100.0;

                    // Clamp to reasonable range
                    _lastCpuPercent = Math.Max(0, Math.Min(100, _lastCpuPercent));
                }

                _lastCpuTime = currentCpuTime;
                _lastCpuCheck = currentTime;

                return _lastCpuPercent;
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Error calculating CPU usage");
                return _lastCpuPercent;
            }
        }
    }
}
