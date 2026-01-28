namespace SystemTrayProcessManager.Core.Enums
{
    /// <summary>
    /// Represents Windows process priority classes.
    /// Values correspond to Windows API priority class constants.
    /// </summary>
    public enum ProcessPriority
    {
        /// <summary>
        /// Idle priority class. Process runs only when system is idle.
        /// Value: IDLE_PRIORITY_CLASS (64)
        /// </summary>
        Idle = 64,

        /// <summary>
        /// Below normal priority class. Lower priority than normal.
        /// Value: BELOW_NORMAL_PRIORITY_CLASS (16384)
        /// </summary>
        BelowNormal = 16384,

        /// <summary>
        /// Normal priority class. Default for most applications.
        /// Value: NORMAL_PRIORITY_CLASS (32)
        /// </summary>
        Normal = 32,

        /// <summary>
        /// Above normal priority class. Higher priority than normal.
        /// Value: ABOVE_NORMAL_PRIORITY_CLASS (32768)
        /// </summary>
        AboveNormal = 32768,

        /// <summary>
        /// High priority class. Time-critical tasks that must execute immediately.
        /// Value: HIGH_PRIORITY_CLASS (128)
        /// </summary>
        High = 128,

        /// <summary>
        /// Realtime priority class. Highest possible priority.
        /// Value: REALTIME_PRIORITY_CLASS (256)
        /// Warning: Requires administrator privileges. Can destabilize system if misused.
        /// </summary>
        RealTime = 256
    }
}
