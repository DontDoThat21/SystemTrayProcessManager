using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using SystemTrayProcessManager.Core.Models;
using SystemTrayProcessManager.Infrastructure.Services;

namespace SystemTrayProcessManager.Tests.Infrastructure
{
    /// <summary>
    /// Unit tests for <see cref="PerformanceMonitorService"/>.
    /// </summary>
    public class PerformanceMonitorServiceTests : IDisposable
    {
        private readonly Mock<ILogger<PerformanceMonitorService>> _mockLogger;
        private readonly PerformanceMonitorService _service;

        public PerformanceMonitorServiceTests()
        {
            _mockLogger = new Mock<ILogger<PerformanceMonitorService>>();
            _service = new PerformanceMonitorService(_mockLogger.Object);
        }

        public void Dispose()
        {
            _service.Dispose();
        }

        // Constructor tests

        [Fact]
        public void Constructor_WithNullLogger_ShouldThrow()
        {
            var act = () => new PerformanceMonitorService(null!);
            act.Should().Throw<ArgumentNullException>();
        }

        [Fact]
        public void Constructor_ShouldInitializeWithDefaultValues()
        {
            _service.StartupTimeMs.Should().Be(-1);
            _service.IsTimingStartup.Should().BeFalse();
            _service.IsMonitoring.Should().BeFalse();
            _service.IconCacheHits.Should().Be(0);
            _service.IconCacheMisses.Should().Be(0);
        }

        // Startup timing tests

        [Fact]
        public void StartStartupTimer_ShouldSetIsTimingStartup()
        {
            _service.StartStartupTimer();
            _service.IsTimingStartup.Should().BeTrue();
        }

        [Fact]
        public void StartStartupTimer_CalledTwice_ShouldNotReset()
        {
            _service.StartStartupTimer();
            Thread.Sleep(10);
            _service.StartStartupTimer();
            _service.RecordStartupComplete();

            // Should still have measured time from first start
            _service.StartupTimeMs.Should().BeGreaterOrEqualTo(10);
        }

        [Fact]
        public void RecordStartupComplete_ShouldClearIsTimingStartup()
        {
            _service.StartStartupTimer();
            _service.RecordStartupComplete();

            _service.IsTimingStartup.Should().BeFalse();
        }

        [Fact]
        public void RecordStartupComplete_ShouldSetStartupTimeMs()
        {
            _service.StartStartupTimer();
            Thread.Sleep(50);
            _service.RecordStartupComplete();

            _service.StartupTimeMs.Should().BeGreaterOrEqualTo(50);
        }

        [Fact]
        public void RecordStartupComplete_WithoutStart_ShouldNotSetTime()
        {
            _service.RecordStartupComplete();
            _service.StartupTimeMs.Should().Be(-1);
        }

        // Icon cache statistics tests

        [Fact]
        public void RecordIconCacheAccess_Hit_ShouldIncrementHits()
        {
            _service.RecordIconCacheAccess(true);
            _service.RecordIconCacheAccess(true);

            _service.IconCacheHits.Should().Be(2);
            _service.IconCacheMisses.Should().Be(0);
        }

        [Fact]
        public void RecordIconCacheAccess_Miss_ShouldIncrementMisses()
        {
            _service.RecordIconCacheAccess(false);
            _service.RecordIconCacheAccess(false);
            _service.RecordIconCacheAccess(false);

            _service.IconCacheHits.Should().Be(0);
            _service.IconCacheMisses.Should().Be(3);
        }

        [Fact]
        public void GetIconCacheHitRate_WithNoAccesses_ShouldReturnZero()
        {
            _service.GetIconCacheHitRate().Should().Be(0.0);
        }

        [Fact]
        public void GetIconCacheHitRate_WithAllHits_ShouldReturnOne()
        {
            for (int i = 0; i < 10; i++)
            {
                _service.RecordIconCacheAccess(true);
            }

            _service.GetIconCacheHitRate().Should().Be(1.0);
        }

        [Fact]
        public void GetIconCacheHitRate_WithMixedAccesses_ShouldCalculateCorrectly()
        {
            for (int i = 0; i < 8; i++)
            {
                _service.RecordIconCacheAccess(true);
            }
            for (int i = 0; i < 2; i++)
            {
                _service.RecordIconCacheAccess(false);
            }

            _service.GetIconCacheHitRate().Should().BeApproximately(0.8, 0.001);
        }

        // GetCurrentMetrics tests

        [Fact]
        public void GetCurrentMetrics_ShouldReturnNonNullMetrics()
        {
            var metrics = _service.GetCurrentMetrics();
            metrics.Should().NotBeNull();
        }

        [Fact]
        public void GetCurrentMetrics_ShouldHavePositiveMemoryValues()
        {
            var metrics = _service.GetCurrentMetrics();

            metrics.WorkingSetBytes.Should().BeGreaterThan(0);
            metrics.PrivateMemoryBytes.Should().BeGreaterThan(0);
            metrics.GCTotalMemoryBytes.Should().BeGreaterThan(0);
        }

        [Fact]
        public void GetCurrentMetrics_ShouldIncludeGCCollections()
        {
            // Force some GC collections
            GC.Collect(0);
            GC.Collect(1);

            var metrics = _service.GetCurrentMetrics();

            // Gen0 should have at least one collection
            metrics.Gen0Collections.Should().BeGreaterThan(0);
        }

        [Fact]
        public void GetCurrentMetrics_ShouldIncludeStartupTime()
        {
            _service.StartStartupTimer();
            Thread.Sleep(10);
            _service.RecordStartupComplete();

            var metrics = _service.GetCurrentMetrics();

            metrics.StartupTimeMs.Should().BeGreaterOrEqualTo(10);
        }

        [Fact]
        public void GetCurrentMetrics_ShouldIncludeIconCacheStats()
        {
            _service.RecordIconCacheAccess(true);
            _service.RecordIconCacheAccess(false);
            _service.UpdateIconCacheStats(50, 100);

            var metrics = _service.GetCurrentMetrics();

            metrics.IconCacheHits.Should().Be(1);
            metrics.IconCacheMisses.Should().Be(1);
            metrics.IconCacheSize.Should().Be(50);
            metrics.IconCacheMaxSize.Should().Be(100);
        }

        [Fact]
        public void GetCurrentMetrics_ShouldHaveRecentTimestamp()
        {
            var before = DateTime.UtcNow;
            var metrics = _service.GetCurrentMetrics();
            var after = DateTime.UtcNow;

            metrics.Timestamp.Should().BeOnOrAfter(before);
            metrics.Timestamp.Should().BeOnOrBefore(after);
        }

        [Fact]
        public void GetCurrentMetrics_ShouldIncludeUptime()
        {
            Thread.Sleep(50);
            var metrics = _service.GetCurrentMetrics();

            metrics.Uptime.TotalMilliseconds.Should().BeGreaterOrEqualTo(50);
        }

        // UpdateIconCacheStats tests

        [Fact]
        public void UpdateIconCacheStats_ShouldUpdateMetrics()
        {
            _service.UpdateIconCacheStats(75, 150);

            var metrics = _service.GetCurrentMetrics();

            metrics.IconCacheSize.Should().Be(75);
            metrics.IconCacheMaxSize.Should().Be(150);
        }

        // UpdateTrackedProcessCount tests

        [Fact]
        public void UpdateTrackedProcessCount_ShouldUpdateMetrics()
        {
            _service.UpdateTrackedProcessCount(42);

            var metrics = _service.GetCurrentMetrics();

            metrics.TrackedProcessCount.Should().Be(42);
        }

        // UpdateProcessRefreshInterval tests

        [Fact]
        public void UpdateProcessRefreshInterval_ShouldUpdateMetrics()
        {
            _service.UpdateProcessRefreshInterval(10000);

            var metrics = _service.GetCurrentMetrics();

            metrics.ProcessRefreshIntervalMs.Should().Be(10000);
        }

        // ForceGarbageCollection tests

        [Fact]
        public void ForceGarbageCollection_ShouldReturnNonNegativeValue()
        {
            var freedBytes = _service.ForceGarbageCollection();
            freedBytes.Should().BeGreaterOrEqualTo(0);
        }

        [Fact]
        public void ForceGarbageCollection_ShouldTriggerGC()
        {
            // Allocate some memory
            var data = new byte[1024 * 1024]; // 1 MB
            data = null;

            var beforeCollections = GC.CollectionCount(0);
            _service.ForceGarbageCollection();
            var afterCollections = GC.CollectionCount(0);

            afterCollections.Should().BeGreaterThan(beforeCollections);
        }

        // Monitoring tests

        [Fact]
        public void StartMonitoring_ShouldSetIsMonitoringTrue()
        {
            _service.StartMonitoring(TimeSpan.FromSeconds(1));
            _service.IsMonitoring.Should().BeTrue();
        }

        [Fact]
        public void StartMonitoring_CalledTwice_ShouldNotThrow()
        {
            _service.StartMonitoring(TimeSpan.FromSeconds(1));
            
            var act = () => _service.StartMonitoring(TimeSpan.FromSeconds(1));
            
            act.Should().NotThrow();
        }

        [Fact]
        public void StopMonitoring_ShouldSetIsMonitoringFalse()
        {
            _service.StartMonitoring(TimeSpan.FromSeconds(1));
            _service.StopMonitoring();

            _service.IsMonitoring.Should().BeFalse();
        }

        [Fact]
        public void StopMonitoring_WhenNotMonitoring_ShouldNotThrow()
        {
            var act = () => _service.StopMonitoring();
            act.Should().NotThrow();
        }

        [Fact]
        public void StartMonitoring_WithSmallInterval_ShouldUseMinimum()
        {
            // Should adjust to minimum 1 second
            _service.StartMonitoring(TimeSpan.FromMilliseconds(100));
            _service.IsMonitoring.Should().BeTrue();
        }

        [Fact]
        public async Task StartMonitoring_ShouldRaiseMetricsUpdatedEvent()
        {
            var eventRaised = new TaskCompletionSource<bool>();
            _service.MetricsUpdated += (_, _) => eventRaised.TrySetResult(true);

            // Minimum interval is 1 second, so we need to wait at least that long
            _service.StartMonitoring(TimeSpan.FromSeconds(1));

            var timeoutTask = Task.Delay(2000);
            var completedTask = await Task.WhenAny(eventRaised.Task, timeoutTask);

            completedTask.Should().Be(eventRaised.Task, "event should have been raised within timeout");
            var result = await eventRaised.Task;
            result.Should().BeTrue();
        }

        // Dispose tests

        [Fact]
        public void Dispose_ShouldStopMonitoring()
        {
            var service = new PerformanceMonitorService(_mockLogger.Object);
            service.StartMonitoring(TimeSpan.FromSeconds(1));
            
            service.Dispose();

            service.IsMonitoring.Should().BeFalse();
        }

        [Fact]
        public void Dispose_CalledMultipleTimes_ShouldNotThrow()
        {
            var service = new PerformanceMonitorService(_mockLogger.Object);
            
            var act = () =>
            {
                service.Dispose();
                service.Dispose();
                service.Dispose();
            };

            act.Should().NotThrow();
        }

        [Fact]
        public void GetCurrentMetrics_AfterDispose_ShouldReturnEmptyMetrics()
        {
            var service = new PerformanceMonitorService(_mockLogger.Object);
            service.Dispose();

            var metrics = service.GetCurrentMetrics();

            metrics.Should().NotBeNull();
        }

        [Fact]
        public void RecordIconCacheAccess_AfterDispose_ShouldNotThrow()
        {
            var service = new PerformanceMonitorService(_mockLogger.Object);
            service.Dispose();

            var act = () => service.RecordIconCacheAccess(true);

            act.Should().NotThrow();
        }
    }
}
