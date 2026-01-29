using Microsoft.Extensions.Logging;
using System.IO;
using System.Text.Json;
using SystemTrayProcessManager.Core.Models;
using SystemTrayProcessManager.Core.Services;

namespace SystemTrayProcessManager.Infrastructure.Services
{
    /// <summary>
    /// Generates, persists, and manages crash reports as JSON files in the
    /// application's local data directory under a "crash-reports" subfolder.
    /// </summary>
    public class CrashReporterService : ICrashReporterService
    {
        private readonly ILogger<CrashReporterService> _logger;
        private readonly string _crashReportDirectory;
        private readonly JsonSerializerOptions _jsonOptions;

        /// <inheritdoc/>
        public string CrashReportDirectory => _crashReportDirectory;

        /// <summary>
        /// Initializes a new instance of <see cref="CrashReporterService"/>.
        /// </summary>
        /// <param name="logger">Logger instance.</param>
        public CrashReporterService(ILogger<CrashReporterService> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            _crashReportDirectory = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "SystemTrayProcessManager",
                "crash-reports");

            _jsonOptions = new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                PropertyNameCaseInsensitive = true
            };
        }

        /// <summary>
        /// Initializes a new instance of <see cref="CrashReporterService"/> with a custom directory.
        /// Used for testing.
        /// </summary>
        internal CrashReporterService(ILogger<CrashReporterService> logger, string crashReportDirectory)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _crashReportDirectory = crashReportDirectory ?? throw new ArgumentNullException(nameof(crashReportDirectory));

            _jsonOptions = new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                PropertyNameCaseInsensitive = true
            };
        }

        /// <inheritdoc/>
        public async Task<CrashReport?> GenerateReportAsync(Exception exception, string source = "", string applicationState = "")
        {
            if (exception == null)
            {
                _logger.LogWarning("GenerateReportAsync called with null exception");
                return null;
            }

            try
            {
                _logger.LogDebug("Generating crash report for {ExceptionType} from {Source}",
                    exception.GetType().Name, source);

                var report = CrashReport.CreateFromException(exception, source, applicationState);

                // Ensure directory exists
                Directory.CreateDirectory(_crashReportDirectory);

                // Write report to file
                var filePath = Path.Combine(_crashReportDirectory, report.GetFileName());
                var json = JsonSerializer.Serialize(report, _jsonOptions);
                await File.WriteAllTextAsync(filePath, json);

                _logger.LogInformation("Crash report saved to {FilePath}", filePath);
                return report;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to generate crash report");
                return null;
            }
        }

        /// <inheritdoc/>
        public async Task<IReadOnlyList<CrashReport>> GetRecentReportsAsync(int maxCount = 20)
        {
            var reports = new List<CrashReport>();

            try
            {
                if (!Directory.Exists(_crashReportDirectory))
                {
                    _logger.LogDebug("Crash report directory does not exist");
                    return reports.AsReadOnly();
                }

                var files = Directory.GetFiles(_crashReportDirectory, "crash_*.json")
                    .OrderByDescending(f => File.GetCreationTimeUtc(f))
                    .Take(maxCount);

                foreach (var file in files)
                {
                    try
                    {
                        var json = await File.ReadAllTextAsync(file);
                        var report = JsonSerializer.Deserialize<CrashReport>(json, _jsonOptions);
                        if (report != null)
                        {
                            reports.Add(report);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to read crash report file: {File}", file);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get recent crash reports");
            }

            return reports.AsReadOnly();
        }

        /// <inheritdoc/>
        public async Task<CrashReport?> GetReportAsync(string reportId)
        {
            if (string.IsNullOrEmpty(reportId))
            {
                _logger.LogWarning("GetReportAsync called with null/empty reportId");
                return null;
            }

            try
            {
                if (!Directory.Exists(_crashReportDirectory))
                    return null;

                // Search for file containing the report ID
                var files = Directory.GetFiles(_crashReportDirectory, "crash_*.json");

                foreach (var file in files)
                {
                    try
                    {
                        var json = await File.ReadAllTextAsync(file);
                        var report = JsonSerializer.Deserialize<CrashReport>(json, _jsonOptions);
                        if (report != null && report.Id == reportId)
                        {
                            return report;
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogDebug(ex, "Failed to read crash report file: {File}", file);
                    }
                }

                _logger.LogDebug("Crash report with ID {ReportId} not found", reportId);
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get crash report {ReportId}", reportId);
                return null;
            }
        }

        /// <inheritdoc/>
        public Task<int> CleanupOldReportsAsync(int daysToKeep = 30)
        {
            var deleted = 0;

            try
            {
                if (!Directory.Exists(_crashReportDirectory))
                    return Task.FromResult(0);

                var cutoff = DateTime.UtcNow.AddDays(-daysToKeep);
                var files = Directory.GetFiles(_crashReportDirectory, "crash_*.json");

                foreach (var file in files)
                {
                    try
                    {
                        var creationTime = File.GetCreationTimeUtc(file);
                        if (creationTime < cutoff)
                        {
                            File.Delete(file);
                            deleted++;
                            _logger.LogDebug("Deleted old crash report: {File}", file);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to delete old crash report: {File}", file);
                    }
                }

                if (deleted > 0)
                {
                    _logger.LogInformation("Cleaned up {Count} old crash reports", deleted);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to cleanup old crash reports");
            }

            return Task.FromResult(deleted);
        }
    }
}
