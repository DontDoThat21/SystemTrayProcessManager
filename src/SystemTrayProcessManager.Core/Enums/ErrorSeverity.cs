namespace SystemTrayProcessManager.Core.Enums
{
    /// <summary>
    /// Defines the severity level of an error or exception.
    /// </summary>
    public enum ErrorSeverity
    {
        /// <summary>No severity assigned.</summary>
        None = 0,

        /// <summary>Low severity - informational issue, no action needed.</summary>
        Low = 1,

        /// <summary>Medium severity - degraded functionality, recoverable.</summary>
        Medium = 2,

        /// <summary>High severity - significant failure, user action may be required.</summary>
        High = 3,

        /// <summary>Critical severity - application stability at risk, crash report generated.</summary>
        Critical = 4
    }
}
