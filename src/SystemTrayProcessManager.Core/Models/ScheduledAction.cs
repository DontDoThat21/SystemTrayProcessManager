using System.Text.Json.Serialization;
using SystemTrayProcessManager.Core.Enums;

namespace SystemTrayProcessManager.Core.Models
{
    /// <summary>
    /// Represents a scheduled action that executes at a specific time or interval.
    /// Supports one-time, recurring interval, and daily schedule types.
    /// </summary>
    public sealed class ScheduledAction : IEquatable<ScheduledAction>
    {
        /// <summary>
        /// Gets or sets the unique identifier for this scheduled action.
        /// </summary>
        public Guid Id { get; init; } = Guid.NewGuid();

        /// <summary>
        /// Gets or sets the user-friendly name for this scheduled action.
        /// </summary>
        public string Name { get; init; } = string.Empty;

        /// <summary>
        /// Gets or sets an optional description of what this action does.
        /// </summary>
        public string Description { get; init; } = string.Empty;

        /// <summary>
        /// Gets or sets the type of action to execute.
        /// </summary>
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public ProcessActionType ActionType { get; init; }

        /// <summary>
        /// Gets or sets the target process name. Null for actions on the foreground window.
        /// </summary>
        public string? TargetProcessName { get; init; }

        /// <summary>
        /// Gets or sets the type of schedule (one-time, interval, or daily).
        /// </summary>
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public ScheduleType ScheduleType { get; init; }

        /// <summary>
        /// Gets or sets the specific time to execute (for one-time schedules).
        /// </summary>
        public DateTime? ExecuteAt { get; init; }

        /// <summary>
        /// Gets or sets the interval between executions (for interval schedules).
        /// </summary>
        public TimeSpan? Interval { get; init; }

        /// <summary>
        /// Gets or sets the time of day to execute (for daily schedules).
        /// </summary>
        public TimeSpan? DailyTime { get; init; }

        /// <summary>
        /// Gets or sets whether this scheduled action is enabled.
        /// </summary>
        public bool IsEnabled { get; init; } = true;

        /// <summary>
        /// Gets or sets the last time this action was executed.
        /// </summary>
        public DateTime? LastExecuted { get; init; }

        /// <summary>
        /// Gets or sets the next scheduled execution time.
        /// </summary>
        public DateTime? NextExecution { get; init; }

        /// <summary>
        /// Gets or sets the timestamp when this scheduled action was created.
        /// </summary>
        public DateTime CreatedAt { get; init; } = DateTime.UtcNow;

        /// <summary>
        /// Gets a value indicating whether this schedule configuration is valid.
        /// </summary>
        [JsonIgnore]
        public bool IsValid => ScheduleType switch
        {
            ScheduleType.OneTime => ExecuteAt.HasValue && ExecuteAt.Value > DateTime.UtcNow,
            ScheduleType.Interval => Interval.HasValue && Interval.Value > TimeSpan.Zero,
            ScheduleType.Daily => DailyTime.HasValue && DailyTime.Value >= TimeSpan.Zero && DailyTime.Value < TimeSpan.FromDays(1),
            _ => false
        };

        /// <summary>
        /// Gets a display-friendly description of the schedule.
        /// </summary>
        [JsonIgnore]
        public string ScheduleDescription => ScheduleType switch
        {
            ScheduleType.OneTime => ExecuteAt.HasValue ? $"Once at {ExecuteAt.Value:g}" : "Not scheduled",
            ScheduleType.Interval => Interval.HasValue ? $"Every {FormatInterval(Interval.Value)}" : "Not scheduled",
            ScheduleType.Daily => DailyTime.HasValue ? $"Daily at {DailyTime.Value:hh\\:mm}" : "Not scheduled",
            _ => "Unknown"
        };

        /// <summary>
        /// Creates a copy with the last executed timestamp updated.
        /// </summary>
        /// <param name="executedAt">The execution timestamp.</param>
        /// <returns>A new ScheduledAction with updated timestamps.</returns>
        public ScheduledAction WithExecuted(DateTime executedAt)
        {
            return new ScheduledAction
            {
                Id = Id,
                Name = Name,
                Description = Description,
                ActionType = ActionType,
                TargetProcessName = TargetProcessName,
                ScheduleType = ScheduleType,
                ExecuteAt = ExecuteAt,
                Interval = Interval,
                DailyTime = DailyTime,
                IsEnabled = ScheduleType == ScheduleType.OneTime ? false : IsEnabled, // One-time disables after execution
                LastExecuted = executedAt,
                NextExecution = CalculateNextExecution(executedAt),
                CreatedAt = CreatedAt
            };
        }

        /// <summary>
        /// Creates a copy with updated enabled state.
        /// </summary>
        /// <param name="enabled">The new enabled state.</param>
        /// <returns>A new ScheduledAction with the updated state.</returns>
        public ScheduledAction WithEnabled(bool enabled)
        {
            return new ScheduledAction
            {
                Id = Id,
                Name = Name,
                Description = Description,
                ActionType = ActionType,
                TargetProcessName = TargetProcessName,
                ScheduleType = ScheduleType,
                ExecuteAt = ExecuteAt,
                Interval = Interval,
                DailyTime = DailyTime,
                IsEnabled = enabled,
                LastExecuted = LastExecuted,
                NextExecution = enabled ? CalculateNextExecution(DateTime.UtcNow) : null,
                CreatedAt = CreatedAt
            };
        }

        /// <summary>
        /// Calculates the next execution time based on the schedule type.
        /// </summary>
        /// <param name="fromTime">The reference time to calculate from.</param>
        /// <returns>The next execution time, or null if not applicable.</returns>
        public DateTime? CalculateNextExecution(DateTime fromTime)
        {
            if (!IsEnabled) return null;

            return ScheduleType switch
            {
                ScheduleType.OneTime => ExecuteAt > fromTime ? ExecuteAt : null,
                ScheduleType.Interval when Interval.HasValue => fromTime.Add(Interval.Value),
                ScheduleType.Daily when DailyTime.HasValue => CalculateNextDaily(fromTime, DailyTime.Value),
                _ => null
            };
        }

        private static DateTime CalculateNextDaily(DateTime fromTime, TimeSpan dailyTime)
        {
            var today = fromTime.Date.Add(dailyTime);
            return today > fromTime ? today : today.AddDays(1);
        }

        private static string FormatInterval(TimeSpan interval)
        {
            if (interval.TotalDays >= 1)
                return $"{interval.TotalDays:F0} day(s)";
            if (interval.TotalHours >= 1)
                return $"{interval.TotalHours:F0} hour(s)";
            if (interval.TotalMinutes >= 1)
                return $"{interval.TotalMinutes:F0} minute(s)";
            return $"{interval.TotalSeconds:F0} second(s)";
        }

        /// <inheritdoc/>
        public bool Equals(ScheduledAction? other)
        {
            if (other is null) return false;
            return Id == other.Id;
        }

        /// <inheritdoc/>
        public override bool Equals(object? obj) => obj is ScheduledAction other && Equals(other);

        /// <inheritdoc/>
        public override int GetHashCode() => Id.GetHashCode();

        /// <inheritdoc/>
        public override string ToString()
        {
            return $"ScheduledAction {{ Id={Id}, Name={Name}, Action={ActionType}, Schedule={ScheduleDescription}, Enabled={IsEnabled} }}";
        }

        /// <summary>
        /// Determines whether two ScheduledAction instances are equal.
        /// </summary>
        public static bool operator ==(ScheduledAction? left, ScheduledAction? right)
        {
            if (left is null) return right is null;
            return left.Equals(right);
        }

        /// <summary>
        /// Determines whether two ScheduledAction instances are not equal.
        /// </summary>
        public static bool operator !=(ScheduledAction? left, ScheduledAction? right) => !(left == right);
    }
}
