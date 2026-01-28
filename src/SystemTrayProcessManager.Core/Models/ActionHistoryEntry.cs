using SystemTrayProcessManager.Core.Enums;

namespace SystemTrayProcessManager.Core.Models
{
    /// <summary>
    /// Represents a record of an executed action for history tracking and undo functionality.
    /// </summary>
    /// <remarks>
    /// History entries are immutable once created. The <see cref="PreviousState"/> property
    /// stores any state needed to reverse the action (e.g., previous window state for restore).
    /// </remarks>
    public sealed class ActionHistoryEntry
    {
        /// <summary>
        /// Gets the unique identifier for this history entry.
        /// </summary>
        public Guid Id { get; init; }

        /// <summary>
        /// Gets the timestamp when the action was executed.
        /// </summary>
        public DateTime Timestamp { get; init; }

        /// <summary>
        /// Gets the type of action that was executed.
        /// </summary>
        public ProcessActionType ActionType { get; init; }

        /// <summary>
        /// Gets the mode used to identify the target process.
        /// </summary>
        public ActionMode Mode { get; init; }

        /// <summary>
        /// Gets the process ID of the target process.
        /// </summary>
        public int ProcessId { get; init; }

        /// <summary>
        /// Gets the name of the target process.
        /// </summary>
        public string ProcessName { get; init; } = string.Empty;

        /// <summary>
        /// Gets the window handle of the target window.
        /// </summary>
        public IntPtr WindowHandle { get; init; }

        /// <summary>
        /// Gets a value indicating whether the action executed successfully.
        /// </summary>
        public bool WasSuccessful { get; init; }

        /// <summary>
        /// Gets a value indicating whether this action can be reversed (undone).
        /// </summary>
        public bool IsReversible { get; init; }

        /// <summary>
        /// Gets the action type that would reverse this action, if applicable.
        /// </summary>
        public ProcessActionType? ReverseActionType { get; init; }

        /// <summary>
        /// Gets the previous state data needed to perform an undo operation.
        /// The type of data depends on the action (e.g., WindowState for minimize/maximize).
        /// </summary>
        public object? PreviousState { get; init; }

        /// <summary>
        /// Creates a new <see cref="ActionHistoryEntry"/> with the specified values.
        /// </summary>
        /// <param name="actionType">The type of action executed.</param>
        /// <param name="mode">The mode used to identify the target.</param>
        /// <param name="processId">The target process ID.</param>
        /// <param name="processName">The target process name.</param>
        /// <param name="windowHandle">The target window handle.</param>
        /// <param name="wasSuccessful">Whether the action succeeded.</param>
        /// <param name="previousState">Optional previous state for undo.</param>
        /// <returns>A new history entry instance.</returns>
        public static ActionHistoryEntry Create(
            ProcessActionType actionType,
            ActionMode mode,
            int processId,
            string processName,
            IntPtr windowHandle,
            bool wasSuccessful,
            object? previousState = null)
        {
            bool isReversible = GetIsReversible(actionType);
            ProcessActionType? reverseAction = GetReverseAction(actionType);

            return new ActionHistoryEntry
            {
                Id = Guid.NewGuid(),
                Timestamp = DateTime.UtcNow,
                ActionType = actionType,
                Mode = mode,
                ProcessId = processId,
                ProcessName = processName ?? string.Empty,
                WindowHandle = windowHandle,
                WasSuccessful = wasSuccessful,
                IsReversible = isReversible && wasSuccessful,
                ReverseActionType = reverseAction,
                PreviousState = previousState
            };
        }

        /// <summary>
        /// Determines whether the specified action type is reversible.
        /// </summary>
        /// <param name="actionType">The action type to check.</param>
        /// <returns>True if the action can be undone; otherwise, false.</returns>
        public static bool GetIsReversible(ProcessActionType actionType)
        {
            return actionType switch
            {
                ProcessActionType.Mute => true,
                ProcessActionType.Unmute => true,
                ProcessActionType.ToggleMute => true,
                ProcessActionType.Minimize => true,
                ProcessActionType.Maximize => true,
                ProcessActionType.Restore => true,
                ProcessActionType.Hide => true,
                ProcessActionType.Show => true,
                ProcessActionType.Close => false,
                ProcessActionType.BringToFront => false,
                _ => false
            };
        }

        /// <summary>
        /// Gets the action type that reverses the specified action.
        /// </summary>
        /// <param name="actionType">The action type to get the reverse for.</param>
        /// <returns>The reverse action type, or null if not reversible.</returns>
        public static ProcessActionType? GetReverseAction(ProcessActionType actionType)
        {
            return actionType switch
            {
                ProcessActionType.Mute => ProcessActionType.Unmute,
                ProcessActionType.Unmute => ProcessActionType.Mute,
                ProcessActionType.ToggleMute => ProcessActionType.ToggleMute,
                ProcessActionType.Minimize => ProcessActionType.Restore,
                ProcessActionType.Maximize => ProcessActionType.Restore,
                ProcessActionType.Restore => null, // Context-dependent
                ProcessActionType.Hide => ProcessActionType.Show,
                ProcessActionType.Show => ProcessActionType.Hide,
                _ => null
            };
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            return $"[{Timestamp:HH:mm:ss}] {ActionType} on {ProcessName} (PID: {ProcessId}) - {(WasSuccessful ? "Success" : "Failed")}";
        }
    }
}
