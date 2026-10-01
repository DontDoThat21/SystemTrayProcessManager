using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Runtime.InteropServices;
using SystemTrayProcessManager.Core.Enums;
using SystemTrayProcessManager.Core.Models;
using SystemTrayProcessManager.Core.Services;
using SystemTrayProcessManager.Infrastructure.WindowsAPI;

namespace SystemTrayProcessManager.Infrastructure.Services
{
    /// <summary>
    /// Provides action mapping and execution capabilities for process management.
    /// Coordinates hotkey triggers with window and audio services to execute mapped actions.
    /// </summary>
    /// <remarks>
    /// This service integrates with the hotkey system to register actions and
    /// executes them when triggered. It supports both Quick Action mode (foreground window)
    /// and Pinned Process mode (specific process by name).
    /// </remarks>
    public sealed class ActionMappingService : IActionMappingService
    {
        private const int MaxHistoryEntries = 100;

        private readonly ILogger<ActionMappingService> _logger;
        private readonly IHotkeyService _hotkeyService;
        private readonly IHotkeyConfigurationService _configService;
        private readonly IWindowService _windowService;
        private readonly IAudioService _audioService;
        private readonly IProcessService _processService;

        private readonly ConcurrentDictionary<Guid, HotkeyConfigItem> _mappings = new();
        private readonly LinkedList<ActionHistoryEntry> _history = new();
        private readonly object _historyLock = new();

        private bool _isInitialized;
        private bool _disposed;

        /// <inheritdoc/>
        public bool IsInitialized => _isInitialized;

        /// <inheritdoc/>
        public int MappingCount => _mappings.Count;

        /// <inheritdoc/>
        public bool CanUndo
        {
            get
            {
                lock (_historyLock)
                {
                    return _history.Any(h => h.IsReversible && h.WasSuccessful);
                }
            }
        }

        /// <inheritdoc/>
        public event EventHandler<ActionExecutionResult>? ActionExecuted;

        /// <summary>
        /// Initializes a new instance of the <see cref="ActionMappingService"/> class.
        /// </summary>
        /// <param name="logger">Logger instance for diagnostic output.</param>
        /// <param name="hotkeyService">Service for hotkey registration.</param>
        /// <param name="configService">Service for hotkey configuration persistence.</param>
        /// <param name="windowService">Service for window manipulation.</param>
        /// <param name="audioService">Service for audio control.</param>
        /// <param name="processService">Service for process discovery.</param>
        /// <exception cref="ArgumentNullException">Thrown when any parameter is null.</exception>
        public ActionMappingService(
            ILogger<ActionMappingService> logger,
            IHotkeyService hotkeyService,
            IHotkeyConfigurationService configService,
            IWindowService windowService,
            IAudioService audioService,
            IProcessService processService)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _hotkeyService = hotkeyService ?? throw new ArgumentNullException(nameof(hotkeyService));
            _configService = configService ?? throw new ArgumentNullException(nameof(configService));
            _windowService = windowService ?? throw new ArgumentNullException(nameof(windowService));
            _audioService = audioService ?? throw new ArgumentNullException(nameof(audioService));
            _processService = processService ?? throw new ArgumentNullException(nameof(processService));

            _logger.LogDebug("ActionMappingService created");
        }

        /// <inheritdoc/>
        public async Task InitializeAsync()
        {
            ObjectDisposedException.ThrowIf(_disposed, this);

            if (_isInitialized)
            {
                throw new InvalidOperationException("ActionMappingService is already initialized.");
            }

            _logger.LogInformation("Initializing ActionMappingService...");

            try
            {
                // Ensure hotkey service is initialized
                if (!_hotkeyService.IsInitialized)
                {
                    _hotkeyService.Initialize();
                }

                // Load configuration
                var config = await _configService.LoadConfigurationAsync();

                if (config?.Items != null)
                {
                    foreach (var item in config.Items.Where(i => i.IsEnabled))
                    {
                        RegisterActionMapping(item);
                    }

                    _logger.LogInformation("Loaded {Count} action mappings from configuration", config.Items.Count);
                }

                _isInitialized = true;
                _logger.LogInformation("ActionMappingService initialized successfully with {Count} mappings", _mappings.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to initialize ActionMappingService");
                throw;
            }
        }

        /// <inheritdoc/>
        public bool RegisterActionMapping(HotkeyConfigItem config)
        {
            ArgumentNullException.ThrowIfNull(config);
            ObjectDisposedException.ThrowIf(_disposed, this);

            if (!config.IsEnabled || !config.IsValid)
            {
                _logger.LogDebug("Skipping invalid or disabled config item: {Name}", config.Name);
                return false;
            }

            try
            {
                // Keep live callbacks independent of mutable editor/configuration objects.
                config = config.Clone();
                var binding = config.ToHotkeyBinding();

                // Create action callback based on config
                Func<Task> actionCallback = async () =>
                {
                    await ExecuteConfiguredActionAsync(config);
                };

                // Register with hotkey service
                bool registered = _hotkeyService.RegisterHotkey(binding, actionCallback, suppressKey: true);

                if (registered)
                {
                    _mappings[config.Id] = config;
                    _logger.LogInformation("Registered action mapping: {Name} ({Binding}) -> {ActionType}",
                        config.Name, config.DisplayString, config.ActionType);
                    return true;
                }
                else
                {
                    _logger.LogWarning("Failed to register hotkey for action mapping: {Name}", config.Name);
                    return false;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error registering action mapping: {Name}", config.Name);
                return false;
            }
        }

        /// <inheritdoc/>
        public bool UnregisterActionMapping(Guid configId)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);

            if (_mappings.TryRemove(configId, out var config))
            {
                var binding = config.ToHotkeyBinding();
                _hotkeyService.UnregisterHotkey(binding);
                _logger.LogInformation("Unregistered action mapping: {Name}", config.Name);
                return true;
            }

            return false;
        }

        /// <inheritdoc/>
        public void UnregisterAllMappings()
        {
            ObjectDisposedException.ThrowIf(_disposed, this);

            foreach (var config in _mappings.Values)
            {
                var binding = config.ToHotkeyBinding();
                _hotkeyService.UnregisterHotkey(binding);
            }

            _mappings.Clear();
            _logger.LogInformation("Unregistered all action mappings");
        }

        /// <inheritdoc/>
        public async Task<ActionExecutionResult> ExecuteQuickActionAsync(ProcessActionType actionType)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);

            _logger.LogDebug("Executing Quick Action: {ActionType}", actionType);

            var stopwatch = Stopwatch.StartNew();

            try
            {
                // Get the foreground window
                IntPtr hwnd = NativeMethods.GetForegroundWindow();

                if (hwnd == IntPtr.Zero)
                {
                    var noWindowResult = ActionExecutionResult.CreateNoForegroundWindow(actionType);
                    RaiseActionExecuted(noWindowResult);
                    return noWindowResult;
                }

                // Get process info from window handle
                NativeMethods.GetWindowThreadProcessId(hwnd, out uint processId);

                string processName = "Unknown";
                try
                {
                    using var process = Process.GetProcessById((int)processId);
                    processName = process.ProcessName;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to get process name for PID {ProcessId}", processId);
                }

                var result = await ExecuteActionOnTargetAsync(actionType, (int)processId, processName, hwnd, ActionMode.QuickAction);

                stopwatch.Stop();
                _logger.LogDebug("Quick Action completed in {ElapsedMs}ms", stopwatch.ElapsedMilliseconds);

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error executing Quick Action: {ActionType}", actionType);
                var errorResult = ActionExecutionResult.CreateFailure(actionType, ex.Message);
                RaiseActionExecuted(errorResult);
                return errorResult;
            }
        }

        /// <inheritdoc/>
        public async Task<ActionExecutionResult> ExecutePinnedActionAsync(ProcessActionType actionType, string processName)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(processName);
            ObjectDisposedException.ThrowIf(_disposed, this);

            _logger.LogDebug("Executing Pinned Action: {ActionType} on {ProcessName}", actionType, processName);

            var stopwatch = Stopwatch.StartNew();

            try
            {
                // Find the target process
                var targetName = processName.Trim();
                if (targetName.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
                {
                    targetName = targetName[..^4];
                }
                var processes = await _processService.GetRunningProcessesAsync();
                var targetProcess = processes.FirstOrDefault(p =>
                    p.Name.Equals(targetName, StringComparison.OrdinalIgnoreCase));

                // Audio applications may continue running without a visible window.
                if (targetProcess == null && actionType is ProcessActionType.Mute or ProcessActionType.Unmute or ProcessActionType.ToggleMute)
                {
                    var audioProcesses = await _audioService.GetAudioProcessesAsync();
                    var audioProcess = audioProcesses.FirstOrDefault(p =>
                        p.ProcessName.Equals(targetName, StringComparison.OrdinalIgnoreCase));
                    if (audioProcess != null)
                    {
                        targetProcess = new ProcessInfo { ProcessId = audioProcess.ProcessId, Name = audioProcess.ProcessName };
                    }
                }

                targetProcess ??= processes.FirstOrDefault(p =>
                    p.Name.Contains(targetName, StringComparison.OrdinalIgnoreCase));

                if (targetProcess == null)
                {
                    _logger.LogWarning("Process not found for Pinned Action: {ProcessName}", processName);
                    var notFoundResult = ActionExecutionResult.CreateProcessNotFound(actionType, processName);
                    RaiseActionExecuted(notFoundResult);
                    return notFoundResult;
                }

                var result = await ExecuteActionOnTargetAsync(
                    actionType,
                    targetProcess.ProcessId,
                    targetProcess.Name,
                    targetProcess.WindowHandle,
                    ActionMode.PinnedProcess);

                stopwatch.Stop();
                _logger.LogDebug("Pinned Action completed in {ElapsedMs}ms", stopwatch.ElapsedMilliseconds);

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error executing Pinned Action: {ActionType} on {ProcessName}", actionType, processName);
                var errorResult = ActionExecutionResult.CreateFailure(actionType, ex.Message, processName: processName);
                RaiseActionExecuted(errorResult);
                return errorResult;
            }
        }

        /// <inheritdoc/>
        public IReadOnlyList<ActionHistoryEntry> GetActionHistory(int maxEntries = 50)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);

            lock (_historyLock)
            {
                return _history.Take(Math.Min(maxEntries, _history.Count)).ToList().AsReadOnly();
            }
        }

        /// <inheritdoc/>
        public async Task<ActionExecutionResult> UndoLastActionAsync()
        {
            ObjectDisposedException.ThrowIf(_disposed, this);

            ActionHistoryEntry? lastReversible;

            lock (_historyLock)
            {
                lastReversible = _history.FirstOrDefault(h => h.IsReversible && h.WasSuccessful);
            }

            if (lastReversible == null || !lastReversible.ReverseActionType.HasValue)
            {
                _logger.LogDebug("No reversible action available for undo");
                return ActionExecutionResult.CreateFailure(ProcessActionType.None, "No action available to undo.");
            }

            _logger.LogInformation("Undoing action: {ActionType} on {ProcessName}",
                lastReversible.ActionType, lastReversible.ProcessName);

            // Execute the reverse action
            var result = await ExecuteActionOnTargetAsync(
                lastReversible.ReverseActionType.Value,
                lastReversible.ProcessId,
                lastReversible.ProcessName,
                lastReversible.WindowHandle,
                lastReversible.Mode,
                isUndo: true);

            // Remove the undone entry from history
            if (result.Success)
            {
                lock (_historyLock)
                {
                    var node = _history.Find(lastReversible);
                    if (node != null)
                    {
                        _history.Remove(node);
                    }
                }
            }

            return result;
        }

        /// <inheritdoc/>
        public void ClearHistory()
        {
            ObjectDisposedException.ThrowIf(_disposed, this);

            lock (_historyLock)
            {
                _history.Clear();
            }

            _logger.LogDebug("Action history cleared");
        }

        /// <inheritdoc/>
        public async Task ReloadMappingsAsync()
        {
            ObjectDisposedException.ThrowIf(_disposed, this);

            _logger.LogInformation("Reloading action mappings...");

            // Read before removing working bindings so a read failure leaves them intact.
            var config = await _configService.LoadConfigurationAsync();
            UnregisterAllMappings();

            bool registrationFailed = false;
            if (config?.Items != null)
            {
                foreach (var item in config.Items.Where(i => i.IsEnabled))
                {
                    registrationFailed |= !RegisterActionMapping(item);
                }
            }

            _logger.LogInformation("Reloaded {Count} action mappings", _mappings.Count);
            if (registrationFailed)
            {
                throw new InvalidOperationException("One or more saved hotkeys could not be registered. Check for duplicate shortcuts.");
            }
        }

        /// <inheritdoc/>
        public void Shutdown()
        {
            if (_disposed) return;

            _logger.LogInformation("Shutting down ActionMappingService...");

            UnregisterAllMappings();
            ClearHistory();

            _isInitialized = false;
            _logger.LogInformation("ActionMappingService shut down");
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            if (_disposed) return;

            _logger.LogDebug("Disposing ActionMappingService...");

            Shutdown();

            _disposed = true;
        }

        /// <summary>
        /// Executes the action configured in the hotkey config item.
        /// </summary>
        private async Task ExecuteConfiguredActionAsync(HotkeyConfigItem config)
        {
            if (!TryParseActionType(config.ActionType, out var actionType))
            {
                _logger.LogWarning("Unknown action type in config: {ActionType}", config.ActionType);
                return;
            }

            ActionExecutionResult result;

            // Older editor versions saved targets without updating ActionMode.
            if (!string.IsNullOrWhiteSpace(config.TargetProcessName))
            {
                result = await ExecutePinnedActionAsync(actionType, config.TargetProcessName);
            }
            else
            {
                result = await ExecuteQuickActionAsync(actionType);
            }

            _logger.LogDebug("Configured action executed: {ActionType} -> {Success}", config.ActionType, result.Success);
        }

        /// <summary>
        /// Executes an action on the specified target.
        /// </summary>
        private async Task<ActionExecutionResult> ExecuteActionOnTargetAsync(
            ProcessActionType actionType,
            int processId,
            string processName,
            IntPtr windowHandle,
            ActionMode mode,
            bool isUndo = false)
        {
            bool success = false;
            object? previousState = null;

            try
            {
                switch (actionType)
                {
                    case ProcessActionType.Mute:
                        success = await _audioService.MuteProcessAsync(processId);
                        break;

                    case ProcessActionType.Unmute:
                        success = await _audioService.UnmuteProcessAsync(processId);
                        break;

                    case ProcessActionType.ToggleMute:
                        success = await _audioService.ToggleMuteProcessAsync(processId);
                        break;

                    case ProcessActionType.Close:
                        if (windowHandle != IntPtr.Zero && _windowService.IsValidWindow(windowHandle))
                        {
                            success = await _windowService.CloseAsync(windowHandle, force: false);
                        }
                        break;

                    case ProcessActionType.Minimize:
                        if (windowHandle != IntPtr.Zero && _windowService.IsValidWindow(windowHandle))
                        {
                            previousState = await _windowService.GetWindowStateAsync(windowHandle);
                            success = await _windowService.MinimizeAsync(windowHandle);
                        }
                        break;

                    case ProcessActionType.Maximize:
                        if (windowHandle != IntPtr.Zero && _windowService.IsValidWindow(windowHandle))
                        {
                            previousState = await _windowService.GetWindowStateAsync(windowHandle);
                            success = await _windowService.MaximizeAsync(windowHandle);
                        }
                        break;

                    case ProcessActionType.Restore:
                        if (windowHandle != IntPtr.Zero && _windowService.IsValidWindow(windowHandle))
                        {
                            success = await _windowService.RestoreAsync(windowHandle);
                        }
                        break;

                    case ProcessActionType.BringToFront:
                        if (windowHandle != IntPtr.Zero && _windowService.IsValidWindow(windowHandle))
                        {
                            success = await _windowService.BringToFrontAsync(windowHandle);
                        }
                        break;

                    case ProcessActionType.Hide:
                        if (windowHandle != IntPtr.Zero && _windowService.IsValidWindow(windowHandle))
                        {
                            success = await _windowService.HideAsync(windowHandle);
                        }
                        break;

                    case ProcessActionType.Show:
                        if (windowHandle != IntPtr.Zero && _windowService.IsValidWindow(windowHandle))
                        {
                            success = await _windowService.ShowAsync(windowHandle);
                        }
                        break;

                    default:
                        _logger.LogWarning("Unsupported action type: {ActionType}", actionType);
                        break;
                }

                if (success)
                {
                    _logger.LogInformation("Action {ActionType} executed successfully on {ProcessName} (PID: {ProcessId})",
                        actionType, processName, processId);
                }
                else
                {
                    _logger.LogWarning("Action {ActionType} failed on {ProcessName} (PID: {ProcessId})",
                        actionType, processName, processId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error executing action {ActionType} on {ProcessName}", actionType, processName);
                success = false;
            }

            // Create history entry (don't create for undo operations)
            ActionHistoryEntry? historyEntry = null;
            if (!isUndo)
            {
                historyEntry = ActionHistoryEntry.Create(
                    actionType,
                    mode,
                    processId,
                    processName,
                    windowHandle,
                    success,
                    previousState);

                AddToHistory(historyEntry);
            }

            var result = success
                ? ActionExecutionResult.CreateSuccess(actionType, processId, processName, windowHandle, historyEntry)
                : ActionExecutionResult.CreateFailure(actionType, "Action execution failed.", processId, processName);

            RaiseActionExecuted(result);
            return result;
        }

        /// <summary>
        /// Adds an entry to the action history.
        /// </summary>
        private void AddToHistory(ActionHistoryEntry entry)
        {
            lock (_historyLock)
            {
                _history.AddFirst(entry);

                // Trim history if it exceeds max entries
                while (_history.Count > MaxHistoryEntries)
                {
                    _history.RemoveLast();
                }
            }

            _logger.LogDebug("Added to history: {Entry}", entry);
        }

        /// <summary>
        /// Raises the ActionExecuted event.
        /// </summary>
        private void RaiseActionExecuted(ActionExecutionResult result)
        {
            try
            {
                ActionExecuted?.Invoke(this, result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in ActionExecuted event handler");
            }
        }

        /// <summary>
        /// Tries to parse the action type from a string.
        /// </summary>
        private static bool TryParseActionType(string actionTypeString, out ProcessActionType actionType)
        {
            // Handle common action type strings
            actionType = actionTypeString?.ToLowerInvariant() switch
            {
                "mute" => ProcessActionType.Mute,
                "unmute" => ProcessActionType.Unmute,
                "togglemute" or "toggle_mute" or "toggle mute" => ProcessActionType.ToggleMute,
                "close" => ProcessActionType.Close,
                "minimize" => ProcessActionType.Minimize,
                "maximize" => ProcessActionType.Maximize,
                "restore" => ProcessActionType.Restore,
                "bringtofront" or "bring_to_front" or "bring to front" or "focus" => ProcessActionType.BringToFront,
                "hide" => ProcessActionType.Hide,
                "show" => ProcessActionType.Show,
                _ => ProcessActionType.None
            };

            // Also try enum parsing
            if (actionType == ProcessActionType.None && !string.IsNullOrEmpty(actionTypeString))
            {
                if (Enum.TryParse<ProcessActionType>(actionTypeString, ignoreCase: true, out var parsed))
                {
                    actionType = parsed;
                }
            }

            return actionType != ProcessActionType.None;
        }
    }
}
