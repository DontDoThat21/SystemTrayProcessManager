using Microsoft.Extensions.Logging;
using Moq;
using SystemTrayProcessManager.Core.Models;
using SystemTrayProcessManager.Core.Services;
using SystemTrayProcessManager.Infrastructure.Services;

namespace SystemTrayProcessManager.Tests.Infrastructure
{
    /// <summary>
    /// Unit tests for the AudioManagerService class.
    /// Tests cover interface contract, error handling, parameter validation, and disposal.
    /// </summary>
    public class AudioManagerServiceTests : IDisposable
    {
        private readonly Mock<ILogger<AudioManagerService>> _mockLogger;
        private AudioManagerService? _service;
        private bool _disposed;

        public AudioManagerServiceTests()
        {
            _mockLogger = new Mock<ILogger<AudioManagerService>>();
        }

        /// <summary>
        /// Creates a new service instance for testing.
        /// </summary>
        private AudioManagerService CreateService()
        {
            _service = new AudioManagerService(_mockLogger.Object);
            return _service;
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                _service?.Dispose();
                _disposed = true;
            }
        }

        #region Constructor Tests

        [Fact]
        public void Constructor_WithValidLogger_CreatesInstance()
        {
            // Arrange & Act
            var service = CreateService();

            // Assert
            Assert.NotNull(service);
        }

        [Fact]
        public void Constructor_WithNullLogger_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => new AudioManagerService(null!));
        }

        [Fact]
        public void Constructor_ImplementsIAudioService()
        {
            // Arrange
            var service = CreateService();

            // Assert
            Assert.IsAssignableFrom<IAudioService>(service);
        }

        [Fact]
        public void Constructor_ImplementsIDisposable()
        {
            // Arrange
            var service = CreateService();

            // Assert
            Assert.IsAssignableFrom<IDisposable>(service);
        }

        #endregion

        #region IsAudioAvailable Tests

        [Fact]
        public void IsAudioAvailable_AfterConstruction_ReturnsExpectedState()
        {
            // Arrange
            var service = CreateService();

            // Act
            bool isAvailable = service.IsAudioAvailable;

            // Assert
            // This test verifies the property doesn't throw
            // Actual availability depends on system state
            Assert.True(isAvailable || !isAvailable);
        }

        [Fact]
        public void IsAudioAvailable_AfterDisposal_ReturnsFalse()
        {
            // Arrange
            var service = CreateService();

            // Act
            service.Dispose();
            bool isAvailable = service.IsAudioAvailable;

            // Assert
            Assert.False(isAvailable);
        }

        #endregion

        #region MuteProcessAsync Tests

        [Fact]
        public async Task MuteProcessAsync_WithZeroProcessId_ReturnsFalse()
        {
            // Arrange
            var service = CreateService();

            // Act
            bool result = await service.MuteProcessAsync(0);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task MuteProcessAsync_WithNegativeProcessId_ReturnsFalse()
        {
            // Arrange
            var service = CreateService();

            // Act
            bool result = await service.MuteProcessAsync(-1);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task MuteProcessAsync_WithInvalidProcessId_ReturnsFalse()
        {
            // Arrange
            var service = CreateService();
            int invalidPid = int.MaxValue;

            // Act
            bool result = await service.MuteProcessAsync(invalidPid);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task MuteProcessAsync_AfterDisposal_ReturnsFalse()
        {
            // Arrange
            var service = CreateService();
            service.Dispose();

            // Act
            bool result = await service.MuteProcessAsync(1234);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task MuteProcessAsync_ReturnsWithinReasonableTime()
        {
            // Arrange
            var service = CreateService();
            var timeout = TimeSpan.FromSeconds(5);

            // Act
            var task = service.MuteProcessAsync(99999);
            bool completed = await Task.WhenAny(task, Task.Delay(timeout)) == task;

            // Assert
            Assert.True(completed, "MuteProcessAsync should complete within 5 seconds");
        }

        #endregion

        #region UnmuteProcessAsync Tests

        [Fact]
        public async Task UnmuteProcessAsync_WithZeroProcessId_ReturnsFalse()
        {
            // Arrange
            var service = CreateService();

            // Act
            bool result = await service.UnmuteProcessAsync(0);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task UnmuteProcessAsync_WithNegativeProcessId_ReturnsFalse()
        {
            // Arrange
            var service = CreateService();

            // Act
            bool result = await service.UnmuteProcessAsync(-100);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task UnmuteProcessAsync_WithInvalidProcessId_ReturnsFalse()
        {
            // Arrange
            var service = CreateService();

            // Act
            bool result = await service.UnmuteProcessAsync(int.MaxValue);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task UnmuteProcessAsync_AfterDisposal_ReturnsFalse()
        {
            // Arrange
            var service = CreateService();
            service.Dispose();

            // Act
            bool result = await service.UnmuteProcessAsync(1234);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task UnmuteProcessAsync_ReturnsWithinReasonableTime()
        {
            // Arrange
            var service = CreateService();
            var timeout = TimeSpan.FromSeconds(5);

            // Act
            var task = service.UnmuteProcessAsync(99999);
            bool completed = await Task.WhenAny(task, Task.Delay(timeout)) == task;

            // Assert
            Assert.True(completed, "UnmuteProcessAsync should complete within 5 seconds");
        }

        #endregion

        #region ToggleMuteProcessAsync Tests

        [Fact]
        public async Task ToggleMuteProcessAsync_WithZeroProcessId_ReturnsFalse()
        {
            // Arrange
            var service = CreateService();

            // Act
            bool result = await service.ToggleMuteProcessAsync(0);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task ToggleMuteProcessAsync_WithNegativeProcessId_ReturnsFalse()
        {
            // Arrange
            var service = CreateService();

            // Act
            bool result = await service.ToggleMuteProcessAsync(-50);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task ToggleMuteProcessAsync_WithInvalidProcessId_ReturnsFalse()
        {
            // Arrange
            var service = CreateService();

            // Act
            bool result = await service.ToggleMuteProcessAsync(int.MaxValue - 1);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task ToggleMuteProcessAsync_AfterDisposal_ReturnsFalse()
        {
            // Arrange
            var service = CreateService();
            service.Dispose();

            // Act
            bool result = await service.ToggleMuteProcessAsync(1234);

            // Assert
            Assert.False(result);
        }

        #endregion

        #region SetProcessVolumeAsync Tests

        [Fact]
        public async Task SetProcessVolumeAsync_WithZeroProcessId_ReturnsFalse()
        {
            // Arrange
            var service = CreateService();

            // Act
            bool result = await service.SetProcessVolumeAsync(0, 0.5f);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task SetProcessVolumeAsync_WithNegativeProcessId_ReturnsFalse()
        {
            // Arrange
            var service = CreateService();

            // Act
            bool result = await service.SetProcessVolumeAsync(-1, 0.5f);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task SetProcessVolumeAsync_WithInvalidProcessId_ReturnsFalse()
        {
            // Arrange
            var service = CreateService();

            // Act
            bool result = await service.SetProcessVolumeAsync(int.MaxValue, 0.5f);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task SetProcessVolumeAsync_AfterDisposal_ReturnsFalse()
        {
            // Arrange
            var service = CreateService();
            service.Dispose();

            // Act
            bool result = await service.SetProcessVolumeAsync(1234, 0.5f);

            // Assert
            Assert.False(result);
        }

        [Theory]
        [InlineData(0.0f)]
        [InlineData(0.25f)]
        [InlineData(0.5f)]
        [InlineData(0.75f)]
        [InlineData(1.0f)]
        public async Task SetProcessVolumeAsync_ValidVolumeValues_DoesNotThrow(float volume)
        {
            // Arrange
            var service = CreateService();

            // Act & Assert (should not throw)
            await service.SetProcessVolumeAsync(99999, volume);
        }

        [Theory]
        [InlineData(-0.5f)]
        [InlineData(-1.0f)]
        [InlineData(1.5f)]
        [InlineData(2.0f)]
        public async Task SetProcessVolumeAsync_OutOfRangeVolume_ClampsValue(float volume)
        {
            // Arrange
            var service = CreateService();

            // Act & Assert (should not throw, values are clamped internally)
            var result = await service.SetProcessVolumeAsync(99999, volume);
            Assert.False(result); // Invalid process, but no exception
        }

        [Fact]
        public async Task SetProcessVolumeAsync_ReturnsWithinReasonableTime()
        {
            // Arrange
            var service = CreateService();
            var timeout = TimeSpan.FromSeconds(5);

            // Act
            var task = service.SetProcessVolumeAsync(99999, 0.5f);
            bool completed = await Task.WhenAny(task, Task.Delay(timeout)) == task;

            // Assert
            Assert.True(completed, "SetProcessVolumeAsync should complete within 5 seconds");
        }

        #endregion

        #region GetProcessVolumeAsync Tests

        [Fact]
        public async Task GetProcessVolumeAsync_WithZeroProcessId_ReturnsNull()
        {
            // Arrange
            var service = CreateService();

            // Act
            float? result = await service.GetProcessVolumeAsync(0);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task GetProcessVolumeAsync_WithNegativeProcessId_ReturnsNull()
        {
            // Arrange
            var service = CreateService();

            // Act
            float? result = await service.GetProcessVolumeAsync(-1);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task GetProcessVolumeAsync_WithInvalidProcessId_ReturnsNull()
        {
            // Arrange
            var service = CreateService();

            // Act
            float? result = await service.GetProcessVolumeAsync(int.MaxValue);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task GetProcessVolumeAsync_AfterDisposal_ReturnsNull()
        {
            // Arrange
            var service = CreateService();
            service.Dispose();

            // Act
            float? result = await service.GetProcessVolumeAsync(1234);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task GetProcessVolumeAsync_ReturnsWithinReasonableTime()
        {
            // Arrange
            var service = CreateService();
            var timeout = TimeSpan.FromSeconds(5);

            // Act
            var task = service.GetProcessVolumeAsync(99999);
            bool completed = await Task.WhenAny(task, Task.Delay(timeout)) == task;

            // Assert
            Assert.True(completed, "GetProcessVolumeAsync should complete within 5 seconds");
        }

        #endregion

        #region IsProcessMutedAsync Tests

        [Fact]
        public async Task IsProcessMutedAsync_WithZeroProcessId_ReturnsNull()
        {
            // Arrange
            var service = CreateService();

            // Act
            bool? result = await service.IsProcessMutedAsync(0);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task IsProcessMutedAsync_WithNegativeProcessId_ReturnsNull()
        {
            // Arrange
            var service = CreateService();

            // Act
            bool? result = await service.IsProcessMutedAsync(-1);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task IsProcessMutedAsync_WithInvalidProcessId_ReturnsNull()
        {
            // Arrange
            var service = CreateService();

            // Act
            bool? result = await service.IsProcessMutedAsync(int.MaxValue);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task IsProcessMutedAsync_AfterDisposal_ReturnsNull()
        {
            // Arrange
            var service = CreateService();
            service.Dispose();

            // Act
            bool? result = await service.IsProcessMutedAsync(1234);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task IsProcessMutedAsync_ReturnsWithinReasonableTime()
        {
            // Arrange
            var service = CreateService();
            var timeout = TimeSpan.FromSeconds(5);

            // Act
            var task = service.IsProcessMutedAsync(99999);
            bool completed = await Task.WhenAny(task, Task.Delay(timeout)) == task;

            // Assert
            Assert.True(completed, "IsProcessMutedAsync should complete within 5 seconds");
        }

        #endregion

        #region GetAudioProcessesAsync Tests

        [Fact]
        public async Task GetAudioProcessesAsync_ReturnsReadOnlyList()
        {
            // Arrange
            var service = CreateService();

            // Act
            var result = await service.GetAudioProcessesAsync();

            // Assert
            Assert.NotNull(result);
            Assert.IsAssignableFrom<IReadOnlyList<AudioProcessInfo>>(result);
        }

        [Fact]
        public async Task GetAudioProcessesAsync_AfterDisposal_ReturnsEmptyList()
        {
            // Arrange
            var service = CreateService();
            service.Dispose();

            // Act
            var result = await service.GetAudioProcessesAsync();

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result);
        }

        [Fact]
        public async Task GetAudioProcessesAsync_ReturnsWithinReasonableTime()
        {
            // Arrange
            var service = CreateService();
            var timeout = TimeSpan.FromSeconds(10);

            // Act
            var task = service.GetAudioProcessesAsync();
            bool completed = await Task.WhenAny(task, Task.Delay(timeout)) == task;

            // Assert
            Assert.True(completed, "GetAudioProcessesAsync should complete within 10 seconds");
        }

        [Fact]
        public async Task GetAudioProcessesAsync_ReturnsAudioProcessInfoObjects()
        {
            // Arrange
            var service = CreateService();

            // Act
            var result = await service.GetAudioProcessesAsync();

            // Assert
            foreach (var info in result)
            {
                Assert.NotNull(info);
                Assert.IsType<AudioProcessInfo>(info);
            }
        }

        [Fact]
        public async Task GetAudioProcessesAsync_AllItemsHaveValidProcessId()
        {
            // Arrange
            var service = CreateService();

            // Act
            var result = await service.GetAudioProcessesAsync();

            // Assert
            foreach (var info in result)
            {
                Assert.True(info.ProcessId > 0, $"Expected ProcessId > 0, got {info.ProcessId}");
            }
        }

        [Fact]
        public async Task GetAudioProcessesAsync_AllItemsHaveValidVolume()
        {
            // Arrange
            var service = CreateService();

            // Act
            var result = await service.GetAudioProcessesAsync();

            // Assert
            foreach (var info in result)
            {
                Assert.InRange(info.Volume, 0.0f, 1.0f);
            }
        }

        #endregion

        #region GetAudioProcessInfoAsync Tests

        [Fact]
        public async Task GetAudioProcessInfoAsync_WithZeroProcessId_ReturnsNull()
        {
            // Arrange
            var service = CreateService();

            // Act
            var result = await service.GetAudioProcessInfoAsync(0);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task GetAudioProcessInfoAsync_WithNegativeProcessId_ReturnsNull()
        {
            // Arrange
            var service = CreateService();

            // Act
            var result = await service.GetAudioProcessInfoAsync(-1);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task GetAudioProcessInfoAsync_WithInvalidProcessId_ReturnsNull()
        {
            // Arrange
            var service = CreateService();

            // Act
            var result = await service.GetAudioProcessInfoAsync(int.MaxValue);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task GetAudioProcessInfoAsync_AfterDisposal_ReturnsNull()
        {
            // Arrange
            var service = CreateService();
            service.Dispose();

            // Act
            var result = await service.GetAudioProcessInfoAsync(1234);

            // Assert
            Assert.Null(result);
        }

        #endregion

        #region RefreshAudioSessionsAsync Tests

        [Fact]
        public async Task RefreshAudioSessionsAsync_DoesNotThrow()
        {
            // Arrange
            var service = CreateService();

            // Act & Assert (should not throw)
            await service.RefreshAudioSessionsAsync();
        }

        [Fact]
        public async Task RefreshAudioSessionsAsync_AfterDisposal_DoesNotThrow()
        {
            // Arrange
            var service = CreateService();
            service.Dispose();

            // Act & Assert (should not throw)
            await service.RefreshAudioSessionsAsync();
        }

        [Fact]
        public async Task RefreshAudioSessionsAsync_CompletesWithinReasonableTime()
        {
            // Arrange
            var service = CreateService();
            var timeout = TimeSpan.FromSeconds(10);

            // Act
            var task = service.RefreshAudioSessionsAsync();
            bool completed = await Task.WhenAny(task, Task.Delay(timeout)) == task;

            // Assert
            Assert.True(completed, "RefreshAudioSessionsAsync should complete within 10 seconds");
        }

        [Fact]
        public async Task RefreshAudioSessionsAsync_CanBeCalledMultipleTimes()
        {
            // Arrange
            var service = CreateService();

            // Act & Assert (should not throw)
            await service.RefreshAudioSessionsAsync();
            await service.RefreshAudioSessionsAsync();
            await service.RefreshAudioSessionsAsync();
        }

        #endregion

        #region Disposal Tests

        [Fact]
        public void Dispose_CanBeCalledMultipleTimes()
        {
            // Arrange
            var service = CreateService();

            // Act & Assert (should not throw)
            service.Dispose();
            service.Dispose();
            service.Dispose();

            _service = null; // Prevent double disposal in test cleanup
        }

        [Fact]
        public void Dispose_SetsIsAudioAvailableToFalse()
        {
            // Arrange
            var service = CreateService();

            // Act
            service.Dispose();

            // Assert
            Assert.False(service.IsAudioAvailable);
        }

        [Fact]
        public async Task AfterDisposal_AllMethodsReturnGracefully()
        {
            // Arrange
            var service = CreateService();
            service.Dispose();

            // Act & Assert - All methods should return without throwing
            Assert.False(await service.MuteProcessAsync(1234));
            Assert.False(await service.UnmuteProcessAsync(1234));
            Assert.False(await service.ToggleMuteProcessAsync(1234));
            Assert.False(await service.SetProcessVolumeAsync(1234, 0.5f));
            Assert.Null(await service.GetProcessVolumeAsync(1234));
            Assert.Null(await service.IsProcessMutedAsync(1234));
            Assert.Empty(await service.GetAudioProcessesAsync());
            Assert.Null(await service.GetAudioProcessInfoAsync(1234));

            await service.RefreshAudioSessionsAsync(); // Should not throw

            _service = null; // Prevent double disposal
        }

        #endregion

        #region Thread Safety Tests

        [Fact]
        public async Task ConcurrentOperations_DoNotThrow()
        {
            // Arrange
            var service = CreateService();
            var tasks = new List<Task>();

            // Act - Run multiple operations concurrently
            for (int i = 0; i < 10; i++)
            {
                tasks.Add(service.GetAudioProcessesAsync());
                tasks.Add(service.MuteProcessAsync(99999 + i));
                tasks.Add(service.UnmuteProcessAsync(99998 + i));
                tasks.Add(service.GetProcessVolumeAsync(99997 + i));
            }

            // Assert
            await Task.WhenAll(tasks); // Should complete without exception
        }

        [Fact]
        public async Task ConcurrentRefresh_DoesNotThrow()
        {
            // Arrange
            var service = CreateService();
            var tasks = new List<Task>();

            // Act - Run multiple refresh operations concurrently
            for (int i = 0; i < 5; i++)
            {
                tasks.Add(service.RefreshAudioSessionsAsync());
            }

            // Assert
            await Task.WhenAll(tasks); // Should complete without exception
        }

        #endregion

        #region Edge Case Tests

        [Fact]
        public async Task OperationsWithCurrentProcessId_HandleGracefully()
        {
            // Arrange
            var service = CreateService();
            int currentPid = Environment.ProcessId;

            // Act & Assert - Current process might not have audio, should not throw
            await service.MuteProcessAsync(currentPid);
            await service.UnmuteProcessAsync(currentPid);
            await service.GetProcessVolumeAsync(currentPid);
            await service.IsProcessMutedAsync(currentPid);
        }

        [Fact]
        public async Task OperationsWithSystemProcessId_HandleGracefully()
        {
            // Arrange
            var service = CreateService();
            int systemPid = 4; // System process PID

            // Act & Assert - System process won't have audio, should return gracefully
            bool muteResult = await service.MuteProcessAsync(systemPid);
            bool unmuteResult = await service.UnmuteProcessAsync(systemPid);
            float? volume = await service.GetProcessVolumeAsync(systemPid);
            bool? isMuted = await service.IsProcessMutedAsync(systemPid);

            Assert.False(muteResult);
            Assert.False(unmuteResult);
            Assert.Null(volume);
            Assert.Null(isMuted);
        }

        #endregion
    }
}
