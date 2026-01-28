using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using System.Diagnostics;
using SystemTrayProcessManager.Core.Models;
using SystemTrayProcessManager.Core.Services;
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
        private readonly ConcurrentDictionary<int, ProcessInfo> _trackedProcesses;
        private readonly TimeSpan _pollingInterval = TimeSpan.FromSeconds(5);
        private readonly HashSet<string> _excludedProcessNames;

        private Timer? _monitorTimer;
        private bool _disposed;
        private readonly object _monitorLock = new();
        private int _currentProcessId;

        /// <inheritdoc/>
        public bool IsMonitoring { get; private set; }

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
            _trackedProcesses = new ConcurrentDictionary<int, ProcessInfo>();
            _currentProcessId = Environment.ProcessId;

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
                // Check tracked processes first
                if (_trackedProcesses.TryGetValue(processId, out var trackedInfo))
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
                        _trackedProcesses.TryAdd(process.ProcessId, process);
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
                var currentIds = new HashSet<int>(currentProcesses.Select(p => p.ProcessId));
                var previousIds = new HashSet<int>(_trackedProcesses.Keys);

                var started = new List<ProcessInfo>();
                var stopped = new List<(int Id, string Name)>();
                var hasChanges = false;

                // Find new processes
                foreach (var process in currentProcesses)
                {
                    if (!previousIds.Contains(process.ProcessId))
                    {
                        _trackedProcesses.TryAdd(process.ProcessId, process);
                        started.Add(process);
                        hasChanges = true;
                    }
                    else
                    {
                        // Update existing process info (window title might have changed)
                        _trackedProcesses[process.ProcessId] = process;
                    }
                }

                // Find stopped processes
                foreach (var previousId in previousIds)
                {
                    if (!currentIds.Contains(previousId))
                    {
                        if (_trackedProcesses.TryRemove(previousId, out var removedProcess))
                        {
                            stopped.Add((previousId, removedProcess.Name));
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
        /// Enumerates all processes with visible windows.
        /// </summary>
        private List<ProcessInfo> EnumerateProcesses()
        {
            var result = new List<ProcessInfo>();

            try
            {
                var processes = Process.GetProcesses();

                foreach (var process in processes)
                {
                    try
                    {
                        // Skip processes without windows
                        if (process.MainWindowHandle == IntPtr.Zero)
                        {
                            continue;
                        }

                        // Skip excluded processes
                        if (ShouldExcludeProcess(process))
                        {
                            continue;
                        }

                        var processInfo = CreateProcessInfo(process);
                        if (processInfo != null)
                        {
                            result.Add(processInfo);
                        }
                    }
                    catch (InvalidOperationException)
                    {
                        // Process has exited
                    }
                    catch (System.ComponentModel.Win32Exception)
                    {
                        // Access denied
                    }
                    catch (Exception ex)
                    {
                        _logger.LogDebug(ex, "Error processing: {ProcessId}", process.Id);
                    }
                    finally
                    {
                        process.Dispose();
                    }
                }
            }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error enumerating processes");
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
