using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using System.IO;
using System.Text.Json;
using SystemTrayProcessManager.Core.Models;
using SystemTrayProcessManager.Infrastructure.Services;

namespace SystemTrayProcessManager.Tests.Infrastructure
{
    /// <summary>
    /// Unit tests for <see cref="CrashReporterService"/>.
    /// </summary>
    public class CrashReporterServiceTests : IDisposable
    {
        private readonly Mock<ILogger<CrashReporterService>> _mockLogger;
        private readonly string _testDirectory;
        private readonly CrashReporterService _service;

        public CrashReporterServiceTests()
        {
            _mockLogger = new Mock<ILogger<CrashReporterService>>();
            _testDirectory = Path.Combine(Path.GetTempPath(), $"CrashReporterTests_{Guid.NewGuid():N}");
            Directory.CreateDirectory(_testDirectory);
            _service = new CrashReporterService(_mockLogger.Object, _testDirectory);
        }

        public void Dispose()
        {
            try
            {
                if (Directory.Exists(_testDirectory))
                    Directory.Delete(_testDirectory, true);
            }
            catch { /* Cleanup best effort */ }
        }

        // Constructor tests

        [Fact]
        public void Constructor_WithNullLogger_ShouldThrow()
        {
            var act = () => new CrashReporterService(null!);
            act.Should().Throw<ArgumentNullException>();
        }

        [Fact]
        public void Constructor_ShouldSetCrashReportDirectory()
        {
            _service.CrashReportDirectory.Should().Be(_testDirectory);
        }

        [Fact]
        public void Constructor_DefaultDirectory_ShouldPointToLocalAppData()
        {
            var defaultService = new CrashReporterService(_mockLogger.Object);
            defaultService.CrashReportDirectory.Should().Contain("SystemTrayProcessManager");
            defaultService.CrashReportDirectory.Should().Contain("crash-reports");
        }

        // GenerateReportAsync tests

        [Fact]
        public async Task GenerateReportAsync_ShouldReturnReport()
        {
            var exception = new InvalidOperationException("Test crash");

            var report = await _service.GenerateReportAsync(exception, "TestService", "Running");

            report.Should().NotBeNull();
            report!.ErrorInfo.ExceptionType.Should().Be("System.InvalidOperationException");
            report.ErrorInfo.Message.Should().Be("Test crash");
            report.ErrorInfo.Source.Should().Be("TestService");
            report.ApplicationState.Should().Be("Running");
        }

        [Fact]
        public async Task GenerateReportAsync_ShouldWriteFileToDirectory()
        {
            var exception = new Exception("File test");

            var report = await _service.GenerateReportAsync(exception);

            var files = Directory.GetFiles(_testDirectory, "crash_*.json");
            files.Should().HaveCount(1);
        }

        [Fact]
        public async Task GenerateReportAsync_ShouldWriteValidJson()
        {
            var exception = new Exception("JSON test");

            var report = await _service.GenerateReportAsync(exception, "Source");

            var files = Directory.GetFiles(_testDirectory, "crash_*.json");
            var json = await File.ReadAllTextAsync(files[0]);

            var options = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                PropertyNameCaseInsensitive = true
            };
            var deserialized = JsonSerializer.Deserialize<CrashReport>(json, options);

            deserialized.Should().NotBeNull();
            deserialized!.ErrorInfo.Message.Should().Be("JSON test");
        }

        [Fact]
        public async Task GenerateReportAsync_WithNullException_ShouldReturnNull()
        {
            var result = await _service.GenerateReportAsync(null!);
            result.Should().BeNull();
        }

        [Fact]
        public async Task GenerateReportAsync_ShouldCreateDirectoryIfMissing()
        {
            var subDir = Path.Combine(_testDirectory, "sub", "nested");
            var service = new CrashReporterService(_mockLogger.Object, subDir);

            await service.GenerateReportAsync(new Exception("Dir test"));

            Directory.Exists(subDir).Should().BeTrue();
        }

        [Fact]
        public async Task GenerateReportAsync_MultipleReports_ShouldCreateMultipleFiles()
        {
            await _service.GenerateReportAsync(new Exception("Error 1"));
            await _service.GenerateReportAsync(new ArgumentException("Error 2"));

            var files = Directory.GetFiles(_testDirectory, "crash_*.json");
            files.Should().HaveCount(2);
        }

        // GetRecentReportsAsync tests

        [Fact]
        public async Task GetRecentReportsAsync_WithNoReports_ShouldReturnEmpty()
        {
            var reports = await _service.GetRecentReportsAsync();
            reports.Should().BeEmpty();
        }

        [Fact]
        public async Task GetRecentReportsAsync_ShouldReturnGeneratedReports()
        {
            await _service.GenerateReportAsync(new Exception("Report 1"), "S1");
            await _service.GenerateReportAsync(new Exception("Report 2"), "S2");

            var reports = await _service.GetRecentReportsAsync();

            reports.Should().HaveCount(2);
        }

        [Fact]
        public async Task GetRecentReportsAsync_ShouldRespectMaxCount()
        {
            for (int i = 0; i < 5; i++)
            {
                await _service.GenerateReportAsync(new Exception($"Report {i}"), $"S{i}");
            }

            var reports = await _service.GetRecentReportsAsync(3);

            reports.Should().HaveCount(3);
        }

        [Fact]
        public async Task GetRecentReportsAsync_WithNonexistentDirectory_ShouldReturnEmpty()
        {
            var service = new CrashReporterService(_mockLogger.Object,
                Path.Combine(_testDirectory, "nonexistent"));

            var reports = await service.GetRecentReportsAsync();
            reports.Should().BeEmpty();
        }

        [Fact]
        public async Task GetRecentReportsAsync_WithCorruptedFile_ShouldSkipIt()
        {
            await _service.GenerateReportAsync(new Exception("Good report"));

            // Write corrupted file
            await File.WriteAllTextAsync(
                Path.Combine(_testDirectory, "crash_20260129_120000_bad12345.json"),
                "not valid json");

            var reports = await _service.GetRecentReportsAsync();
            reports.Should().HaveCount(1);
        }

        // GetReportAsync tests

        [Fact]
        public async Task GetReportAsync_ShouldReturnMatchingReport()
        {
            var generated = await _service.GenerateReportAsync(new Exception("Find me"), "Src");

            var found = await _service.GetReportAsync(generated!.Id);

            found.Should().NotBeNull();
            found!.Id.Should().Be(generated.Id);
            found.ErrorInfo.Message.Should().Be("Find me");
        }

        [Fact]
        public async Task GetReportAsync_WithUnknownId_ShouldReturnNull()
        {
            await _service.GenerateReportAsync(new Exception("Existing"));

            var result = await _service.GetReportAsync("nonexistent_id");
            result.Should().BeNull();
        }

        [Fact]
        public async Task GetReportAsync_WithNullId_ShouldReturnNull()
        {
            var result = await _service.GetReportAsync(null!);
            result.Should().BeNull();
        }

        [Fact]
        public async Task GetReportAsync_WithEmptyId_ShouldReturnNull()
        {
            var result = await _service.GetReportAsync(string.Empty);
            result.Should().BeNull();
        }

        [Fact]
        public async Task GetReportAsync_WithNonexistentDirectory_ShouldReturnNull()
        {
            var service = new CrashReporterService(_mockLogger.Object,
                Path.Combine(_testDirectory, "nonexistent"));

            var result = await service.GetReportAsync("some-id");
            result.Should().BeNull();
        }

        // CleanupOldReportsAsync tests

        [Fact]
        public async Task CleanupOldReportsAsync_ShouldDeleteOldReports()
        {
            // Create a file with old creation time
            var oldFile = Path.Combine(_testDirectory, "crash_20250101_120000_old12345.json");
            await File.WriteAllTextAsync(oldFile, "{}");
            File.SetCreationTimeUtc(oldFile, DateTime.UtcNow.AddDays(-60));

            // Create a recent file
            await _service.GenerateReportAsync(new Exception("Recent"));

            var deleted = await _service.CleanupOldReportsAsync(30);

            deleted.Should().Be(1);
            var remaining = Directory.GetFiles(_testDirectory, "crash_*.json");
            remaining.Should().HaveCount(1);
        }

        [Fact]
        public async Task CleanupOldReportsAsync_WithNoOldFiles_ShouldReturnZero()
        {
            await _service.GenerateReportAsync(new Exception("Recent"));

            var deleted = await _service.CleanupOldReportsAsync(30);

            deleted.Should().Be(0);
        }

        [Fact]
        public async Task CleanupOldReportsAsync_WithNonexistentDirectory_ShouldReturnZero()
        {
            var service = new CrashReporterService(_mockLogger.Object,
                Path.Combine(_testDirectory, "nonexistent"));

            var deleted = await service.CleanupOldReportsAsync();
            deleted.Should().Be(0);
        }

        [Fact]
        public async Task CleanupOldReportsAsync_WithZeroDays_ShouldDeleteAll()
        {
            await _service.GenerateReportAsync(new Exception("Delete me"));

            // Need to set the creation time to be slightly in the past
            var files = Directory.GetFiles(_testDirectory, "crash_*.json");
            foreach (var f in files)
            {
                File.SetCreationTimeUtc(f, DateTime.UtcNow.AddMinutes(-1));
            }

            var deleted = await _service.CleanupOldReportsAsync(0);
            deleted.Should().Be(1);
        }
    }
}
