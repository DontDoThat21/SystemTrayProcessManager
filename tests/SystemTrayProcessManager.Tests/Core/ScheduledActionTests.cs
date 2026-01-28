using SystemTrayProcessManager.Core.Enums;
using SystemTrayProcessManager.Core.Models;

namespace SystemTrayProcessManager.Tests.Core
{
    /// <summary>
    /// Unit tests for the ScheduledAction model.
    /// </summary>
    public class ScheduledActionTests
    {
        #region Constructor and Default Values

        [Fact]
        public void Constructor_ShouldSetDefaultId()
        {
            var action = new ScheduledAction();
            Assert.NotEqual(Guid.Empty, action.Id);
        }

        [Fact]
        public void Constructor_ShouldSetEmptyName()
        {
            var action = new ScheduledAction();
            Assert.Equal(string.Empty, action.Name);
        }

        [Fact]
        public void Constructor_ShouldSetEnabledToTrue()
        {
            var action = new ScheduledAction();
            Assert.True(action.IsEnabled);
        }

        [Fact]
        public void Constructor_ShouldSetNullScheduleValues()
        {
            var action = new ScheduledAction();
            Assert.Null(action.ExecuteAt);
            Assert.Null(action.Interval);
            Assert.Null(action.DailyTime);
        }

        #endregion

        #region IsValid Property

        [Fact]
        public void IsValid_OneTime_WithFutureTime_ShouldReturnTrue()
        {
            var action = new ScheduledAction
            {
                ScheduleType = ScheduleType.OneTime,
                ExecuteAt = DateTime.UtcNow.AddHours(1)
            };
            
            Assert.True(action.IsValid);
        }

        [Fact]
        public void IsValid_OneTime_WithPastTime_ShouldReturnFalse()
        {
            var action = new ScheduledAction
            {
                ScheduleType = ScheduleType.OneTime,
                ExecuteAt = DateTime.UtcNow.AddHours(-1)
            };
            
            Assert.False(action.IsValid);
        }

        [Fact]
        public void IsValid_OneTime_WithNullTime_ShouldReturnFalse()
        {
            var action = new ScheduledAction
            {
                ScheduleType = ScheduleType.OneTime,
                ExecuteAt = null
            };
            
            Assert.False(action.IsValid);
        }

        [Fact]
        public void IsValid_Interval_WithPositiveInterval_ShouldReturnTrue()
        {
            var action = new ScheduledAction
            {
                ScheduleType = ScheduleType.Interval,
                Interval = TimeSpan.FromMinutes(5)
            };
            
            Assert.True(action.IsValid);
        }

        [Fact]
        public void IsValid_Interval_WithZeroInterval_ShouldReturnFalse()
        {
            var action = new ScheduledAction
            {
                ScheduleType = ScheduleType.Interval,
                Interval = TimeSpan.Zero
            };
            
            Assert.False(action.IsValid);
        }

        [Fact]
        public void IsValid_Daily_WithValidTime_ShouldReturnTrue()
        {
            var action = new ScheduledAction
            {
                ScheduleType = ScheduleType.Daily,
                DailyTime = TimeSpan.FromHours(14) // 2:00 PM
            };
            
            Assert.True(action.IsValid);
        }

        [Fact]
        public void IsValid_Daily_WithNullTime_ShouldReturnFalse()
        {
            var action = new ScheduledAction
            {
                ScheduleType = ScheduleType.Daily,
                DailyTime = null
            };
            
            Assert.False(action.IsValid);
        }

        [Fact]
        public void IsValid_Daily_With24HourTime_ShouldReturnFalse()
        {
            var action = new ScheduledAction
            {
                ScheduleType = ScheduleType.Daily,
                DailyTime = TimeSpan.FromDays(1) // Exactly 24 hours - invalid
            };
            
            Assert.False(action.IsValid);
        }

        #endregion

        #region ScheduleDescription Property

        [Fact]
        public void ScheduleDescription_OneTime_WithTime_ShouldContainOnce()
        {
            var action = new ScheduledAction
            {
                ScheduleType = ScheduleType.OneTime,
                ExecuteAt = DateTime.UtcNow.AddDays(1)
            };
            
            Assert.Contains("Once", action.ScheduleDescription);
        }

        [Fact]
        public void ScheduleDescription_Interval_ShouldContainEvery()
        {
            var action = new ScheduledAction
            {
                ScheduleType = ScheduleType.Interval,
                Interval = TimeSpan.FromHours(2)
            };
            
            Assert.Contains("Every", action.ScheduleDescription);
        }

        [Fact]
        public void ScheduleDescription_Daily_ShouldContainDaily()
        {
            var action = new ScheduledAction
            {
                ScheduleType = ScheduleType.Daily,
                DailyTime = TimeSpan.FromHours(10)
            };
            
            Assert.Contains("Daily", action.ScheduleDescription);
        }

        #endregion

        #region WithExecuted Method

        [Fact]
        public void WithExecuted_ShouldSetLastExecuted()
        {
            var action = new ScheduledAction
            {
                ScheduleType = ScheduleType.Interval,
                Interval = TimeSpan.FromMinutes(30)
            };
            var executedAt = DateTime.UtcNow;
            
            var updated = action.WithExecuted(executedAt);
            
            Assert.Equal(executedAt, updated.LastExecuted);
        }

        [Fact]
        public void WithExecuted_OneTime_ShouldDisableAction()
        {
            var action = new ScheduledAction
            {
                ScheduleType = ScheduleType.OneTime,
                ExecuteAt = DateTime.UtcNow.AddHours(1),
                IsEnabled = true
            };
            
            var updated = action.WithExecuted(DateTime.UtcNow);
            
            Assert.False(updated.IsEnabled);
        }

        [Fact]
        public void WithExecuted_Interval_ShouldKeepEnabled()
        {
            var action = new ScheduledAction
            {
                ScheduleType = ScheduleType.Interval,
                Interval = TimeSpan.FromMinutes(30),
                IsEnabled = true
            };
            
            var updated = action.WithExecuted(DateTime.UtcNow);
            
            Assert.True(updated.IsEnabled);
        }

        [Fact]
        public void WithExecuted_Interval_ShouldCalculateNextExecution()
        {
            var interval = TimeSpan.FromMinutes(30);
            var action = new ScheduledAction
            {
                ScheduleType = ScheduleType.Interval,
                Interval = interval,
                IsEnabled = true
            };
            var executedAt = DateTime.UtcNow;
            
            var updated = action.WithExecuted(executedAt);
            
            Assert.NotNull(updated.NextExecution);
            Assert.True(updated.NextExecution >= executedAt.Add(interval));
        }

        #endregion

        #region WithEnabled Method

        [Fact]
        public void WithEnabled_True_ShouldSetIsEnabled()
        {
            var action = new ScheduledAction { IsEnabled = false };
            
            var updated = action.WithEnabled(true);
            
            Assert.True(updated.IsEnabled);
        }

        [Fact]
        public void WithEnabled_False_ShouldClearNextExecution()
        {
            var action = new ScheduledAction
            {
                ScheduleType = ScheduleType.Interval,
                Interval = TimeSpan.FromMinutes(30),
                IsEnabled = true,
                NextExecution = DateTime.UtcNow.AddMinutes(15)
            };
            
            var updated = action.WithEnabled(false);
            
            Assert.Null(updated.NextExecution);
        }

        #endregion

        #region CalculateNextExecution Method

        [Fact]
        public void CalculateNextExecution_Disabled_ShouldReturnNull()
        {
            var action = new ScheduledAction
            {
                ScheduleType = ScheduleType.Interval,
                Interval = TimeSpan.FromMinutes(30),
                IsEnabled = false
            };
            
            var next = action.CalculateNextExecution(DateTime.UtcNow);
            
            Assert.Null(next);
        }

        [Fact]
        public void CalculateNextExecution_OneTime_FutureTime_ShouldReturnExecuteAt()
        {
            var executeAt = DateTime.UtcNow.AddHours(5);
            var action = new ScheduledAction
            {
                ScheduleType = ScheduleType.OneTime,
                ExecuteAt = executeAt,
                IsEnabled = true
            };
            
            var next = action.CalculateNextExecution(DateTime.UtcNow);
            
            Assert.Equal(executeAt, next);
        }

        [Fact]
        public void CalculateNextExecution_OneTime_PastTime_ShouldReturnNull()
        {
            var action = new ScheduledAction
            {
                ScheduleType = ScheduleType.OneTime,
                ExecuteAt = DateTime.UtcNow.AddHours(-1),
                IsEnabled = true
            };
            
            var next = action.CalculateNextExecution(DateTime.UtcNow);
            
            Assert.Null(next);
        }

        [Fact]
        public void CalculateNextExecution_Interval_ShouldAddInterval()
        {
            var interval = TimeSpan.FromMinutes(45);
            var fromTime = DateTime.UtcNow;
            var action = new ScheduledAction
            {
                ScheduleType = ScheduleType.Interval,
                Interval = interval,
                IsEnabled = true
            };
            
            var next = action.CalculateNextExecution(fromTime);
            
            Assert.Equal(fromTime.Add(interval), next);
        }

        [Fact]
        public void CalculateNextExecution_Daily_BeforeTime_ShouldReturnToday()
        {
            var dailyTime = TimeSpan.FromHours(23); // 11 PM
            var fromTime = DateTime.UtcNow.Date.AddHours(10); // 10 AM today
            var action = new ScheduledAction
            {
                ScheduleType = ScheduleType.Daily,
                DailyTime = dailyTime,
                IsEnabled = true
            };
            
            var next = action.CalculateNextExecution(fromTime);
            
            Assert.Equal(fromTime.Date.Add(dailyTime), next);
        }

        #endregion

        #region Equality

        [Fact]
        public void Equals_SameId_ShouldReturnTrue()
        {
            var id = Guid.NewGuid();
            var action1 = new ScheduledAction { Id = id };
            var action2 = new ScheduledAction { Id = id };
            
            Assert.Equal(action1, action2);
        }

        [Fact]
        public void Equals_DifferentId_ShouldReturnFalse()
        {
            var action1 = new ScheduledAction();
            var action2 = new ScheduledAction();
            
            Assert.NotEqual(action1, action2);
        }

        [Fact]
        public void GetHashCode_SameId_ShouldBeSame()
        {
            var id = Guid.NewGuid();
            var action1 = new ScheduledAction { Id = id };
            var action2 = new ScheduledAction { Id = id };
            
            Assert.Equal(action1.GetHashCode(), action2.GetHashCode());
        }

        #endregion

        #region ToString

        [Fact]
        public void ToString_ShouldContainName()
        {
            var action = new ScheduledAction { Name = "TestAction" };
            
            var result = action.ToString();
            
            Assert.Contains("TestAction", result);
        }

        [Fact]
        public void ToString_ShouldContainActionType()
        {
            var action = new ScheduledAction { ActionType = ProcessActionType.Mute };
            
            var result = action.ToString();
            
            Assert.Contains("Mute", result);
        }

        #endregion
    }
}
