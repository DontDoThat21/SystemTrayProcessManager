namespace SystemTrayProcessManager.Core.Enums
{
    /// <summary>
    /// Specifies the type of schedule for a scheduled action.
    /// </summary>
    public enum ScheduleType
    {
        /// <summary>
        /// Action executes once at a specific time.
        /// </summary>
        OneTime = 0,

        /// <summary>
        /// Action executes repeatedly at a fixed interval.
        /// </summary>
        Interval = 1,

        /// <summary>
        /// Action executes daily at a specific time.
        /// </summary>
        Daily = 2
    }
}
