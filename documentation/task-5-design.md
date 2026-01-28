# Task 5: Audio Control Integration - Design Document

## Overview
**Task**: 2.3 Audio Control Integration  
**Priority**: High  
**Estimated Time**: 3-4 hours  
**Dependencies**: Task 3 (Process Discovery & Monitoring)  
**Date**: 2026-01-27

## Objective
Implement per-process audio control functionality using NAudio library to integrate with Windows Core Audio API (WASAPI). This allows muting, unmuting, and adjusting volume for individual application processes.

## Requirements

### Functional Requirements
1. Mute process audio
2. Unmute process audio
3. Set process volume (0.0 - 1.0 range)
4. Get process volume level
5. Check if process is muted
6. Audio session event handling
7. Proper COM object disposal

### Non-Functional Requirements
- Thread-safe operations
- Async/await pattern for UI responsiveness
- Comprehensive error handling with detailed logging
- Graceful degradation when audio sessions are unavailable
- Resource cleanup on disposal

## Architecture

### Component Diagram
```
┌─────────────────────────────────────────────────────────────┐
│                    SystemTrayProcessManager.Core            │
│  ┌─────────────────────────────────────────────────────┐   │
│  │  IAudioService (Interface)                          │   │
│  │  - MuteProcessAsync(int processId)                  │   │
│  │  - UnmuteProcessAsync(int processId)                │   │
│  │  - SetProcessVolumeAsync(int processId, float vol)  │   │
│  │  - GetProcessVolumeAsync(int processId)             │   │
│  │  - IsProcessMutedAsync(int processId)               │   │
│  │  - GetAudioProcessesAsync()                         │   │
│  └─────────────────────────────────────────────────────┘   │
└─────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────┐
│              SystemTrayProcessManager.Infrastructure        │
│  ┌─────────────────────────────────────────────────────┐   │
│  │  AudioManagerService : IAudioService, IDisposable   │   │
│  │                                                     │   │
│  │  Dependencies:                                      │   │
│  │  - ILogger<AudioManagerService>                     │   │
│  │  - NAudio.CoreAudioApi.MMDeviceEnumerator           │   │
│  │                                                     │   │
│  │  Private Members:                                   │   │
│  │  - _deviceEnumerator: MMDeviceEnumerator            │   │
│  │  - _audioSessionCache: ConcurrentDictionary         │   │
│  │  - _sessionLock: SemaphoreSlim                      │   │
│  └─────────────────────────────────────────────────────┘   │
└─────────────────────────────────────────────────────────────┘
```

### Class Design

#### IAudioService Interface
```csharp
public interface IAudioService : IDisposable
{
    Task<bool> MuteProcessAsync(int processId);
    Task<bool> UnmuteProcessAsync(int processId);
    Task<bool> ToggleMuteProcessAsync(int processId);
    Task<bool> SetProcessVolumeAsync(int processId, float volume);
    Task<float?> GetProcessVolumeAsync(int processId);
    Task<bool?> IsProcessMutedAsync(int processId);
    Task<IReadOnlyList<AudioProcessInfo>> GetAudioProcessesAsync();
    Task RefreshAudioSessionsAsync();
}
```

#### AudioProcessInfo Model
```csharp
public class AudioProcessInfo
{
    public int ProcessId { get; init; }
    public string ProcessName { get; init; } = string.Empty;
    public string DisplayName { get; init; } = string.Empty;
    public float Volume { get; init; }
    public bool IsMuted { get; init; }
    public bool IsActive { get; init; }
}
```

#### AudioManagerService Implementation
Key implementation details:
1. Uses `MMDeviceEnumerator` to get default audio endpoint
2. Enumerates `AudioSessionManager2` to find process sessions
3. Caches sessions for performance, with refresh capability
4. Uses `SimpleAudioVolume` interface for mute/volume control
5. Implements proper COM object disposal patterns

## Technical Implementation

### NAudio WASAPI Integration
NAudio provides managed wrappers for Windows Core Audio API:
- `MMDeviceEnumerator`: Enumerates audio devices
- `MMDevice`: Represents an audio endpoint device
- `AudioSessionManager2`: Manages audio sessions
- `AudioSessionControl`: Controls individual audio sessions
- `SimpleAudioVolume`: Controls volume/mute for a session

### Thread Safety
- Use `SemaphoreSlim` for async locking during session enumeration
- Use `ConcurrentDictionary` for session caching
- All public methods are async to prevent UI blocking
- COM objects accessed on dedicated thread when needed

### Error Handling Strategy
1. Catch and log `COMException` for audio API failures
2. Return `null` or `false` for failed operations (no exceptions to caller)
3. Log at appropriate levels (Debug, Warning, Error)
4. Handle process termination gracefully (session cleanup)

### Session Lifecycle Management
- Sessions are cached on first access per process
- Cache is refreshed on explicit request or after failures
- Disposed sessions are detected and removed from cache
- Service disposal cleans up all cached sessions and COM objects

## Files to Create

### Core Project
1. `Core/Services/IAudioService.cs` - Interface definition
2. `Core/Models/AudioProcessInfo.cs` - Model for audio process data

### Infrastructure Project
1. `Infrastructure/Services/AudioManagerService.cs` - Implementation

### Test Project
1. `Tests/Infrastructure/AudioManagerServiceTests.cs` - Unit tests
2. `Tests/Core/AudioProcessInfoTests.cs` - Model tests

## Integration Points

### DI Registration (App.xaml.cs)
```csharp
services.AddSingleton<IAudioService, AudioManagerService>();
```

### Service Dependencies
- `ILogger<AudioManagerService>` for logging
- NAudio package (already installed in Infrastructure project)

## Testing Strategy

### Unit Tests
1. Constructor validation
2. Parameter validation (invalid process IDs)
3. Interface implementation verification
4. Disposal behavior
5. Thread safety basics
6. Return value validation for error cases

### Integration Considerations
- Audio sessions only exist when a process is producing audio
- Tests may need to handle cases where no audio sessions exist
- Mock-based testing for interface contract verification

## Success Criteria
- [x] IAudioService interface defined with XML documentation
- [x] AudioProcessInfo model created
- [x] AudioManagerService implemented with all methods
- [x] Proper error handling and logging throughout
- [x] COM object disposal implemented correctly
- [x] Service registered in DI container
- [x] Unit tests created with >80% code coverage
- [x] Code compiles with zero warnings
- [x] All tests pass

## Risk Mitigation

### Risk: No Audio Sessions Available
- **Mitigation**: Return empty list/null gracefully, log as info not error

### Risk: COM Threading Issues
- **Mitigation**: Use async patterns, avoid cross-thread COM access

### Risk: Process Terminated During Operation
- **Mitigation**: Catch exceptions, refresh cache, retry logic where appropriate

### Risk: Audio Device Changes
- **Mitigation**: Re-enumerate devices on failure, handle device removal gracefully
