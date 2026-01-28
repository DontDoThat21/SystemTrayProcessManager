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
    /// Provides CPU process priority management functionality.
    /// Allows getting, setting, and auto-applying process priorities.
    /// </summary>
    public sealed class ProcessPriorityService : IProcessPriorityService, IDisposable
    {
        private readonly ILogger<ProcessPriorityService> _logger;
        private readonly IProcessService _processService;
        private readonly ConcurrentDictionary<string, ProcessPriorityConfig> _configs;
        private readonly string _configFilePath;
        private readonly SemaphoreSlim _configLock = new(1, 1);

        private bool _autoApplyEnabled = true;
        private bool _isInitialized;
        private bool _disposed;

        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
        };

        /// <inheritdoc/>
        public event EventHandler<(string ProcessName, ProcessPriority Priority)>? PriorityChanged;

        /// <inheritdoc/>
        public bool AutoApplyEnabled
        {
            get => _autoApplyEnabled;
            set => _autoApplyEnabled = value;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ProcessPriorityService"/> class.
        /// </summary>
        public ProcessPriorityService(
            ILogger<ProcessPriorityService> logger,
            IProcessService processService)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _processService = processService ?? throw new ArgumentNullException(nameof(processService));

            _configs = new ConcurrentDictionary<string, ProcessPriorityConfig>(StringComparer.OrdinalIgnoreCase);

            var configDirectory = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "SystemTrayProcessManager");

            _configFilePath = Path.Combine(configDirectory, "process-priorities.json");

            // Subscribe to process started events for auto-apply
            _processService.ProcessStarted += OnProcessStarted;

            _logger.LogDebug("ProcessPriorityService initialized. Config path: {Path}", _configFilePath);
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

                await LoadConfigsAsync().ConfigureAwait(false);
                _isInitialized = true;

                _logger.LogInformation("ProcessPriorityService initialized with {Count} configurations", _configs.Count);
            }
            finally
            {
                _configLock.Release();
            }
        }

        /// <inheritdoc/>
        public async Task<ProcessPriority?> GetProcessPriorityAsync(int processId)
        {
            try
            {
                var handle = NativeMethods.OpenProcess(
                    NativeMethods.PROCESS_QUERY_LIMITED_INFORMATION,
                    false,
                    processId);

                if (handle == IntPtr.Zero)
                {
                    var error = System.Runtime.InteropServices.Marshal.GetLastWin32Error();
                    _logger.LogWarning("Failed to open process {ProcessId} for priority query. Error: {Error}", processId, error);
                    return null;
                }

                try
                {
                    var priorityClass = NativeMethods.GetPriorityClass(handle);
                    if (priorityClass == 0)
                    {
                        _logger.LogWarning("Failed to get priority for process {ProcessId}", processId);
                        return null;
                    }

                    return (ProcessPriority)priorityClass;
                }
                finally
                {
                    NativeMethods.CloseHandle(handle);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting priority for process {ProcessId}", processId);
                return null;
            }
        }

        /// <inheritdoc/>
        public async Task<bool> SetProcessPriorityAsync(int processId, ProcessPriority priority)
        {
            try
            {
                // RealTime priority requires SE_INC_BASE_PRIORITY_NAME privilege
                if (priority == ProcessPriority.RealTime)
                {
                    _logger.LogWarning("Attempting to set RealTime priority - this requires administrator privileges");
                }

                var handle = NativeMethods.OpenProcess(
                    NativeMethods.PROCESS_SET_INFORMATION | NativeMethods.PROCESS_QUERY_LIMITED_INFORMATION,
                    false,
                    processId);

                if (handle == IntPtr.Zero)
                {
                    var error = System.Runtime.InteropServices.Marshal.GetLastWin32Error();
                    _logger.LogWarning("Failed to open process {ProcessId} for priority change. Error: {Error}", processId, error);
                    return false;
                }

                try
                {
                    var success = NativeMethods.SetPriorityClass(handle, (uint)priority);
                    if (!success)
                    {
                        var error = System.Runtime.InteropServices.Marshal.GetLastWin32Error();
                        _logger.LogWarning("Failed to set priority for process {ProcessId}. Error: {Error}", processId, error);
                        return false;
                    }

                    _logger.LogDebug("Set process {ProcessId} priority to {Priority}", processId, priority);

                    // Get process name for event
                    try
                    {
                        using var process = System.Diagnostics.Process.GetProcessById(processId);
                        PriorityChanged?.Invoke(this, (process.ProcessName, priority));
                    }
                    catch
                    {
                        // Process may have exited
                    }

                    return true;
                }
                finally
                {
                    NativeMethods.CloseHandle(handle);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error setting priority for process {ProcessId}", processId);
                return false;
            }
        }

        /// <inheritdoc/>
        public async Task<bool> SetProcessPriorityByNameAsync(string processName, ProcessPriority priority)
        {
            if (string.IsNullOrWhiteSpace(processName))
            {
                return false;
            }

            try
            {
                var processes = System.Diagnostics.Process.GetProcessesByName(processName);
                if (processes.Length == 0)
                {
                    _logger.LogDebug("No processes found with name {ProcessName}", processName);
                    return false;
                }

                var anySuccess = false;
                foreach (var process in processes)
                {
                    try
                    {
                        var success = await SetProcessPriorityAsync(process.Id, priority).ConfigureAwait(false);
                        if (success) anySuccess = true;
                    }
                    finally
                    {
                        process.Dispose();
                    }
                }

                return anySuccess;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error setting priority for {ProcessName}", processName);
                return false;
            }
        }

        /// <inheritdoc/>
        public IReadOnlyList<ProcessPriorityConfig> GetSavedPriorityConfigs()
        {
            return _configs.Values.ToList().AsReadOnly();
        }

        /// <inheritdoc/>
        public ProcessPriorityConfig? GetPriorityConfig(string processName)
        {
            _configs.TryGetValue(processName, out var config);
            return config;
        }

        /// <inheritdoc/>
        public async Task SavePriorityConfigAsync(ProcessPriorityConfig config)
        {
            await EnsureInitializedAsync().ConfigureAwait(false);

            _configs[config.ProcessName] = config;
            await SaveConfigsAsync().ConfigureAwait(false);

            _logger.LogDebug("Saved priority config for {ProcessName}: {Priority}", config.ProcessName, config.Priority);

            // Apply immediately if auto-apply is enabled
            if (_autoApplyEnabled && config.AutoApply)
            {
                await SetProcessPriorityByNameAsync(config.ProcessName, config.Priority).ConfigureAwait(false);
            }
        }

        /// <inheritdoc/>
        public async Task RemovePriorityConfigAsync(string processName)
        {
            await EnsureInitializedAsync().ConfigureAwait(false);

            _configs.TryRemove(processName, out _);
            await SaveConfigsAsync().ConfigureAwait(false);

            _logger.LogDebug("Removed priority config for {ProcessName}", processName);
        }

        /// <inheritdoc/>
        public async Task<int> ApplyAllSavedPrioritiesAsync()
        {
            await EnsureInitializedAsync().ConfigureAwait(false);

            var appliedCount = 0;
            foreach (var config in _configs.Values.Where(c => c.AutoApply))
            {
                var success = await SetProcessPriorityByNameAsync(config.ProcessName, config.Priority).ConfigureAwait(false);
                if (success) appliedCount++;
            }

            _logger.LogInformation("Applied {Count} priority configurations", appliedCount);
            return appliedCount;
        }

        /// <summary>
        /// Handles process started events for auto-apply.
        /// </summary>
        private async void OnProcessStarted(object? sender, ProcessInfo process)
        {
            if (!_autoApplyEnabled || !_isInitialized) return;

            if (_configs.TryGetValue(process.Name, out var config) && config.AutoApply)
            {
                _logger.LogDebug("Auto-applying priority {Priority} to newly started {ProcessName}",
                    config.Priority, process.Name);

                // Delay slightly to allow process to fully initialize
                await Task.Delay(500).ConfigureAwait(false);
                await SetProcessPriorityByNameAsync(process.Name, config.Priority).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Loads configurations from disk.
        /// </summary>
        private async Task LoadConfigsAsync()
        {
            try
            {
                if (!File.Exists(_configFilePath))
                {
                    _logger.LogDebug("Priority config file not found, starting fresh");
                    return;
                }

                var json = await File.ReadAllTextAsync(_configFilePath).ConfigureAwait(false);
                var configs = JsonSerializer.Deserialize<List<ProcessPriorityConfig>>(json, JsonOptions);

                if (configs != null)
                {
                    foreach (var config in configs)
                    {
                        _configs[config.ProcessName] = config;
                    }
                }

                _logger.LogDebug("Loaded {Count} priority configurations", _configs.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading priority configurations");
            }
        }

        /// <summary>
        /// Saves configurations to disk.
        /// </summary>
        private async Task SaveConfigsAsync()
        {
            await _configLock.WaitAsync().ConfigureAwait(false);
            try
            {
                var directory = Path.GetDirectoryName(_configFilePath);
                if (!string.IsNullOrEmpty(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                var configs = _configs.Values.ToList();
                var json = JsonSerializer.Serialize(configs, JsonOptions);
                await File.WriteAllTextAsync(_configFilePath, json).ConfigureAwait(false);

                _logger.LogDebug("Saved priority configurations");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving priority configurations");
            }
            finally
            {
                _configLock.Release();
            }
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            if (_disposed) return;

            _processService.ProcessStarted -= OnProcessStarted;
            _configLock.Dispose();
            _disposed = true;

            _logger.LogDebug("ProcessPriorityService disposed");
        }
    }
}
