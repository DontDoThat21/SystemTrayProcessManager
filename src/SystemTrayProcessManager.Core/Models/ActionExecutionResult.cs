using SystemTrayProcessManager.Core.Enums;

namespace SystemTrayProcessManager.Core.Models
{
    /// <summary>
    /// Represents the result of executing an action on a process or window.
    /// </summary>
    /// <remarks>
    /// This class provides detailed information about action execution including
    /// success status, error messages, and the history entry if tracking is enabled.
    /// </remarks>
    public sealed class ActionExecutionResult
    {
        /// <summary>
        /// Gets a value indicating whether the action executed successfully.
        /// </summary>
        public bool Success { get; init; }

        /// <summary>
        /// Gets the type of action that was executed.
        /// </summary>
        public ProcessActionType ActionType { get; init; }

        /// <summary>
        /// Gets the process ID of the target process, if available.
        /// </summary>
        public int? ProcessId { get; init; }

        /// <summary>
        /// Gets the name of the target process, if available.
        /// </summary>
        public string? ProcessName { get; init; }

        /// <summary>
        /// Gets the window handle of the target window, if applicable.
        /// </summary>
        public IntPtr? WindowHandle { get; init; }

        /// <summary>
        /// Gets an error message describing why the action failed, if applicable.
        /// </summary>
        public string? ErrorMessage { get; init; }

        /// <summary>
        /// Gets the history entry created for this action, if tracking is enabled.
        /// </summary>
        public ActionHistoryEntry? HistoryEntry { get; init; }

        /// <summary>
        /// Creates a successful result for the specified action.
        /// </summary>
        /// <param name="actionType">The type of action executed.</param>
        /// <param name="processId">The target process ID.</param>
        /// <param name="processName">The target process name.</param>
        /// <param name="windowHandle">The target window handle.</param>
        /// <param name="historyEntry">Optional history entry for tracking.</param>
        /// <returns>A success result instance.</returns>
        public static ActionExecutionResult CreateSuccess(
            ProcessActionType actionType,
            int processId,
            string processName,
            IntPtr windowHandle,
            ActionHistoryEntry? historyEntry = null)
        {
            return new ActionExecutionResult
            {
                Success = true,
                ActionType = actionType,
                ProcessId = processId,
                ProcessName = processName,
                WindowHandle = windowHandle,
                ErrorMessage = null,
                HistoryEntry = historyEntry
            };
        }

        /// <summary>
        /// Creates a failure result with the specified error message.
        /// </summary>
        /// <param name="actionType">The type of action that failed.</param>
        /// <param name="errorMessage">A description of why the action failed.</param>
        /// <param name="processId">Optional process ID if known.</param>
        /// <param name="processName">Optional process name if known.</param>
        /// <returns>A failure result instance.</returns>
        public static ActionExecutionResult CreateFailure(
            ProcessActionType actionType,
            string errorMessage,
            int? processId = null,
            string? processName = null)
        {
            return new ActionExecutionResult
            {
                Success = false,
                ActionType = actionType,
                ProcessId = processId,
                ProcessName = processName,
                WindowHandle = null,
                ErrorMessage = errorMessage,
                HistoryEntry = null
            };
        }

        /// <summary>
        /// Creates a failure result indicating the target process was not found.
        /// </summary>
        /// <param name="actionType">The type of action that failed.</param>
        /// <param name="processName">The name of the process that was not found.</param>
        /// <returns>A failure result instance.</returns>
        public static ActionExecutionResult CreateProcessNotFound(
            ProcessActionType actionType,
            string? processName)
        {
            return CreateFailure(
                actionType,
                processName != null
                    ? $"Process '{processName}' not found."
                    : "Target process not found.",
                processName: processName);
        }

        /// <summary>
        /// Creates a failure result indicating no foreground window was available.
        /// </summary>
        /// <param name="actionType">The type of action that failed.</param>
        /// <returns>A failure result instance.</returns>
        public static ActionExecutionResult CreateNoForegroundWindow(ProcessActionType actionType)
        {
            return CreateFailure(
                actionType,
                "No foreground window available for Quick Action.");
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            if (Success)
            {
                return $"Success: {ActionType} on {ProcessName ?? "unknown"} (PID: {ProcessId?.ToString() ?? "N/A"})";
            }
            else
            {
                return $"Failed: {ActionType} - {ErrorMessage}";
            }
        }
    }
}
