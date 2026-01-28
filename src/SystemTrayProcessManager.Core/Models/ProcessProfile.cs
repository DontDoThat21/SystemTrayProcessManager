using System.Text.Json.Serialization;

namespace SystemTrayProcessManager.Core.Models
{
    /// <summary>
    /// Represents a saved profile for a process containing window position, audio level, and other settings.
    /// Profiles can be auto-applied when a matching process is detected.
    /// </summary>
    public sealed class ProcessProfile : IEquatable<ProcessProfile>
    {
        /// <summary>
        /// Gets or sets the unique identifier for this profile.
        /// </summary>
        public Guid Id { get; init; } = Guid.NewGuid();

        /// <summary>
        /// Gets or sets the name of the process this profile applies to (case-insensitive matching).
        /// </summary>
        public string ProcessName { get; init; } = string.Empty;

        /// <summary>
        /// Gets or sets a user-friendly display name for this profile.
        /// </summary>
        public string DisplayName { get; init; } = string.Empty;

        /// <summary>
        /// Gets or sets the window X position. Null means don't modify.
        /// </summary>
        public int? WindowX { get; init; }

        /// <summary>
        /// Gets or sets the window Y position. Null means don't modify.
        /// </summary>
        public int? WindowY { get; init; }

        /// <summary>
        /// Gets or sets the window width. Null means don't modify.
        /// </summary>
        public int? WindowWidth { get; init; }

        /// <summary>
        /// Gets or sets the window height. Null means don't modify.
        /// </summary>
        public int? WindowHeight { get; init; }

        /// <summary>
        /// Gets or sets the audio level (0.0 to 1.0). Null means don't modify.
        /// </summary>
        public float? AudioLevel { get; init; }

        /// <summary>
        /// Gets or sets whether the process should be muted. Null means don't modify.
        /// </summary>
        public bool? IsMuted { get; init; }

        /// <summary>
        /// Gets or sets whether the window should be always on top. Null means don't modify.
        /// </summary>
        public bool? AlwaysOnTop { get; init; }

        /// <summary>
        /// Gets or sets the target window state. Null means don't modify.
        /// </summary>
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public Enums.WindowState? TargetWindowState { get; init; }

        /// <summary>
        /// Gets or sets whether this profile should be auto-applied when the process is detected.
        /// </summary>
        public bool AutoApplyOnDetection { get; init; }

        /// <summary>
        /// Gets or sets the timestamp when this profile was created.
        /// </summary>
        public DateTime CreatedAt { get; init; } = DateTime.UtcNow;

        /// <summary>
        /// Gets or sets the timestamp when this profile was last modified.
        /// </summary>
        public DateTime LastModified { get; init; } = DateTime.UtcNow;

        /// <summary>
        /// Gets a value indicating whether this profile has any window position settings.
        /// </summary>
        [JsonIgnore]
        public bool HasWindowPosition => WindowX.HasValue || WindowY.HasValue || 
                                          WindowWidth.HasValue || WindowHeight.HasValue;

        /// <summary>
        /// Gets a value indicating whether this profile has any audio settings.
        /// </summary>
        [JsonIgnore]
        public bool HasAudioSettings => AudioLevel.HasValue || IsMuted.HasValue;

        /// <summary>
        /// Gets a value indicating whether this profile has any settings to apply.
        /// </summary>
        [JsonIgnore]
        public bool HasAnySettings => HasWindowPosition || HasAudioSettings || 
                                       AlwaysOnTop.HasValue || TargetWindowState.HasValue;

        /// <summary>
        /// Creates a new profile with updated timestamp.
        /// </summary>
        /// <returns>A new profile with the current timestamp as LastModified.</returns>
        public ProcessProfile WithUpdatedTimestamp()
        {
            return new ProcessProfile
            {
                Id = Id,
                ProcessName = ProcessName,
                DisplayName = DisplayName,
                WindowX = WindowX,
                WindowY = WindowY,
                WindowWidth = WindowWidth,
                WindowHeight = WindowHeight,
                AudioLevel = AudioLevel,
                IsMuted = IsMuted,
                AlwaysOnTop = AlwaysOnTop,
                TargetWindowState = TargetWindowState,
                AutoApplyOnDetection = AutoApplyOnDetection,
                CreatedAt = CreatedAt,
                LastModified = DateTime.UtcNow
            };
        }

        /// <inheritdoc/>
        public bool Equals(ProcessProfile? other)
        {
            if (other is null) return false;
            return Id == other.Id;
        }

        /// <inheritdoc/>
        public override bool Equals(object? obj) => obj is ProcessProfile other && Equals(other);

        /// <inheritdoc/>
        public override int GetHashCode() => Id.GetHashCode();

        /// <inheritdoc/>
        public override string ToString()
        {
            return $"ProcessProfile {{ Id={Id}, ProcessName={ProcessName}, DisplayName={DisplayName}, AutoApply={AutoApplyOnDetection} }}";
        }

        /// <summary>
        /// Determines whether two ProcessProfile instances are equal.
        /// </summary>
        public static bool operator ==(ProcessProfile? left, ProcessProfile? right)
        {
            if (left is null) return right is null;
            return left.Equals(right);
        }

        /// <summary>
        /// Determines whether two ProcessProfile instances are not equal.
        /// </summary>
        public static bool operator !=(ProcessProfile? left, ProcessProfile? right) => !(left == right);
    }
}
