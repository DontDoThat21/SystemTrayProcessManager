using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Text;
using SystemTrayProcessManager.Core.Models;
using SystemTrayProcessManager.Core.Services;
using SystemTrayProcessManager.Infrastructure.WindowsAPI;
using Timer = System.Threading.Timer;

namespace SystemTrayProcessManager.Infrastructure.Services
{
    /// <summary>
    /// Monitors running processes and provides process discovery and change detection.
    /// Implements background polling to detect process starts and stops.
    /// </summary>
    public sealed class ProcessMonitorService : IProcessService
    {
        private readonly ILogger<ProcessMonitorService> _logger;
        private readonly IIconExtractor _iconExtractor;
        private readonly ConcurrentDictionary<IntPtr, ProcessInfo> _trackedProcesses;
        private readonly HashSet<string> _excludedProcessNames;
        private readonly object _monitorLock = new();

        private const int MinPollingIntervalMs = 1000;
        private const int MaxPollingIntervalMs = 30000;
        private const int DefaultPollingIntervalMs = 5000;

        private TimeSpan _pollingInterval;
        private Timer? _monitorTimer;
        private bool _disposed;
        private int _currentProcessId;

        /// <inheritdoc/>
        public bool IsMonitoring { get; private set; }

        /// <inheritdoc/>
        public int PollingIntervalMs => (int)_pollingInterval.TotalMilliseconds;

        /// <inheritdoc/>
        public int TrackedProcessCount => _trackedProcesses.Count;

        /// <inheritdoc/>
        public event EventHandler<ProcessInfo>? ProcessStarted;

        /// <inheritdoc/>
        public event EventHandler<ProcessStoppedEventArgs>? ProcessStopped;

        /// <inheritdoc/>
        public event EventHandler<IReadOnlyList<ProcessInfo>>? ProcessListChanged;

        /// <summary>
        /// Initializes a new instance of the <see cref="ProcessMonitorService"/> class.
        /// </summary>
        /// <param name="logger">Logger instance for diagnostic output.</param>
        /// <param name="iconExtractor">Icon extractor for process icons.</param>
        /// <exception cref="ArgumentNullException">Thrown when any parameter is null.</exception>
        public ProcessMonitorService(
            ILogger<ProcessMonitorService> logger,
            IIconExtractor iconExtractor)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _iconExtractor = iconExtractor ?? throw new ArgumentNullException(nameof(iconExtractor));
            _trackedProcesses = new ConcurrentDictionary<IntPtr, ProcessInfo>();
            _currentProcessId = Environment.ProcessId;
            _pollingInterval = TimeSpan.FromMilliseconds(DefaultPollingIntervalMs);

            // System processes to always exclude
            _excludedProcessNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "svchost", "csrss", "smss", "wininit", "services", "lsass",
                "dwm", "fontdrvhost", "winlogon", "LogonUI", "sihost",
                "ctfmon", "RuntimeBroker", "taskhostw", "ShellExperienceHost",
                "SearchHost", "StartMenuExperienceHost", "TextInputHost",
                "SystemSettings", "SecurityHealthSystray", "SecurityHealthService",
                "System", "Idle", "Registry", "Memory Compression"
            };
        }

        /// <inheritdoc/>
        public void UpdatePollingInterval(int intervalMs)
        {
            if (_disposed)
            {
                return;
            }

            // Clamp to valid range
            if (intervalMs < MinPollingIntervalMs)
            {
                _logger.LogWarning(
                    "Polling interval {Requested}ms is below minimum, using {Min}ms",
                    intervalMs, MinPollingIntervalMs);
                intervalMs = MinPollingIntervalMs;
            }
            else if (intervalMs > MaxPollingIntervalMs)
            {
                _logger.LogWarning(
                    "Polling interval {Requested}ms is above maximum, using {Max}ms",
                    intervalMs, MaxPollingIntervalMs);
                intervalMs = MaxPollingIntervalMs;
            }

            lock (_monitorLock)
            {
                _pollingInterval = TimeSpan.FromMilliseconds(intervalMs);

                // If monitoring is active, update the timer
                if (IsMonitoring && _monitorTimer != null)
                {
                    _monitorTimer.Change(_pollingInterval, _pollingInterval);
                    _logger.LogInformation(
                        "Process polling interval updated to {Interval}ms",
                        intervalMs);
                }
                else
                {
                    _logger.LogDebug(
                        "Process polling interval set to {Interval}ms (will apply when monitoring starts)",
                        intervalMs);
                }
            }
        }

        /// <inheritdoc/>
        public async Task<IReadOnlyList<ProcessInfo>> GetRunningProcessesAsync(CancellationToken cancellationToken = default)
        {
            if (_disposed)
            {
                return Array.Empty<ProcessInfo>();
            }

            try
            {
                _logger.LogDebug("Enumerating running processes...");

                var processes = await Task.Run(() => EnumerateProcesses(), cancellationToken);

                _logger.LogDebug("Found {Count} windowed processes", processes.Count);
                return processes;
            }
            catch (OperationCanceledException)
            {
                _logger.LogDebug("Process enumeration was cancelled");
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to enumerate running processes");
                return Array.Empty<ProcessInfo>();
            }
        }

        /// <inheritdoc/>
        public async Task<ProcessInfo?> GetProcessByIdAsync(int processId, CancellationToken cancellationToken = default)
        {
            if (_disposed)
            {
                return null;
            }

            try
            {
                // Check tracked processes first (keyed by WindowHandle now)
                var trackedInfo = _trackedProcesses.Values.FirstOrDefault(p => p.ProcessId == processId);
                if (trackedInfo != null)
                {
                    return trackedInfo;
                }

                return await Task.Run(() =>
                {
                    try
                    {
                        var process = Process.GetProcessById(processId);
                        if (process.MainWindowHandle != IntPtr.Zero && !ShouldExcludeProcess(process))
                        {
                            return CreateProcessInfo(process);
                        }
                    }
                    catch (ArgumentException)
                    {
                        // Process no longer exists
                    }
                    catch (InvalidOperationException)
                    {
                        // Process has exited
                    }

                    return null;
                }, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Error getting process by ID: {ProcessId}", processId);
                return null;
            }
        }

        /// <inheritdoc/>
        public async Task<IReadOnlyList<ProcessInfo>> SearchProcessesAsync(string searchTerm, CancellationToken cancellationToken = default)
        {
            if (_disposed)
            {
                return Array.Empty<ProcessInfo>();
            }

            if (string.IsNullOrWhiteSpace(searchTerm))
            {
                return await GetRunningProcessesAsync(cancellationToken);
            }

            try
            {
                var allProcesses = await GetRunningProcessesAsync(cancellationToken);

                var searchLower = searchTerm.ToLowerInvariant();
                var filtered = allProcesses
                    .Where(p =>
                        p.Name.Contains(searchLower, StringComparison.OrdinalIgnoreCase) ||
                        (p.WindowTitle?.Contains(searchLower, StringComparison.OrdinalIgnoreCase) ?? false))
                    .ToList();

                _logger.LogDebug("Search for '{SearchTerm}' returned {Count} results", searchTerm, filtered.Count);
                return filtered;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching processes for term: {SearchTerm}", searchTerm);
                return Array.Empty<ProcessInfo>();
            }
        }

        /// <inheritdoc/>
        public void StartMonitoring()
        {
            if (_disposed)
            {
                return;
            }

            lock (_monitorLock)
            {
                if (IsMonitoring)
                {
                    _logger.LogWarning("Process monitoring is already active");
                    return;
                }

                _logger.LogInformation("Starting process monitoring with {Interval}s interval", _pollingInterval.TotalSeconds);

                // Initial population
                try
                {
                    var processes = EnumerateProcesses();
                    foreach (var process in processes)
                    {
                        _trackedProcesses.TryAdd(process.WindowHandle, process);
                    }

                    _logger.LogDebug("Initial population: {Count} processes tracked", _trackedProcesses.Count);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error during initial process population");
                }

                // Start the monitoring timer
                _monitorTimer = new Timer(
                    MonitorCallback,
                    null,
                    _pollingInterval,
                    _pollingInterval);

                IsMonitoring = true;
                _logger.LogInformation("Process monitoring started successfully");
            }
        }

        /// <inheritdoc/>
        public void StopMonitoring()
        {
            if (_disposed)
            {
                return;
            }

            lock (_monitorLock)
            {
                if (!IsMonitoring)
                {
                    return;
                }

                _logger.LogInformation("Stopping process monitoring...");

                _monitorTimer?.Dispose();
                _monitorTimer = null;
                IsMonitoring = false;

                _logger.LogInformation("Process monitoring stopped");
            }
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _logger.LogDebug("Disposing ProcessMonitorService...");

            StopMonitoring();
            _trackedProcesses.Clear();

            _disposed = true;
            _logger.LogDebug("ProcessMonitorService disposed");
        }

        /// <summary>
        /// Timer callback for monitoring process changes.
        /// </summary>
        private async void MonitorCallback(object? state)
        {
            if (_disposed || !IsMonitoring)
            {
                return;
            }

            try
            {
                await DetectProcessChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in process monitoring callback");
            }
        }

        /// <summary>
        /// Detects changes in the process list and raises appropriate events.
        /// </summary>
        private async Task DetectProcessChangesAsync()
        {
            try
            {
                var currentProcesses = await Task.Run(EnumerateProcesses);
                var currentHandles = new HashSet<IntPtr>(currentProcesses.Select(p => p.WindowHandle));
                var previousHandles = new HashSet<IntPtr>(_trackedProcesses.Keys);

                var started = new List<ProcessInfo>();
                var stopped = new List<(int Id, string Name)>();
                var hasChanges = false;

                // Find new windows
                foreach (var process in currentProcesses)
                {
                    if (!previousHandles.Contains(process.WindowHandle))
                    {
                        _trackedProcesses.TryAdd(process.WindowHandle, process);
                        started.Add(process);
                        hasChanges = true;
                    }
                    else
                    {
                        // Update existing process info (window title might have changed)
                        _trackedProcesses[process.WindowHandle] = process;
                    }
                }

                // Find closed windows
                foreach (var previousHandle in previousHandles)
                {
                    if (!currentHandles.Contains(previousHandle))
                    {
                        if (_trackedProcesses.TryRemove(previousHandle, out var removedProcess))
                        {
                            stopped.Add((removedProcess.ProcessId, removedProcess.Name));
                            hasChanges = true;
                        }
                    }
                }

                // Raise events for started processes
                foreach (var process in started)
                {
                    _logger.LogDebug("Process started: {Name} (PID: {PID})", process.Name, process.ProcessId);

                    try
                    {
                        ProcessStarted?.Invoke(this, process);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error in ProcessStarted event handler");
                    }
                }

                // Raise events for stopped processes
                foreach (var (id, name) in stopped)
                {
                    _logger.LogDebug("Process stopped: {Name} (PID: {PID})", name, id);

                    try
                    {
                        ProcessStopped?.Invoke(this, new ProcessStoppedEventArgs(id, name));
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error in ProcessStopped event handler");
                    }
                }

                // Raise list changed event if any changes occurred
                if (hasChanges)
                {
                    _logger.LogDebug("Process list changed. Started: {Started}, Stopped: {Stopped}", started.Count, stopped.Count);

                    try
                    {
                        ProcessListChanged?.Invoke(this, currentProcesses);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error in ProcessListChanged event handler");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error detecting process changes");
            }
        }

        /// <summary>
        /// Enumerates all top-level visible windows and creates ProcessInfo entries per window.
        /// This ensures multiple windows from the same process (e.g. Chrome) are listed individually.
        /// </summary>
        private List<ProcessInfo> EnumerateProcesses()
        {
            var result = new List<ProcessInfo>();
            var seenHandles = new HashSet<IntPtr>();

            try
            {
                NativeMethods.EnumWindows((hWnd, lParam) =>
                {
                    try
                    {
                        if (!NativeMethods.IsWindowVisible(hWnd))
                        {
                            return true;
                        }

                        // Skip owned windows (popups, dialogs)
                        IntPtr owner = NativeMethods.GetWindow(hWnd, NativeMethods.GW_OWNER);
                        if (owner != IntPtr.Zero)
                        {
                            return true;
                        }

                        long exStyle = NativeMethods.GetWindowLongAuto(hWnd, WindowConstants.GWL_EXSTYLE);
                        long style = NativeMethods.GetWindowLongAuto(hWnd, WindowConstants.GWL_STYLE);

                        // Skip tool windows unless they have WS_EX_APPWINDOW
                        if ((exStyle & WindowConstants.WS_EX_TOOLWINDOW) != 0 &&
                            (exStyle & WindowConstants.WS_EX_APPWINDOW) == 0)
                        {
                            return true;
                        }

                        // Skip child windows
                        if ((style & WindowConstants.WS_CHILD) != 0)
                        {
                            return true;
                        }

                        // Get the owning process
                        NativeMethods.GetWindowThreadProcessId(hWnd, out uint pid);
                        if (pid == 0 || (int)pid == _currentProcessId)
                        {
                            return true;
                        }

                        // Get window title
                        int titleLength = NativeMethods.GetWindowTextLength(hWnd);
                        string? windowTitle = null;
                        if (titleLength > 0)
                        {
                            var sb = new StringBuilder(titleLength + 1);
                            NativeMethods.GetWindowText(hWnd, sb, sb.Capacity);
                            windowTitle = sb.ToString();
                        }

                        // Skip windows with no title (usually system processes) unless
                        // they belong to well-known applications
                        if (string.IsNullOrWhiteSpace(windowTitle))
                        {
                            return true;
                        }

                        // Resolve process details
                        Process? process = null;
                        try
                        {
                            process = Process.GetProcessById((int)pid);
                        }
                        catch
                        {
                            return true;
                        }

                        try
                        {
                            if (_excludedProcessNames.Contains(process.ProcessName))
                            {
                                return true;
                            }

                            string? executablePath = null;
                            DateTime startTime = DateTime.MinValue;

                            try { executablePath = process.MainModule?.FileName; } catch { }
                            try { startTime = process.StartTime; } catch { }

                            // Skip immersive/UWP system apps
                            if (!string.IsNullOrEmpty(executablePath) &&
                                executablePath.Contains("\\SystemApps\\", StringComparison.OrdinalIgnoreCase))
                            {
                                return true;
                            }

                            var icon = !string.IsNullOrEmpty(executablePath)
                                ? _iconExtractor.ExtractIcon(executablePath)
                                : _iconExtractor.ExtractIconFromHandle(hWnd);

                            if (seenHandles.Add(hWnd))
                            {
                                result.Add(new ProcessInfo
                                {
                                    ProcessId = (int)pid,
                                    Name = process.ProcessName,
                                    WindowTitle = windowTitle,
                                    ExecutablePath = executablePath,
                                    WindowHandle = hWnd,
                                    Icon = icon,
                                    StartTime = startTime,
                                    IsResponding = process.Responding
                                });
                            }
                        }
                        catch (InvalidOperationException) { }
                        catch (System.ComponentModel.Win32Exception) { }
                        catch (Exception ex)
                        {
                            _logger.LogDebug(ex, "Error processing window 0x{Handle:X}", hWnd.ToInt64());
                        }
                        finally
                        {
                            process.Dispose();
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogDebug(ex, "Error in EnumWindows callback for 0x{Handle:X}", hWnd.ToInt64());
                    }

                    return true;
                }, IntPtr.Zero);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error enumerating windows");
            }

            return result.OrderBy(p => p.Name, StringComparer.OrdinalIgnoreCase).ToList();
        }

        /// <summary>
        /// Creates a ProcessInfo object from a Process instance.
        /// </summary>
        private ProcessInfo? CreateProcessInfo(Process process)
        {
            try
            {
                string? executablePath = null;
                DateTime startTime = DateTime.MinValue;

                try
                {
                    executablePath = process.MainModule?.FileName;
                }
                catch
                {
                    // Access denied for some processes
                }

                try
                {
                    startTime = process.StartTime;
                }
                catch
                {
                    // Access denied
                }

                // Extract icon
                var icon = !string.IsNullOrEmpty(executablePath)
                    ? _iconExtractor.ExtractIcon(executablePath)
                    : _iconExtractor.ExtractIconFromHandle(process.MainWindowHandle);

                return new ProcessInfo
                {
                    ProcessId = process.Id,
                    Name = process.ProcessName,
                    WindowTitle = string.IsNullOrWhiteSpace(process.MainWindowTitle) ? null : process.MainWindowTitle,
                    ExecutablePath = executablePath,
                    WindowHandle = process.MainWindowHandle,
                    Icon = icon,
                    StartTime = startTime,
                    IsResponding = process.Responding
                };
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Error creating ProcessInfo for PID: {ProcessId}", process.Id);
                return null;
            }
        }

        /// <summary>
        /// Determines if a process should be excluded from enumeration.
        /// </summary>
        private bool ShouldExcludeProcess(Process process)
        {
            try
            {
                // Exclude current application
                if (process.Id == _currentProcessId)
                {
                    return true;
                }

                // Exclude by name
                if (_excludedProcessNames.Contains(process.ProcessName))
                {
                    return true;
                }

                // Exclude processes with empty window titles (usually system processes)
                if (string.IsNullOrWhiteSpace(process.MainWindowTitle))
                {
                    // Allow processes with known names even without titles
                    var commonApps = new[] { "explorer", "cmd", "powershell", "pwsh", "WindowsTerminal" };
                    if (!commonApps.Contains(process.ProcessName, StringComparer.OrdinalIgnoreCase))
                    {
                        return true;
                    }
                }

                // Try to check executable path for system processes
                try
                {
                    var path = process.MainModule?.FileName;
                    if (!string.IsNullOrEmpty(path))
                    {
                        var windowsDir = Environment.GetFolderPath(Environment.SpecialFolder.Windows);

                        // Exclude immersive/UWP system apps (but allow user-installed UWP apps)
                        if (path.Contains("\\SystemApps\\", StringComparison.OrdinalIgnoreCase))
                        {
                            return true;
                        }
                    }
                }
                catch
                {
                    // Access denied - don't exclude based on path if we can't read it
                }

                return false;
            }
            catch
            {
                return true; // If we can't determine, exclude it
            }
        }
    }
}
