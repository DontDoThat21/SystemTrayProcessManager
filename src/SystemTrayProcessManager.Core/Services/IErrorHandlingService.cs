using SystemTrayProcessManager.Core.Enums;
using SystemTrayProcessManager.Core.Models;

namespace SystemTrayProcessManager.Core.Services
{
    /// <summary>
    /// Provides centralized error handling with severity classification,
    /// throttling, user-friendly messages, and error history tracking.
    /// </summary>
    public interface IErrorHandlingService
    {
        /// <summary>
        /// Handles an exception with the specified source and severity.
        /// Logs the error, tracks it in history, raises <see cref="ErrorOccurred"/>,
        /// and triggers crash reporting for <see cref="ErrorSeverity.Critical"/> errors.
        /// </summary>
        /// <param name="exception">The exception to handle.</param>
        /// <param name="source">The source service or component name.</param>
        /// <param name="severity">The severity level of the error.</param>
        void HandleError(Exception exception, string source = "", ErrorSeverity severity = ErrorSeverity.Medium);

        /// <summary>
        /// Handles a message-only error (no exception) with the specified source and severity.
        /// </summary>
        /// <param name="message">The error message.</param>
        /// <param name="source">The source service or component name.</param>
        /// <param name="severity">The severity level of the error.</param>
        void HandleError(string message, string source = "", ErrorSeverity severity = ErrorSeverity.Medium);

        /// <summary>
        /// Maps an exception to a user-friendly error message suitable for display in the UI.
        /// </summary>
        /// <param name="exception">The exception to translate.</param>
        /// <returns>A user-friendly error message string.</returns>
        string GetUserFriendlyMessage(Exception exception);

        /// <summary>
        /// Gets a read-only collection of recent errors (most recent first).
        /// </summary>
        IReadOnlyList<ErrorInfo> RecentErrors { get; }

        /// <summary>
        /// Raised when an error is handled. Subscribers can use this for UI notifications.
        /// </summary>
        event EventHandler<ErrorInfo>? ErrorOccurred;

        /// <summary>
        /// Clears all tracked error history.
        /// </summary>
        void ClearErrors();
    }
}
