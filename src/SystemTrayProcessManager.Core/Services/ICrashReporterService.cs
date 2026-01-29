using SystemTrayProcessManager.Core.Models;

namespace SystemTrayProcessManager.Core.Services
{
    /// <summary>
    /// Generates, persists, and manages crash reports containing error
    /// information and system diagnostics. Reports are stored as JSON
    /// files in the application's local data directory.
    /// </summary>
    public interface ICrashReporterService
    {
        /// <summary>
        /// Generates a crash report from an exception, captures current diagnostics,
        /// and persists the report to disk as a JSON file.
        /// </summary>
        /// <param name="exception">The exception that triggered the crash.</param>
        /// <param name="source">The source component or service.</param>
        /// <param name="applicationState">Optional description of application state at crash time.</param>
        /// <returns>The generated <see cref="CrashReport"/>, or null if generation failed.</returns>
        Task<CrashReport?> GenerateReportAsync(Exception exception, string source = "", string applicationState = "");

        /// <summary>
        /// Gets a list of recent crash reports from the report directory, ordered by date descending.
        /// </summary>
        /// <param name="maxCount">Maximum number of reports to return. Defaults to 20.</param>
        /// <returns>A list of <see cref="CrashReport"/> objects.</returns>
        Task<IReadOnlyList<CrashReport>> GetRecentReportsAsync(int maxCount = 20);

        /// <summary>
        /// Loads a specific crash report by its identifier.
        /// </summary>
        /// <param name="reportId">The unique report identifier.</param>
        /// <returns>The <see cref="CrashReport"/> if found; otherwise null.</returns>
        Task<CrashReport?> GetReportAsync(string reportId);

        /// <summary>
        /// Deletes crash reports older than the specified number of days.
        /// </summary>
        /// <param name="daysToKeep">Number of days of reports to retain. Defaults to 30.</param>
        /// <returns>The number of reports deleted.</returns>
        Task<int> CleanupOldReportsAsync(int daysToKeep = 30);

        /// <summary>
        /// Gets the full path to the crash reports directory.
        /// </summary>
        string CrashReportDirectory { get; }
    }
}
