using SystemTrayProcessManager.Core.Models;

namespace SystemTrayProcessManager.Core.Services
{
    /// <summary>
    /// Provides process discovery, monitoring, and enumeration capabilities.
    /// Allows tracking of running processes with visible windows and detecting process lifecycle changes.
    /// </summary>
    public interface IProcessService : IDisposable
    {
        /// <summary>
        /// Gets all currently running processes that have visible windows.
        /// Excludes system processes and processes without window handles.
        /// </summary>
        /// <param name="cancellationToken">Token to cancel the operation.</param>
        /// <returns>A read-only list of process information for all visible windowed processes.</returns>
        Task<IReadOnlyList<ProcessInfo>> GetRunningProcessesAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets information about a specific process by its identifier.
        /// </summary>
        /// <param name="processId">The process identifier to look up.</param>
        /// <param name="cancellationToken">Token to cancel the operation.</param>
        /// <returns>Process information if found and has a visible window; otherwise, null.</returns>
        Task<ProcessInfo?> GetProcessByIdAsync(int processId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Searches for processes matching the specified search term.
        /// Searches process names and window titles (case-insensitive).
        /// </summary>
        /// <param name="searchTerm">The term to search for in process names and window titles.</param>
        /// <param name="cancellationToken">Token to cancel the operation.</param>
        /// <returns>A read-only list of matching process information.</returns>
        Task<IReadOnlyList<ProcessInfo>> SearchProcessesAsync(string searchTerm, CancellationToken cancellationToken = default);

        /// <summary>
        /// Starts background monitoring of process changes.
        /// The service will poll for changes at a 5-second interval and raise events when processes start or stop.
        /// </summary>
        void StartMonitoring();

        /// <summary>
        /// Stops background monitoring of process changes.
        /// </summary>
        void StopMonitoring();

        /// <summary>
        /// Gets a value indicating whether the service is currently monitoring for process changes.
        /// </summary>
        bool IsMonitoring { get; }

        /// <summary>
        /// Occurs when a new process with a visible window is detected.
        /// </summary>
        event EventHandler<ProcessInfo>? ProcessStarted;

        /// <summary>
        /// Occurs when a previously tracked process stops running.
        /// </summary>
        event EventHandler<ProcessStoppedEventArgs>? ProcessStopped;

        /// <summary>
        /// Occurs when the process list changes (process started or stopped).
        /// Provides the complete updated list of running processes.
        /// </summary>
        event EventHandler<IReadOnlyList<ProcessInfo>>? ProcessListChanged;
    }
}
