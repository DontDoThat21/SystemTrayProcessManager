namespace SystemTrayProcessManager.Core.Services
{
    /// <summary>
    /// Detects whether the application is running with administrator privileges
    /// and provides functionality to check if elevation is required for specific operations.
    /// </summary>
    public interface IElevationService
    {
        /// <summary>
        /// Gets whether the application is currently running with administrator privileges.
        /// This value is cached on first access.
        /// </summary>
        bool IsRunningAsAdministrator { get; }

        /// <summary>
        /// Checks whether administrator elevation is required to interact with the specified process.
        /// Protected system processes and processes running at a higher integrity level require elevation.
        /// </summary>
        /// <param name="processId">The process ID to check.</param>
        /// <returns>True if elevation is required; false if the process can be accessed without elevation.</returns>
        bool IsElevationRequired(int processId);

        /// <summary>
        /// Attempts to relaunch the application with administrator privileges via UAC prompt.
        /// If successful, the current instance should be shut down by the caller.
        /// </summary>
        /// <returns>True if the elevated process was started; false if the user declined UAC or an error occurred.</returns>
        bool RequestElevation();
    }
}
