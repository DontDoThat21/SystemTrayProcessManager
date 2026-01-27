# SystemTrayProcessManager WPF Development Skill

## Overview
This skill guides development of SystemTrayProcessManager, a sophisticated WPF system tray application for process management with global hotkeys, audio control, and window manipulation. The project demonstrates advanced Windows API integration, modern C# patterns, and professional WPF architecture for portfolio presentation.

## Core Development Principles

### Architecture Philosophy
- **MVVM Pattern**: Strict separation between Views, ViewModels, and Models with CommunityToolkit.Mvvm
- **Dependency Injection**: Use Microsoft.Extensions.DependencyInjection for loose coupling and testability
- **Single Responsibility**: Each class has one clear, well-defined purpose
- **Interface-Based Design**: Program to interfaces, not concrete implementations
- **Async-First**: All I/O and expensive operations must be asynchronous (use async/await)
- **Clean Code**: Self-documenting code with XML comments for public APIs

### Required Project Structure
```
SystemTrayProcessManager/
├── SystemTrayProcessManager.sln
├── src/
│   ├── SystemTrayProcessManager.Core/              # Business logic, models, interfaces
│   │   ├── Models/
│   │   │   ├── ProcessInfo.cs
│   │   │   ├── HotkeyBinding.cs
│   │   │   ├── ProcessAction.cs
│   │   │   └── AppSettings.cs
│   │   ├── Services/
│   │   │   ├── IProcessService.cs
│   │   │   ├── IHotkeyService.cs
│   │   │   ├── IAudioService.cs
│   │   │   ├── IWindowService.cs
│   │   │   ├── IConfigurationService.cs
│   │   │   └── INotificationService.cs
│   │   ├── Enums/
│   │   │   ├── ProcessActionType.cs
│   │   │   ├── HotkeyModifier.cs
│   │   │   └── WindowState.cs
│   │   └── Extensions/
│   │       └── ProcessExtensions.cs
│   │
│   ├── SystemTrayProcessManager.Infrastructure/    # Windows API, implementations
│   │   ├── WindowsAPI/
│   │   │   ├── NativeMethods.cs                   # P/Invoke declarations
│   │   │   ├── Constants.cs                       # Win32 constants
│   │   │   ├── Structures.cs                      # Win32 structures
│   │   │   └── SafeHandles/                       # SafeHandle implementations
│   │   ├── Services/
│   │   │   ├── WindowManipulationService.cs
│   │   │   ├── HotkeyManagerService.cs
│   │   │   ├── AudioManagerService.cs
│   │   │   ├── ProcessMonitorService.cs
│   │   │   └── ConfigurationService.cs
│   │   └── Helpers/
│   │       ├── IconExtractor.cs
│   │       └── ProcessHelper.cs
│   │
│   ├── SystemTrayProcessManager.UI/               # WPF application
│   │   ├── App.xaml / App.xaml.cs
│   │   ├── ViewModels/
│   │   │   ├── MainViewModel.cs
│   │   │   ├── SettingsViewModel.cs
│   │   │   ├── HotkeyConfigViewModel.cs
│   │   │   ├── ProcessListViewModel.cs
│   │   │   └── TrayIconViewModel.cs
│   │   ├── Views/
│   │   │   ├── MainWindow.xaml
│   │   │   ├── SettingsWindow.xaml
│   │   │   ├── HotkeyConfigWindow.xaml
│   │   │   └── FirstRunWizard.xaml
│   │   ├── Controls/
│   │   │   ├── ProcessCard.xaml                   # Custom control for process display
│   │   │   ├── HotkeyCaptureBox.xaml              # Hotkey input control
│   │   │   └── VolumeSlider.xaml
│   │   ├── Converters/
│   │   │   ├── BoolToVisibilityConverter.cs
│   │   │   ├── ProcessToIconConverter.cs
│   │   │   └── BytesToMBConverter.cs
│   │   ├── Resources/
│   │   │   ├── Styles/
│   │   │   │   ├── ButtonStyles.xaml
│   │   │   │   ├── WindowStyles.xaml
│   │   │   │   └── Colors.xaml
│   │   │   ├── Themes/
│   │   │   │   └── DarkTheme.xaml
│   │   │   └── Icons/
│   │   │       └── tray-icon.ico
│   │   └── Services/
│   │       └── TrayIconService.cs                 # UI-specific tray management
│   │
│   └── SystemTrayProcessManager.Tests/
│       ├── Core/
│       │   └── Services/
│       └── Infrastructure/
│           └── Services/
│
├── docs/
│   ├── README.md
│   ├── USER_GUIDE.md
│   ├── ARCHITECTURE.md
│   └── screenshots/
│
└── app.spec.features.status.md                    # Task tracking document
```

## Critical Implementation Guidelines

### 1. Windows API Integration

#### P/Invoke Declaration Standards
```csharp
// File: NativeMethods.cs
using System;
using System.Runtime.InteropServices;

namespace SystemTrayProcessManager.Infrastructure.WindowsAPI
{
    /// <summary>
    /// Native Windows API methods for window and process manipulation
    /// </summary>
    internal static class NativeMethods
    {
        // ALWAYS use these conventions:
        // - internal visibility (not public)
        // - SetLastError = true for debugging
        // - Explicit calling convention
        // - Proper marshaling attributes
        
        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool SetForegroundWindow(IntPtr hWnd);
        
        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);
        
        [DllImport("user32.dll", SetLastError = true)]
        internal static extern IntPtr SetWindowsHookEx(
            int idHook,
            LowLevelKeyboardProc lpfn,
            IntPtr hMod,
            uint dwThreadId);
        
        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool UnhookWindowsHookEx(IntPtr hhk);
        
        [DllImport("user32.dll")]
        internal static extern IntPtr CallNextHookEx(
            IntPtr hhk,
            int nCode,
            IntPtr wParam,
            IntPtr lParam);
        
        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);
        
        [DllImport("user32.dll", SetLastError = true)]
        internal static extern int GetWindowLong(IntPtr hWnd, int nIndex);
        
        [DllImport("user32.dll", SetLastError = true)]
        internal static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);
        
        [DllImport("user32.dll")]
        internal static extern bool SetLayeredWindowAttributes(
            IntPtr hwnd,
            uint crKey,
            byte bAlpha,
            uint dwFlags);
        
        [DllImport("user32.dll")]
        internal static extern IntPtr SendMessage(
            IntPtr hWnd,
            uint Msg,
            IntPtr wParam,
            IntPtr lParam);
    }
    
    // Delegate for keyboard hook
    internal delegate IntPtr LowLevelKeyboardProc(
        int nCode, 
        IntPtr wParam, 
        IntPtr lParam);
}
```

#### Constants Organization
```csharp
// File: Constants.cs
namespace SystemTrayProcessManager.Infrastructure.WindowsAPI
{
    /// <summary>
    /// Windows API constants
    /// </summary>
    internal static class Constants
    {
        // ShowWindow commands
        internal const int SW_HIDE = 0;
        internal const int SW_SHOWNORMAL = 1;
        internal const int SW_SHOWMINIMIZED = 2;
        internal const int SW_SHOWMAXIMIZED = 3;
        internal const int SW_RESTORE = 9;
        internal const int SW_SHOW = 5;
        
        // Window messages
        internal const uint WM_CLOSE = 0x0010;
        internal const uint WM_KEYDOWN = 0x0100;
        internal const uint WM_KEYUP = 0x0101;
        
        // Hook types
        internal const int WH_KEYBOARD_LL = 13;
        
        // Window styles
        internal const int GWL_EXSTYLE = -20;
        internal const int WS_EX_LAYERED = 0x80000;
        internal const int WS_EX_TOPMOST = 0x00000008;
        
        // Layered window attributes
        internal const uint LWA_ALPHA = 0x2;
    }
    
    [StructLayout(LayoutKind.Sequential)]
    internal struct RECT
    {
        internal int Left;
        internal int Top;
        internal int Right;
        internal int Bottom;
    }
    
    [StructLayout(LayoutKind.Sequential)]
    internal struct KBDLLHOOKSTRUCT
    {
        internal int vkCode;
        internal int scanCode;
        internal int flags;
        internal int time;
        internal IntPtr dwExtraInfo;
    }
}
```

#### Safe Error Handling Pattern
```csharp
// File: WindowManipulationService.cs
using Microsoft.Extensions.Logging;
using System;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace SystemTrayProcessManager.Infrastructure.Services
{
    public class WindowManipulationService : IWindowService
    {
        private readonly ILogger<WindowManipulationService> _logger;
        
        public WindowManipulationService(ILogger<WindowManipulationService> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }
        
        public async Task<bool> BringToFrontAsync(IntPtr windowHandle)
        {
            return await Task.Run(() =>
            {
                try
                {
                    if (windowHandle == IntPtr.Zero)
                    {
                        _logger.LogWarning("Invalid window handle provided");
                        return false;
                    }

                    // First restore if minimized
                    if (!NativeMethods.ShowWindow(windowHandle, Constants.SW_RESTORE))
                    {
                        LogWin32Error("ShowWindow");
                    }
                    
                    // Bring to front
                    if (!NativeMethods.SetForegroundWindow(windowHandle))
                    {
                        LogWin32Error("SetForegroundWindow");
                        return false;
                    }
                    
                    _logger.LogDebug("Window {Handle} brought to front successfully", windowHandle);
                    return true;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to bring window {Handle} to front", windowHandle);
                    return false;
                }
            });
        }
        
        public async Task<bool> MinimizeWindowAsync(IntPtr windowHandle)
        {
            return await Task.Run(() =>
            {
                try
                {
                    if (windowHandle == IntPtr.Zero) return false;
                    
                    return NativeMethods.ShowWindow(windowHandle, Constants.SW_SHOWMINIMIZED);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to minimize window");
                    return false;
                }
            });
        }
        
        public async Task<bool> CloseWindowAsync(IntPtr windowHandle, bool force = false)
        {
            return await Task.Run(() =>
            {
                try
                {
                    if (windowHandle == IntPtr.Zero) return false;
                    
                    // Try graceful close first
                    NativeMethods.SendMessage(windowHandle, Constants.WM_CLOSE, IntPtr.Zero, IntPtr.Zero);
                    
                    // If force is true and window still exists, terminate process
                    if (force)
                    {
                        // TODO: Implement force kill logic
                        _logger.LogWarning("Force close requested for window {Handle}", windowHandle);
                    }
                    
                    return true;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to close window");
                    return false;
                }
            });
        }
        
        public async Task<bool> SetAlwaysOnTopAsync(IntPtr windowHandle, bool alwaysOnTop)
        {
            return await Task.Run(() =>
            {
                try
                {
                    if (windowHandle == IntPtr.Zero) return false;
                    
                    int exStyle = NativeMethods.GetWindowLong(windowHandle, Constants.GWL_EXSTYLE);
                    
                    if (alwaysOnTop)
                        exStyle |= Constants.WS_EX_TOPMOST;
                    else
                        exStyle &= ~Constants.WS_EX_TOPMOST;
                    
                    NativeMethods.SetWindowLong(windowHandle, Constants.GWL_EXSTYLE, exStyle);
                    
                    return true;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to set always on top");
                    return false;
                }
            });
        }
        
        private void LogWin32Error(string apiName)
        {
            int error = Marshal.GetLastWin32Error();
            if (error != 0)
            {
                _logger.LogWarning("{ApiName} failed with error code: {ErrorCode}", apiName, error);
            }
        }
    }
}
```

### 2. Global Hotkey System Implementation

#### Hotkey Service with Low-Level Hook
```csharp
// File: HotkeyManagerService.cs
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace SystemTrayProcessManager.Infrastructure.Services
{
    public class HotkeyManagerService : IHotkeyService, IDisposable
    {
        private readonly ILogger<HotkeyManagerService> _logger;
        private IntPtr _hookId = IntPtr.Zero;
        private readonly LowLevelKeyboardProc _hookCallback;
        private readonly ConcurrentDictionary<int, HotkeyAction> _registeredHotkeys;
        private readonly HashSet<int> _currentPressedKeys;
        private bool _disposed;
        
        public HotkeyManagerService(ILogger<HotkeyManagerService> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _hookCallback = HookCallback; // CRITICAL: Store reference to prevent GC
            _registeredHotkeys = new ConcurrentDictionary<int, HotkeyAction>();
            _currentPressedKeys = new HashSet<int>();
        }
        
        public void Initialize()
        {
            if (_hookId != IntPtr.Zero)
            {
                _logger.LogWarning("Hotkey service already initialized");
                return;
            }
            
            _hookId = SetHook(_hookCallback);
            
            if (_hookId == IntPtr.Zero)
            {
                int error = Marshal.GetLastWin32Error();
                _logger.LogError("Failed to set keyboard hook. Error: {Error}", error);
                throw new InvalidOperationException($"Failed to initialize hotkey hook. Error code: {error}");
            }
            
            _logger.LogInformation("Hotkey service initialized successfully");
        }
        
        private IntPtr SetHook(LowLevelKeyboardProc proc)
        {
            using var curProcess = Process.GetCurrentProcess();
            using var curModule = curProcess.MainModule;
            
            if (curModule == null)
            {
                _logger.LogError("Failed to get current module");
                return IntPtr.Zero;
            }
            
            return NativeMethods.SetWindowsHookEx(
                Constants.WH_KEYBOARD_LL,
                proc,
                NativeMethods.GetModuleHandle(curModule.ModuleName),
                0
            );
        }
        
        private IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            try
            {
                if (nCode >= 0)
                {
                    bool isKeyDown = wParam == (IntPtr)Constants.WM_KEYDOWN;
                    bool isKeyUp = wParam == (IntPtr)Constants.WM_KEYUP;
                    
                    var hookStruct = Marshal.PtrToStructure<KBDLLHOOKSTRUCT>(lParam);
                    int vkCode = hookStruct.vkCode;
                    
                    if (isKeyDown)
                    {
                        _currentPressedKeys.Add(vkCode);
                        
                        // Calculate hotkey hash from currently pressed keys
                        int hotkeyHash = CalculateHotkeyHash(_currentPressedKeys);
                        
                        if (_registeredHotkeys.TryGetValue(hotkeyHash, out var action))
                        {
                            _logger.LogDebug("Hotkey triggered: {Hash}", hotkeyHash);
                            
                            // Execute action asynchronously to avoid blocking hook
                            Task.Run(() =>
                            {
                                try
                                {
                                    action.Execute();
                                }
                                catch (Exception ex)
                                {
                                    _logger.LogError(ex, "Error executing hotkey action");
                                }
                            });
                            
                            // Optionally suppress the key
                            if (action.SuppressKey)
                            {
                                return (IntPtr)1;
                            }
                        }
                    }
                    else if (isKeyUp)
                    {
                        _currentPressedKeys.Remove(vkCode);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in keyboard hook callback");
            }
            
            return NativeMethods.CallNextHookEx(_hookId, nCode, wParam, lParam);
        }
        
        public bool RegisterHotkey(HotkeyBinding binding, Action action, bool suppressKey = false)
        {
            try
            {
                int hash = CalculateHotkeyHash(binding);
                
                if (_registeredHotkeys.ContainsKey(hash))
                {
                    _logger.LogWarning("Hotkey already registered: {Binding}", binding);
                    return false;
                }
                
                var hotkeyAction = new HotkeyAction
                {
                    Binding = binding,
                    Execute = action,
                    SuppressKey = suppressKey
                };
                
                _registeredHotkeys.TryAdd(hash, hotkeyAction);
                _logger.LogInformation("Hotkey registered: {Binding}", binding);
                
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to register hotkey");
                return false;
            }
        }
        
        public bool UnregisterHotkey(HotkeyBinding binding)
        {
            int hash = CalculateHotkeyHash(binding);
            return _registeredHotkeys.TryRemove(hash, out _);
        }
        
        private int CalculateHotkeyHash(HotkeyBinding binding)
        {
            // Create unique hash from modifiers and key
            int hash = binding.Key;
            if (binding.Ctrl) hash ^= 0x1000;
            if (binding.Alt) hash ^= 0x2000;
            if (binding.Shift) hash ^= 0x4000;
            if (binding.Win) hash ^= 0x8000;
            return hash;
        }
        
        private int CalculateHotkeyHash(HashSet<int> keys)
        {
            // Calculate hash from currently pressed keys
            // This is a simplified implementation
            int hash = 0;
            foreach (var key in keys)
            {
                hash ^= key;
            }
            return hash;
        }
        
        public void Dispose()
        {
            if (_disposed) return;
            
            if (_hookId != IntPtr.Zero)
            {
                NativeMethods.UnhookWindowsHookEx(_hookId);
                _hookId = IntPtr.Zero;
                _logger.LogInformation("Keyboard hook released");
            }
            
            _registeredHotkeys.Clear();
            _disposed = true;
        }
        
        private class HotkeyAction
        {
            public HotkeyBinding Binding { get; set; }
            public Action Execute { get; set; }
            public bool SuppressKey { get; set; }
        }
    }
}
```

### 3. Audio Control with CoreAudio

#### Audio Service Implementation
```csharp
// File: AudioManagerService.cs
using NAudio.CoreAudioApi;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SystemTrayProcessManager.Infrastructure.Services
{
    public class AudioManagerService : IAudioService, IDisposable
    {
        private readonly ILogger<AudioManagerService> _logger;
        private readonly MMDeviceEnumerator _deviceEnumerator;
        private MMDevice _defaultDevice;
        private bool _disposed;
        
        public AudioManagerService(ILogger<AudioManagerService> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _deviceEnumerator = new MMDeviceEnumerator();
            
            try
            {
                _defaultDevice = _deviceEnumerator.GetDefaultAudioEndpoint(
                    DataFlow.Render, 
                    Role.Multimedia);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get default audio device");
            }
        }
        
        public async Task<bool> MuteProcessAsync(int processId)
        {
            return await Task.Run(() =>
            {
                try
                {
                    var session = GetAudioSessionForProcess(processId);
                    if (session == null)
                    {
                        _logger.LogWarning("No audio session found for process {ProcessId}", processId);
                        return false;
                    }
                    
                    var volumeControl = session.SimpleAudioVolume;
                    volumeControl.Mute = true;
                    
                    _logger.LogInformation("Muted process {ProcessId}", processId);
                    return true;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to mute process {ProcessId}", processId);
                    return false;
                }
            });
        }
        
        public async Task<bool> UnmuteProcessAsync(int processId)
        {
            return await Task.Run(() =>
            {
                try
                {
                    var session = GetAudioSessionForProcess(processId);
                    if (session == null) return false;
                    
                    var volumeControl = session.SimpleAudioVolume;
                    volumeControl.Mute = false;
                    
                    _logger.LogInformation("Unmuted process {ProcessId}", processId);
                    return true;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to unmute process {ProcessId}", processId);
                    return false;
                }
            });
        }
        
        public async Task<bool> SetProcessVolumeAsync(int processId, float volume)
        {
            return await Task.Run(() =>
            {
                try
                {
                    if (volume < 0 || volume > 1)
                    {
                        _logger.LogWarning("Invalid volume value: {Volume}", volume);
                        return false;
                    }
                    
                    var session = GetAudioSessionForProcess(processId);
                    if (session == null) return false;
                    
                    var volumeControl = session.SimpleAudioVolume;
                    volumeControl.Volume = volume;
                    
                    _logger.LogInformation("Set volume for process {ProcessId} to {Volume}", processId, volume);
                    return true;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to set volume for process {ProcessId}", processId);
                    return false;
                }
            });
        }
        
        public async Task<bool> IsProcessMutedAsync(int processId)
        {
            return await Task.Run(() =>
            {
                try
                {
                    var session = GetAudioSessionForProcess(processId);
                    return session?.SimpleAudioVolume.Mute ?? false;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to check mute status for process {ProcessId}", processId);
                    return false;
                }
            });
        }
        
        private AudioSessionControl GetAudioSessionForProcess(int processId)
        {
            try
            {
                if (_defaultDevice == null) return null;
                
                var sessionManager = _defaultDevice.AudioSessionManager;
                var sessions = sessionManager.Sessions;
                
                for (int i = 0; i < sessions.Count; i++)
                {
                    var session = sessions[i];
                    if (session.GetProcessID == processId)
                    {
                        return session;
                    }
                }
                
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting audio session for process {ProcessId}", processId);
                return null;
            }
        }
        
        public void Dispose()
        {
            if (_disposed) return;
            
            _defaultDevice?.Dispose();
            _deviceEnumerator?.Dispose();
            _disposed = true;
        }
    }
}
```

### 4. System Tray Integration

#### Tray Icon Service Pattern
```csharp
// File: TrayIconService.cs
using System.Windows.Forms;
using Microsoft.Extensions.Logging;

namespace SystemTrayProcessManager.UI.Services
{
    public class TrayIconService : IDisposable
    {
        private readonly NotifyIcon _notifyIcon;
        private readonly IProcessService _processService;
        private readonly IAudioService _audioService;
        private readonly ILogger<TrayIconService> _logger;
        private bool _disposed;
        
        public TrayIconService(
            IProcessService processService,
            IAudioService audioService,
            ILogger<TrayIconService> logger)
        {
            _processService = processService ?? throw new ArgumentNullException(nameof(processService));
            _audioService = audioService ?? throw new ArgumentNullException(nameof(audioService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            
            _notifyIcon = new NotifyIcon
            {
                Icon = LoadTrayIcon(),
                Visible = true,
                Text = "SystemTrayProcessManager"
            };
            
            _notifyIcon.MouseClick += OnTrayIconClick;
            _notifyIcon.MouseDoubleClick += OnTrayIconDoubleClick;
            
            BuildContextMenu();
        }
        
        private Icon LoadTrayIcon()
        {
            try
            {
                var iconUri = new Uri("pack://application:,,,/Resources/Icons/tray-icon.ico");
                var streamInfo = Application.GetResourceStream(iconUri);
                return new Icon(streamInfo.Stream);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load tray icon");
                return SystemIcons.Application;
            }
        }
        
        private void BuildContextMenu()
        {
            var contextMenu = new ContextMenuStrip();
            
            // Add dynamic process list
            var processesMenu = new ToolStripMenuItem("Running Processes");
            UpdateProcessList(processesMenu);
            contextMenu.Items.Add(processesMenu);
            
            contextMenu.Items.Add(new ToolStripSeparator());
            
            // Static menu items
            contextMenu.Items.Add("Open Dashboard", null, OnOpenDashboard);
            contextMenu.Items.Add("Settings", null, OnOpenSettings);
            contextMenu.Items.Add(new ToolStripSeparator());
            contextMenu.Items.Add("Exit", null, OnExit);
            
            _notifyIcon.ContextMenuStrip = contextMenu;
            
            // Refresh process list periodically
            var refreshTimer = new System.Windows.Forms.Timer { Interval = 5000 };
            refreshTimer.Tick += (s, e) => UpdateProcessList(processesMenu);
            refreshTimer.Start();
        }
        
        private void UpdateProcessList(ToolStripMenuItem processesMenu)
        {
            processesMenu.DropDownItems.Clear();
            
            var processes = _processService.GetRunningProcesses().Take(15);
            
            foreach (var process in processes)
            {
                var processItem = new ToolStripMenuItem(process.Name);
                
                // Add sub-menu items for each process
                processItem.DropDownItems.Add(
                    "Bring to Front", 
                    null, 
                    (s, e) => BringProcessToFront(process));
                    
                processItem.DropDownItems.Add(
                    "Minimize", 
                    null, 
                    (s, e) => MinimizeProcess(process));
                    
                var muteItem = new ToolStripMenuItem(
                    "Mute/Unmute", 
                    null, 
                    (s, e) => ToggleMuteProcess(process));
                processItem.DropDownItems.Add(muteItem);
                
                processItem.DropDownItems.Add(new ToolStripSeparator());
                
                processItem.DropDownItems.Add(
                    "Close", 
                    null, 
                    (s, e) => CloseProcess(process));
                
                processesMenu.DropDownItems.Add(processItem);
            }
            
            if (!processes.Any())
            {
                processesMenu.DropDownItems.Add(new ToolStripMenuItem("No processes found") 
                { 
                    Enabled = false 
                });
            }
        }
        
        private void OnTrayIconClick(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                // Show quick action menu or main window
                ShowMainWindow();
            }
        }
        
        private void OnTrayIconDoubleClick(object sender, MouseEventArgs e)
        {
            ShowMainWindow();
        }
        
        public void ShowNotification(string title, string message, ToolTipIcon icon = ToolTipIcon.Info)
        {
            _notifyIcon.ShowBalloonTip(3000, title, message, icon);
        }
        
        private void ShowMainWindow()
        {
            // Implementation to show main window
            Application.Current.Dispatcher.Invoke(() =>
            {
                var mainWindow = Application.Current.MainWindow;
                if (mainWindow != null)
                {
                    mainWindow.Show();
                    mainWindow.WindowState = WindowState.Normal;
                    mainWindow.Activate();
                }
            });
        }
        
        private async void BringProcessToFront(ProcessInfo process)
        {
            // Implementation
        }
        
        private async void MinimizeProcess(ProcessInfo process)
        {
            // Implementation
        }
        
        private async void ToggleMuteProcess(ProcessInfo process)
        {
            // Implementation
        }
        
        private async void CloseProcess(ProcessInfo process)
        {
            // Implementation
        }
        
        private void OnOpenDashboard(object sender, EventArgs e)
        {
            ShowMainWindow();
        }
        
        private void OnOpenSettings(object sender, EventArgs e)
        {
            // Show settings window
        }
        
        private void OnExit(object sender, EventArgs e)
        {
            Application.Current.Shutdown();
        }
        
        public void Dispose()
        {
            if (_disposed) return;
            
            _notifyIcon?.Dispose();
            _disposed = true;
        }
    }
}
```

### 5. MVVM Implementation with CommunityToolkit

#### ViewModel Base Pattern
```csharp
// File: MainViewModel.cs
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;

namespace SystemTrayProcessManager.UI.ViewModels
{
    public partial class MainViewModel : ObservableObject
    {
        private readonly IProcessService _processService;
        private readonly IAudioService _audioService;
        private readonly IWindowService _windowService;
        private readonly ILogger<MainViewModel> _logger;
        
        [ObservableProperty]
        private ObservableCollection<ProcessInfoViewModel> _processes;
        
        [ObservableProperty]
        private ProcessInfoViewModel _selectedProcess;
        
        [ObservableProperty]
        private string _searchText;
        
        [ObservableProperty]
        private bool _isLoading;
        
        public MainViewModel(
            IProcessService processService,
            IAudioService audioService,
            IWindowService windowService,
            ILogger<MainViewModel> logger)
        {
            _processService = processService ?? throw new ArgumentNullException(nameof(processService));
            _audioService = audioService ?? throw new ArgumentNullException(nameof(audioService));
            _windowService = windowService ?? throw new ArgumentNullException(nameof(windowService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            
            _processes = new ObservableCollection<ProcessInfoViewModel>();
            
            // Start monitoring
            _ = LoadProcessesAsync();
        }
        
        [RelayCommand]
        private async Task LoadProcessesAsync()
        {
            IsLoading = true;
            
            try
            {
                var processes = await _processService.GetRunningProcessesAsync();
                
                Processes.Clear();
                foreach (var process in processes)
                {
                    Processes.Add(new ProcessInfoViewModel(process, _audioService, _windowService));
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load processes");
            }
            finally
            {
                IsLoading = false;
            }
        }
        
        [RelayCommand]
        private async Task RefreshAsync()
        {
            await LoadProcessesAsync();
        }
        
        [RelayCommand(CanExecute = nameof(CanExecuteProcessCommand))]
        private async Task BringToFrontAsync()
        {
            if (SelectedProcess == null) return;
            
            await SelectedProcess.BringToFrontAsync();
        }
        
        [RelayCommand(CanExecute = nameof(CanExecuteProcessCommand))]
        private async Task MinimizeAsync()
        {
            if (SelectedProcess == null) return;
            
            await SelectedProcess.MinimizeAsync();
        }
        
        [RelayCommand(CanExecute = nameof(CanExecuteProcessCommand))]
        private async Task ToggleMuteAsync()
        {
            if (SelectedProcess == null) return;
            
            await SelectedProcess.ToggleMuteAsync();
        }
        
        private bool CanExecuteProcessCommand()
        {
            return SelectedProcess != null;
        }
        
        partial void OnSelectedProcessChanged(ProcessInfoViewModel value)
        {
            // Update command can execute state
            BringToFrontCommand.NotifyCanExecuteChanged();
            MinimizeCommand.NotifyCanExecuteChanged();
            ToggleMuteCommand.NotifyCanExecuteChanged();
        }
    }
    
    public partial class ProcessInfoViewModel : ObservableObject
    {
        private readonly ProcessInfo _processInfo;
        private readonly IAudioService _audioService;
        private readonly IWindowService _windowService;
        
        [ObservableProperty]
        private bool _isMuted;
        
        public ProcessInfoViewModel(
            ProcessInfo processInfo,
            IAudioService audioService,
            IWindowService windowService)
        {
            _processInfo = processInfo;
            _audioService = audioService;
            _windowService = windowService;
            
            _ = UpdateMuteStatusAsync();
        }
        
        public string Name => _processInfo.Name;
        public int ProcessId => _processInfo.ProcessId;
        public string WindowTitle => _processInfo.WindowTitle;
        
        public async Task BringToFrontAsync()
        {
            await _windowService.BringToFrontAsync(_processInfo.WindowHandle);
        }
        
        public async Task MinimizeAsync()
        {
            await _windowService.MinimizeWindowAsync(_processInfo.WindowHandle);
        }
        
        public async Task ToggleMuteAsync()
        {
            if (IsMuted)
                await _audioService.UnmuteProcessAsync(ProcessId);
            else
                await _audioService.MuteProcessAsync(ProcessId);
            
            await UpdateMuteStatusAsync();
        }
        
        private async Task UpdateMuteStatusAsync()
        {
            IsMuted = await _audioService.IsProcessMutedAsync(ProcessId);
        }
    }
}
```

### 6. Dependency Injection Setup

#### App.xaml.cs Configuration
```csharp
// File: App.xaml.cs
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;
using System.Windows;

namespace SystemTrayProcessManager.UI
{
    public partial class App : Application
    {
        private IServiceProvider _serviceProvider;
        private TrayIconService _trayIconService;
        
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            
            // Configure Serilog
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .WriteTo.File(
                    Path.Combine(
                        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                        "SystemTrayProcessManager",
                        "logs",
                        "app.log"),
                    rollingInterval: RollingInterval.Day,
                    retainedFileCountLimit: 7)
                .CreateLogger();
            
            // Setup DI container
            var services = new ServiceCollection();
            ConfigureServices(services);
            _serviceProvider = services.BuildServiceProvider();
            
            // Initialize tray icon
            _trayIconService = _serviceProvider.GetRequiredService<TrayIconService>();
            
            // Don't show main window on startup (tray app)
            MainWindow = null;
            
            Log.Information("Application started");
        }
        
        private void ConfigureServices(IServiceCollection services)
        {
            // Logging
            services.AddLogging(builder =>
            {
                builder.ClearProviders();
                builder.AddSerilog(dispose: true);
            });
            
            // Core services
            services.AddSingleton<IProcessService, ProcessMonitorService>();
            services.AddSingleton<IAudioService, AudioManagerService>();
            services.AddSingleton<IWindowService, WindowManipulationService>();
            services.AddSingleton<IHotkeyService, HotkeyManagerService>();
            services.AddSingleton<IConfigurationService, ConfigurationService>();
            services.AddSingleton<INotificationService, NotificationService>();
            
            // UI services
            services.AddSingleton<TrayIconService>();
            
            // ViewModels
            services.AddTransient<MainViewModel>();
            services.AddTransient<SettingsViewModel>();
            services.AddTransient<HotkeyConfigViewModel>();
            
            // Initialize hotkey service
            var hotkeyService = services.BuildServiceProvider().GetRequiredService<IHotkeyService>();
            hotkeyService.Initialize();
        }
        
        protected override void OnExit(ExitEventArgs e)
        {
            _trayIconService?.Dispose();
            
            if (_serviceProvider is IDisposable disposable)
            {
                disposable.Dispose();
            }
            
            Log.Information("Application exiting");
            Log.CloseAndFlush();
            
            base.OnExit(e);
        }
    }
}
```

## Performance Guidelines

### 1. Process Monitoring Optimization
- Poll processes every 2-5 seconds, not continuously
- Cache process icons and information
- Use `Process.GetProcesses()` sparingly
- Implement smart filtering to reduce enumeration overhead

### 2. Hotkey Response Time
- Target <50ms response time
- Execute actions asynchronously outside hook callback
- Never block in the hook callback
- Use ThreadPool for action execution

### 3. Memory Management
- Dispose of Win32 handles properly
- Use `using` statements for IDisposable resources
- Unregister event handlers to prevent memory leaks
- Clear cached data periodically

## Testing Requirements

### Unit Tests
```csharp
// Example: ProcessServiceTests.cs
public class ProcessServiceTests
{
    [Fact]
    public async Task GetRunningProcesses_ReturnsNonEmptyList()
    {
        // Arrange
        var logger = new Mock<ILogger<ProcessMonitorService>>();
        var service = new ProcessMonitorService(logger.Object);
        
        // Act
        var processes = await service.GetRunningProcessesAsync();
        
        // Assert
        Assert.NotEmpty(processes);
    }
}
```

## Documentation Standards

### XML Comments Required
```csharp
/// <summary>
/// Brings the specified window to the foreground and activates it.
/// </summary>
/// <param name="windowHandle">Handle to the window to bring to front.</param>
/// <returns>True if successful, false otherwise.</returns>
/// <exception cref="ArgumentException">Thrown when windowHandle is IntPtr.Zero.</exception>
public async Task<bool> BringToFrontAsync(IntPtr windowHandle)
```

### README.md Structure
1. Project overview with screenshot
2. Features list
3. Installation instructions
4. Usage guide with examples
5. Hotkey configuration guide
6. Architecture overview
7. Technologies used
8. Build instructions
9. License

## Security Considerations

1. **UAC Elevation**: Detect when elevation is needed, prompt user appropriately
2. **Process Access**: Handle access denied gracefully
3. **Configuration Files**: Validate JSON input, handle corruption
4. **Hotkey Conflicts**: Warn users about system hotkey conflicts

## Common Pitfalls to Avoid

1. ❌ **Don't** store delegates without keeping references (GC will collect them)
2. ❌ **Don't** block UI thread with Win32 API calls
3. ❌ **Don't** forget to unhook keyboard hooks
4. ❌ **Don't** ignore Win32 error codes
5. ❌ **Don't** forget to dispose of COM objects (audio sessions)
6. ✅ **Do** use async/await for all I/O operations
7. ✅ **Do** log errors comprehensively
8. ✅ **Do** test with UAC enabled
9. ✅ **Do** handle edge cases (process terminated mid-operation)
10. ✅ **Do** provide user feedback for all actions

## Portfolio Presentation Tips

1. **Demo Video**: Record 2-3 minute demo showing key features
2. **Code Samples**: Highlight complex Win32 integration in README
3. **Architecture Diagram**: Show clean separation of concerns
4. **Performance Metrics**: Show <50ms hotkey response, low CPU usage
5. **Error Handling**: Demonstrate graceful failure handling
6. **UI Polish**: Show smooth animations, modern design

## Reference Documentation

- [Win32 API Documentation](https://learn.microsoft.com/en-us/windows/win32/)
- [NAudio Documentation](https://github.com/naudio/NAudio)
- [CommunityToolkit.Mvvm](https://learn.microsoft.com/en-us/dotnet/communitytoolkit/mvvm/)
- [WPF Documentation](https://learn.microsoft.com/en-us/dotnet/desktop/wpf/)

---

This skill provides comprehensive guidance for building a professional, portfolio-worthy WPF application with advanced Windows integration. Follow these patterns consistently throughout development.