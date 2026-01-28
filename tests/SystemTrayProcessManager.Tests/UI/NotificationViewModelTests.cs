using FluentAssertions;
using SystemTrayProcessManager.Core.Models;
using SystemTrayProcessManager.UI.ViewModels;

namespace SystemTrayProcessManager.Tests.UI
{
    /// <summary>
    /// Unit tests for the <see cref="NotificationViewModel"/> class.
    /// </summary>
    public class NotificationViewModelTests
    {
        [Fact]
        public void Constructor_NullNotification_ShouldThrow()
        {
            var action = () => new NotificationViewModel(null!);

            action.Should().Throw<ArgumentNullException>().WithParameterName("notification");
        }

        [Fact]
        public void Constructor_ShouldMapProperties()
        {
            var notification = new AppNotification
            {
                Title = "Test",
                Message = "Body",
                Level = NotificationLevel.Warning
            };

            var vm = new NotificationViewModel(notification);

            vm.Id.Should().Be(notification.Id);
            vm.Title.Should().Be("Test");
            vm.Message.Should().Be("Body");
            vm.Level.Should().Be(NotificationLevel.Warning);
            vm.Timestamp.Should().Be(notification.Timestamp);
        }

        [Fact]
        public void TimeAgo_ShouldDelegateToModel()
        {
            var notification = new AppNotification { Timestamp = DateTime.Now };
            var vm = new NotificationViewModel(notification);

            vm.TimeAgo.Should().Be("just now");
        }

        [Theory]
        [InlineData(NotificationLevel.Info, "\u2139")]
        [InlineData(NotificationLevel.Success, "\u2713")]
        [InlineData(NotificationLevel.Warning, "\u26A0")]
        [InlineData(NotificationLevel.Error, "\u2717")]
        public void LevelIcon_ShouldReturnCorrectSymbol(NotificationLevel level, string expectedIcon)
        {
            var notification = new AppNotification { Level = level };
            var vm = new NotificationViewModel(notification);

            vm.LevelIcon.Should().Be(expectedIcon);
        }

        [Fact]
        public void ToString_ShouldIncludeLevelTitleMessage()
        {
            var notification = AppNotification.Error("ErrorTitle", "ErrorMsg");
            var vm = new NotificationViewModel(notification);

            vm.ToString().Should().Contain("Error");
            vm.ToString().Should().Contain("ErrorTitle");
            vm.ToString().Should().Contain("ErrorMsg");
        }

        [Fact]
        public void Id_ShouldNotBeNullOrEmpty()
        {
            var notification = new AppNotification();
            var vm = new NotificationViewModel(notification);

            vm.Id.Should().NotBeNullOrEmpty();
        }

        [Fact]
        public void Timestamp_ShouldBeRecentlyCreated()
        {
            var notification = new AppNotification();
            var vm = new NotificationViewModel(notification);

            vm.Timestamp.Should().BeCloseTo(DateTime.Now, TimeSpan.FromSeconds(2));
        }

        [Fact]
        public void AllLevels_ShouldHaveIcons()
        {
            foreach (NotificationLevel level in Enum.GetValues(typeof(NotificationLevel)))
            {
                var notification = new AppNotification { Level = level };
                var vm = new NotificationViewModel(notification);

                vm.LevelIcon.Should().NotBeNullOrEmpty();
            }
        }
    }
}
