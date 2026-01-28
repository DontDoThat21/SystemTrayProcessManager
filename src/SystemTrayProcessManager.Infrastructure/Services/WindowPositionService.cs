using System.Collections.Concurrent;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using SystemTrayProcessManager.Core.Enums;
using SystemTrayProcessManager.Core.Models;
using SystemTrayProcessManager.Core.Services;
using SystemTrayProcessManager.Infrastructure.WindowsAPI;

namespace SystemTrayProcessManager.Infrastructure.Services
{
    /// <summary>
    /// Provides window position persistence and restoration per monitor configuration.
    /// Saves positions to disk and restores them when requested or on monitor changes.
    /// </summary>
    public sealed class WindowPositionService : IWindowPositionService
    {
        private readonly ILogger<WindowPositionService> _logger;
        private readonly IWindowService _windowService;
        private readonly IProcessService _processService;
        private readonly string _configDirectory;
        private readonly string _configFilePath;
        private readonly ConcurrentDictionary<string, List<WindowPosition>> _positionsByLayout;
        private readonly SemaphoreSlim _fileLock = new(1, 1);

        private MonitorLayout? _currentLayout;
        private bool _isInitialized;

        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
        };

        /// <inheritdoc/>
        public event EventHandler<MonitorLayout>? MonitorLayoutChanged;

        /// <summary>
        /// Initializes a new instance of the <see cref="WindowPositionService"/> class.
        /// </summary>
        /// <param name="logger">The logger instance.</param>
        /// <param name="windowService">The window manipulation service.</param>
        /// <param name="processService">The process service.</param>
        public WindowPositionService(
            ILogger<WindowPositionService> logger,
            IWindowService windowService,
            IProcessService processService)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _windowService = windowService ?? throw new ArgumentNullException(nameof(windowService));
            _processService = processService ?? throw new ArgumentNullException(nameof(processService));

            _positionsByLayout = new ConcurrentDictionary<string, List<WindowPosition>>();

            _configDirectory = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "SystemTrayProcessManager");

            _configFilePath = Path.Combine(_configDirectory, "window-positions.json");

            _logger.LogDebug("WindowPositionService initialized. Config path: {Path}", _configFilePath);
        }

        /// <summary>
        /// Ensures the service is initialized by loading positions from disk.
        /// </summary>
        private async Task EnsureInitializedAsync()
        {
            if (_isInitialized) return;

            await _fileLock.WaitAsync().ConfigureAwait(false);
            try
            {
                if (_isInitialized) return;

                await LoadPositionsFromDiskAsync().ConfigureAwait(false);
                _currentLayout = await GetCurrentLayoutInternalAsync().ConfigureAwait(false);
                _isInitialized = true;

                _logger.LogInformation("WindowPositionService initialized. Current layout: {Hash}", _currentLayout.LayoutHash);
            }
            finally
            {
                _fileLock.Release();
            }
        }

        /// <inheritdoc/>
        public async Task<MonitorLayout> GetCurrentLayoutAsync()
        {
            await EnsureInitializedAsync().ConfigureAwait(false);

            var newLayout = await GetCurrentLayoutInternalAsync().ConfigureAwait(false);

            if (_currentLayout != null && !_currentLayout.IsSameConfiguration(newLayout))
            {
                _logger.LogInformation("Monitor layout changed. Old: {OldHash}, New: {NewHash}",
                    _currentLayout.LayoutHash, newLayout.LayoutHash);

                _currentLayout = newLayout;
                MonitorLayoutChanged?.Invoke(this, newLayout);
            }

            return newLayout;
        }

        /// <summary>
        /// Gets the current monitor layout without triggering change events.
        /// </summary>
        private Task<MonitorLayout> GetCurrentLayoutInternalAsync()
        {
            return Task.Run(() =>
            {
                var monitors = new List<MonitorInfo>();

                NativeMethods.EnumDisplayMonitors(IntPtr.Zero, IntPtr.Zero, (IntPtr hMonitor, IntPtr hdcMonitor, ref RECT lprcMonitor, IntPtr dwData) =>
                {
                    var monitorInfo = MONITORINFOEX.Create();
                    if (NativeMethods.GetMonitorInfo(hMonitor, ref monitorInfo))
                    {
                        var info = MonitorInfo.Create(
                            monitorInfo.szDevice,
                            monitorInfo.rcMonitor.Left,
                            monitorInfo.rcMonitor.Top,
                            monitorInfo.rcMonitor.Width,
                            monitorInfo.rcMonitor.Height,
                            (monitorInfo.dwFlags & 1) != 0); // MONITORINFOF_PRIMARY = 1

                        monitors.Add(info);
                    }
                    return true;
                }, IntPtr.Zero);

                return MonitorLayout.Create(monitors);
            });
        }

        /// <inheritdoc/>
        public async Task<WindowPosition?> SaveWindowPositionAsync(IntPtr windowHandle, string processName, string? windowTitle = null)
        {
            if (windowHandle == IntPtr.Zero || string.IsNullOrWhiteSpace(processName))
            {
                return null;
            }

            try
            {
                await EnsureInitializedAsync().ConfigureAwait(false);

                // Get current window position
                if (!NativeMethods.GetWindowRect(windowHandle, out var rect))
                {
                    _logger.LogWarning("Failed to get window rect for {ProcessName}", processName);
                    return null;
                }

                // Get window state
                var windowState = await _windowService.GetWindowStateAsync(windowHandle).ConfigureAwait(false);

                var layout = await GetCurrentLayoutAsync().ConfigureAwait(false);
                var position = WindowPosition.Create(
                    processName,
                    rect.Left,
                    rect.Top,
                    rect.Width,
                    rect.Height,
                    windowState,
                    layout.LayoutHash,
                    windowTitle);

                // Add or update in cache
                var positions = _positionsByLayout.GetOrAdd(layout.LayoutHash, _ => new List<WindowPosition>());
                lock (positions)
                {
                    // Remove existing position for same process
                    positions.RemoveAll(p => p.Matches(processName, windowTitle));
                    positions.Add(position);
                }

                // Save to disk
                await SavePositionsToDiskAsync().ConfigureAwait(false);

                _logger.LogDebug("Saved position for {ProcessName}: ({X},{Y}) {Width}x{Height}",
                    processName, position.X, position.Y, position.Width, position.Height);

                return position;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving window position for {ProcessName}", processName);
                return null;
            }
        }

        /// <inheritdoc/>
        public async Task<WindowPosition?> GetSavedPositionAsync(string processName, string? layoutHash = null)
        {
            await EnsureInitializedAsync().ConfigureAwait(false);

            var hash = layoutHash ?? _currentLayout?.LayoutHash;
            if (string.IsNullOrEmpty(hash)) return null;

            if (_positionsByLayout.TryGetValue(hash, out var positions))
            {
                lock (positions)
                {
                    return positions.FirstOrDefault(p => p.Matches(processName));
                }
            }

            return null;
        }

        /// <inheritdoc/>
        public async Task<IReadOnlyList<WindowPosition>> GetAllSavedPositionsAsync(string? layoutHash = null)
        {
            await EnsureInitializedAsync().ConfigureAwait(false);

            var hash = layoutHash ?? _currentLayout?.LayoutHash;
            if (string.IsNullOrEmpty(hash)) return Array.Empty<WindowPosition>();

            if (_positionsByLayout.TryGetValue(hash, out var positions))
            {
                lock (positions)
                {
                    return positions.ToList().AsReadOnly();
                }
            }

            return Array.Empty<WindowPosition>();
        }

        /// <inheritdoc/>
        public async Task<bool> RestoreWindowPositionAsync(IntPtr windowHandle, string processName)
        {
            if (windowHandle == IntPtr.Zero || string.IsNullOrWhiteSpace(processName))
            {
                return false;
            }

            try
            {
                var position = await GetSavedPositionAsync(processName).ConfigureAwait(false);
                if (position == null)
                {
                    _logger.LogDebug("No saved position found for {ProcessName}", processName);
                    return false;
                }

                // Restore window state first if needed
                if (position.WindowState != WindowState.Normal)
                {
                    if (position.WindowState == WindowState.Minimized)
                    {
                        await _windowService.MinimizeAsync(windowHandle).ConfigureAwait(false);
                        return true;
                    }
                    else if (position.WindowState == WindowState.Maximized)
                    {
                        await _windowService.MaximizeAsync(windowHandle).ConfigureAwait(false);
                        return true;
                    }
                }

                // Restore position
                var success = NativeMethods.MoveWindow(
                    windowHandle,
                    position.X,
                    position.Y,
                    position.Width,
                    position.Height,
                    true);

                if (success)
                {
                    _logger.LogDebug("Restored position for {ProcessName}: ({X},{Y}) {Width}x{Height}",
                        processName, position.X, position.Y, position.Width, position.Height);
                }
                else
                {
                    _logger.LogWarning("Failed to restore position for {ProcessName}", processName);
                }

                return success;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error restoring window position for {ProcessName}", processName);
                return false;
            }
        }

        /// <inheritdoc/>
        public async Task<int> RestoreAllWindowPositionsAsync()
        {
            try
            {
                await EnsureInitializedAsync().ConfigureAwait(false);

                var processes = await _processService.GetRunningProcessesAsync().ConfigureAwait(false);
                var restoredCount = 0;

                foreach (var process in processes)
                {
                    if (process.WindowHandle == IntPtr.Zero) continue;

                    var restored = await RestoreWindowPositionAsync(process.WindowHandle, process.Name).ConfigureAwait(false);
                    if (restored) restoredCount++;
                }

                _logger.LogInformation("Restored positions for {Count} windows", restoredCount);
                return restoredCount;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error restoring all window positions");
                return 0;
            }
        }

        /// <inheritdoc/>
        public async Task ClearSavedPositionsAsync(string? layoutHash = null)
        {
            await EnsureInitializedAsync().ConfigureAwait(false);

            var hash = layoutHash ?? _currentLayout?.LayoutHash;
            if (string.IsNullOrEmpty(hash)) return;

            _positionsByLayout.TryRemove(hash, out _);
            await SavePositionsToDiskAsync().ConfigureAwait(false);

            _logger.LogDebug("Cleared saved positions for layout {Hash}", hash);
        }

        /// <inheritdoc/>
        public async Task ClearAllSavedPositionsAsync()
        {
            await EnsureInitializedAsync().ConfigureAwait(false);

            _positionsByLayout.Clear();
            await SavePositionsToDiskAsync().ConfigureAwait(false);

            _logger.LogDebug("Cleared all saved positions");
        }

        /// <inheritdoc/>
        public async Task DeleteSavedPositionAsync(Guid positionId)
        {
            await EnsureInitializedAsync().ConfigureAwait(false);

            foreach (var (_, positions) in _positionsByLayout)
            {
                lock (positions)
                {
                    positions.RemoveAll(p => p.Id == positionId);
                }
            }

            await SavePositionsToDiskAsync().ConfigureAwait(false);
        }

        /// <summary>
        /// Loads positions from disk.
        /// </summary>
        private async Task LoadPositionsFromDiskAsync()
        {
            try
            {
                if (!File.Exists(_configFilePath))
                {
                    _logger.LogDebug("Position file not found, starting fresh");
                    return;
                }

                var json = await File.ReadAllTextAsync(_configFilePath).ConfigureAwait(false);
                var data = JsonSerializer.Deserialize<Dictionary<string, List<WindowPosition>>>(json, JsonOptions);

                if (data != null)
                {
                    foreach (var (hash, positions) in data)
                    {
                        _positionsByLayout[hash] = positions;
                    }
                }

                _logger.LogDebug("Loaded positions from disk");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading positions from disk");
            }
        }

        /// <summary>
        /// Saves positions to disk.
        /// </summary>
        private async Task SavePositionsToDiskAsync()
        {
            await _fileLock.WaitAsync().ConfigureAwait(false);
            try
            {
                Directory.CreateDirectory(_configDirectory);

                var data = _positionsByLayout.ToDictionary(
                    kvp => kvp.Key,
                    kvp => kvp.Value.ToList());

                var json = JsonSerializer.Serialize(data, JsonOptions);
                await File.WriteAllTextAsync(_configFilePath, json).ConfigureAwait(false);

                _logger.LogDebug("Saved positions to disk");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving positions to disk");
            }
            finally
            {
                _fileLock.Release();
            }
        }
    }
}
