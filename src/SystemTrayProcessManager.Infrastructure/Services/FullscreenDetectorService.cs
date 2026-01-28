using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using SystemTrayProcessManager.Core.Models;
using SystemTrayProcessManager.Core.Services;
using SystemTrayProcessManager.Infrastructure.WindowsAPI;

namespace SystemTrayProcessManager.Infrastructure.Services
{
    /// <summary>
    /// Provides fullscreen window detection and automatic audio muting for background processes.
    /// Uses polling-based detection to minimize system overhead.
    /// </summary>
    public sealed class FullscreenDetectorService : IFullscreenDetectorService
    {
        private readonly ILogger<FullscreenDetectorService> _logger;
        private readonly IAudioService _audioService;
        private readonly ConcurrentDictionary<string, bool> _autoMuteProcesses;
        private readonly ConcurrentDictionary<int, float> _previousVolumes;

        private System.Threading.Timer? _detectionTimer;
        private FullscreenState? _currentFullscreenWindow;
        private bool _autoMuteEnabled;
        private bool _isDetecting;
        private bool _disposed;
        private readonly object _stateLock = new();

        private const int DetectionIntervalMs = 1000; // 1 second polling interval

        /// <inheritdoc/>
        public event EventHandler<FullscreenState>? FullscreenStateChanged;

        /// <inheritdoc/>
        public bool IsAnyWindowFullscreen => _currentFullscreenWindow?.IsFullscreen ?? false;

        /// <inheritdoc/>
        public FullscreenState? CurrentFullscreenWindow => _currentFullscreenWindow;

        /// <inheritdoc/>
        public IReadOnlyList<string> AutoMuteProcesses => _autoMuteProcesses.Keys.ToList().AsReadOnly();

        /// <inheritdoc/>
        public bool AutoMuteEnabled
        {
            get => _autoMuteEnabled;
            set => _autoMuteEnabled = value;
        }

        /// <inheritdoc/>
        public bool IsDetecting => _isDetecting;

        /// <summary>
        /// Initializes a new instance of the <see cref="FullscreenDetectorService"/> class.
        /// </summary>
        /// <param name="logger">The logger instance.</param>
        /// <param name="audioService">The audio service for muting processes.</param>
        public FullscreenDetectorService(
            ILogger<FullscreenDetectorService> logger,
            IAudioService audioService)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _audioService = audioService ?? throw new ArgumentNullException(nameof(audioService));
            _autoMuteProcesses = new ConcurrentDictionary<string, bool>(StringComparer.OrdinalIgnoreCase);
            _previousVolumes = new ConcurrentDictionary<int, float>();

            _logger.LogDebug("FullscreenDetectorService initialized");
        }

        /// <inheritdoc/>
        public void AddAutoMuteProcess(string processName)
        {
            if (string.IsNullOrWhiteSpace(processName)) return;
            _autoMuteProcesses.TryAdd(processName.Trim(), true);
            _logger.LogDebug("Added {ProcessName} to auto-mute list", processName);
        }

        /// <inheritdoc/>
        public void RemoveAutoMuteProcess(string processName)
        {
            if (string.IsNullOrWhiteSpace(processName)) return;
            _autoMuteProcesses.TryRemove(processName.Trim(), out _);
            _logger.LogDebug("Removed {ProcessName} from auto-mute list", processName);
        }

        /// <inheritdoc/>
        public void SetAutoMuteProcesses(IEnumerable<string> processNames)
        {
            _autoMuteProcesses.Clear();
            foreach (var name in processNames ?? Enumerable.Empty<string>())
            {
                if (!string.IsNullOrWhiteSpace(name))
                {
                    _autoMuteProcesses.TryAdd(name.Trim(), true);
                }
            }
            _logger.LogDebug("Set auto-mute list with {Count} processes", _autoMuteProcesses.Count);
        }

        /// <inheritdoc/>
        public void Start()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(FullscreenDetectorService));
            }

            if (_isDetecting)
            {
                _logger.LogDebug("Fullscreen detection already active");
                return;
            }

            _detectionTimer = new System.Threading.Timer(
                DetectionCallback,
                null,
                0,
                DetectionIntervalMs);

            _isDetecting = true;
            _logger.LogInformation("Fullscreen detection started with {Interval}ms interval", DetectionIntervalMs);
        }

        /// <inheritdoc/>
        public void Stop()
        {
            if (!_isDetecting) return;

            _detectionTimer?.Dispose();
            _detectionTimer = null;
            _isDetecting = false;

            // Restore any muted processes
            RestoreMutedProcesses().Wait();

            _logger.LogInformation("Fullscreen detection stopped");
        }

        /// <inheritdoc/>
        public async Task<FullscreenState?> CheckNowAsync()
        {
            return await Task.Run(() => DetectFullscreenWindow()).ConfigureAwait(false);
        }

        /// <summary>
        /// Timer callback for fullscreen detection.
        /// </summary>
        private void DetectionCallback(object? state)
        {
            if (_disposed) return;

            try
            {
                var newState = DetectFullscreenWindow();
                HandleStateChange(newState);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during fullscreen detection");
            }
        }

        /// <summary>
        /// Detects if any window is currently fullscreen.
        /// </summary>
        private FullscreenState? DetectFullscreenWindow()
        {
            try
            {
                var foregroundWindow = NativeMethods.GetForegroundWindow();
                if (foregroundWindow == IntPtr.Zero)
                {
                    return FullscreenState.None;
                }

                // Get window rectangle
                if (!NativeMethods.GetWindowRect(foregroundWindow, out var windowRect))
                {
                    return FullscreenState.None;
                }

                // Get monitor info for the window's monitor
                var monitor = NativeMethods.MonitorFromWindow(
                    foregroundWindow,
                    NativeMethods.MONITOR_DEFAULTTONEAREST);

                if (monitor == IntPtr.Zero)
                {
                    return FullscreenState.None;
                }

                var monitorInfo = MONITORINFOEX.Create();
                if (!NativeMethods.GetMonitorInfo(monitor, ref monitorInfo))
                {
                    return FullscreenState.None;
                }

                // Check if window covers the entire monitor
                var isFullscreen = 
                    windowRect.Left <= monitorInfo.rcMonitor.Left &&
                    windowRect.Top <= monitorInfo.rcMonitor.Top &&
                    windowRect.Right >= monitorInfo.rcMonitor.Right &&
                    windowRect.Bottom >= monitorInfo.rcMonitor.Bottom;

                // Additional check: exclude desktop and shell windows
                if (isFullscreen)
                {
                    NativeMethods.GetWindowThreadProcessId(foregroundWindow, out var processId);
                    if (processId == 0) return FullscreenState.None;

                    var processName = GetProcessName((int)processId);
                    
                    // Exclude common shell processes
                    if (IsShellProcess(processName))
                    {
                        return FullscreenState.None;
                    }

                    return FullscreenState.Create(
                        foregroundWindow,
                        (int)processId,
                        processName,
                        true);
                }

                return FullscreenState.None;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error detecting fullscreen window");
                return FullscreenState.None;
            }
        }

        /// <summary>
        /// Handles a fullscreen state change.
        /// </summary>
        private void HandleStateChange(FullscreenState? newState)
        {
            lock (_stateLock)
            {
                var wasFullscreen = _currentFullscreenWindow?.IsFullscreen ?? false;
                var isNowFullscreen = newState?.IsFullscreen ?? false;

                if (wasFullscreen == isNowFullscreen) return;

                _currentFullscreenWindow = newState;

                if (isNowFullscreen && _autoMuteEnabled)
                {
                    _logger.LogInformation("Fullscreen detected: {ProcessName}", newState?.ProcessName);
                    MuteBackgroundProcesses(newState!.ProcessId);
                }
                else if (!isNowFullscreen && _autoMuteEnabled)
                {
                    _logger.LogInformation("Fullscreen exited");
                    RestoreMutedProcesses().Wait();
                }

                // Raise event on background thread
                Task.Run(() => FullscreenStateChanged?.Invoke(this, newState ?? FullscreenState.None));
            }
        }

        /// <summary>
        /// Mutes background processes in the auto-mute list.
        /// </summary>
        private void MuteBackgroundProcesses(int excludeProcessId)
        {
            if (!_autoMuteEnabled) return;

            try
            {
                var processes = System.Diagnostics.Process.GetProcesses();
                foreach (var process in processes)
                {
                    try
                    {
                        if (process.Id == excludeProcessId) continue;

                        var processName = process.ProcessName;
                        if (!_autoMuteProcesses.ContainsKey(processName)) continue;

                        // Save current volume before muting
                        var currentVolume = _audioService.GetProcessVolumeAsync(process.Id).Result;
                        if (currentVolume.HasValue && !_previousVolumes.ContainsKey(process.Id))
                        {
                            _previousVolumes.TryAdd(process.Id, currentVolume.Value);
                        }

                        // Mute the process
                        _ = _audioService.MuteProcessAsync(process.Id).Result;
                        _logger.LogDebug("Auto-muted {ProcessName} (PID: {ProcessId})", processName, process.Id);
                    }
                    catch
                    {
                        // Ignore individual process errors
                    }
                    finally
                    {
                        process.Dispose();
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error muting background processes");
            }
        }

        /// <summary>
        /// Restores previously muted processes.
        /// </summary>
        private async Task RestoreMutedProcesses()
        {
            try
            {
                foreach (var (processId, previousVolume) in _previousVolumes)
                {
                    try
                    {
                        await _audioService.UnmuteProcessAsync(processId).ConfigureAwait(false);
                        _logger.LogDebug("Restored audio for PID: {ProcessId}", processId);
                    }
                    catch
                    {
                        // Process may have exited
                    }
                }

                _previousVolumes.Clear();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error restoring muted processes");
            }
        }

        /// <summary>
        /// Checks if a process is a shell process (explorer, etc.).
        /// </summary>
        private static bool IsShellProcess(string processName)
        {
            var shellProcesses = new[] { "explorer", "shellexperiencehost", "searchhost", "startmenuexperiencehost" };
            return shellProcesses.Contains(processName, StringComparer.OrdinalIgnoreCase);
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

        /// <inheritdoc/>
        public void Dispose()
        {
            if (_disposed) return;

            Stop();
            _autoMuteProcesses.Clear();
            _previousVolumes.Clear();
            _disposed = true;

            _logger.LogDebug("FullscreenDetectorService disposed");
        }
    }
}
