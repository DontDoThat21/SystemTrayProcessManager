using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Runtime.InteropServices;
using SystemTrayProcessManager.Core.Enums;
using SystemTrayProcessManager.Core.Models;
using SystemTrayProcessManager.Core.Services;
using SystemTrayProcessManager.Infrastructure.WindowsAPI;

namespace SystemTrayProcessManager.Infrastructure.Services
{
    /// <summary>
    /// Provides global hotkey registration and management using a low-level keyboard hook (WH_KEYBOARD_LL).
    /// This service captures keyboard events system-wide and executes registered actions when hotkeys are triggered.
    /// </summary>
    /// <remarks>
    /// Performance target: &lt;50ms response time from key press to action execution.
    /// Thread safety: All public methods are thread-safe.
    /// </remarks>
    public sealed class HotkeyManagerService : IHotkeyService
    {
        private readonly ILogger<HotkeyManagerService> _logger;
        private readonly ConcurrentDictionary<HotkeyBinding, HotkeyRegistration> _registrations;
        private readonly object _hookLock = new();
        private readonly Stopwatch _responseTimer;
        private readonly ConcurrentDictionary<int, byte> _pressedKeys = new();

        // CRITICAL: Keep delegate reference to prevent garbage collection
        private readonly LowLevelKeyboardProc _hookCallback;

        private IntPtr _hookId = IntPtr.Zero;
        private volatile bool _isInitialized;
        private volatile bool _isSuspended;
        private volatile bool _disposed;

        /// <inheritdoc/>
        public bool IsInitialized => _isInitialized;

        /// <inheritdoc/>
        public bool IsSuspended => _isSuspended;

        /// <inheritdoc/>
        public int RegisteredCount => _registrations.Count;

        /// <inheritdoc/>
        public event EventHandler<HotkeyTriggeredEventArgs>? HotkeyTriggered;

        /// <inheritdoc/>
        public event EventHandler<HotkeyErrorEventArgs>? HotkeyError;

        /// <summary>
        /// Initializes a new instance of the <see cref="HotkeyManagerService"/> class.
        /// </summary>
        /// <param name="logger">The logger instance.</param>
        /// <exception cref="ArgumentNullException">Thrown when logger is null.</exception>
        public HotkeyManagerService(ILogger<HotkeyManagerService> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _registrations = new ConcurrentDictionary<HotkeyBinding, HotkeyRegistration>();
            _responseTimer = new Stopwatch();

            // CRITICAL: Store callback reference to prevent GC collection
            _hookCallback = HookCallback;

            _logger.LogDebug("HotkeyManagerService created");
        }

        /// <inheritdoc/>
        public void Initialize()
        {
            ThrowIfDisposed();

            lock (_hookLock)
            {
                if (_isInitialized)
                {
                    _logger.LogWarning("HotkeyManagerService.Initialize called but service is already initialized");
                    return;
                }

                _logger.LogInformation("Initializing keyboard hook...");

                try
                {
                    _hookId = SetHook(_hookCallback);

                    if (_hookId == IntPtr.Zero)
                    {
                        int errorCode = Marshal.GetLastWin32Error();
                        var message = $"Failed to install keyboard hook. Win32 error code: {errorCode}";
                        _logger.LogError(message);
                        throw new InvalidOperationException(message);
                    }

                    _isInitialized = true;
                    _logger.LogInformation("Keyboard hook installed successfully. Hook ID: 0x{HookId:X}", _hookId.ToInt64());
                }
                catch (Exception ex) when (ex is not InvalidOperationException)
                {
                    _logger.LogError(ex, "Exception during keyboard hook installation");
                    throw new InvalidOperationException("Failed to initialize hotkey service.", ex);
                }
            }
        }

        /// <inheritdoc/>
        public bool RegisterHotkey(HotkeyBinding binding, Action action, bool suppressKey = false)
        {
            ArgumentNullException.ThrowIfNull(action);
            ThrowIfDisposed();
            ThrowIfNotInitialized();

            var registration = new HotkeyRegistration(binding, action, suppressKey);
            return RegisterInternal(registration);
        }

        /// <inheritdoc/>
        public bool RegisterHotkey(HotkeyBinding binding, Func<Task> asyncAction, bool suppressKey = false)
        {
            ArgumentNullException.ThrowIfNull(asyncAction);
            ThrowIfDisposed();
            ThrowIfNotInitialized();

            var registration = new HotkeyRegistration(binding, asyncAction, suppressKey);
            return RegisterInternal(registration);
        }

        /// <inheritdoc/>
        public bool UnregisterHotkey(HotkeyBinding binding)
        {
            ThrowIfDisposed();

            if (_registrations.TryRemove(binding, out _))
            {
                _logger.LogInformation("Unregistered hotkey: {Binding}", binding);
                return true;
            }

            _logger.LogDebug("Attempted to unregister non-existent hotkey: {Binding}", binding);
            return false;
        }

        /// <inheritdoc/>
        public void UnregisterAllHotkeys()
        {
            ThrowIfDisposed();

            var count = _registrations.Count;
            _registrations.Clear();
            _logger.LogInformation("Unregistered all hotkeys. Count: {Count}", count);
        }

        /// <inheritdoc/>
        public bool IsHotkeyRegistered(HotkeyBinding binding)
        {
            return _registrations.ContainsKey(binding);
        }

        /// <inheritdoc/>
        public IReadOnlyCollection<HotkeyBinding> GetRegisteredHotkeys()
        {
            return _registrations.Keys.ToList().AsReadOnly();
        }

        /// <inheritdoc/>
        public void Suspend()
        {
            ThrowIfDisposed();

            if (_isSuspended)
            {
                _logger.LogDebug("HotkeyManagerService.Suspend called but service is already suspended");
                return;
            }

            _isSuspended = true;
            _logger.LogInformation("Hotkey processing suspended");
        }

        /// <inheritdoc/>
        public void Resume()
        {
            ThrowIfDisposed();

            if (!_isSuspended)
            {
                _logger.LogDebug("HotkeyManagerService.Resume called but service is not suspended");
                return;
            }

            _isSuspended = false;
            _logger.LogInformation("Hotkey processing resumed");
        }

        /// <inheritdoc/>
        public void Shutdown()
        {
            if (_disposed)
            {
                return;
            }

            _logger.LogInformation("Shutting down HotkeyManagerService...");

            lock (_hookLock)
            {
                if (_hookId != IntPtr.Zero)
                {
                    _logger.LogDebug("Removing keyboard hook during shutdown...");

                    if (NativeMethods.UnhookWindowsHookEx(_hookId))
                    {
                        _logger.LogInformation("Keyboard hook removed during shutdown");
                    }
                    else
                    {
                        int errorCode = Marshal.GetLastWin32Error();
                        _logger.LogWarning("Failed to remove keyboard hook during shutdown. Win32 error code: {ErrorCode}", errorCode);
                    }

                    _hookId = IntPtr.Zero;
                }
            }

            _registrations.Clear();
            _pressedKeys.Clear();
            _isInitialized = false;

            _logger.LogInformation("HotkeyManagerService shutdown complete");
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;

            lock (_hookLock)
            {
                if (_hookId != IntPtr.Zero)
                {
                    _logger.LogDebug("Removing keyboard hook...");

                    if (NativeMethods.UnhookWindowsHookEx(_hookId))
                    {
                        _logger.LogInformation("Keyboard hook removed successfully");
                    }
                    else
                    {
                        int errorCode = Marshal.GetLastWin32Error();
                        _logger.LogWarning("Failed to remove keyboard hook. Win32 error code: {ErrorCode}", errorCode);
                    }

                    _hookId = IntPtr.Zero;
                }
            }

            _registrations.Clear();
            _pressedKeys.Clear();
            _isInitialized = false;

            _logger.LogInformation("HotkeyManagerService disposed");
        }

        #region Private Methods

        private bool RegisterInternal(HotkeyRegistration registration)
        {
            if (_registrations.TryAdd(registration.Binding, registration))
            {
                _logger.LogInformation(
                    "Registered hotkey: {Binding} (Suppress: {Suppress}, Async: {IsAsync})",
                    registration.Binding,
                    registration.SuppressKey,
                    registration.IsAsync);
                return true;
            }

            _logger.LogWarning("Failed to register hotkey - already registered: {Binding}", registration.Binding);
            return false;
        }

        private IntPtr SetHook(LowLevelKeyboardProc proc)
        {
            // For WH_KEYBOARD_LL, the hMod parameter should be NULL (IntPtr.Zero)
            // or the handle of the DLL containing the hook procedure.
            // Since we're using managed code, we pass IntPtr.Zero.
            _logger.LogDebug("Installing keyboard hook with WH_KEYBOARD_LL");

            // Convert delegate to function pointer for P/Invoke
            IntPtr procPtr = Marshal.GetFunctionPointerForDelegate(proc);

            return NativeMethods.SetWindowsHookEx(
                WindowConstants.WH_KEYBOARD_LL,
                procPtr,
                IntPtr.Zero,  // FIXED: Use IntPtr.Zero for low-level hooks in managed code
                0);
        }

        private IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            // CRITICAL: This callback must be fast and must not throw exceptions
            try
            {
                // Track key transitions while suspended too, so releases are not missed.
                if (nCode >= WindowConstants.HC_ACTION && !_disposed)
                {
                    ProcessKeyboardMessage(wParam, lParam);
                }
            }
            catch (Exception ex)
            {
                // Log but never throw from hook callback
                _logger.LogError(ex, "Exception in keyboard hook callback");
            }

            // Always pass to next hook
            return NativeMethods.CallNextHookEx(_hookId, nCode, wParam, lParam);
        }

        private void ProcessKeyboardMessage(IntPtr wParam, IntPtr lParam)
        {
            int msg = wParam.ToInt32();

            bool isKeyDown = msg == WindowConstants.WM_KEYDOWN || msg == WindowConstants.WM_SYSKEYDOWN;
            bool isKeyUp = msg == WindowConstants.WM_KEYUP || msg == WindowConstants.WM_SYSKEYUP;
            if (!isKeyDown && !isKeyUp)
            {
                return;
            }

            // Parse the keyboard hook structure
            var hookStruct = Marshal.PtrToStructure<KBDLLHOOKSTRUCT>(lParam);
            int vkCode = hookStruct.vkCode;

            // Track releases even while capture has suspended actions.
            if (!TryBeginKeyPress(vkCode, isKeyDown) || _isSuspended)
            {
                return;
            }

            // Skip if this is a modifier key itself (we wait for the actual key)
            if (VirtualKeyCodes.IsModifierKey(vkCode))
            {
                return;
            }

            // Get current modifier state
            var modifiers = GetCurrentModifiers();

            // Create a binding to match against registrations
            var pressedBinding = new HotkeyBinding(vkCode, modifiers);

            // Check if we have a registration for this hotkey
            if (_registrations.TryGetValue(pressedBinding, out var registration))
            {
                _responseTimer.Restart();

                _logger.LogDebug("Hotkey matched: {Binding}", pressedBinding);

                // Execute action asynchronously to avoid blocking the hook
                _ = Task.Run(async () =>
                {
                    try
                    {
                        await registration.ExecuteAsync().ConfigureAwait(false);

                        _responseTimer.Stop();
                        var responseTime = _responseTimer.Elapsed.TotalMilliseconds;

                        _logger.LogDebug(
                            "Hotkey action executed. Binding: {Binding}, ResponseTime: {ResponseTime:F2}ms",
                            pressedBinding,
                            responseTime);

                        // Raise event
                        HotkeyTriggered?.Invoke(this, new HotkeyTriggeredEventArgs(pressedBinding, responseTime));
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error executing hotkey action for {Binding}", pressedBinding);
                        HotkeyError?.Invoke(this, new HotkeyErrorEventArgs(ex, pressedBinding));
                    }
                });
            }
        }

        /// <summary>
        /// Tracks key transitions and allows only one action per physical press.
        /// </summary>
        internal bool TryBeginKeyPress(int virtualKeyCode, bool isKeyDown)
        {
            if (!isKeyDown)
            {
                _pressedKeys.TryRemove(virtualKeyCode, out _);
                return false;
            }

            return _pressedKeys.TryAdd(virtualKeyCode, 0);
        }

        private static HotkeyModifier GetCurrentModifiers()
        {
            var modifiers = HotkeyModifier.None;

            // Check Ctrl (either left or right)
            if (NativeMethods.IsKeyPressed(VirtualKeyCodes.VK_CONTROL) ||
                NativeMethods.IsKeyPressed(VirtualKeyCodes.VK_LCONTROL) ||
                NativeMethods.IsKeyPressed(VirtualKeyCodes.VK_RCONTROL))
            {
                modifiers |= HotkeyModifier.Ctrl;
            }

            // Check Alt (either left or right)
            if (NativeMethods.IsKeyPressed(VirtualKeyCodes.VK_MENU) ||
                NativeMethods.IsKeyPressed(VirtualKeyCodes.VK_LMENU) ||
                NativeMethods.IsKeyPressed(VirtualKeyCodes.VK_RMENU))
            {
                modifiers |= HotkeyModifier.Alt;
            }

            // Check Shift (either left or right)
            if (NativeMethods.IsKeyPressed(VirtualKeyCodes.VK_SHIFT) ||
                NativeMethods.IsKeyPressed(VirtualKeyCodes.VK_LSHIFT) ||
                NativeMethods.IsKeyPressed(VirtualKeyCodes.VK_RSHIFT))
            {
                modifiers |= HotkeyModifier.Shift;
            }

            // Check Win (either left or right)
            if (NativeMethods.IsKeyPressed(VirtualKeyCodes.VK_LWIN) ||
                NativeMethods.IsKeyPressed(VirtualKeyCodes.VK_RWIN))
            {
                modifiers |= HotkeyModifier.Win;
            }

            return modifiers;
        }

        private void ThrowIfDisposed()
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
        }

        private void ThrowIfNotInitialized()
        {
            if (!_isInitialized)
            {
                throw new InvalidOperationException(
                    "HotkeyManagerService has not been initialized. Call Initialize() first.");
            }
        }

        #endregion
    }
}
