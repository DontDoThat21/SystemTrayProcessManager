using FluentAssertions;
using SystemTrayProcessManager.Core.Enums;
using SystemTrayProcessManager.Core.Models;

namespace SystemTrayProcessManager.Tests.Core
{
    /// <summary>
    /// Unit tests for <see cref="ErrorInfo"/> model.
    /// </summary>
    public class ErrorInfoTests
    {
        [Fact]
        public void DefaultConstructor_ShouldSetDefaults()
        {
            var error = new ErrorInfo();

            error.ExceptionType.Should().Be(string.Empty);
            error.Message.Should().Be(string.Empty);
            error.StackTrace.Should().BeNull();
            error.InnerExceptionMessage.Should().BeNull();
            error.InnerExceptionType.Should().BeNull();
            error.Source.Should().Be(string.Empty);
            error.Severity.Should().Be(ErrorSeverity.Medium);
            error.Timestamp.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
            error.AdditionalContext.Should().NotBeNull().And.BeEmpty();
        }

        [Fact]
        public void Properties_ShouldBeSettable()
        {
            var error = new ErrorInfo
            {
                ExceptionType = "System.InvalidOperationException",
                Message = "Test error",
                StackTrace = "at Test.Method()",
                InnerExceptionMessage = "Inner error",
                InnerExceptionType = "System.ArgumentException",
                Source = "TestService",
                Severity = ErrorSeverity.High,
                Timestamp = new DateTime(2026, 1, 29, 12, 0, 0, DateTimeKind.Utc)
            };

            error.ExceptionType.Should().Be("System.InvalidOperationException");
            error.Message.Should().Be("Test error");
            error.StackTrace.Should().Be("at Test.Method()");
            error.InnerExceptionMessage.Should().Be("Inner error");
            error.InnerExceptionType.Should().Be("System.ArgumentException");
            error.Source.Should().Be("TestService");
            error.Severity.Should().Be(ErrorSeverity.High);
            error.Timestamp.Should().Be(new DateTime(2026, 1, 29, 12, 0, 0, DateTimeKind.Utc));
        }

        [Fact]
        public void AdditionalContext_ShouldSupportKeyValuePairs()
        {
            var error = new ErrorInfo();
            error.AdditionalContext["ProcessId"] = "1234";
            error.AdditionalContext["WindowHandle"] = "0x0001";

            error.AdditionalContext.Should().HaveCount(2);
            error.AdditionalContext["ProcessId"].Should().Be("1234");
            error.AdditionalContext["WindowHandle"].Should().Be("0x0001");
        }

        // FromException tests

        [Fact]
        public void FromException_ShouldPopulateFromException()
        {
            var inner = new ArgumentException("Bad argument");
            var exception = new InvalidOperationException("Test operation failed", inner);

            var error = ErrorInfo.FromException(exception, "TestService", ErrorSeverity.High);

            error.ExceptionType.Should().Be("System.InvalidOperationException");
            error.Message.Should().Be("Test operation failed");
            error.InnerExceptionMessage.Should().Be("Bad argument");
            error.InnerExceptionType.Should().Be("System.ArgumentException");
            error.Source.Should().Be("TestService");
            error.Severity.Should().Be(ErrorSeverity.High);
            error.Timestamp.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        }

        [Fact]
        public void FromException_WithNoInnerException_ShouldHaveNullInnerFields()
        {
            var exception = new InvalidOperationException("No inner");

            var error = ErrorInfo.FromException(exception);

            error.InnerExceptionMessage.Should().BeNull();
            error.InnerExceptionType.Should().BeNull();
        }

        [Fact]
        public void FromException_WithDefaultParameters_ShouldUseMediumSeverity()
        {
            var exception = new Exception("Test");

            var error = ErrorInfo.FromException(exception);

            error.Severity.Should().Be(ErrorSeverity.Medium);
            error.Source.Should().Be(string.Empty);
        }

        [Fact]
        public void FromException_WithNullException_ShouldThrowArgumentNullException()
        {
            var act = () => ErrorInfo.FromException(null!);
            act.Should().Throw<ArgumentNullException>();
        }

        [Fact]
        public void FromException_ShouldCaptureStackTrace()
        {
            Exception? captured = null;
            try
            {
                throw new InvalidOperationException("Stack trace test");
            }
            catch (Exception ex)
            {
                captured = ex;
            }

            var error = ErrorInfo.FromException(captured!);
            error.StackTrace.Should().NotBeNullOrEmpty();
            (error.StackTrace!.Contains("Stack trace test") || error.StackTrace.Contains("ErrorInfoTests"))
                .Should().BeTrue("stack trace should contain relevant method info");
        }

        // FromMessage tests

        [Fact]
        public void FromMessage_ShouldCreateErrorInfoFromString()
        {
            var error = ErrorInfo.FromMessage("Something went wrong", "MyService", ErrorSeverity.Low);

            error.ExceptionType.Should().Be("UserMessage");
            error.Message.Should().Be("Something went wrong");
            error.Source.Should().Be("MyService");
            error.Severity.Should().Be(ErrorSeverity.Low);
            error.StackTrace.Should().BeNull();
        }

        [Fact]
        public void FromMessage_WithNullMessage_ShouldUseEmptyString()
        {
            var error = ErrorInfo.FromMessage(null!, "Source");

            error.Message.Should().Be(string.Empty);
        }

        [Fact]
        public void FromMessage_WithDefaultParameters_ShouldUseMediumSeverity()
        {
            var error = ErrorInfo.FromMessage("Test");

            error.Severity.Should().Be(ErrorSeverity.Medium);
            error.Source.Should().Be(string.Empty);
        }

        // ToString tests

        [Fact]
        public void ToString_ShouldIncludeSeverityTypeAndMessage()
        {
            var error = new ErrorInfo
            {
                ExceptionType = "System.Exception",
                Message = "Test error",
                Source = "TestService",
                Severity = ErrorSeverity.High
            };

            var result = error.ToString();

            result.Should().Contain("High");
            result.Should().Contain("System.Exception");
            result.Should().Contain("Test error");
            result.Should().Contain("TestService");
        }

        [Fact]
        public void FromException_WithThrownException_ShouldHaveCorrectType()
        {
            try
            {
                throw new TimeoutException("Timed out");
            }
            catch (Exception ex)
            {
                var error = ErrorInfo.FromException(ex, "Network", ErrorSeverity.Critical);
                error.ExceptionType.Should().Be("System.TimeoutException");
                error.Message.Should().Be("Timed out");
                error.Severity.Should().Be(ErrorSeverity.Critical);
            }
        }

        [Fact]
        public void AdditionalContext_DefaultInstance_ShouldBeEmptyDictionary()
        {
            var error = new ErrorInfo();
            error.AdditionalContext.Should().BeOfType<Dictionary<string, string>>();
            error.AdditionalContext.Should().BeEmpty();
        }

        [Fact]
        public void FromException_WithNestedInnerExceptions_ShouldCaptureFirstInner()
        {
            var innerMost = new ArgumentException("Inner most");
            var middle = new InvalidOperationException("Middle", innerMost);
            var outer = new Exception("Outer", middle);

            var error = ErrorInfo.FromException(outer);

            error.InnerExceptionMessage.Should().Be("Middle");
            error.InnerExceptionType.Should().Be("System.InvalidOperationException");
        }
    }
}
