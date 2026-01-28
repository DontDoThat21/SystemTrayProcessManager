using Microsoft.Extensions.Logging;
using Microsoft.Win32;
using SystemTrayProcessManager.Core.Services;

namespace SystemTrayProcessManager.Infrastructure.Services
{
    /// <summary>
    /// Provides Windows startup management functionality via registry.
    /// Manages automatic application startup when Windows starts.
    /// </summary>
    public sealed class StartupManagerService : IStartupManagerService
    {
        private readonly ILogger<StartupManagerService> _logger;
        private const string RegistryKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
        private const string AppName = "SystemTrayProcessManager";

        private bool _isStartupEnabled;
        private string? _executablePath;

        /// <inheritdoc/>
        public event EventHandler<bool>? StartupStateChanged;

        /// <inheritdoc/>
        public bool IsStartupEnabled => _isStartupEnabled;

        /// <inheritdoc/>
        public string ExecutablePath => _executablePath ?? GetExecutablePath();

        /// <summary>
        /// Initializes a new instance of the <see cref="StartupManagerService"/> class.
        /// </summary>
        public StartupManagerService(ILogger<StartupManagerService> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            _executablePath = GetExecutablePath();

            // Check initial startup status
            _isStartupEnabled = CheckStartupStatus();

            _logger.LogDebug("StartupManagerService initialized. Startup enabled: {IsEnabled}, Path: {Path}",
                _isStartupEnabled, _executablePath);
        }

        /// <inheritdoc/>
        public async Task<bool> EnableStartupAsync(bool startMinimized = true)
        {
            try
            {
                return await Task.Run(() =>
                {
                    using var key = Registry.CurrentUser.OpenSubKey(RegistryKeyPath, writable: true);
                    if (key == null)
                    {
                        _logger.LogError("Failed to open registry key: {KeyPath}", RegistryKeyPath);
                        return false;
                    }

                    var executablePath = GetExecutablePath();
                    var value = startMinimized
                        ? $"\"{executablePath}\" --minimized"
                        : $"\"{executablePath}\"";

                    key.SetValue(AppName, value, RegistryValueKind.String);

                    _isStartupEnabled = true;
                    _logger.LogInformation("Enabled startup with Windows. Path: {Path}, Minimized: {Minimized}",
                        executablePath, startMinimized);

                    StartupStateChanged?.Invoke(this, true);
                    return true;
                }).ConfigureAwait(false);
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogError(ex, "Access denied when trying to enable startup");
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error enabling startup");
                return false;
            }
        }

        /// <inheritdoc/>
        public async Task<bool> DisableStartupAsync()
        {
            try
            {
                return await Task.Run(() =>
                {
                    using var key = Registry.CurrentUser.OpenSubKey(RegistryKeyPath, writable: true);
                    if (key == null)
                    {
                        _logger.LogError("Failed to open registry key: {KeyPath}", RegistryKeyPath);
                        return false;
                    }

                    // Check if value exists before trying to delete
                    var existingValue = key.GetValue(AppName);
                    if (existingValue != null)
                    {
                        key.DeleteValue(AppName, throwOnMissingValue: false);
                    }

                    _isStartupEnabled = false;
                    _logger.LogInformation("Disabled startup with Windows");

                    StartupStateChanged?.Invoke(this, false);
                    return true;
                }).ConfigureAwait(false);
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogError(ex, "Access denied when trying to disable startup");
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error disabling startup");
                return false;
            }
        }

        /// <inheritdoc/>
        public async Task<bool> ToggleStartupAsync(bool startMinimized = true)
        {
            return _isStartupEnabled
                ? await DisableStartupAsync().ConfigureAwait(false)
                : await EnableStartupAsync(startMinimized).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public async Task<bool> RefreshStartupStatusAsync()
        {
            try
            {
                return await Task.Run(() =>
                {
                    _isStartupEnabled = CheckStartupStatus();
                    _logger.LogDebug("Refreshed startup status: {IsEnabled}", _isStartupEnabled);
                    return true;
                }).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error refreshing startup status");
                return false;
            }
        }

        /// <summary>
        /// Checks the current startup status from the registry.
        /// </summary>
        private bool CheckStartupStatus()
        {
            try
            {
                using var key = Registry.CurrentUser.OpenSubKey(RegistryKeyPath, writable: false);
                if (key == null)
                {
                    return false;
                }

                var value = key.GetValue(AppName);
                return value != null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking startup status");
                return false;
            }
        }

        /// <summary>
        /// Gets the path to the current executable.
        /// </summary>
        private static string GetExecutablePath()
        {
            // Get the executing assembly's location
            var location = System.Reflection.Assembly.GetEntryAssembly()?.Location;

            if (string.IsNullOrEmpty(location))
            {
                location = Environment.ProcessPath;
            }

            // Handle .dll entries (common in .NET 5+)
            if (location?.EndsWith(".dll", StringComparison.OrdinalIgnoreCase) == true)
            {
                location = location[..^4] + ".exe";
            }

            return location ?? string.Empty;
        }
    }
}
