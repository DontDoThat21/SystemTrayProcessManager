# Task 3: Process Discovery & Monitoring - Design Document

## Overview
**Task**: Process Discovery & Monitoring  
**Phase**: Phase 2 - Process Management Core  
**Priority**: Critical  
**Dependencies**: Task 1 (Project Setup), Task 2 (System Tray Integration)  
**Estimated Time**: 3-4 hours

## Objective
Implement real-time process discovery and monitoring capabilities that enumerate running Windows processes, detect process changes, extract/cache process icons, and provide filtering/search functionality.

## Requirements Analysis

### Functional Requirements
1. **Process Enumeration**: Enumerate all running processes with window handles
2. **Process Filtering**: Filter out system processes, show only user-visible windowed apps
3. **Change Detection**: Detect when processes start or stop
4. **Icon Extraction**: Extract process icons from executable files
5. **Icon Caching**: Cache extracted icons to improve performance
6. **Search/Filter**: Allow searching processes by name or window title
7. **Background Polling**: Monitor processes on 5-second intervals

### Non-Functional Requirements
- Performance: Enumeration should complete within 100ms
- Memory: Icon cache should not exceed 50MB
- Thread Safety: All operations must be thread-safe
- Error Handling: Graceful handling of access-denied scenarios

## Architecture Design

### Component Diagram
```
┌─────────────────────────────────────────────────────────────────┐
│                    Core Project                                  │
├─────────────────────────────────────────────────────────────────┤
│  Models/                                                        │
│    └── ProcessInfo.cs          (Process data model)             │
│  Services/                                                      │
│    └── IProcessService.cs      (Process service interface)      │
└─────────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────────┐
│                 Infrastructure Project                           │
├─────────────────────────────────────────────────────────────────┤
│  Services/                                                      │
│    └── ProcessMonitorService.cs (Process enumeration/monitoring)│
│  Helpers/                                                       │
│    └── IconExtractor.cs         (Icon extraction & caching)     │
└─────────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────────┐
│                      UI Project                                  │
├─────────────────────────────────────────────────────────────────┤
│  Services/                                                      │
│    └── TrayIconService.cs       (Updated with process menu)     │
│  App.xaml.cs                    (DI registration)               │
└─────────────────────────────────────────────────────────────────┘
```

### Data Model

#### ProcessInfo Model
```csharp
public class ProcessInfo
{
    public int ProcessId { get; init; }           // PID
    public string Name { get; init; }              // Process name
    public string? WindowTitle { get; init; }      // Main window title
    public string? ExecutablePath { get; init; }   // Full path to exe
    public IntPtr WindowHandle { get; init; }      // Main window handle
    public ImageSource? Icon { get; init; }        // Cached icon
    public DateTime StartTime { get; init; }       // Process start time
    public bool IsResponding { get; init; }        // Is window responding
}
```

### Interface Design

#### IProcessService
```csharp
public interface IProcessService : IDisposable
{
    // Enumeration
    Task<IReadOnlyList<ProcessInfo>> GetRunningProcessesAsync(CancellationToken cancellationToken = default);
    Task<ProcessInfo?> GetProcessByIdAsync(int processId, CancellationToken cancellationToken = default);
    
    // Filtering
    Task<IReadOnlyList<ProcessInfo>> SearchProcessesAsync(string searchTerm, CancellationToken cancellationToken = default);
    
    // Monitoring
    void StartMonitoring();
    void StopMonitoring();
    bool IsMonitoring { get; }
    
    // Events
    event EventHandler<ProcessInfo>? ProcessStarted;
    event EventHandler<ProcessStoppedEventArgs>? ProcessStopped;
    event EventHandler<IReadOnlyList<ProcessInfo>>? ProcessListChanged;
}
```

#### IIconExtractor
```csharp
public interface IIconExtractor : IDisposable
{
    ImageSource? ExtractIcon(string executablePath);
    ImageSource? ExtractIconFromHandle(IntPtr handle);
    void ClearCache();
    int CachedIconCount { get; }
}
```

## Implementation Details

### 1. ProcessInfo Model (`Core/Models/ProcessInfo.cs`)
- Immutable record with `init` setters
- Contains all relevant process information
- Uses `ImageSource` for WPF-compatible icon representation

### 2. ProcessStoppedEventArgs (`Core/Models/ProcessStoppedEventArgs.cs`)
- Contains process ID and name of stopped process
- Allows subscribers to identify which process stopped

### 3. IProcessService Interface (`Core/Services/IProcessService.cs`)
- Async enumeration methods
- Monitoring start/stop control
- Events for process changes
- Thread-safe design

### 4. IIconExtractor Interface (`Core/Services/IIconExtractor.cs`)
- Icon extraction from executables
- Cache management
- Memory-efficient design

### 5. ProcessMonitorService (`Infrastructure/Services/ProcessMonitorService.cs`)
- Uses `System.Diagnostics.Process` for enumeration
- Implements 5-second polling with `Timer`
- Tracks previous process list for change detection
- Filters system processes (no window handle, system paths)
- Thread-safe using `ConcurrentDictionary`

### 6. IconExtractor (`Infrastructure/Helpers/IconExtractor.cs`)
- Uses Shell32 API for icon extraction
- Implements memory cache with LRU eviction
- Handles access denied gracefully
- Converts to WPF `ImageSource`

### Process Filtering Logic
Exclude processes where:
- `MainWindowHandle == IntPtr.Zero` (no visible window)
- Process name starts with system prefixes
- Executable path is in system directories
- Process is current application

### Threading Model
- All I/O operations run on background threads via `Task.Run`
- Events marshaled to UI thread when necessary
- `CancellationToken` support throughout
- Timer callback uses `async void` with proper exception handling

## File Structure

### Files to Create
```
Core/
├── Models/
│   ├── ProcessInfo.cs
│   └── ProcessStoppedEventArgs.cs
└── Services/
    ├── IProcessService.cs
    └── IIconExtractor.cs

Infrastructure/
├── Services/
│   └── ProcessMonitorService.cs
└── Helpers/
    └── IconExtractor.cs

Tests/
└── Infrastructure/
    └── ProcessMonitorServiceTests.cs
```

### Files to Modify
- `UI/App.xaml.cs` - Register new services in DI
- `UI/Services/TrayIconService.cs` - Add process list to context menu
- `Infrastructure/SystemTrayProcessManager.Infrastructure.csproj` - Add System.Drawing.Common reference

## Testing Strategy

### Unit Tests
1. **ProcessInfo Model Tests**
   - Property initialization
   - Equality comparison
   - Immutability verification

2. **ProcessMonitorService Tests**
   - Process enumeration returns results
   - Filtering excludes system processes
   - Search functionality works correctly
   - Monitoring start/stop lifecycle
   - Change detection fires events
   - Graceful handling of disposed processes

3. **IconExtractor Tests**
   - Icon extraction from valid path
   - Null return for invalid path
   - Cache hit verification
   - Cache clear functionality

### Integration Considerations
- Use mocked `ILogger` for all tests
- Test with actual system processes where safe
- Verify thread safety with concurrent access

## Error Handling

### Expected Errors
| Error | Handling |
|-------|----------|
| Access Denied | Log warning, skip process |
| Process Exited | Catch exception, remove from list |
| Invalid Path | Return null icon, continue |
| COM Exceptions | Log error, return default |

## Performance Considerations

### Optimization Strategies
1. **Lazy Icon Loading**: Only extract icons when needed
2. **Cache with Expiration**: Clear unused icons after 5 minutes
3. **Batch Updates**: Aggregate changes before firing events
4. **Async Enumeration**: Never block UI thread

### Memory Management
- Icon cache limited to 100 entries (LRU eviction)
- Dispose icons when removed from cache
- Use weak references where appropriate

## Success Criteria
- [ ] ProcessInfo model created with all properties
- [ ] IProcessService interface defined with events
- [ ] ProcessMonitorService implemented with filtering
- [ ] IconExtractor implemented with caching
- [ ] 5-second polling operational
- [ ] Process change events firing correctly
- [ ] Search/filter functionality working
- [ ] Services registered in DI container
- [ ] Context menu updated with process list
- [ ] All unit tests passing
- [ ] Zero compiler warnings

## Estimated Breakdown
| Component | Time |
|-----------|------|
| Models & Interfaces | 30 min |
| ProcessMonitorService | 60 min |
| IconExtractor | 45 min |
| DI Integration | 15 min |
| TrayIcon Integration | 30 min |
| Unit Tests | 60 min |
| **Total** | **4 hours** |
