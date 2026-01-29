using FluentAssertions;
using System.Text.Json;
using SystemTrayProcessManager.Core.Enums;
using SystemTrayProcessManager.Core.Models;

namespace SystemTrayProcessManager.Tests.Core
{
    /// <summary>
    /// Unit tests for <see cref="CrashReport"/> model.
    /// </summary>
    public class CrashReportTests
    {
        [Fact]
        public void DefaultConstructor_ShouldSetDefaults()
        {
            var report = new CrashReport();

            report.Id.Should().NotBeNullOrEmpty();
            report.Id.Should().HaveLength(32); // GUID with "N" format
            report.Timestamp.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
            report.ErrorInfo.Should().NotBeNull();
            report.DiagnosticInfo.Should().NotBeNull();
            report.ApplicationState.Should().Be(string.Empty);
            report.Notes.Should().Be(string.Empty);
        }

        [Fact]
        public void Properties_ShouldBeSettable()
        {
            var report = new CrashReport
            {
                Id = "abc12345",
                ApplicationState = "Running, 5 processes monitored",
                Notes = "User was configuring hotkeys"
            };

            report.Id.Should().Be("abc12345");
            report.ApplicationState.Should().Be("Running, 5 processes monitored");
            report.Notes.Should().Be("User was configuring hotkeys");
        }

        [Fact]
        public void EachInstance_ShouldHaveUniqueId()
        {
            var report1 = new CrashReport();
            var report2 = new CrashReport();

            report1.Id.Should().NotBe(report2.Id);
        }

        // CreateFromException tests

        [Fact]
        public void CreateFromException_ShouldPopulateReport()
        {
            var exception = new InvalidOperationException("Test crash");

            var report = CrashReport.CreateFromException(exception, "TestService", "Processing");

            report.Should().NotBeNull();
            report.ErrorInfo.ExceptionType.Should().Be("System.InvalidOperationException");
            report.ErrorInfo.Message.Should().Be("Test crash");
            report.ErrorInfo.Source.Should().Be("TestService");
            report.ErrorInfo.Severity.Should().Be(ErrorSeverity.Critical);
            report.ApplicationState.Should().Be("Processing");
            report.DiagnosticInfo.Should().NotBeNull();
            report.DiagnosticInfo.OSVersion.Should().NotBeNullOrEmpty();
        }

        [Fact]
        public void CreateFromException_ShouldCaptureDiagnostics()
        {
            var exception = new Exception("Diag test");

            var report = CrashReport.CreateFromException(exception);

            report.DiagnosticInfo.ProcessorCount.Should().BeGreaterThan(0);
            report.DiagnosticInfo.ProcessMemoryMB.Should().BeGreaterThan(0);
            report.DiagnosticInfo.DotNetVersion.Should().NotBeNullOrEmpty();
        }

        [Fact]
        public void CreateFromException_WithNullException_ShouldThrow()
        {
            var act = () => CrashReport.CreateFromException(null!);
            act.Should().Throw<ArgumentNullException>();
        }

        [Fact]
        public void CreateFromException_WithDefaultParams_ShouldUseEmptyStrings()
        {
            var report = CrashReport.CreateFromException(new Exception("Test"));

            report.ErrorInfo.Source.Should().Be(string.Empty);
            report.ApplicationState.Should().Be(string.Empty);
        }

        [Fact]
        public void CreateFromException_ShouldSetTimestampToNow()
        {
            var before = DateTime.UtcNow;
            var report = CrashReport.CreateFromException(new Exception("Test"));
            var after = DateTime.UtcNow;

            report.Timestamp.Should().BeOnOrAfter(before);
            report.Timestamp.Should().BeOnOrBefore(after);
        }

        // GetFileName tests

        [Fact]
        public void GetFileName_ShouldReturnValidFileName()
        {
            var report = new CrashReport
            {
                Id = "a1b2c3d4e5f6g7h8",
                Timestamp = new DateTime(2026, 1, 29, 14, 30, 52, DateTimeKind.Utc)
            };

            var fileName = report.GetFileName();

            fileName.Should().Be("crash_20260129_143052_a1b2c3d4.json");
        }

        [Fact]
        public void GetFileName_WithShortId_ShouldUseFullId()
        {
            var report = new CrashReport
            {
                Id = "abc",
                Timestamp = new DateTime(2026, 6, 15, 8, 0, 0, DateTimeKind.Utc)
            };

            var fileName = report.GetFileName();

            fileName.Should().Be("crash_20260615_080000_abc.json");
        }

        [Fact]
        public void GetFileName_ShouldEndWithJson()
        {
            var report = new CrashReport();
            report.GetFileName().Should().EndWith(".json");
        }

        [Fact]
        public void GetFileName_ShouldStartWithCrash()
        {
            var report = new CrashReport();
            report.GetFileName().Should().StartWith("crash_");
        }

        // ToString tests

        [Fact]
        public void ToString_ShouldContainIdAndExceptionInfo()
        {
            var report = CrashReport.CreateFromException(
                new InvalidOperationException("Test crash"), "MyService");

            var result = report.ToString();

            result.Should().Contain("CrashReport");
            result.Should().Contain("InvalidOperationException");
            result.Should().Contain("Test crash");
        }

        // JSON serialization tests

        [Fact]
        public void ShouldBeSerializableToJson()
        {
            var report = CrashReport.CreateFromException(
                new Exception("JSON test"), "SerializationTest");

            var json = JsonSerializer.Serialize(report);

            json.Should().NotBeNullOrEmpty();
            json.Should().Contain("JSON test");
            json.Should().Contain("SerializationTest");
        }

        [Fact]
        public void ShouldBeDeserializableFromJson()
        {
            var original = CrashReport.CreateFromException(
                new Exception("Round trip"), "DeserTest", "Active state");

            var options = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                PropertyNameCaseInsensitive = true
            };

            var json = JsonSerializer.Serialize(original, options);
            var deserialized = JsonSerializer.Deserialize<CrashReport>(json, options);

            deserialized.Should().NotBeNull();
            deserialized!.Id.Should().Be(original.Id);
            deserialized.ErrorInfo.Message.Should().Be("Round trip");
            deserialized.ApplicationState.Should().Be("Active state");
        }
    }
}
