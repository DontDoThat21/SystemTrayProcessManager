using SystemTrayProcessManager.Core.Enums;

namespace SystemTrayProcessManager.Core.Models
{
    /// <summary>
    /// Configuration for per-process CPU priority settings.
    /// Allows automatic priority adjustment when processes start.
    /// </summary>
    public sealed record ProcessPriorityConfig
    {
        /// <summary>
        /// Gets the unique identifier for this configuration.
        /// </summary>
        public Guid Id { get; init; } = Guid.NewGuid();

        /// <summary>
        /// Gets the name of the process (without .exe extension).
        /// </summary>
        public string ProcessName { get; init; } = string.Empty;

        /// <summary>
        /// Gets the CPU priority to set for this process.
        /// </summary>
        public ProcessPriority Priority { get; init; } = ProcessPriority.Normal;

        /// <summary>
        /// Gets a value indicating whether to automatically apply
        /// this priority when the process starts.
        /// </summary>
        public bool AutoApply { get; init; } = true;

        /// <summary>
        /// Gets the timestamp when this configuration was created.
        /// </summary>
        public DateTime CreatedAt { get; init; } = DateTime.UtcNow;

        /// <summary>
        /// Gets the timestamp when this configuration was last modified.
        /// </summary>
        public DateTime LastModified { get; init; } = DateTime.UtcNow;

        /// <summary>
        /// Creates a new process priority configuration.
        /// </summary>
        /// <param name="processName">Name of the process.</param>
        /// <param name="priority">Priority to set.</param>
        /// <param name="autoApply">Whether to auto-apply on process start.</param>
        /// <returns>A new configuration.</returns>
        public static ProcessPriorityConfig Create(
            string processName,
            ProcessPriority priority,
            bool autoApply = true)
        {
            return new ProcessPriorityConfig
            {
                Id = Guid.NewGuid(),
                ProcessName = processName ?? string.Empty,
                Priority = priority,
                AutoApply = autoApply,
                CreatedAt = DateTime.UtcNow,
                LastModified = DateTime.UtcNow
            };
        }

        /// <summary>
        /// Creates a copy with updated priority.
        /// </summary>
        public ProcessPriorityConfig WithPriority(ProcessPriority priority)
        {
            return this with
            {
                Priority = priority,
                LastModified = DateTime.UtcNow
            };
        }

        /// <summary>
        /// Creates a copy with updated auto-apply setting.
        /// </summary>
        public ProcessPriorityConfig WithAutoApply(bool autoApply)
        {
            return this with
            {
                AutoApply = autoApply,
                LastModified = DateTime.UtcNow
            };
        }

        /// <summary>
        /// Determines whether this config matches the specified process name.
        /// </summary>
        public bool Matches(string processName)
        {
            return string.Equals(ProcessName, processName, StringComparison.OrdinalIgnoreCase);
        }
    }
}
