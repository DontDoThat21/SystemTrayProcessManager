using System.Collections.Concurrent;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.Extensions.Logging;
using SystemTrayProcessManager.Core.Models;
using SystemTrayProcessManager.Core.Services;
using SystemTrayProcessManager.Infrastructure.WindowsAPI;

namespace SystemTrayProcessManager.Infrastructure.Services
{
    /// <summary>
    /// Provides focus history tracking using Windows event hooks.
    /// Maintains a circular buffer of recently focused windows.
    /// </summary>
    public sealed class FocusHistoryService : IFocusHistoryService
    {
        private readonly ILogger<FocusHistoryService> _logger;
        private readonly IWindowService _windowService;
        private readonly LinkedList<FocusHistoryEntry> _history;
        private readonly object _historyLock = new();

        private IntPtr _hookHandle;
        private GCHandle _delegateHandle;
        private WinEventProc? _winEventDelegate;
        private bool _isTracking;
        private bool _disposed;
        private int _maxHistorySize = 10;

        // Delegate for the WinEventProc callback
        private delegate void WinEventProc(
            IntPtr hWinEventHook,
            uint eventType,
            IntPtr hwnd,
            int idObject,
            int idChild,
            uint dwEventThread,
            uint dwmsEventTime);

        /// <inheritdoc/>
        public event EventHandler<FocusHistoryEntry>? FocusChanged;

        /// <inheritdoc/>
        public int MaxHistorySize
        {
            get => _maxHistorySize;
            set
            {
                if (value < 1) value = 1;
                if (value > 100) value = 100;
                _maxHistorySize = value;
                TrimHistory();
            }
        }

        /// <inheritdoc/>
        public bool IsTracking => _isTracking;

        /// <summary>
        /// Initializes a new instance of the <see cref="FocusHistoryService"/> class.
        /// </summary>
        /// <param name="logger">The logger instance.</param>
        /// <param name="windowService">The window service for bringing windows to front.</param>
        public FocusHistoryService(
            ILogger<FocusHistoryService> logger,
            IWindowService windowService)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _windowService = windowService ?? throw new ArgumentNullException(nameof(windowService));
            _history = new LinkedList<FocusHistoryEntry>();

            _logger.LogDebug("FocusHistoryService initialized");
        }

        /// <inheritdoc/>
        public IReadOnlyList<FocusHistoryEntry> GetHistory()
        {
            lock (_historyLock)
            {
                return _history.ToList().AsReadOnly();
            }
        }

        /// <inheritdoc/>
        public FocusHistoryEntry? GetLastFocused()
        {
            lock (_historyLock)
            {
                return _history.Count > 1 ? _history.ElementAt(1) : null;
            }
        }

        /// <inheritdoc/>
        public FocusHistoryEntry? GetEntryAt(int index)
        {
            if (index < 0) return null;

            lock (_historyLock)
            {
                if (index >= _history.Count) return null;
                return _history.ElementAt(index);
            }
        }

        /// <inheritdoc/>
        public async Task<bool> SwitchToLastFocusedAsync(int skipCount = 0)
        {
            try
            {
                FocusHistoryEntry? entry;
                lock (_historyLock)
                {
                    // Skip current window (+1) and any additional skip count
                    var targetIndex = 1 + skipCount;
                    if (targetIndex >= _history.Count)
                    {
                        _logger.LogWarning("No window at position {Index} in focus history", targetIndex);
                        return false;
                    }

                    entry = _history.ElementAt(targetIndex);
                }

                if (entry == null || entry.WindowHandle == IntPtr.Zero)
                {
                    _logger.LogWarning("Invalid focus history entry");
                    return false;
                }

                _logger.LogDebug("Switching to {ProcessName} (PID: {ProcessId})", entry.ProcessName, entry.ProcessId);
                return await _windowService.BringToFrontAsync(entry.WindowHandle).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error switching to last focused window");
                return false;
            }
        }

        /// <inheritdoc/>
        public void ClearHistory()
        {
            lock (_historyLock)
            {
                _history.Clear();
            }
            _logger.LogDebug("Focus history cleared");
        }

        /// <inheritdoc/>
        public void Start()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(FocusHistoryService));
            }

            if (_isTracking)
            {
                _logger.LogDebug("Focus tracking already active");
                return;
            }

            try
            {
                // Create and pin the delegate to prevent GC collection
                _winEventDelegate = new WinEventProc(WinEventCallback);
                _delegateHandle = GCHandle.Alloc(_winEventDelegate);

                var funcPtr = Marshal.GetFunctionPointerForDelegate(_winEventDelegate);

                _hookHandle = NativeMethods.SetWinEventHook(
                    NativeMethods.EVENT_SYSTEM_FOREGROUND,
                    NativeMethods.EVENT_SYSTEM_FOREGROUND,
                    IntPtr.Zero,
                    funcPtr,
                    0,
                    0,
                    NativeMethods.WINEVENT_OUTOFCONTEXT | NativeMethods.WINEVENT_SKIPOWNPROCESS);

                if (_hookHandle == IntPtr.Zero)
                {
                    var error = Marshal.GetLastWin32Error();
                    _logger.LogError("Failed to set focus event hook. Error code: {ErrorCode}", error);
                    CleanupHook();
                    return;
                }

                _isTracking = true;
                _logger.LogInformation("Focus tracking started. Hook handle: 0x{Handle:X8}", _hookHandle);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error starting focus tracking");
                CleanupHook();
            }
        }

        /// <inheritdoc/>
        public void Stop()
        {
            if (!_isTracking)
            {
                return;
            }

            CleanupHook();
            _isTracking = false;
            _logger.LogInformation("Focus tracking stopped");
        }

        /// <summary>
        /// Callback function for focus change events.
        /// </summary>
        private void WinEventCallback(
            IntPtr hWinEventHook,
            uint eventType,
            IntPtr hwnd,
            int idObject,
            int idChild,
            uint dwEventThread,
            uint dwmsEventTime)
        {
            if (hwnd == IntPtr.Zero) return;

            try
            {
                // Get process information
                NativeMethods.GetWindowThreadProcessId(hwnd, out var processId);
                if (processId == 0) return;

                // Get window title
                var titleLength = NativeMethods.GetWindowTextLength(hwnd);
                var titleBuilder = new StringBuilder(titleLength + 1);
                NativeMethods.GetWindowText(hwnd, titleBuilder, titleBuilder.Capacity);
                var windowTitle = titleBuilder.ToString();

                // Get process name
                var processName = GetProcessName((int)processId);
                if (string.IsNullOrEmpty(processName)) return;

                var entry = FocusHistoryEntry.Create(hwnd, (int)processId, processName, windowTitle);

                // Add to history
                lock (_historyLock)
                {
                    // Don't add duplicate consecutive entries
                    if (_history.Count > 0 && _history.First?.Value.WindowHandle == hwnd)
                    {
                        return;
                    }

                    _history.AddFirst(entry);
                    TrimHistory();
                }

                _logger.LogDebug("Focus changed to {ProcessName}: {WindowTitle}", processName, windowTitle);

                // Raise event (on a background thread to avoid blocking the hook)
                Task.Run(() => FocusChanged?.Invoke(this, entry));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing focus change event");
            }
        }

        /// <summary>
        /// Gets the process name for a process ID.
        /// </summary>
        private static string GetProcessName(int processId)
        {
            try
            {
                using var process = System.Diagnostics.Process.GetProcessById(processId);
                return process.ProcessName;
            }
            catch
            {
                return string.Empty;
            }
        }

        /// <summary>
        /// Trims the history to the maximum size.
        /// </summary>
        private void TrimHistory()
        {
            lock (_historyLock)
            {
                while (_history.Count > _maxHistorySize)
                {
                    _history.RemoveLast();
                }
            }
        }

        /// <summary>
        /// Cleans up the event hook.
        /// </summary>
        private void CleanupHook()
        {
            if (_hookHandle != IntPtr.Zero)
            {
                NativeMethods.UnhookWinEvent(_hookHandle);
                _hookHandle = IntPtr.Zero;
            }

            if (_delegateHandle.IsAllocated)
            {
                _delegateHandle.Free();
            }

            _winEventDelegate = null;
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            if (_disposed) return;

            Stop();
            ClearHistory();
            _disposed = true;

            _logger.LogDebug("FocusHistoryService disposed");
        }
    }
}
