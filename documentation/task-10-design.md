# Task 10: Smart Features - Design Document

## Overview
**Task**: 4.2 Smart Features  
**Priority**: Medium  
**Estimated Time**: 4-5 hours  
**Dependencies**: Task 4 (Window Manipulation), Task 5 (Audio Control), Task 9 (Profiles)  
**Status**: 🔄 In Progress

## Objective
Implement intelligent automation features that enhance user productivity by tracking focus history, detecting fullscreen applications, managing window positions across monitor configurations, providing a gaming mode, adjusting process priorities, and enabling automatic startup with Windows.

## Feature Requirements

### 1. Focus History
- Track recently focused processes in a circular buffer
- Support configurable history size (default: 10)
- Provide "Switch to last N processes" functionality
- Expose API for quick window switching via hotkeys

### 2. Auto-Mute on Fullscreen Detection
- Detect fullscreen windows (games, media players)
- Auto-mute configurable background apps (Discord, Slack, etc.)
- Auto-restore audio when exiting fullscreen
- Configurable list of apps to auto-mute

### 3. Window Position Memory
- Save window positions per monitor configuration
- Detect monitor configuration changes
- Restore positions on app launch or configuration change
- Hash monitor layout for unique identification

### 4. Gaming Mode
- One-click performance mode toggle
- Suppress notifications while active
- Close/minimize resource-heavy background processes
- Adjust process priorities for active game
- Configurable profile for gaming mode actions

### 5. Process Priority Adjustment
- Change CPU priority levels for any process
- Support all Windows priority classes (Idle, BelowNormal, Normal, AboveNormal, High, RealTime)
- Persist priority settings per process name
- Apply priorities automatically on process detection

### 6. Startup with Windows
- Registry integration for auto-start
- UI toggle for enabling/disabling
- Command-line argument support for minimized start

## Architecture

### Core Layer (Interfaces & Models)

#### Enums

```csharp
// ProcessPriority - CPU priority levels
public enum ProcessPriority
{
    Idle = 64,           // IDLE_PRIORITY_CLASS
    BelowNormal = 16384, // BELOW_NORMAL_PRIORITY_CLASS
    Normal = 32,         // NORMAL_PRIORITY_CLASS
    AboveNormal = 32768, // ABOVE_NORMAL_PRIORITY_CLASS
    High = 128,          // HIGH_PRIORITY_CLASS
    RealTime = 256       // REALTIME_PRIORITY_CLASS (requires admin)
}
```

#### Models

```csharp
// FocusHistoryEntry - Tracks a focus change event
public sealed record FocusHistoryEntry
{
    public IntPtr WindowHandle { get; init; }
    public int ProcessId { get; init; }
    public string ProcessName { get; init; }
    public string WindowTitle { get; init; }
    public DateTime FocusedAt { get; init; }
}

// FullscreenState - Tracks fullscreen window state
public sealed record FullscreenState
{
    public IntPtr WindowHandle { get; init; }
    public int ProcessId { get; init; }
    public string ProcessName { get; init; }
    public bool IsFullscreen { get; init; }
    public DateTime DetectedAt { get; init; }
}

// MonitorLayout - Identifies a monitor configuration
public sealed record MonitorLayout
{
    public string LayoutHash { get; init; }          // Unique hash for layout
    public int MonitorCount { get; init; }
    public List<MonitorInfo> Monitors { get; init; }
}

// MonitorInfo - Information about a single monitor
public sealed record MonitorInfo
{
    public string DeviceName { get; init; }
    public int X { get; init; }
    public int Y { get; init; }
    public int Width { get; init; }
    public int Height { get; init; }
    public bool IsPrimary { get; init; }
}

// WindowPosition - Saved window position
public sealed record WindowPosition
{
    public string ProcessName { get; init; }
    public string? WindowTitle { get; init; }
    public int X { get; init; }
    public int Y { get; init; }
    public int Width { get; init; }
    public int Height { get; init; }
    public WindowState WindowState { get; init; }
    public string MonitorLayoutHash { get; init; }
}

// GamingModeConfig - Gaming mode settings
public sealed record GamingModeConfig
{
    public bool IsEnabled { get; init; }
    public List<string> ProcessesToClose { get; init; }
    public List<string> ProcessesToMute { get; init; }
    public ProcessPriority GamePriority { get; init; }
    public bool SuppressNotifications { get; init; }
}

// ProcessPriorityConfig - Per-process priority settings
public sealed record ProcessPriorityConfig
{
    public string ProcessName { get; init; }
    public ProcessPriority Priority { get; init; }
    public bool AutoApply { get; init; }
}

// SmartFeaturesConfiguration - Aggregated settings
public sealed class SmartFeaturesConfiguration
{
    public int FocusHistorySize { get; set; } = 10;
    public bool AutoMuteOnFullscreen { get; set; }
    public List<string> AutoMuteProcesses { get; set; } = new();
    public bool WindowPositionMemoryEnabled { get; set; }
    public List<WindowPosition> SavedPositions { get; set; } = new();
    public GamingModeConfig GamingMode { get; set; } = new();
    public List<ProcessPriorityConfig> ProcessPriorities { get; set; } = new();
    public bool StartWithWindows { get; set; }
}
```

#### Interfaces

```csharp
// IFocusHistoryService - Focus tracking
public interface IFocusHistoryService : IDisposable
{
    IReadOnlyList<FocusHistoryEntry> GetHistory();
    FocusHistoryEntry? GetLastFocused();
    Task<bool> SwitchToLastFocusedAsync(int skipCount = 0);
    void Start();
    void Stop();
    
    event EventHandler<FocusHistoryEntry>? FocusChanged;
}

// IFullscreenDetectorService - Fullscreen detection and auto-mute
public interface IFullscreenDetectorService : IDisposable
{
    bool IsAnyWindowFullscreen { get; }
    FullscreenState? CurrentFullscreenWindow { get; }
    IReadOnlyList<string> AutoMuteProcesses { get; }
    void AddAutoMuteProcess(string processName);
    void RemoveAutoMuteProcess(string processName);
    void Start();
    void Stop();
    
    event EventHandler<FullscreenState>? FullscreenStateChanged;
}

// IWindowPositionService - Window position memory
public interface IWindowPositionService
{
    Task<MonitorLayout> GetCurrentLayoutAsync();
    Task SaveWindowPositionAsync(IntPtr windowHandle, string processName);
    Task<WindowPosition?> GetSavedPositionAsync(string processName, string? layoutHash = null);
    Task<bool> RestoreWindowPositionAsync(IntPtr windowHandle, string processName);
    Task RestoreAllWindowPositionsAsync();
    Task ClearSavedPositionsAsync(string? layoutHash = null);
    
    event EventHandler<MonitorLayout>? MonitorLayoutChanged;
}

// IGamingModeService - Gaming mode management
public interface IGamingModeService
{
    bool IsGamingModeActive { get; }
    GamingModeConfig Configuration { get; }
    Task<bool> EnableGamingModeAsync(string? gameProcessName = null);
    Task<bool> DisableGamingModeAsync();
    Task UpdateConfigurationAsync(GamingModeConfig config);
    
    event EventHandler<bool>? GamingModeChanged;
}

// IProcessPriorityService - Process priority management
public interface IProcessPriorityService
{
    Task<ProcessPriority?> GetProcessPriorityAsync(int processId);
    Task<bool> SetProcessPriorityAsync(int processId, ProcessPriority priority);
    Task<bool> SetProcessPriorityByNameAsync(string processName, ProcessPriority priority);
    IReadOnlyList<ProcessPriorityConfig> GetSavedPriorityConfigs();
    Task SavePriorityConfigAsync(ProcessPriorityConfig config);
    Task RemovePriorityConfigAsync(string processName);
}

// IStartupManagerService - Windows startup management
public interface IStartupManagerService
{
    bool IsStartupEnabled { get; }
    Task<bool> EnableStartupAsync(bool startMinimized = true);
    Task<bool> DisableStartupAsync();
    Task<bool> RefreshStartupStatusAsync();
}

// ISmartFeaturesService - Aggregated smart features coordination
public interface ISmartFeaturesService : IDisposable
{
    IFocusHistoryService FocusHistory { get; }
    IFullscreenDetectorService FullscreenDetector { get; }
    IWindowPositionService WindowPosition { get; }
    IGamingModeService GamingMode { get; }
    IProcessPriorityService ProcessPriority { get; }
    IStartupManagerService StartupManager { get; }
    
    Task InitializeAsync();
    Task<SmartFeaturesConfiguration> LoadConfigurationAsync();
    Task SaveConfigurationAsync(SmartFeaturesConfiguration config);
}
```

### Infrastructure Layer (Implementations)

#### NativeMethods Additions
- `GetPriorityClass` / `SetPriorityClass` - Process priority
- `MonitorFromWindow` / `GetMonitorInfo` / `EnumDisplayMonitors` - Monitor detection
- `GetWindowRect` / `MoveWindow` - Window positioning
- `SHQueryUserNotificationState` - Notification state (gaming mode)

#### FocusHistoryService
- Uses `SetWinEventHook` with `EVENT_SYSTEM_FOREGROUND`
- Circular buffer with configurable size
- Thread-safe with concurrent collections

#### FullscreenDetectorService
- Uses heuristic detection (window covers entire screen)
- Polls at 1-second intervals (less intrusive than hooks)
- Coordinates with IAudioService for auto-mute

#### WindowPositionService
- Computes layout hash from monitor configuration
- Persists to `%LOCALAPPDATA%\SystemTrayProcessManager\positions.json`
- Uses `SetWindowPos` for restoration

#### GamingModeService
- Coordinates with IProcessService, IAudioService, IWindowService
- Manages notification suppression via Windows API
- Persists config to `%LOCALAPPDATA%\SystemTrayProcessManager\gaming.json`

#### ProcessPriorityService
- Uses `SetPriorityClass` P/Invoke
- Subscribes to IProcessService.ProcessStarted for auto-apply
- Handles admin requirement for RealTime priority

#### StartupManagerService
- Uses Registry key: `HKCU\Software\Microsoft\Windows\CurrentVersion\Run`
- Includes minimized flag in registry value

### File Structure

```
src/
├── SystemTrayProcessManager.Core/
│   ├── Enums/
│   │   └── ProcessPriority.cs
│   ├── Models/
│   │   ├── FocusHistoryEntry.cs
│   │   ├── FullscreenState.cs
│   │   ├── MonitorInfo.cs
│   │   ├── MonitorLayout.cs
│   │   ├── WindowPosition.cs
│   │   ├── GamingModeConfig.cs
│   │   ├── ProcessPriorityConfig.cs
│   │   └── SmartFeaturesConfiguration.cs
│   └── Services/
│       ├── IFocusHistoryService.cs
│       ├── IFullscreenDetectorService.cs
│       ├── IWindowPositionService.cs
│       ├── IGamingModeService.cs
│       ├── IProcessPriorityService.cs
│       ├── IStartupManagerService.cs
│       └── ISmartFeaturesService.cs
├── SystemTrayProcessManager.Infrastructure/
│   ├── WindowsAPI/
│   │   └── NativeMethods.cs (additions)
│   └── Services/
│       ├── FocusHistoryService.cs
│       ├── FullscreenDetectorService.cs
│       ├── WindowPositionService.cs
│       ├── GamingModeService.cs
│       ├── ProcessPriorityService.cs
│       ├── StartupManagerService.cs
│       └── SmartFeaturesService.cs
└── SystemTrayProcessManager.UI/
    └── App.xaml.cs (DI registration)

tests/
└── SystemTrayProcessManager.Tests/
    ├── Core/
    │   ├── FocusHistoryEntryTests.cs
    │   ├── FullscreenStateTests.cs
    │   ├── MonitorInfoTests.cs
    │   ├── GamingModeConfigTests.cs
    │   └── ProcessPriorityConfigTests.cs
    └── Infrastructure/
        ├── FocusHistoryServiceTests.cs
        ├── FullscreenDetectorServiceTests.cs
        ├── ProcessPriorityServiceTests.cs
        └── StartupManagerServiceTests.cs
```

## Implementation Plan

### Phase 1: Core Models & Enums (30 min)
1. Create ProcessPriority enum
2. Create all model records
3. Create interfaces

### Phase 2: Native Methods (30 min)
1. Add process priority P/Invoke declarations
2. Add monitor enumeration P/Invoke declarations
3. Add window positioning P/Invoke declarations
4. Add notification state P/Invoke declarations

### Phase 3: Service Implementations (2.5 hours)
1. FocusHistoryService - focus tracking with circular buffer
2. FullscreenDetectorService - fullscreen detection and auto-mute
3. WindowPositionService - window position persistence
4. GamingModeService - gaming mode coordination
5. ProcessPriorityService - priority management
6. StartupManagerService - registry integration
7. SmartFeaturesService - coordinated initialization

### Phase 4: Integration (30 min)
1. Register services in DI container
2. Initialize smart features service on startup
3. Add cleanup in App.OnExit

### Phase 5: Unit Tests (1 hour)
1. Model tests (construction, equality, immutability)
2. Service tests (behavior, edge cases)

## Success Criteria
- [ ] All models created with proper immutability
- [ ] All interfaces defined with XML documentation
- [ ] All services implemented with error handling
- [ ] Focus history tracks window changes
- [ ] Fullscreen detection works for common apps
- [ ] Window positions save and restore correctly
- [ ] Gaming mode enables/disables cleanly
- [ ] Process priority changes work (admin for RealTime)
- [ ] Startup registry entries managed correctly
- [ ] All services registered in DI container
- [ ] Unit tests passing with good coverage
- [ ] Zero compiler warnings

## Performance Targets
- Focus history update: <5ms
- Fullscreen detection poll: <10ms
- Position save/restore: <20ms
- Gaming mode toggle: <100ms
- Priority change: <10ms
