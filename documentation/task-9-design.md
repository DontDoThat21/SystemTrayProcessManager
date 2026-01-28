# Task 9: Process Profiles & Automation - Design Document

## Overview
**Task**: 4.1 Process Profiles & Automation  
**Priority**: Medium  
**Estimated Time**: 4-5 hours  
**Dependencies**: Task 4 (Window Manipulation), Task 5 (Audio Control)  
**Status**: 🔄 In Progress

## Objective
Implement a comprehensive process profile and automation system that allows users to save and restore process configurations, schedule actions, create process groups for batch operations, and auto-launch startup processes.

## Feature Requirements

### 1. Process Profiles
- Save profiles for individual processes containing:
  - Window position (X, Y, Width, Height)
  - Audio level (0.0 - 1.0)
  - Mute state
  - Always-on-top flag
  - Window state (Normal, Minimized, Maximized)
- Load/apply profiles to running processes
- Auto-load profiles when a process is detected
- JSON-based persistence

### 2. Scheduled Actions
- Schedule actions to execute at specific times or intervals
- Support one-time and recurring schedules
- Integrate with existing ProcessActionType
- Persist schedules across application restarts

### 3. Startup Process Launcher
- Configure processes to auto-launch on application startup
- Support launch delays
- Optional window state on launch

### 4. Process Groups
- Group processes for batch operations
- Batch operations: Mute All, Unmute All, Close All, Minimize All, Hide All
- Pattern-based group matching (e.g., "Chrome*", "firefox*")

## Architecture

### Core Layer (Interfaces & Models)

#### Models

```csharp
// ProcessProfile - Stores configuration for a process
public sealed class ProcessProfile
{
    public Guid Id { get; init; }
    public string ProcessName { get; init; }
    public int? WindowX { get; init; }
    public int? WindowY { get; init; }
    public int? WindowWidth { get; init; }
    public int? WindowHeight { get; init; }
    public float? AudioLevel { get; init; }
    public bool? IsMuted { get; init; }
    public bool? AlwaysOnTop { get; init; }
    public WindowState? WindowState { get; init; }
    public bool AutoApplyOnDetection { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime LastModified { get; init; }
}

// ScheduledAction - Defines a scheduled action
public sealed class ScheduledAction
{
    public Guid Id { get; init; }
    public string Name { get; init; }
    public ProcessActionType ActionType { get; init; }
    public string? TargetProcessName { get; init; }
    public ScheduleType ScheduleType { get; init; }
    public DateTime? ExecuteAt { get; init; }        // One-time
    public TimeSpan? Interval { get; init; }          // Recurring
    public TimeSpan? DailyTime { get; init; }         // Daily at time
    public bool IsEnabled { get; init; }
    public DateTime? LastExecuted { get; init; }
    public DateTime? NextExecution { get; init; }
}

// StartupProcess - Process to launch on startup
public sealed class StartupProcess
{
    public Guid Id { get; init; }
    public string ExecutablePath { get; init; }
    public string? Arguments { get; init; }
    public string? WorkingDirectory { get; init; }
    public int DelayMilliseconds { get; init; }
    public WindowState LaunchWindowState { get; init; }
    public bool IsEnabled { get; init; }
}

// ProcessGroup - Group of processes for batch operations
public sealed class ProcessGroup
{
    public Guid Id { get; init; }
    public string Name { get; init; }
    public string Description { get; init; }
    public List<string> ProcessPatterns { get; init; }  // Supports wildcards
    public DateTime CreatedAt { get; init; }
}
```

#### Enums

```csharp
public enum ScheduleType
{
    OneTime,
    Interval,
    Daily
}
```

#### Interfaces

```csharp
// IProfileService - Profile management
public interface IProfileService
{
    Task<ProcessProfile?> GetProfileAsync(string processName);
    Task<IReadOnlyList<ProcessProfile>> GetAllProfilesAsync();
    Task SaveProfileAsync(ProcessProfile profile);
    Task DeleteProfileAsync(Guid profileId);
    Task<bool> ApplyProfileAsync(ProcessProfile profile, IntPtr windowHandle, int processId);
    Task<ProcessProfile> CaptureProfileAsync(ProcessInfo process);
    
    event EventHandler<ProcessProfile>? ProfileApplied;
}

// ISchedulerService - Scheduled actions
public interface ISchedulerService : IDisposable
{
    Task<IReadOnlyList<ScheduledAction>> GetScheduledActionsAsync();
    Task AddScheduledActionAsync(ScheduledAction action);
    Task UpdateScheduledActionAsync(ScheduledAction action);
    Task RemoveScheduledActionAsync(Guid actionId);
    Task<bool> EnableActionAsync(Guid actionId, bool enabled);
    void Start();
    void Stop();
    
    event EventHandler<ScheduledAction>? ActionExecuted;
}

// IStartupService - Startup process management
public interface IStartupService
{
    Task<IReadOnlyList<StartupProcess>> GetStartupProcessesAsync();
    Task AddStartupProcessAsync(StartupProcess process);
    Task UpdateStartupProcessAsync(StartupProcess process);
    Task RemoveStartupProcessAsync(Guid processId);
    Task LaunchStartupProcessesAsync();
}

// IProcessGroupService - Process groups
public interface IProcessGroupService
{
    Task<IReadOnlyList<ProcessGroup>> GetGroupsAsync();
    Task<ProcessGroup?> GetGroupAsync(Guid groupId);
    Task SaveGroupAsync(ProcessGroup group);
    Task DeleteGroupAsync(Guid groupId);
    Task<IReadOnlyList<ProcessInfo>> GetMatchingProcessesAsync(ProcessGroup group);
    Task<bool> ExecuteBatchActionAsync(Guid groupId, ProcessActionType action);
}
```

### Infrastructure Layer (Implementations)

#### ProfileService
- Uses `IWindowService` to apply window state
- Uses `IAudioService` to apply audio settings
- Subscribes to `IProcessService.ProcessStarted` for auto-apply
- Persists to `%LOCALAPPDATA%\SystemTrayProcessManager\profiles.json`

#### SchedulerService
- Uses `System.Threading.Timer` for scheduling
- Executes actions via `IActionMappingService`
- Persists to `%LOCALAPPDATA%\SystemTrayProcessManager\schedules.json`

#### StartupService
- Launches processes via `Process.Start()`
- Manages delays using `Task.Delay`
- Persists to `%LOCALAPPDATA%\SystemTrayProcessManager\startup.json`

#### ProcessGroupService
- Pattern matching using `Regex` with wildcard support
- Batch execution using `IActionMappingService`
- Persists to `%LOCALAPPDATA%\SystemTrayProcessManager\groups.json`

## File Structure

### Files to Create

**Core/Models/**
- `ProcessProfile.cs`
- `ScheduledAction.cs`
- `StartupProcess.cs`
- `ProcessGroup.cs`
- `ProfileConfiguration.cs` (aggregates all profiles/schedules for persistence)

**Core/Enums/**
- `ScheduleType.cs`

**Core/Services/**
- `IProfileService.cs`
- `ISchedulerService.cs`
- `IStartupService.cs`
- `IProcessGroupService.cs`

**Infrastructure/Services/**
- `ProfileService.cs`
- `SchedulerService.cs`
- `StartupService.cs`
- `ProcessGroupService.cs`

**Tests/Core/**
- `ProcessProfileTests.cs`
- `ScheduledActionTests.cs`
- `StartupProcessTests.cs`
- `ProcessGroupTests.cs`

**Tests/Infrastructure/**
- `ProfileServiceTests.cs`
- `SchedulerServiceTests.cs`
- `StartupServiceTests.cs`
- `ProcessGroupServiceTests.cs`

## Implementation Order

1. **Core Models** - Define all data models with proper validation
2. **Core Enums** - Define ScheduleType enum
3. **Core Interfaces** - Define service interfaces
4. **ProfileService** - Implement profile save/load/apply
5. **SchedulerService** - Implement scheduled actions
6. **StartupService** - Implement startup launcher
7. **ProcessGroupService** - Implement batch operations
8. **Unit Tests** - Create comprehensive tests for all components
9. **DI Registration** - Register services in App.xaml.cs

## Error Handling

- All services return bool/null on failure (no exceptions to caller)
- Comprehensive logging at Debug, Info, Warning, Error levels
- Graceful handling of:
  - Missing files (create defaults)
  - Invalid JSON (backup and recreate)
  - Process not found
  - Permission denied

## Performance Considerations

- Profile lookup: O(1) using dictionary by process name
- Schedule evaluation: Timer-based, no busy waiting
- Batch operations: Parallel execution with configurable concurrency
- File I/O: Async with caching

## Testing Strategy

- Unit tests for all models (equality, validation)
- Unit tests for service logic (mocked dependencies)
- Integration tests for file persistence
- Edge case coverage (empty configs, invalid data)

## Security Notes

- Validate executable paths before launch
- No admin elevation for normal operations
- Sanitize process names for file operations
