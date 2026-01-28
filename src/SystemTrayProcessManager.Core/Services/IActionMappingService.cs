using SystemTrayProcessManager.Core.Enums;
using SystemTrayProcessManager.Core.Models;

namespace SystemTrayProcessManager.Core.Services
{
    /// <summary>
    /// Provides action mapping and execution capabilities for process management.
    /// Maps hotkeys to specific actions and coordinates with window and audio services.
    /// </summary>
    /// <remarks>
    /// The action mapping service supports two execution modes:
    /// <list type="bullet">
    /// <item><description><see cref="ActionMode.QuickAction"/>: Targets the currently focused window</description></item>
    /// <item><description><see cref="ActionMode.PinnedProcess"/>: Targets a specific process by name</description></item>
    /// </list>
    /// </remarks>
    public interface IActionMappingService : IDisposable
    {
        /// <summary>
        /// Gets a value indicating whether the service is initialized and ready to execute actions.
        /// </summary>
        bool IsInitialized { get; }

        /// <summary>
        /// Gets the number of registered action mappings.
        /// </summary>
        int MappingCount { get; }

        /// <summary>
        /// Gets a value indicating whether undo is available (at least one reversible action in history).
        /// </summary>
        bool CanUndo { get; }

        /// <summary>
        /// Occurs when an action is executed (successfully or not).
        /// </summary>
        event EventHandler<ActionExecutionResult>? ActionExecuted;

        /// <summary>
        /// Initializes the action mapping service and loads configured mappings.
        /// </summary>
        /// <returns>A task representing the initialization operation.</returns>
        /// <exception cref="InvalidOperationException">Thrown if the service is already initialized.</exception>
        Task InitializeAsync();

        /// <summary>
        /// Registers an action mapping for a hotkey configuration.
        /// The hotkey will be registered with the hotkey service and the action will execute when triggered.
        /// </summary>
        /// <param name="config">The hotkey configuration item containing the action mapping.</param>
        /// <returns>True if the mapping was registered successfully; otherwise, false.</returns>
        /// <exception cref="ArgumentNullException">Thrown when config is null.</exception>
        bool RegisterActionMapping(HotkeyConfigItem config);

        /// <summary>
        /// Unregisters an action mapping by its configuration ID.
        /// </summary>
        /// <param name="configId">The unique identifier of the configuration to unregister.</param>
        /// <returns>True if the mapping was found and unregistered; otherwise, false.</returns>
        bool UnregisterActionMapping(Guid configId);

        /// <summary>
        /// Unregisters all action mappings.
        /// </summary>
        void UnregisterAllMappings();

        /// <summary>
        /// Executes an action using Quick Action mode, targeting the currently focused window.
        /// </summary>
        /// <param name="actionType">The type of action to execute.</param>
        /// <returns>A task containing the result of the action execution.</returns>
        Task<ActionExecutionResult> ExecuteQuickActionAsync(ProcessActionType actionType);

        /// <summary>
        /// Executes an action targeting a specific process by name.
        /// </summary>
        /// <param name="actionType">The type of action to execute.</param>
        /// <param name="processName">The name of the target process (case-insensitive).</param>
        /// <returns>A task containing the result of the action execution.</returns>
        /// <exception cref="ArgumentException">Thrown when processName is null or empty.</exception>
        Task<ActionExecutionResult> ExecutePinnedActionAsync(ProcessActionType actionType, string processName);

        /// <summary>
        /// Gets the action history, ordered by most recent first.
        /// </summary>
        /// <param name="maxEntries">The maximum number of entries to return (default 50).</param>
        /// <returns>A read-only list of action history entries.</returns>
        IReadOnlyList<ActionHistoryEntry> GetActionHistory(int maxEntries = 50);

        /// <summary>
        /// Undoes the last reversible action in the history.
        /// </summary>
        /// <returns>A task containing the result of the undo operation.</returns>
        /// <remarks>
        /// Only reversible actions (mute/unmute, minimize/restore, hide/show) can be undone.
        /// Actions like Close and BringToFront cannot be undone.
        /// </remarks>
        Task<ActionExecutionResult> UndoLastActionAsync();

        /// <summary>
        /// Clears the action history.
        /// </summary>
        void ClearHistory();

        /// <summary>
        /// Reloads action mappings from the configuration service.
        /// This unregisters all current mappings and re-registers from saved configuration.
        /// </summary>
        /// <returns>A task representing the reload operation.</returns>
        Task ReloadMappingsAsync();

        /// <summary>
        /// Shuts down the action mapping service and unregisters all mappings.
        /// </summary>
        void Shutdown();
    }
}
