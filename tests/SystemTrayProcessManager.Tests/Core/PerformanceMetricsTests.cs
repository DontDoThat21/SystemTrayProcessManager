using FluentAssertions;
using SystemTrayProcessManager.Core.Models;

namespace SystemTrayProcessManager.Tests.Core
{
    /// <summary>
    /// Unit tests for <see cref="PerformanceMetrics"/>.
    /// </summary>
    public class PerformanceMetricsTests
    {
        // Constructor / Default values tests

        [Fact]
        public void Constructor_Default_ShouldHaveCorrectDefaultValues()
        {
            var metrics = new PerformanceMetrics();

            metrics.StartupTimeMs.Should().Be(0);
            metrics.WorkingSetBytes.Should().Be(0);
            metrics.PrivateMemoryBytes.Should().Be(0);
            metrics.GCTotalMemoryBytes.Should().Be(0);
            metrics.CpuUsagePercent.Should().Be(0);
            metrics.Gen0Collections.Should().Be(0);
            metrics.Gen1Collections.Should().Be(0);
            metrics.Gen2Collections.Should().Be(0);
            metrics.IconCacheSize.Should().Be(0);
            metrics.IconCacheMaxSize.Should().Be(0);
            metrics.IconCacheHits.Should().Be(0);
            metrics.IconCacheMisses.Should().Be(0);
            metrics.TrackedProcessCount.Should().Be(0);
            metrics.ProcessRefreshIntervalMs.Should().Be(0);
            metrics.Uptime.Should().Be(TimeSpan.Zero);
        }

        // Target constants tests

        [Fact]
        public void TargetConstants_ShouldHaveExpectedValues()
        {
            PerformanceMetrics.TargetStartupTimeMs.Should().Be(2000);
            PerformanceMetrics.TargetMemoryMB.Should().Be(50.0);
            PerformanceMetrics.TargetCpuPercent.Should().Be(1.0);
            PerformanceMetrics.TargetCacheHitRate.Should().Be(0.80);
        }

        // Startup time target tests

        [Fact]
        public void StartupTimeWithinTarget_WhenBelowTarget_ShouldReturnTrue()
        {
            var metrics = new PerformanceMetrics { StartupTimeMs = 1500 };
            metrics.StartupTimeWithinTarget.Should().BeTrue();
        }

        [Fact]
        public void StartupTimeWithinTarget_WhenAtTarget_ShouldReturnFalse()
        {
            var metrics = new PerformanceMetrics { StartupTimeMs = 2000 };
            metrics.StartupTimeWithinTarget.Should().BeFalse();
        }

        [Fact]
        public void StartupTimeWithinTarget_WhenAboveTarget_ShouldReturnFalse()
        {
            var metrics = new PerformanceMetrics { StartupTimeMs = 3000 };
            metrics.StartupTimeWithinTarget.Should().BeFalse();
        }

        // Memory calculation tests

        [Fact]
        public void MemoryUsageMB_ShouldConvertBytesToMegabytes()
        {
            var metrics = new PerformanceMetrics { WorkingSetBytes = 52428800 }; // 50 MB
            metrics.MemoryUsageMB.Should().Be(50.0);
        }

        [Fact]
        public void PrivateMemoryMB_ShouldConvertBytesToMegabytes()
        {
            var metrics = new PerformanceMetrics { PrivateMemoryBytes = 26214400 }; // 25 MB
            metrics.PrivateMemoryMB.Should().Be(25.0);
        }

        [Fact]
        public void GCMemoryMB_ShouldConvertBytesToMegabytes()
        {
            var metrics = new PerformanceMetrics { GCTotalMemoryBytes = 10485760 }; // 10 MB
            metrics.GCMemoryMB.Should().Be(10.0);
        }

        // Memory target tests

        [Fact]
        public void MemoryWithinTarget_WhenBelowTarget_ShouldReturnTrue()
        {
            var metrics = new PerformanceMetrics { WorkingSetBytes = 41943040 }; // 40 MB
            metrics.MemoryWithinTarget.Should().BeTrue();
        }

        [Fact]
        public void MemoryWithinTarget_WhenAtTarget_ShouldReturnFalse()
        {
            var metrics = new PerformanceMetrics { WorkingSetBytes = 52428800 }; // 50 MB
            metrics.MemoryWithinTarget.Should().BeFalse();
        }

        [Fact]
        public void MemoryWithinTarget_WhenAboveTarget_ShouldReturnFalse()
        {
            var metrics = new PerformanceMetrics { WorkingSetBytes = 62914560 }; // 60 MB
            metrics.MemoryWithinTarget.Should().BeFalse();
        }

        // CPU target tests

        [Fact]
        public void CpuWithinTarget_WhenBelowTarget_ShouldReturnTrue()
        {
            var metrics = new PerformanceMetrics { CpuUsagePercent = 0.5 };
            metrics.CpuWithinTarget.Should().BeTrue();
        }

        [Fact]
        public void CpuWithinTarget_WhenAtTarget_ShouldReturnFalse()
        {
            var metrics = new PerformanceMetrics { CpuUsagePercent = 1.0 };
            metrics.CpuWithinTarget.Should().BeFalse();
        }

        [Fact]
        public void CpuWithinTarget_WhenAboveTarget_ShouldReturnFalse()
        {
            var metrics = new PerformanceMetrics { CpuUsagePercent = 2.5 };
            metrics.CpuWithinTarget.Should().BeFalse();
        }

        // GC collections tests

        [Fact]
        public void TotalGCCollections_ShouldSumAllGenerations()
        {
            var metrics = new PerformanceMetrics
            {
                Gen0Collections = 100,
                Gen1Collections = 20,
                Gen2Collections = 5
            };

            metrics.TotalGCCollections.Should().Be(125);
        }

        // Cache hit rate tests

        [Fact]
        public void IconCacheHitRate_WithNoAccesses_ShouldReturnZero()
        {
            var metrics = new PerformanceMetrics
            {
                IconCacheHits = 0,
                IconCacheMisses = 0
            };

            metrics.IconCacheHitRate.Should().Be(0.0);
        }

        [Fact]
        public void IconCacheHitRate_WithAllHits_ShouldReturnOne()
        {
            var metrics = new PerformanceMetrics
            {
                IconCacheHits = 100,
                IconCacheMisses = 0
            };

            metrics.IconCacheHitRate.Should().Be(1.0);
        }

        [Fact]
        public void IconCacheHitRate_WithAllMisses_ShouldReturnZero()
        {
            var metrics = new PerformanceMetrics
            {
                IconCacheHits = 0,
                IconCacheMisses = 100
            };

            metrics.IconCacheHitRate.Should().Be(0.0);
        }

        [Fact]
        public void IconCacheHitRate_WithMixedAccesses_ShouldCalculateCorrectly()
        {
            var metrics = new PerformanceMetrics
            {
                IconCacheHits = 80,
                IconCacheMisses = 20
            };

            metrics.IconCacheHitRate.Should().Be(0.8);
        }

        // Cache hit rate target tests

        [Fact]
        public void CacheHitRateWithinTarget_WhenAtTarget_ShouldReturnTrue()
        {
            var metrics = new PerformanceMetrics
            {
                IconCacheHits = 80,
                IconCacheMisses = 20
            };

            metrics.CacheHitRateWithinTarget.Should().BeTrue();
        }

        [Fact]
        public void CacheHitRateWithinTarget_WhenAboveTarget_ShouldReturnTrue()
        {
            var metrics = new PerformanceMetrics
            {
                IconCacheHits = 90,
                IconCacheMisses = 10
            };

            metrics.CacheHitRateWithinTarget.Should().BeTrue();
        }

        [Fact]
        public void CacheHitRateWithinTarget_WhenBelowTarget_ShouldReturnFalse()
        {
            var metrics = new PerformanceMetrics
            {
                IconCacheHits = 70,
                IconCacheMisses = 30
            };

            metrics.CacheHitRateWithinTarget.Should().BeFalse();
        }

        // AllTargetsMet tests

        [Fact]
        public void AllTargetsMet_WhenAllWithinTarget_ShouldReturnTrue()
        {
            var metrics = new PerformanceMetrics
            {
                StartupTimeMs = 1000,
                WorkingSetBytes = 31457280, // 30 MB
                CpuUsagePercent = 0.5
            };

            metrics.AllTargetsMet.Should().BeTrue();
        }

        [Fact]
        public void AllTargetsMet_WhenStartupExceeds_ShouldReturnFalse()
        {
            var metrics = new PerformanceMetrics
            {
                StartupTimeMs = 3000, // Exceeds
                WorkingSetBytes = 31457280,
                CpuUsagePercent = 0.5
            };

            metrics.AllTargetsMet.Should().BeFalse();
        }

        [Fact]
        public void AllTargetsMet_WhenMemoryExceeds_ShouldReturnFalse()
        {
            var metrics = new PerformanceMetrics
            {
                StartupTimeMs = 1000,
                WorkingSetBytes = 62914560, // 60 MB - Exceeds
                CpuUsagePercent = 0.5
            };

            metrics.AllTargetsMet.Should().BeFalse();
        }

        [Fact]
        public void AllTargetsMet_WhenCpuExceeds_ShouldReturnFalse()
        {
            var metrics = new PerformanceMetrics
            {
                StartupTimeMs = 1000,
                WorkingSetBytes = 31457280,
                CpuUsagePercent = 2.0 // Exceeds
            };

            metrics.AllTargetsMet.Should().BeFalse();
        }

        // StatusSummary tests

        [Fact]
        public void StatusSummary_WhenAllTargetsMet_ShouldReturnAllTargetsMet()
        {
            var metrics = new PerformanceMetrics
            {
                StartupTimeMs = 1000,
                WorkingSetBytes = 31457280,
                CpuUsagePercent = 0.5
            };

            metrics.StatusSummary.Should().Be("All targets met");
        }

        [Fact]
        public void StatusSummary_WhenTargetsExceeded_ShouldListIssues()
        {
            var metrics = new PerformanceMetrics
            {
                StartupTimeMs = 3000,
                WorkingSetBytes = 31457280,
                CpuUsagePercent = 0.5
            };

            metrics.StatusSummary.Should().Contain("Startup: 3000ms");
            metrics.StatusSummary.Should().Contain("target: <2000ms");
        }

        [Fact]
        public void StatusSummary_WithMultipleIssues_ShouldListAll()
        {
            var metrics = new PerformanceMetrics
            {
                StartupTimeMs = 3000,
                WorkingSetBytes = 62914560, // 60 MB
                CpuUsagePercent = 2.0
            };

            metrics.StatusSummary.Should().Contain("Startup");
            metrics.StatusSummary.Should().Contain("Memory");
            metrics.StatusSummary.Should().Contain("CPU");
        }

        // ToString tests

        [Fact]
        public void ToString_ShouldContainKeyMetrics()
        {
            var metrics = new PerformanceMetrics
            {
                StartupTimeMs = 1500,
                WorkingSetBytes = 41943040, // 40 MB
                CpuUsagePercent = 0.5,
                IconCacheHits = 80,
                IconCacheMisses = 20,
                Uptime = TimeSpan.FromMinutes(5)
            };

            var result = metrics.ToString();

            result.Should().Contain("Startup=1500ms");
            result.Should().Contain("Memory=40.0MB");
            result.Should().Contain("CPU=0.50%");
            result.Should().Contain("CacheHit=80");
            result.Should().Contain("AllTargetsMet=True");
        }

        // Timestamp tests

        [Fact]
        public void Timestamp_Default_ShouldBeRecentUtcNow()
        {
            var before = DateTime.UtcNow;
            var metrics = new PerformanceMetrics();
            var after = DateTime.UtcNow;

            metrics.Timestamp.Should().BeOnOrAfter(before);
            metrics.Timestamp.Should().BeOnOrBefore(after);
        }

        [Fact]
        public void Timestamp_CanBeSetViaInitializer()
        {
            var specificTime = new DateTime(2026, 1, 29, 12, 0, 0, DateTimeKind.Utc);
            var metrics = new PerformanceMetrics { Timestamp = specificTime };

            metrics.Timestamp.Should().Be(specificTime);
        }
    }
}
