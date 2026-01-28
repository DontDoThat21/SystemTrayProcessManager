namespace SystemTrayProcessManager.Core.Models
{
    /// <summary>
    /// Aggregates all profile-related configurations for persistence.
    /// Contains process profiles, scheduled actions, startup processes, and process groups.
    /// </summary>
    public sealed class ProfileConfiguration
    {
        /// <summary>
        /// Gets or sets the version of this configuration schema.
        /// </summary>
        public int Version { get; init; } = 1;

        /// <summary>
        /// Gets or sets the list of process profiles.
        /// </summary>
        public List<ProcessProfile> Profiles { get; init; } = [];

        /// <summary>
        /// Gets or sets the list of scheduled actions.
        /// </summary>
        public List<ScheduledAction> ScheduledActions { get; init; } = [];

        /// <summary>
        /// Gets or sets the list of startup processes.
        /// </summary>
        public List<StartupProcess> StartupProcesses { get; init; } = [];

        /// <summary>
        /// Gets or sets the list of process groups.
        /// </summary>
        public List<ProcessGroup> ProcessGroups { get; init; } = [];

        /// <summary>
        /// Gets or sets the timestamp when this configuration was last saved.
        /// </summary>
        public DateTime LastSaved { get; init; } = DateTime.UtcNow;

        /// <summary>
        /// Creates an empty default configuration.
        /// </summary>
        /// <returns>A new ProfileConfiguration with default values.</returns>
        public static ProfileConfiguration CreateDefault()
        {
            return new ProfileConfiguration
            {
                Version = 1,
                Profiles = [],
                ScheduledActions = [],
                StartupProcesses = [],
                ProcessGroups = [],
                LastSaved = DateTime.UtcNow
            };
        }

        /// <summary>
        /// Creates a copy with the specified profiles.
        /// </summary>
        /// <param name="profiles">The profiles to include.</param>
        /// <returns>A new configuration with updated profiles.</returns>
        public ProfileConfiguration WithProfiles(List<ProcessProfile> profiles)
        {
            return new ProfileConfiguration
            {
                Version = Version,
                Profiles = profiles,
                ScheduledActions = ScheduledActions,
                StartupProcesses = StartupProcesses,
                ProcessGroups = ProcessGroups,
                LastSaved = DateTime.UtcNow
            };
        }

        /// <summary>
        /// Creates a copy with the specified scheduled actions.
        /// </summary>
        /// <param name="actions">The scheduled actions to include.</param>
        /// <returns>A new configuration with updated scheduled actions.</returns>
        public ProfileConfiguration WithScheduledActions(List<ScheduledAction> actions)
        {
            return new ProfileConfiguration
            {
                Version = Version,
                Profiles = Profiles,
                ScheduledActions = actions,
                StartupProcesses = StartupProcesses,
                ProcessGroups = ProcessGroups,
                LastSaved = DateTime.UtcNow
            };
        }

        /// <summary>
        /// Creates a copy with the specified startup processes.
        /// </summary>
        /// <param name="processes">The startup processes to include.</param>
        /// <returns>A new configuration with updated startup processes.</returns>
        public ProfileConfiguration WithStartupProcesses(List<StartupProcess> processes)
        {
            return new ProfileConfiguration
            {
                Version = Version,
                Profiles = Profiles,
                ScheduledActions = ScheduledActions,
                StartupProcesses = processes,
                ProcessGroups = ProcessGroups,
                LastSaved = DateTime.UtcNow
            };
        }

        /// <summary>
        /// Creates a copy with the specified process groups.
        /// </summary>
        /// <param name="groups">The process groups to include.</param>
        /// <returns>A new configuration with updated process groups.</returns>
        public ProfileConfiguration WithProcessGroups(List<ProcessGroup> groups)
        {
            return new ProfileConfiguration
            {
                Version = Version,
                Profiles = Profiles,
                ScheduledActions = ScheduledActions,
                StartupProcesses = StartupProcesses,
                ProcessGroups = groups,
                LastSaved = DateTime.UtcNow
            };
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            return $"ProfileConfiguration {{ Version={Version}, Profiles={Profiles.Count}, ScheduledActions={ScheduledActions.Count}, StartupProcesses={StartupProcesses.Count}, ProcessGroups={ProcessGroups.Count} }}";
        }
    }
}
