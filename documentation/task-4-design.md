# Task 4: Window Manipulation Features - Design Document

## Overview
**Task**: Window Manipulation Features  
**Status**: 🔄 In Progress  
**Priority**: Critical  
**Estimated Time**: 4-5 hours  
**Dependencies**: Task 3 (Process Discovery & Monitoring)  
**Date Started**: 2026-01-27

## Objective
Implement comprehensive window manipulation capabilities through Windows API P/Invoke integration. This enables users to control application windows programmatically, including bringing windows to front, minimizing, maximizing, closing, hiding/showing, setting transparency, and always-on-top functionality.

## Architecture

### Component Diagram
```
┌─────────────────────────────────────────────────────────────┐
│                     Core Project                              │
│  ┌────────────────────────────────────────────────────────┐ │
│  │ IWindowService                                          │ │
│  │ - BringToFrontAsync(IntPtr)                            │ │
│  │ - MinimizeAsync(IntPtr)                                │ │
│  │ - MaximizeAsync(IntPtr)                                │ │
│  │ - CloseAsync(IntPtr, bool force)                       │ │
│  │ - HideAsync(IntPtr)                                    │ │
│  │ - ShowAsync(IntPtr)                                    │ │
│  │ - SetTransparencyAsync(IntPtr, byte)                   │ │
│  │ - SetAlwaysOnTopAsync(IntPtr, bool)                    │ │
│  │ - GetWindowStateAsync(IntPtr)                          │ │
│  └────────────────────────────────────────────────────────┘ │
│  ┌────────────────────────────────────────────────────────┐ │
│  │ Enums/WindowState.cs                                    │ │
│  │ - Normal, Minimized, Maximized, Hidden                 │ │
│  └────────────────────────────────────────────────────────┘ │
└─────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────┐
│                  Infrastructure Project                       │
│  ┌────────────────────────────────────────────────────────┐ │
│  │ WindowsAPI/NativeMethods.cs                             │ │
│  │ - SetForegroundWindow, ShowWindow, GetWindowLong       │ │
│  │ - SetWindowLong, SetLayeredWindowAttributes            │ │
│  │ - SendMessage, PostMessage, GetLastError               │ │
│  │ - IsWindowVisible, IsIconic, IsZoomed                  │ │
│  └────────────────────────────────────────────────────────┘ │
│  ┌────────────────────────────────────────────────────────┐ │
│  │ WindowsAPI/WindowConstants.cs                           │ │
│  │ - SW_SHOW, SW_HIDE, SW_MINIMIZE, SW_MAXIMIZE, etc.     │ │
│  │ - WM_CLOSE, WM_SYSCOMMAND, SC_CLOSE, etc.              │ │
│  │ - GWL_STYLE, GWL_EXSTYLE, WS_EX_LAYERED, etc.          │ │
│  └────────────────────────────────────────────────────────┘ │
│  ┌────────────────────────────────────────────────────────┐ │
│  │ Services/WindowManipulationService.cs                   │ │
│  │ - Implements IWindowService                             │ │
│  │ - Comprehensive error handling with Win32 errors       │ │
│  │ - Full logging for all operations                      │ │
│  └────────────────────────────────────────────────────────┘ │
└─────────────────────────────────────────────────────────────┘
```

## Technical Design

### 1. Interface Definition (Core/Services/IWindowService.cs)

```csharp
public interface IWindowService
{
    Task<bool> BringToFrontAsync(IntPtr windowHandle);
    Task<bool> MinimizeAsync(IntPtr windowHandle);
    Task<bool> MaximizeAsync(IntPtr windowHandle);
    Task<bool> RestoreAsync(IntPtr windowHandle);
    Task<bool> CloseAsync(IntPtr windowHandle, bool force = false);
    Task<bool> HideAsync(IntPtr windowHandle);
    Task<bool> ShowAsync(IntPtr windowHandle);
    Task<bool> SetTransparencyAsync(IntPtr windowHandle, byte alpha);
    Task<bool> SetAlwaysOnTopAsync(IntPtr windowHandle, bool enabled);
    Task<WindowState> GetWindowStateAsync(IntPtr windowHandle);
    Task<bool> IsWindowVisibleAsync(IntPtr windowHandle);
}
```

### 2. Native Methods (Infrastructure/WindowsAPI/NativeMethods.cs)

Required P/Invoke declarations:
- `SetForegroundWindow` - Bring window to foreground
- `ShowWindow` - Control window visibility state
- `GetWindowLong` / `GetWindowLongPtr` - Get window style flags
- `SetWindowLong` / `SetWindowLongPtr` - Set window style flags
- `SetLayeredWindowAttributes` - Set window transparency
- `SendMessage` / `PostMessage` - Send window messages
- `GetLastError` - Get Win32 error codes
- `IsWindow` - Validate window handle
- `IsWindowVisible` - Check visibility
- `IsIconic` - Check if minimized
- `IsZoomed` - Check if maximized
- `SetWindowPos` - Set window position and z-order

### 3. Window Constants (Infrastructure/WindowsAPI/WindowConstants.cs)

Required constants:
```csharp
// ShowWindow commands
SW_HIDE = 0
SW_SHOWNORMAL = 1
SW_SHOWMINIMIZED = 2
SW_SHOWMAXIMIZED = 3
SW_RESTORE = 9
SW_SHOW = 5

// Window messages
WM_CLOSE = 0x0010
WM_SYSCOMMAND = 0x0112
SC_CLOSE = 0xF060

// Window styles
GWL_STYLE = -16
GWL_EXSTYLE = -20
WS_EX_LAYERED = 0x00080000
WS_EX_TOPMOST = 0x00000008

// SetLayeredWindowAttributes flags
LWA_ALPHA = 0x2

// SetWindowPos flags and positions
HWND_TOPMOST = -1
HWND_NOTOPMOST = -2
SWP_NOMOVE = 0x0002
SWP_NOSIZE = 0x0001
```

### 4. WindowState Enum (Core/Enums/WindowState.cs)

```csharp
public enum WindowState
{
    Normal = 0,
    Minimized = 1,
    Maximized = 2,
    Hidden = 3,
    Invalid = -1
}
```

### 5. Service Implementation Strategy

Each method follows this pattern:
1. Validate window handle (IntPtr.Zero check)
2. Verify window exists using `IsWindow`
3. Execute Win32 API call
4. Check for errors using `GetLastError`
5. Log operation result
6. Return success/failure boolean

### Error Handling Strategy

- All methods return `bool` for success/failure
- Win32 error codes captured and logged
- Common errors handled gracefully:
  - `ERROR_INVALID_WINDOW_HANDLE` (1400)
  - `ERROR_ACCESS_DENIED` (5)
  - `ERROR_INVALID_PARAMETER` (87)
- No exceptions thrown to caller for API failures

## Files to Create

### Core Project
1. `src/SystemTrayProcessManager.Core/Services/IWindowService.cs`
2. `src/SystemTrayProcessManager.Core/Enums/WindowState.cs`

### Infrastructure Project
3. `src/SystemTrayProcessManager.Infrastructure/WindowsAPI/NativeMethods.cs`
4. `src/SystemTrayProcessManager.Infrastructure/WindowsAPI/WindowConstants.cs`
5. `src/SystemTrayProcessManager.Infrastructure/Services/WindowManipulationService.cs`

### Tests Project
6. `tests/SystemTrayProcessManager.Tests/Infrastructure/WindowManipulationServiceTests.cs`
7. `tests/SystemTrayProcessManager.Tests/Core/WindowStateTests.cs`

## Files to Modify
1. `src/SystemTrayProcessManager.UI/App.xaml.cs` - Register service in DI

## Test Strategy

### Unit Tests
- Interface contract verification
- Window handle validation
- Error handling paths
- State transitions
- Integration with mocked native methods

### Integration Tests (Manual)
- Bring Notepad to front
- Minimize/maximize windows
- Close applications gracefully and forcefully
- Set transparency levels
- Toggle always-on-top

## Success Criteria
- [ ] All P/Invoke declarations compile without warnings
- [ ] All 8 window manipulation operations work correctly
- [ ] Win32 error codes properly captured and logged
- [ ] Service registered in DI container
- [ ] 50+ unit tests passing
- [ ] Zero compiler warnings
- [ ] Code review approved

## Risks and Mitigations

| Risk | Mitigation |
|------|------------|
| Protected windows (UAC) | Log warning, return false gracefully |
| 32/64-bit pointer issues | Use IntPtr consistently, proper GetWindowLongPtr |
| Window handle invalidation | Validate with IsWindow before operations |
| Transparency on non-layered windows | Add WS_EX_LAYERED style first |

## Timeline

1. **Hour 1**: Create interface, enum, and native methods
2. **Hour 2**: Implement WindowManipulationService (Part 1: basic operations)
3. **Hour 3**: Implement WindowManipulationService (Part 2: advanced operations)
4. **Hour 4**: Create unit tests
5. **Hour 5**: DI registration, integration testing, documentation

## References
- [SetForegroundWindow](https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-setforegroundwindow)
- [ShowWindow](https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-showwindow)
- [SetLayeredWindowAttributes](https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-setlayeredwindowattributes)
- [SetWindowPos](https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-setwindowpos)
