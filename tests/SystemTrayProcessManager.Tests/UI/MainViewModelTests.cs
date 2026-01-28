using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using SystemTrayProcessManager.Core.Models;
using SystemTrayProcessManager.Core.Services;
using SystemTrayProcessManager.UI.ViewModels;

namespace SystemTrayProcessManager.Tests.UI
{
    /// <summary>
    /// Unit tests for the <see cref="MainViewModel"/> class.
    /// </summary>
    public class MainViewModelTests
    {
        private readonly Mock<IProcessService> _processServiceMock;
        private readonly Mock<IWindowService> _windowServiceMock;
        private readonly Mock<IAudioService> _audioServiceMock;
        private readonly Mock<INotificationService> _notificationServiceMock;
        private readonly Mock<ILogger<MainViewModel>> _loggerMock;

        public MainViewModelTests()
        {
            _processServiceMock = new Mock<IProcessService>();
            _windowServiceMock = new Mock<IWindowService>();
            _audioServiceMock = new Mock<IAudioService>();
            _notificationServiceMock = new Mock<INotificationService>();
            _loggerMock = new Mock<ILogger<MainViewModel>>();
        }

        private MainViewModel CreateViewModel()
        {
            return new MainViewModel(
                _processServiceMock.Object,
                _windowServiceMock.Object,
                _audioServiceMock.Object,
                _notificationServiceMock.Object,
                _loggerMock.Object);
        }

        [Fact]
        public void Constructor_NullProcessService_ShouldThrow()
        {
            var action = () => new MainViewModel(
                null!,
                _windowServiceMock.Object,
                _audioServiceMock.Object,
                _notificationServiceMock.Object,
                _loggerMock.Object);

            action.Should().Throw<ArgumentNullException>().WithParameterName("processService");
        }

        [Fact]
        public void Constructor_NullWindowService_ShouldThrow()
        {
            var action = () => new MainViewModel(
                _processServiceMock.Object,
                null!,
                _audioServiceMock.Object,
                _notificationServiceMock.Object,
                _loggerMock.Object);

            action.Should().Throw<ArgumentNullException>().WithParameterName("windowService");
        }

        [Fact]
        public void Constructor_NullAudioService_ShouldThrow()
        {
            var action = () => new MainViewModel(
                _processServiceMock.Object,
                _windowServiceMock.Object,
                null!,
                _notificationServiceMock.Object,
                _loggerMock.Object);

            action.Should().Throw<ArgumentNullException>().WithParameterName("audioService");
        }

        [Fact]
        public void Constructor_NullNotificationService_ShouldThrow()
        {
            var action = () => new MainViewModel(
                _processServiceMock.Object,
                _windowServiceMock.Object,
                _audioServiceMock.Object,
                null!,
                _loggerMock.Object);

            action.Should().Throw<ArgumentNullException>().WithParameterName("notificationService");
        }

        [Fact]
        public void Constructor_NullLogger_ShouldThrow()
        {
            var action = () => new MainViewModel(
                _processServiceMock.Object,
                _windowServiceMock.Object,
                _audioServiceMock.Object,
                _notificationServiceMock.Object,
                null!);

            action.Should().Throw<ArgumentNullException>().WithParameterName("logger");
        }

        [Fact]
        public void Constructor_ShouldInitializeDefaults()
        {
            var vm = CreateViewModel();

            vm.SearchText.Should().BeEmpty();
            vm.IsLoading.Should().BeFalse();
            vm.TotalProcessCount.Should().Be(0);
            vm.FilteredProcessCount.Should().Be(0);
            vm.StatusText.Should().Be("Ready");
            vm.HasNotifications.Should().BeFalse();
            vm.IsNotificationPanelOpen.Should().BeFalse();
            vm.ProcessCards.Should().NotBeNull();
            vm.Notifications.Should().NotBeNull();
        }

        [Fact]
        public void Constructor_ShouldSubscribeToProcessEvents()
        {
            var vm = CreateViewModel();

            _processServiceMock.VerifyAdd(s => s.ProcessStarted += It.IsAny<EventHandler<ProcessInfo>>(), Times.Once);
            _processServiceMock.VerifyAdd(s => s.ProcessStopped += It.IsAny<EventHandler<ProcessStoppedEventArgs>>(), Times.Once);
        }

        [Fact]
        public void Constructor_ShouldSubscribeToNotificationEvents()
        {
            var vm = CreateViewModel();

            _notificationServiceMock.VerifyAdd(s => s.NotificationAdded += It.IsAny<EventHandler<AppNotification>>(), Times.Once);
            _notificationServiceMock.VerifyAdd(s => s.NotificationDismissed += It.IsAny<EventHandler<string>>(), Times.Once);
        }

        [Fact]
        public void SearchText_PropertyChanged_ShouldBeRaised()
        {
            var vm = CreateViewModel();
            bool raised = false;
            vm.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(MainViewModel.SearchText))
                    raised = true;
            };

            vm.SearchText = "test";

            raised.Should().BeTrue();
        }

        [Fact]
        public void IsLoading_PropertyChanged_ShouldBeRaised()
        {
            var vm = CreateViewModel();
            bool raised = false;
            vm.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(MainViewModel.IsLoading))
                    raised = true;
            };

            vm.IsLoading = true;

            raised.Should().BeTrue();
        }

        [Fact]
        public void StatusText_PropertyChanged_ShouldBeRaised()
        {
            var vm = CreateViewModel();
            bool raised = false;
            vm.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(MainViewModel.StatusText))
                    raised = true;
            };

            vm.StatusText = "Updated";

            raised.Should().BeTrue();
            vm.StatusText.Should().Be("Updated");
        }

        [Fact]
        public void ToggleNotificationPanel_ShouldToggleState()
        {
            var vm = CreateViewModel();

            vm.IsNotificationPanelOpen.Should().BeFalse();
            vm.ToggleNotificationPanelCommand.Execute(null);
            vm.IsNotificationPanelOpen.Should().BeTrue();
            vm.ToggleNotificationPanelCommand.Execute(null);
            vm.IsNotificationPanelOpen.Should().BeFalse();
        }

        [Fact]
        public void DismissAllNotifications_ShouldCallService()
        {
            var vm = CreateViewModel();

            vm.DismissAllNotificationsCommand.Execute(null);

            _notificationServiceMock.Verify(s => s.DismissAll(), Times.Once);
        }

        [Fact]
        public void DismissNotification_WithId_ShouldCallService()
        {
            var vm = CreateViewModel();

            vm.DismissNotificationCommand.Execute("test-id");

            _notificationServiceMock.Verify(s => s.DismissNotification("test-id"), Times.Once);
        }

        [Fact]
        public void DismissNotification_NullId_ShouldNotCallService()
        {
            var vm = CreateViewModel();

            vm.DismissNotificationCommand.Execute(null);

            _notificationServiceMock.Verify(s => s.DismissNotification(It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public void Dispose_ShouldUnsubscribeFromEvents()
        {
            var vm = CreateViewModel();

            vm.Dispose();

            _processServiceMock.VerifyRemove(s => s.ProcessStarted -= It.IsAny<EventHandler<ProcessInfo>>(), Times.Once);
            _processServiceMock.VerifyRemove(s => s.ProcessStopped -= It.IsAny<EventHandler<ProcessStoppedEventArgs>>(), Times.Once);
        }

        [Fact]
        public void Dispose_MultipleCalls_ShouldNotThrow()
        {
            var vm = CreateViewModel();

            var action = () =>
            {
                vm.Dispose();
                vm.Dispose();
            };

            action.Should().NotThrow();
        }

        [Fact]
        public void HasNotifications_Default_ShouldBeFalse()
        {
            var vm = CreateViewModel();

            vm.HasNotifications.Should().BeFalse();
        }

        [Fact]
        public void RefreshProcessesCommand_ShouldNotBeNull()
        {
            var vm = CreateViewModel();

            vm.RefreshProcessesCommand.Should().NotBeNull();
        }

        [Fact]
        public void ToggleNotificationPanelCommand_ShouldNotBeNull()
        {
            var vm = CreateViewModel();

            vm.ToggleNotificationPanelCommand.Should().NotBeNull();
        }

        [Fact]
        public void DismissAllNotificationsCommand_ShouldNotBeNull()
        {
            var vm = CreateViewModel();

            vm.DismissAllNotificationsCommand.Should().NotBeNull();
        }

        [Fact]
        public void DismissNotificationCommand_ShouldNotBeNull()
        {
            var vm = CreateViewModel();

            vm.DismissNotificationCommand.Should().NotBeNull();
        }
    }
}
