# Task 14: Documentation & Distribution - Design Document

## Document Overview
**Task**: Task 14: Documentation & Distribution (Phase 5.3)  
**Priority**: High  
**Estimated Time**: 4-5 hours  
**Dependencies**: All previous tasks (Tasks 1-13)  
**Created**: 2026-01-29  
**Author**: WPF Engineer Agent

---

## 1. Task Overview

### 1.1 Objective
Create comprehensive documentation and distribution packages for portfolio-quality presentation:
- Professional README.md with project overview, features, and screenshots
- USER_GUIDE.md with detailed usage instructions
- ARCHITECTURE.md with system design documentation
- Inline UI tooltips and help integration
- Portable single-file executable configuration
- GitHub release preparation with versioning

### 1.2 Scope
**In Scope**:
- `README.md` - Project overview, features, installation, build instructions
- `USER_GUIDE.md` - Getting started, configuration, troubleshooting
- `ARCHITECTURE.md` - System diagrams, design patterns, API details
- UI tooltip service and inline help infrastructure
- Publish profile for single-file portable executable
- Version information and assembly metadata
- GitHub release tag preparation (v1.0.0)
- Unit tests for new components

**Out of Scope**:
- WiX/Inno Setup installer (separate task if needed)
- Demo video recording
- Actual GitHub release publishing (manual step)
- CI/CD pipeline setup

### 1.3 Success Criteria
- [ ] README.md with professional formatting and feature list
- [ ] USER_GUIDE.md with comprehensive documentation
- [ ] ARCHITECTURE.md with diagrams and patterns
- [ ] ITooltipService interface and implementation
- [ ] TooltipHelper for XAML tooltip assignment
- [ ] Publish profile for single-file executable
- [ ] Assembly version set to 1.0.0
- [ ] All unit tests pass
- [ ] No compiler warnings

---

## 2. Architecture

### 2.1 New Components

```
Documentation/
+-- README.md                       Project overview and quick start
+-- USER_GUIDE.md                   Detailed user documentation
+-- ARCHITECTURE.md                 Technical architecture documentation

Core/Services/
+-- ITooltipService.cs              Interface for tooltip text management

Infrastructure/Services/
+-- TooltipService.cs               Implementation with resource-based strings

UI/Helpers/
+-- TooltipHelper.cs                Attached property for XAML tooltip binding

UI/Resources/
+-- TooltipStrings.resx             Localizable tooltip strings

Publish Profiles/
+-- PortableRelease.pubxml          Single-file publish configuration
```

### 2.2 Documentation Structure

```
README.md
├── Banner/Logo placeholder
├── Description & Purpose
├── Features (organized by phase)
├── Screenshots placeholder
├── Installation
│   ├── Portable version
│   └── Build from source
├── Technologies Used
├── Project Structure
├── Contributing (placeholder)
└── License (MIT)

USER_GUIDE.md
├── Getting Started
│   ├── System Requirements
│   ├── Installation
│   └── First Launch
├── System Tray Usage
│   ├── Icon behavior
│   ├── Context menu
│   └── Notifications
├── Process Management
│   ├── Viewing processes
│   ├── Window operations
│   └── Audio control
├── Hotkey Configuration
│   ├── Creating hotkeys
│   ├── Action types
│   └── Import/Export
├── Settings
│   ├── Categories
│   └── Backup/Restore
├── Troubleshooting
│   ├── Common issues
│   ├── Crash reports
│   └── Logs location
└── FAQ

ARCHITECTURE.md
├── Overview & Goals
├── Three-Layer Architecture
│   ├── Core (interfaces, models)
│   ├── Infrastructure (services)
│   └── UI (WPF, MVVM)
├── Design Patterns
│   ├── MVVM
│   ├── Dependency Injection
│   ├── Service Pattern
│   └── Factory Methods
├── Windows API Integration
│   ├── P/Invoke declarations
│   ├── Window manipulation
│   ├── Audio control (NAudio)
│   └── Keyboard hooks
├── Key Services
│   ├── ProcessMonitorService
│   ├── WindowManipulationService
│   ├── AudioManagerService
│   ├── HotkeyManagerService
│   └── ActionMappingService
└── Data Flow Diagrams
```

---

## 3. Service Design

### 3.1 ITooltipService Interface

```csharp
namespace SystemTrayProcessManager.Core.Services
{
    /// <summary>
    /// Service for managing application tooltip text.
    /// </summary>
    public interface ITooltipService
    {
        /// <summary>
        /// Gets tooltip text for a specific key.
        /// </summary>
        string GetTooltip(string key);
        
        /// <summary>
        /// Gets tooltip with formatted parameters.
        /// </summary>
        string GetTooltip(string key, params object[] args);
        
        /// <summary>
        /// Checks if a tooltip exists for the key.
        /// </summary>
        bool HasTooltip(string key);
    }
}
```

### 3.2 TooltipService Implementation

```csharp
namespace SystemTrayProcessManager.Infrastructure.Services
{
    /// <summary>
    /// Tooltip service using embedded resource strings.
    /// </summary>
    public class TooltipService : ITooltipService
    {
        private readonly Dictionary<string, string> _tooltips;
        private readonly ILogger<TooltipService> _logger;
        
        public TooltipService(ILogger<TooltipService> logger)
        {
            _logger = logger;
            _tooltips = InitializeTooltips();
        }
        
        private static Dictionary<string, string> InitializeTooltips()
        {
            return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                // Process management
                ["ProcessCard.BringToFront"] = "Bring this window to the foreground",
                ["ProcessCard.Minimize"] = "Minimize this window",
                ["ProcessCard.Maximize"] = "Maximize this window",
                ["ProcessCard.Close"] = "Close this process",
                ["ProcessCard.Mute"] = "Mute/unmute audio for this process",
                
                // Hotkey configuration
                ["Hotkey.Capture"] = "Click and press your desired key combination",
                ["Hotkey.Conflict"] = "This hotkey conflicts with another binding",
                ["Hotkey.Import"] = "Import hotkey configuration from a JSON file",
                ["Hotkey.Export"] = "Export hotkey configuration to a JSON file",
                
                // Settings
                ["Settings.StartWithWindows"] = "Launch application when Windows starts",
                ["Settings.MinimizeToTray"] = "Minimize to system tray instead of taskbar",
                ["Settings.HotkeysEnabled"] = "Enable or disable all global hotkeys",
                ["Settings.DefaultVolume"] = "Default volume level for new audio sessions",
                ["Settings.ProcessRefresh"] = "How often to refresh the process list (milliseconds)",
                
                // Main window
                ["Main.Search"] = "Search by process name, window title, or PID",
                ["Main.Refresh"] = "Refresh the process list immediately",
                ["Main.Notifications"] = "View recent application notifications",
                
                // General
                ["General.Exit"] = "Exit the application completely"
            };
        }
        
        public string GetTooltip(string key)
        {
            if (_tooltips.TryGetValue(key, out var tooltip))
                return tooltip;
            
            _logger.LogWarning("Tooltip not found for key: {Key}", key);
            return string.Empty;
        }
        
        public string GetTooltip(string key, params object[] args)
        {
            var template = GetTooltip(key);
            if (string.IsNullOrEmpty(template))
                return string.Empty;
            
            try
            {
                return string.Format(template, args);
            }
            catch (FormatException ex)
            {
                _logger.LogError(ex, "Failed to format tooltip {Key}", key);
                return template;
            }
        }
        
        public bool HasTooltip(string key) => _tooltips.ContainsKey(key);
    }
}
```

### 3.3 TooltipHelper Attached Property

```csharp
namespace SystemTrayProcessManager.UI.Helpers
{
    /// <summary>
    /// Attached property for binding tooltips via ITooltipService.
    /// </summary>
    public static class TooltipHelper
    {
        private static ITooltipService? _tooltipService;
        
        public static void Initialize(ITooltipService tooltipService)
        {
            _tooltipService = tooltipService;
        }
        
        public static readonly DependencyProperty TooltipKeyProperty =
            DependencyProperty.RegisterAttached(
                "TooltipKey",
                typeof(string),
                typeof(TooltipHelper),
                new PropertyMetadata(null, OnTooltipKeyChanged));
        
        public static string GetTooltipKey(DependencyObject obj)
            => (string)obj.GetValue(TooltipKeyProperty);
        
        public static void SetTooltipKey(DependencyObject obj, string value)
            => obj.SetValue(TooltipKeyProperty, value);
        
        private static void OnTooltipKeyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is FrameworkElement element && e.NewValue is string key && _tooltipService != null)
            {
                var tooltip = _tooltipService.GetTooltip(key);
                if (!string.IsNullOrEmpty(tooltip))
                {
                    element.ToolTip = tooltip;
                }
            }
        }
    }
}
```

---

## 4. Publish Configuration

### 4.1 Single-File Publish Profile

```xml
<!-- Properties/PublishProfiles/PortableRelease.pubxml -->
<?xml version="1.0" encoding="utf-8"?>
<Project ToolsVersion="4.0" xmlns="http://schemas.microsoft.com/developer/msbuild/2003">
  <PropertyGroup>
    <Configuration>Release</Configuration>
    <Platform>Any CPU</Platform>
    <PublishDir>bin\Publish\Portable\</PublishDir>
    <PublishProtocol>FileSystem</PublishProtocol>
    <TargetFramework>net10.0-windows</TargetFramework>
    <RuntimeIdentifier>win-x64</RuntimeIdentifier>
    <SelfContained>true</SelfContained>
    <PublishSingleFile>true</PublishSingleFile>
    <PublishReadyToRun>true</PublishReadyToRun>
    <IncludeNativeLibrariesForSelfExtract>true</IncludeNativeLibrariesForSelfExtract>
    <EnableCompressionInSingleFile>true</EnableCompressionInSingleFile>
  </PropertyGroup>
</Project>
```

### 4.2 Version Configuration

```xml
<!-- Added to UI project file -->
<PropertyGroup>
  <Version>1.0.0</Version>
  <AssemblyVersion>1.0.0.0</AssemblyVersion>
  <FileVersion>1.0.0.0</FileVersion>
  <Product>SystemTray Process Manager</Product>
  <Company>Portfolio Project</Company>
  <Copyright>Copyright © 2026</Copyright>
  <Description>Advanced Windows process management with global hotkeys and audio control</Description>
</PropertyGroup>
```

---

## 5. Test Plan

### 5.1 TooltipService Tests
| Test | Description |
|------|-------------|
| GetTooltip_ValidKey_ReturnsText | Returns correct tooltip for known key |
| GetTooltip_InvalidKey_ReturnsEmpty | Returns empty string for unknown key |
| GetTooltip_WithParams_FormatsCorrectly | String.Format with parameters works |
| HasTooltip_ValidKey_ReturnsTrue | Correctly identifies existing keys |
| HasTooltip_InvalidKey_ReturnsFalse | Correctly identifies missing keys |
| GetTooltip_CaseInsensitive | Key lookup is case-insensitive |

### 5.2 TooltipHelper Tests
| Test | Description |
|------|-------------|
| Initialize_SetsService | Service is stored correctly |
| SetTooltipKey_ValidKey_SetsTooltip | FrameworkElement gets tooltip |
| SetTooltipKey_InvalidKey_NoTooltip | No tooltip set for unknown key |
| SetTooltipKey_BeforeInit_NoException | Graceful handling before init |

---

## 6. Implementation Order

1. **Documentation First**:
   - Create README.md
   - Create USER_GUIDE.md
   - Create ARCHITECTURE.md

2. **UI Tooltip Infrastructure**:
   - Create ITooltipService interface
   - Implement TooltipService
   - Create TooltipHelper attached property
   - Register in DI container

3. **Distribution Setup**:
   - Add version metadata to project file
   - Create publish profile
   - Test single-file publish

4. **Testing**:
   - Unit tests for TooltipService
   - Build verification

5. **Finalization**:
   - Update app.spec.features.status.md
   - Create summary document

---

## 7. Risk Assessment

| Risk | Probability | Impact | Mitigation |
|------|-------------|--------|------------|
| Single-file size too large | Low | Low | Compression enabled, trimming optional |
| Tooltip resource localization | Low | Low | Dictionary-based approach extensible |
| Missing documentation coverage | Medium | Medium | Follow structured outline |

---

## 8. Timeline Estimate

| Phase | Duration |
|-------|----------|
| Documentation (README, USER_GUIDE, ARCH) | 2 hours |
| Tooltip service implementation | 30 minutes |
| Publish profile and versioning | 30 minutes |
| Unit tests | 30 minutes |
| Integration and review | 30 minutes |
| **Total** | **4 hours** |
