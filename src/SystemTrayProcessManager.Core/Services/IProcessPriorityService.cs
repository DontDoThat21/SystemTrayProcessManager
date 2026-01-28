using SystemTrayProcessManager.Core.Enums;
using SystemTrayProcessManager.Core.Models;

namespace SystemTrayProcessManager.Core.Services
{
    /// <summary>
    /// Provides process CPU priority management functionality.
    /// Allows getting, setting, and auto-applying priority levels.
    /// </summary>
    public interface IProcessPriorityService
    {
        /// <summary>
        /// Gets the current CPU priority of a process.
        /// </summary>
        /// <param name="processId">The process identifier.</param>
        /// <returns>The priority level, or null if the process cannot be accessed.</returns>
        Task<ProcessPriority?> GetProcessPriorityAsync(int processId);

        /// <summary>
        /// Sets the CPU priority of a process by ID.
        /// </summary>
        /// <param name="processId">The process identifier.</param>
        /// <param name="priority">The priority level to set.</param>
        /// <returns>True if the priority was set successfully; false otherwise.</returns>
        Task<bool> SetProcessPriorityAsync(int processId, ProcessPriority priority);

        /// <summary>
        /// Sets the CPU priority of all processes with the specified name.
        /// </summary>
        /// <param name="processName">The name of the process (without .exe).</param>
        /// <param name="priority">The priority level to set.</param>
        /// <returns>True if at least one process was modified; false otherwise.</returns>
        Task<bool> SetProcessPriorityByNameAsync(string processName, ProcessPriority priority);

        /// <summary>
        /// Gets all saved priority configurations.
        /// </summary>
        /// <returns>A read-only list of saved configurations.</returns>
        IReadOnlyList<ProcessPriorityConfig> GetSavedPriorityConfigs();

        /// <summary>
        /// Gets the saved priority configuration for a process.
        /// </summary>
        /// <param name="processName">The process name.</param>
        /// <returns>The configuration, or null if not found.</returns>
        ProcessPriorityConfig? GetPriorityConfig(string processName);

        /// <summary>
        /// Saves a priority configuration for a process.
        /// </summary>
        /// <param name="config">The configuration to save.</param>
        Task SavePriorityConfigAsync(ProcessPriorityConfig config);

        /// <summary>
        /// Removes the saved priority configuration for a process.
        /// </summary>
        /// <param name="processName">The process name.</param>
        Task RemovePriorityConfigAsync(string processName);

        /// <summary>
        /// Applies all auto-apply priority configurations to currently running processes.
        /// </summary>
        /// <returns>The number of processes whose priority was changed.</returns>
        Task<int> ApplyAllSavedPrioritiesAsync();

        /// <summary>
        /// Gets or sets a value indicating whether auto-apply is enabled.
        /// When enabled, saved priorities are automatically applied when processes start.
        /// </summary>
        bool AutoApplyEnabled { get; set; }

        /// <summary>
        /// Occurs when a process priority is changed.
        /// Provides the process name and new priority.
        /// </summary>
        event EventHandler<(string ProcessName, ProcessPriority Priority)>? PriorityChanged;
    }
}
