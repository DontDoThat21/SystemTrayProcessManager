using System.Text.Json.Serialization;
using SystemTrayProcessManager.Core.Enums;

namespace SystemTrayProcessManager.Core.Models
{
    /// <summary>
    /// Represents structured error information captured from an exception or error condition.
    /// </summary>
    public sealed class ErrorInfo
    {
        /// <summary>
        /// Gets or sets the fully qualified exception type name (e.g., "System.InvalidOperationException").
        /// </summary>
        public string ExceptionType { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the error message.
        /// </summary>
        public string Message { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the stack trace, if available.
        /// </summary>
        public string? StackTrace { get; set; }

        /// <summary>
        /// Gets or sets the inner exception message, if present.
        /// </summary>
        public string? InnerExceptionMessage { get; set; }

        /// <summary>
        /// Gets or sets the inner exception type, if present.
        /// </summary>
        public string? InnerExceptionType { get; set; }

        /// <summary>
        /// Gets or sets the source service or component that produced the error.
        /// </summary>
        public string Source { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the severity level of the error.
        /// </summary>
        public ErrorSeverity Severity { get; set; } = ErrorSeverity.Medium;

        /// <summary>
        /// Gets or sets the UTC timestamp when the error occurred.
        /// </summary>
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Gets or sets additional context information as key-value pairs.
        /// </summary>
        public Dictionary<string, string> AdditionalContext { get; set; } = new();

        /// <summary>
        /// Creates an <see cref="ErrorInfo"/> from an exception.
        /// </summary>
        /// <param name="exception">The exception to capture.</param>
        /// <param name="source">The source service or component.</param>
        /// <param name="severity">The error severity.</param>
        /// <returns>A new <see cref="ErrorInfo"/> populated from the exception.</returns>
        public static ErrorInfo FromException(Exception exception, string source = "", ErrorSeverity severity = ErrorSeverity.Medium)
        {
            ArgumentNullException.ThrowIfNull(exception);

            return new ErrorInfo
            {
                ExceptionType = exception.GetType().FullName ?? exception.GetType().Name,
                Message = exception.Message,
                StackTrace = exception.StackTrace,
                InnerExceptionMessage = exception.InnerException?.Message,
                InnerExceptionType = exception.InnerException?.GetType().FullName,
                Source = source,
                Severity = severity,
                Timestamp = DateTime.UtcNow
            };
        }

        /// <summary>
        /// Creates an <see cref="ErrorInfo"/> from a message string (no exception).
        /// </summary>
        /// <param name="message">The error message.</param>
        /// <param name="source">The source service or component.</param>
        /// <param name="severity">The error severity.</param>
        /// <returns>A new <see cref="ErrorInfo"/>.</returns>
        public static ErrorInfo FromMessage(string message, string source = "", ErrorSeverity severity = ErrorSeverity.Medium)
        {
            return new ErrorInfo
            {
                ExceptionType = "UserMessage",
                Message = message ?? string.Empty,
                Source = source,
                Severity = severity,
                Timestamp = DateTime.UtcNow
            };
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            return $"[{Severity}] {ExceptionType}: {Message} (Source: {Source})";
        }
    }
}
