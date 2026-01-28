using SystemTrayProcessManager.Core.Models;

namespace SystemTrayProcessManager.Core.Services
{
    /// <summary>
    /// Provides focus history tracking functionality.
    /// Tracks recently focused windows for quick switching.
    /// </summary>
    public interface IFocusHistoryService : IDisposable
    {
        /// <summary>
        /// Gets the focus history entries, most recent first.
        /// </summary>
        /// <returns>A read-only list of focus history entries.</returns>
        IReadOnlyList<FocusHistoryEntry> GetHistory();

        /// <summary>
        /// Gets the most recently focused window entry.
        /// </summary>
        /// <returns>The last focused entry, or null if history is empty.</returns>
        FocusHistoryEntry? GetLastFocused();

        /// <summary>
        /// Gets the entry at the specified position in history.
        /// </summary>
        /// <param name="index">Zero-based index (0 = most recent).</param>
        /// <returns>The entry at the index, or null if out of range.</returns>
        FocusHistoryEntry? GetEntryAt(int index);

        /// <summary>
        /// Switches focus to a previously focused window.
        /// </summary>
        /// <param name="skipCount">Number of entries to skip (0 = last focused).</param>
        /// <returns>True if the switch was successful; false otherwise.</returns>
        Task<bool> SwitchToLastFocusedAsync(int skipCount = 0);

        /// <summary>
        /// Clears the focus history.
        /// </summary>
        void ClearHistory();

        /// <summary>
        /// Gets or sets the maximum number of entries to track.
        /// </summary>
        int MaxHistorySize { get; set; }

        /// <summary>
        /// Gets a value indicating whether focus tracking is active.
        /// </summary>
        bool IsTracking { get; }

        /// <summary>
        /// Starts tracking focus changes.
        /// </summary>
        void Start();

        /// <summary>
        /// Stops tracking focus changes.
        /// </summary>
        void Stop();

        /// <summary>
        /// Occurs when a new window gains focus.
        /// </summary>
        event EventHandler<FocusHistoryEntry>? FocusChanged;
    }
}
