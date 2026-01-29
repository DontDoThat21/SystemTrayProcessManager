using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Security.Principal;
using SystemTrayProcessManager.Core.Services;

namespace SystemTrayProcessManager.Infrastructure.Services
{
    /// <summary>
    /// Detects administrator elevation status using <see cref="WindowsIdentity"/>
    /// and provides functionality to check if elevation is required for specific processes.
    /// </summary>
    public class ElevationService : IElevationService
    {
        private readonly ILogger<ElevationService> _logger;
        private readonly Lazy<bool> _isAdmin;

        /// <inheritdoc/>
        public bool IsRunningAsAdministrator => _isAdmin.Value;

        /// <summary>
        /// Initializes a new instance of <see cref="ElevationService"/>.
        /// </summary>
        /// <param name="logger">Logger instance.</param>
        public ElevationService(ILogger<ElevationService> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _isAdmin = new Lazy<bool>(CheckAdminStatus);
        }

        /// <inheritdoc/>
        public bool IsElevationRequired(int processId)
        {
            if (processId <= 0)
            {
                _logger.LogDebug("IsElevationRequired called with invalid processId: {ProcessId}", processId);
                return false;
            }

            try
            {
                using var process = Process.GetProcessById(processId);

                // System Idle Process (0) and System (4) always require elevation
                if (processId <= 4)
                {
                    return true;
                }

                // Try to access the process main module - this will fail for protected processes
                try
                {
                    _ = process.MainModule;
                    return false; // Successfully accessed - no elevation needed
                }
                catch (System.ComponentModel.Win32Exception)
                {
                    // Access denied - elevation required
                    _logger.LogDebug("Elevation required for process {ProcessId} ({ProcessName})",
                        processId, GetSafeProcessName(process));
                    return true;
                }
                catch (InvalidOperationException)
                {
                    // Process has exited
                    return false;
                }
            }
            catch (ArgumentException)
            {
                // Process not found
                _logger.LogDebug("Process {ProcessId} not found for elevation check", processId);
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error checking elevation requirement for process {ProcessId}", processId);
                return false;
            }
        }

        /// <inheritdoc/>
        public bool RequestElevation()
        {
            try
            {
                _logger.LogInformation("Requesting elevation via UAC prompt");

                var exePath = Environment.ProcessPath;
                if (string.IsNullOrEmpty(exePath))
                {
                    _logger.LogError("Cannot determine application path for elevation");
                    return false;
                }

                var startInfo = new ProcessStartInfo
                {
                    FileName = exePath,
                    UseShellExecute = true,
                    Verb = "runas"
                };

                var process = Process.Start(startInfo);
                if (process != null)
                {
                    _logger.LogInformation("Elevated process started with PID {PID}", process.Id);
                    return true;
                }

                _logger.LogWarning("Process.Start returned null for elevation request");
                return false;
            }
            catch (System.ComponentModel.Win32Exception ex) when (ex.NativeErrorCode == 1223)
            {
                // ERROR_CANCELLED - user declined UAC prompt
                _logger.LogInformation("User cancelled UAC elevation prompt");
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to request elevation");
                return false;
            }
        }

        /// <summary>
        /// Checks if the current process is running with administrator privileges.
        /// </summary>
        private bool CheckAdminStatus()
        {
            try
            {
                using var identity = WindowsIdentity.GetCurrent();
                var principal = new WindowsPrincipal(identity);
                var isAdmin = principal.IsInRole(WindowsBuiltInRole.Administrator);
                _logger.LogDebug("Administrator status: {IsAdmin}", isAdmin);
                return isAdmin;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to check administrator status");
                return false;
            }
        }

        /// <summary>
        /// Safely gets a process name without throwing.
        /// </summary>
        private static string GetSafeProcessName(Process process)
        {
            try { return process.ProcessName; }
            catch { return "Unknown"; }
        }
    }
}
