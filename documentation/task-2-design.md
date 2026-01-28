# Design Document - Task 2: System Tray Integration

## Task Overview
**Task**: Task 2: System Tray Integration  
**Status**: In Progress  
**Started**: 2026-01-27  
**Engineer**: WPF Engineer Agent  
**Reviewer**: Reviewer Agent

## Objective
Implement system tray functionality to allow the application to run minimized to the system tray with a context menu, balloon notifications, and proper lifecycle integration.

## Design Decisions

### 1. System Tray Implementation Strategy

#### Technology Choice: System.Windows.Forms.NotifyIcon
**Rationale**:
- WPF does not have native system tray support
- `NotifyIcon` from Windows Forms is the standard approach
- Well-documented and reliable
- Integrates seamlessly with WPF applications
- Supports all required features (icons, context menus, balloon tips)

#### Windows Forms Interop
```xml
<!-- Required in csproj -->
<UseWindowsForms>true</UseWindowsForms>
```

### 2. Architecture

#### Service Interface (Core)
```csharp
// SystemTrayProcessManager.Core/Services/ITrayIconService.cs
public interface ITrayIconService : IDisposable
{
    /// <summary>Initializes the tray icon and context menu.</summary>
    void Initialize();
    
    /// <summary>Shows a balloon notification.</summary>
    void ShowBalloonTip(string title, string message, BalloonIcon icon = BalloonIcon.Info);
    
    /// <summary>Updates the tray icon.</summary>
    void SetIcon(System.Drawing.Icon icon);
    
    /// <summary>Sets the tray icon tooltip text.</summary>
    void SetTooltip(string tooltip);
    
    /// <summary>Shows or hides the tray icon.</summary>
    void SetVisible(bool visible);
    
    /// <summary>Occurs when the tray icon is left-clicked.</summary>
    event EventHandler? TrayIconClicked;
    
    /// <summary>Occurs when the user requests to exit.</summary>
    event EventHandler? ExitRequested;
}
```

#### Balloon Icon Enum (Core)
```csharp
// SystemTrayProcessManager.Core/Enums/BalloonIcon.cs
public enum BalloonIcon
{
    None,
    Info,
    Warning,
    Error
}
```

#### Service Implementation (UI)
```csharp
// SystemTrayProcessManager.UI/Services/TrayIconService.cs
public class TrayIconService : ITrayIconService
{
    private readonly ILogger<TrayIconService> _logger;
    private NotifyIcon? _notifyIcon;
    private bool _disposed;
    
    // Implementation follows...
}
```

### 3. Tray Icon Features

#### Context Menu Structure
```
┌─────────────────────────────┐
│ SystemTray Process Manager  │  (Header, disabled)
├─────────────────────────────┤
│ Show Window                 │  (Shows/activates main window)
│ ─────────────────────────── │  (Separator)
│ [Process List Placeholder]  │  (Future: Dynamic process submenu)
│ ─────────────────────────── │  (Separator)
│ Exit                        │  (Exits application)
└─────────────────────────────┘
```

#### Icon States
- **Default**: Normal application icon (tray-icon.ico)
- **Action Feedback**: Could animate/change on actions (future enhancement)

#### Interactions
| Action | Behavior |
|--------|----------|
| Left-click | Show/activate main window |
| Right-click | Show context menu |
| Double-click | Show/activate main window |
| Balloon click | Show/activate main window |

### 4. Window Behavior

#### Minimize to Tray
```csharp
// MainWindow behavior
private void OnStateChanged(object sender, EventArgs e)
{
    if (WindowState == WindowState.Minimized)
    {
        Hide(); // Hide from taskbar
        // Tray icon remains visible
    }
}
```

#### Restore from Tray
```csharp
// TrayIconService raises TrayIconClicked
// MainWindow handles by showing and activating
private void OnTrayIconClicked(object sender, EventArgs e)
{
    Show();
    WindowState = WindowState.Normal;
    Activate();
}
```

### 5. Balloon Notifications

#### Use Cases
- Application started: "SystemTray Process Manager is running"
- Action feedback: "Volume muted for [Process]"
- Errors: "Failed to perform action"

#### API
```csharp
_trayService.ShowBalloonTip(
    "Process Manager",
    "Application is running in the system tray",
    BalloonIcon.Info);
```

### 6. Resource Management

#### Icon Resource
- Location: `UI/Resources/Icons/tray-icon.ico`
- Format: Multi-resolution ICO (16x16, 32x32, 48x48, 256x256)
- Build Action: Resource or EmbeddedResource

#### Embedded Resource Loading
```csharp
// Load from embedded resource
var assembly = Assembly.GetExecutingAssembly();
using var stream = assembly.GetManifestResourceStream("SystemTrayProcessManager.UI.Resources.Icons.tray-icon.ico");
var icon = new System.Drawing.Icon(stream);
```

### 7. Application Lifecycle Integration

#### Startup Sequence
1. Create TrayIconService
2. Initialize tray icon
3. Show startup balloon notification
4. Show main window (or start minimized based on settings)

#### Shutdown Sequence
1. User clicks Exit (or closes window with setting)
2. TrayIconService.ExitRequested event raised
3. App.OnExit handles cleanup
4. TrayIconService.Dispose() called
5. NotifyIcon hidden and disposed

### 8. DI Registration

```csharp
// In App.ConfigureServices()
services.AddSingleton<ITrayIconService, TrayIconService>();
```

**Lifetime: Singleton**
- Single tray icon for entire application lifetime
- Must persist while app is running

### 9. Error Handling

#### Scenarios
| Scenario | Handling |
|----------|----------|
| Icon resource missing | Use default system icon, log warning |
| NotifyIcon creation fails | Log error, continue without tray (degraded mode) |
| Context menu creation fails | Log error, provide minimal menu |
| Balloon tip fails | Log debug, silent failure (non-critical) |

### 10. Testing Strategy

#### Unit Tests
1. Service initialization
2. Event raising (TrayIconClicked, ExitRequested)
3. Tooltip setting
4. Visibility toggling
5. Disposal verification

#### Manual Testing Checklist
- [ ] Tray icon appears on startup
- [ ] Left-click shows window
- [ ] Right-click shows context menu
- [ ] Context menu items work
- [ ] Minimizing hides to tray
- [ ] Exit properly closes app
- [ ] Balloon notifications appear
- [ ] Icon persists after explorer restart

---

## Files to Create

### Core Project
1. `Core/Enums/BalloonIcon.cs` - Balloon notification icon types
2. `Core/Services/ITrayIconService.cs` - Tray service interface

### UI Project
1. `UI/Services/TrayIconService.cs` - Tray service implementation
2. `UI/Resources/Icons/tray-icon.ico` - Tray icon resource

### Test Project
1. `Tests/UI/TrayIconServiceTests.cs` - Unit tests

### Files to Modify
1. `UI/SystemTrayProcessManager.csproj` - Add Windows Forms reference
2. `UI/App.xaml.cs` - Register and initialize tray service
3. `UI/MainWindow.xaml.cs` - Handle minimize to tray, restore

---

## Implementation Steps

1. Create `BalloonIcon` enum in Core
2. Create `ITrayIconService` interface in Core
3. Add Windows Forms support to UI project
4. Create tray icon resource (placeholder or generated)
5. Implement `TrayIconService` in UI
6. Update `App.xaml.cs` to register and use tray service
7. Update `MainWindow` for minimize-to-tray behavior
8. Create unit tests
9. Manual testing
10. Documentation update

---

## Success Criteria

- [ ] Application starts with visible tray icon
- [ ] Left-click on tray icon shows main window
- [ ] Right-click shows context menu
- [ ] "Show Window" menu item works
- [ ] "Exit" menu item closes application properly
- [ ] Minimizing window hides to tray
- [ ] Balloon notification shows on startup
- [ ] All resources properly disposed on exit
- [ ] Zero compiler warnings
- [ ] All unit tests pass

---

## Estimated Time
2-3 hours

## Dependencies
- Task 1 (Project Setup) - ✅ Complete
