# SystemTrayProcessManager - Quick Reference Guide

## Quick Start for Agents

### 1. Starting a New Task
```bash
# Read these in order:
1. /mnt/skills/user/systemtray-processmanager/app.spec.features.status.md (current task)
2. /mnt/skills/user/systemtray-processmanager/SKILL.md (implementation patterns)
3. /mnt/skills/user/systemtray-processmanager/AGENTS.md (workflow guidance)
```

### 2. Common File Locations
```
Interfaces:        SystemTrayProcessManager.Core/Services/
Models:           SystemTrayProcessManager.Core/Models/
Enums:            SystemTrayProcessManager.Core/Enums/
Implementations:  SystemTrayProcessManager.Infrastructure/Services/
Win32 API:        SystemTrayProcessManager.Infrastructure/WindowsAPI/
ViewModels:       SystemTrayProcessManager.UI/ViewModels/
Views:            SystemTrayProcessManager.UI/Views/
Controls:         SystemTrayProcessManager.UI/Controls/
```

### 3. NuGet Packages by Project

**Core** (none - pure interfaces and models)

**Infrastructure**:
```xml
<PackageReference Include="NAudio" Version="2.2.1" />
<PackageReference Include="Microsoft.Extensions.Logging.Abstractions" Version="8.0.0" />
```

**UI**:
```xml
<PackageReference Include="CommunityToolkit.Mvvm" Version="8.2.2" />
<PackageReference Include="Microsoft.Extensions.DependencyInjection" Version="8.0.0" />
<PackageReference Include="Serilog" Version="3.1.1" />
<PackageReference Include="Serilog.Sinks.File" Version="5.0.0" />
```

### 4. Common Code Patterns

#### Creating a New Service
```csharp
// 1. Interface in Core
namespace SystemTrayProcessManager.Core.Services
{
    public interface IMyService
    {
        Task<bool> DoSomethingAsync();
    }
}

// 2. Implementation in Infrastructure
namespace SystemTrayProcessManager.Infrastructure.Services
{
    public class MyService : IMyService, IDisposable
    {
        private readonly ILogger<MyService> _logger;
        
        public MyService(ILogger<MyService> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }
        
        public async Task<bool> DoSomethingAsync()
        {
            try
            {
                _logger.LogDebug("DoSomething called");
                // Implementation
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "DoSomething failed");
                return false;
            }
        }
        
        public void Dispose()
        {
            // Cleanup
        }
    }
}

// 3. Register in App.xaml.cs
services.AddSingleton<IMyService, MyService>();
```

#### Adding P/Invoke
```csharp
// In Infrastructure/WindowsAPI/NativeMethods.cs
[DllImport("user32.dll", SetLastError = true)]
[return: MarshalAs(UnmanagedType.Bool)]
internal static extern bool MyWin32Function(IntPtr hWnd);

// In Infrastructure/WindowsAPI/Constants.cs
internal const int MY_CONSTANT = 0x0001;
```

#### Creating ViewModel
```csharp
// In UI/ViewModels/MyViewModel.cs
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

public partial class MyViewModel : ObservableObject
{
    [ObservableProperty]
    private string _myProperty;
    
    [RelayCommand]
    private async Task ExecuteAsync()
    {
        // Implementation
    }
}

// Register in App.xaml.cs
services.AddTransient<MyViewModel>();
```

### 5. Logging Levels
```csharp
_logger.LogTrace("Very detailed debugging");      // Rarely used
_logger.LogDebug("Debugging information");        // Development only
_logger.LogInformation("Normal flow");            // Key operations
_logger.LogWarning("Unexpected but handled");     // Potential issues
_logger.LogError(ex, "Operation failed");         // Errors
_logger.LogCritical(ex, "System failure");        // Catastrophic
```

### 6. Update Task Status
```markdown
### Task X: Task Name
**Status**: ✅ Complete
**Completed**: 2024-01-27

#### Subtasks
- [x] Subtask 1
- [x] Subtask 2

**Implementation Notes**:
- Implemented using pattern X
- Handled edge case Y

**Files Created**:
- SystemTrayProcessManager.Core/Services/IMyService.cs
- SystemTrayProcessManager.Infrastructure/Services/MyService.cs
```

### 7. Common Win32 API Calls

#### Window Manipulation
```csharp
// Bring to front
NativeMethods.SetForegroundWindow(handle);

// Minimize
NativeMethods.ShowWindow(handle, Constants.SW_MINIMIZE);

// Maximize
NativeMethods.ShowWindow(handle, Constants.SW_MAXIMIZE);

// Close
NativeMethods.SendMessage(handle, Constants.WM_CLOSE, IntPtr.Zero, IntPtr.Zero);

// Set always on top
int style = NativeMethods.GetWindowLong(handle, Constants.GWL_EXSTYLE);
style |= Constants.WS_EX_TOPMOST;
NativeMethods.SetWindowLong(handle, Constants.GWL_EXSTYLE, style);
```

#### Audio Control
```csharp
// Using NAudio
var enumerator = new MMDeviceEnumerator();
var device = enumerator.GetDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia);
var sessions = device.AudioSessionManager.Sessions;

// Find session for process
for (int i = 0; i < sessions.Count; i++)
{
    if (sessions[i].GetProcessID == processId)
    {
        sessions[i].SimpleAudioVolume.Mute = true;
        break;
    }
}
```

### 8. Testing Checklist
Before marking task complete:
- [ ] Code compiles without warnings
- [ ] All using statements necessary
- [ ] Null checks for dependencies
- [ ] Try-catch with logging
- [ ] Resources disposed properly
- [ ] Service registered in DI
- [ ] Task status updated
- [ ] Files listed in status document

### 9. Git Commit Message Format
```
[Task X] Brief description

- Implemented IMyService interface
- Added MyService with error handling
- Registered service in DI container
- Added comprehensive logging

Refs: Task X in app.spec.features.status.md
```

### 10. Performance Targets
- **Startup**: <2 seconds
- **Memory (idle)**: <50 MB
- **CPU (idle)**: <1%
- **Hotkey response**: <50ms
- **Process refresh**: 5 second interval

### 11. Common Namespaces
```csharp
// Core
using System;
using System.Threading.Tasks;
using System.Collections.Generic;

// Logging
using Microsoft.Extensions.Logging;

// MVVM
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

// Win32
using System.Runtime.InteropServices;

// Audio
using NAudio.CoreAudioApi;

// WPF
using System.Windows;
using System.Windows.Controls;
```

### 12. Error Handling Pattern
```csharp
public async Task<bool> MethodAsync()
{
    try
    {
        // Validate input
        if (invalidInput)
        {
            _logger.LogWarning("Invalid input");
            return false;
        }
        
        // Log entry
        _logger.LogDebug("Method called");
        
        // Do work
        await DoWorkAsync();
        
        // Log success
        _logger.LogInformation("Method completed");
        return true;
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Method failed");
        return false;
    }
}
```

### 13. Dependency Injection Lifetimes
```csharp
// Singleton - one instance for app lifetime
services.AddSingleton<IProcessService, ProcessMonitorService>();

// Transient - new instance each time
services.AddTransient<MainViewModel>();

// Scoped - one instance per scope (rarely used in desktop apps)
services.AddScoped<IScopedService, ScopedService>();
```

### 14. XAML Binding Basics
```xml
<!-- OneWay: UI updates from ViewModel -->
<TextBlock Text="{Binding MyProperty}" />

<!-- TwoWay: Both directions -->
<TextBox Text="{Binding MyProperty, Mode=TwoWay}" />

<!-- Command binding -->
<Button Command="{Binding MyCommand}" Content="Click Me" />

<!-- ItemsSource binding -->
<ListBox ItemsSource="{Binding MyCollection}" />
```

### 15. Configuration File Location
```csharp
// AppData path
var appDataPath = Path.Combine(
    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
    "SystemTrayProcessManager"
);

// Settings file
var settingsPath = Path.Combine(appDataPath, "settings.json");

// Log file
var logPath = Path.Combine(appDataPath, "logs", "app.log");
```

---

## Emergency Troubleshooting

### Build Errors
1. Check all project references are correct
2. Verify NuGet packages are restored
3. Clean and rebuild solution
4. Check for missing using statements
5. Verify interface implementations are complete

### Runtime Crashes
1. Check log file in AppData
2. Look for unhandled exceptions
3. Verify resource disposal
4. Check for null references
5. Verify Win32 handles are valid

### Hotkeys Not Working
1. Verify hook is initialized in App.xaml.cs
2. Check delegate reference is stored (prevents GC)
3. Verify unhook in Dispose is called
4. Check for conflicting system hotkeys
5. Test with debugger attached

### Audio Control Not Working
1. Verify NAudio package is installed
2. Check default audio device exists
3. Verify process has audio session
4. Check COM object disposal
5. Test with multiple audio processes

---

## Quick Command Reference

### View Task Status
```bash
view /mnt/skills/user/systemtray-processmanager/app.spec.features.status.md
```

### View Skill Guide
```bash
view /mnt/skills/user/systemtray-processmanager/SKILL.md
```

### View Agent Workflow
```bash
view /mnt/skills/user/systemtray-processmanager/AGENTS.md
```

---

This quick reference should speed up common development tasks!