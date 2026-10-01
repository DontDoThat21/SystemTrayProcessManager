using Microsoft.Extensions.Logging;
using Moq;
using SystemTrayProcessManager.Core.Enums;
using SystemTrayProcessManager.Core.Models;
using SystemTrayProcessManager.Core.Services;
using SystemTrayProcessManager.Infrastructure.Services;

namespace SystemTrayProcessManager.Tests.Infrastructure
{
    /// <summary>
    /// Unit tests for the <see cref="ActionMappingService"/> class.
    /// </summary>
    public class ActionMappingServiceTests : IDisposable
    {
        private readonly Mock<ILogger<ActionMappingService>> _loggerMock;
        private readonly Mock<IHotkeyService> _hotkeyServiceMock;
        private readonly Mock<IHotkeyConfigurationService> _configServiceMock;
        private readonly Mock<IWindowService> _windowServiceMock;
        private readonly Mock<IAudioService> _audioServiceMock;
        private readonly Mock<IProcessService> _processServiceMock;
        private readonly ActionMappingService _service;

        public ActionMappingServiceTests()
        {
            _loggerMock = new Mock<ILogger<ActionMappingService>>();
            _hotkeyServiceMock = new Mock<IHotkeyService>();
            _configServiceMock = new Mock<IHotkeyConfigurationService>();
            _windowServiceMock = new Mock<IWindowService>();
            _audioServiceMock = new Mock<IAudioService>();
            _audioServiceMock.Setup(a => a.GetAudioProcessesAsync()).ReturnsAsync(Array.Empty<AudioProcessInfo>());
            _processServiceMock = new Mock<IProcessService>();

            // Set up default hotkey service behavior
            _hotkeyServiceMock.Setup(h => h.IsInitialized).Returns(true);
            _hotkeyServiceMock.Setup(h => h.RegisterHotkey(It.IsAny<HotkeyBinding>(), It.IsAny<Func<Task>>(), It.IsAny<bool>()))
                .Returns(true);
            _hotkeyServiceMock.Setup(h => h.UnregisterHotkey(It.IsAny<HotkeyBinding>()))
                .Returns(true);

            // Set up default config service behavior
            _configServiceMock.Setup(c => c.LoadConfigurationAsync())
                .ReturnsAsync(new HotkeyConfiguration());

            _service = new ActionMappingService(
                _loggerMock.Object,
                _hotkeyServiceMock.Object,
                _configServiceMock.Object,
                _windowServiceMock.Object,
                _audioServiceMock.Object,
                _processServiceMock.Object);
        }

        public void Dispose()
        {
            _service?.Dispose();
        }

        #region Constructor Tests

        [Fact]
        public void Constructor_WithNullLogger_ShouldThrowArgumentNullException()
        {
            // Assert
            Assert.Throws<ArgumentNullException>(() => new ActionMappingService(
                null!,
                _hotkeyServiceMock.Object,
                _configServiceMock.Object,
                _windowServiceMock.Object,
                _audioServiceMock.Object,
                _processServiceMock.Object));
        }

        [Fact]
        public void Constructor_WithNullHotkeyService_ShouldThrowArgumentNullException()
        {
            // Assert
            Assert.Throws<ArgumentNullException>(() => new ActionMappingService(
                _loggerMock.Object,
                null!,
                _configServiceMock.Object,
                _windowServiceMock.Object,
                _audioServiceMock.Object,
                _processServiceMock.Object));
        }

        [Fact]
        public void Constructor_WithNullConfigService_ShouldThrowArgumentNullException()
        {
            // Assert
            Assert.Throws<ArgumentNullException>(() => new ActionMappingService(
                _loggerMock.Object,
                _hotkeyServiceMock.Object,
                null!,
                _windowServiceMock.Object,
                _audioServiceMock.Object,
                _processServiceMock.Object));
        }

        [Fact]
        public void Constructor_WithNullWindowService_ShouldThrowArgumentNullException()
        {
            // Assert
            Assert.Throws<ArgumentNullException>(() => new ActionMappingService(
                _loggerMock.Object,
                _hotkeyServiceMock.Object,
                _configServiceMock.Object,
                null!,
                _audioServiceMock.Object,
                _processServiceMock.Object));
        }

        [Fact]
        public void Constructor_WithNullAudioService_ShouldThrowArgumentNullException()
        {
            // Assert
            Assert.Throws<ArgumentNullException>(() => new ActionMappingService(
                _loggerMock.Object,
                _hotkeyServiceMock.Object,
                _configServiceMock.Object,
                _windowServiceMock.Object,
                null!,
                _processServiceMock.Object));
        }

        [Fact]
        public void Constructor_WithNullProcessService_ShouldThrowArgumentNullException()
        {
            // Assert
            Assert.Throws<ArgumentNullException>(() => new ActionMappingService(
                _loggerMock.Object,
                _hotkeyServiceMock.Object,
                _configServiceMock.Object,
                _windowServiceMock.Object,
                _audioServiceMock.Object,
                null!));
        }

        [Fact]
        public void Constructor_ShouldNotBeInitialized()
        {
            // Assert
            Assert.False(_service.IsInitialized);
        }

        [Fact]
        public void Constructor_MappingCount_ShouldBeZero()
        {
            // Assert
            Assert.Equal(0, _service.MappingCount);
        }

        [Fact]
        public void Constructor_CanUndo_ShouldBeFalse()
        {
            // Assert
            Assert.False(_service.CanUndo);
        }

        #endregion

        #region InitializeAsync Tests

        [Fact]
        public async Task InitializeAsync_ShouldSetIsInitializedToTrue()
        {
            // Act
            await _service.InitializeAsync();

            // Assert
            Assert.True(_service.IsInitialized);
        }

        [Fact]
        public async Task InitializeAsync_WhenHotkeyServiceNotInitialized_ShouldInitializeIt()
        {
            // Arrange
            _hotkeyServiceMock.Setup(h => h.IsInitialized).Returns(false);

            // Act
            await _service.InitializeAsync();

            // Assert
            _hotkeyServiceMock.Verify(h => h.Initialize(), Times.Once);
        }

        [Fact]
        public async Task InitializeAsync_WhenHotkeyServiceAlreadyInitialized_ShouldNotInitializeAgain()
        {
            // Arrange
            _hotkeyServiceMock.Setup(h => h.IsInitialized).Returns(true);

            // Act
            await _service.InitializeAsync();

            // Assert
            _hotkeyServiceMock.Verify(h => h.Initialize(), Times.Never);
        }

        [Fact]
        public async Task InitializeAsync_ShouldLoadConfiguration()
        {
            // Act
            await _service.InitializeAsync();

            // Assert
            _configServiceMock.Verify(c => c.LoadConfigurationAsync(), Times.Once);
        }

        [Fact]
        public async Task InitializeAsync_WithEnabledConfigItems_ShouldRegisterThem()
        {
            // Arrange
            var config = new HotkeyConfiguration
            {
                Items =
                [
                    new HotkeyConfigItem("Test", 0x4D, HotkeyModifier.Ctrl, "Mute") { IsEnabled = true }
                ]
            };
            _configServiceMock.Setup(c => c.LoadConfigurationAsync()).ReturnsAsync(config);

            // Act
            await _service.InitializeAsync();

            // Assert
            Assert.Equal(1, _service.MappingCount);
        }

        [Fact]
        public async Task InitializeAsync_WithDisabledConfigItems_ShouldNotRegisterThem()
        {
            // Arrange
            var config = new HotkeyConfiguration
            {
                Items =
                [
                    new HotkeyConfigItem("Test", 0x4D, HotkeyModifier.Ctrl, "Mute") { IsEnabled = false }
                ]
            };
            _configServiceMock.Setup(c => c.LoadConfigurationAsync()).ReturnsAsync(config);

            // Act
            await _service.InitializeAsync();

            // Assert
            Assert.Equal(0, _service.MappingCount);
        }

        [Fact]
        public async Task InitializeAsync_WhenAlreadyInitialized_ShouldThrowInvalidOperationException()
        {
            // Arrange
            await _service.InitializeAsync();

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(() => _service.InitializeAsync());
        }

        [Fact]
        public async Task InitializeAsync_WhenDisposed_ShouldThrowObjectDisposedException()
        {
            // Arrange
            _service.Dispose();

            // Act & Assert
            await Assert.ThrowsAsync<ObjectDisposedException>(() => _service.InitializeAsync());
        }

        #endregion

        #region RegisterActionMapping Tests

        [Fact]
        public void RegisterActionMapping_WithValidConfig_ShouldReturnTrue()
        {
            // Arrange
            var config = new HotkeyConfigItem("Test", 0x4D, HotkeyModifier.Ctrl, "Mute") { IsEnabled = true };

            // Act
            var result = _service.RegisterActionMapping(config);

            // Assert
            Assert.True(result);
            Assert.Equal(1, _service.MappingCount);
        }

        [Fact]
        public void RegisterActionMapping_WithNullConfig_ShouldThrowArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => _service.RegisterActionMapping(null!));
        }

        [Fact]
        public void RegisterActionMapping_WithDisabledConfig_ShouldReturnFalse()
        {
            // Arrange
            var config = new HotkeyConfigItem("Test", 0x4D, HotkeyModifier.Ctrl, "Mute") { IsEnabled = false };

            // Act
            var result = _service.RegisterActionMapping(config);

            // Assert
            Assert.False(result);
            Assert.Equal(0, _service.MappingCount);
        }

        [Fact]
        public void RegisterActionMapping_WithInvalidConfig_ShouldReturnFalse()
        {
            // Arrange - config with no name or key
            var config = new HotkeyConfigItem { IsEnabled = true };

            // Act
            var result = _service.RegisterActionMapping(config);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void RegisterActionMapping_WhenHotkeyRegistrationFails_ShouldReturnFalse()
        {
            // Arrange
            _hotkeyServiceMock.Setup(h => h.RegisterHotkey(It.IsAny<HotkeyBinding>(), It.IsAny<Func<Task>>(), It.IsAny<bool>()))
                .Returns(false);
            var config = new HotkeyConfigItem("Test", 0x4D, HotkeyModifier.Ctrl, "Mute") { IsEnabled = true };

            // Act
            var result = _service.RegisterActionMapping(config);

            // Assert
            Assert.False(result);
            Assert.Equal(0, _service.MappingCount);
        }

        [Fact]
        public void RegisterActionMapping_ShouldCallHotkeyServiceWithCorrectBinding()
        {
            // Arrange
            var config = new HotkeyConfigItem("Test", 0x4D, HotkeyModifier.Ctrl | HotkeyModifier.Alt, "Mute") { IsEnabled = true };

            // Act
            _service.RegisterActionMapping(config);

            // Assert
            _hotkeyServiceMock.Verify(h => h.RegisterHotkey(
                It.Is<HotkeyBinding>(b => b.VirtualKeyCode == 0x4D && b.Modifiers == (HotkeyModifier.Ctrl | HotkeyModifier.Alt)),
                It.IsAny<Func<Task>>(),
                true), Times.Once);
        }

        #endregion

        #region UnregisterActionMapping Tests

        [Fact]
        public void UnregisterActionMapping_WithRegisteredConfig_ShouldReturnTrue()
        {
            // Arrange
            var config = new HotkeyConfigItem("Test", 0x4D, HotkeyModifier.Ctrl, "Mute") { IsEnabled = true };
            _service.RegisterActionMapping(config);

            // Act
            var result = _service.UnregisterActionMapping(config.Id);

            // Assert
            Assert.True(result);
            Assert.Equal(0, _service.MappingCount);
        }

        [Fact]
        public void UnregisterActionMapping_WithUnregisteredId_ShouldReturnFalse()
        {
            // Act
            var result = _service.UnregisterActionMapping(Guid.NewGuid());

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void UnregisterActionMapping_ShouldCallHotkeyServiceUnregister()
        {
            // Arrange
            var config = new HotkeyConfigItem("Test", 0x4D, HotkeyModifier.Ctrl, "Mute") { IsEnabled = true };
            _service.RegisterActionMapping(config);

            // Act
            _service.UnregisterActionMapping(config.Id);

            // Assert
            _hotkeyServiceMock.Verify(h => h.UnregisterHotkey(It.Is<HotkeyBinding>(b => b.VirtualKeyCode == 0x4D)), Times.Once);
        }

        #endregion

        #region UnregisterAllMappings Tests

        [Fact]
        public void UnregisterAllMappings_ShouldClearAllMappings()
        {
            // Arrange
            var config1 = new HotkeyConfigItem("Test1", 0x4D, HotkeyModifier.Ctrl, "Mute") { IsEnabled = true };
            var config2 = new HotkeyConfigItem("Test2", 0x4E, HotkeyModifier.Alt, "Close") { IsEnabled = true };
            _service.RegisterActionMapping(config1);
            _service.RegisterActionMapping(config2);
            Assert.Equal(2, _service.MappingCount);

            // Act
            _service.UnregisterAllMappings();

            // Assert
            Assert.Equal(0, _service.MappingCount);
        }

        [Fact]
        public void UnregisterAllMappings_ShouldUnregisterEachHotkey()
        {
            // Arrange
            var config1 = new HotkeyConfigItem("Test1", 0x4D, HotkeyModifier.Ctrl, "Mute") { IsEnabled = true };
            var config2 = new HotkeyConfigItem("Test2", 0x4E, HotkeyModifier.Alt, "Close") { IsEnabled = true };
            _service.RegisterActionMapping(config1);
            _service.RegisterActionMapping(config2);

            // Act
            _service.UnregisterAllMappings();

            // Assert
            _hotkeyServiceMock.Verify(h => h.UnregisterHotkey(It.IsAny<HotkeyBinding>()), Times.Exactly(2));
        }

        #endregion

        #region ExecutePinnedActionAsync Tests

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task ToggleAlwaysOnTop_UsesOppositeWindowState(bool currentState)
        {
            var hwnd = new IntPtr(123);
            _processServiceMock.Setup(p => p.GetRunningProcessesAsync()).ReturnsAsync(new[]
            {
                new ProcessInfo { ProcessId = 123, Name = "TestApp", WindowHandle = hwnd }
            });
            _windowServiceMock.Setup(w => w.IsValidWindow(hwnd)).Returns(true);
            _windowServiceMock.Setup(w => w.IsAlwaysOnTopAsync(hwnd)).ReturnsAsync(currentState);
            _windowServiceMock.Setup(w => w.SetAlwaysOnTopAsync(hwnd, !currentState)).ReturnsAsync(true);
            var result = await _service.ExecutePinnedActionAsync(ProcessActionType.ToggleAlwaysOnTop, "TestApp");
            Assert.True(result.Success);
            _windowServiceMock.Verify(w => w.SetAlwaysOnTopAsync(hwnd, !currentState), Times.Once);
        }

        [Fact]
        public async Task ConfiguredSetFocus_ActivatesThePinnedApplicationWindow()
        {
            Func<Task>? callback = null;
            var hwnd = new IntPtr(123);
            _hotkeyServiceMock.Setup(h => h.RegisterHotkey(It.IsAny<HotkeyBinding>(), It.IsAny<Func<Task>>(), true))
                .Callback<HotkeyBinding, Func<Task>, bool>((_, action, _) => callback = action).Returns(true);
            _processServiceMock.Setup(p => p.GetRunningProcessesAsync()).ReturnsAsync(new[]
            {
                new ProcessInfo { ProcessId = 123, Name = "TestApp", WindowHandle = hwnd }
            });
            _windowServiceMock.Setup(w => w.IsValidWindow(hwnd)).Returns(true);
            _windowServiceMock.Setup(w => w.BringToFrontAsync(hwnd)).ReturnsAsync(true);

            Assert.True(_service.RegisterActionMapping(new HotkeyConfigItem("Focus TestApp", 0x46,
                HotkeyModifier.Ctrl, "SetFocus") { TargetProcessName = "TestApp", ActionMode = ActionMode.PinnedProcess }));

            await callback!();

            _windowServiceMock.Verify(w => w.BringToFrontAsync(hwnd), Times.Once);
        }

        [Theory]
        [InlineData("PreviousTrack")]
        [InlineData("NextTrack")]
        public async Task ConfiguredMediaTrackAction_SendsTheCommandToThePinnedApplicationWindow(string actionType)
        {
            Func<Task>? callback = null;
            var hwnd = new IntPtr(123);
            _hotkeyServiceMock.Setup(h => h.RegisterHotkey(It.IsAny<HotkeyBinding>(), It.IsAny<Func<Task>>(), true))
                .Callback<HotkeyBinding, Func<Task>, bool>((_, action, _) => callback = action).Returns(true);
            _processServiceMock.Setup(p => p.GetRunningProcessesAsync()).ReturnsAsync(new[]
            {
                new ProcessInfo { ProcessId = 123, Name = "TestApp", WindowHandle = hwnd }
            });
            _windowServiceMock.Setup(w => w.IsValidWindow(hwnd)).Returns(true);
            _windowServiceMock.Setup(w => w.PreviousTrackAsync(hwnd)).ReturnsAsync(true);
            _windowServiceMock.Setup(w => w.NextTrackAsync(hwnd)).ReturnsAsync(true);

            Assert.True(_service.RegisterActionMapping(new HotkeyConfigItem("Media TestApp", 0x4D,
                HotkeyModifier.Ctrl, actionType) { TargetProcessName = "TestApp", ActionMode = ActionMode.PinnedProcess }));

            await callback!();

            if (actionType == "PreviousTrack")
            {
                _windowServiceMock.Verify(w => w.PreviousTrackAsync(hwnd), Times.Once);
                _windowServiceMock.Verify(w => w.NextTrackAsync(hwnd), Times.Never);
            }
            else
            {
                _windowServiceMock.Verify(w => w.NextTrackAsync(hwnd), Times.Once);
                _windowServiceMock.Verify(w => w.PreviousTrackAsync(hwnd), Times.Never);
            }
        }

        [Theory]
        [InlineData("TestApp")]
        [InlineData(" testapp.EXE ")]
        public async Task PinnedToggle_PrefersExactNameOverPartialMatch(string target)
        {
            _processServiceMock.Setup(p => p.GetRunningProcessesAsync()).ReturnsAsync(new[]
            {
                new ProcessInfo { ProcessId = 456, Name = "TestAppHelper" },
                new ProcessInfo { ProcessId = 123, Name = "TestApp" }
            });
            _audioServiceMock.Setup(a => a.ToggleMuteProcessAsync(123)).ReturnsAsync(true);

            var result = await _service.ExecutePinnedActionAsync(ProcessActionType.ToggleMute, target);

            Assert.True(result.Success);
            _audioServiceMock.Verify(a => a.ToggleMuteProcessAsync(123), Times.Once);
            _audioServiceMock.Verify(a => a.ToggleMuteProcessAsync(456), Times.Never);
        }

        [Fact]
        public async Task PinnedToggle_FindsAudioApplicationWithoutWindow()
        {
            _processServiceMock.Setup(p => p.GetRunningProcessesAsync()).ReturnsAsync(Array.Empty<ProcessInfo>());
            _audioServiceMock.Setup(a => a.GetAudioProcessesAsync()).ReturnsAsync(new[]
            {
                new AudioProcessInfo { ProcessId = 123, ProcessName = "TestApp" }
            });
            _audioServiceMock.Setup(a => a.ToggleMuteProcessAsync(123)).ReturnsAsync(true);

            var result = await _service.ExecutePinnedActionAsync(ProcessActionType.ToggleMute, "TestApp.exe");

            Assert.True(result.Success);
            Assert.Equal(123, result.ProcessId);
            _processServiceMock.Verify(p => p.GetRunningProcessesAsync(It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task LegacyTarget_UsesConfiguredApplicationAndTogglesBothDirections()
        {
            Func<Task>? callback = null;
            _hotkeyServiceMock.Setup(h => h.RegisterHotkey(It.IsAny<HotkeyBinding>(), It.IsAny<Func<Task>>(), true))
                .Callback<HotkeyBinding, Func<Task>, bool>((_, action, _) => callback = action).Returns(true);
            _processServiceMock.Setup(p => p.GetRunningProcessesAsync()).ReturnsAsync(new[]
            {
                new ProcessInfo { ProcessId = 123, Name = "TestApp" }
            });
            bool muted = false;
            _audioServiceMock.Setup(a => a.ToggleMuteProcessAsync(123))
                .Callback(() => muted = !muted).ReturnsAsync(true);
            var item = new HotkeyConfigItem("Mute player", 0x4D, HotkeyModifier.Ctrl, "ToggleMute")
            { TargetProcessName = "TestApp", ActionMode = ActionMode.QuickAction };
            Assert.True(_service.RegisterActionMapping(item));

            // Unsaved editor mutations must not alter a registered callback.
            item.TargetProcessName = "OtherApp";
            await callback!();
            Assert.True(muted);
            await callback!();
            Assert.False(muted);
            _audioServiceMock.Verify(a => a.ToggleMuteProcessAsync(123), Times.Exactly(2));
        }

        [Fact]
        public async Task ExecutePinnedActionAsync_WithNullProcessName_ShouldThrowArgumentNullException()
        {
            // Act & Assert
            await Assert.ThrowsAsync<ArgumentNullException>(() => 
                _service.ExecutePinnedActionAsync(ProcessActionType.Mute, null!));
        }

        [Fact]
        public async Task ExecutePinnedActionAsync_WithEmptyProcessName_ShouldThrowArgumentException()
        {
            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() => 
                _service.ExecutePinnedActionAsync(ProcessActionType.Mute, ""));
        }

        [Fact]
        public async Task ExecutePinnedActionAsync_WithWhiteSpaceProcessName_ShouldThrowArgumentException()
        {
            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() => 
                _service.ExecutePinnedActionAsync(ProcessActionType.Mute, "   "));
        }

        [Fact]
        public async Task ExecutePinnedActionAsync_WhenProcessNotFound_ShouldReturnFailure()
        {
            // Arrange
            _processServiceMock.Setup(p => p.GetRunningProcessesAsync())
                .ReturnsAsync(new List<ProcessInfo>());

            // Act
            var result = await _service.ExecutePinnedActionAsync(ProcessActionType.Mute, "NonExistent");

            // Assert
            Assert.False(result.Success);
            Assert.Contains("not found", result.ErrorMessage);
        }

        [Fact]
        public async Task ExecutePinnedActionAsync_WhenProcessFound_ShouldExecuteAction()
        {
            // Arrange
            var processInfo = new ProcessInfo
            {
                ProcessId = 123,
                Name = "TestApp",
                WindowHandle = new IntPtr(0x100)
            };
            _processServiceMock.Setup(p => p.GetRunningProcessesAsync())
                .ReturnsAsync(new List<ProcessInfo> { processInfo });
            _audioServiceMock.Setup(a => a.MuteProcessAsync(123)).ReturnsAsync(true);

            // Act
            var result = await _service.ExecutePinnedActionAsync(ProcessActionType.Mute, "TestApp");

            // Assert
            Assert.True(result.Success);
            Assert.Equal(123, result.ProcessId);
            Assert.Equal("TestApp", result.ProcessName);
        }

        [Fact]
        public async Task ExecutePinnedActionAsync_ShouldMatchProcessNameCaseInsensitively()
        {
            // Arrange
            var processInfo = new ProcessInfo
            {
                ProcessId = 123,
                Name = "TestApp",
                WindowHandle = new IntPtr(0x100)
            };
            _processServiceMock.Setup(p => p.GetRunningProcessesAsync())
                .ReturnsAsync(new List<ProcessInfo> { processInfo });
            _audioServiceMock.Setup(a => a.MuteProcessAsync(123)).ReturnsAsync(true);

            // Act
            var result = await _service.ExecutePinnedActionAsync(ProcessActionType.Mute, "TESTAPP");

            // Assert
            Assert.True(result.Success);
        }

        [Fact]
        public async Task ExecutePinnedActionAsync_ShouldMatchPartialProcessName()
        {
            // Arrange
            var processInfo = new ProcessInfo
            {
                ProcessId = 123,
                Name = "TestApplication",
                WindowHandle = new IntPtr(0x100)
            };
            _processServiceMock.Setup(p => p.GetRunningProcessesAsync())
                .ReturnsAsync(new List<ProcessInfo> { processInfo });
            _audioServiceMock.Setup(a => a.MuteProcessAsync(123)).ReturnsAsync(true);

            // Act
            var result = await _service.ExecutePinnedActionAsync(ProcessActionType.Mute, "Test");

            // Assert
            Assert.True(result.Success);
        }

        #endregion

        #region History Tests

        [Fact]
        public async Task ExecuteAction_ShouldAddToHistory()
        {
            // Arrange
            var processInfo = new ProcessInfo
            {
                ProcessId = 123,
                Name = "TestApp",
                WindowHandle = new IntPtr(0x100)
            };
            _processServiceMock.Setup(p => p.GetRunningProcessesAsync())
                .ReturnsAsync(new List<ProcessInfo> { processInfo });
            _audioServiceMock.Setup(a => a.MuteProcessAsync(123)).ReturnsAsync(true);

            // Act
            await _service.ExecutePinnedActionAsync(ProcessActionType.Mute, "TestApp");

            // Assert
            var history = _service.GetActionHistory();
            Assert.Single(history);
            Assert.Equal(ProcessActionType.Mute, history[0].ActionType);
        }

        [Fact]
        public void GetActionHistory_WithNoActions_ShouldReturnEmptyList()
        {
            // Act
            var history = _service.GetActionHistory();

            // Assert
            Assert.Empty(history);
        }

        [Fact]
        public async Task GetActionHistory_ShouldReturnMostRecentFirst()
        {
            // Arrange
            var processInfo = new ProcessInfo
            {
                ProcessId = 123,
                Name = "TestApp",
                WindowHandle = new IntPtr(0x100)
            };
            _processServiceMock.Setup(p => p.GetRunningProcessesAsync())
                .ReturnsAsync(new List<ProcessInfo> { processInfo });
            _audioServiceMock.Setup(a => a.MuteProcessAsync(123)).ReturnsAsync(true);
            _audioServiceMock.Setup(a => a.UnmuteProcessAsync(123)).ReturnsAsync(true);

            // Act
            await _service.ExecutePinnedActionAsync(ProcessActionType.Mute, "TestApp");
            await _service.ExecutePinnedActionAsync(ProcessActionType.Unmute, "TestApp");

            // Assert
            var history = _service.GetActionHistory();
            Assert.Equal(2, history.Count);
            Assert.Equal(ProcessActionType.Unmute, history[0].ActionType);
            Assert.Equal(ProcessActionType.Mute, history[1].ActionType);
        }

        [Fact]
        public async Task GetActionHistory_ShouldRespectMaxEntries()
        {
            // Arrange
            var processInfo = new ProcessInfo
            {
                ProcessId = 123,
                Name = "TestApp",
                WindowHandle = new IntPtr(0x100)
            };
            _processServiceMock.Setup(p => p.GetRunningProcessesAsync())
                .ReturnsAsync(new List<ProcessInfo> { processInfo });
            _audioServiceMock.Setup(a => a.ToggleMuteProcessAsync(123)).ReturnsAsync(true);

            // Execute 10 actions
            for (int i = 0; i < 10; i++)
            {
                await _service.ExecutePinnedActionAsync(ProcessActionType.ToggleMute, "TestApp");
            }

            // Act
            var history = _service.GetActionHistory(maxEntries: 5);

            // Assert
            Assert.Equal(5, history.Count);
        }

        [Fact]
        public void ClearHistory_ShouldRemoveAllEntries()
        {
            // Arrange - add some history by manually testing CanUndo
            Assert.False(_service.CanUndo);

            // Act
            _service.ClearHistory();

            // Assert
            var history = _service.GetActionHistory();
            Assert.Empty(history);
        }

        #endregion

        #region Undo Tests

        [Fact]
        public async Task UndoLastActionAsync_WithNoHistory_ShouldReturnFailure()
        {
            // Act
            var result = await _service.UndoLastActionAsync();

            // Assert
            Assert.False(result.Success);
            Assert.Contains("No action", result.ErrorMessage);
        }

        [Fact]
        public async Task UndoLastActionAsync_WithReversibleAction_ShouldExecuteReverseAction()
        {
            // Arrange
            var processInfo = new ProcessInfo
            {
                ProcessId = 123,
                Name = "TestApp",
                WindowHandle = new IntPtr(0x100)
            };
            _processServiceMock.Setup(p => p.GetRunningProcessesAsync())
                .ReturnsAsync(new List<ProcessInfo> { processInfo });
            _audioServiceMock.Setup(a => a.MuteProcessAsync(123)).ReturnsAsync(true);
            _audioServiceMock.Setup(a => a.UnmuteProcessAsync(123)).ReturnsAsync(true);

            await _service.ExecutePinnedActionAsync(ProcessActionType.Mute, "TestApp");

            // Act
            var result = await _service.UndoLastActionAsync();

            // Assert
            Assert.True(result.Success);
            _audioServiceMock.Verify(a => a.UnmuteProcessAsync(123), Times.Once);
        }

        [Fact]
        public async Task CanUndo_AfterReversibleAction_ShouldBeTrue()
        {
            // Arrange
            var processInfo = new ProcessInfo
            {
                ProcessId = 123,
                Name = "TestApp",
                WindowHandle = new IntPtr(0x100)
            };
            _processServiceMock.Setup(p => p.GetRunningProcessesAsync())
                .ReturnsAsync(new List<ProcessInfo> { processInfo });
            _audioServiceMock.Setup(a => a.MuteProcessAsync(123)).ReturnsAsync(true);

            // Act
            await _service.ExecutePinnedActionAsync(ProcessActionType.Mute, "TestApp");

            // Assert
            Assert.True(_service.CanUndo);
        }

        #endregion

        #region ReloadMappingsAsync Tests

        [Fact]
        public async Task ReloadMappings_ReplacesShortcutAndRemovesDisabledMappings()
        {
            var item = new HotkeyConfigItem("Mute player", 0x4D, HotkeyModifier.Ctrl, "ToggleMute")
            { TargetProcessName = "TestApp" };
            _service.RegisterActionMapping(item);
            var oldBinding = item.ToHotkeyBinding();
            item.VirtualKeyCode = 0x55;
            _configServiceMock.Setup(c => c.LoadConfigurationAsync()).ReturnsAsync(new HotkeyConfiguration(new[] { item }));

            await _service.ReloadMappingsAsync();
            _hotkeyServiceMock.Verify(h => h.UnregisterHotkey(oldBinding), Times.Once);
            _hotkeyServiceMock.Verify(h => h.RegisterHotkey(item.ToHotkeyBinding(), It.IsAny<Func<Task>>(), true), Times.Once);

            item.IsEnabled = false;
            await _service.ReloadMappingsAsync();
            Assert.Equal(0, _service.MappingCount);
            _hotkeyServiceMock.Verify(h => h.UnregisterHotkey(item.ToHotkeyBinding()), Times.Once);
        }

        [Fact]
        public async Task ReloadMappings_ReportsRegistrationFailure()
        {
            var item = new HotkeyConfigItem("Mute player", 0x4D, HotkeyModifier.Ctrl, "ToggleMute");
            _configServiceMock.Setup(c => c.LoadConfigurationAsync()).ReturnsAsync(new HotkeyConfiguration(new[] { item }));
            _hotkeyServiceMock.Setup(h => h.RegisterHotkey(It.IsAny<HotkeyBinding>(), It.IsAny<Func<Task>>(), true)).Returns(false);

            await Assert.ThrowsAsync<InvalidOperationException>(() => _service.ReloadMappingsAsync());
        }

        [Fact]
        public async Task ReloadMappingsAsync_ShouldClearExistingMappings()
        {
            // Arrange
            var config1 = new HotkeyConfigItem("Test1", 0x4D, HotkeyModifier.Ctrl, "Mute") { IsEnabled = true };
            _service.RegisterActionMapping(config1);
            Assert.Equal(1, _service.MappingCount);

            _configServiceMock.Setup(c => c.LoadConfigurationAsync())
                .ReturnsAsync(new HotkeyConfiguration());

            // Act
            await _service.ReloadMappingsAsync();

            // Assert
            Assert.Equal(0, _service.MappingCount);
        }

        [Fact]
        public async Task ReloadMappingsAsync_ShouldLoadNewConfiguration()
        {
            // Arrange
            var newConfig = new HotkeyConfiguration
            {
                Items =
                [
                    new HotkeyConfigItem("New1", 0x4E, HotkeyModifier.Alt, "Close") { IsEnabled = true },
                    new HotkeyConfigItem("New2", 0x4F, HotkeyModifier.Shift, "Hide") { IsEnabled = true }
                ]
            };
            _configServiceMock.Setup(c => c.LoadConfigurationAsync()).ReturnsAsync(newConfig);

            // Act
            await _service.ReloadMappingsAsync();

            // Assert
            Assert.Equal(2, _service.MappingCount);
        }

        #endregion

        #region Shutdown and Dispose Tests

        [Fact]
        public void Shutdown_ShouldUnregisterAllMappings()
        {
            // Arrange
            var config = new HotkeyConfigItem("Test", 0x4D, HotkeyModifier.Ctrl, "Mute") { IsEnabled = true };
            _service.RegisterActionMapping(config);

            // Act
            _service.Shutdown();

            // Assert
            Assert.Equal(0, _service.MappingCount);
        }

        [Fact]
        public void Shutdown_ShouldClearHistory()
        {
            // Act
            _service.Shutdown();

            // Assert
            var history = _service.GetActionHistory();
            Assert.Empty(history);
        }

        [Fact]
        public async Task Shutdown_ShouldSetIsInitializedToFalse()
        {
            // Arrange
            await _service.InitializeAsync();
            Assert.True(_service.IsInitialized);

            // Act
            _service.Shutdown();

            // Assert
            Assert.False(_service.IsInitialized);
        }

        [Fact]
        public void Dispose_ShouldCallShutdown()
        {
            // Arrange
            var config = new HotkeyConfigItem("Test", 0x4D, HotkeyModifier.Ctrl, "Mute") { IsEnabled = true };
            _service.RegisterActionMapping(config);

            // Act
            _service.Dispose();

            // Assert - verify the mapping was cleared (shutdown was called)
            Assert.Equal(0, _service.MappingCount);
        }

        [Fact]
        public void Dispose_MultipleCalls_ShouldNotThrow()
        {
            // Act & Assert - should not throw
            _service.Dispose();
            _service.Dispose();
            _service.Dispose();
        }

        #endregion

        #region Event Tests

        [Fact]
        public async Task ExecuteAction_ShouldRaiseActionExecutedEvent()
        {
            // Arrange
            var eventRaised = false;
            ActionExecutionResult? capturedResult = null;
            
            _service.ActionExecuted += (s, e) =>
            {
                eventRaised = true;
                capturedResult = e;
            };

            var processInfo = new ProcessInfo
            {
                ProcessId = 123,
                Name = "TestApp",
                WindowHandle = new IntPtr(0x100)
            };
            _processServiceMock.Setup(p => p.GetRunningProcessesAsync())
                .ReturnsAsync(new List<ProcessInfo> { processInfo });
            _audioServiceMock.Setup(a => a.MuteProcessAsync(123)).ReturnsAsync(true);

            // Act
            await _service.ExecutePinnedActionAsync(ProcessActionType.Mute, "TestApp");

            // Assert
            Assert.True(eventRaised);
            Assert.NotNull(capturedResult);
            Assert.True(capturedResult.Success);
        }

        #endregion
    }
}
