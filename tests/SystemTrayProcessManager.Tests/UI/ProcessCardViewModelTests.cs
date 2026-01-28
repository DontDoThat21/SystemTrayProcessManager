using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using SystemTrayProcessManager.Core.Models;
using SystemTrayProcessManager.Core.Services;
using SystemTrayProcessManager.UI.ViewModels;

namespace SystemTrayProcessManager.Tests.UI
{
    /// <summary>
    /// Unit tests for the <see cref="ProcessCardViewModel"/> class.
    /// </summary>
    public class ProcessCardViewModelTests
    {
        private readonly Mock<IWindowService> _windowServiceMock;
        private readonly Mock<IAudioService> _audioServiceMock;
        private readonly Mock<ILogger> _loggerMock;
        private readonly ProcessInfo _testProcess;

        public ProcessCardViewModelTests()
        {
            _windowServiceMock = new Mock<IWindowService>();
            _audioServiceMock = new Mock<IAudioService>();
            _loggerMock = new Mock<ILogger>();

            _testProcess = new ProcessInfo
            {
                ProcessId = 1234,
                Name = "TestProcess",
                WindowTitle = "Test Window",
                WindowHandle = new IntPtr(0x12345)
            };
        }

        private ProcessCardViewModel CreateViewModel(ProcessInfo? process = null)
        {
            return new ProcessCardViewModel(
                process ?? _testProcess,
                _windowServiceMock.Object,
                _audioServiceMock.Object,
                _loggerMock.Object);
        }

        [Fact]
        public void Constructor_NullProcessInfo_ShouldThrow()
        {
            var action = () => new ProcessCardViewModel(
                null!,
                _windowServiceMock.Object,
                _audioServiceMock.Object,
                _loggerMock.Object);

            action.Should().Throw<ArgumentNullException>().WithParameterName("processInfo");
        }

        [Fact]
        public void Constructor_NullWindowService_ShouldThrow()
        {
            var action = () => new ProcessCardViewModel(
                _testProcess,
                null!,
                _audioServiceMock.Object,
                _loggerMock.Object);

            action.Should().Throw<ArgumentNullException>().WithParameterName("windowService");
        }

        [Fact]
        public void Constructor_NullAudioService_ShouldThrow()
        {
            var action = () => new ProcessCardViewModel(
                _testProcess,
                _windowServiceMock.Object,
                null!,
                _loggerMock.Object);

            action.Should().Throw<ArgumentNullException>().WithParameterName("audioService");
        }

        [Fact]
        public void Constructor_NullLogger_ShouldThrow()
        {
            var action = () => new ProcessCardViewModel(
                _testProcess,
                _windowServiceMock.Object,
                _audioServiceMock.Object,
                null!);

            action.Should().Throw<ArgumentNullException>().WithParameterName("logger");
        }

        [Fact]
        public void Constructor_ShouldMapProcessInfoProperties()
        {
            var vm = CreateViewModel();

            vm.ProcessId.Should().Be(1234);
            vm.Name.Should().Be("TestProcess");
            vm.WindowTitle.Should().Be("Test Window");
            vm.WindowHandle.Should().NotBe(IntPtr.Zero);
        }

        [Fact]
        public void Constructor_ShouldSetDefaultValues()
        {
            var vm = CreateViewModel();

            vm.IsMuted.Should().BeFalse();
            vm.Volume.Should().Be(1.0f);
            vm.IsSelected.Should().BeFalse();
            vm.IsBusy.Should().BeFalse();
        }

        [Fact]
        public void DisplayName_WithWindowTitle_ShouldCombineNameAndTitle()
        {
            var vm = CreateViewModel();

            vm.DisplayName.Should().Be("TestProcess - Test Window");
        }

        [Fact]
        public void DisplayName_WithoutWindowTitle_ShouldUseNameOnly()
        {
            var process = new ProcessInfo { ProcessId = 1, Name = "NoTitle" };
            var vm = CreateViewModel(process);

            vm.DisplayName.Should().Be("NoTitle");
        }

        [Fact]
        public async Task ToggleMuteCommand_ShouldCallAudioService()
        {
            _audioServiceMock.Setup(s => s.ToggleMuteProcessAsync(1234)).ReturnsAsync(true);
            _audioServiceMock.Setup(s => s.IsProcessMutedAsync(1234)).ReturnsAsync(true);

            var vm = CreateViewModel();
            await vm.ToggleMuteCommand.ExecuteAsync(null);

            _audioServiceMock.Verify(s => s.ToggleMuteProcessAsync(1234), Times.Once);
            vm.IsMuted.Should().BeTrue();
        }

        [Fact]
        public async Task ToggleMuteCommand_Failure_ShouldNotChangeState()
        {
            _audioServiceMock.Setup(s => s.ToggleMuteProcessAsync(1234)).ReturnsAsync(false);

            var vm = CreateViewModel();
            vm.IsMuted = false;
            await vm.ToggleMuteCommand.ExecuteAsync(null);

            vm.IsMuted.Should().BeFalse();
        }

        [Fact]
        public async Task MinimizeCommand_ShouldCallWindowService()
        {
            _windowServiceMock.Setup(s => s.MinimizeAsync(It.IsAny<IntPtr>())).ReturnsAsync(true);

            var vm = CreateViewModel();
            await vm.MinimizeCommand.ExecuteAsync(null);

            _windowServiceMock.Verify(s => s.MinimizeAsync(_testProcess.WindowHandle), Times.Once);
        }

        [Fact]
        public async Task CloseCommand_ShouldCallWindowServiceGraceful()
        {
            _windowServiceMock.Setup(s => s.CloseAsync(It.IsAny<IntPtr>(), false)).ReturnsAsync(true);

            var vm = CreateViewModel();
            await vm.CloseCommand.ExecuteAsync(null);

            _windowServiceMock.Verify(s => s.CloseAsync(_testProcess.WindowHandle, false), Times.Once);
        }

        [Fact]
        public async Task BringToFrontCommand_ShouldCallWindowService()
        {
            _windowServiceMock.Setup(s => s.BringToFrontAsync(It.IsAny<IntPtr>())).ReturnsAsync(true);

            var vm = CreateViewModel();
            await vm.BringToFrontCommand.ExecuteAsync(null);

            _windowServiceMock.Verify(s => s.BringToFrontAsync(_testProcess.WindowHandle), Times.Once);
        }

        [Fact]
        public async Task RefreshAudioState_ShouldUpdateProperties()
        {
            _audioServiceMock.Setup(s => s.IsProcessMutedAsync(1234)).ReturnsAsync(true);
            _audioServiceMock.Setup(s => s.GetProcessVolumeAsync(1234)).ReturnsAsync(0.5f);

            var vm = CreateViewModel();
            await vm.RefreshAudioStateAsync();

            vm.IsMuted.Should().BeTrue();
            vm.Volume.Should().Be(0.5f);
        }

        [Fact]
        public async Task RefreshAudioState_NullVolume_ShouldDefaultToOne()
        {
            _audioServiceMock.Setup(s => s.IsProcessMutedAsync(1234)).ReturnsAsync((bool?)null);
            _audioServiceMock.Setup(s => s.GetProcessVolumeAsync(1234)).ReturnsAsync((float?)null);

            var vm = CreateViewModel();
            await vm.RefreshAudioStateAsync();

            vm.IsMuted.Should().BeFalse();
            vm.Volume.Should().Be(1.0f);
        }

        [Fact]
        public void IsMuted_PropertyChanged_ShouldBeRaised()
        {
            var vm = CreateViewModel();
            bool raised = false;
            vm.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(ProcessCardViewModel.IsMuted))
                    raised = true;
            };

            vm.IsMuted = true;

            raised.Should().BeTrue();
        }

        [Fact]
        public void IsSelected_PropertyChanged_ShouldBeRaised()
        {
            var vm = CreateViewModel();
            bool raised = false;
            vm.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(ProcessCardViewModel.IsSelected))
                    raised = true;
            };

            vm.IsSelected = true;

            raised.Should().BeTrue();
        }

        [Fact]
        public void IsBusy_PropertyChanged_ShouldBeRaised()
        {
            var vm = CreateViewModel();
            bool raised = false;
            vm.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(ProcessCardViewModel.IsBusy))
                    raised = true;
            };

            vm.IsBusy = true;

            raised.Should().BeTrue();
        }

        [Fact]
        public void ToString_ShouldIncludeNameAndPID()
        {
            var vm = CreateViewModel();

            vm.ToString().Should().Contain("TestProcess");
            vm.ToString().Should().Contain("1234");
        }

        [Fact]
        public void Commands_ShouldNotBeNull()
        {
            var vm = CreateViewModel();

            vm.ToggleMuteCommand.Should().NotBeNull();
            vm.MinimizeCommand.Should().NotBeNull();
            vm.CloseCommand.Should().NotBeNull();
            vm.BringToFrontCommand.Should().NotBeNull();
        }

        [Fact]
        public async Task ToggleMuteCommand_ExceptionThrown_ShouldNotCrash()
        {
            _audioServiceMock.Setup(s => s.ToggleMuteProcessAsync(1234))
                .ThrowsAsync(new InvalidOperationException("Audio error"));

            var vm = CreateViewModel();
            var action = async () => await vm.ToggleMuteCommand.ExecuteAsync(null);

            await action.Should().NotThrowAsync();
        }

        [Fact]
        public async Task MinimizeCommand_ExceptionThrown_ShouldNotCrash()
        {
            _windowServiceMock.Setup(s => s.MinimizeAsync(It.IsAny<IntPtr>()))
                .ThrowsAsync(new InvalidOperationException("Window error"));

            var vm = CreateViewModel();
            var action = async () => await vm.MinimizeCommand.ExecuteAsync(null);

            await action.Should().NotThrowAsync();
        }

        [Fact]
        public async Task CloseCommand_ExceptionThrown_ShouldNotCrash()
        {
            _windowServiceMock.Setup(s => s.CloseAsync(It.IsAny<IntPtr>(), false))
                .ThrowsAsync(new InvalidOperationException("Close error"));

            var vm = CreateViewModel();
            var action = async () => await vm.CloseCommand.ExecuteAsync(null);

            await action.Should().NotThrowAsync();
        }

        [Fact]
        public async Task BringToFrontCommand_ExceptionThrown_ShouldNotCrash()
        {
            _windowServiceMock.Setup(s => s.BringToFrontAsync(It.IsAny<IntPtr>()))
                .ThrowsAsync(new InvalidOperationException("BringToFront error"));

            var vm = CreateViewModel();
            var action = async () => await vm.BringToFrontCommand.ExecuteAsync(null);

            await action.Should().NotThrowAsync();
        }
    }
}
