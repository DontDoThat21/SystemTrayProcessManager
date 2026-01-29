namespace SystemTrayProcessManager.Core.Models
{
    /// <summary>
    /// Represents a complete crash report combining error information and system diagnostics.
    /// Serializable to JSON for persistence and analysis.
    /// </summary>
    public sealed class CrashReport
    {
        /// <summary>
        /// Gets or sets the unique identifier for this crash report.
        /// </summary>
        public string Id { get; set; } = Guid.NewGuid().ToString("N");

        /// <summary>
        /// Gets or sets the UTC timestamp when this report was generated.
        /// </summary>
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Gets or sets the structured error information.
        /// </summary>
        public ErrorInfo ErrorInfo { get; set; } = new();

        /// <summary>
        /// Gets or sets the system diagnostic snapshot.
        /// </summary>
        public DiagnosticInfo DiagnosticInfo { get; set; } = new();

        /// <summary>
        /// Gets or sets a summary of the application state at the time of the crash.
        /// </summary>
        public string ApplicationState { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets additional notes or context for the crash report.
        /// </summary>
        public string Notes { get; set; } = string.Empty;

        /// <summary>
        /// Creates a <see cref="CrashReport"/> from an exception, capturing current diagnostics.
        /// </summary>
        /// <param name="exception">The exception that triggered the crash.</param>
        /// <param name="source">The source component.</param>
        /// <param name="applicationState">Optional application state description.</param>
        /// <returns>A new <see cref="CrashReport"/>.</returns>
        public static CrashReport CreateFromException(
            Exception exception,
            string source = "",
            string applicationState = "")
        {
            ArgumentNullException.ThrowIfNull(exception);

            return new CrashReport
            {
                Timestamp = DateTime.UtcNow,
                ErrorInfo = ErrorInfo.FromException(exception, source, Enums.ErrorSeverity.Critical),
                DiagnosticInfo = DiagnosticInfo.Capture(),
                ApplicationState = applicationState
            };
        }

        /// <summary>
        /// Gets a file-system-safe filename for this report.
        /// </summary>
        /// <returns>A filename string like "crash_20260129_143052_abc12345.json".</returns>
        public string GetFileName()
        {
            var ts = Timestamp.ToString("yyyyMMdd_HHmmss");
            var shortId = Id.Length >= 8 ? Id[..8] : Id;
            return $"crash_{ts}_{shortId}.json";
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            return $"CrashReport {Id[..8]}: {ErrorInfo.ExceptionType} - {ErrorInfo.Message}";
        }
    }
}
