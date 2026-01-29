using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using SystemTrayProcessManager.Core.Enums;
using SystemTrayProcessManager.Core.Models;
using SystemTrayProcessManager.Core.Services;
using SystemTrayProcessManager.Infrastructure.Services;

namespace SystemTrayProcessManager.Tests.Infrastructure
{
    /// <summary>
    /// Unit tests for <see cref="ErrorHandlingService"/>.
    /// </summary>
    public class ErrorHandlingServiceTests
    {
        private readonly Mock<ILogger<ErrorHandlingService>> _mockLogger;
        private readonly Mock<ICrashReporterService> _mockCrashReporter;
        private readonly ErrorHandlingService _service;
        private readonly ErrorHandlingService _serviceNoCrashReporter;

        public ErrorHandlingServiceTests()
        {
            _mockLogger = new Mock<ILogger<ErrorHandlingService>>();
            _mockCrashReporter = new Mock<ICrashReporterService>();
            _service = new ErrorHandlingService(_mockLogger.Object, _mockCrashReporter.Object);
            _serviceNoCrashReporter = new ErrorHandlingService(_mockLogger.Object);
        }

        // Constructor tests

        [Fact]
        public void Constructor_WithNullLogger_ShouldThrow()
        {
            var act = () => new ErrorHandlingService(null!);
            act.Should().Throw<ArgumentNullException>();
        }

        [Fact]
        public void Constructor_WithNullCrashReporter_ShouldNotThrow()
        {
            var act = () => new ErrorHandlingService(_mockLogger.Object, null);
            act.Should().NotThrow();
        }

        [Fact]
        public void Constructor_ShouldInitializeEmptyRecentErrors()
        {
            _service.RecentErrors.Should().BeEmpty();
        }

        // HandleError (Exception) tests

        [Fact]
        public void HandleError_WithException_ShouldAddToRecentErrors()
        {
            var exception = new InvalidOperationException("Test error");

            _service.HandleError(exception, "TestService", ErrorSeverity.High);

            _service.RecentErrors.Should().HaveCount(1);
            _service.RecentErrors[0].ExceptionType.Should().Be("System.InvalidOperationException");
            _service.RecentErrors[0].Message.Should().Be("Test error");
            _service.RecentErrors[0].Source.Should().Be("TestService");
            _service.RecentErrors[0].Severity.Should().Be(ErrorSeverity.High);
        }

        [Fact]
        public void HandleError_WithNullException_ShouldNotAddToHistory()
        {
            _service.HandleError((Exception)null!, "TestService");

            _service.RecentErrors.Should().BeEmpty();
        }

        [Fact]
        public void HandleError_ShouldRaiseErrorOccurredEvent()
        {
            ErrorInfo? receivedError = null;
            _service.ErrorOccurred += (_, e) => receivedError = e;

            _service.HandleError(new Exception("Event test"), "Source");

            receivedError.Should().NotBeNull();
            receivedError!.Message.Should().Be("Event test");
        }

        [Fact]
        public void HandleError_WithDefaultParameters_ShouldUseMediumSeverity()
        {
            _service.HandleError(new Exception("Default params"));

            _service.RecentErrors[0].Severity.Should().Be(ErrorSeverity.Medium);
            _service.RecentErrors[0].Source.Should().Be(string.Empty);
        }

        [Fact]
        public void HandleError_MultipleErrors_ShouldOrderMostRecentFirst()
        {
            _service.HandleError(new Exception("First"), "A");
            // Wait to bypass throttle
            Thread.Sleep(10);
            _service.HandleError(new ArgumentException("Second"), "B");

            _service.RecentErrors.Should().HaveCount(2);
            _service.RecentErrors[0].Message.Should().Be("Second");
            _service.RecentErrors[1].Message.Should().Be("First");
        }

        [Fact]
        public void HandleError_ExceedingMaxHistory_ShouldTrimOldest()
        {
            // Add MaxRecentErrors + 5 errors
            for (int i = 0; i < ErrorHandlingService.MaxRecentErrors + 5; i++)
            {
                _service.HandleError(new Exception($"Error {i}"), $"Source{i}");
            }

            _service.RecentErrors.Should().HaveCount(ErrorHandlingService.MaxRecentErrors);
        }

        // HandleError (string message) tests

        [Fact]
        public void HandleError_WithMessage_ShouldAddToRecentErrors()
        {
            _service.HandleError("Something went wrong", "TestService", ErrorSeverity.Low);

            _service.RecentErrors.Should().HaveCount(1);
            _service.RecentErrors[0].ExceptionType.Should().Be("UserMessage");
            _service.RecentErrors[0].Message.Should().Be("Something went wrong");
            _service.RecentErrors[0].Source.Should().Be("TestService");
            _service.RecentErrors[0].Severity.Should().Be(ErrorSeverity.Low);
        }

        [Fact]
        public void HandleError_WithNullMessage_ShouldUseEmptyString()
        {
            _service.HandleError((string)null!, "Source");

            _service.RecentErrors[0].Message.Should().Be(string.Empty);
        }

        // Throttling tests

        [Fact]
        public void HandleError_DuplicateWithinThrottleWindow_ShouldBeThrottled()
        {
            var exception = new InvalidOperationException("Duplicate error");

            _service.HandleError(exception, "TestService");
            _service.HandleError(exception, "TestService"); // Should be throttled

            _service.RecentErrors.Should().HaveCount(1);
        }

        [Fact]
        public void HandleError_DifferentSourcesSameError_ShouldNotBeThrottled()
        {
            _service.HandleError(new InvalidOperationException("Error"), "ServiceA");
            _service.HandleError(new InvalidOperationException("Error"), "ServiceB");

            _service.RecentErrors.Should().HaveCount(2);
        }

        [Fact]
        public void HandleError_DifferentErrorsSameSource_ShouldNotBeThrottled()
        {
            _service.HandleError(new InvalidOperationException("Error 1"), "TestService");
            _service.HandleError(new ArgumentException("Error 2"), "TestService");

            _service.RecentErrors.Should().HaveCount(2);
        }

        // Critical error / crash reporter tests

        [Fact]
        public void HandleError_CriticalSeverity_ShouldTriggerCrashReporter()
        {
            var exception = new Exception("Critical failure");

            _service.HandleError(exception, "TestService", ErrorSeverity.Critical);

            // Give async crash reporter time to be invoked
            Thread.Sleep(200);

            _mockCrashReporter.Verify(
                c => c.GenerateReportAsync(exception, "TestService", It.IsAny<string>()),
                Times.Once);
        }

        [Fact]
        public void HandleError_NonCriticalSeverity_ShouldNotTriggerCrashReporter()
        {
            _service.HandleError(new Exception("Warning"), "Test", ErrorSeverity.Medium);

            Thread.Sleep(100);

            _mockCrashReporter.Verify(
                c => c.GenerateReportAsync(It.IsAny<Exception>(), It.IsAny<string>(), It.IsAny<string>()),
                Times.Never);
        }

        [Fact]
        public void HandleError_CriticalWithNoCrashReporter_ShouldNotThrow()
        {
            var act = () => _serviceNoCrashReporter.HandleError(
                new Exception("Critical"), "Test", ErrorSeverity.Critical);

            act.Should().NotThrow();
        }

        // GetUserFriendlyMessage tests

        [Fact]
        public void GetUserFriendlyMessage_UnauthorizedAccess_ShouldMentionAdmin()
        {
            var msg = _service.GetUserFriendlyMessage(new UnauthorizedAccessException());
            msg.Should().Contain("administrator");
        }

        [Fact]
        public void GetUserFriendlyMessage_Win32Exception_ShouldIncludeErrorCode()
        {
            var msg = _service.GetUserFriendlyMessage(new System.ComponentModel.Win32Exception(5));
            msg.Should().Contain("5");
        }

        [Fact]
        public void GetUserFriendlyMessage_ObjectDisposed_ShouldMentionResource()
        {
            var msg = _service.GetUserFriendlyMessage(new ObjectDisposedException("obj"));
            msg.Should().Contain("resource");
        }

        [Fact]
        public void GetUserFriendlyMessage_InvalidOperation_ShouldIncludeMessage()
        {
            var msg = _service.GetUserFriendlyMessage(new InvalidOperationException("Specific issue"));
            msg.Should().Contain("Specific issue");
        }

        [Fact]
        public void GetUserFriendlyMessage_Timeout_ShouldMentionTimeout()
        {
            var msg = _service.GetUserFriendlyMessage(new TimeoutException());
            msg.Should().Contain("timed out");
        }

        [Fact]
        public void GetUserFriendlyMessage_FileNotFound_ShouldMentionFile()
        {
            var msg = _service.GetUserFriendlyMessage(new System.IO.FileNotFoundException());
            msg.Should().Contain("file");
        }

        [Fact]
        public void GetUserFriendlyMessage_IOException_ShouldMentionFileSystem()
        {
            var msg = _service.GetUserFriendlyMessage(new System.IO.IOException());
            msg.Should().Contain("file system");
        }

        [Fact]
        public void GetUserFriendlyMessage_ArgumentException_ShouldMentionInput()
        {
            var msg = _service.GetUserFriendlyMessage(new ArgumentException("Bad value"));
            msg.Should().Contain("input");
        }

        [Fact]
        public void GetUserFriendlyMessage_NotSupported_ShouldMentionSupported()
        {
            var msg = _service.GetUserFriendlyMessage(new NotSupportedException());
            msg.Should().Contain("not supported");
        }

        [Fact]
        public void GetUserFriendlyMessage_AggregateException_ShouldUnwrapInner()
        {
            var inner = new TimeoutException("Inner timeout");
            var aggregate = new AggregateException(inner);

            var msg = _service.GetUserFriendlyMessage(aggregate);
            msg.Should().Contain("timed out");
        }

        [Fact]
        public void GetUserFriendlyMessage_GenericException_ShouldIncludeMessage()
        {
            var msg = _service.GetUserFriendlyMessage(new Exception("Something broke"));
            msg.Should().Contain("Something broke");
        }

        [Fact]
        public void GetUserFriendlyMessage_NullException_ShouldReturnGenericMessage()
        {
            var msg = _service.GetUserFriendlyMessage(null!);
            msg.Should().Contain("unknown error");
        }

        // ClearErrors tests

        [Fact]
        public void ClearErrors_ShouldEmptyRecentErrors()
        {
            _service.HandleError(new Exception("Error 1"), "A");
            _service.HandleError(new Exception("Error 2"), "B");

            _service.ClearErrors();

            _service.RecentErrors.Should().BeEmpty();
        }

        // Event handler exception safety tests

        [Fact]
        public void HandleError_WhenEventHandlerThrows_ShouldNotAffectProcessing()
        {
            _service.ErrorOccurred += (_, _) => throw new Exception("Handler failure");

            var act = () => _service.HandleError(new Exception("Test"), "Source");

            act.Should().NotThrow();
            _service.RecentErrors.Should().HaveCount(1);
        }

        // MaxRecentErrors constant test

        [Fact]
        public void MaxRecentErrors_ShouldBe100()
        {
            ErrorHandlingService.MaxRecentErrors.Should().Be(100);
        }

        // ThrottleInterval constant test

        [Fact]
        public void ThrottleInterval_ShouldBe5Seconds()
        {
            ErrorHandlingService.ThrottleInterval.Should().Be(TimeSpan.FromSeconds(5));
        }
    }
}
