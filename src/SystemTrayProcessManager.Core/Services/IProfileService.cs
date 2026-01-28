using SystemTrayProcessManager.Core.Models;

namespace SystemTrayProcessManager.Core.Services
{
    /// <summary>
    /// Provides process profile management capabilities for saving, loading, and applying
    /// process-specific configurations including window position, audio settings, and more.
    /// </summary>
    public interface IProfileService
    {
        /// <summary>
        /// Gets a profile for the specified process name.
        /// </summary>
        /// <param name="processName">The name of the process (case-insensitive).</param>
        /// <returns>The profile if found; otherwise, null.</returns>
        Task<ProcessProfile?> GetProfileAsync(string processName);

        /// <summary>
        /// Gets a profile by its unique identifier.
        /// </summary>
        /// <param name="profileId">The unique identifier of the profile.</param>
        /// <returns>The profile if found; otherwise, null.</returns>
        Task<ProcessProfile?> GetProfileByIdAsync(Guid profileId);

        /// <summary>
        /// Gets all saved profiles.
        /// </summary>
        /// <returns>A read-only list of all profiles.</returns>
        Task<IReadOnlyList<ProcessProfile>> GetAllProfilesAsync();

        /// <summary>
        /// Saves or updates a process profile.
        /// If a profile with the same ID exists, it will be updated.
        /// </summary>
        /// <param name="profile">The profile to save.</param>
        /// <returns>True if the profile was saved successfully; otherwise, false.</returns>
        Task<bool> SaveProfileAsync(ProcessProfile profile);

        /// <summary>
        /// Deletes a profile by its unique identifier.
        /// </summary>
        /// <param name="profileId">The unique identifier of the profile to delete.</param>
        /// <returns>True if the profile was deleted; false if not found or deletion failed.</returns>
        Task<bool> DeleteProfileAsync(Guid profileId);

        /// <summary>
        /// Applies a profile's settings to a running process window.
        /// </summary>
        /// <param name="profile">The profile containing settings to apply.</param>
        /// <param name="windowHandle">The handle to the target window.</param>
        /// <param name="processId">The process identifier for audio settings.</param>
        /// <returns>True if all settings were applied successfully; otherwise, false.</returns>
        Task<bool> ApplyProfileAsync(ProcessProfile profile, IntPtr windowHandle, int processId);

        /// <summary>
        /// Captures the current settings of a process into a new profile.
        /// </summary>
        /// <param name="process">The process to capture settings from.</param>
        /// <returns>A new profile containing the current settings; null if capture failed.</returns>
        Task<ProcessProfile?> CaptureProfileAsync(ProcessInfo process);

        /// <summary>
        /// Gets all profiles that have auto-apply enabled.
        /// </summary>
        /// <returns>A read-only list of auto-apply profiles.</returns>
        Task<IReadOnlyList<ProcessProfile>> GetAutoApplyProfilesAsync();

        /// <summary>
        /// Enables or disables auto-apply for a profile.
        /// </summary>
        /// <param name="profileId">The unique identifier of the profile.</param>
        /// <param name="enabled">True to enable auto-apply; false to disable.</param>
        /// <returns>True if the profile was updated; otherwise, false.</returns>
        Task<bool> SetAutoApplyAsync(Guid profileId, bool enabled);

        /// <summary>
        /// Occurs when a profile is applied to a process.
        /// </summary>
        event EventHandler<ProfileAppliedEventArgs>? ProfileApplied;
    }

    /// <summary>
    /// Event arguments for the ProfileApplied event.
    /// </summary>
    public sealed class ProfileAppliedEventArgs : EventArgs
    {
        /// <summary>
        /// Gets the profile that was applied.
        /// </summary>
        public ProcessProfile Profile { get; }

        /// <summary>
        /// Gets the process ID the profile was applied to.
        /// </summary>
        public int ProcessId { get; }

        /// <summary>
        /// Gets a value indicating whether all settings were applied successfully.
        /// </summary>
        public bool Success { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="ProfileAppliedEventArgs"/> class.
        /// </summary>
        /// <param name="profile">The profile that was applied.</param>
        /// <param name="processId">The process ID.</param>
        /// <param name="success">Whether the application was successful.</param>
        public ProfileAppliedEventArgs(ProcessProfile profile, int processId, bool success)
        {
            Profile = profile;
            ProcessId = processId;
            Success = success;
        }
    }
}
