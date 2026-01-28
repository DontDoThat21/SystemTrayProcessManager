using SystemTrayProcessManager.Core.Models;

namespace SystemTrayProcessManager.Core.Services
{
    /// <summary>
    /// Provides scheduled action management for executing process actions at specific times or intervals.
    /// Supports one-time, recurring interval, and daily schedules.
    /// </summary>
    public interface ISchedulerService : IDisposable
    {
        /// <summary>
        /// Gets all scheduled actions.
        /// </summary>
        /// <returns>A read-only list of all scheduled actions.</returns>
        Task<IReadOnlyList<ScheduledAction>> GetScheduledActionsAsync();

        /// <summary>
        /// Gets a scheduled action by its unique identifier.
        /// </summary>
        /// <param name="actionId">The unique identifier of the action.</param>
        /// <returns>The scheduled action if found; otherwise, null.</returns>
        Task<ScheduledAction?> GetScheduledActionAsync(Guid actionId);

        /// <summary>
        /// Adds a new scheduled action.
        /// </summary>
        /// <param name="action">The action to add.</param>
        /// <returns>True if the action was added successfully; otherwise, false.</returns>
        Task<bool> AddScheduledActionAsync(ScheduledAction action);

        /// <summary>
        /// Updates an existing scheduled action.
        /// </summary>
        /// <param name="action">The action to update.</param>
        /// <returns>True if the action was updated; otherwise, false.</returns>
        Task<bool> UpdateScheduledActionAsync(ScheduledAction action);

        /// <summary>
        /// Removes a scheduled action.
        /// </summary>
        /// <param name="actionId">The unique identifier of the action to remove.</param>
        /// <returns>True if the action was removed; otherwise, false.</returns>
        Task<bool> RemoveScheduledActionAsync(Guid actionId);

        /// <summary>
        /// Enables or disables a scheduled action.
        /// </summary>
        /// <param name="actionId">The unique identifier of the action.</param>
        /// <param name="enabled">True to enable; false to disable.</param>
        /// <returns>True if the action was updated; otherwise, false.</returns>
        Task<bool> SetEnabledAsync(Guid actionId, bool enabled);

        /// <summary>
        /// Gets all enabled actions that are due for execution.
        /// </summary>
        /// <returns>A list of actions that should be executed.</returns>
        Task<IReadOnlyList<ScheduledAction>> GetDueActionsAsync();

        /// <summary>
        /// Starts the scheduler to monitor and execute due actions.
        /// </summary>
        void Start();

        /// <summary>
        /// Stops the scheduler.
        /// </summary>
        void Stop();

        /// <summary>
        /// Gets a value indicating whether the scheduler is currently running.
        /// </summary>
        bool IsRunning { get; }

        /// <summary>
        /// Occurs when a scheduled action is executed.
        /// </summary>
        event EventHandler<ScheduledActionExecutedEventArgs>? ActionExecuted;

        /// <summary>
        /// Occurs when an error occurs during action execution.
        /// </summary>
        event EventHandler<ScheduledActionErrorEventArgs>? ActionError;
    }

    /// <summary>
    /// Event arguments for the ActionExecuted event.
    /// </summary>
    public sealed class ScheduledActionExecutedEventArgs : EventArgs
    {
        /// <summary>
        /// Gets the action that was executed.
        /// </summary>
        public ScheduledAction Action { get; }

        /// <summary>
        /// Gets the timestamp when the action was executed.
        /// </summary>
        public DateTime ExecutedAt { get; }

        /// <summary>
        /// Gets a value indicating whether the execution was successful.
        /// </summary>
        public bool Success { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="ScheduledActionExecutedEventArgs"/> class.
        /// </summary>
        /// <param name="action">The executed action.</param>
        /// <param name="executedAt">The execution timestamp.</param>
        /// <param name="success">Whether the execution was successful.</param>
        public ScheduledActionExecutedEventArgs(ScheduledAction action, DateTime executedAt, bool success)
        {
            Action = action;
            ExecutedAt = executedAt;
            Success = success;
        }
    }

    /// <summary>
    /// Event arguments for the ActionError event.
    /// </summary>
    public sealed class ScheduledActionErrorEventArgs : EventArgs
    {
        /// <summary>
        /// Gets the action that encountered an error.
        /// </summary>
        public ScheduledAction Action { get; }

        /// <summary>
        /// Gets the error message.
        /// </summary>
        public string ErrorMessage { get; }

        /// <summary>
        /// Gets the exception that occurred, if any.
        /// </summary>
        public Exception? Exception { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="ScheduledActionErrorEventArgs"/> class.
        /// </summary>
        /// <param name="action">The action that failed.</param>
        /// <param name="errorMessage">The error message.</param>
        /// <param name="exception">The exception, if any.</param>
        public ScheduledActionErrorEventArgs(ScheduledAction action, string errorMessage, Exception? exception = null)
        {
            Action = action;
            ErrorMessage = errorMessage;
            Exception = exception;
        }
    }
}
