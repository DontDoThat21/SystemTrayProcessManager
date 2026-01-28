using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using SystemTrayProcessManager.Core.Models;
using SystemTrayProcessManager.Core.Services;

namespace SystemTrayProcessManager.Infrastructure.Services
{
    /// <summary>
    /// Coordinates all smart features and provides unified configuration management.
    /// Acts as a facade for accessing individual smart feature services.
    /// </summary>
    public sealed class SmartFeaturesService : ISmartFeaturesService
    {
        private readonly ILogger<SmartFeaturesService> _logger;
        private readonly string _configFilePath;
        private readonly SemaphoreSlim _configLock = new(1, 1);

        private SmartFeaturesConfiguration _config = SmartFeaturesConfiguration.Default;
        private bool _isInitialized;
        private bool _disposed;

        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
        };

        /// <inheritdoc/>
        public IFocusHistoryService FocusHistory { get; }

        /// <inheritdoc/>
        public IFullscreenDetectorService FullscreenDetector { get; }

        /// <inheritdoc/>
        public IWindowPositionService WindowPosition { get; }

        /// <inheritdoc/>
        public IGamingModeService GamingMode { get; }

        /// <inheritdoc/>
        public IProcessPriorityService ProcessPriority { get; }

        /// <inheritdoc/>
        public IStartupManagerService StartupManager { get; }

        /// <inheritdoc/>
        public bool IsInitialized => _isInitialized;

        /// <summary>
        /// Initializes a new instance of the <see cref="SmartFeaturesService"/> class.
        /// </summary>
        public SmartFeaturesService(
            ILogger<SmartFeaturesService> logger,
            IFocusHistoryService focusHistory,
            IFullscreenDetectorService fullscreenDetector,
            IWindowPositionService windowPosition,
            IGamingModeService gamingMode,
            IProcessPriorityService processPriority,
            IStartupManagerService startupManager)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            FocusHistory = focusHistory ?? throw new ArgumentNullException(nameof(focusHistory));
            FullscreenDetector = fullscreenDetector ?? throw new ArgumentNullException(nameof(fullscreenDetector));
            WindowPosition = windowPosition ?? throw new ArgumentNullException(nameof(windowPosition));
            GamingMode = gamingMode ?? throw new ArgumentNullException(nameof(gamingMode));
            ProcessPriority = processPriority ?? throw new ArgumentNullException(nameof(processPriority));
            StartupManager = startupManager ?? throw new ArgumentNullException(nameof(startupManager));

            var configDirectory = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "SystemTrayProcessManager");

            _configFilePath = Path.Combine(configDirectory, "smart-features.json");

            _logger.LogDebug("SmartFeaturesService initialized. Config path: {Path}", _configFilePath);
        }

        /// <inheritdoc/>
        public async Task InitializeAsync()
        {
            if (_isInitialized)
            {
                _logger.LogDebug("SmartFeaturesService already initialized");
                return;
            }

            try
            {
                _logger.LogInformation("Initializing smart features...");

                // Load configuration
                _config = await LoadConfigurationAsync().ConfigureAwait(false);

                // Configure individual services based on loaded config
                FocusHistory.MaxHistorySize = _config.FocusHistorySize;
                FullscreenDetector.SetAutoMuteProcesses(_config.AutoMuteProcesses);
                FullscreenDetector.AutoMuteEnabled = _config.AutoMuteOnFullscreen;
                ProcessPriority.AutoApplyEnabled = true;

                // Start enabled features
                StartAll();

                _isInitialized = true;
                _logger.LogInformation("Smart features initialized successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error initializing smart features");
                throw;
            }
        }

        /// <inheritdoc/>
        public async Task<SmartFeaturesConfiguration> LoadConfigurationAsync()
        {
            await _configLock.WaitAsync().ConfigureAwait(false);
            try
            {
                if (!File.Exists(_configFilePath))
                {
                    _logger.LogDebug("Configuration file not found, using defaults");
                    return SmartFeaturesConfiguration.Default;
                }

                var json = await File.ReadAllTextAsync(_configFilePath).ConfigureAwait(false);
                var config = JsonSerializer.Deserialize<SmartFeaturesConfiguration>(json, JsonOptions);

                _logger.LogDebug("Loaded smart features configuration");
                return config ?? SmartFeaturesConfiguration.Default;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading smart features configuration");
                return SmartFeaturesConfiguration.Default;
            }
            finally
            {
                _configLock.Release();
            }
        }

        /// <inheritdoc/>
        public async Task SaveConfigurationAsync(SmartFeaturesConfiguration config)
        {
            await _configLock.WaitAsync().ConfigureAwait(false);
            try
            {
                var directory = Path.GetDirectoryName(_configFilePath);
                if (!string.IsNullOrEmpty(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                config.LastSaved = DateTime.UtcNow;
                var json = JsonSerializer.Serialize(config, JsonOptions);
                await File.WriteAllTextAsync(_configFilePath, json).ConfigureAwait(false);

                _config = config;
                _logger.LogDebug("Saved smart features configuration");

                // Apply configuration changes to services
                ApplyConfiguration(config);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving smart features configuration");
            }
            finally
            {
                _configLock.Release();
            }
        }

        /// <inheritdoc/>
        public SmartFeaturesConfiguration GetCurrentConfiguration()
        {
            return _config.Clone();
        }

        /// <inheritdoc/>
        public void StartAll()
        {
            try
            {
                if (_config.FocusHistoryEnabled)
                {
                    FocusHistory.Start();
                    _logger.LogDebug("Focus history tracking started");
                }

                if (_config.AutoMuteOnFullscreen)
                {
                    FullscreenDetector.Start();
                    _logger.LogDebug("Fullscreen detector started");
                }

                _logger.LogInformation("Smart features started");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error starting smart features");
            }
        }

        /// <inheritdoc/>
        public void StopAll()
        {
            try
            {
                FocusHistory.Stop();
                FullscreenDetector.Stop();

                _logger.LogInformation("Smart features stopped");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error stopping smart features");
            }
        }

        /// <summary>
        /// Applies configuration changes to services.
        /// </summary>
        private void ApplyConfiguration(SmartFeaturesConfiguration config)
        {
            try
            {
                // Focus history
                FocusHistory.MaxHistorySize = config.FocusHistorySize;
                if (config.FocusHistoryEnabled && !FocusHistory.IsTracking)
                {
                    FocusHistory.Start();
                }
                else if (!config.FocusHistoryEnabled && FocusHistory.IsTracking)
                {
                    FocusHistory.Stop();
                }

                // Fullscreen detector
                FullscreenDetector.SetAutoMuteProcesses(config.AutoMuteProcesses);
                FullscreenDetector.AutoMuteEnabled = config.AutoMuteOnFullscreen;
                if (config.AutoMuteOnFullscreen && !FullscreenDetector.IsDetecting)
                {
                    FullscreenDetector.Start();
                }
                else if (!config.AutoMuteOnFullscreen && FullscreenDetector.IsDetecting)
                {
                    FullscreenDetector.Stop();
                }

                _logger.LogDebug("Configuration applied to services");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error applying configuration");
            }
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            if (_disposed) return;

            StopAll();

            FocusHistory.Dispose();
            FullscreenDetector.Dispose();

            if (ProcessPriority is IDisposable priorityDisposable)
            {
                priorityDisposable.Dispose();
            }

            _configLock.Dispose();
            _disposed = true;

            _logger.LogDebug("SmartFeaturesService disposed");
        }
    }
}
