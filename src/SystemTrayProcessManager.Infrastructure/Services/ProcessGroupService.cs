using System.Collections.Concurrent;
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
    /// Provides process group management for organizing processes and executing batch operations.
    /// </summary>
    public sealed class ProcessGroupService : IProcessGroupService
    {
        private readonly ILogger<ProcessGroupService> _logger;
        private readonly IProcessService _processService;
        private readonly IActionMappingService _actionMappingService;
        private readonly string _configDirectory;
        private readonly string _configFilePath;
        private readonly ConcurrentDictionary<Guid, ProcessGroup> _processGroups;
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
        public event EventHandler<BatchActionExecutedEventArgs>? BatchActionExecuted;

        /// <summary>
        /// Initializes a new instance of the <see cref="ProcessGroupService"/> class.
        /// </summary>
        /// <param name="logger">The logger instance.</param>
        /// <param name="processService">The process monitoring service.</param>
        /// <param name="actionMappingService">The action mapping service for executing actions.</param>
        public ProcessGroupService(
            ILogger<ProcessGroupService> logger,
            IProcessService processService,
            IActionMappingService actionMappingService)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _processService = processService ?? throw new ArgumentNullException(nameof(processService));
            _actionMappingService = actionMappingService ?? throw new ArgumentNullException(nameof(actionMappingService));

            _processGroups = new ConcurrentDictionary<Guid, ProcessGroup>();

            _configDirectory = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "SystemTrayProcessManager");

            _configFilePath = Path.Combine(_configDirectory, "groups.json");

            _logger.LogDebug("ProcessGroupService initialized. Config path: {Path}", _configFilePath);
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
                foreach (var group in config.ProcessGroups)
                {
                    _processGroups[group.Id] = group;
                }

                _isInitialized = true;
                _logger.LogInformation("ProcessGroupService initialized with {Count} process groups", _processGroups.Count);
            }
            finally
            {
                _fileLock.Release();
            }
        }

        /// <inheritdoc/>
        public async Task<IReadOnlyList<ProcessGroup>> GetGroupsAsync()
        {
            await EnsureInitializedAsync().ConfigureAwait(false);

            var groups = _processGroups.Values.ToList();
            _logger.LogDebug("Returning {Count} process groups", groups.Count);
            return groups.AsReadOnly();
        }

        /// <inheritdoc/>
        public async Task<ProcessGroup?> GetGroupAsync(Guid groupId)
        {
            await EnsureInitializedAsync().ConfigureAwait(false);

            if (_processGroups.TryGetValue(groupId, out var group))
            {
                _logger.LogDebug("Found process group {GroupId}: {Name}", groupId, group.Name);
                return group;
            }

            return null;
        }

        /// <inheritdoc/>
        public async Task<ProcessGroup?> GetGroupByNameAsync(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                return null;
            }

            await EnsureInitializedAsync().ConfigureAwait(false);

            var group = _processGroups.Values
                .FirstOrDefault(g => g.Name.Equals(name, StringComparison.OrdinalIgnoreCase));

            if (group != null)
            {
                _logger.LogDebug("Found process group by name: {Name}", name);
            }

            return group;
        }

        /// <inheritdoc/>
        public async Task<bool> SaveGroupAsync(ProcessGroup group)
        {
            ArgumentNullException.ThrowIfNull(group);

            if (string.IsNullOrWhiteSpace(group.Name))
            {
                _logger.LogWarning("Cannot save process group with empty name");
                return false;
            }

            try
            {
                await EnsureInitializedAsync().ConfigureAwait(false);

                // Update timestamp if this is an update
                var updatedGroup = _processGroups.ContainsKey(group.Id)
                    ? new ProcessGroup
                    {
                        Id = group.Id,
                        Name = group.Name,
                        Description = group.Description,
                        ProcessPatterns = group.ProcessPatterns,
                        CreatedAt = group.CreatedAt,
                        LastModified = DateTime.UtcNow
                    }
                    : group;

                _processGroups[group.Id] = updatedGroup;
                await SaveConfigurationAsync().ConfigureAwait(false);

                _logger.LogInformation("Saved process group {GroupId}: {Name}", group.Id, group.Name);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to save process group {GroupId}", group.Id);
                return false;
            }
        }

        /// <inheritdoc/>
        public async Task<bool> DeleteGroupAsync(Guid groupId)
        {
            try
            {
                await EnsureInitializedAsync().ConfigureAwait(false);

                if (_processGroups.TryRemove(groupId, out var removed))
                {
                    await SaveConfigurationAsync().ConfigureAwait(false);
                    _logger.LogInformation("Deleted process group {GroupId}: {Name}", groupId, removed.Name);
                    return true;
                }

                _logger.LogWarning("Cannot delete process group - not found: {GroupId}", groupId);
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to delete process group {GroupId}", groupId);
                return false;
            }
        }

        /// <inheritdoc/>
        public async Task<IReadOnlyList<ProcessInfo>> GetMatchingProcessesAsync(Guid groupId)
        {
            await EnsureInitializedAsync().ConfigureAwait(false);

            if (!_processGroups.TryGetValue(groupId, out var group))
            {
                _logger.LogWarning("Cannot get matching processes - group not found: {GroupId}", groupId);
                return Array.Empty<ProcessInfo>();
            }

            return await GetMatchingProcessesAsync(group).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public async Task<IReadOnlyList<ProcessInfo>> GetMatchingProcessesAsync(ProcessGroup group)
        {
            ArgumentNullException.ThrowIfNull(group);

            try
            {
                var allProcesses = await _processService.GetRunningProcessesAsync().ConfigureAwait(false);

                var matchingProcesses = allProcesses
                    .Where(p => group.MatchesProcess(p.Name))
                    .ToList();

                _logger.LogDebug("Found {Count} processes matching group {GroupName}",
                    matchingProcesses.Count, group.Name);

                return matchingProcesses.AsReadOnly();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get matching processes for group {GroupId}", group.Id);
                return Array.Empty<ProcessInfo>();
            }
        }

        /// <inheritdoc/>
        public async Task<BatchActionResult> ExecuteBatchActionAsync(Guid groupId, ProcessActionType action)
        {
            await EnsureInitializedAsync().ConfigureAwait(false);

            if (!_processGroups.TryGetValue(groupId, out var group))
            {
                _logger.LogWarning("Cannot execute batch action - group not found: {GroupId}", groupId);
                return BatchActionResult.NoProcesses(new ProcessGroup { Name = "Unknown" }, action);
            }

            return await ExecuteBatchActionAsync(group, action).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public async Task<BatchActionResult> ExecuteBatchActionAsync(ProcessGroup group, ProcessActionType action)
        {
            ArgumentNullException.ThrowIfNull(group);

            try
            {
                _logger.LogDebug("Executing batch action {Action} on group {GroupName}", action, group.Name);

                var matchingProcesses = await GetMatchingProcessesAsync(group).ConfigureAwait(false);

                if (matchingProcesses.Count == 0)
                {
                    _logger.LogDebug("No matching processes found for group {GroupName}", group.Name);
                    var emptyResult = BatchActionResult.NoProcesses(group, action);
                    BatchActionExecuted?.Invoke(this, new BatchActionExecutedEventArgs(emptyResult, DateTime.UtcNow));
                    return emptyResult;
                }

                var successCount = 0;
                var failureCount = 0;

                foreach (var process in matchingProcesses)
                {
                    try
                    {
                        var result = await _actionMappingService.ExecutePinnedActionAsync(
                            action, process.Name).ConfigureAwait(false);

                        if (result.Success)
                        {
                            successCount++;
                        }
                        else
                        {
                            failureCount++;
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to execute action on process {ProcessName}", process.Name);
                        failureCount++;
                    }
                }

                var batchResult = new BatchActionResult(
                    group,
                    action,
                    matchingProcesses.Count,
                    successCount,
                    failureCount);

                _logger.LogInformation("Batch action {Action} on group {GroupName}: {Success}/{Total} succeeded",
                    action, group.Name, successCount, matchingProcesses.Count);

                BatchActionExecuted?.Invoke(this, new BatchActionExecutedEventArgs(batchResult, DateTime.UtcNow));

                return batchResult;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to execute batch action on group {GroupId}", group.Id);
                return new BatchActionResult(group, action, 0, 0, 1);
            }
        }

        /// <inheritdoc/>
        public async Task<bool> AddPatternAsync(Guid groupId, string pattern)
        {
            if (string.IsNullOrWhiteSpace(pattern))
            {
                _logger.LogWarning("Cannot add empty pattern to group");
                return false;
            }

            try
            {
                await EnsureInitializedAsync().ConfigureAwait(false);

                if (!_processGroups.TryGetValue(groupId, out var group))
                {
                    _logger.LogWarning("Cannot add pattern - group not found: {GroupId}", groupId);
                    return false;
                }

                var updatedGroup = group.WithAddedPattern(pattern);
                _processGroups[groupId] = updatedGroup;
                await SaveConfigurationAsync().ConfigureAwait(false);

                _logger.LogInformation("Added pattern '{Pattern}' to group {GroupName}", pattern, group.Name);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to add pattern to group {GroupId}", groupId);
                return false;
            }
        }

        /// <inheritdoc/>
        public async Task<bool> RemovePatternAsync(Guid groupId, string pattern)
        {
            if (string.IsNullOrWhiteSpace(pattern))
            {
                return false;
            }

            try
            {
                await EnsureInitializedAsync().ConfigureAwait(false);

                if (!_processGroups.TryGetValue(groupId, out var group))
                {
                    _logger.LogWarning("Cannot remove pattern - group not found: {GroupId}", groupId);
                    return false;
                }

                var updatedGroup = group.WithRemovedPattern(pattern);
                _processGroups[groupId] = updatedGroup;
                await SaveConfigurationAsync().ConfigureAwait(false);

                _logger.LogInformation("Removed pattern '{Pattern}' from group {GroupName}", pattern, group.Name);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to remove pattern from group {GroupId}", groupId);
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
                    _logger.LogDebug("No existing groups file found, returning default configuration");
                    return ProfileConfiguration.CreateDefault();
                }

                var json = await File.ReadAllTextAsync(_configFilePath).ConfigureAwait(false);
                var config = JsonSerializer.Deserialize<ProfileConfiguration>(json, JsonOptions);

                if (config == null)
                {
                    _logger.LogWarning("Failed to deserialize groups configuration, returning default");
                    return ProfileConfiguration.CreateDefault();
                }

                _logger.LogDebug("Loaded groups configuration with {Count} groups", config.ProcessGroups.Count);
                return config;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load groups configuration");
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
                    ProcessGroups = _processGroups.Values.ToList(),
                    LastSaved = DateTime.UtcNow
                };

                var json = JsonSerializer.Serialize(config, JsonOptions);
                await File.WriteAllTextAsync(_configFilePath, json).ConfigureAwait(false);

                _logger.LogDebug("Saved groups configuration with {Count} groups", config.ProcessGroups.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to save groups configuration");
                throw;
            }
            finally
            {
                _fileLock.Release();
            }
        }
    }
}
