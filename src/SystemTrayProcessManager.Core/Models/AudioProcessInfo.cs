namespace SystemTrayProcessManager.Core.Models
{
    /// <summary>
    /// Represents audio session information for a process.
    /// Contains volume level, mute state, and session activity status.
    /// </summary>
    public sealed class AudioProcessInfo
    {
        /// <summary>
        /// Gets the process identifier for the audio session.
        /// </summary>
        public int ProcessId { get; init; }

        /// <summary>
        /// Gets the name of the process associated with this audio session.
        /// </summary>
        public string ProcessName { get; init; } = string.Empty;

        /// <summary>
        /// Gets the display name for the audio session.
        /// This may be set by the application and differs from the process name.
        /// </summary>
        public string DisplayName { get; init; } = string.Empty;

        /// <summary>
        /// Gets the current volume level for this audio session.
        /// Value ranges from 0.0 (silent) to 1.0 (full volume).
        /// </summary>
        public float Volume { get; init; }

        /// <summary>
        /// Gets a value indicating whether this audio session is currently muted.
        /// </summary>
        public bool IsMuted { get; init; }

        /// <summary>
        /// Gets a value indicating whether this audio session is currently active (producing audio).
        /// </summary>
        public bool IsActive { get; init; }

        /// <summary>
        /// Gets the session identifier string.
        /// </summary>
        public string SessionIdentifier { get; init; } = string.Empty;

        /// <summary>
        /// Gets the icon path for the audio session if available.
        /// </summary>
        public string? IconPath { get; init; }

        /// <summary>
        /// Returns a string representation of the audio process info.
        /// </summary>
        /// <returns>A string containing the process name, ID, volume, and mute state.</returns>
        public override string ToString()
        {
            var muteStatus = IsMuted ? "Muted" : "Unmuted";
            var volumePercent = (int)(Volume * 100);
            return $"{ProcessName} (PID: {ProcessId}) - {volumePercent}% [{muteStatus}]";
        }
    }
}
