using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using SystemTrayProcessManager.Core.Models;
using SystemTrayProcessManager.Infrastructure.Services;

namespace SystemTrayProcessManager.Tests.Infrastructure
{
    /// <summary>
    /// Unit tests for the <see cref="NotificationService"/> class.
    /// </summary>
    public class NotificationServiceTests
    {
        private readonly Mock<ILogger<NotificationService>> _loggerMock;
        private readonly NotificationService _service;

        public NotificationServiceTests()
        {
            _loggerMock = new Mock<ILogger<NotificationService>>();
            _service = new NotificationService(_loggerMock.Object);
        }

        [Fact]
        public void Constructor_NullLogger_ShouldThrowArgumentNullException()
        {
            var action = () => new NotificationService(null!);
            action.Should().Throw<ArgumentNullException>().WithParameterName("logger");
        }

        [Fact]
        public void ActiveNotifications_Initially_ShouldBeEmpty()
        {
            _service.ActiveNotifications.Should().BeEmpty();
            _service.ActiveCount.Should().Be(0);
        }

        [Fact]
        public void AddNotification_ValidNotification_ShouldAddToList()
        {
            var notification = AppNotification.Info("Test", "Message");

            _service.AddNotification(notification);

            _service.ActiveNotifications.Should().HaveCount(1);
            _service.ActiveCount.Should().Be(1);
            _service.ActiveNotifications[0].Title.Should().Be("Test");
        }

        [Fact]
        public void AddNotification_Null_ShouldNotThrow()
        {
            var action = () => _service.AddNotification(null!);
            action.Should().NotThrow();
            _service.ActiveCount.Should().Be(0);
        }

        [Fact]
        public void AddNotification_ShouldRaiseNotificationAddedEvent()
        {
            AppNotification? raised = null;
            _service.NotificationAdded += (s, n) => raised = n;

            var notification = AppNotification.Success("Title", "Body");
            _service.AddNotification(notification);

            raised.Should().NotBeNull();
            raised!.Title.Should().Be("Title");
        }

        [Fact]
        public void AddNotification_Multiple_ShouldReturnInReverseChronologicalOrder()
        {
            _service.AddNotification(new AppNotification { Title = "First", Timestamp = DateTime.Now.AddMinutes(-2) });
            _service.AddNotification(new AppNotification { Title = "Second", Timestamp = DateTime.Now.AddMinutes(-1) });
            _service.AddNotification(new AppNotification { Title = "Third", Timestamp = DateTime.Now });

            var active = _service.ActiveNotifications;
            active.Should().HaveCount(3);
            active[0].Title.Should().Be("Third");
            active[2].Title.Should().Be("First");
        }

        [Fact]
        public void DismissNotification_ValidId_ShouldDismiss()
        {
            var notification = AppNotification.Info("Test", "Msg");
            _service.AddNotification(notification);

            _service.DismissNotification(notification.Id);

            _service.ActiveCount.Should().Be(0);
            _service.ActiveNotifications.Should().BeEmpty();
        }

        [Fact]
        public void DismissNotification_InvalidId_ShouldNotThrow()
        {
            var action = () => _service.DismissNotification("nonexistent");
            action.Should().NotThrow();
        }

        [Fact]
        public void DismissNotification_NullId_ShouldNotThrow()
        {
            var action = () => _service.DismissNotification(null!);
            action.Should().NotThrow();
        }

        [Fact]
        public void DismissNotification_EmptyId_ShouldNotThrow()
        {
            var action = () => _service.DismissNotification(string.Empty);
            action.Should().NotThrow();
        }

        [Fact]
        public void DismissNotification_ShouldRaiseNotificationDismissedEvent()
        {
            string? dismissed = null;
            _service.NotificationDismissed += (s, id) => dismissed = id;

            var notification = AppNotification.Info("Test", "Msg");
            _service.AddNotification(notification);
            _service.DismissNotification(notification.Id);

            dismissed.Should().Be(notification.Id);
        }

        [Fact]
        public void DismissAll_ShouldDismissAllNotifications()
        {
            _service.AddNotification(AppNotification.Info("A", "1"));
            _service.AddNotification(AppNotification.Info("B", "2"));
            _service.AddNotification(AppNotification.Info("C", "3"));

            _service.DismissAll();

            _service.ActiveCount.Should().Be(0);
            _service.ActiveNotifications.Should().BeEmpty();
        }

        [Fact]
        public void DismissAll_NoNotifications_ShouldNotThrow()
        {
            var action = () => _service.DismissAll();
            action.Should().NotThrow();
        }

        [Fact]
        public void AddNotification_ExceedingMaxLimit_ShouldTrimOldest()
        {
            // Add more than 50 notifications (MaxNotifications = 50)
            for (int i = 0; i < 55; i++)
            {
                _service.AddNotification(new AppNotification
                {
                    Title = $"Notification {i}",
                    AutoDismissMs = 0 // Disable auto-dismiss
                });
            }

            _service.ActiveCount.Should().BeLessOrEqualTo(50);
        }

        [Fact]
        public void ActiveNotifications_ExcludesDismissed()
        {
            var n1 = AppNotification.Info("A", "1");
            var n2 = AppNotification.Info("B", "2");
            _service.AddNotification(n1);
            _service.AddNotification(n2);

            _service.DismissNotification(n1.Id);

            _service.ActiveNotifications.Should().HaveCount(1);
            _service.ActiveNotifications[0].Title.Should().Be("B");
        }

        [Fact]
        public async Task AutoDismiss_ShouldDismissAfterDelay()
        {
            var notification = new AppNotification
            {
                Title = "Auto",
                AutoDismissMs = 100 // Short delay for testing
            };

            _service.AddNotification(notification);
            _service.ActiveCount.Should().Be(1);

            // Wait for auto-dismiss
            await Task.Delay(300);

            _service.ActiveCount.Should().Be(0);
        }

        [Fact]
        public void AddNotification_DifferentLevels_AllAdded()
        {
            _service.AddNotification(AppNotification.Info("Info", "i"));
            _service.AddNotification(AppNotification.Success("Success", "s"));
            _service.AddNotification(AppNotification.Warn("Warning", "w"));
            _service.AddNotification(AppNotification.Error("Error", "e"));

            _service.ActiveCount.Should().Be(4);
        }

        [Fact]
        public void DismissNotification_AlreadyDismissed_ShouldNotThrow()
        {
            var notification = AppNotification.Info("Test", "Msg");
            _service.AddNotification(notification);

            _service.DismissNotification(notification.Id);
            var action = () => _service.DismissNotification(notification.Id);

            action.Should().NotThrow();
        }
    }
}
