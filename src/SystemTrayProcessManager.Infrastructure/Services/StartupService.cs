using System.Collections.Concurrent;
using System.Diagnostics;
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
    /// Provides startup process management for launching processes on application startup.
    /// </summary>
    public sealed class StartupService : IStartupService
    {
        private readonly ILogger<StartupService> _logger;
        private readonly string _configDirectory;
        private readonly string _configFilePath;
        private readonly ConcurrentDictionary<Guid, StartupProcess> _startupProcesses;
        private readonly SemaphoreSlim _fileLock = new(1, 1);
        private bool _isInitialized;

        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
        };

        /// <inheritdoc/>
        public event EventHandler<StartupProcessLaunchedEventArgs>? ProcessLaunched;

        /// <summary>
        /// Initializes a new instance of the <see cref="StartupService"/> class.
        /// </summary>
        /// <param name="logger">The logger instance.</param>
        public StartupService(ILogger<StartupService> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            _startupProcesses = new ConcurrentDictionary<Guid, StartupProcess>();

            _configDirectory = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "SystemTrayProcessManager");

            _configFilePath = Path.Combine(_configDirectory, "startup.json");

            _logger.LogDebug("StartupService initialized. Config path: {Path}", _configFilePath);
        }

        /// <summary>
        /// Ensures the service is initialized by loading configuration from disk.
        /// </summary>
        private async Task EnsureInitializedAsync()
        {
            if (_isInitialized) return;

            await _fileLock.WaitAsync().ConfigureAwait(false);
            try
            {
                if (_isInitialized) return;

                var config = await LoadConfigurationInternalAsync().ConfigureAwait(false);
                foreach (var process in config.StartupProcesses)
                {
                    _startupProcesses[process.Id] = process;
                }

                _isInitialized = true;
                _logger.LogInformation("StartupService initialized with {Count} startup processes", _startupProcesses.Count);
            }
            finally
            {
                _fileLock.Release();
            }
        }

        /// <inheritdoc/>
        public async Task<IReadOnlyList<StartupProcess>> GetStartupProcessesAsync()
        {
            await EnsureInitializedAsync().ConfigureAwait(false);

            var processes = _startupProcesses.Values.OrderBy(p => p.SortOrder).ToList();
            _logger.LogDebug("Returning {Count} startup processes", processes.Count);
            return processes.AsReadOnly();
        }

        /// <inheritdoc/>
        public async Task<StartupProcess?> GetStartupProcessAsync(Guid processId)
        {
            await EnsureInitializedAsync().ConfigureAwait(false);

            if (_startupProcesses.TryGetValue(processId, out var process))
            {
                _logger.LogDebug("Found startup process {ProcessId}", processId);
                return process;
            }

            return null;
        }

        /// <inheritdoc/>
        public async Task<bool> AddStartupProcessAsync(StartupProcess process)
        {
            ArgumentNullException.ThrowIfNull(process);

            if (!process.IsValid)
            {
                _logger.LogWarning("Cannot add startup process - invalid configuration");
                return false;
            }

            try
            {
                await EnsureInitializedAsync().ConfigureAwait(false);

                if (_startupProcesses.TryAdd(process.Id, process))
                {
                    await SaveConfigurationAsync().ConfigureAwait(false);
                    _logger.LogInformation("Added startup process {ProcessId}: {Name}", process.Id, process.DisplayName);
                    return true;
                }

                _logger.LogWarning("Failed to add startup process - ID already exists: {ProcessId}", process.Id);
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to add startup process {ProcessId}", process.Id);
                return false;
            }
        }

        /// <inheritdoc/>
        public async Task<bool> UpdateStartupProcessAsync(StartupProcess process)
        {
            ArgumentNullException.ThrowIfNull(process);

            try
            {
                await EnsureInitializedAsync().ConfigureAwait(false);

                if (!_startupProcesses.ContainsKey(process.Id))
                {
                    _logger.LogWarning("Cannot update startup process - not found: {ProcessId}", process.Id);
                    return false;
                }

                _startupProcesses[process.Id] = process;
                await SaveConfigurationAsync().ConfigureAwait(false);

                _logger.LogInformation("Updated startup process {ProcessId}: {Name}", process.Id, process.DisplayName);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to update startup process {ProcessId}", process.Id);
                return false;
            }
        }

        /// <inheritdoc/>
        public async Task<bool> RemoveStartupProcessAsync(Guid processId)
        {
            try
            {
                await EnsureInitializedAsync().ConfigureAwait(false);

                if (_startupProcesses.TryRemove(processId, out var removed))
                {
                    await SaveConfigurationAsync().ConfigureAwait(false);
                    _logger.LogInformation("Removed startup process {ProcessId}: {Name}", processId, removed.DisplayName);
                    return true;
                }

                _logger.LogWarning("Cannot remove startup process - not found: {ProcessId}", processId);
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to remove startup process {ProcessId}", processId);
                return false;
            }
        }

        /// <inheritdoc/>
        public async Task<bool> SetEnabledAsync(Guid processId, bool enabled)
        {
            try
            {
                await EnsureInitializedAsync().ConfigureAwait(false);

                if (!_startupProcesses.TryGetValue(processId, out var process))
                {
                    _logger.LogWarning("Cannot set enabled state - process not found: {ProcessId}", processId);
                    return false;
                }

                var updatedProcess = process.WithEnabled(enabled);
                _startupProcesses[processId] = updatedProcess;
                await SaveConfigurationAsync().ConfigureAwait(false);

                _logger.LogInformation("Set enabled={Enabled} for startup process {ProcessId}", enabled, processId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to set enabled state for startup process {ProcessId}", processId);
                return false;
            }
        }

        /// <inheritdoc/>
        public async Task<int> LaunchStartupProcessesAsync()
        {
            await EnsureInitializedAsync().ConfigureAwait(false);

            var enabledProcesses = _startupProcesses.Values
                .Where(p => p.IsEnabled)
                .OrderBy(p => p.SortOrder)
                .ToList();

            if (enabledProcesses.Count == 0)
            {
                _logger.LogDebug("No enabled startup processes to launch");
                return 0;
            }

            _logger.LogInformation("Launching {Count} startup processes", enabledProcesses.Count);

            var successCount = 0;

            foreach (var process in enabledProcesses)
            {
                try
                {
                    // Apply delay if specified
                    if (process.DelayMilliseconds > 0)
                    {
                        _logger.LogDebug("Waiting {Delay}ms before launching {Name}",
                            process.DelayMilliseconds, process.DisplayName);
                        await Task.Delay(process.DelayMilliseconds).ConfigureAwait(false);
                    }

                    var result = await LaunchProcessInternalAsync(process).ConfigureAwait(false);
                    if (result)
                    {
                        successCount++;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to launch startup process {ProcessId}: {Name}",
                        process.Id, process.DisplayName);

                    ProcessLaunched?.Invoke(this, new StartupProcessLaunchedEventArgs(
                        process, null, false, ex.Message));
                }
            }

            _logger.LogInformation("Launched {Success}/{Total} startup processes", successCount, enabledProcesses.Count);
            return successCount;
        }

        /// <inheritdoc/>
        public async Task<bool> LaunchProcessAsync(Guid processId)
        {
            await EnsureInitializedAsync().ConfigureAwait(false);

            if (!_startupProcesses.TryGetValue(processId, out var process))
            {
                _logger.LogWarning("Cannot launch - process not found: {ProcessId}", processId);
                return false;
            }

            return await LaunchProcessInternalAsync(process).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public async Task<IReadOnlyList<StartupProcess>> ValidatePathsAsync()
        {
            await EnsureInitializedAsync().ConfigureAwait(false);

            var invalidProcesses = _startupProcesses.Values
                .Where(p => !p.ExecutableExists)
                .ToList();

            if (invalidProcesses.Count > 0)
            {
                _logger.LogWarning("Found {Count} startup processes with invalid paths", invalidProcesses.Count);
            }

            return invalidProcesses.AsReadOnly();
        }

        private async Task<bool> LaunchProcessInternalAsync(StartupProcess startupProcess)
        {
            try
            {
                if (!startupProcess.ExecutableExists)
                {
                    var errorMsg = $"Executable not found: {startupProcess.ExecutablePath}";
                    _logger.LogWarning("Cannot launch {Name}: {Error}", startupProcess.DisplayName, errorMsg);

                    ProcessLaunched?.Invoke(this, new StartupProcessLaunchedEventArgs(
                        startupProcess, null, false, errorMsg));
                    return false;
                }

                var startInfo = new ProcessStartInfo
                {
                    FileName = startupProcess.ExecutablePath,
                    Arguments = startupProcess.Arguments ?? string.Empty,
                    WorkingDirectory = startupProcess.WorkingDirectory ?? 
                        Path.GetDirectoryName(startupProcess.ExecutablePath) ?? string.Empty,
                    UseShellExecute = true // Required for launching GUI apps
                };

                // Set window style based on LaunchWindowState
                startInfo.WindowStyle = startupProcess.LaunchWindowState switch
                {
                    WindowState.Minimized => ProcessWindowStyle.Minimized,
                    WindowState.Maximized => ProcessWindowStyle.Maximized,
                    WindowState.Hidden => ProcessWindowStyle.Hidden,
                    _ => ProcessWindowStyle.Normal
                };

                _logger.LogDebug("Launching process: {Path} {Args}",
                    startupProcess.ExecutablePath, startupProcess.Arguments ?? string.Empty);

                var process = Process.Start(startInfo);

                if (process != null)
                {
                    _logger.LogInformation("Launched startup process {Name} (PID: {ProcessId})",
                        startupProcess.DisplayName, process.Id);

                    ProcessLaunched?.Invoke(this, new StartupProcessLaunchedEventArgs(
                        startupProcess, process.Id, true));

                    return true;
                }
                else
                {
                    _logger.LogWarning("Process.Start returned null for {Name}", startupProcess.DisplayName);

                    ProcessLaunched?.Invoke(this, new StartupProcessLaunchedEventArgs(
                        startupProcess, null, false, "Process.Start returned null"));

                    return false;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to launch process {Name}", startupProcess.DisplayName);

                ProcessLaunched?.Invoke(this, new StartupProcessLaunchedEventArgs(
                    startupProcess, null, false, ex.Message));

                return false;
            }
        }

        private async Task<ProfileConfiguration> LoadConfigurationInternalAsync()
        {
            try
            {
                if (!Directory.Exists(_configDirectory))
                {
                    Directory.CreateDirectory(_configDirectory);
                }

                if (!File.Exists(_configFilePath))
                {
                    _logger.LogDebug("No existing startup file found, returning default configuration");
                    return ProfileConfiguration.CreateDefault();
                }

                var json = await File.ReadAllTextAsync(_configFilePath).ConfigureAwait(false);
                var config = JsonSerializer.Deserialize<ProfileConfiguration>(json, JsonOptions);

                if (config == null)
                {
                    _logger.LogWarning("Failed to deserialize startup configuration, returning default");
                    return ProfileConfiguration.CreateDefault();
                }

                _logger.LogDebug("Loaded startup configuration with {Count} processes", config.StartupProcesses.Count);
                return config;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load startup configuration");
                return ProfileConfiguration.CreateDefault();
            }
        }

        private async Task SaveConfigurationAsync()
        {
            await _fileLock.WaitAsync().ConfigureAwait(false);
            try
            {
                if (!Directory.Exists(_configDirectory))
                {
                    Directory.CreateDirectory(_configDirectory);
                }

                var config = new ProfileConfiguration
                {
                    Version = 1,
                    StartupProcesses = _startupProcesses.Values.ToList(),
                    LastSaved = DateTime.UtcNow
                };

                var json = JsonSerializer.Serialize(config, JsonOptions);
                await File.WriteAllTextAsync(_configFilePath, json).ConfigureAwait(false);

                _logger.LogDebug("Saved startup configuration with {Count} processes", config.StartupProcesses.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to save startup configuration");
                throw;
            }
            finally
            {
                _fileLock.Release();
            }
        }
    }
}
