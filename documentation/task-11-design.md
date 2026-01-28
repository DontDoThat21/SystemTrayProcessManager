# Task 11: Visual Enhancements - Design Document

## Task Overview
**Task**: 4.3 Visual Enhancements (Task 11)  
**Phase**: 4 - Advanced Features  
**Priority**: Medium  
**Dependencies**: Task 2 (System Tray), Task 3 (Process Discovery)  
**Estimated Time**: 5-6 hours

## Objectives
Transform the placeholder MainWindow into a fully-featured modern dashboard with dark theme, process cards, animations, notification system, and hotkey feedback overlay.

## Feature Requirements

### 1. Dark Theme (`DarkTheme.xaml`)
- Custom color palette with primary (#0078D4), background (#1E1E1E), surface (#252526), card (#2D2D30)
- Brush resources for all UI elements
- Font resources for consistent typography

### 2. Modern Styles
- **ButtonStyles.xaml**: Primary, secondary, danger, icon-only button styles with hover/press animations
- **WindowStyles.xaml**: Custom window chrome styles, borderless modern look

### 3. ProcessCard Custom Control
- Displays process icon, name, PID, window title
- Quick action buttons (mute/unmute, minimize, close)
- Hover animation with subtle elevation effect
- Data-bound to ProcessCardViewModel

### 4. Main Dashboard (MainWindow rewrite)
- Grid/card layout using WrapPanel for process cards
- Search/filter bar with real-time filtering
- Refresh button and process count indicator
- Loading state indicator
- Fully MVVM data-bound via MainViewModel

### 5. MainViewModel
- ObservableCollection of ProcessCardViewModel
- Search/filter text with reactive filtering
- Commands: Refresh, MuteProcess, CloseProcess, MinimizeProcess, BringToFront
- Process event subscription (started/stopped)
- Loading state management

### 6. Smooth Animations
- Fade-in on window show
- Card hover elevation (scale transform)
- Loading spinner animation
- Notification slide-in/fade-out

### 7. In-App Notification Panel
- INotificationService interface in Core
- NotificationService implementation tracking process events
- NotificationPanel control with slide-in animation
- Auto-dismiss after 3 seconds

### 8. Hotkey Feedback Overlay
- Transparent, topmost popup window
- Shows pressed hotkey combination text
- Fades out after 2 seconds
- OBS-style rounded rectangle

## Architecture

### New Files
```
Core/
  Services/INotificationService.cs
  Models/AppNotification.cs

UI/
  ViewModels/MainViewModel.cs
  ViewModels/ProcessCardViewModel.cs
  ViewModels/NotificationViewModel.cs
  Controls/ProcessCard.cs
  Controls/HotkeyFeedbackOverlay.xaml + .cs
  Controls/NotificationPanel.xaml + .cs
  Resources/Themes/DarkTheme.xaml
  Resources/Styles/ButtonStyles.xaml
  Resources/Styles/WindowStyles.xaml
  Resources/Styles/ProcessCardStyle.xaml
  Converters/InverseBoolConverter.cs
  Converters/BoolToMuteIconConverter.cs

Tests/
  UI/MainViewModelTests.cs
  UI/ProcessCardViewModelTests.cs
  UI/NotificationViewModelTests.cs
  Core/AppNotificationTests.cs
```

### Modified Files
```
UI/MainWindow.xaml - Complete rewrite
UI/MainWindow.xaml.cs - ViewModel injection
UI/App.xaml - Merge new resource dictionaries
UI/App.xaml.cs - Register new services and ViewModels
```

### Data Flow
```
ProcessMonitorService --> MainViewModel --> ProcessCardViewModels --> ProcessCard controls
                     --> NotificationViewModel --> NotificationPanel
HotkeyService --> HotkeyFeedbackOverlay
```

## Color Palette (Dark Theme)
| Token | Hex | Usage |
|-------|-----|-------|
| Background | #1E1E1E | Window background |
| Surface | #252526 | Panels, sidebars |
| Card | #2D2D30 | Cards, elevated surfaces |
| CardHover | #3E3E42 | Card hover state |
| Primary | #0078D4 | Accent, active elements |
| PrimaryHover | #106EBE | Primary hover |
| Danger | #E81123 | Close buttons, errors |
| DangerHover | #C50F1F | Danger hover |
| Warning | #FFB900 | Warnings |
| Success | #107C10 | Success indicators |
| TextPrimary | #CCCCCC | Main text |
| TextSecondary | #969696 | Secondary text |
| TextDisabled | #5D5D5D | Disabled text |
| Border | #3F3F46 | Borders |
| Divider | #333337 | Separators |

## Success Criteria
- [x] Dark theme applied consistently across all windows
- [x] ProcessCard shows icon, name, PID, quick actions
- [x] Main dashboard with card grid and search bar
- [x] Smooth hover animations on cards
- [x] In-app notification panel for events
- [x] Hotkey feedback overlay (OBS-style)
- [x] All code compiles with zero warnings
- [x] Unit tests with xUnit coverage
- [x] MVVM pattern strictly followed
