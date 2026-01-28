namespace SystemTrayProcessManager.Core.Models
{
    /// <summary>
    /// Aggregated configuration for all smart features.
    /// Persisted to disk and loaded on application startup.
    /// </summary>
    public sealed class SmartFeaturesConfiguration
    {
        /// <summary>
        /// Gets or sets the maximum number of entries in the focus history.
        /// </summary>
        public int FocusHistorySize { get; set; } = 10;

        /// <summary>
        /// Gets or sets a value indicating whether focus history tracking is enabled.
        /// </summary>
        public bool FocusHistoryEnabled { get; set; } = true;

        /// <summary>
        /// Gets or sets a value indicating whether auto-mute on fullscreen is enabled.
        /// </summary>
        public bool AutoMuteOnFullscreen { get; set; }

        /// <summary>
        /// Gets or sets the list of process names to auto-mute during fullscreen.
        /// </summary>
        public List<string> AutoMuteProcesses { get; set; } = new()
        {
            "Discord",
            "Slack",
            "Teams",
            "Skype"
        };

        /// <summary>
        /// Gets or sets a value indicating whether window position memory is enabled.
        /// </summary>
        public bool WindowPositionMemoryEnabled { get; set; }

        /// <summary>
        /// Gets or sets the list of saved window positions.
        /// </summary>
        public List<WindowPosition> SavedPositions { get; set; } = new();

        /// <summary>
        /// Gets or sets the gaming mode configuration.
        /// </summary>
        public GamingModeConfig GamingMode { get; set; } = GamingModeConfig.Default;

        /// <summary>
        /// Gets or sets the list of per-process priority configurations.
        /// </summary>
        public List<ProcessPriorityConfig> ProcessPriorities { get; set; } = new();

        /// <summary>
        /// Gets or sets a value indicating whether to start with Windows.
        /// </summary>
        public bool StartWithWindows { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether to start minimized.
        /// </summary>
        public bool StartMinimized { get; set; } = true;

        /// <summary>
        /// Gets or sets the last known monitor layout hash.
        /// Used to detect monitor configuration changes.
        /// </summary>
        public string? LastMonitorLayoutHash { get; set; }

        /// <summary>
        /// Gets or sets the timestamp of the last configuration save.
        /// </summary>
        public DateTime LastSaved { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Creates a default smart features configuration.
        /// </summary>
        public static SmartFeaturesConfiguration Default => new();

        /// <summary>
        /// Creates a deep copy of this configuration.
        /// </summary>
        public SmartFeaturesConfiguration Clone()
        {
            return new SmartFeaturesConfiguration
            {
                FocusHistorySize = FocusHistorySize,
                FocusHistoryEnabled = FocusHistoryEnabled,
                AutoMuteOnFullscreen = AutoMuteOnFullscreen,
                AutoMuteProcesses = new List<string>(AutoMuteProcesses),
                WindowPositionMemoryEnabled = WindowPositionMemoryEnabled,
                SavedPositions = SavedPositions.Select(p => p with { }).ToList(),
                GamingMode = GamingMode with { },
                ProcessPriorities = ProcessPriorities.Select(p => p with { }).ToList(),
                StartWithWindows = StartWithWindows,
                StartMinimized = StartMinimized,
                LastMonitorLayoutHash = LastMonitorLayoutHash,
                LastSaved = LastSaved
            };
        }
    }
}
