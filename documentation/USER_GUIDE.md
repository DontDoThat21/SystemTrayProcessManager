# SystemTray Process Manager - User Guide

## Table of Contents

1. [Getting Started](#1-getting-started)
2. [System Tray Usage](#2-system-tray-usage)
3. [Process Management](#3-process-management)
4. [Window Operations](#4-window-operations)
5. [Audio Control](#5-audio-control)
6. [Hotkey Configuration](#6-hotkey-configuration)
7. [Smart Features](#7-smart-features)
8. [Settings](#8-settings)
9. [Troubleshooting](#9-troubleshooting)
10. [FAQ](#10-faq)

---

## 1. Getting Started

### System Requirements

- **Operating System**: Windows 10 or Windows 11 (64-bit)
- **Framework**: .NET 10 Runtime (included in portable version)
- **Memory**: 50 MB minimum
- **Disk Space**: 100 MB (portable version)

### Installation

#### Portable Version (Recommended)

1. Download `SystemTrayProcessManager-portable.zip` from the [Releases](https://github.com/DontDoThat21/SystemTrayProcessManager/releases) page
2. Extract to any folder (e.g., `C:\Tools\SystemTrayProcessManager`)
3. Run `SystemTrayProcessManager.exe`

No installation required - just extract and run!

#### First Launch

When you first run the application:

1. **First-Run Wizard** appears with:
   - Welcome message
   - Hotkey enable/disable option
   - Permission information
   - Completion confirmation

2. Application minimizes to **system tray** (notification area)
3. Look for the **blue "PM" icon** near your clock

---

## 2. System Tray Usage

### Tray Icon Behavior

| Action | Result |
|--------|--------|
| **Left-click** | Show/hide main window |
| **Right-click** | Open context menu |
| **Balloon notification** | Click to view details |

### Context Menu Options

- **Show Window** - Open the main dashboard
- **Hotkey Configuration** - Configure keyboard shortcuts
- **Settings** - Open application settings
- **Separator** - Divides menu sections
- **[Process List]** - Top 10 running processes (click to bring to front)
- **Exit** - Close the application completely

### Notifications

The application shows balloon notifications for:
- Application startup
- Hotkey actions performed
- Errors and warnings
- Process events (when enabled)

---

## 3. Process Management

### Main Dashboard

The main window displays running processes as cards in a grid layout.

#### Process Card Information

Each card shows:
- **Icon** - Application icon (40x40 pixels)
- **Name** - Process name
- **PID** - Process ID number
- **Window Title** - Current window title

#### Quick Actions

Hover over a card to reveal action buttons:

| Button | Action |
|--------|--------|
| 📌 (Pin) | Bring window to front |
| 🔈 (Speaker) | Toggle mute |
| ➖ (Minimize) | Minimize window |
| ✖ (Close) | Close process |

### Searching Processes

Use the search bar at the top to filter processes:
- Search by **process name** (e.g., "chrome")
- Search by **window title** (e.g., "GitHub")
- Search by **PID** (e.g., "1234")

Search is **case-insensitive** and updates as you type.

### Refreshing the List

- **Automatic**: Every 5 seconds (configurable in settings)
- **Manual**: Click the **Refresh** button (↻)

---

## 4. Window Operations

### Available Operations

| Operation | Description | Reversible |
|-----------|-------------|------------|
| **Bring to Front** | Activate and focus the window | No |
| **Minimize** | Minimize to taskbar | Yes (Restore) |
| **Maximize** | Maximize to full screen | Yes (Restore) |
| **Restore** | Return to normal size | No |
| **Close** | Close the window/process | No |
| **Hide** | Make window invisible | Yes (Show) |
| **Show** | Make hidden window visible | No |
| **Set Transparency** | Adjust window opacity (0-100%) | Yes |
| **Always on Top** | Pin window above others | Yes |

### Using Window Operations

**From Dashboard:**
1. Find the process card
2. Hover to reveal action buttons
3. Click the desired action

**Via Hotkeys:**
1. Configure a hotkey (see [Hotkey Configuration](#6-hotkey-configuration))
2. Press the hotkey combination
3. Action applies to focused window (Quick Action) or specific process (Pinned)

---

## 5. Audio Control

### Per-Process Volume

Control audio for individual applications:

**Mute/Unmute:**
- Click the speaker icon on any process card
- Icon changes to show muted state (🔇)

**Volume Adjustment:**
- Available via hotkey actions
- Set volume from 0% to 100%

### Audio Session Detection

The application monitors audio sessions in real-time:
- New audio sessions are detected automatically
- Device changes (plugging in headphones) are handled
- Processes without audio sessions show disabled controls

---

## 6. Hotkey Configuration

### Opening Configuration

Access via:
- Right-click tray icon → **Hotkey Configuration**
- Main window menu → **Configuration** → **Hotkeys**

### Creating a Hotkey

1. Click **Add New** button
2. Click in the **Key Capture** box
3. Press your desired key combination (e.g., `Ctrl+Alt+M`)
4. Select the **Action** (e.g., Toggle Mute)
5. Choose the **Mode**:
   - **Quick Action** - Affects currently focused window
   - **Pinned Process** - Targets a specific process name
6. If Pinned Process, enter the **Process Name** (e.g., "spotify")
7. Click **Save**

### Action Types

| Action | Description |
|--------|-------------|
| Mute | Mute process audio |
| Unmute | Unmute process audio |
| Toggle Mute | Switch mute state |
| Minimize | Minimize window |
| Maximize | Maximize window |
| Restore | Restore window size |
| Close | Close window |
| Bring To Front | Activate window |
| Hide | Hide window |
| Show | Show hidden window |

### Modifier Keys

Hotkeys support combinations of:
- **Ctrl** (Control)
- **Alt**
- **Shift**
- **Win** (Windows key)

Example combinations:
- `Ctrl+Alt+M`
- `Ctrl+Shift+F1`
- `Win+Alt+Up`

### Import/Export

**Export Configuration:**
1. Click **Export** button
2. Choose save location
3. Save as `.json` file

**Import Configuration:**
1. Click **Import** button
2. Select `.json` file
3. Existing bindings are replaced

### Conflict Detection

The application warns if:
- Hotkey is already registered by another binding
- Hotkey conflicts with a Windows system shortcut

---

## 7. Smart Features

### Gaming Mode

**Activate:** Configured via hotkey or settings

**What it does:**
- Suppresses notifications
- Optionally closes resource-heavy background apps
- Adjusts process priorities for games

### Auto-Mute on Fullscreen

When a fullscreen application is detected:
1. Background apps (Discord, Slack, Teams) are automatically muted
2. When you exit fullscreen, audio is restored

**Configure in:** Settings → Smart Features

### Focus History

Track and switch between recently focused windows:
- Maintains history of last 10 focused windows
- Configure hotkey to cycle through history

### Window Position Memory

Save and restore window layouts:
1. Arrange windows as desired
2. Use "Save Layout" option
3. Layouts are remembered per monitor configuration
4. Restore when monitors change

### Process Profiles

Automatically apply settings when apps launch:
- Window position and size
- Audio level
- Always-on-top state
- Transparency

---

## 8. Settings

### Accessing Settings

- Right-click tray icon → **Settings**
- Main window → **Settings** button

### Settings Categories

#### Hotkeys Tab
- **Enable Global Hotkeys** - Master switch for all hotkeys

#### Audio Tab
- **Default Volume** - Initial volume for new sessions (0-100%)
- **Mute on Minimize** - Auto-mute when window minimizes

#### Behavior Tab
- **Start with Windows** - Launch on system startup
- **Start Minimized** - Start directly to tray
- **Enable Notifications** - Show balloon notifications
- **Minimize to Tray** - Minimize to tray instead of taskbar
- **Process Refresh Interval** - Polling frequency (ms)

#### Appearance Tab
- **Theme** - Dark (default) or Light
- **Show Process Icons** - Display icons in process cards
- **Enable Animations** - Smooth transitions and effects

### Backup & Restore

**Auto-Backup:** Settings are backed up before every save

**Manual Restore:**
1. Click **Restore from Backup** in Settings
2. Confirms restoration from last backup

**Reset to Defaults:**
1. Click **Reset to Defaults** in Settings
2. All settings return to factory values

### Import/Export Settings

**Export:** Save all settings to a `.json` file for backup or sharing
**Import:** Load settings from a previously exported file

---

## 9. Troubleshooting

### Common Issues

#### Application Won't Start

**Symptom:** Double-clicking does nothing

**Solutions:**
1. Check if already running (look for tray icon)
2. Run as Administrator
3. Check Windows Event Viewer for errors
4. Delete `settings.json` to reset corrupted config

**Location:** `%LOCALAPPDATA%\SystemTrayProcessManager\settings.json`

#### Hotkeys Don't Work

**Symptom:** Pressing hotkey does nothing

**Solutions:**
1. Verify hotkeys are enabled in Settings
2. Check for conflicts with other applications
3. Run as Administrator for system-wide hooks
4. Try re-registering the hotkey

#### Audio Control Not Working

**Symptom:** Mute button doesn't affect audio

**Solutions:**
1. Check Windows Volume Mixer - is the app listed?
2. Restart the target application
3. Check if audio session exists (not all apps have one)
4. Restart SystemTray Process Manager

#### High Memory Usage

**Symptom:** Memory exceeds 100 MB

**Solutions:**
1. Increase process refresh interval (Settings → Behavior)
2. Check for memory leaks using Task Manager
3. Restart the application

### Viewing Logs

Logs are stored at:
```
%LOCALAPPDATA%\SystemTrayProcessManager\logs\
```

Log files:
- Named by date: `log-YYYYMMDD.txt`
- Kept for 7 days
- Maximum 50 MB per file

### Crash Reports

If the application crashes:

1. A crash report is generated automatically
2. Located at: `%LOCALAPPDATA%\SystemTrayProcessManager\crash-reports\`
3. Contains:
   - Error details
   - Stack trace
   - System diagnostics

**To report a crash:**
1. Find the crash report file
2. Open an issue on GitHub
3. Attach the crash report

---

## 10. FAQ

### General

**Q: Does this work with all Windows applications?**
A: Most applications are supported. Some system processes and UWP apps may have limited functionality.

**Q: Is administrator required?**
A: Not required for basic use. Administrator is needed for:
- Controlling elevated (admin) processes
- Some window operations on protected windows

**Q: How much resources does it use?**
A: Minimal - typically <25 MB memory and <0.5% CPU when idle.

### Hotkeys

**Q: Can I use function keys (F1-F12)?**
A: Yes, with or without modifiers.

**Q: Do hotkeys work in games?**
A: Yes, the low-level keyboard hook works system-wide, including fullscreen games.

**Q: What if a hotkey conflicts?**
A: The application warns you and the conflicting hotkey won't be registered.

### Audio

**Q: Why can't I control some apps' audio?**
A: Some applications don't create audio sessions until they actually play sound. Try playing audio first.

**Q: Does it work with Bluetooth headphones?**
A: Yes, the application handles audio device changes automatically.

### Window Management

**Q: Can I control windows on multiple monitors?**
A: Yes, window operations work across all connected monitors.

**Q: Why can't I control some windows?**
A: System windows (like Task Manager when run elevated) require administrator privileges.

---

## Getting Help

- **Documentation**: [README.md](../README.md)
- **Architecture**: [ARCHITECTURE.md](ARCHITECTURE.md)
- **Issues**: [GitHub Issues](https://github.com/DontDoThat21/SystemTrayProcessManager/issues)

---

*Last updated: 2026-01-29*
