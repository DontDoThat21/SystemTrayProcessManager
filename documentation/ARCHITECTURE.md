# SystemTray Process Manager - Architecture Documentation

## Table of Contents

1. [Overview](#1-overview)
2. [Project Structure](#2-project-structure)
3. [Three-Layer Architecture](#3-three-layer-architecture)
4. [Design Patterns](#4-design-patterns)
5. [Key Services](#5-key-services)
6. [Windows API Integration](#6-windows-api-integration)
7. [Data Flow](#7-data-flow)
8. [Configuration & Persistence](#8-configuration--persistence)
9. [Error Handling](#9-error-handling)
10. [Testing Strategy](#10-testing-strategy)

---

## 1. Overview

### Goals

SystemTray Process Manager is designed with the following architectural goals:

1. **Separation of Concerns** - Clear boundaries between UI, business logic, and infrastructure
2. **Testability** - All components can be unit tested in isolation
3. **Extensibility** - New features can be added without modifying existing code
4. **Maintainability** - Clean code that is easy to understand and modify
5. **Performance** - Efficient resource usage with responsive UI

### Tech Stack

| Layer | Technologies |
|-------|--------------|
| UI | WPF, XAML, CommunityToolkit.Mvvm |
| Infrastructure | .NET 10, P/Invoke, NAudio |
| Core | Pure C# interfaces and models |
| Testing | xUnit, Moq, FluentAssertions |
| Logging | Serilog |
| DI | Microsoft.Extensions.DependencyInjection |

---

## 2. Project Structure

```
SystemTrayProcessManager/
├── SystemTrayProcessManager.sln
│
├── src/
│   ├── SystemTrayProcessManager.Core/           # Layer 1: Domain
│   │   ├── Enums/
│   │   │   ├── ActionMode.cs
│   │   │   ├── BalloonIcon.cs
│   │   │   ├── ErrorSeverity.cs
│   │   │   ├── HotkeyModifier.cs
│   │   │   ├── ProcessActionType.cs
│   │   │   ├── ProcessPriority.cs
│   │   │   ├── ScheduleType.cs
│   │   │   └── WindowState.cs
│   │   ├── Models/
│   │   │   ├── ActionExecutionResult.cs
│   │   │   ├── ActionHistoryEntry.cs
│   │   │   ├── AppNotification.cs
│   │   │   ├── AppSettings.cs
│   │   │   ├── AudioProcessInfo.cs
│   │   │   ├── CrashReport.cs
│   │   │   ├── DiagnosticInfo.cs
│   │   │   ├── ErrorInfo.cs
│   │   │   ├── FocusHistoryEntry.cs
│   │   │   ├── FullscreenState.cs
│   │   │   ├── GamingModeConfig.cs
│   │   │   ├── HotkeyBinding.cs
│   │   │   ├── HotkeyConfigItem.cs
│   │   │   ├── HotkeyConfiguration.cs
│   │   │   ├── ProcessGroup.cs
│   │   │   ├── ProcessInfo.cs
│   │   │   ├── ProcessProfile.cs
│   │   │   ├── ScheduledAction.cs
│   │   │   ├── SmartFeaturesConfiguration.cs
│   │   │   ├── StartupProcess.cs
│   │   │   └── WindowPosition.cs
│   │   └── Services/
│   │       ├── IActionMappingService.cs
│   │       ├── IAudioService.cs
│   │       ├── IConfigurationService.cs
│   │       ├── ICrashReporterService.cs
│   │       ├── IElevationService.cs
│   │       ├── IErrorHandlingService.cs
│   │       ├── IFocusHistoryService.cs
│   │       ├── IFullscreenDetectorService.cs
│   │       ├── IGamingModeService.cs
│   │       ├── IHotkeyConfigurationService.cs
│   │       ├── IHotkeyService.cs
│   │       ├── IIconExtractor.cs
│   │       ├── INotificationService.cs
│   │       ├── IProcessGroupService.cs
│   │       ├── IProcessPriorityService.cs
│   │       ├── IProcessService.cs
│   │       ├── IProfileService.cs
│   │       ├── ISchedulerService.cs
│   │       ├── ISmartFeaturesService.cs
│   │       ├── IStartupManagerService.cs
│   │       ├── IStartupService.cs
│   │       ├── ITooltipService.cs
│   │       ├── ITrayIconService.cs
│   │       ├── IWindowPositionService.cs
│   │       └── IWindowService.cs
│   │
│   ├── SystemTrayProcessManager.Infrastructure/ # Layer 2: Services
│   │   ├── Helpers/
│   │   │   └── IconExtractor.cs
│   │   ├── Services/
│   │   │   ├── ActionMappingService.cs
│   │   │   ├── AudioManagerService.cs
│   │   │   ├── ConfigurationService.cs
│   │   │   ├── CrashReporterService.cs
│   │   │   ├── ElevationService.cs
│   │   │   ├── ErrorHandlingService.cs
│   │   │   ├── FocusHistoryService.cs
│   │   │   ├── FullscreenDetectorService.cs
│   │   │   ├── GamingModeService.cs
│   │   │   ├── HotkeyConfigurationService.cs
│   │   │   ├── HotkeyManagerService.cs
│   │   │   ├── NotificationService.cs
│   │   │   ├── ProcessGroupService.cs
│   │   │   ├── ProcessMonitorService.cs
│   │   │   ├── ProcessPriorityService.cs
│   │   │   ├── ProfileService.cs
│   │   │   ├── SmartFeaturesService.cs
│   │   │   ├── StartupManagerService.cs
│   │   │   ├── StartupService.cs
│   │   │   ├── TooltipService.cs
│   │   │   ├── WindowManipulationService.cs
│   │   │   └── WindowPositionService.cs
│   │   └── WindowsAPI/
│   │       ├── NativeMethods.cs
│   │       ├── Structures.cs
│   │       └── WindowConstants.cs
│   │
│   └── SystemTrayProcessManager.UI/             # Layer 3: Presentation
│       ├── App.xaml / App.xaml.cs
│       ├── MainWindow.xaml / MainWindow.xaml.cs
│       ├── Controls/
│       │   ├── HotkeyCaptureBox.cs
│       │   ├── HotkeyFeedbackOverlay.xaml
│       │   └── NotificationPanel.xaml
│       ├── Converters/
│       │   ├── BoolToMuteIconConverter.cs
│       │   ├── InverseBoolConverter.cs
│       │   └── NotificationLevelToBrushConverter.cs
│       ├── Helpers/
│       │   └── TooltipHelper.cs
│       ├── Resources/
│       │   ├── Styles/
│       │   │   ├── ButtonStyles.xaml
│       │   │   ├── HotkeyCaptureBoxStyle.xaml
│       │   │   ├── ProcessCardStyle.xaml
│       │   │   └── WindowStyles.xaml
│       │   └── Themes/
│       │       └── DarkTheme.xaml
│       ├── Services/
│       │   └── TrayIconService.cs
│       ├── ViewModels/
│       │   ├── HotkeyConfigViewModel.cs
│       │   ├── MainViewModel.cs
│       │   ├── NotificationViewModel.cs
│       │   ├── ProcessCardViewModel.cs
│       │   └── SettingsViewModel.cs
│       └── Views/
│           ├── FirstRunWizard.xaml
│           ├── HotkeyConfigWindow.xaml
│           └── SettingsWindow.xaml
│
├── tests/
│   └── SystemTrayProcessManager.Tests/
│       ├── Core/                    # Model and enum tests
│       ├── Infrastructure/          # Service implementation tests
│       └── UI/                      # ViewModel tests
│
└── documentation/
    ├── app.spec.md
    ├── app.spec.features.status.md
    ├── ARCHITECTURE.md (this file)
    ├── USER_GUIDE.md
    └── task-*-{design,review,summary}.md
```

---

## 3. Three-Layer Architecture

### Layer Diagram

```
┌─────────────────────────────────────────────────────────────────────┐
│                    PRESENTATION LAYER (UI)                          │
│  ┌───────────────┐  ┌───────────────┐  ┌───────────────────────┐   │
│  │   ViewModels  │  │    Views      │  │   Custom Controls     │   │
│  │ (MVVM Binding)│  │   (XAML)      │  │ (ProcessCard, etc.)   │   │
│  └───────┬───────┘  └───────┬───────┘  └───────────┬───────────┘   │
│          │                  │                      │               │
│          └──────────────────┼──────────────────────┘               │
│                             │                                       │
│  ┌──────────────────────────┴──────────────────────────────────┐   │
│  │              TrayIconService (WinForms NotifyIcon)           │   │
│  └──────────────────────────┬──────────────────────────────────┘   │
└─────────────────────────────┼───────────────────────────────────────┘
                              │
              Dependency Injection (IServiceProvider)
                              │
┌─────────────────────────────┼───────────────────────────────────────┐
│                    INFRASTRUCTURE LAYER                             │
│                             │                                       │
│  ┌──────────────────────────┴──────────────────────────────────┐   │
│  │                    Service Implementations                    │   │
│  │  ┌─────────────────┐  ┌─────────────────┐  ┌──────────────┐  │   │
│  │  │ProcessMonitor   │  │WindowManipulation│  │AudioManager  │  │   │
│  │  │Service          │  │Service           │  │Service       │  │   │
│  │  └────────┬────────┘  └────────┬─────────┘  └──────┬───────┘  │   │
│  │           │                    │                   │          │   │
│  │  ┌────────┴────────┐  ┌────────┴─────────┐  ┌──────┴───────┐  │   │
│  │  │HotkeyManager    │  │ActionMapping     │  │Configuration │  │   │
│  │  │Service          │  │Service           │  │Service       │  │   │
│  │  └─────────────────┘  └──────────────────┘  └──────────────┘  │   │
│  └──────────────────────────────────────────────────────────────┘   │
│                                                                     │
│  ┌──────────────────────────────────────────────────────────────┐   │
│  │                    Windows API (P/Invoke)                     │   │
│  │  ┌───────────────┐  ┌───────────────┐  ┌──────────────────┐  │   │
│  │  │NativeMethods  │  │WindowConstants│  │Structures        │  │   │
│  │  │(LibraryImport)│  │(SW_*, WS_*)   │  │(KBDLLHOOKSTRUCT) │  │   │
│  │  └───────────────┘  └───────────────┘  └──────────────────┘  │   │
│  └──────────────────────────────────────────────────────────────┘   │
└─────────────────────────────────────────────────────────────────────┘
                              │
                    Interface Contracts
                              │
┌─────────────────────────────┼───────────────────────────────────────┐
│                       CORE LAYER (Domain)                           │
│                             │                                       │
│  ┌──────────────────────────┴──────────────────────────────────┐   │
│  │                      Interfaces (Contracts)                   │   │
│  │  IProcessService, IWindowService, IAudioService,             │   │
│  │  IHotkeyService, IConfigurationService, etc.                 │   │
│  └──────────────────────────────────────────────────────────────┘   │
│                                                                     │
│  ┌──────────────────────────────────────────────────────────────┐   │
│  │                         Models                                │   │
│  │  ProcessInfo, HotkeyBinding, AppSettings, CrashReport, etc.  │   │
│  └──────────────────────────────────────────────────────────────┘   │
│                                                                     │
│  ┌──────────────────────────────────────────────────────────────┐   │
│  │                          Enums                                │   │
│  │  WindowState, ProcessActionType, HotkeyModifier, etc.        │   │
│  └──────────────────────────────────────────────────────────────┘   │
└─────────────────────────────────────────────────────────────────────┘
```

### Layer Responsibilities

#### Core Layer (SystemTrayProcessManager.Core)

**Purpose:** Define contracts and domain models with zero dependencies.

- **Interfaces:** All service contracts (e.g., `IProcessService`, `IWindowService`)
- **Models:** Data structures (e.g., `ProcessInfo`, `HotkeyBinding`)
- **Enums:** Type-safe enumerations (e.g., `WindowState`, `ProcessActionType`)

**Dependencies:** None (pure .NET)

#### Infrastructure Layer (SystemTrayProcessManager.Infrastructure)

**Purpose:** Implement services and handle external integrations.

- **Services:** Implementations of Core interfaces
- **Windows API:** P/Invoke declarations and wrappers
- **Helpers:** Utility classes (e.g., `IconExtractor`)

**Dependencies:** Core, Microsoft.Extensions.Logging, NAudio

#### Presentation Layer (SystemTrayProcessManager.UI)

**Purpose:** User interface and user interaction handling.

- **ViewModels:** MVVM logic with CommunityToolkit.Mvvm
- **Views:** XAML UI definitions
- **Controls:** Custom WPF controls
- **Services:** UI-specific services (e.g., `TrayIconService`)

**Dependencies:** Core, Infrastructure, WPF, CommunityToolkit.Mvvm

---

## 4. Design Patterns

### MVVM (Model-View-ViewModel)

```
┌─────────────┐       ┌─────────────────┐       ┌─────────────┐
│    View     │──────▶│   ViewModel     │──────▶│    Model    │
│   (XAML)    │       │ (ObservableObj) │       │  (POCO/DTO) │
└─────────────┘       └─────────────────┘       └─────────────┘
      │                       │
      │   {Binding}           │
      ▼                       ▼
  UI Elements            Commands & Properties
```

**Implementation:**
- ViewModels inherit from `ObservableObject` (CommunityToolkit.Mvvm)
- Properties use `[ObservableProperty]` or `SetProperty()`
- Commands use `[RelayCommand]` or explicit `RelayCommand<T>`
- Views use `{Binding}` expressions

**Example:**
```csharp
public partial class MainViewModel : ObservableObject
{
    [ObservableProperty]
    private string _searchText = string.Empty;
    
    [RelayCommand]
    private async Task RefreshAsync()
    {
        // Implementation
    }
}
```

### Dependency Injection

**Container:** Microsoft.Extensions.DependencyInjection

**Registration (App.xaml.cs):**
```csharp
private static IServiceProvider ConfigureServices()
{
    var services = new ServiceCollection();
    
    // Core services (singletons)
    services.AddSingleton<IProcessService, ProcessMonitorService>();
    services.AddSingleton<IWindowService, WindowManipulationService>();
    services.AddSingleton<IAudioService, AudioManagerService>();
    
    // ViewModels (transient)
    services.AddTransient<MainViewModel>();
    services.AddTransient<SettingsViewModel>();
    
    // Views (transient)
    services.AddTransient<MainWindow>();
    services.AddTransient<SettingsWindow>();
    
    return services.BuildServiceProvider();
}
```

### Service Pattern

All business logic is encapsulated in services:

```csharp
// Interface (Core)
public interface IWindowService
{
    Task<bool> BringToFrontAsync(IntPtr handle);
    Task<bool> MinimizeAsync(IntPtr handle);
    // ...
}

// Implementation (Infrastructure)
public class WindowManipulationService : IWindowService, IDisposable
{
    private readonly ILogger<WindowManipulationService> _logger;
    
    public WindowManipulationService(ILogger<WindowManipulationService> logger)
    {
        _logger = logger;
    }
    
    public async Task<bool> BringToFrontAsync(IntPtr handle)
    {
        // Implementation using P/Invoke
    }
}
```

### Factory Methods

Used for complex object creation:

```csharp
// ErrorInfo.cs
public static ErrorInfo FromException(Exception ex, string source = "")
{
    ArgumentNullException.ThrowIfNull(ex);
    return new ErrorInfo
    {
        ExceptionType = ex.GetType().FullName,
        Message = ex.Message,
        StackTrace = ex.StackTrace,
        Source = source,
        Timestamp = DateTime.UtcNow
    };
}
```

### Observer Pattern

Events for cross-service communication:

```csharp
// In IProcessService
public event EventHandler<ProcessInfo>? ProcessStarted;
public event EventHandler<ProcessStoppedEventArgs>? ProcessStopped;

// Raised in ProcessMonitorService
ProcessStarted?.Invoke(this, newProcess);
```

---

## 5. Key Services

### ProcessMonitorService

**Purpose:** Monitor running processes and detect changes.

**Key Features:**
- Background polling every 5 seconds
- Process filtering (windowed apps only)
- Thread-safe process tracking
- Icon extraction and caching

**Interface:** `IProcessService`

```csharp
public interface IProcessService
{
    IReadOnlyCollection<ProcessInfo> GetProcesses();
    void StartMonitoring();
    void StopMonitoring();
    event EventHandler<ProcessInfo>? ProcessStarted;
    event EventHandler<ProcessStoppedEventArgs>? ProcessStopped;
}
```

### WindowManipulationService

**Purpose:** Control window states and properties.

**Key Features:**
- Window activation with thread attachment
- State changes (minimize, maximize, restore)
- Transparency control
- Always-on-top management

**Interface:** `IWindowService`

### AudioManagerService

**Purpose:** Per-process audio control via Windows Core Audio.

**Key Features:**
- NAudio WASAPI integration
- Session caching for performance
- Device change handling
- Thread-safe operations

**Interface:** `IAudioService`

### HotkeyManagerService

**Purpose:** System-wide keyboard hook and hotkey management.

**Key Features:**
- Low-level keyboard hook (WH_KEYBOARD_LL)
- Async action execution
- Conflict detection
- <50ms response time

**Interface:** `IHotkeyService`

### ActionMappingService

**Purpose:** Map hotkeys to window/audio actions.

**Key Features:**
- Quick Action mode (focused window)
- Pinned Process mode (specific app)
- Action history tracking
- Undo support for reversible actions

**Interface:** `IActionMappingService`

---

## 6. Windows API Integration

### P/Invoke Pattern

Modern `LibraryImport` attribute (source-generated):

```csharp
internal static partial class NativeMethods
{
    [LibraryImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool SetForegroundWindow(IntPtr hWnd);
    
    [LibraryImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool ShowWindow(IntPtr hWnd, int nCmdShow);
}
```

### Window Manipulation APIs

| API | Purpose |
|-----|---------|
| `SetForegroundWindow` | Bring window to front |
| `ShowWindow` | Minimize, maximize, restore |
| `SetWindowLongPtr` | Set window styles (transparency, topmost) |
| `SetLayeredWindowAttributes` | Set window opacity |
| `SetWindowPos` | Position and z-order |
| `AttachThreadInput` | Thread attachment for activation |

### Keyboard Hook APIs

| API | Purpose |
|-----|---------|
| `SetWindowsHookEx` | Install keyboard hook |
| `UnhookWindowsHookEx` | Remove hook |
| `CallNextHookEx` | Pass to next hook |
| `GetModuleHandle` | Get module for hook |

### Audio Integration (NAudio)

```csharp
using NAudio.CoreAudioApi;

var enumerator = new MMDeviceEnumerator();
var device = enumerator.GetDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia);
var sessionManager = device.AudioSessionManager;

foreach (var session in sessionManager.Sessions)
{
    var processId = (int)session.GetProcessID;
    var volume = session.SimpleAudioVolume;
    // Control volume/mute
}
```

---

## 7. Data Flow

### Hotkey Action Flow

```
┌─────────────────┐
│ User presses    │
│ Ctrl+Alt+M      │
└────────┬────────┘
         │
         ▼
┌─────────────────┐
│ HotkeyManager   │  Low-level hook callback
│ Service         │  (WH_KEYBOARD_LL)
└────────┬────────┘
         │
         ▼
┌─────────────────┐
│ Action Mapping  │  Lookup hotkey → action
│ Service         │  
└────────┬────────┘
         │
         ├──────────────────────┐
         ▼                      ▼
┌─────────────────┐    ┌─────────────────┐
│ Quick Action    │    │ Pinned Process  │
│ (Focused Window)│    │ (By Name)       │
└────────┬────────┘    └────────┬────────┘
         │                      │
         ▼                      ▼
┌─────────────────┐    ┌─────────────────┐
│ Get Foreground  │    │ Find Process    │
│ Window Handle   │    │ By Name         │
└────────┬────────┘    └────────┬────────┘
         │                      │
         └──────────┬───────────┘
                    │
                    ▼
         ┌─────────────────┐
         │ Execute Action  │
         │ (Window/Audio)  │
         └────────┬────────┘
                  │
         ┌────────┴────────┐
         ▼                 ▼
┌─────────────────┐ ┌─────────────────┐
│ WindowService   │ │ AudioService    │
│ (P/Invoke)      │ │ (NAudio)        │
└─────────────────┘ └─────────────────┘
```

### Process Monitoring Flow

```
┌─────────────────┐
│ Timer Tick      │  Every 5 seconds
│ (Background)    │
└────────┬────────┘
         │
         ▼
┌─────────────────┐
│ Enumerate       │  System.Diagnostics.Process
│ Processes       │
└────────┬────────┘
         │
         ▼
┌─────────────────┐
│ Filter          │  - HasMainWindow
│ Processes       │  - Not System Process
└────────┬────────┘
         │
         ▼
┌─────────────────┐
│ Compare with    │  Detect started/stopped
│ Previous List   │
└────────┬────────┘
         │
    ┌────┴────┐
    ▼         ▼
┌────────┐ ┌────────┐
│ New    │ │ Removed│
│ Process│ │ Process│
└────┬───┘ └────┬───┘
     │          │
     ▼          ▼
┌─────────────────┐
│ Raise Events    │  ProcessStarted/Stopped
└────────┬────────┘
         │
         ▼
┌─────────────────┐
│ UI Updates      │  Via ViewModel binding
│ (Dispatcher)    │
└─────────────────┘
```

---

## 8. Configuration & Persistence

### Settings Structure

```json
{
  "version": "1.0.0",
  "lastModified": "2026-01-29T12:00:00Z",
  "isFirstRun": false,
  "hotkeysEnabled": true,
  "defaultVolume": 0.8,
  "muteOnMinimize": false,
  "startWithWindows": true,
  "startMinimized": true,
  "enableNotifications": true,
  "minimizeToTray": true,
  "processRefreshIntervalMs": 5000,
  "theme": "Dark",
  "showProcessIcons": true,
  "animationsEnabled": true
}
```

### File Locations

```
%LOCALAPPDATA%\SystemTrayProcessManager\
├── settings.json           # Application settings
├── settings.backup.json    # Auto-backup
├── hotkeys.json            # Hotkey bindings
├── profiles.json           # Process profiles
├── logs/
│   └── log-YYYYMMDD.txt    # Daily log files
└── crash-reports/
    └── crash_*.json        # Crash reports
```

### ConfigurationService

```csharp
public interface IConfigurationService
{
    Task<AppSettings> LoadSettingsAsync();
    Task<bool> SaveSettingsAsync(AppSettings settings);
    Task<bool> BackupSettingsAsync();
    Task<bool> RestoreFromBackupAsync();
    Task<bool> ResetToDefaultsAsync();
    event EventHandler<AppSettings>? SettingsChanged;
}
```

---

## 9. Error Handling

### Strategy

1. **Try-Catch at Boundaries** - All service methods handle exceptions
2. **Logging** - All errors logged with context
3. **Graceful Degradation** - Return false/null on failure, don't throw
4. **Crash Reports** - Unhandled exceptions generate diagnostic reports

### Error Handling Service

```csharp
public interface IErrorHandlingService
{
    void HandleError(Exception exception, string source);
    void HandleError(string message, ErrorSeverity severity, string source);
    string GetUserFriendlyMessage(Exception exception);
    event EventHandler<ErrorInfo>? ErrorOccurred;
}
```

### Global Exception Handlers

```csharp
// UI Thread
DispatcherUnhandledException += (s, e) =>
{
    _crashReporterService.GenerateReportAsync(e.Exception, "UI Thread");
    e.Handled = true;
};

// Background Threads
AppDomain.CurrentDomain.UnhandledException += (s, e) =>
{
    var ex = e.ExceptionObject as Exception;
    _crashReporterService.GenerateReportAsync(ex, "Background Thread");
};

// Unobserved Tasks
TaskScheduler.UnobservedTaskException += (s, e) =>
{
    _crashReporterService.GenerateReportAsync(e.Exception, "Unobserved Task");
    e.SetObserved();
};
```

---

## 10. Testing Strategy

### Test Pyramid

```
          ┌─────────┐
          │   E2E   │  (Manual testing)
          │ (Few)   │
         ┌┴─────────┴┐
         │Integration │  (Service integration)
         │  (Some)    │
        ┌┴───────────┴┐
        │  Unit Tests  │  (Models, ViewModels, Services)
        │   (Many)     │
        └──────────────┘
```

### Test Organization

```
SystemTrayProcessManager.Tests/
├── Core/
│   ├── ProcessInfoTests.cs
│   ├── HotkeyBindingTests.cs
│   ├── AppSettingsTests.cs
│   └── ...
├── Infrastructure/
│   ├── ProcessMonitorServiceTests.cs
│   ├── ConfigurationServiceTests.cs
│   ├── CrashReporterServiceTests.cs
│   └── ...
└── UI/
    ├── MainViewModelTests.cs
    ├── SettingsViewModelTests.cs
    └── ...
```

### Test Patterns

**Arrange-Act-Assert:**
```csharp
[Fact]
public void GetTooltip_ValidKey_ReturnsText()
{
    // Arrange
    var service = new TooltipService(_mockLogger.Object);
    
    // Act
    var result = service.GetTooltip("ProcessCard.BringToFront");
    
    // Assert
    result.Should().NotBeNullOrEmpty();
}
```

**Mocking:**
```csharp
var mockLogger = new Mock<ILogger<TooltipService>>();
var service = new TooltipService(mockLogger.Object);
```

### Code Coverage

- **Target:** >80% coverage
- **Actual:** 1,140+ tests, all passing
- **Tools:** dotnet test, Coverlet

---

## Appendix: Glossary

| Term | Definition |
|------|------------|
| **P/Invoke** | Platform Invocation Services - calling unmanaged (Win32) functions |
| **WASAPI** | Windows Audio Session API - low-level audio control |
| **MVVM** | Model-View-ViewModel pattern for UI separation |
| **DI** | Dependency Injection - inversion of control pattern |
| **Hook** | System callback for intercepting events (keyboard, mouse) |
| **TFM** | Target Framework Moniker (e.g., net10.0-windows) |

---

*Last updated: 2026-01-29*
