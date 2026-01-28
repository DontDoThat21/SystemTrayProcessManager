# SystemTrayProcessManager - Feature Specification

## Project Overview
**Name**: SystemTrayProcessManager  
**Type**: WPF System Tray Application  
**Purpose**: Portfolio project demonstrating advanced Windows API integration, process management, and global hotkey system  
**Tech Stack**: WPF, C# 12, .NET 8, CommunityToolkit.Mvvm, NAudio, Serilog

---

## Feature Catalog

### Phase 1: Core Infrastructure

#### 1.1 Project Setup & Architecture
- Visual Studio solution with three projects (Core, Infrastructure, UI)
- Dependency injection container setup
- Serilog file logging to AppData
- Single instance application using Mutex
- Application lifecycle management (startup, shutdown, crash handling)
- Organized folder structure per architecture guidelines

#### 1.2 System Tray Integration
- System tray icon with NotifyIcon
- Dynamic context menu showing process list
- Tray icon animations for action feedback
- Balloon notifications for user feedback
- Left-click to show main window
- Right-click for context menu
- Exit option with proper cleanup

---

### Phase 2: Process Management Core

#### 2.1 Process Discovery & Monitoring
- Real-time process enumeration
- Process filtering (exclude system processes, show only windowed apps)
- Process change detection (started/stopped events)
- Icon extraction and caching
- Search and filter functionality
- Background polling (5 second interval)
- ProcessInfo model with Name, PID, WindowTitle, Icon, Path, WindowHandle

#### 2.2 Window Manipulation Features
- Bring window to front
- Minimize window
- Maximize window
- Close window (graceful and forced)
- Hide window
- Show hidden window
- Set window transparency (0-255 alpha)
- Set always on top flag
- Windows API P/Invoke integration
- Comprehensive error handling with Win32 error codes

#### 2.3 Audio Control Integration
- Mute process audio
- Unmute process audio
- Set process volume (0.0 - 1.0)
- Get process volume level
- Check if process is muted
- NAudio integration for per-process audio control
- Audio session event handling
- Proper COM object disposal

---

### Phase 3: Global Hotkey System

#### 3.1 Low-Level Keyboard Hook
- System-wide keyboard hook (WH_KEYBOARD_LL)
- Hotkey registration system
- Support for modifier keys (Ctrl, Alt, Shift, Win)
- Key combination tracking
    - Conflict detection
        - Optional key suppression
            - Asynchronous action execution
                - Fallback to RegisterHotKey API
- Performance target: <50ms response time

#### 3.2 Hotkey Configuration UI
- Visual hotkey capture control
- Display current hotkey bindings
- Add, edit, and delete bindings
- Preset configurations
- Import/export hotkeys (JSON format)
- Conflict validation with system shortcuts
- Warning display for conflicting bindings
- Reset to defaults functionality

#### 3.3 Action Mapping System
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

---

### Phase 4: Advanced Features

#### 4.1 Process Profiles & Automation
- Process profile system (window position, audio level, always on top)
- Save and load profiles per process
- Auto-load profiles on process detection
- Scheduled actions with timer system
- Startup process launcher
- Process groups for batch operations (close all, mute all)
- Profile management UI

#### 4.2 Smart Features
- **Focus History**: Track and switch to recently focused processes
- **Auto-Mute on Fullscreen**: Detect fullscreen games and auto-mute background apps (Discord, Slack)
- **Window Position Memory**: Save and restore window positions per monitor configuration
- **Gaming Mode**: One-click performance mode with notification suppression
- **Process Priority Adjustment**: Change CPU priority levels
- **Startup with Windows**: Registry integration for auto-start

#### 4.3 Visual Enhancements
- Dark theme with custom color palette
- Modern button and window styles
- ProcessCard custom control with icon, name, PID, and quick actions
- Main dashboard with grid/card layout
- Smooth animations (fade in/out, slide)
- Search and filter bar
- In-app notification panel for process events
- Hotkey feedback overlay (OBS-style)
- Optional CPU/Memory usage graphs

---

### Phase 5: Polish & Portfolio Readiness

#### 5.1 Settings & Persistence
- JSON-based settings storage in AppData
- Settings categories:
  - Hotkeys configuration
  - Audio defaults
  - Behavior (startup, notifications)
  - Appearance (theme selection)
- Configuration backup and restore
- First-run wizard with welcome and setup screens
- Reset to defaults functionality

#### 5.2 Error Handling & Stability
- Comprehensive exception handling across all services
- User-friendly error messages
- Permission elevation detection and prompts
- Edge case handling:
  - Process terminated during operation
  - Invalid window handles
  - Missing audio devices
  - Corrupted configuration files
- Crash reporter with diagnostic logging
- Automated recovery for corrupted settings
- UAC compatibility testing

#### 5.3 Documentation & Distribution
- README.md with project overview, screenshots, features, installation
- USER_GUIDE.md with getting started, configuration, and troubleshooting
- ARCHITECTURE.md with system diagrams and design patterns
- Inline tooltips and help buttons
- Installer package (WiX Toolset or Inno Setup)
- Portable single-file executable
- GitHub releases with versioned tags
- Demo video (optional)

#### 5.4 Performance Optimization
- Throttled process monitoring (5-10 second intervals)
- Process information and icon caching
- Background threading for expensive operations
- Optimized Win32 API call patterns
- Memory leak prevention
- Lazy loading of services
- Performance targets:
  - Startup time: <2 seconds
  - Memory usage: <50 MB idle
  - CPU usage: <1% idle
  - Hotkey response: <50ms

---

## Bonus Features (Portfolio Standouts)

### Plugin System (Advanced)
- MEF-based plugin architecture
- Plugin API and lifecycle management
- Plugin discovery system
- Sample plugins:
  - Spotify controller
  - Discord RPC integration
  - Screenshot tool
- Plugin development guide and template

### Multi-Monitor Support
- Monitor configuration change detection
- Hotkeys for moving windows between monitors
- Monitor-specific window position profiles
- Auto-restore on monitor configuration change

### Command Line Interface
- CLI argument parsing
- Commands:
  - `mute <process>`
  - `unmute <process>`
  - `close <process>`
  - `list`
- PowerShell automation support
- JSON output for scripting
- Help documentation

---

## Technical Architecture

### Project Structure
```
SystemTrayProcessManager.sln
├── SystemTrayProcessManager.Core (Class Library)
│   ├── Models/
│   ├── Services/ (Interfaces)
│   └── Enums/
├── SystemTrayProcessManager.Infrastructure (Class Library)
│   ├── Services/ (Implementations)
│   ├── WindowsAPI/
│   └── Helpers/
└── SystemTrayProcessManager.UI (WPF Application)
    ├── Views/
    ├── ViewModels/
    ├── Controls/
    ├── Resources/
    └── Services/
```

### Key Technologies
- **Framework**: .NET 8, C# 12
- **UI**: WPF with MVVM pattern
- **DI Container**: Microsoft.Extensions.DependencyInjection
- **MVVM**: CommunityToolkit.Mvvm
- **Audio**: NAudio
- **Logging**: Serilog
- **Windows API**: P/Invoke for Win32 interop

### Design Patterns
- MVVM (Model-View-ViewModel)
- Dependency Injection
- Repository Pattern (for settings/profiles)
- Observer Pattern (for process events)
- Command Pattern (for actions)
- Factory Pattern (for services)

---

## Performance Requirements
- **Startup Time**: <2 seconds
- **Memory Usage**: <50 MB idle
- **CPU Usage**: <1% idle
- **Hotkey Response**: <50ms from keypress to action
- **Process Poll Interval**: 5 seconds (configurable)

---

## Security Considerations
- Run with minimum required permissions
- Elevate to admin only when needed for protected processes
- No credential storage
- Safe handling of process termination
- Validation of all user inputs
- Secure settings storage

---

**Version**: 1.0.0  
**Last Updated**: 2025  
**Status**: Reference Document
