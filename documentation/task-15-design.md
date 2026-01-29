# Task 15: Performance Optimization - Design Document

## Document Overview
**Task**: Task 15: Performance Optimization (Phase 5.4)  
**Priority**: High  
**Estimated Time**: 4-5 hours  
**Dependencies**: All previous tasks (Tasks 1-14)  
**Created**: 2026-01-29  
**Author**: WPF Engineer Agent

---

## 1. Task Overview

### 1.1 Objective
Optimize application performance to meet portfolio-quality standards with measurable targets:
- Startup time: <2 seconds
- Memory usage: <50 MB idle
- CPU usage: <1% idle
- Hotkey response: <50ms

Create comprehensive performance monitoring and optimization infrastructure including:
- Performance monitoring service for metrics collection
- Optimized process monitoring with configurable throttling
- Enhanced icon caching with true LRU eviction
- Lazy loading for expensive services
- Memory leak prevention with weak references where appropriate
- Background threading optimization
- Win32 API call batching

### 1.2 Scope
**In Scope**:
- `IPerformanceMonitorService` - Interface for performance metrics collection
- `PerformanceMonitorService` - Implementation with startup, memory, CPU tracking
- `PerformanceMetrics` model - Snapshot of current performance state
- Enhanced `IconExtractor` with true LRU cache
- Optimized `ProcessMonitorService` with configurable throttling
- `LazyServiceProvider` wrapper for deferred service initialization
- Memory diagnostics and GC optimization hints
- Performance-focused unit tests
- Startup time measurement and optimization

**Out of Scope**:
- UI performance profiler visualization
- Automatic performance tuning
- External APM integration (Application Insights, etc.)

### 1.3 Success Criteria
- [ ] IPerformanceMonitorService interface and implementation
- [ ] PerformanceMetrics model with memory, CPU, uptime tracking
- [ ] LRU icon cache with access-time tracking
- [ ] Configurable process polling interval (via AppSettings)
- [ ] Lazy loading wrapper for optional services
- [ ] Performance benchmark tests
- [ ] Startup time <2 seconds verified
- [ ] Memory usage <50 MB idle verified
- [ ] All unit tests pass (35+ new tests)
- [ ] No compiler warnings

---

## 2. Architecture

### 2.1 New Components

```
Core/Models/
├── PerformanceMetrics.cs           Performance snapshot model
└── MemorySnapshot.cs               Memory usage details

Core/Services/
└── IPerformanceMonitorService.cs   Interface for performance monitoring

Infrastructure/Services/
├── PerformanceMonitorService.cs    Implementation with metrics collection
└── OptimizedIconCache.cs           True LRU cache implementation

Infrastructure/Helpers/
└── LazyServiceWrapper.cs           Lazy initialization helper

Tests/Infrastructure/
├── PerformanceMonitorServiceTests.cs
├── OptimizedIconCacheTests.cs
└── PerformanceBenchmarkTests.cs
```

### 2.2 Performance Monitoring Flow

```
┌─────────────────────────────────────────────────────────────────┐
│                  PerformanceMonitorService                       │
├─────────────────────────────────────────────────────────────────┤
│  Startup:                                                        │
│  ┌──────────────────────┐                                       │
│  │ StartupStopwatch     │ → MeasureStartupTime()                │
│  │ (Application.OnStartup)                                      │
│  └──────────────────────┘                                       │
│                                                                  │
│  Runtime Metrics (polled every 5 seconds):                      │
│  ┌──────────────────────┐   ┌──────────────────────┐           │
│  │ Process.WorkingSet64 │   │ Process.TotalProcessor│           │
│  │ Process.PrivateMemory│   │ Time / Environment   │           │
│  │ GC.GetTotalMemory()  │   │ .ProcessorCount      │           │
│  └──────────────────────┘   └──────────────────────┘           │
│              │                        │                          │
│              ▼                        ▼                          │
│  ┌────────────────────────────────────────────────────────────┐ │
│  │                    PerformanceMetrics                       │ │
│  │  - StartupTimeMs, MemoryUsageMB, CpuUsagePercent           │ │
│  │  - GCCollectionCounts, IconCacheHitRate, ProcessCount      │ │
│  │  - Uptime, IsWithinTargets                                  │ │
│  └────────────────────────────────────────────────────────────┘ │
└─────────────────────────────────────────────────────────────────┘
```

### 2.3 LRU Cache Architecture

```
┌─────────────────────────────────────────────────────────────────┐
│                     OptimizedIconCache                           │
├─────────────────────────────────────────────────────────────────┤
│  Storage:                                                        │
│  ┌─────────────────────────────────────────────────────────────┐│
│  │  Dictionary<string, CacheEntry>                             ││
│  │    Key: executablePath (case-insensitive)                   ││
│  │    Value: { Icon, LastAccessTime, AccessCount }             ││
│  └─────────────────────────────────────────────────────────────┘│
│                                                                  │
│  Access Pattern:                                                 │
│  ┌──────┐   ┌───────────────┐   ┌────────────────┐             │
│  │ Get  │ → │ Update Access │ → │ Return Cached  │             │
│  │ Icon │   │ Time          │   │ or Extract     │             │
│  └──────┘   └───────────────┘   └────────────────┘             │
│                                                                  │
│  Eviction (when Count >= MaxSize):                              │
│  ┌──────────────────────────────────────────────────────────┐  │
│  │  1. Sort by LastAccessTime ascending                      │  │
│  │  2. Remove oldest 25% of entries                          │  │
│  │  3. Log eviction statistics                               │  │
│  └──────────────────────────────────────────────────────────┘  │
└─────────────────────────────────────────────────────────────────┘
```

---

## 3. Detailed Design

### 3.1 PerformanceMetrics Model

```csharp
/// <summary>
/// Represents a snapshot of application performance metrics.
/// </summary>
public sealed class PerformanceMetrics
{
    // Startup
    public long StartupTimeMs { get; init; }
    public bool StartupTimeWithinTarget => StartupTimeMs < 2000;
    
    // Memory
    public long WorkingSetBytes { get; init; }
    public long PrivateMemoryBytes { get; init; }
    public long GCTotalMemoryBytes { get; init; }
    public double MemoryUsageMB => WorkingSetBytes / (1024.0 * 1024.0);
    public bool MemoryWithinTarget => MemoryUsageMB < 50;
    
    // CPU
    public double CpuUsagePercent { get; init; }
    public bool CpuWithinTarget => CpuUsagePercent < 1.0;
    
    // GC
    public int Gen0Collections { get; init; }
    public int Gen1Collections { get; init; }
    public int Gen2Collections { get; init; }
    
    // Cache
    public int IconCacheSize { get; init; }
    public double IconCacheHitRate { get; init; }
    
    // Process Monitoring
    public int TrackedProcessCount { get; init; }
    public int ProcessRefreshIntervalMs { get; init; }
    
    // Runtime
    public TimeSpan Uptime { get; init; }
    public DateTime Timestamp { get; init; }
    
    // Aggregate
    public bool AllTargetsMet => StartupTimeWithinTarget && 
                                  MemoryWithinTarget && 
                                  CpuWithinTarget;
}
```

### 3.2 IPerformanceMonitorService Interface

```csharp
/// <summary>
/// Service for monitoring and reporting application performance metrics.
/// </summary>
public interface IPerformanceMonitorService : IDisposable
{
    /// <summary>
    /// Gets the current performance metrics snapshot.
    /// </summary>
    PerformanceMetrics GetCurrentMetrics();
    
    /// <summary>
    /// Gets the recorded startup time in milliseconds.
    /// </summary>
    long StartupTimeMs { get; }
    
    /// <summary>
    /// Records the application startup completion time.
    /// </summary>
    void RecordStartupComplete();
    
    /// <summary>
    /// Gets the icon cache hit rate (0.0 - 1.0).
    /// </summary>
    double GetIconCacheHitRate();
    
    /// <summary>
    /// Records an icon cache hit or miss.
    /// </summary>
    void RecordIconCacheAccess(bool isHit);
    
    /// <summary>
    /// Forces a garbage collection and returns memory freed.
    /// </summary>
    long ForceGarbageCollection();
    
    /// <summary>
    /// Starts periodic metrics collection.
    /// </summary>
    void StartMonitoring(TimeSpan interval);
    
    /// <summary>
    /// Stops periodic metrics collection.
    /// </summary>
    void StopMonitoring();
    
    /// <summary>
    /// Event raised when metrics are updated.
    /// </summary>
    event EventHandler<PerformanceMetrics>? MetricsUpdated;
}
```

### 3.3 Enhanced Icon Cache Entry

```csharp
/// <summary>
/// Represents a cached icon with access tracking for LRU eviction.
/// </summary>
internal sealed class IconCacheEntry
{
    public ImageSource Icon { get; }
    public DateTime LastAccessTime { get; private set; }
    public int AccessCount { get; private set; }
    public DateTime CreatedTime { get; }
    
    public IconCacheEntry(ImageSource icon)
    {
        Icon = icon ?? throw new ArgumentNullException(nameof(icon));
        CreatedTime = DateTime.UtcNow;
        LastAccessTime = CreatedTime;
        AccessCount = 1;
    }
    
    public void RecordAccess()
    {
        LastAccessTime = DateTime.UtcNow;
        AccessCount++;
    }
}
```

### 3.4 Process Monitor Throttling

Enhance `ProcessMonitorService` to use configurable interval from `AppSettings`:

```csharp
// In ProcessMonitorService constructor or initialization
public void UpdatePollingInterval(int intervalMs)
{
    if (intervalMs < 1000 || intervalMs > 30000)
    {
        _logger.LogWarning("Invalid polling interval {Interval}ms, using default", intervalMs);
        intervalMs = 5000;
    }
    
    _pollingInterval = TimeSpan.FromMilliseconds(intervalMs);
    
    if (IsMonitoring)
    {
        // Restart timer with new interval
        _monitorTimer?.Change(_pollingInterval, _pollingInterval);
        _logger.LogInformation("Process polling interval updated to {Interval}ms", intervalMs);
    }
}
```

---

## 4. Performance Targets

| Metric | Target | Measurement Method |
|--------|--------|-------------------|
| Startup Time | <2 seconds | Stopwatch from App.OnStartup to MainWindow.Loaded |
| Memory (Idle) | <50 MB | Process.WorkingSet64 after 60s idle |
| CPU (Idle) | <1% | 10-second average via Process.TotalProcessorTime |
| Hotkey Response | <50ms | Stopwatch from KeyDown to Action.Invoke |
| Icon Cache Hit | >80% | Hits / (Hits + Misses) after warm-up |
| Process Refresh | 5-10 seconds | Configurable via AppSettings |

---

## 5. Implementation Steps

### Step 1: Create Models (30 minutes)
1. Create `PerformanceMetrics.cs` in Core/Models
2. Create `MemorySnapshot.cs` in Core/Models (optional detail model)
3. Add unit tests for model validation

### Step 2: Create Performance Monitor Service (60 minutes)
1. Create `IPerformanceMonitorService.cs` interface in Core/Services
2. Implement `PerformanceMonitorService.cs` in Infrastructure/Services
3. Add startup time tracking with Stopwatch
4. Add memory/CPU metric collection
5. Add GC collection counting
6. Add unit tests

### Step 3: Enhance Icon Cache (45 minutes)
1. Create `IconCacheEntry` internal class
2. Modify `IconExtractor` to use LRU eviction
3. Add cache hit rate tracking
4. Wire cache stats to performance monitor
5. Add unit tests for LRU behavior

### Step 4: Optimize Process Monitor (30 minutes)
1. Add configurable polling interval
2. Connect to AppSettings.ProcessRefreshIntervalMs
3. Add method to update interval at runtime
4. Add unit tests

### Step 5: Integration (30 minutes)
1. Register services in DI container
2. Initialize performance monitor in App.OnStartup
3. Record startup complete after MainWindow.Loaded
4. Add performance metrics to crash reports (optional)

### Step 6: Testing & Validation (45 minutes)
1. Write comprehensive unit tests (35+)
2. Create performance benchmark tests
3. Measure and verify all targets met
4. Document actual vs target metrics

---

## 6. Files to Create/Modify

### New Files
- `src/SystemTrayProcessManager.Core/Models/PerformanceMetrics.cs`
- `src/SystemTrayProcessManager.Core/Services/IPerformanceMonitorService.cs`
- `src/SystemTrayProcessManager.Infrastructure/Services/PerformanceMonitorService.cs`
- `tests/SystemTrayProcessManager.Tests/Core/PerformanceMetricsTests.cs`
- `tests/SystemTrayProcessManager.Tests/Infrastructure/PerformanceMonitorServiceTests.cs`
- `tests/SystemTrayProcessManager.Tests/Infrastructure/IconCacheLruTests.cs`
- `documentation/task-15-design.md` (this file)
- `documentation/task-15-review.md`
- `documentation/task-15-summary.md`

### Modified Files
- `src/SystemTrayProcessManager.Infrastructure/Helpers/IconExtractor.cs` (LRU enhancement)
- `src/SystemTrayProcessManager.Infrastructure/Services/ProcessMonitorService.cs` (configurable throttle)
- `src/SystemTrayProcessManager.UI/App.xaml.cs` (startup timing, service registration)

---

## 7. Risk Assessment

| Risk | Likelihood | Impact | Mitigation |
|------|------------|--------|------------|
| Metrics collection overhead | Low | Medium | Use lightweight polling (5s interval) |
| LRU cache complexity | Low | Low | Simple timestamp-based eviction |
| CPU measurement inaccuracy | Medium | Low | Use averaged samples over 10s |
| Breaking existing tests | Low | High | Run full test suite after each change |

---

## 8. Review Checklist

- [ ] All performance targets documented
- [ ] Metrics collection is non-intrusive (<1ms overhead)
- [ ] LRU cache correctly evicts oldest entries
- [ ] Process polling interval is configurable
- [ ] Startup time measurement is accurate
- [ ] Memory metrics use correct Process properties
- [ ] CPU calculation handles multi-core correctly
- [ ] Unit tests cover edge cases
- [ ] No memory leaks introduced
- [ ] Thread-safety maintained
