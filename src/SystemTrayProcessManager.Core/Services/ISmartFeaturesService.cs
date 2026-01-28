using SystemTrayProcessManager.Core.Models;

namespace SystemTrayProcessManager.Core.Services
{
    /// <summary>
    /// Provides coordinated access to all smart features.
    /// Manages initialization, configuration persistence, and feature coordination.
    /// </summary>
    public interface ISmartFeaturesService : IDisposable
    {
        /// <summary>
        /// Gets the focus history service.
        /// </summary>
        IFocusHistoryService FocusHistory { get; }

        /// <summary>
        /// Gets the fullscreen detector service.
        /// </summary>
        IFullscreenDetectorService FullscreenDetector { get; }

        /// <summary>
        /// Gets the window position service.
        /// </summary>
        IWindowPositionService WindowPosition { get; }

        /// <summary>
        /// Gets the gaming mode service.
        /// </summary>
        IGamingModeService GamingMode { get; }

        /// <summary>
        /// Gets the process priority service.
        /// </summary>
        IProcessPriorityService ProcessPriority { get; }

        /// <summary>
        /// Gets the startup manager service.
        /// </summary>
        IStartupManagerService StartupManager { get; }

        /// <summary>
        /// Gets a value indicating whether the smart features are initialized.
        /// </summary>
        bool IsInitialized { get; }

        /// <summary>
        /// Initializes all smart features and loads configuration.
        /// </summary>
        Task InitializeAsync();

        /// <summary>
        /// Loads the smart features configuration from disk.
        /// </summary>
        /// <returns>The loaded configuration.</returns>
        Task<SmartFeaturesConfiguration> LoadConfigurationAsync();

        /// <summary>
        /// Saves the smart features configuration to disk.
        /// </summary>
        /// <param name="config">The configuration to save.</param>
        Task SaveConfigurationAsync(SmartFeaturesConfiguration config);

        /// <summary>
        /// Gets the current configuration snapshot.
        /// </summary>
        /// <returns>A snapshot of the current configuration.</returns>
        SmartFeaturesConfiguration GetCurrentConfiguration();

        /// <summary>
        /// Starts all enabled smart features.
        /// </summary>
        void StartAll();

        /// <summary>
        /// Stops all smart features.
        /// </summary>
        void StopAll();
    }
}
