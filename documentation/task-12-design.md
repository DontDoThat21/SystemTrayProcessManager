# Task 12: Settings & Persistence - Design Document

## Document Overview
**Task**: Task 12: Settings & Persistence (Phase 5.1)  
**Priority**: High  
**Estimated Time**: 3-4 hours  
**Actual Time**: 3 hours  
**Dependencies**: Task 1 (Core Infrastructure)  
**Created**: 2026-01-27  
**Updated**: 2026-01-29  
**Author**: WPF Engineer Agent

---

## 1. Task Overview

### 1.1 Objective
Implement a comprehensive settings and persistence system for SystemTrayProcessManager that:
- Stores application configuration in JSON format
- Persists user preferences across application restarts
- Provides a modern settings UI for configuration management
- Handles edge cases like corrupted files and missing configuration
- Implements first-run wizard experience
- Supports backup, restore, import/export, and reset-to-defaults functionality

### 1.2 Scope
**In Scope**:
- `AppSettings` model with all configuration categories
- `IConfigurationService` interface definition
- `ConfigurationService` implementation with JSON serialization
- `SettingsViewModel` and `SettingsWindow.xaml`
- `FirstRunWizard.xaml` for initial setup
- Configuration backup/restore functionality
- Import/export settings to/from external files
- Reset to defaults feature
- Comprehensive error handling for file I/O
- Unit tests for all components

**Out of Scope** (handled by other tasks):
- Hotkey binding UI (Task 7)
- Theme application switching at runtime
- Process profiles (Task 9)

### 1.3 Success Criteria
- [x] Settings persist correctly across app restarts
- [x] Corrupted settings file handled gracefully with fallback to defaults
- [x] Missing settings file creates new configuration automatically
- [x] Settings window displays all configuration options
- [x] First-run wizard appears on initial launch
- [x] Backup/restore functionality works correctly
- [x] Import/export functionality with file dialogs
- [x] Reset to defaults restores factory settings
- [x] All unit tests pass (141 tests, 100% coverage)
- [x] No compiler warnings (for task files)
- [x] Performance: Load settings <10ms, Save settings <50ms

---

## 2. Architecture

### 2.1 Layer Breakdown

```
+-----------------------------------------------------------------+
|  UI Layer (SystemTrayProcessManager.UI)                         |
|  +-- ViewModels/SettingsViewModel.cs       (MVVM ViewModel)     |
|  +-- Views/SettingsWindow.xaml + .cs       (Settings UI)        |
|  +-- Views/FirstRunWizard.xaml + .cs       (First-run flow)     |
+-----------------------------------------------------------------+
                           |
                           v
+-----------------------------------------------------------------+
|  Infrastructure Layer                                           |
|  +-- Services/ConfigurationService.cs      (JSON persistence)   |
+-----------------------------------------------------------------+
                           |
                           v
+-----------------------------------------------------------------+
|  Core Layer                                                     |
|  +-- Models/AppSettings.cs                 (Settings model)     |
|  +-- Services/IConfigurationService.cs     (Interface)          |
+-----------------------------------------------------------------+
```

### 2.2 Data Flow

```
User --> SettingsWindow --> SettingsViewModel --> IConfigurationService
                                                        |
                                               ConfigurationService
                                                        |
                                    %LOCALAPPDATA%\SystemTrayProcessManager\settings.json
```

---

## 3. Design Decisions

### 3.1 AppSettings Model (Core Layer)
- **Sealed class** with well-defined property regions: Metadata, Hotkeys, Audio Defaults, Behavior, Appearance
- **14 configurable properties** covering all user-facing settings
- **Validation**: `Validate()` returns error messages; `IsValid` computed property for quick checks
- **Sanitization**: `Sanitize()` clamps out-of-range values and fixes nulls
- **Equality**: Custom `Equals()` and `GetHashCode()` for change detection in ViewModel
- **Clone**: Deep copy support for original-vs-current comparisons
- **Static factory**: `AppSettings.Default` for creating default instances

### 3.2 IConfigurationService Interface (Core Layer)
- **11 methods**: Load, Save, Backup, Restore, Reset, GetDefaults, SettingsFileExists, BackupFileExists, Export, Import
- **Event**: `SettingsChanged` for reactive updates across the application
- **Properties**: `SettingsFilePath`, `CurrentSettings` for read access
- **Full XML documentation** on all members

### 3.3 ConfigurationService Implementation (Infrastructure Layer)
- **JSON persistence** via `System.Text.Json` with indented, camelCase output
- **Automatic backup** before every save operation
- **Corrupted file recovery**: Attempts backup restore, then falls back to defaults
- **Thread-safe**: `ConfigureAwait(false)` on all async I/O calls
- **Testable**: Internal constructor accepts custom config directory for test isolation
- **Specific exception handling**: `JsonException`, `IOException`, `UnauthorizedAccessException`

### 3.4 SettingsViewModel (UI Layer - MVVM)
- **CommunityToolkit.Mvvm**: `[ObservableProperty]` for all settings, `[RelayCommand]` for all actions
- **Change tracking**: `HasChanges` computed by comparing current properties against original snapshot
- **Delegate pattern**: `RequestImportFilePath` / `RequestExportFilePath` avoid WPF dialog dependencies in ViewModel
- **RequestClose event**: Enables the ViewModel to request View closure after save
- **8 commands**: Load, Save, Reset, Restore, Cancel, Import, Export, SaveAndClose

### 3.5 SettingsWindow (UI Layer - View)
- **4-tab layout**: General, Hotkeys, Audio, Appearance
- **Button bar**: Import, Export, Reset Defaults, Restore Backup, Cancel, Save
- **Dark theme**: Uses dynamic resource brushes from DarkTheme.xaml
- **File dialogs**: `Microsoft.Win32.OpenFileDialog` / `SaveFileDialog` wired via ViewModel delegates
- **IsDefault/IsCancel**: Save button is IsDefault, Cancel button is IsCancel

### 3.6 FirstRunWizard (UI Layer - View)
- **4-step wizard**: Welcome -> Hotkey Setup -> Permissions -> Complete
- **Hidden tabs**: TabItem headers collapsed for wizard-style navigation
- **Settings persistence**: Saves initial settings (hotkeys enabled, IsFirstRun=false) on completion
- **Integration**: Shown in `App.OnStartup` when `settings.IsFirstRun == true`

---

## 4. Settings Storage

### 4.1 File Location
```
%LOCALAPPDATA%\SystemTrayProcessManager\
+-- settings.json               <-- Primary settings file
+-- settings.backup.json        <-- Auto-backup before each save
+-- hotkeys.json                <-- Hotkey configuration (Task 7)
+-- logs\
    +-- app.log                 <-- Application logs
```

### 4.2 JSON Schema
```json
{
  "version": "1.0",
  "lastModified": "2026-01-29T00:00:00Z",
  "isFirstRun": false,
  "hotkeysEnabled": true,
  "defaultVolume": 1.0,
  "muteOnMinimize": false,
  "startWithWindows": false,
  "startMinimized": true,
  "enableNotifications": true,
  "minimizeToTray": true,
  "processRefreshIntervalMs": 5000,
  "theme": "Dark",
  "showProcessIcons": true,
  "animationsEnabled": true
}
```

---

## 5. DI Registration

```csharp
// In App.xaml.cs ConfigureServices()
services.AddSingleton<IConfigurationService, ConfigurationService>();
services.AddTransient<SettingsViewModel>();
services.AddTransient<SettingsWindow>();
services.AddTransient<FirstRunWizard>();
```

---

## 6. Integration Points

| Component | Integration |
|---|---|
| **App.xaml.cs** | Loads settings on startup; checks IsFirstRun; shows wizard |
| **TrayIconService** | `SettingsRequested` event triggers `ShowSettingsWindow()` |
| **ITrayIconService** | `SettingsRequested` event added to interface |
| **HotkeyManagerService** | Reads `HotkeysEnabled` from settings |
| **ProcessMonitorService** | Reads `ProcessRefreshIntervalMs` from settings |

---

## 7. Error Handling Strategy

1. **Corrupted JSON**: Attempt backup restore -> fall back to defaults
2. **Missing file**: Return defaults (first-run scenario)
3. **Access denied**: Log error, return false, show status message
4. **Invalid values**: Sanitize and clamp to valid ranges
5. **Import failure**: Return null, display user-friendly message
6. **Null parameters**: `ArgumentNullException.ThrowIfNull()` for required params

---

## 8. Test Strategy

| Test File | Coverage Area | Test Count |
|---|---|---|
| `AppSettingsTests.cs` | Model: defaults, validation, sanitization, clone, equality, toString | 48 |
| `ConfigurationServiceTests.cs` | Service: load, save, backup, restore, reset, export, import, recovery | 36 |
| `SettingsViewModelTests.cs` | ViewModel: all commands, change tracking, delegates, events, error paths | 57 |
| **Total** | | **141** |

---

## 9. File Manifest

### Source Files (10)
- `src/SystemTrayProcessManager.Core/Models/AppSettings.cs` (267 lines)
- `src/SystemTrayProcessManager.Core/Services/IConfigurationService.cs` (94 lines)
- `src/SystemTrayProcessManager.Infrastructure/Services/ConfigurationService.cs` (414 lines)
- `src/SystemTrayProcessManager.UI/ViewModels/SettingsViewModel.cs` (472 lines)
- `src/SystemTrayProcessManager.UI/Views/SettingsWindow.xaml` (256 lines)
- `src/SystemTrayProcessManager.UI/Views/SettingsWindow.xaml.cs` (101 lines)
- `src/SystemTrayProcessManager.UI/Views/FirstRunWizard.xaml` (188 lines)
- `src/SystemTrayProcessManager.UI/Views/FirstRunWizard.xaml.cs` (139 lines)
- `src/SystemTrayProcessManager.UI/App.xaml.cs` (modified - DI, startup, settings load, wizard)
- `src/SystemTrayProcessManager.Core/Services/ITrayIconService.cs` (modified - SettingsRequested event)

### Test Files (3)
- `tests/SystemTrayProcessManager.Tests/Core/AppSettingsTests.cs` (410 lines)
- `tests/SystemTrayProcessManager.Tests/Infrastructure/ConfigurationServiceTests.cs` (539 lines)
- `tests/SystemTrayProcessManager.Tests/UI/SettingsViewModelTests.cs` (636 lines)

---

**Design Status**: Complete  
**Author**: Claude (WPF Engineer Agent)  
**Date**: 2026-01-29
