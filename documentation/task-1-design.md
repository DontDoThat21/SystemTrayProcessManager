# Design Document - Task 1: Project Setup & Architecture

## Task Overview
**Task**: Task 1: Project Setup & Architecture  
**Status**: In Progress  
**Started**: 2026-01-27  
**Engineer**: WPF Engineer Agent  
**Reviewer**: Reviewer Agent

## Objective
Establish the foundational architecture for SystemTrayProcessManager with proper project structure, dependency injection, logging infrastructure, and application lifecycle management.

## Design Decisions

### 1. Project Structure

#### Three-Layer Architecture
```
SystemTrayProcessManager/
├── SystemTrayProcessManager.Core/          (Class Library, .NET 8)
│   ├── Models/                             (Data models)
│   ├── Services/                           (Interface definitions)
│   └── Enums/                              (Enumeration types)
├── SystemTrayProcessManager.Infrastructure/ (Class Library, .NET 8)
│   ├── Services/                           (Service implementations)
│   ├── WindowsAPI/                         (P/Invoke declarations)
│   └── Helpers/                            (Utility classes)
├── SystemTrayProcessManager.UI/            (WPF Application, .NET 8)
│   ├── ViewModels/                         (MVVM ViewModels)
│   ├── Views/                              (XAML Views)
│   ├── Controls/                           (Custom controls)
│   ├── Resources/                          (Styles, themes, icons)
│   └── Services/                           (UI-specific services)
└── SystemTrayProcessManager.Tests/         (xUnit Test Project, .NET 8)
    ├── Core/                               (Core layer tests)
    ├── Infrastructure/                     (Infrastructure layer tests)
    └── UI/                                 (UI layer tests)
```

**Rationale**:
- **Core**: Contains pure interfaces and models with no dependencies - ensures testability
- **Infrastructure**: Implements business logic and Windows API integration - can be tested independently
- **UI**: WPF-specific code using MVVM pattern - separates presentation from logic
- **Tests**: Comprehensive unit test coverage using xUnit and Moq

#### Project Dependencies
```
UI → Infrastructure → Core
Tests → All Projects
```

### 2. Dependency Injection Setup

#### DI Container: Microsoft.Extensions.DependencyInjection
**Rationale**: 
- Native .NET support
- Lightweight and performant
- Well-documented
- Integrates seamlessly with Microsoft.Extensions.Logging

#### Service Lifetimes
```csharp
// Singleton: Shared instance across application
- ILogger<T>
- Serilog Logger
- Configuration services
- State management services

// Transient: New instance per request
- ViewModels
- UI services (dialogs, etc.)

// Scoped: Not used in desktop apps (web concept)
```

#### App.xaml.cs Structure
```csharp
public partial class App : Application
{
    private IServiceProvider _serviceProvider;
    private Mutex _instanceMutex;
    
    protected override void OnStartup(StartupEventArgs e)
    {
        // 1. Configure logging
        // 2. Configure services
        // 3. Single instance check
        // 4. Initialize main window
    }
}
```

### 3. Logging Infrastructure

#### Logging Framework: Serilog
**Rationale**:
- Structured logging support
- Multiple sinks (file, console, debug)
- High performance
- Easy configuration

#### Log Configuration
```csharp
Log Location: %LOCALAPPDATA%\SystemTrayProcessManager\logs\app.log
Rolling: Daily, retain 7 days
Format: JSON for structured data
Levels:
  - Debug: Development diagnostics
  - Information: General application flow
  - Warning: Recoverable issues
  - Error: Failures requiring attention
  - Fatal: Application crashes
```

#### Logging Pattern
```csharp
public async Task<bool> MethodAsync(string parameter)
{
    try
    {
        _logger.LogDebug("MethodAsync called with {Parameter}", parameter);
        
        // Implementation
        
        _logger.LogInformation("MethodAsync completed successfully");
        return true;
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "MethodAsync failed with parameter {Parameter}", parameter);
        return false;
    }
}
```

### 4. Single Instance Application

#### Implementation: Named Mutex
**Rationale**:
- System-wide mutex prevents multiple instances
- Simple and reliable
- Cross-session support
- Proper cleanup on exit

#### Mutex Pattern
```csharp
private const string MutexName = "Global\\SystemTrayProcessManager_SingleInstance";
private Mutex _instanceMutex;

private bool CheckSingleInstance()
{
    _instanceMutex = new Mutex(true, MutexName, out bool createdNew);
    
    if (!createdNew)
    {
        MessageBox.Show(
            "SystemTrayProcessManager is already running.",
            "Already Running",
            MessageBoxButton.OK,
            MessageBoxImage.Information);
        return false;
    }
    
    return true;
}
```

### 5. Application Lifecycle Management

#### Startup Sequence
1. **Configure Logging**: Initialize Serilog before any other operations
2. **Configure DI**: Register all services in container
3. **Single Instance Check**: Verify no other instance running
4. **Load Configuration**: Read settings from AppData
5. **Initialize Services**: Start background services (future tasks)
6. **Show Main Window**: Launch UI

#### Shutdown Sequence
1. **Save State**: Persist unsaved configuration
2. **Stop Services**: Gracefully stop background services
3. **Dispose Resources**: Clean up disposable resources
4. **Release Mutex**: Allow future instances
5. **Flush Logs**: Ensure all logs written

#### Exception Handling
```csharp
// Global exception handlers
AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;
Application.Current.DispatcherUnhandledException += OnDispatcherUnhandledException;
TaskScheduler.UnobservedTaskException += OnUnobservedTaskException;
```

### 6. NuGet Package Requirements

#### UI Project
```xml
<PackageReference Include="CommunityToolkit.Mvvm" Version="8.2.2" />
<PackageReference Include="Microsoft.Extensions.DependencyInjection" Version="8.0.0" />
<PackageReference Include="Serilog" Version="3.1.1" />
<PackageReference Include="Serilog.Sinks.File" Version="5.0.0" />
<PackageReference Include="Serilog.Extensions.Logging" Version="8.0.0" />
<PackageReference Include="Microsoft.Extensions.Logging" Version="8.0.0" />
```

#### Infrastructure Project
```xml
<PackageReference Include="Microsoft.Extensions.Logging.Abstractions" Version="8.0.0" />
<PackageReference Include="NAudio" Version="2.2.1" /> <!-- For future audio control -->
```

#### Test Project
```xml
<PackageReference Include="xunit" Version="2.6.4" />
<PackageReference Include="xunit.runner.visualstudio" Version="2.5.6" />
<PackageReference Include="Moq" Version="4.20.70" />
<PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.8.0" />
<PackageReference Include="FluentAssertions" Version="6.12.0" />
```

## Implementation Plan

### Phase 1: Project Structure Setup
1. ✅ Rename existing project to SystemTrayProcessManager.UI
2. ✅ Create SystemTrayProcessManager.Core class library
3. ✅ Create SystemTrayProcessManager.Infrastructure class library
4. ✅ Create SystemTrayProcessManager.Tests test project
5. ✅ Set up project references
6. ✅ Create folder structures in each project
7. ✅ Create .sln file for solution management

### Phase 2: Logging Infrastructure
1. ✅ Add Serilog NuGet packages to UI project
2. ✅ Create logging configuration helper
3. ✅ Configure file sink with AppData location
4. ✅ Add structured logging templates
5. ✅ Test logging output

### Phase 3: Dependency Injection Setup
1. ✅ Add Microsoft.Extensions.DependencyInjection packages
2. ✅ Create ServiceCollectionExtensions for registration
3. ✅ Configure services in App.xaml.cs
4. ✅ Register logger infrastructure
5. ✅ Create IServiceProvider property

### Phase 4: Single Instance Check
1. ✅ Implement Mutex-based single instance check
2. ✅ Add user-friendly error message
3. ✅ Handle mutex disposal on exit
4. ✅ Test multiple launch attempts

### Phase 5: Application Lifecycle
1. ✅ Implement OnStartup override
2. ✅ Implement OnExit override
3. ✅ Add global exception handlers
4. ✅ Create graceful shutdown logic
5. ✅ Add crash logging

### Phase 6: Testing
1. ✅ Create unit tests for DI configuration
2. ✅ Create tests for single instance behavior
3. ✅ Create tests for logging configuration
4. ✅ Verify all tests pass

## Success Criteria

### Functional Requirements
- [x] Solution compiles without errors or warnings
- [ ] Application launches to empty window (for now)
- [ ] Only one instance can run at a time
- [ ] Logs are written to AppData folder
- [ ] DI container is properly configured
- [ ] Application shuts down gracefully
- [ ] Global exceptions are logged

### Non-Functional Requirements
- [ ] Startup time < 2 seconds
- [ ] Memory usage < 20 MB (minimal UI)
- [ ] No memory leaks on repeated start/stop
- [ ] All code has XML documentation
- [ ] Unit test coverage > 80%

### Code Quality Requirements
- [ ] All patterns from SKILL.md followed
- [ ] Comprehensive error handling
- [ ] Appropriate logging throughout
- [ ] Clean architecture principles maintained
- [ ] Passes code review

## Risk Analysis

### Risk 1: Mutex Abandonment
**Risk**: Mutex not released if application crashes  
**Mitigation**: Use Global\ prefix for cross-session cleanup, document manual cleanup process
**Impact**: Low - Windows cleans up on process exit

### Risk 2: Log File Growth
**Risk**: Log files consume excessive disk space  
**Mitigation**: Configure rolling file with 7-day retention, 50MB size limit per file
**Impact**: Low - Managed by Serilog configuration

### Risk 3: DI Configuration Errors
**Risk**: Services not registered correctly causing runtime errors  
**Mitigation**: Comprehensive unit tests, validate container on startup
**Impact**: Medium - Would cause application failure

### Risk 4: AppData Permissions
**Risk**: Unable to write to AppData folder  
**Mitigation**: Fallback to temp directory, log error, graceful degradation
**Impact**: Low - AppData is always writable by user

## Performance Considerations

### Startup Performance
- Lazy load non-critical services
- Defer log file initialization until first write
- Minimize DI registration overhead
- Target: < 500ms to show tray icon (future task)

### Memory Footprint
- Use singleton pattern for shared services
- Dispose of transient services promptly
- Monitor for memory leaks in long-running services
- Target: < 20 MB initial footprint

## Testing Strategy

### Unit Tests
```
AppLifecycleTests:
  - OnStartup_InitializesServicesCorrectly()
  - OnExit_DisposesResourcesCorrectly()
  - CheckSingleInstance_PreventsDuplicateLaunch()
  - UnhandledException_IsLoggedCorrectly()

LoggingConfigurationTests:
  - ConfigureLogging_CreatesLogFile()
  - LogEntry_WrittenToFile()
  - LogLevel_FilteredCorrectly()

DependencyInjectionTests:
  - ServiceProvider_ResolvesLogger()
  - ServiceRegistration_AllServicesRegistered()
  - ServiceLifetime_CorrectLifetimeApplied()
```

### Manual Tests
1. Launch application → Verify window appears
2. Launch second instance → Verify error message
3. Check AppData folder → Verify log file created
4. Perform actions → Verify logs written
5. Close application → Verify clean shutdown
6. Check Task Manager → Verify process terminated

## Documentation Requirements

### Code Documentation
- XML comments on all public classes and methods
- Inline comments for complex logic
- Summary comments for each file

### User Documentation
- None required for this task (infrastructure only)

### Developer Documentation
- Architecture diagram (ASCII art acceptable)
- DI container registration guide
- Logging guidelines
- Testing instructions

## Future Considerations

### Extensibility Points
- Additional log sinks (e.g., Application Insights)
- Alternative DI containers
- Configuration file support
- Plugin system integration

### Technical Debt
- None anticipated for this task
- Future refactoring may consolidate service registration

## Approval

**Design Reviewed By**: [Reviewer Agent]  
**Approved**: [Date]  
**Implementation Start**: 2026-01-27

---

**Document Version**: 1.0  
**Last Updated**: 2026-01-27  
**Status**: In Review
