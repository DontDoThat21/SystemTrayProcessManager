using FluentAssertions;
using SystemTrayProcessManager.Core.Models;

namespace SystemTrayProcessManager.Tests.Core
{
    /// <summary>
    /// Unit tests for the <see cref="AppNotification"/> model.
    /// </summary>
    public class AppNotificationTests
    {
        [Fact]
        public void DefaultValues_ShouldBeCorrect()
        {
            var notification = new AppNotification();

            notification.Id.Should().NotBeNullOrEmpty();
            notification.Title.Should().BeEmpty();
            notification.Message.Should().BeEmpty();
            notification.Level.Should().Be(NotificationLevel.Info);
            notification.IsDismissed.Should().BeFalse();
            notification.AutoDismissMs.Should().Be(3000);
            notification.Timestamp.Should().BeCloseTo(DateTime.Now, TimeSpan.FromSeconds(2));
        }

        [Fact]
        public void Id_ShouldBeUnique()
        {
            var n1 = new AppNotification();
            var n2 = new AppNotification();

            n1.Id.Should().NotBe(n2.Id);
        }

        [Fact]
        public void Info_FactoryMethod_ShouldCreateInfoNotification()
        {
            var notification = AppNotification.Info("Test Title", "Test Message");

            notification.Title.Should().Be("Test Title");
            notification.Message.Should().Be("Test Message");
            notification.Level.Should().Be(NotificationLevel.Info);
        }

        [Fact]
        public void Success_FactoryMethod_ShouldCreateSuccessNotification()
        {
            var notification = AppNotification.Success("Success", "Done");

            notification.Level.Should().Be(NotificationLevel.Success);
            notification.Title.Should().Be("Success");
        }

        [Fact]
        public void Warn_FactoryMethod_ShouldCreateWarningNotification()
        {
            var notification = AppNotification.Warn("Warning", "Check this");

            notification.Level.Should().Be(NotificationLevel.Warning);
        }

        [Fact]
        public void Error_FactoryMethod_ShouldCreateErrorNotification()
        {
            var notification = AppNotification.Error("Error", "Failed");

            notification.Level.Should().Be(NotificationLevel.Error);
        }

        [Fact]
        public void IsDismissed_CanBeSet()
        {
            var notification = new AppNotification();
            notification.IsDismissed.Should().BeFalse();

            notification.IsDismissed = true;
            notification.IsDismissed.Should().BeTrue();
        }

        [Fact]
        public void TimeAgo_JustCreated_ShouldReturnJustNow()
        {
            var notification = new AppNotification { Timestamp = DateTime.Now };

            notification.TimeAgo.Should().Be("just now");
        }

        [Fact]
        public void TimeAgo_OldTimestamp_ShouldReturnMinutesAgo()
        {
            var notification = new AppNotification 
            { 
                Timestamp = DateTime.Now.AddMinutes(-5) 
            };

            notification.TimeAgo.Should().Be("5m ago");
        }

        [Fact]
        public void TimeAgo_HoursOld_ShouldReturnHoursAgo()
        {
            var notification = new AppNotification 
            { 
                Timestamp = DateTime.Now.AddHours(-3) 
            };

            notification.TimeAgo.Should().Be("3h ago");
        }

        [Fact]
        public void TimeAgo_DaysOld_ShouldReturnDateFormat()
        {
            var timestamp = DateTime.Now.AddDays(-2);
            var notification = new AppNotification { Timestamp = timestamp };

            notification.TimeAgo.Should().Be(timestamp.ToString("MMM d, HH:mm"));
        }

        [Fact]
        public void TimeAgo_SecondsOld_ShouldReturnSecondsAgo()
        {
            var notification = new AppNotification 
            { 
                Timestamp = DateTime.Now.AddSeconds(-45) 
            };

            notification.TimeAgo.Should().Be("45s ago");
        }

        [Fact]
        public void ToString_ShouldIncludeLevelTitleAndMessage()
        {
            var notification = AppNotification.Error("TestTitle", "TestMsg");

            notification.ToString().Should().Contain("Error");
            notification.ToString().Should().Contain("TestTitle");
            notification.ToString().Should().Contain("TestMsg");
        }

        [Fact]
        public void AutoDismissMs_CanBeCustomized()
        {
            var notification = new AppNotification { AutoDismissMs = 5000 };

            notification.AutoDismissMs.Should().Be(5000);
        }

        [Fact]
        public void AutoDismissMs_ZeroMeansNoDismiss()
        {
            var notification = new AppNotification { AutoDismissMs = 0 };

            notification.AutoDismissMs.Should().Be(0);
        }

        [Theory]
        [InlineData(NotificationLevel.Info)]
        [InlineData(NotificationLevel.Success)]
        [InlineData(NotificationLevel.Warning)]
        [InlineData(NotificationLevel.Error)]
        public void Level_CanBeSetToAllValues(NotificationLevel level)
        {
            var notification = new AppNotification { Level = level };

            notification.Level.Should().Be(level);
        }
    }
}
