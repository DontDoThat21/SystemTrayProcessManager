using System.IO;
using System.Text.Json.Serialization;

namespace SystemTrayProcessManager.Core.Models
{
    /// <summary>
    /// Represents a process that should be automatically launched on application startup.
    /// </summary>
    public sealed class StartupProcess : IEquatable<StartupProcess>
    {
        /// <summary>
        /// Gets or sets the unique identifier for this startup process.
        /// </summary>
        public Guid Id { get; init; } = Guid.NewGuid();

        /// <summary>
        /// Gets or sets a user-friendly name for this startup process.
        /// </summary>
        public string Name { get; init; } = string.Empty;

        /// <summary>
        /// Gets or sets the full path to the executable to launch.
        /// </summary>
        public string ExecutablePath { get; init; } = string.Empty;

        /// <summary>
        /// Gets or sets optional command-line arguments for the process.
        /// </summary>
        public string? Arguments { get; init; }

        /// <summary>
        /// Gets or sets the working directory for the process.
        /// Null means use the executable's directory.
        /// </summary>
        public string? WorkingDirectory { get; init; }

        /// <summary>
        /// Gets or sets the delay in milliseconds before launching this process.
        /// Useful for staggering multiple startup processes.
        /// </summary>
        public int DelayMilliseconds { get; init; }

        /// <summary>
        /// Gets or sets the initial window state when launching the process.
        /// </summary>
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public Enums.WindowState LaunchWindowState { get; init; } = Enums.WindowState.Normal;

        /// <summary>
        /// Gets or sets whether this startup process is enabled.
        /// </summary>
        public bool IsEnabled { get; init; } = true;

        /// <summary>
        /// Gets or sets the sort order for launching (lower numbers launch first).
        /// </summary>
        public int SortOrder { get; init; }

        /// <summary>
        /// Gets or sets the timestamp when this startup process was created.
        /// </summary>
        public DateTime CreatedAt { get; init; } = DateTime.UtcNow;

        /// <summary>
        /// Gets a value indicating whether the executable path is valid.
        /// </summary>
        [JsonIgnore]
        public bool IsValid => !string.IsNullOrWhiteSpace(ExecutablePath) && 
                               !string.IsNullOrWhiteSpace(Name);

        /// <summary>
        /// Gets a value indicating whether the executable file exists.
        /// </summary>
        [JsonIgnore]
        public bool ExecutableExists => !string.IsNullOrWhiteSpace(ExecutablePath) && 
                                        File.Exists(ExecutablePath);

        /// <summary>
        /// Gets the display name, showing the process name or executable file name.
        /// </summary>
        [JsonIgnore]
        public string DisplayName
        {
            get
            {
                if (!string.IsNullOrWhiteSpace(Name))
                    return Name;

                var fileName = Path.GetFileNameWithoutExtension(ExecutablePath);
                return string.IsNullOrWhiteSpace(fileName) ? "Unknown" : fileName;
            }
        }

        /// <summary>
        /// Creates a copy with the enabled state updated.
        /// </summary>
        /// <param name="enabled">The new enabled state.</param>
        /// <returns>A new StartupProcess with the updated state.</returns>
        public StartupProcess WithEnabled(bool enabled)
        {
            return new StartupProcess
            {
                Id = Id,
                Name = Name,
                ExecutablePath = ExecutablePath,
                Arguments = Arguments,
                WorkingDirectory = WorkingDirectory,
                DelayMilliseconds = DelayMilliseconds,
                LaunchWindowState = LaunchWindowState,
                IsEnabled = enabled,
                SortOrder = SortOrder,
                CreatedAt = CreatedAt
            };
        }

        /// <inheritdoc/>
        public bool Equals(StartupProcess? other)
        {
            if (other is null) return false;
            return Id == other.Id;
        }

        /// <inheritdoc/>
        public override bool Equals(object? obj) => obj is StartupProcess other && Equals(other);

        /// <inheritdoc/>
        public override int GetHashCode() => Id.GetHashCode();

        /// <inheritdoc/>
        public override string ToString()
        {
            return $"StartupProcess {{ Id={Id}, Name={DisplayName}, Path={ExecutablePath}, Enabled={IsEnabled} }}";
        }

        /// <summary>
        /// Determines whether two StartupProcess instances are equal.
        /// </summary>
        public static bool operator ==(StartupProcess? left, StartupProcess? right)
        {
            if (left is null) return right is null;
            return left.Equals(right);
        }

        /// <summary>
        /// Determines whether two StartupProcess instances are not equal.
        /// </summary>
        public static bool operator !=(StartupProcess? left, StartupProcess? right) => !(left == right);
    }
}
