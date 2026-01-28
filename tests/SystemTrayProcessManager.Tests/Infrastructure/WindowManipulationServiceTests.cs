using Microsoft.Extensions.Logging;
using Moq;
using SystemTrayProcessManager.Core.Enums;
using SystemTrayProcessManager.Core.Services;
using SystemTrayProcessManager.Infrastructure.Services;

namespace SystemTrayProcessManager.Tests.Infrastructure
{
    /// <summary>
    /// Unit tests for the WindowManipulationService class.
    /// Tests cover interface contract, error handling, and parameter validation.
    /// </summary>
    public class WindowManipulationServiceTests
    {
        private readonly Mock<ILogger<WindowManipulationService>> _mockLogger;
        private readonly WindowManipulationService _service;

        public WindowManipulationServiceTests()
        {
            _mockLogger = new Mock<ILogger<WindowManipulationService>>();
            _service = new WindowManipulationService(_mockLogger.Object);
        }

        #region Constructor Tests

        [Fact]
        public void Constructor_WithValidLogger_CreatesInstance()
        {
            // Arrange & Act
            var service = new WindowManipulationService(_mockLogger.Object);

            // Assert
            Assert.NotNull(service);
        }

        [Fact]
        public void Constructor_WithNullLogger_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => new WindowManipulationService(null!));
        }

        [Fact]
        public void Constructor_ImplementsIWindowService()
        {
            // Assert
            Assert.IsAssignableFrom<IWindowService>(_service);
        }

        #endregion

        #region IsValidWindow Tests

        [Fact]
        public void IsValidWindow_WithZeroHandle_ReturnsFalse()
        {
            // Act
            bool result = _service.IsValidWindow(IntPtr.Zero);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void IsValidWindow_WithInvalidHandle_ReturnsFalse()
        {
            // Arrange - use an obviously invalid handle
            IntPtr invalidHandle = new(0x12345678);

            // Act
            bool result = _service.IsValidWindow(invalidHandle);

            // Assert
            Assert.False(result);
        }

        #endregion

        #region BringToFrontAsync Tests

        [Fact]
        public async Task BringToFrontAsync_WithZeroHandle_ReturnsFalse()
        {
            // Act
            bool result = await _service.BringToFrontAsync(IntPtr.Zero);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task BringToFrontAsync_WithInvalidHandle_ReturnsFalse()
        {
            // Arrange
            IntPtr invalidHandle = new(0x99999999);

            // Act
            bool result = await _service.BringToFrontAsync(invalidHandle);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task BringToFrontAsync_ReturnsWithinReasonableTime()
        {
            // Arrange
            IntPtr invalidHandle = new(0x12345678);
            var timeout = TimeSpan.FromSeconds(5);

            // Act
            var task = _service.BringToFrontAsync(invalidHandle);
            bool completed = await Task.WhenAny(task, Task.Delay(timeout)) == task;

            // Assert
            Assert.True(completed, "BringToFrontAsync should complete within 5 seconds");
        }

        #endregion

        #region MinimizeAsync Tests

        [Fact]
        public async Task MinimizeAsync_WithZeroHandle_ReturnsFalse()
        {
            // Act
            bool result = await _service.MinimizeAsync(IntPtr.Zero);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task MinimizeAsync_WithInvalidHandle_ReturnsFalse()
        {
            // Arrange
            IntPtr invalidHandle = new(0x99999999);

            // Act
            bool result = await _service.MinimizeAsync(invalidHandle);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task MinimizeAsync_ReturnsWithinReasonableTime()
        {
            // Arrange
            IntPtr invalidHandle = new(0x12345678);
            var timeout = TimeSpan.FromSeconds(5);

            // Act
            var task = _service.MinimizeAsync(invalidHandle);
            bool completed = await Task.WhenAny(task, Task.Delay(timeout)) == task;

            // Assert
            Assert.True(completed, "MinimizeAsync should complete within 5 seconds");
        }

        #endregion

        #region MaximizeAsync Tests

        [Fact]
        public async Task MaximizeAsync_WithZeroHandle_ReturnsFalse()
        {
            // Act
            bool result = await _service.MaximizeAsync(IntPtr.Zero);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task MaximizeAsync_WithInvalidHandle_ReturnsFalse()
        {
            // Arrange
            IntPtr invalidHandle = new(0x99999999);

            // Act
            bool result = await _service.MaximizeAsync(invalidHandle);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task MaximizeAsync_ReturnsWithinReasonableTime()
        {
            // Arrange
            IntPtr invalidHandle = new(0x12345678);
            var timeout = TimeSpan.FromSeconds(5);

            // Act
            var task = _service.MaximizeAsync(invalidHandle);
            bool completed = await Task.WhenAny(task, Task.Delay(timeout)) == task;

            // Assert
            Assert.True(completed, "MaximizeAsync should complete within 5 seconds");
        }

        #endregion

        #region RestoreAsync Tests

        [Fact]
        public async Task RestoreAsync_WithZeroHandle_ReturnsFalse()
        {
            // Act
            bool result = await _service.RestoreAsync(IntPtr.Zero);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task RestoreAsync_WithInvalidHandle_ReturnsFalse()
        {
            // Arrange
            IntPtr invalidHandle = new(0x99999999);

            // Act
            bool result = await _service.RestoreAsync(invalidHandle);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task RestoreAsync_ReturnsWithinReasonableTime()
        {
            // Arrange
            IntPtr invalidHandle = new(0x12345678);
            var timeout = TimeSpan.FromSeconds(5);

            // Act
            var task = _service.RestoreAsync(invalidHandle);
            bool completed = await Task.WhenAny(task, Task.Delay(timeout)) == task;

            // Assert
            Assert.True(completed, "RestoreAsync should complete within 5 seconds");
        }

        #endregion

        #region CloseAsync Tests

        [Fact]
        public async Task CloseAsync_WithZeroHandle_ReturnsFalse()
        {
            // Act
            bool result = await _service.CloseAsync(IntPtr.Zero);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task CloseAsync_WithInvalidHandle_ReturnsFalse()
        {
            // Arrange
            IntPtr invalidHandle = new(0x99999999);

            // Act
            bool result = await _service.CloseAsync(invalidHandle);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task CloseAsync_WithForceTrue_ReturnsFalseForInvalidHandle()
        {
            // Arrange
            IntPtr invalidHandle = new(0x99999999);

            // Act
            bool result = await _service.CloseAsync(invalidHandle, force: true);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task CloseAsync_WithForceFalse_ReturnsFalseForInvalidHandle()
        {
            // Arrange
            IntPtr invalidHandle = new(0x99999999);

            // Act
            bool result = await _service.CloseAsync(invalidHandle, force: false);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task CloseAsync_ReturnsWithinReasonableTime()
        {
            // Arrange
            IntPtr invalidHandle = new(0x12345678);
            var timeout = TimeSpan.FromSeconds(5);

            // Act
            var task = _service.CloseAsync(invalidHandle);
            bool completed = await Task.WhenAny(task, Task.Delay(timeout)) == task;

            // Assert
            Assert.True(completed, "CloseAsync should complete within 5 seconds");
        }

        #endregion

        #region HideAsync Tests

        [Fact]
        public async Task HideAsync_WithZeroHandle_ReturnsFalse()
        {
            // Act
            bool result = await _service.HideAsync(IntPtr.Zero);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task HideAsync_WithInvalidHandle_ReturnsFalse()
        {
            // Arrange
            IntPtr invalidHandle = new(0x99999999);

            // Act
            bool result = await _service.HideAsync(invalidHandle);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task HideAsync_ReturnsWithinReasonableTime()
        {
            // Arrange
            IntPtr invalidHandle = new(0x12345678);
            var timeout = TimeSpan.FromSeconds(5);

            // Act
            var task = _service.HideAsync(invalidHandle);
            bool completed = await Task.WhenAny(task, Task.Delay(timeout)) == task;

            // Assert
            Assert.True(completed, "HideAsync should complete within 5 seconds");
        }

        #endregion

        #region ShowAsync Tests

        [Fact]
        public async Task ShowAsync_WithZeroHandle_ReturnsFalse()
        {
            // Act
            bool result = await _service.ShowAsync(IntPtr.Zero);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task ShowAsync_WithInvalidHandle_ReturnsFalse()
        {
            // Arrange
            IntPtr invalidHandle = new(0x99999999);

            // Act
            bool result = await _service.ShowAsync(invalidHandle);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task ShowAsync_ReturnsWithinReasonableTime()
        {
            // Arrange
            IntPtr invalidHandle = new(0x12345678);
            var timeout = TimeSpan.FromSeconds(5);

            // Act
            var task = _service.ShowAsync(invalidHandle);
            bool completed = await Task.WhenAny(task, Task.Delay(timeout)) == task;

            // Assert
            Assert.True(completed, "ShowAsync should complete within 5 seconds");
        }

        #endregion

        #region SetTransparencyAsync Tests

        [Fact]
        public async Task SetTransparencyAsync_WithZeroHandle_ReturnsFalse()
        {
            // Act
            bool result = await _service.SetTransparencyAsync(IntPtr.Zero, 128);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task SetTransparencyAsync_WithInvalidHandle_ReturnsFalse()
        {
            // Arrange
            IntPtr invalidHandle = new(0x99999999);

            // Act
            bool result = await _service.SetTransparencyAsync(invalidHandle, 128);

            // Assert
            Assert.False(result);
        }

        [Theory]
        [InlineData(0)]    // Fully transparent
        [InlineData(128)]  // Half transparent
        [InlineData(255)]  // Fully opaque
        public async Task SetTransparencyAsync_AcceptsValidAlphaValues(byte alpha)
        {
            // Arrange
            IntPtr invalidHandle = new(0x99999999);

            // Act - should not throw for any valid byte value
            bool result = await _service.SetTransparencyAsync(invalidHandle, alpha);

            // Assert - returns false for invalid handle, but accepts the alpha value
            Assert.False(result);
        }

        [Fact]
        public async Task SetTransparencyAsync_ReturnsWithinReasonableTime()
        {
            // Arrange
            IntPtr invalidHandle = new(0x12345678);
            var timeout = TimeSpan.FromSeconds(5);

            // Act
            var task = _service.SetTransparencyAsync(invalidHandle, 128);
            bool completed = await Task.WhenAny(task, Task.Delay(timeout)) == task;

            // Assert
            Assert.True(completed, "SetTransparencyAsync should complete within 5 seconds");
        }

        #endregion

        #region SetAlwaysOnTopAsync Tests

        [Fact]
        public async Task SetAlwaysOnTopAsync_WithZeroHandle_ReturnsFalse()
        {
            // Act
            bool result = await _service.SetAlwaysOnTopAsync(IntPtr.Zero, true);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task SetAlwaysOnTopAsync_WithInvalidHandle_ReturnsFalse()
        {
            // Arrange
            IntPtr invalidHandle = new(0x99999999);

            // Act
            bool result = await _service.SetAlwaysOnTopAsync(invalidHandle, true);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task SetAlwaysOnTopAsync_EnabledTrue_ReturnsFalseForInvalidHandle()
        {
            // Arrange
            IntPtr invalidHandle = new(0x99999999);

            // Act
            bool result = await _service.SetAlwaysOnTopAsync(invalidHandle, enabled: true);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task SetAlwaysOnTopAsync_EnabledFalse_ReturnsFalseForInvalidHandle()
        {
            // Arrange
            IntPtr invalidHandle = new(0x99999999);

            // Act
            bool result = await _service.SetAlwaysOnTopAsync(invalidHandle, enabled: false);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task SetAlwaysOnTopAsync_ReturnsWithinReasonableTime()
        {
            // Arrange
            IntPtr invalidHandle = new(0x12345678);
            var timeout = TimeSpan.FromSeconds(5);

            // Act
            var task = _service.SetAlwaysOnTopAsync(invalidHandle, true);
            bool completed = await Task.WhenAny(task, Task.Delay(timeout)) == task;

            // Assert
            Assert.True(completed, "SetAlwaysOnTopAsync should complete within 5 seconds");
        }

        #endregion

        #region GetWindowStateAsync Tests

        [Fact]
        public async Task GetWindowStateAsync_WithZeroHandle_ReturnsInvalid()
        {
            // Act
            var result = await _service.GetWindowStateAsync(IntPtr.Zero);

            // Assert
            Assert.Equal(WindowState.Invalid, result);
        }

        [Fact]
        public async Task GetWindowStateAsync_WithInvalidHandle_ReturnsInvalid()
        {
            // Arrange
            IntPtr invalidHandle = new(0x99999999);

            // Act
            var result = await _service.GetWindowStateAsync(invalidHandle);

            // Assert
            Assert.Equal(WindowState.Invalid, result);
        }

        [Fact]
        public async Task GetWindowStateAsync_ReturnsWindowStateEnum()
        {
            // Arrange
            IntPtr invalidHandle = new(0x99999999);

            // Act
            var result = await _service.GetWindowStateAsync(invalidHandle);

            // Assert
            Assert.IsType<WindowState>(result);
        }

        [Fact]
        public async Task GetWindowStateAsync_ReturnsWithinReasonableTime()
        {
            // Arrange
            IntPtr invalidHandle = new(0x12345678);
            var timeout = TimeSpan.FromSeconds(5);

            // Act
            var task = _service.GetWindowStateAsync(invalidHandle);
            bool completed = await Task.WhenAny(task, Task.Delay(timeout)) == task;

            // Assert
            Assert.True(completed, "GetWindowStateAsync should complete within 5 seconds");
        }

        #endregion

        #region IsWindowVisibleAsync Tests

        [Fact]
        public async Task IsWindowVisibleAsync_WithZeroHandle_ReturnsFalse()
        {
            // Act
            bool result = await _service.IsWindowVisibleAsync(IntPtr.Zero);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task IsWindowVisibleAsync_WithInvalidHandle_ReturnsFalse()
        {
            // Arrange
            IntPtr invalidHandle = new(0x99999999);

            // Act
            bool result = await _service.IsWindowVisibleAsync(invalidHandle);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task IsWindowVisibleAsync_ReturnsWithinReasonableTime()
        {
            // Arrange
            IntPtr invalidHandle = new(0x12345678);
            var timeout = TimeSpan.FromSeconds(5);

            // Act
            var task = _service.IsWindowVisibleAsync(invalidHandle);
            bool completed = await Task.WhenAny(task, Task.Delay(timeout)) == task;

            // Assert
            Assert.True(completed, "IsWindowVisibleAsync should complete within 5 seconds");
        }

        #endregion

        #region IsAlwaysOnTopAsync Tests

        [Fact]
        public async Task IsAlwaysOnTopAsync_WithZeroHandle_ReturnsFalse()
        {
            // Act
            bool result = await _service.IsAlwaysOnTopAsync(IntPtr.Zero);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task IsAlwaysOnTopAsync_WithInvalidHandle_ReturnsFalse()
        {
            // Arrange
            IntPtr invalidHandle = new(0x99999999);

            // Act
            bool result = await _service.IsAlwaysOnTopAsync(invalidHandle);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task IsAlwaysOnTopAsync_ReturnsWithinReasonableTime()
        {
            // Arrange
            IntPtr invalidHandle = new(0x12345678);
            var timeout = TimeSpan.FromSeconds(5);

            // Act
            var task = _service.IsAlwaysOnTopAsync(invalidHandle);
            bool completed = await Task.WhenAny(task, Task.Delay(timeout)) == task;

            // Assert
            Assert.True(completed, "IsAlwaysOnTopAsync should complete within 5 seconds");
        }

        #endregion

        #region GetTransparencyAsync Tests

        [Fact]
        public async Task GetTransparencyAsync_WithZeroHandle_Returns255()
        {
            // Act
            byte result = await _service.GetTransparencyAsync(IntPtr.Zero);

            // Assert
            Assert.Equal(255, result);
        }

        [Fact]
        public async Task GetTransparencyAsync_WithInvalidHandle_Returns255()
        {
            // Arrange
            IntPtr invalidHandle = new(0x99999999);

            // Act
            byte result = await _service.GetTransparencyAsync(invalidHandle);

            // Assert
            Assert.Equal(255, result);
        }

        [Fact]
        public async Task GetTransparencyAsync_ReturnsWithinReasonableTime()
        {
            // Arrange
            IntPtr invalidHandle = new(0x12345678);
            var timeout = TimeSpan.FromSeconds(5);

            // Act
            var task = _service.GetTransparencyAsync(invalidHandle);
            bool completed = await Task.WhenAny(task, Task.Delay(timeout)) == task;

            // Assert
            Assert.True(completed, "GetTransparencyAsync should complete within 5 seconds");
        }

        #endregion

        #region Concurrency Tests

        [Fact]
        public async Task MultipleOperations_CanRunConcurrently()
        {
            // Arrange
            IntPtr invalidHandle = new(0x12345678);

            // Act - run multiple operations concurrently
            var tasks = new List<Task>
            {
                _service.BringToFrontAsync(invalidHandle),
                _service.MinimizeAsync(invalidHandle),
                _service.MaximizeAsync(invalidHandle),
                _service.GetWindowStateAsync(invalidHandle),
                _service.IsWindowVisibleAsync(invalidHandle)
            };

            // Assert - all should complete without exception
            await Task.WhenAll(tasks);
        }

        [Fact]
        public async Task Service_CanBeUsedFromMultipleThreads()
        {
            // Arrange
            IntPtr invalidHandle = new(0x12345678);
            int threadCount = 10;
            var tasks = new List<Task>();

            // Act - call from multiple threads simultaneously
            for (int i = 0; i < threadCount; i++)
            {
                tasks.Add(Task.Run(async () =>
                {
                    await _service.GetWindowStateAsync(invalidHandle);
                    await _service.IsWindowVisibleAsync(invalidHandle);
                }));
            }

            // Assert - all should complete without deadlock or exception
            await Task.WhenAll(tasks);
        }

        #endregion

        #region Edge Case Tests

        [Fact]
        public void IsValidWindow_WithNegativeHandle_ReturnsFalse()
        {
            // Arrange
            IntPtr negativeHandle = new(-1);

            // Act
            bool result = _service.IsValidWindow(negativeHandle);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task Operations_WithMaxIntHandle_ReturnFalse()
        {
            // Arrange
            IntPtr maxHandle = new(int.MaxValue);

            // Act
            bool bringToFront = await _service.BringToFrontAsync(maxHandle);
            bool minimize = await _service.MinimizeAsync(maxHandle);
            bool maximize = await _service.MaximizeAsync(maxHandle);

            // Assert
            Assert.False(bringToFront);
            Assert.False(minimize);
            Assert.False(maximize);
        }

        [Fact]
        public async Task Operations_WithMinIntHandle_ReturnFalse()
        {
            // Arrange
            IntPtr minHandle = new(int.MinValue);

            // Act
            bool bringToFront = await _service.BringToFrontAsync(minHandle);
            bool minimize = await _service.MinimizeAsync(minHandle);

            // Assert
            Assert.False(bringToFront);
            Assert.False(minimize);
        }

        #endregion

        #region Interface Contract Tests

        [Fact]
        public void Interface_DefinesAllExpectedMethods()
        {
            // Arrange
            var interfaceType = typeof(IWindowService);
            var methods = interfaceType.GetMethods();

            // Act & Assert - verify all expected methods exist
            Assert.Contains(methods, m => m.Name == "BringToFrontAsync");
            Assert.Contains(methods, m => m.Name == "MinimizeAsync");
            Assert.Contains(methods, m => m.Name == "MaximizeAsync");
            Assert.Contains(methods, m => m.Name == "RestoreAsync");
            Assert.Contains(methods, m => m.Name == "CloseAsync");
            Assert.Contains(methods, m => m.Name == "HideAsync");
            Assert.Contains(methods, m => m.Name == "ShowAsync");
            Assert.Contains(methods, m => m.Name == "SetTransparencyAsync");
            Assert.Contains(methods, m => m.Name == "SetAlwaysOnTopAsync");
            Assert.Contains(methods, m => m.Name == "GetWindowStateAsync");
            Assert.Contains(methods, m => m.Name == "IsWindowVisibleAsync");
            Assert.Contains(methods, m => m.Name == "IsAlwaysOnTopAsync");
            Assert.Contains(methods, m => m.Name == "GetTransparencyAsync");
            Assert.Contains(methods, m => m.Name == "IsValidWindow");
        }

        [Fact]
        public void Service_ImplementsAllInterfaceMethods()
        {
            // Arrange
            var serviceType = typeof(WindowManipulationService);
            var interfaceType = typeof(IWindowService);

            // Act & Assert
            Assert.True(interfaceType.IsAssignableFrom(serviceType));
        }

        #endregion
    }
}
