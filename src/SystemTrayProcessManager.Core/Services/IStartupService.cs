using SystemTrayProcessManager.Core.Models;

namespace SystemTrayProcessManager.Core.Services
{
    /// <summary>
    /// Provides management of processes that should be automatically launched on application startup.
    /// </summary>
    public interface IStartupService
    {
        /// <summary>
        /// Gets all configured startup processes.
        /// </summary>
        /// <returns>A read-only list of all startup processes.</returns>
        Task<IReadOnlyList<StartupProcess>> GetStartupProcessesAsync();

        /// <summary>
        /// Gets a startup process by its unique identifier.
        /// </summary>
        /// <param name="processId">The unique identifier of the startup process.</param>
        /// <returns>The startup process if found; otherwise, null.</returns>
        Task<StartupProcess?> GetStartupProcessAsync(Guid processId);

        /// <summary>
        /// Adds a new startup process.
        /// </summary>
        /// <param name="process">The startup process to add.</param>
        /// <returns>True if the process was added successfully; otherwise, false.</returns>
        Task<bool> AddStartupProcessAsync(StartupProcess process);

        /// <summary>
        /// Updates an existing startup process.
        /// </summary>
        /// <param name="process">The startup process to update.</param>
        /// <returns>True if the process was updated; otherwise, false.</returns>
        Task<bool> UpdateStartupProcessAsync(StartupProcess process);

        /// <summary>
        /// Removes a startup process.
        /// </summary>
        /// <param name="processId">The unique identifier of the process to remove.</param>
        /// <returns>True if the process was removed; otherwise, false.</returns>
        Task<bool> RemoveStartupProcessAsync(Guid processId);

        /// <summary>
        /// Enables or disables a startup process.
        /// </summary>
        /// <param name="processId">The unique identifier of the process.</param>
        /// <param name="enabled">True to enable; false to disable.</param>
        /// <returns>True if the process was updated; otherwise, false.</returns>
        Task<bool> SetEnabledAsync(Guid processId, bool enabled);

        /// <summary>
        /// Launches all enabled startup processes in order.
        /// </summary>
        /// <returns>The number of processes successfully launched.</returns>
        Task<int> LaunchStartupProcessesAsync();

        /// <summary>
        /// Launches a specific startup process.
        /// </summary>
        /// <param name="processId">The unique identifier of the process to launch.</param>
        /// <returns>True if the process was launched successfully; otherwise, false.</returns>
        Task<bool> LaunchProcessAsync(Guid processId);

        /// <summary>
        /// Validates that all startup process executable paths exist.
        /// </summary>
        /// <returns>A list of processes with invalid paths.</returns>
        Task<IReadOnlyList<StartupProcess>> ValidatePathsAsync();

        /// <summary>
        /// Occurs when a startup process is launched.
        /// </summary>
        event EventHandler<StartupProcessLaunchedEventArgs>? ProcessLaunched;
    }

    /// <summary>
    /// Event arguments for the ProcessLaunched event.
    /// </summary>
    public sealed class StartupProcessLaunchedEventArgs : EventArgs
    {
        /// <summary>
        /// Gets the startup process that was launched.
        /// </summary>
        public StartupProcess StartupProcess { get; }

        /// <summary>
        /// Gets the process ID of the launched process, or null if launch failed.
        /// </summary>
        public int? LaunchedProcessId { get; }

        /// <summary>
        /// Gets a value indicating whether the launch was successful.
        /// </summary>
        public bool Success { get; }

        /// <summary>
        /// Gets the error message if the launch failed.
        /// </summary>
        public string? ErrorMessage { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="StartupProcessLaunchedEventArgs"/> class.
        /// </summary>
        /// <param name="startupProcess">The startup process.</param>
        /// <param name="launchedProcessId">The launched process ID.</param>
        /// <param name="success">Whether the launch was successful.</param>
        /// <param name="errorMessage">The error message, if any.</param>
        public StartupProcessLaunchedEventArgs(
            StartupProcess startupProcess, 
            int? launchedProcessId, 
            bool success, 
            string? errorMessage = null)
        {
            StartupProcess = startupProcess;
            LaunchedProcessId = launchedProcessId;
            Success = success;
            ErrorMessage = errorMessage;
        }
    }
}
