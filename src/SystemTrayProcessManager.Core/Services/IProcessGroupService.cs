using SystemTrayProcessManager.Core.Enums;
using SystemTrayProcessManager.Core.Models;

namespace SystemTrayProcessManager.Core.Services
{
    /// <summary>
    /// Provides process group management for organizing processes and executing batch operations.
    /// Supports wildcard pattern matching for flexible process grouping.
    /// </summary>
    public interface IProcessGroupService
    {
        /// <summary>
        /// Gets all process groups.
        /// </summary>
        /// <returns>A read-only list of all process groups.</returns>
        Task<IReadOnlyList<ProcessGroup>> GetGroupsAsync();

        /// <summary>
        /// Gets a process group by its unique identifier.
        /// </summary>
        /// <param name="groupId">The unique identifier of the group.</param>
        /// <returns>The process group if found; otherwise, null.</returns>
        Task<ProcessGroup?> GetGroupAsync(Guid groupId);

        /// <summary>
        /// Gets a process group by name.
        /// </summary>
        /// <param name="name">The name of the group (case-insensitive).</param>
        /// <returns>The process group if found; otherwise, null.</returns>
        Task<ProcessGroup?> GetGroupByNameAsync(string name);

        /// <summary>
        /// Saves or updates a process group.
        /// If a group with the same ID exists, it will be updated.
        /// </summary>
        /// <param name="group">The process group to save.</param>
        /// <returns>True if the group was saved successfully; otherwise, false.</returns>
        Task<bool> SaveGroupAsync(ProcessGroup group);

        /// <summary>
        /// Deletes a process group.
        /// </summary>
        /// <param name="groupId">The unique identifier of the group to delete.</param>
        /// <returns>True if the group was deleted; otherwise, false.</returns>
        Task<bool> DeleteGroupAsync(Guid groupId);

        /// <summary>
        /// Gets all currently running processes that match a group's patterns.
        /// </summary>
        /// <param name="groupId">The unique identifier of the group.</param>
        /// <returns>A read-only list of matching processes.</returns>
        Task<IReadOnlyList<ProcessInfo>> GetMatchingProcessesAsync(Guid groupId);

        /// <summary>
        /// Gets all currently running processes that match a specific group.
        /// </summary>
        /// <param name="group">The process group to match against.</param>
        /// <returns>A read-only list of matching processes.</returns>
        Task<IReadOnlyList<ProcessInfo>> GetMatchingProcessesAsync(ProcessGroup group);

        /// <summary>
        /// Executes a batch action on all processes matching a group.
        /// </summary>
        /// <param name="groupId">The unique identifier of the group.</param>
        /// <param name="action">The action to execute.</param>
        /// <returns>The result of the batch operation.</returns>
        Task<BatchActionResult> ExecuteBatchActionAsync(Guid groupId, ProcessActionType action);

        /// <summary>
        /// Executes a batch action on all processes matching a group.
        /// </summary>
        /// <param name="group">The process group to target.</param>
        /// <param name="action">The action to execute.</param>
        /// <returns>The result of the batch operation.</returns>
        Task<BatchActionResult> ExecuteBatchActionAsync(ProcessGroup group, ProcessActionType action);

        /// <summary>
        /// Adds a pattern to an existing group.
        /// </summary>
        /// <param name="groupId">The unique identifier of the group.</param>
        /// <param name="pattern">The pattern to add.</param>
        /// <returns>True if the pattern was added; otherwise, false.</returns>
        Task<bool> AddPatternAsync(Guid groupId, string pattern);

        /// <summary>
        /// Removes a pattern from an existing group.
        /// </summary>
        /// <param name="groupId">The unique identifier of the group.</param>
        /// <param name="pattern">The pattern to remove.</param>
        /// <returns>True if the pattern was removed; otherwise, false.</returns>
        Task<bool> RemovePatternAsync(Guid groupId, string pattern);

        /// <summary>
        /// Occurs when a batch action is executed on a group.
        /// </summary>
        event EventHandler<BatchActionExecutedEventArgs>? BatchActionExecuted;
    }

    /// <summary>
    /// Represents the result of a batch action execution.
    /// </summary>
    public sealed class BatchActionResult
    {
        /// <summary>
        /// Gets the process group the action was executed on.
        /// </summary>
        public ProcessGroup Group { get; }

        /// <summary>
        /// Gets the action that was executed.
        /// </summary>
        public ProcessActionType Action { get; }

        /// <summary>
        /// Gets the total number of processes targeted.
        /// </summary>
        public int TotalProcesses { get; }

        /// <summary>
        /// Gets the number of processes successfully affected.
        /// </summary>
        public int SuccessCount { get; }

        /// <summary>
        /// Gets the number of processes that failed.
        /// </summary>
        public int FailureCount { get; }

        /// <summary>
        /// Gets a value indicating whether all operations succeeded.
        /// </summary>
        public bool AllSucceeded => FailureCount == 0 && TotalProcesses > 0;

        /// <summary>
        /// Gets a value indicating whether any operations succeeded.
        /// </summary>
        public bool AnySucceeded => SuccessCount > 0;

        /// <summary>
        /// Gets a value indicating whether the result represents a complete failure.
        /// </summary>
        public bool AllFailed => SuccessCount == 0 && TotalProcesses > 0;

        /// <summary>
        /// Initializes a new instance of the <see cref="BatchActionResult"/> class.
        /// </summary>
        /// <param name="group">The process group.</param>
        /// <param name="action">The action executed.</param>
        /// <param name="totalProcesses">The total number of processes.</param>
        /// <param name="successCount">The number of successes.</param>
        /// <param name="failureCount">The number of failures.</param>
        public BatchActionResult(
            ProcessGroup group,
            ProcessActionType action,
            int totalProcesses,
            int successCount,
            int failureCount)
        {
            Group = group;
            Action = action;
            TotalProcesses = totalProcesses;
            SuccessCount = successCount;
            FailureCount = failureCount;
        }

        /// <summary>
        /// Creates a result indicating no processes were found.
        /// </summary>
        /// <param name="group">The process group.</param>
        /// <param name="action">The action attempted.</param>
        /// <returns>A result with zero processes.</returns>
        public static BatchActionResult NoProcesses(ProcessGroup group, ProcessActionType action)
        {
            return new BatchActionResult(group, action, 0, 0, 0);
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            return $"BatchActionResult {{ Group={Group.Name}, Action={Action}, Total={TotalProcesses}, Success={SuccessCount}, Failed={FailureCount} }}";
        }
    }

    /// <summary>
    /// Event arguments for the BatchActionExecuted event.
    /// </summary>
    public sealed class BatchActionExecutedEventArgs : EventArgs
    {
        /// <summary>
        /// Gets the result of the batch action.
        /// </summary>
        public BatchActionResult Result { get; }

        /// <summary>
        /// Gets the timestamp when the action was executed.
        /// </summary>
        public DateTime ExecutedAt { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="BatchActionExecutedEventArgs"/> class.
        /// </summary>
        /// <param name="result">The batch action result.</param>
        /// <param name="executedAt">The execution timestamp.</param>
        public BatchActionExecutedEventArgs(BatchActionResult result, DateTime executedAt)
        {
            Result = result;
            ExecutedAt = executedAt;
        }
    }
}
