using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using SystemTrayProcessManager.Core.Enums;
using SystemTrayProcessManager.Core.Models;
using SystemTrayProcessManager.Core.Services;

namespace SystemTrayProcessManager.Infrastructure.Services
{
    /// <summary>
    /// Provides gaming mode functionality for optimizing system performance during gaming.
    /// Coordinates muting, closing, and prioritizing processes.
    /// </summary>
    public sealed class GamingModeService : IGamingModeService
    {
        private readonly ILogger<GamingModeService> _logger;
        private readonly IProcessService _processService;
        private readonly IAudioService _audioService;
        private readonly IWindowService _windowService;
        private readonly IProcessPriorityService _priorityService;
        private readonly string _configFilePath;
        private readonly SemaphoreSlim _configLock = new(1, 1);
        private readonly Dictionary<int, float> _savedVolumes = new();
        private readonly Dictionary<int, ProcessPriority> _savedPriorities = new();

        private GamingModeConfig _config = GamingModeConfig.Default;
        private bool _isInitialized;

        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
        };

        /// <inheritdoc/>
        public event EventHandler<bool>? GamingModeChanged;

        /// <inheritdoc/>
        public bool IsGamingModeActive => _config.IsEnabled;

        /// <inheritdoc/>
        public GamingModeConfig Configuration => _config;

        /// <summary>
        /// Initializes a new instance of the <see cref="GamingModeService"/> class.
        /// </summary>
        public GamingModeService(
            ILogger<GamingModeService> logger,
            IProcessService processService,
            IAudioService audioService,
            IWindowService windowService,
            IProcessPriorityService priorityService)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _processService = processService ?? throw new ArgumentNullException(nameof(processService));
            _audioService = audioService ?? throw new ArgumentNullException(nameof(audioService));
            _windowService = windowService ?? throw new ArgumentNullException(nameof(windowService));
            _priorityService = priorityService ?? throw new ArgumentNullException(nameof(priorityService));

            var configDirectory = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "SystemTrayProcessManager");

            _configFilePath = Path.Combine(configDirectory, "gaming-mode.json");

            _logger.LogDebug("GamingModeService initialized. Config path: {Path}", _configFilePath);
        }

        /// <summary>
        /// Ensures configuration is loaded.
        /// </summary>
        private async Task EnsureInitializedAsync()
        {
            if (_isInitialized) return;

            await _configLock.WaitAsync().ConfigureAwait(false);
            try
            {
                if (_isInitialized) return;

                await LoadConfigAsync().ConfigureAwait(false);
                _isInitialized = true;
            }
            finally
            {
                _configLock.Release();
            }
        }

        /// <inheritdoc/>
        public async Task<bool> EnableGamingModeAsync(string? gameProcessName = null)
        {
            try
            {
                await EnsureInitializedAsync().ConfigureAwait(false);

                if (_config.IsEnabled)
                {
                    _logger.LogDebug("Gaming mode already enabled");
                    return true;
                }

                _logger.LogInformation("Enabling gaming mode for {GameProcess}", gameProcessName ?? "unspecified game");

                // Save current states before making changes
                await SaveCurrentStatesAsync().ConfigureAwait(false);

                // Close configured processes
                await CloseProcessesAsync(_config.ProcessesToClose).ConfigureAwait(false);

                // Minimize configured processes
                await MinimizeProcessesAsync(_config.ProcessesToMinimize).ConfigureAwait(false);

                // Mute configured processes
                await MuteProcessesAsync(_config.ProcessesToMute).ConfigureAwait(false);

                // Set game priority if specified
                if (!string.IsNullOrWhiteSpace(gameProcessName))
                {
                    await SetGamePriorityAsync(gameProcessName, _config.GamePriority).ConfigureAwait(false);
                }

                // Update config
                _config = _config.Enable(gameProcessName);
                await SaveConfigAsync().ConfigureAwait(false);

                _logger.LogInformation("Gaming mode enabled successfully");
                GamingModeChanged?.Invoke(this, true);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error enabling gaming mode");
                return false;
            }
        }

        /// <inheritdoc/>
        public async Task<bool> DisableGamingModeAsync()
        {
            try
            {
                await EnsureInitializedAsync().ConfigureAwait(false);

                if (!_config.IsEnabled)
                {
                    _logger.LogDebug("Gaming mode already disabled");
                    return true;
                }

                _logger.LogInformation("Disabling gaming mode");

                // Restore muted processes
                await RestoreMutedProcessesAsync().ConfigureAwait(false);

                // Restore game priority
                if (!string.IsNullOrWhiteSpace(_config.ActiveGameProcess))
                {
                    await RestoreGamePriorityAsync().ConfigureAwait(false);
                }

                // Clear saved states
                _savedVolumes.Clear();
                _savedPriorities.Clear();

                // Update config
                _config = _config.Disable();
                await SaveConfigAsync().ConfigureAwait(false);

                _logger.LogInformation("Gaming mode disabled successfully");
                GamingModeChanged?.Invoke(this, false);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error disabling gaming mode");
                return false;
            }
        }

        /// <inheritdoc/>
        public async Task<bool> ToggleGamingModeAsync(string? gameProcessName = null)
        {
            return _config.IsEnabled
                ? await DisableGamingModeAsync().ConfigureAwait(false)
                : await EnableGamingModeAsync(gameProcessName).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public async Task UpdateConfigurationAsync(GamingModeConfig config)
        {
            await EnsureInitializedAsync().ConfigureAwait(false);

            // Preserve enabled state
            _config = config with { IsEnabled = _config.IsEnabled };
            await SaveConfigAsync().ConfigureAwait(false);

            _logger.LogDebug("Gaming mode configuration updated");
        }

        /// <inheritdoc/>
        public async Task AddProcessToCloseAsync(string processName)
        {
            await EnsureInitializedAsync().ConfigureAwait(false);

            var list = _config.ProcessesToClose.ToList();
            if (!list.Contains(processName, StringComparer.OrdinalIgnoreCase))
            {
                list.Add(processName);
                _config = _config with { ProcessesToClose = list.AsReadOnly() };
                await SaveConfigAsync().ConfigureAwait(false);
            }
        }

        /// <inheritdoc/>
        public async Task AddProcessToMuteAsync(string processName)
        {
            await EnsureInitializedAsync().ConfigureAwait(false);

            var list = _config.ProcessesToMute.ToList();
            if (!list.Contains(processName, StringComparer.OrdinalIgnoreCase))
            {
                list.Add(processName);
                _config = _config with { ProcessesToMute = list.AsReadOnly() };
                await SaveConfigAsync().ConfigureAwait(false);
            }
        }

        /// <inheritdoc/>
        public async Task AddProcessToMinimizeAsync(string processName)
        {
            await EnsureInitializedAsync().ConfigureAwait(false);

            var list = _config.ProcessesToMinimize.ToList();
            if (!list.Contains(processName, StringComparer.OrdinalIgnoreCase))
            {
                list.Add(processName);
                _config = _config with { ProcessesToMinimize = list.AsReadOnly() };
                await SaveConfigAsync().ConfigureAwait(false);
            }
        }

        /// <inheritdoc/>
        public async Task RemoveProcessFromListsAsync(string processName)
        {
            await EnsureInitializedAsync().ConfigureAwait(false);

            var closeList = _config.ProcessesToClose.Where(p => !p.Equals(processName, StringComparison.OrdinalIgnoreCase)).ToList();
            var muteList = _config.ProcessesToMute.Where(p => !p.Equals(processName, StringComparison.OrdinalIgnoreCase)).ToList();
            var minimizeList = _config.ProcessesToMinimize.Where(p => !p.Equals(processName, StringComparison.OrdinalIgnoreCase)).ToList();

            _config = _config.WithProcessLists(closeList, muteList, minimizeList);
            await SaveConfigAsync().ConfigureAwait(false);
        }

        /// <summary>
        /// Saves current process states before gaming mode changes.
        /// </summary>
        private async Task SaveCurrentStatesAsync()
        {
            try
            {
                var processes = System.Diagnostics.Process.GetProcesses();
                foreach (var process in processes)
                {
                    try
                    {
                        var processName = process.ProcessName;

                        // Save volume for mute targets
                        if (_config.ProcessesToMute.Contains(processName, StringComparer.OrdinalIgnoreCase))
                        {
                            var volume = await _audioService.GetProcessVolumeAsync(process.Id).ConfigureAwait(false);
                            if (volume.HasValue)
                            {
                                _savedVolumes[process.Id] = volume.Value;
                            }
                        }
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
                _logger.LogError(ex, "Error saving current states");
            }
        }

        /// <summary>
        /// Closes processes in the close list.
        /// </summary>
        private async Task CloseProcessesAsync(IEnumerable<string> processNames)
        {
            foreach (var processName in processNames)
            {
                try
                {
                    var processes = System.Diagnostics.Process.GetProcessesByName(processName);
                    foreach (var process in processes)
                    {
                        try
                        {
                            if (process.MainWindowHandle != IntPtr.Zero)
                            {
                                await _windowService.CloseAsync(process.MainWindowHandle, force: false).ConfigureAwait(false);
                                _logger.LogDebug("Closed {ProcessName} (PID: {ProcessId})", processName, process.Id);
                            }
                        }
                        finally
                        {
                            process.Dispose();
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Error closing {ProcessName}", processName);
                }
            }
        }

        /// <summary>
        /// Minimizes processes in the minimize list.
        /// </summary>
        private async Task MinimizeProcessesAsync(IEnumerable<string> processNames)
        {
            foreach (var processName in processNames)
            {
                try
                {
                    var processes = System.Diagnostics.Process.GetProcessesByName(processName);
                    foreach (var process in processes)
                    {
                        try
                        {
                            if (process.MainWindowHandle != IntPtr.Zero)
                            {
                                await _windowService.MinimizeAsync(process.MainWindowHandle).ConfigureAwait(false);
                                _logger.LogDebug("Minimized {ProcessName} (PID: {ProcessId})", processName, process.Id);
                            }
                        }
                        finally
                        {
                            process.Dispose();
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Error minimizing {ProcessName}", processName);
                }
            }
        }

        /// <summary>
        /// Mutes processes in the mute list.
        /// </summary>
        private async Task MuteProcessesAsync(IEnumerable<string> processNames)
        {
            foreach (var processName in processNames)
            {
                try
                {
                    var processes = System.Diagnostics.Process.GetProcessesByName(processName);
                    foreach (var process in processes)
                    {
                        try
                        {
                            await _audioService.MuteProcessAsync(process.Id).ConfigureAwait(false);
                            _logger.LogDebug("Muted {ProcessName} (PID: {ProcessId})", processName, process.Id);
                        }
                        finally
                        {
                            process.Dispose();
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Error muting {ProcessName}", processName);
                }
            }
        }

        /// <summary>
        /// Sets the priority of the game process.
        /// </summary>
        private async Task SetGamePriorityAsync(string processName, ProcessPriority priority)
        {
            try
            {
                var processes = System.Diagnostics.Process.GetProcessesByName(processName);
                foreach (var process in processes)
                {
                    try
                    {
                        // Save current priority
                        var currentPriority = await _priorityService.GetProcessPriorityAsync(process.Id).ConfigureAwait(false);
                        if (currentPriority.HasValue)
                        {
                            _savedPriorities[process.Id] = currentPriority.Value;
                        }

                        // Set new priority
                        await _priorityService.SetProcessPriorityAsync(process.Id, priority).ConfigureAwait(false);
                        _logger.LogDebug("Set {ProcessName} priority to {Priority}", processName, priority);
                    }
                    finally
                    {
                        process.Dispose();
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error setting game priority for {ProcessName}", processName);
            }
        }

        /// <summary>
        /// Restores muted processes.
        /// </summary>
        private async Task RestoreMutedProcessesAsync()
        {
            foreach (var (processId, _) in _savedVolumes)
            {
                try
                {
                    await _audioService.UnmuteProcessAsync(processId).ConfigureAwait(false);
                }
                catch
                {
                    // Process may have exited
                }
            }
        }

        /// <summary>
        /// Restores the game process priority.
        /// </summary>
        private async Task RestoreGamePriorityAsync()
        {
            foreach (var (processId, priority) in _savedPriorities)
            {
                try
                {
                    await _priorityService.SetProcessPriorityAsync(processId, priority).ConfigureAwait(false);
                }
                catch
                {
                    // Process may have exited
                }
            }
        }

        /// <summary>
        /// Loads configuration from disk.
        /// </summary>
        private async Task LoadConfigAsync()
        {
            try
            {
                if (!File.Exists(_configFilePath))
                {
                    _config = GamingModeConfig.Default;
                    return;
                }

                var json = await File.ReadAllTextAsync(_configFilePath).ConfigureAwait(false);
                _config = JsonSerializer.Deserialize<GamingModeConfig>(json, JsonOptions) ?? GamingModeConfig.Default;

                // Always start with gaming mode disabled
                _config = _config.Disable();

                _logger.LogDebug("Loaded gaming mode configuration");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading gaming mode configuration");
                _config = GamingModeConfig.Default;
            }
        }

        /// <summary>
        /// Saves configuration to disk.
        /// </summary>
        private async Task SaveConfigAsync()
        {
            await _configLock.WaitAsync().ConfigureAwait(false);
            try
            {
                var directory = Path.GetDirectoryName(_configFilePath);
                if (!string.IsNullOrEmpty(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                var json = JsonSerializer.Serialize(_config, JsonOptions);
                await File.WriteAllTextAsync(_configFilePath, json).ConfigureAwait(false);

                _logger.LogDebug("Saved gaming mode configuration");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving gaming mode configuration");
            }
            finally
            {
                _configLock.Release();
            }
        }
    }
}
