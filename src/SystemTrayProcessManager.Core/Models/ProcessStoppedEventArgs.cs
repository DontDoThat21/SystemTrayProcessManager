namespace SystemTrayProcessManager.Core.Models
{
    /// <summary>
    /// Event arguments for when a process stops running.
    /// </summary>
    public sealed class ProcessStoppedEventArgs : EventArgs
    {
        /// <summary>
        /// Gets the process identifier of the stopped process.
        /// </summary>
        public int ProcessId { get; }

        /// <summary>
        /// Gets the name of the stopped process.
        /// </summary>
        public string ProcessName { get; }

        /// <summary>
        /// Gets the time when the process stop was detected.
        /// </summary>
        public DateTime StoppedAt { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="ProcessStoppedEventArgs"/> class.
        /// </summary>
        /// <param name="processId">The process identifier of the stopped process.</param>
        /// <param name="processName">The name of the stopped process.</param>
        public ProcessStoppedEventArgs(int processId, string processName)
        {
            ProcessId = processId;
            ProcessName = processName ?? string.Empty;
            StoppedAt = DateTime.UtcNow;
        }
    }
}
