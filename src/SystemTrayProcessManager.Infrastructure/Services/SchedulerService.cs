using System.Collections.Concurrent;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using SystemTrayProcessManager.Core.Enums;
using SystemTrayProcessManager.Core.Models;
using SystemTrayProcessManager.Core.Services;

using Timer = System.Threading.Timer;

namespace SystemTrayProcessManager.Infrastructure.Services
{
    /// <summary>
    /// Provides scheduled action management with timer-based execution.
    /// Supports one-time, interval-based, and daily scheduled actions.
    /// </summary>
    public sealed class SchedulerService : ISchedulerService
    {
        private readonly ILogger<SchedulerService> _logger;
        private readonly IActionMappingService _actionMappingService;
        private readonly IProcessService _processService;
        private readonly string _configDirectory;
        private readonly string _configFilePath;
        private readonly ConcurrentDictionary<Guid, ScheduledAction> _scheduledActions;
        private readonly SemaphoreSlim _fileLock = new(1, 1);
        private Timer? _schedulerTimer;
        private bool _isRunning;
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
        public bool IsRunning => _isRunning;

        /// <inheritdoc/>
        public event EventHandler<ScheduledActionExecutedEventArgs>? ActionExecuted;

        /// <inheritdoc/>
        public event EventHandler<ScheduledActionErrorEventArgs>? ActionError;

        /// <summary>
        /// Initializes a new instance of the <see cref="SchedulerService"/> class.
        /// </summary>
        /// <param name="logger">The logger instance.</param>
        /// <param name="actionMappingService">The action mapping service for executing actions.</param>
        /// <param name="processService">The process service for finding target processes.</param>
        public SchedulerService(
            ILogger<SchedulerService> logger,
            IActionMappingService actionMappingService,
            IProcessService processService)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _actionMappingService = actionMappingService ?? throw new ArgumentNullException(nameof(actionMappingService));
            _processService = processService ?? throw new ArgumentNullException(nameof(processService));

            _scheduledActions = new ConcurrentDictionary<Guid, ScheduledAction>();

            _configDirectory = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "SystemTrayProcessManager");

            _configFilePath = Path.Combine(_configDirectory, "schedules.json");

            _logger.LogDebug("SchedulerService initialized. Config path: {Path}", _configFilePath);
        }

        /// <summary>
        /// Ensures the service is initialized by loading schedules from disk.
        /// </summary>
        private async Task EnsureInitializedAsync()
        {
            if (_isInitialized) return;

            await _fileLock.WaitAsync().ConfigureAwait(false);
            try
            {
                if (_isInitialized) return;

                var config = await LoadConfigurationInternalAsync().ConfigureAwait(false);
                foreach (var action in config.ScheduledActions)
                {
                    _scheduledActions[action.Id] = action;
                }

                _isInitialized = true;
                _logger.LogInformation("SchedulerService initialized with {Count} scheduled actions", _scheduledActions.Count);
            }
            finally
            {
                _fileLock.Release();
            }
        }

        /// <inheritdoc/>
        public async Task<IReadOnlyList<ScheduledAction>> GetScheduledActionsAsync()
        {
            await EnsureInitializedAsync().ConfigureAwait(false);

            var actions = _scheduledActions.Values.ToList();
            _logger.LogDebug("Returning {Count} scheduled actions", actions.Count);
            return actions.AsReadOnly();
        }

        /// <inheritdoc/>
        public async Task<ScheduledAction?> GetScheduledActionAsync(Guid actionId)
        {
            await EnsureInitializedAsync().ConfigureAwait(false);

            if (_scheduledActions.TryGetValue(actionId, out var action))
            {
                _logger.LogDebug("Found scheduled action {ActionId}", actionId);
                return action;
            }

            return null;
        }

        /// <inheritdoc/>
        public async Task<bool> AddScheduledActionAsync(ScheduledAction action)
        {
            ArgumentNullException.ThrowIfNull(action);

            try
            {
                await EnsureInitializedAsync().ConfigureAwait(false);

                // Calculate initial next execution time
                var actionWithNextExecution = new ScheduledAction
                {
                    Id = action.Id,
                    Name = action.Name,
                    Description = action.Description,
                    ActionType = action.ActionType,
                    TargetProcessName = action.TargetProcessName,
                    ScheduleType = action.ScheduleType,
                    ExecuteAt = action.ExecuteAt,
                    Interval = action.Interval,
                    DailyTime = action.DailyTime,
                    IsEnabled = action.IsEnabled,
                    LastExecuted = action.LastExecuted,
                    NextExecution = action.CalculateNextExecution(DateTime.UtcNow),
                    CreatedAt = action.CreatedAt
                };

                if (_scheduledActions.TryAdd(action.Id, actionWithNextExecution))
                {
                    await SaveConfigurationAsync().ConfigureAwait(false);
                    _logger.LogInformation("Added scheduled action {ActionId}: {Name}", action.Id, action.Name);
                    return true;
                }

                _logger.LogWarning("Failed to add scheduled action - ID already exists: {ActionId}", action.Id);
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to add scheduled action {ActionId}", action.Id);
                return false;
            }
        }

        /// <inheritdoc/>
        public async Task<bool> UpdateScheduledActionAsync(ScheduledAction action)
        {
            ArgumentNullException.ThrowIfNull(action);

            try
            {
                await EnsureInitializedAsync().ConfigureAwait(false);

                if (!_scheduledActions.ContainsKey(action.Id))
                {
                    _logger.LogWarning("Cannot update scheduled action - not found: {ActionId}", action.Id);
                    return false;
                }

                var actionWithNextExecution = new ScheduledAction
                {
                    Id = action.Id,
                    Name = action.Name,
                    Description = action.Description,
                    ActionType = action.ActionType,
                    TargetProcessName = action.TargetProcessName,
                    ScheduleType = action.ScheduleType,
                    ExecuteAt = action.ExecuteAt,
                    Interval = action.Interval,
                    DailyTime = action.DailyTime,
                    IsEnabled = action.IsEnabled,
                    LastExecuted = action.LastExecuted,
                    NextExecution = action.IsEnabled ? action.CalculateNextExecution(DateTime.UtcNow) : null,
                    CreatedAt = action.CreatedAt
                };

                _scheduledActions[action.Id] = actionWithNextExecution;
                await SaveConfigurationAsync().ConfigureAwait(false);

                _logger.LogInformation("Updated scheduled action {ActionId}: {Name}", action.Id, action.Name);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to update scheduled action {ActionId}", action.Id);
                return false;
            }
        }

        /// <inheritdoc/>
        public async Task<bool> RemoveScheduledActionAsync(Guid actionId)
        {
            try
            {
                await EnsureInitializedAsync().ConfigureAwait(false);

                if (_scheduledActions.TryRemove(actionId, out var removed))
                {
                    await SaveConfigurationAsync().ConfigureAwait(false);
                    _logger.LogInformation("Removed scheduled action {ActionId}: {Name}", actionId, removed.Name);
                    return true;
                }

                _logger.LogWarning("Cannot remove scheduled action - not found: {ActionId}", actionId);
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to remove scheduled action {ActionId}", actionId);
                return false;
            }
        }

        /// <inheritdoc/>
        public async Task<bool> SetEnabledAsync(Guid actionId, bool enabled)
        {
            try
            {
                await EnsureInitializedAsync().ConfigureAwait(false);

                if (!_scheduledActions.TryGetValue(actionId, out var action))
                {
                    _logger.LogWarning("Cannot set enabled state - action not found: {ActionId}", actionId);
                    return false;
                }

                var updatedAction = action.WithEnabled(enabled);
                _scheduledActions[actionId] = updatedAction;
                await SaveConfigurationAsync().ConfigureAwait(false);

                _logger.LogInformation("Set enabled={Enabled} for scheduled action {ActionId}", enabled, actionId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to set enabled state for scheduled action {ActionId}", actionId);
                return false;
            }
        }

        /// <inheritdoc/>
        public async Task<IReadOnlyList<ScheduledAction>> GetDueActionsAsync()
        {
            await EnsureInitializedAsync().ConfigureAwait(false);

            var now = DateTime.UtcNow;
            var dueActions = _scheduledActions.Values
                .Where(a => a.IsEnabled && a.NextExecution.HasValue && a.NextExecution.Value <= now)
                .ToList();

            _logger.LogDebug("Found {Count} due scheduled actions", dueActions.Count);
            return dueActions.AsReadOnly();
        }

        /// <inheritdoc/>
        public void Start()
        {
            if (_isRunning)
            {
                _logger.LogDebug("Scheduler is already running");
                return;
            }

            _schedulerTimer = new Timer(
                OnTimerCallback,
                null,
                TimeSpan.FromSeconds(10), // Initial delay
                TimeSpan.FromSeconds(30)  // Check every 30 seconds
            );

            _isRunning = true;
            _logger.LogInformation("Scheduler started");
        }

        /// <inheritdoc/>
        public void Stop()
        {
            if (!_isRunning)
            {
                _logger.LogDebug("Scheduler is not running");
                return;
            }

            _schedulerTimer?.Change(Timeout.Infinite, Timeout.Infinite);
            _schedulerTimer?.Dispose();
            _schedulerTimer = null;

            _isRunning = false;
            _logger.LogInformation("Scheduler stopped");
        }

        private async void OnTimerCallback(object? state)
        {
            try
            {
                await ProcessDueActionsAsync().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing scheduled actions");
            }
        }

        private async Task ProcessDueActionsAsync()
        {
            var dueActions = await GetDueActionsAsync().ConfigureAwait(false);

            foreach (var action in dueActions)
            {
                try
                {
                    _logger.LogDebug("Executing scheduled action {ActionId}: {Name}", action.Id, action.Name);

                    var success = await ExecuteActionAsync(action).ConfigureAwait(false);
                    var executedAt = DateTime.UtcNow;

                    // Update the action with execution info
                    var updatedAction = action.WithExecuted(executedAt);
                    _scheduledActions[action.Id] = updatedAction;

                    ActionExecuted?.Invoke(this, new ScheduledActionExecutedEventArgs(action, executedAt, success));

                    _logger.LogInformation("Executed scheduled action {ActionId}: Success={Success}", action.Id, success);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to execute scheduled action {ActionId}", action.Id);
                    ActionError?.Invoke(this, new ScheduledActionErrorEventArgs(action, ex.Message, ex));
                }
            }

            // Save updated actions if any were executed
            if (dueActions.Count > 0)
            {
                await SaveConfigurationAsync().ConfigureAwait(false);
            }
        }

        private async Task<bool> ExecuteActionAsync(ScheduledAction action)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(action.TargetProcessName))
                {
                    // Execute on foreground window
                    var result = await _actionMappingService.ExecuteQuickActionAsync(action.ActionType).ConfigureAwait(false);
                    return result.Success;
                }
                else
                {
                    // Execute on specific process
                    var result = await _actionMappingService.ExecutePinnedActionAsync(action.ActionType, action.TargetProcessName).ConfigureAwait(false);
                    return result.Success;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to execute action {ActionType} for process {ProcessName}",
                    action.ActionType, action.TargetProcessName ?? "(foreground)");
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
                    _logger.LogDebug("No existing schedules file found, returning default configuration");
                    return ProfileConfiguration.CreateDefault();
                }

                var json = await File.ReadAllTextAsync(_configFilePath).ConfigureAwait(false);
                var config = JsonSerializer.Deserialize<ProfileConfiguration>(json, JsonOptions);

                if (config == null)
                {
                    _logger.LogWarning("Failed to deserialize schedules configuration, returning default");
                    return ProfileConfiguration.CreateDefault();
                }

                _logger.LogDebug("Loaded schedules configuration with {Count} scheduled actions", 
                    config.ScheduledActions.Count);
                return config;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load schedules configuration");
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
                    ScheduledActions = _scheduledActions.Values.ToList(),
                    LastSaved = DateTime.UtcNow
                };

                var json = JsonSerializer.Serialize(config, JsonOptions);
                await File.WriteAllTextAsync(_configFilePath, json).ConfigureAwait(false);

                _logger.LogDebug("Saved schedules configuration with {Count} actions", config.ScheduledActions.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to save schedules configuration");
                throw;
            }
            finally
            {
                _fileLock.Release();
            }
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            if (_disposed) return;

            Stop();
            _fileLock.Dispose();

            _disposed = true;
            _logger.LogDebug("SchedulerService disposed");
        }
    }
}
