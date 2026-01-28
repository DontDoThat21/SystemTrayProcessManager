# Task 8: Action Mapping System - Design Document

## Overview
Task 8 implements the Action Mapping System for SystemTrayProcessManager. This system connects hotkeys to specific actions (like mute, close, minimize) and provides two execution modes: Quick Action (affects the focused window) and Pinned Process (targets a specific process by name).

## Feature Requirements (from app.spec.md)

### 3.3 Action Mapping System
- Map hotkeys to specific actions
- Quick Action mode (affects focused window)
- Pinned Process mode (targets specific process)
- Action history tracking
- Undo functionality for reversible actions
- Supported actions:
  - Mute/Unmute/Toggle Mute
  - Close/Minimize/Maximize
  - Bring To Front
  - Hide/Show
- Action logging and coordination

## Architecture

### Component Overview
```
┌─────────────────────────────────────────────────────┐
│                 HotkeyConfigItem                     │
│  (existing - has ActionType, TargetProcessName)     │
└─────────────────────────────────────────────────────┘
                        │
                        ▼
┌─────────────────────────────────────────────────────┐
│              IActionMappingService                   │
│  - RegisterActionMapping                            │
│  - ExecuteAction                                    │
│  - GetActionHistory                                 │
│  - UndoLastAction                                   │
└─────────────────────────────────────────────────────┘
                        │
                        ▼
┌─────────────────────────────────────────────────────┐
│             ActionMappingService                     │
│  - Coordinates IWindowService & IAudioService       │
│  - Tracks action history                            │
│  - Implements undo for reversible actions           │
└─────────────────────────────────────────────────────┘
```

### Flow Diagram
```
User presses hotkey
        │
        ▼
┌───────────────────┐
│  HotkeyManager    │──► Triggers callback
│  Service          │
└───────────────────┘
        │
        ▼
┌───────────────────┐     ┌─────────────────────┐
│ ActionMapping     │────►│ Quick Action Mode   │
│ Service           │     │ (GetForegroundWindow)│
└───────────────────┘     └─────────────────────┘
        │                          │
        │     ┌─────────────────────┐
        └────►│ Pinned Process Mode │
              │ (Find by name)      │
              └─────────────────────┘
                       │
                       ▼
              ┌─────────────────────┐
              │ Execute Action      │
              │ (Window/Audio)      │
              └─────────────────────┘
                       │
                       ▼
              ┌─────────────────────┐
              │ Record History      │
              │ (for undo)          │
              └─────────────────────┘
```

## Data Models

### 1. ProcessActionType (Core/Enums)
```csharp
[Flags]
public enum ProcessActionType
{
    None = 0,
    Mute = 1,
    Unmute = 2,
    ToggleMute = 4,
    Close = 8,
    Minimize = 16,
    Maximize = 32,
    Restore = 64,
    BringToFront = 128,
    Hide = 256,
    Show = 512
}
```

### 2. ActionMode (Core/Enums)
```csharp
public enum ActionMode
{
    /// <summary>
    /// Affects the currently focused/foreground window.
    /// </summary>
    QuickAction,
    
    /// <summary>
    /// Targets a specific process by name.
    /// </summary>
    PinnedProcess
}
```

### 3. ActionHistoryEntry (Core/Models)
```csharp
public class ActionHistoryEntry
{
    public Guid Id { get; init; }
    public DateTime Timestamp { get; init; }
    public ProcessActionType ActionType { get; init; }
    public ActionMode Mode { get; init; }
    public int ProcessId { get; init; }
    public string ProcessName { get; init; }
    public IntPtr WindowHandle { get; init; }
    public bool WasSuccessful { get; init; }
    public bool IsReversible { get; init; }
    public ProcessActionType? ReverseActionType { get; init; }
    public object? PreviousState { get; init; } // For undo (e.g., previous volume level)
}
```

### 4. ActionExecutionResult (Core/Models)
```csharp
public class ActionExecutionResult
{
    public bool Success { get; init; }
    public ProcessActionType ActionType { get; init; }
    public int? ProcessId { get; init; }
    public string? ProcessName { get; init; }
    public string? ErrorMessage { get; init; }
    public ActionHistoryEntry? HistoryEntry { get; init; }
}
```

## Interface Design

### IActionMappingService (Core/Services)
```csharp
public interface IActionMappingService : IDisposable
{
    /// <summary>
    /// Gets a value indicating whether the service is initialized.
    /// </summary>
    bool IsInitialized { get; }
    
    /// <summary>
    /// Gets the count of registered action mappings.
    /// </summary>
    int MappingCount { get; }
    
    /// <summary>
    /// Occurs when an action is executed.
    /// </summary>
    event EventHandler<ActionExecutionResult>? ActionExecuted;
    
    /// <summary>
    /// Initializes the action mapping service and registers hotkeys.
    /// </summary>
    Task InitializeAsync();
    
    /// <summary>
    /// Registers an action mapping for a hotkey configuration.
    /// </summary>
    bool RegisterActionMapping(HotkeyConfigItem config);
    
    /// <summary>
    /// Unregisters an action mapping.
    /// </summary>
    bool UnregisterActionMapping(Guid configId);
    
    /// <summary>
    /// Unregisters all action mappings.
    /// </summary>
    void UnregisterAllMappings();
    
    /// <summary>
    /// Executes an action using Quick Action mode (foreground window).
    /// </summary>
    Task<ActionExecutionResult> ExecuteQuickActionAsync(ProcessActionType actionType);
    
    /// <summary>
    /// Executes an action targeting a specific process by name.
    /// </summary>
    Task<ActionExecutionResult> ExecutePinnedActionAsync(ProcessActionType actionType, string processName);
    
    /// <summary>
    /// Gets the action history (most recent first).
    /// </summary>
    IReadOnlyList<ActionHistoryEntry> GetActionHistory(int maxEntries = 50);
    
    /// <summary>
    /// Undoes the last reversible action.
    /// </summary>
    Task<ActionExecutionResult> UndoLastActionAsync();
    
    /// <summary>
    /// Clears the action history.
    /// </summary>
    void ClearHistory();
    
    /// <summary>
    /// Reloads action mappings from configuration.
    /// </summary>
    Task ReloadMappingsAsync();
}
```

## Implementation Details

### ActionMappingService (Infrastructure/Services)

#### Dependencies
- `ILogger<ActionMappingService>` - Logging
- `IHotkeyService` - Hotkey registration
- `IHotkeyConfigurationService` - Configuration persistence
- `IWindowService` - Window manipulation
- `IAudioService` - Audio control
- `IProcessService` - Process discovery

#### Key Implementation Points

1. **Quick Action Mode**
   - Uses `GetForegroundWindow()` to find the current window
   - Gets process ID via `GetWindowThreadProcessId()`
   - Executes action on that process/window

2. **Pinned Process Mode**
   - Uses `IProcessService.GetRunningProcessesAsync()` to find process by name
   - Supports partial matching (case-insensitive)
   - Falls back to first matching process if multiple found

3. **Reversible Actions**
   | Action | Reverse Action | State to Save |
   |--------|---------------|---------------|
   | Mute | Unmute | N/A |
   | Unmute | Mute | N/A |
   | Minimize | Restore | Previous WindowState |
   | Maximize | Restore | Previous WindowState |
   | Hide | Show | N/A |
   | Show | Hide | N/A |
   | BringToFront | N/A | Not reversible |
   | Close | N/A | Not reversible |

4. **Action History**
   - Stored in `LinkedList<ActionHistoryEntry>` for efficient add/remove
   - Limited to 100 entries (configurable)
   - Thread-safe with locks

5. **Hotkey Registration Integration**
   - On initialization, loads config from `IHotkeyConfigurationService`
   - Registers each enabled config item with `IHotkeyService`
   - Action callback executes the mapped action

## File Structure

### Files to Create
```
src/SystemTrayProcessManager.Core/
├── Enums/
│   ├── ProcessActionType.cs
│   └── ActionMode.cs
├── Models/
│   ├── ActionHistoryEntry.cs
│   └── ActionExecutionResult.cs
└── Services/
    └── IActionMappingService.cs

src/SystemTrayProcessManager.Infrastructure/
└── Services/
    └── ActionMappingService.cs

tests/SystemTrayProcessManager.Tests/
├── Core/
│   ├── ProcessActionTypeTests.cs
│   ├── ActionModeTests.cs
│   ├── ActionHistoryEntryTests.cs
│   └── ActionExecutionResultTests.cs
└── Infrastructure/
    └── ActionMappingServiceTests.cs
```

### Files to Modify
```
src/SystemTrayProcessManager.Core/Models/HotkeyConfigItem.cs
  - Add ActionMode property
  
src/SystemTrayProcessManager.Infrastructure/WindowsAPI/NativeMethods.cs
  - Add GetForegroundWindow (if not present - already exists)
  
src/SystemTrayProcessManager.UI/App.xaml.cs
  - Register IActionMappingService
  - Initialize on startup
```

## Integration with Existing Components

### HotkeyConfigItem Enhancement
The existing `HotkeyConfigItem` already has:
- `ActionType` (string) - needs to use `ProcessActionType` enum
- `TargetProcessName` - for pinned process mode

We'll add:
- `ActionMode` property to determine Quick Action vs Pinned Process

### HotkeyService Integration
When a hotkey is triggered:
1. `HotkeyManagerService` fires `HotkeyTriggered` event
2. `ActionMappingService` receives the event
3. Looks up the action mapping by hotkey binding
4. Executes the appropriate action based on mode

## Error Handling

1. **Process Not Found**
   - Log warning
   - Return failure result with descriptive message

2. **Window Operation Failed**
   - Log error with Win32 error code
   - Return failure result
   - Don't add to undo history

3. **Audio Operation Failed**
   - Log error
   - Return failure result
   - Don't add to undo history

4. **Undo Failed**
   - Log error
   - Keep history entry but mark as failed undo
   - Return failure result

## Performance Considerations

1. **Async Execution**
   - All action execution is async to avoid blocking UI
   - Use `Task.Run` for CPU-bound operations

2. **History Management**
   - Limit history to 100 entries
   - Use efficient data structures (LinkedList)

3. **Hotkey Response Time**
   - Target: <50ms from keypress to action completion
   - Use cached process list when possible

## Testing Strategy

### Unit Tests
1. **Enum Tests**
   - ProcessActionType flag combinations
   - ActionMode value validation

2. **Model Tests**
   - ActionHistoryEntry creation and properties
   - ActionExecutionResult creation

3. **Service Tests (with mocks)**
   - RegisterActionMapping
   - ExecuteQuickActionAsync (mock window service)
   - ExecutePinnedActionAsync (mock process service)
   - UndoLastActionAsync
   - History management

### Integration Tests
- Full flow: Configure hotkey → Press hotkey → Action executes
- Requires actual services (not mocked)

## Success Criteria

- [x] All subtasks from app.spec.features.status.md completed
- [ ] Code compiles without warnings
- [ ] All unit tests pass
- [ ] Error handling in place for all edge cases
- [ ] Logging present for all operations
- [ ] Service registered in DI container
- [ ] Integration tested with existing hotkey system

## Estimated Time
3-4 hours (as specified in task)

## Dependencies
- Task 6: Low-Level Keyboard Hook (complete)
- Task 4: Window Manipulation Features (complete)
- Task 5: Audio Control Integration (complete)
