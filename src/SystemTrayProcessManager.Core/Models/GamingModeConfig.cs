using SystemTrayProcessManager.Core.Enums;

namespace SystemTrayProcessManager.Core.Models
{
    /// <summary>
    /// Configuration settings for Gaming Mode.
    /// Defines what actions to take when gaming mode is enabled.
    /// </summary>
    public sealed record GamingModeConfig
    {
        /// <summary>
        /// Gets a value indicating whether gaming mode is currently enabled.
        /// </summary>
        public bool IsEnabled { get; init; }

        /// <summary>
        /// Gets the list of process names to close when entering gaming mode.
        /// </summary>
        public IReadOnlyList<string> ProcessesToClose { get; init; } = Array.Empty<string>();

        /// <summary>
        /// Gets the list of process names to mute when entering gaming mode.
        /// </summary>
        public IReadOnlyList<string> ProcessesToMute { get; init; } = Array.Empty<string>();

        /// <summary>
        /// Gets the list of process names to minimize when entering gaming mode.
        /// </summary>
        public IReadOnlyList<string> ProcessesToMinimize { get; init; } = Array.Empty<string>();

        /// <summary>
        /// Gets the CPU priority to set for the active game process.
        /// </summary>
        public ProcessPriority GamePriority { get; init; } = ProcessPriority.High;

        /// <summary>
        /// Gets a value indicating whether to suppress Windows notifications.
        /// </summary>
        public bool SuppressNotifications { get; init; } = true;

        /// <summary>
        /// Gets the process name of the active game (when gaming mode is enabled).
        /// </summary>
        public string? ActiveGameProcess { get; init; }

        /// <summary>
        /// Gets the timestamp when gaming mode was enabled.
        /// </summary>
        public DateTime? EnabledAt { get; init; }

        /// <summary>
        /// Creates a default gaming mode configuration.
        /// </summary>
        public static GamingModeConfig Default => new()
        {
            IsEnabled = false,
            ProcessesToClose = new List<string>().AsReadOnly(),
            ProcessesToMute = new List<string>
            {
                "Discord",
                "Slack",
                "Teams",
                "Skype"
            }.AsReadOnly(),
            ProcessesToMinimize = new List<string>().AsReadOnly(),
            GamePriority = ProcessPriority.High,
            SuppressNotifications = true,
            ActiveGameProcess = null,
            EnabledAt = null
        };

        /// <summary>
        /// Creates a new configuration with gaming mode enabled.
        /// </summary>
        /// <param name="gameProcessName">The name of the game process.</param>
        /// <returns>A new configuration with gaming mode enabled.</returns>
        public GamingModeConfig Enable(string? gameProcessName = null)
        {
            return this with
            {
                IsEnabled = true,
                ActiveGameProcess = gameProcessName,
                EnabledAt = DateTime.UtcNow
            };
        }

        /// <summary>
        /// Creates a new configuration with gaming mode disabled.
        /// </summary>
        /// <returns>A new configuration with gaming mode disabled.</returns>
        public GamingModeConfig Disable()
        {
            return this with
            {
                IsEnabled = false,
                ActiveGameProcess = null,
                EnabledAt = null
            };
        }

        /// <summary>
        /// Creates a new configuration with updated process lists.
        /// </summary>
        public GamingModeConfig WithProcessLists(
            IEnumerable<string>? processesToClose = null,
            IEnumerable<string>? processesToMute = null,
            IEnumerable<string>? processesToMinimize = null)
        {
            return this with
            {
                ProcessesToClose = (processesToClose?.ToList() ?? ProcessesToClose.ToList()).AsReadOnly(),
                ProcessesToMute = (processesToMute?.ToList() ?? ProcessesToMute.ToList()).AsReadOnly(),
                ProcessesToMinimize = (processesToMinimize?.ToList() ?? ProcessesToMinimize.ToList()).AsReadOnly()
            };
        }
    }
}
