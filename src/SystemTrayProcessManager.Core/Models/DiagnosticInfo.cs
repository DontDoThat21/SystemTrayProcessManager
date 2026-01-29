using System.Diagnostics;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.Principal;

namespace SystemTrayProcessManager.Core.Models
{
    /// <summary>
    /// Captures a snapshot of system diagnostic information for crash reports.
    /// </summary>
    public sealed class DiagnosticInfo
    {
        /// <summary>
        /// Gets or sets the operating system version string.
        /// </summary>
        public string OSVersion { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the .NET runtime version.
        /// </summary>
        public string DotNetVersion { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the machine name.
        /// </summary>
        public string MachineName { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the current user name.
        /// </summary>
        public string UserName { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the process memory usage in megabytes.
        /// </summary>
        public double ProcessMemoryMB { get; set; }

        /// <summary>
        /// Gets or sets the number of logical processors.
        /// </summary>
        public int ProcessorCount { get; set; }

        /// <summary>
        /// Gets or sets the application version string.
        /// </summary>
        public string ApplicationVersion { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the application uptime as a formatted string.
        /// </summary>
        public string Uptime { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets whether the application is running with administrator privileges.
        /// </summary>
        public bool IsAdministrator { get; set; }

        /// <summary>
        /// Gets or sets the OS architecture (e.g., "X64", "Arm64").
        /// </summary>
        public string OSArchitecture { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the process architecture.
        /// </summary>
        public string ProcessArchitecture { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the UTC timestamp when this snapshot was captured.
        /// </summary>
        public DateTime CapturedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Captures current system diagnostic information.
        /// </summary>
        /// <returns>A new <see cref="DiagnosticInfo"/> populated with current system data.</returns>
        public static DiagnosticInfo Capture()
        {
            var info = new DiagnosticInfo
            {
                OSVersion = RuntimeInformation.OSDescription,
                DotNetVersion = RuntimeInformation.FrameworkDescription,
                MachineName = GetSafe(() => Environment.MachineName, "Unknown"),
                UserName = GetSafe(() => Environment.UserName, "Unknown"),
                ProcessorCount = Environment.ProcessorCount,
                OSArchitecture = RuntimeInformation.OSArchitecture.ToString(),
                ProcessArchitecture = RuntimeInformation.ProcessArchitecture.ToString(),
                CapturedAt = DateTime.UtcNow
            };

            // Process memory
            try
            {
                using var process = Process.GetCurrentProcess();
                info.ProcessMemoryMB = Math.Round(process.WorkingSet64 / (1024.0 * 1024.0), 2);
            }
            catch
            {
                info.ProcessMemoryMB = -1;
            }

            // Application version
            try
            {
                var assembly = Assembly.GetEntryAssembly() ?? Assembly.GetExecutingAssembly();
                info.ApplicationVersion = assembly.GetName().Version?.ToString() ?? "Unknown";
            }
            catch
            {
                info.ApplicationVersion = "Unknown";
            }

            // Uptime
            try
            {
                using var process = Process.GetCurrentProcess();
                var uptime = DateTime.Now - process.StartTime;
                info.Uptime = uptime.ToString(@"d\.hh\:mm\:ss");
            }
            catch
            {
                info.Uptime = "Unknown";
            }

            // Administrator check
            try
            {
                using var identity = WindowsIdentity.GetCurrent();
                var principal = new WindowsPrincipal(identity);
                info.IsAdministrator = principal.IsInRole(WindowsBuiltInRole.Administrator);
            }
            catch
            {
                info.IsAdministrator = false;
            }

            return info;
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            return $"OS: {OSVersion}, .NET: {DotNetVersion}, Memory: {ProcessMemoryMB}MB, Admin: {IsAdministrator}";
        }

        private static string GetSafe(Func<string> getter, string fallback)
        {
            try { return getter(); }
            catch { return fallback; }
        }
    }
}
