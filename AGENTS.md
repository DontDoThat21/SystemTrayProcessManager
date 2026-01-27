# SystemTrayProcessManager - Agent Workflow Guide

## Overview
This document defines how Claude should approach building the SystemTrayProcessManager project using an agent-driven workflow. The agent will work through tasks defined in `app.spec.features.status.md`, following best practices from SKILL.md.

## Agent Operating Principles

### 1. Task-Driven Development
- Read tasks from `app.spec.features.status.md`
- Complete tasks sequentially within each phase
- Update task status after completion
- Document blockers and dependencies

### 2. Before Starting Any Task

#### Required Checks
1. **Read SKILL.md**: Always review relevant sections before writing code
2. **Check Dependencies**: Verify previous tasks are complete
3. **Review Context**: Check existing code to understand current state
4. **Plan Approach**: Outline implementation strategy before coding

#### Task Status Format in app.spec.features.status.md
```markdown
## Phase 1: Core Infrastructure

### Task 1: Project Setup & Architecture
**Status**: ⏳ In Progress | ✅ Complete | ❌ Blocked | 📋 Not Started
**Assigned**: [Date]
**Completed**: [Date]
**Blockers**: [Any issues preventing completion]

- [x] Create WPF application with MVVM architecture using CommunityToolkit.Mvvm
- [x] Set up dependency injection container
- [ ] Configure app to run as single instance
- [ ] Implement proper app lifecycle management
- [ ] Add logging framework (Serilog)

**Implementation Notes**:
- Solution created with Core, Infrastructure, UI projects
- DI configured in App.xaml.cs using Microsoft.Extensions.DependencyInjection
- Next: Single instance enforcement using Mutex

**Files Created**:
- SystemTrayProcessManager.sln
- SystemTrayProcessManager.Core/Services/IProcessService.cs
- SystemTrayProcessManager.UI/App.xaml.cs
```

### 3. Implementation Workflow

#### Step 1: Task Analysis
```
Before writing code, the agent should:
1. Read the task description completely
2. Identify all sub-tasks
3. Check SKILL.md for relevant patterns
4. List required files and classes
5. Identify dependencies on other tasks
```

#### Step 2: Design Phase
```
For each task, create a brief design:
1. List interfaces needed
2. List concrete implementations
3. Identify P/Invoke requirements
4. Plan error handling approach
5. Consider testing requirements
```

#### Step 3: Implementation Phase
```
Follow this order:
1. Create Core interfaces first
2. Create Infrastructure implementations second
3. Create UI ViewModels third
4. Create Views last
5. Wire up DI registrations
6. Add logging throughout
```

#### Step 4: Validation Phase
```
After implementation:
1. Review code against SKILL.md patterns
2. Check for error handling
3. Verify logging is present
4. Check for resource disposal
5. Test basic functionality
6. Update task status
```

### 4. Code Generation Guidelines

#### File Organization Rules
1. **One class per file** (except for small nested classes)
2. **Interfaces in Core project** (IProcessService.cs)
3. **Implementations in Infrastructure** (ProcessMonitorService.cs)
4. **ViewModels in UI/ViewModels** (MainViewModel.cs)
5. **Views in UI/Views** (MainWindow.xaml)

#### Naming Conventions
- **Interfaces**: `IServiceName` (e.g., IProcessService)
- **Services**: `ServiceNameService` (e.g., ProcessMonitorService)
- **ViewModels**: `ViewNameViewModel` (e.g., MainViewModel)
- **Views**: `ViewName` (e.g., MainWindow, SettingsWindow)
- **Models**: Descriptive nouns (e.g., ProcessInfo, HotkeyBinding)

#### Required Code Elements
Every file must include:
- XML documentation comments for public members
- Proper error handling with logging
- Resource disposal (IDisposable where needed)
- Null checking for dependencies
- Async/await for I/O operations

### 5. Testing Strategy

#### Unit Test Requirements
For each service, create tests for:
- Happy path scenarios
- Error conditions
- Edge cases
- Resource disposal

#### Manual Testing Checklist
Before marking tasks complete:
- [ ] Code compiles without warnings
- [ ] Application launches successfully
- [ ] No exceptions in log file
- [ ] Memory leaks checked
- [ ] Performance is acceptable

## Phase-by-Phase Agent Instructions

### Phase 1: Core Infrastructure

**Goal**: Establish foundational architecture with DI, logging, and basic app lifecycle.

**Agent Tasks**:
1. Create solution structure with three projects (Core, Infrastructure, UI)
2. Add required NuGet packages to each project
3. Set up dependency injection in App.xaml.cs
4. Configure Serilog with file output
5. Implement single instance check using Mutex
6. Add proper application shutdown handling

**Success Criteria**:
- Application launches to system tray
- Logs are written to AppData folder
- Only one instance can run at a time
- Clean shutdown releases all resources

**Key Files to Create**:
- `SystemTrayProcessManager.sln`
- `SystemTrayProcessManager.Core/SystemTrayProcessManager.Core.csproj`
- `SystemTrayProcessManager.Infrastructure/SystemTrayProcessManager.Infrastructure.csproj`
- `SystemTrayProcessManager.UI/SystemTrayProcessManager.UI.csproj`
- `SystemTrayProcessManager.UI/App.xaml` and `App.xaml.cs`

### Phase 2: Process Management Core

**Goal**: Implement process enumeration, monitoring, and basic window manipulation.

**Agent Tasks**:
1. Define core interfaces (IProcessService, IWindowService)
2. Create ProcessInfo model
3. Implement ProcessMonitorService with real-time updates
4. Implement WindowManipulationService with Win32 APIs
5. Add icon extraction functionality
6. Create process filtering logic

**CRITICAL**: Must read SKILL.md sections on Windows API integration before implementing.

**Success Criteria**:
- Can enumerate running processes
- Can extract process icons
- Can bring windows to front
- Can minimize/maximize windows
- Can close windows gracefully

**Key Files to Create**:
- `Core/Models/ProcessInfo.cs`
- `Core/Services/IProcessService.cs`
- `Core/Services/IWindowService.cs`
- `Infrastructure/WindowsAPI/NativeMethods.cs`
- `Infrastructure/WindowsAPI/Constants.cs`
- `Infrastructure/Services/ProcessMonitorService.cs`
- `Infrastructure/Services/WindowManipulationService.cs`

### Phase 3: Global Hotkey System

**Goal**: Implement low-level keyboard hook for global hotkeys.

**Agent Tasks**:
1. Define IHotkeyService interface
2. Create HotkeyBinding model
3. Implement HotkeyManagerService with SetWindowsHookEx
4. Add hotkey registration/unregistration
5. Implement conflict detection
6. Add proper cleanup in Dispose

**CRITICAL**: Must read SKILL.md section on hotkey implementation. This is the most complex part.

**Success Criteria**:
- Global hotkeys work even when other apps are focused
- Hotkeys respond within 50ms
- No memory leaks from hook
- Hook is properly released on exit
- Multiple hotkeys can be registered

**Key Files to Create**:
- `Core/Models/HotkeyBinding.cs`
- `Core/Services/IHotkeyService.cs`
- `Infrastructure/Services/HotkeyManagerService.cs`

### Phase 4: Audio Control

**Goal**: Implement per-process audio control using Windows Core Audio API.

**Agent Tasks**:
1. Add NAudio NuGet package
2. Define IAudioService interface
3. Implement AudioManagerService
4. Add mute/unmute functionality
5. Add volume control
6. Handle audio session events

**Success Criteria**:
- Can mute/unmute individual processes
- Can adjust process volume
- Audio changes persist across process restarts
- Handles missing audio devices gracefully

**Key Files to Create**:
- `Core/Services/IAudioService.cs`
- `Infrastructure/Services/AudioManagerService.cs`

### Phase 5: UI Implementation

**Goal**: Create modern WPF UI with MVVM pattern.

**Agent Tasks**:
1. Create ViewModels using CommunityToolkit.Mvvm
2. Create XAML views with modern styling
3. Implement data binding
4. Add custom controls (HotkeyCaptureBox, ProcessCard)
5. Create settings window
6. Implement tray icon integration

**Success Criteria**:
- UI is responsive and smooth
- All features accessible from UI
- Settings persist
- Dark theme applied
- Tray icon has context menu

**Key Files to Create**:
- `UI/ViewModels/MainViewModel.cs`
- `UI/ViewModels/SettingsViewModel.cs`
- `UI/ViewModels/HotkeyConfigViewModel.cs`
- `UI/Views/MainWindow.xaml`
- `UI/Views/SettingsWindow.xaml`
- `UI/Controls/HotkeyCaptureBox.xaml`
- `UI/Services/TrayIconService.cs`

## Common Agent Workflows

### Workflow 1: Implementing a New Service

```
1. READ: Review SKILL.md for service patterns
2. CREATE: Define interface in Core project
3. CREATE: Implement service in Infrastructure project
4. ADD: Error handling and logging
5. ADD: XML documentation
6. REGISTER: Add to DI container in App.xaml.cs
7. TEST: Create unit tests
8. UPDATE: Mark task as complete in app.spec.features.status.md
```

### Workflow 2: Adding Windows API Integration

```
1. READ: Review SKILL.md Windows API section
2. RESEARCH: Find required P/Invoke signatures
3. ADD: Declarations to NativeMethods.cs
4. ADD: Constants to Constants.cs
5. CREATE: Service method using P/Invoke
6. ADD: Error handling with Marshal.GetLastWin32Error()
7. ADD: Comprehensive logging
8. TEST: Verify with various processes
9. DOCUMENT: Add XML comments
```

### Workflow 3: Creating a ViewModel

```
1. READ: Review SKILL.md MVVM section
2. CREATE: ViewModel class inheriting ObservableObject
3. ADD: Observable properties with [ObservableProperty]
4. ADD: Commands with [RelayCommand]
5. INJECT: Required services via constructor
6. IMPLEMENT: Command logic with async/await
7. ADD: Error handling and logging
8. REGISTER: ViewModel in DI container
9. CREATE: Corresponding View XAML
10. BIND: Properties and commands in XAML
```

### Workflow 4: Handling Errors

```
Every method should follow this pattern:
1. Validate input parameters
2. Log method entry at Debug level
3. Wrap core logic in try-catch
4. Log errors at Error level with context
5. Return appropriate default/error value
6. Never throw exceptions to UI layer
```

## Agent Communication Protocol

### Status Updates
After each significant action, agent should report:
```
✅ Completed: [Task name]
📁 Files created: [List of new files]
🔧 Files modified: [List of modified files]
📝 Next action: [What's next]
⚠️ Issues: [Any problems encountered]
```

### Requesting Clarification
When unclear, agent should ask:
```
❓ Question: [Specific question]
📋 Context: [Why this is needed]
💡 Options: [Possible approaches]
🎯 Recommendation: [Suggested approach]
```

### Reporting Blockers
When blocked, agent should report:
```
🚫 Blocker: [What's blocking progress]
📊 Impact: [Which tasks are affected]
🔍 Investigation: [What was tried]
🆘 Needed: [What's needed to unblock]
```

## Quality Checklist

Before marking any task complete, verify:

### Code Quality
- [ ] Follows patterns from SKILL.md
- [ ] All public members have XML comments
- [ ] Error handling is comprehensive
- [ ] Logging is present at appropriate levels
- [ ] No hardcoded values (use configuration)
- [ ] Async/await used for I/O operations
- [ ] Resources are properly disposed

### Architecture Quality
- [ ] Interfaces defined in Core project
- [ ] Implementations in Infrastructure project
- [ ] ViewModels in UI project
- [ ] Services registered in DI container
- [ ] Dependencies injected via constructor
- [ ] No circular dependencies

### Functionality Quality
- [ ] Feature works as specified
- [ ] Edge cases handled
- [ ] Performance is acceptable (<50ms for hotkeys)
- [ ] Memory usage is reasonable
- [ ] No crashes or unhandled exceptions

### Documentation Quality
- [ ] Task status updated in app.spec.features.status.md
- [ ] Implementation notes added
- [ ] Files created/modified listed
- [ ] Any blockers documented

## File Templates

### Interface Template
```csharp
using System.Threading.Tasks;

namespace SystemTrayProcessManager.Core.Services
{
    /// <summary>
    /// [Service description]
    /// </summary>
    public interface IServiceName
    {
        /// <summary>
        /// [Method description]
        /// </summary>
        /// <param name="paramName">[Parameter description]</param>
        /// <returns>[Return value description]</returns>
        Task<bool> MethodNameAsync(string paramName);
    }
}
```

### Service Implementation Template
```csharp
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace SystemTrayProcessManager.Infrastructure.Services
{
    /// <summary>
    /// [Service description]
    /// </summary>
    public class ServiceNameService : IServiceName, IDisposable
    {
        private readonly ILogger<ServiceNameService> _logger;
        private bool _disposed;
        
        public ServiceNameService(ILogger<ServiceNameService> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }
        
        /// <inheritdoc/>
        public async Task<bool> MethodNameAsync(string paramName)
        {
            try
            {
                _logger.LogDebug("MethodName called with {ParamName}", paramName);
                
                // Implementation
                await Task.CompletedTask;
                
                _logger.LogInformation("MethodName completed successfully");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "MethodName failed");
                return false;
            }
        }
        
        public void Dispose()
        {
            if (_disposed) return;
            
            // Cleanup resources
            
            _disposed = true;
        }
    }
}
```

### ViewModel Template
```csharp
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace SystemTrayProcessManager.UI.ViewModels
{
    /// <summary>
    /// [ViewModel description]
    /// </summary>
    public partial class ViewNameViewModel : ObservableObject
    {
        private readonly IServiceName _service;
        private readonly ILogger<ViewNameViewModel> _logger;
        
        [ObservableProperty]
        private string _propertyName;
        
        public ViewNameViewModel(
            IServiceName service,
            ILogger<ViewNameViewModel> logger)
        {
            _service = service ?? throw new ArgumentNullException(nameof(service));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }
        
        [RelayCommand]
        private async Task ExecuteActionAsync()
        {
            try
            {
                _logger.LogDebug("ExecuteAction called");
                
                // Implementation
                
                _logger.LogInformation("ExecuteAction completed");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ExecuteAction failed");
            }
        }
    }
}
```

## Progress Tracking

### Metrics to Monitor
1. **Tasks Completed**: X/Y tasks per phase
2. **Code Coverage**: Unit test coverage percentage
3. **Performance**: Hotkey response time, CPU usage, memory usage
4. **Quality**: Compiler warnings, code analysis issues

### Daily Summary Format
```markdown
## Daily Progress Summary - [Date]

### Completed Today
- Task 1: [Description]
- Task 2: [Description]

### In Progress
- Task 3: [Current status]

### Blocked
- Task 4: [Blocker description]

### Metrics
- Tasks completed: X/Y
- Files created: N
- Lines of code: ~N
- Tests passing: X/Y

### Tomorrow's Focus
- Complete Task 3
- Start Task 5
- Resolve blocker for Task 4
```

## Emergency Procedures

### If Build Fails
1. Check all project references
2. Verify NuGet packages are restored
3. Check for missing using statements
4. Review error messages carefully
5. Check SKILL.md for correct patterns

### If Runtime Crashes
1. Check log files in AppData
2. Add try-catch with logging
3. Verify resource disposal
4. Check for null references
5. Test with debugger attached

### If Performance Is Poor
1. Profile with diagnostic tools
2. Check for excessive polling
3. Review caching strategy
4. Check for memory leaks
5. Optimize Win32 API calls

## Final Delivery Checklist

Before considering the project complete:

### Code Completeness
- [ ] All phases complete
- [ ] All tasks marked as done
- [ ] No compiler warnings
- [ ] All tests passing
- [ ] Code review performed

### Documentation
- [ ] README.md complete with screenshots
- [ ] USER_GUIDE.md written
- [ ] ARCHITECTURE.md documented
- [ ] Inline XML comments complete
- [ ] app.spec.features.status.md updated

### Quality
- [ ] No memory leaks detected
- [ ] Performance targets met
- [ ] Error handling comprehensive
- [ ] Logging appropriate
- [ ] Resource disposal verified

### Deliverables
- [ ] Installer created
- [ ] Portable version built
- [ ] Source code clean and organized
- [ ] Demo video recorded (optional)
- [ ] GitHub repository prepared

---

## Agent Workflow Summary

```
For each task:
1. READ app.spec.features.status.md for current task
2. READ SKILL.md for implementation guidance
3. PLAN implementation approach
4. CREATE code following templates
5. TEST functionality
6. UPDATE task status
7. COMMIT changes with clear message
8. MOVE to next task

Remember:
- Quality over speed
- Follow SKILL.md patterns strictly
- Document everything
- Test thoroughly
- Update status frequently
```

Good luck building SystemTrayProcessManager! 🚀