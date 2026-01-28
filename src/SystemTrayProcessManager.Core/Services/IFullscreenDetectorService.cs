using SystemTrayProcessManager.Core.Models;

namespace SystemTrayProcessManager.Core.Services
{
    /// <summary>
    /// Provides fullscreen window detection and automatic audio muting.
    /// Monitors for fullscreen applications and mutes configured background apps.
    /// </summary>
    public interface IFullscreenDetectorService : IDisposable
    {
        /// <summary>
        /// Gets a value indicating whether any window is currently fullscreen.
        /// </summary>
        bool IsAnyWindowFullscreen { get; }

        /// <summary>
        /// Gets the current fullscreen window state, or null if no window is fullscreen.
        /// </summary>
        FullscreenState? CurrentFullscreenWindow { get; }

        /// <summary>
        /// Gets the list of process names that will be auto-muted during fullscreen.
        /// </summary>
        IReadOnlyList<string> AutoMuteProcesses { get; }

        /// <summary>
        /// Adds a process to the auto-mute list.
        /// </summary>
        /// <param name="processName">The name of the process to auto-mute.</param>
        void AddAutoMuteProcess(string processName);

        /// <summary>
        /// Removes a process from the auto-mute list.
        /// </summary>
        /// <param name="processName">The name of the process to remove.</param>
        void RemoveAutoMuteProcess(string processName);

        /// <summary>
        /// Sets the entire list of processes to auto-mute.
        /// </summary>
        /// <param name="processNames">The list of process names.</param>
        void SetAutoMuteProcesses(IEnumerable<string> processNames);

        /// <summary>
        /// Gets or sets a value indicating whether auto-mute functionality is enabled.
        /// </summary>
        bool AutoMuteEnabled { get; set; }

        /// <summary>
        /// Gets a value indicating whether detection is currently active.
        /// </summary>
        bool IsDetecting { get; }

        /// <summary>
        /// Starts fullscreen detection.
        /// </summary>
        void Start();

        /// <summary>
        /// Stops fullscreen detection.
        /// </summary>
        void Stop();

        /// <summary>
        /// Forces an immediate check for fullscreen windows.
        /// </summary>
        /// <returns>The current fullscreen state.</returns>
        Task<FullscreenState?> CheckNowAsync();

        /// <summary>
        /// Occurs when the fullscreen state changes.
        /// </summary>
        event EventHandler<FullscreenState>? FullscreenStateChanged;
    }
}
