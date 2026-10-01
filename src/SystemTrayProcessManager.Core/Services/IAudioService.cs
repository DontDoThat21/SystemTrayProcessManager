using SystemTrayProcessManager.Core.Models;

namespace SystemTrayProcessManager.Core.Services
{
    /// <summary>
    /// Provides per-process audio control capabilities using Windows Core Audio API.
    /// Enables muting, unmuting, and volume adjustment for individual application processes.
    /// </summary>
    /// <remarks>
    /// Queries refresh sessions across all active playback devices, including applications
    /// started after this service. Mute and volume changes apply to every matching session.
    /// Exclusive-mode streams that bypass Windows session volume are not controllable here.
    /// </remarks>
    public interface IAudioService : IDisposable
    {
        /// <summary>
        /// Mutes the audio for the specified process.
        /// </summary>
        /// <param name="processId">The process identifier to mute.</param>
        /// <returns>
        /// True if the process was successfully muted; 
        /// false if the process has no audio session or the operation failed.
        /// </returns>
        Task<bool> MuteProcessAsync(int processId);

        /// <summary>
        /// Unmutes the audio for the specified process.
        /// </summary>
        /// <param name="processId">The process identifier to unmute.</param>
        /// <returns>
        /// True if the process was successfully unmuted; 
        /// false if the process has no audio session or the operation failed.
        /// </returns>
        Task<bool> UnmuteProcessAsync(int processId);

        /// <summary>
        /// Toggles the mute state for the specified process.
        /// If every session is muted, all are unmuted; otherwise all are muted.
        /// </summary>
        /// <param name="processId">The process identifier to toggle mute.</param>
        /// <returns>
        /// True if the toggle operation succeeded; 
        /// false if the process has no audio session or the operation failed.
        /// </returns>
        Task<bool> ToggleMuteProcessAsync(int processId);

        /// <summary>
        /// Sets the volume level for the specified process.
        /// </summary>
        /// <param name="processId">The process identifier to adjust volume.</param>
        /// <param name="volume">
        /// The volume level from 0.0 (silent) to 1.0 (full volume).
        /// Values outside this range will be clamped.
        /// </param>
        /// <returns>
        /// True if the volume was successfully set; 
        /// false if the process has no audio session or the operation failed.
        /// </returns>
        Task<bool> SetProcessVolumeAsync(int processId, float volume);

        /// <summary>
        /// Gets the current volume level for the specified process.
        /// </summary>
        /// <param name="processId">The process identifier to query.</param>
        /// <returns>
        /// The current volume level (0.0 to 1.0); 
        /// null if the process has no audio session or the operation failed.
        /// </returns>
        Task<float?> GetProcessVolumeAsync(int processId);

        /// <summary>
        /// Gets a value indicating whether the specified process is currently muted.
        /// </summary>
        /// <param name="processId">The process identifier to query.</param>
        /// <returns>
        /// True if the process is muted; false if unmuted; 
        /// null if the process has no audio session or the operation failed.
        /// </returns>
        Task<bool?> IsProcessMutedAsync(int processId);

        /// <summary>
        /// Gets information about all processes that currently have active audio sessions.
        /// </summary>
        /// <returns>
        /// A read-only list of audio process information for all active audio sessions.
        /// Returns an empty list if no audio sessions are found or on error.
        /// </returns>
        Task<IReadOnlyList<AudioProcessInfo>> GetAudioProcessesAsync();

        /// <summary>
        /// Gets audio session information for a specific process.
        /// </summary>
        /// <param name="processId">The process identifier to query.</param>
        /// <returns>
        /// Audio process information if an audio session exists for the process;
        /// null if no audio session exists or on error.
        /// </returns>
        Task<AudioProcessInfo?> GetAudioProcessInfoAsync(int processId);

        /// <summary>
        /// Refreshes the cached audio session information.
        /// Call this method after audio device changes or to ensure up-to-date session data.
        /// </summary>
        /// <returns>A task representing the asynchronous refresh operation.</returns>
        Task RefreshAudioSessionsAsync();

        /// <summary>
        /// Gets a value indicating whether audio services are available on this system.
        /// </summary>
        bool IsAudioAvailable { get; }
    }
}
