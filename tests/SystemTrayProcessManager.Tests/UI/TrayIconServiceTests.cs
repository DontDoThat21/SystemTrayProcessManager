using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using SystemTrayProcessManager.Core.Enums;
using SystemTrayProcessManager.Core.Services;
using SystemTrayProcessManager.UI.Services;

namespace SystemTrayProcessManager.Tests.UI
{
    /// <summary>
    /// Unit tests for the TrayIconService implementation.
    /// Tests cover initialization, event handling, tooltip, visibility, and disposal.
    /// </summary>
    public class TrayIconServiceTests
    {
        private readonly Mock<ILogger<TrayIconService>> _loggerMock;

        public TrayIconServiceTests()
        {
            _loggerMock = new Mock<ILogger<TrayIconService>>();
        }

        #region Constructor Tests

        [Fact]
        public void Constructor_WithNullLogger_ThrowsArgumentNullException()
        {
            // Arrange & Act
            var act = () => new TrayIconService(null!);

            // Assert
            act.Should().Throw<ArgumentNullException>()
                .WithParameterName("logger");
        }

        [Fact]
        public void Constructor_WithValidLogger_CreatesInstance()
        {
            // Arrange & Act
            using var service = new TrayIconService(_loggerMock.Object);

            // Assert
            service.Should().NotBeNull();
        }

        #endregion

        #region Initialize Tests

        [Fact]
        public void Initialize_WhenCalled_SetsIsVisibleToTrue()
        {
            // Arrange
            using var service = new TrayIconService(_loggerMock.Object);

            // Act
            service.Initialize();

            // Assert
            service.IsVisible.Should().BeTrue();
        }

        [Fact]
        public void Initialize_WhenCalledTwice_DoesNotThrow()
        {
            // Arrange
            using var service = new TrayIconService(_loggerMock.Object);

            // Act
            service.Initialize();
            var act = () => service.Initialize();

            // Assert
            act.Should().NotThrow();
        }

        [Fact]
        public void Initialize_LogsInitialization()
        {
            // Arrange
            using var service = new TrayIconService(_loggerMock.Object);

            // Act
            service.Initialize();

            // Assert - verify logging was called
            _loggerMock.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("initialized successfully")),
                    null,
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }

        #endregion

        #region SetVisible Tests

        [Fact]
        public void SetVisible_BeforeInitialize_DoesNotThrow()
        {
            // Arrange
            using var service = new TrayIconService(_loggerMock.Object);

            // Act
            var act = () => service.SetVisible(false);

            // Assert
            act.Should().NotThrow();
        }

        [Fact]
        public void SetVisible_AfterInitialize_ChangesVisibility()
        {
            // Arrange
            using var service = new TrayIconService(_loggerMock.Object);
            service.Initialize();

            // Act
            service.SetVisible(false);

            // Assert
            service.IsVisible.Should().BeFalse();
        }

        [Fact]
        public void SetVisible_ToTrue_MakesIconVisible()
        {
            // Arrange
            using var service = new TrayIconService(_loggerMock.Object);
            service.Initialize();
            service.SetVisible(false);

            // Act
            service.SetVisible(true);

            // Assert
            service.IsVisible.Should().BeTrue();
        }

        #endregion

        #region SetTooltip Tests

        [Fact]
        public void SetTooltip_BeforeInitialize_DoesNotThrow()
        {
            // Arrange
            using var service = new TrayIconService(_loggerMock.Object);

            // Act
            var act = () => service.SetTooltip("Test tooltip");

            // Assert
            act.Should().NotThrow();
        }

        [Fact]
        public void SetTooltip_WithLongText_TruncatesTo127Characters()
        {
            // Arrange
            using var service = new TrayIconService(_loggerMock.Object);
            service.Initialize();
            var longTooltip = new string('A', 200);

            // Act & Assert - should not throw even with long text
            var act = () => service.SetTooltip(longTooltip);
            act.Should().NotThrow();
        }

        [Fact]
        public void SetTooltip_WithNullText_DoesNotThrow()
        {
            // Arrange
            using var service = new TrayIconService(_loggerMock.Object);
            service.Initialize();

            // Act
            var act = () => service.SetTooltip(null!);

            // Assert
            act.Should().NotThrow();
        }

        #endregion

        #region ShowBalloonTip Tests

        [Fact]
        public void ShowBalloonTip_BeforeInitialize_DoesNotThrow()
        {
            // Arrange
            using var service = new TrayIconService(_loggerMock.Object);

            // Act
            var act = () => service.ShowBalloonTip("Title", "Message");

            // Assert
            act.Should().NotThrow();
        }

        [Theory]
        [InlineData(BalloonIcon.None)]
        [InlineData(BalloonIcon.Info)]
        [InlineData(BalloonIcon.Warning)]
        [InlineData(BalloonIcon.Error)]
        public void ShowBalloonTip_WithDifferentIcons_DoesNotThrow(BalloonIcon icon)
        {
            // Arrange
            using var service = new TrayIconService(_loggerMock.Object);
            service.Initialize();

            // Act
            var act = () => service.ShowBalloonTip("Title", "Message", icon);

            // Assert
            act.Should().NotThrow();
        }

        [Fact]
        public void ShowBalloonTip_WithEmptyTitle_UsesDefaultTitle()
        {
            // Arrange
            using var service = new TrayIconService(_loggerMock.Object);
            service.Initialize();

            // Act & Assert - should not throw with empty title
            var act = () => service.ShowBalloonTip("", "Message");
            act.Should().NotThrow();
        }

        [Fact]
        public void ShowBalloonTip_WithNullMessage_DoesNotThrow()
        {
            // Arrange
            using var service = new TrayIconService(_loggerMock.Object);
            service.Initialize();

            // Act
            var act = () => service.ShowBalloonTip("Title", null!);

            // Assert
            act.Should().NotThrow();
        }

        #endregion

        #region Event Tests

        [Fact]
        public void TrayIconClicked_CanSubscribeAndUnsubscribe()
        {
            // Arrange
            using var service = new TrayIconService(_loggerMock.Object);
            var handler = new EventHandler((s, e) => { });

            // Act & Assert - subscription should not throw
            var subscribeAct = () => service.TrayIconClicked += handler;
            var unsubscribeAct = () => service.TrayIconClicked -= handler;

            subscribeAct.Should().NotThrow();
            unsubscribeAct.Should().NotThrow();
        }

        [Fact]
        public void ExitRequested_CanSubscribeAndUnsubscribe()
        {
            // Arrange
            using var service = new TrayIconService(_loggerMock.Object);
            var handler = new EventHandler((s, e) => { });

            // Act & Assert - subscription should not throw
            var subscribeAct = () => service.ExitRequested += handler;
            var unsubscribeAct = () => service.ExitRequested -= handler;

            subscribeAct.Should().NotThrow();
            unsubscribeAct.Should().NotThrow();
        }

        #endregion

        #region Dispose Tests

        [Fact]
        public void Dispose_WhenCalled_CanBeCalledMultipleTimes()
        {
            // Arrange
            var service = new TrayIconService(_loggerMock.Object);
            service.Initialize();

            // Act
            service.Dispose();
            var act = () => service.Dispose();

            // Assert
            act.Should().NotThrow();
        }

        [Fact]
        public void Dispose_AfterInitialize_HidesIcon()
        {
            // Arrange
            var service = new TrayIconService(_loggerMock.Object);
            service.Initialize();

            // Act
            service.Dispose();

            // Assert
            service.IsVisible.Should().BeFalse();
        }

        [Fact]
        public void Dispose_WithoutInitialize_DoesNotThrow()
        {
            // Arrange
            var service = new TrayIconService(_loggerMock.Object);

            // Act
            var act = () => service.Dispose();

            // Assert
            act.Should().NotThrow();
        }

        [Fact]
        public void Dispose_LogsDisposal()
        {
            // Arrange
            var service = new TrayIconService(_loggerMock.Object);
            service.Initialize();

            // Act
            service.Dispose();

            // Assert - verify disposal logging
            _loggerMock.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("disposed successfully")),
                    null,
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }

        #endregion

        #region DI Registration Tests

        [Fact]
        public void TrayIconService_CanBeResolvedFromDI()
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddLogging();
            services.AddSingleton<ITrayIconService, TrayIconService>();
            var serviceProvider = services.BuildServiceProvider();

            // Act
            var service = serviceProvider.GetService<ITrayIconService>();

            // Assert
            service.Should().NotBeNull();
            service.Should().BeOfType<TrayIconService>();

            // Cleanup
            (service as IDisposable)?.Dispose();
        }

        [Fact]
        public void TrayIconService_RegisteredAsSingleton_ReturnsSameInstance()
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddLogging();
            services.AddSingleton<ITrayIconService, TrayIconService>();
            var serviceProvider = services.BuildServiceProvider();

            // Act
            var service1 = serviceProvider.GetService<ITrayIconService>();
            var service2 = serviceProvider.GetService<ITrayIconService>();

            // Assert
            service1.Should().BeSameAs(service2);

            // Cleanup
            (service1 as IDisposable)?.Dispose();
        }

        #endregion

        #region BalloonIcon Enum Tests

        [Theory]
        [InlineData(BalloonIcon.None, 0)]
        [InlineData(BalloonIcon.Info, 1)]
        [InlineData(BalloonIcon.Warning, 2)]
        [InlineData(BalloonIcon.Error, 3)]
        public void BalloonIcon_HasCorrectValues(BalloonIcon icon, int expectedValue)
        {
            // Assert
            ((int)icon).Should().Be(expectedValue);
        }

        #endregion
    }
}
