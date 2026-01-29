using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using SystemTrayProcessManager.Core.Enums;
using SystemTrayProcessManager.Core.Models;
using SystemTrayProcessManager.Core.Services;

namespace SystemTrayProcessManager.Infrastructure.Services
{
    /// <summary>
    /// Centralized error handling service with severity classification,
    /// duplicate throttling, user-friendly message mapping, and error history tracking.
    /// </summary>
    public class ErrorHandlingService : IErrorHandlingService
    {
        private readonly ILogger<ErrorHandlingService> _logger;
        private readonly ICrashReporterService? _crashReporter;
        private readonly List<ErrorInfo> _recentErrors = new();
        private readonly ConcurrentDictionary<string, DateTime> _throttleTracker = new();
        private readonly object _lock = new();

        /// <summary>
        /// Maximum number of recent errors to retain in history.
        /// </summary>
        internal const int MaxRecentErrors = 100;

        /// <summary>
        /// Minimum interval between duplicate error reports from the same source.
        /// </summary>
        internal static readonly TimeSpan ThrottleInterval = TimeSpan.FromSeconds(5);

        /// <inheritdoc/>
        public IReadOnlyList<ErrorInfo> RecentErrors
        {
            get
            {
                lock (_lock)
                {
                    return _recentErrors.AsReadOnly();
                }
            }
        }

        /// <inheritdoc/>
        public event EventHandler<ErrorInfo>? ErrorOccurred;

        /// <summary>
        /// Initializes a new instance of <see cref="ErrorHandlingService"/>.
        /// </summary>
        /// <param name="logger">Logger instance.</param>
        /// <param name="crashReporter">Optional crash reporter for critical errors.</param>
        public ErrorHandlingService(
            ILogger<ErrorHandlingService> logger,
            ICrashReporterService? crashReporter = null)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _crashReporter = crashReporter;
        }

        /// <inheritdoc/>
        public void HandleError(Exception exception, string source = "", ErrorSeverity severity = ErrorSeverity.Medium)
        {
            if (exception == null)
            {
                _logger.LogWarning("HandleError called with null exception from source '{Source}'", source);
                return;
            }

            var errorInfo = ErrorInfo.FromException(exception, source, severity);
            ProcessError(errorInfo, exception);
        }

        /// <inheritdoc/>
        public void HandleError(string message, string source = "", ErrorSeverity severity = ErrorSeverity.Medium)
        {
            var errorInfo = ErrorInfo.FromMessage(message ?? string.Empty, source, severity);
            ProcessError(errorInfo, null);
        }

        /// <inheritdoc/>
        public string GetUserFriendlyMessage(Exception exception)
        {
            if (exception == null)
                return "An unknown error occurred.";

            return exception switch
            {
                UnauthorizedAccessException => "Access denied. You may need to run the application as administrator.",
                System.ComponentModel.Win32Exception w32 => $"A Windows system error occurred (code {w32.NativeErrorCode}). Some operations require elevated privileges.",
                ObjectDisposedException => "The target resource is no longer available. It may have been closed.",
                InvalidOperationException => $"The operation could not be completed: {exception.Message}",
                TimeoutException => "The operation timed out. The target process may not be responding.",
                System.IO.FileNotFoundException => "A required file was not found. The application configuration may be corrupted.",
                System.IO.IOException => "A file system error occurred. Please check disk space and permissions.",
                System.Runtime.InteropServices.COMException => "A system component error occurred. Please try the operation again.",
                ArgumentException => $"Invalid input: {exception.Message}",
                NotSupportedException => "This operation is not supported on the current system configuration.",
                AggregateException ae => GetUserFriendlyMessage(ae.InnerException ?? ae),
                _ => $"An unexpected error occurred: {exception.Message}"
            };
        }

        /// <inheritdoc/>
        public void ClearErrors()
        {
            lock (_lock)
            {
                _recentErrors.Clear();
            }
            _throttleTracker.Clear();
            _logger.LogDebug("Error history cleared");
        }

        /// <summary>
        /// Processes an error: checks throttling, adds to history, logs, raises event, and triggers crash reporter if critical.
        /// </summary>
        private void ProcessError(ErrorInfo errorInfo, Exception? exception)
        {
            // Throttle duplicate errors from same source
            var throttleKey = $"{errorInfo.Source}:{errorInfo.ExceptionType}:{errorInfo.Message}";
            if (_throttleTracker.TryGetValue(throttleKey, out var lastTime) &&
                DateTime.UtcNow - lastTime < ThrottleInterval)
            {
                _logger.LogDebug("Throttled duplicate error from {Source}: {Message}", errorInfo.Source, errorInfo.Message);
                return;
            }
            _throttleTracker[throttleKey] = DateTime.UtcNow;

            // Add to history
            lock (_lock)
            {
                _recentErrors.Insert(0, errorInfo);
                if (_recentErrors.Count > MaxRecentErrors)
                {
                    _recentErrors.RemoveAt(_recentErrors.Count - 1);
                }
            }

            // Log at appropriate level
            LogError(errorInfo, exception);

            // Raise event
            try
            {
                ErrorOccurred?.Invoke(this, errorInfo);
            }
            catch (Exception eventEx)
            {
                _logger.LogWarning(eventEx, "Exception in ErrorOccurred event handler");
            }

            // Trigger crash reporter for critical errors
            if (errorInfo.Severity == ErrorSeverity.Critical && _crashReporter != null && exception != null)
            {
                _ = Task.Run(async () =>
                {
                    try
                    {
                        await _crashReporter.GenerateReportAsync(exception, errorInfo.Source);
                    }
                    catch (Exception reportEx)
                    {
                        _logger.LogError(reportEx, "Failed to generate crash report for critical error");
                    }
                });
            }
        }

        /// <summary>
        /// Logs the error at the appropriate log level based on severity.
        /// </summary>
        private void LogError(ErrorInfo errorInfo, Exception? exception)
        {
            var message = "Error in {Source}: [{Severity}] {ErrorType} - {ErrorMessage}";
            var args = new object[] { errorInfo.Source, errorInfo.Severity, errorInfo.ExceptionType, errorInfo.Message };

            switch (errorInfo.Severity)
            {
                case ErrorSeverity.Low:
                    _logger.LogDebug(exception, message, args);
                    break;
                case ErrorSeverity.Medium:
                    _logger.LogWarning(exception, message, args);
                    break;
                case ErrorSeverity.High:
                    _logger.LogError(exception, message, args);
                    break;
                case ErrorSeverity.Critical:
                    _logger.LogCritical(exception, message, args);
                    break;
                default:
                    _logger.LogInformation(exception, message, args);
                    break;
            }
        }
    }
}
