using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using SystemTrayProcessManager.Core.Models;
using SystemTrayProcessManager.Core.Services;
using SystemTrayProcessManager.Infrastructure.Services;

namespace SystemTrayProcessManager.Tests.Infrastructure
{
    /// <summary>
    /// Unit tests for the ProcessMonitorService implementation.
    /// Tests cover process enumeration, filtering, search, monitoring, and lifecycle management.
    /// </summary>
    public class ProcessMonitorServiceTests
    {
        private readonly Mock<ILogger<ProcessMonitorService>> _loggerMock;
        private readonly Mock<IIconExtractor> _iconExtractorMock;

        public ProcessMonitorServiceTests()
        {
            _loggerMock = new Mock<ILogger<ProcessMonitorService>>();
            _iconExtractorMock = new Mock<IIconExtractor>();
        }

        #region Constructor Tests

        [Fact]
        public void Constructor_WithNullLogger_ThrowsArgumentNullException()
        {
            // Arrange & Act
            var act = () => new ProcessMonitorService(null!, _iconExtractorMock.Object);

            // Assert
            act.Should().Throw<ArgumentNullException>()
                .WithParameterName("logger");
        }

        [Fact]
        public void Constructor_WithNullIconExtractor_ThrowsArgumentNullException()
        {
            // Arrange & Act
            var act = () => new ProcessMonitorService(_loggerMock.Object, null!);

            // Assert
            act.Should().Throw<ArgumentNullException>()
                .WithParameterName("iconExtractor");
        }

        [Fact]
        public void Constructor_WithValidParameters_CreatesInstance()
        {
            // Arrange & Act
            using var service = new ProcessMonitorService(_loggerMock.Object, _iconExtractorMock.Object);

            // Assert
            service.Should().NotBeNull();
            service.IsMonitoring.Should().BeFalse();
        }

        #endregion

        #region GetRunningProcessesAsync Tests

        [Fact]
        public async Task GetRunningProcessesAsync_ReturnsProcesses()
        {
            // Arrange
            using var service = new ProcessMonitorService(_loggerMock.Object, _iconExtractorMock.Object);

            // Act
            var processes = await service.GetRunningProcessesAsync();

            // Assert
            processes.Should().NotBeNull();
            // There should be at least some windowed processes running
        }

        [Fact]
        public async Task GetRunningProcessesAsync_ExcludesCurrentProcess()
        {
            // Arrange
            using var service = new ProcessMonitorService(_loggerMock.Object, _iconExtractorMock.Object);
            var currentPid = Environment.ProcessId;

            // Act
            var processes = await service.GetRunningProcessesAsync();

            // Assert
            processes.Should().NotContain(p => p.ProcessId == currentPid);
        }

        [Fact]
        public async Task GetRunningProcessesAsync_ReturnsOrderedByName()
        {
            // Arrange
            using var service = new ProcessMonitorService(_loggerMock.Object, _iconExtractorMock.Object);

            // Act
            var processes = await service.GetRunningProcessesAsync();
            var processList = processes.ToList();

            // Assert
            if (processList.Count > 1)
            {
                // OrderBy uses ordinal comparison by default, so we use case-insensitive comparer
                var sortedNames = processList.Select(p => p.Name).ToList();
                sortedNames.Should().BeInAscendingOrder(StringComparer.OrdinalIgnoreCase);
            }
        }

        [Fact]
        public async Task GetRunningProcessesAsync_WithCancellation_ThrowsOperationCanceledException()
        {
            // Arrange
            using var service = new ProcessMonitorService(_loggerMock.Object, _iconExtractorMock.Object);
            using var cts = new CancellationTokenSource();
            cts.Cancel();

            // Act & Assert - TaskCanceledException inherits from OperationCanceledException
            await Assert.ThrowsAnyAsync<OperationCanceledException>(
                () => service.GetRunningProcessesAsync(cts.Token));
        }

        [Fact]
        public async Task GetRunningProcessesAsync_AfterDispose_ReturnsEmptyList()
        {
            // Arrange
            var service = new ProcessMonitorService(_loggerMock.Object, _iconExtractorMock.Object);
            service.Dispose();

            // Act
            var processes = await service.GetRunningProcessesAsync();

            // Assert
            processes.Should().BeEmpty();
        }

        #endregion

        #region GetProcessByIdAsync Tests

        [Fact]
        public async Task GetProcessByIdAsync_WithInvalidId_ReturnsNull()
        {
            // Arrange
            using var service = new ProcessMonitorService(_loggerMock.Object, _iconExtractorMock.Object);

            // Act
            var result = await service.GetProcessByIdAsync(-1);

            // Assert
            result.Should().BeNull();
        }

        [Fact]
        public async Task GetProcessByIdAsync_AfterDispose_ReturnsNull()
        {
            // Arrange
            var service = new ProcessMonitorService(_loggerMock.Object, _iconExtractorMock.Object);
            service.Dispose();

            // Act
            var result = await service.GetProcessByIdAsync(1234);

            // Assert
            result.Should().BeNull();
        }

        #endregion

        #region SearchProcessesAsync Tests

        [Fact]
        public async Task SearchProcessesAsync_WithEmptyTerm_ReturnsAllProcesses()
        {
            // Arrange
            using var service = new ProcessMonitorService(_loggerMock.Object, _iconExtractorMock.Object);

            // Act
            var allProcesses = await service.GetRunningProcessesAsync();
            var searchResult = await service.SearchProcessesAsync("");

            // Assert
            searchResult.Count.Should().Be(allProcesses.Count);
        }

        [Fact]
        public async Task SearchProcessesAsync_WithNullTerm_ReturnsAllProcesses()
        {
            // Arrange
            using var service = new ProcessMonitorService(_loggerMock.Object, _iconExtractorMock.Object);

            // Act
            var allProcesses = await service.GetRunningProcessesAsync();
            var searchResult = await service.SearchProcessesAsync(null!);

            // Assert
            searchResult.Count.Should().Be(allProcesses.Count);
        }

        [Fact]
        public async Task SearchProcessesAsync_WithValidTerm_FiltersResults()
        {
            // Arrange
            using var service = new ProcessMonitorService(_loggerMock.Object, _iconExtractorMock.Object);
            var allProcesses = await service.GetRunningProcessesAsync();
            
            if (allProcesses.Count == 0)
            {
                return; // Skip if no processes available
            }

            var firstProcessName = allProcesses.First().Name;

            // Act
            var searchResult = await service.SearchProcessesAsync(firstProcessName);

            // Assert
            searchResult.Should().NotBeEmpty();
            searchResult.All(p => 
                p.Name.Contains(firstProcessName, StringComparison.OrdinalIgnoreCase) ||
                (p.WindowTitle?.Contains(firstProcessName, StringComparison.OrdinalIgnoreCase) ?? false))
                .Should().BeTrue();
        }

        [Fact]
        public async Task SearchProcessesAsync_CaseInsensitive()
        {
            // Arrange
            using var service = new ProcessMonitorService(_loggerMock.Object, _iconExtractorMock.Object);
            var allProcesses = await service.GetRunningProcessesAsync();
            
            if (allProcesses.Count == 0)
            {
                return; // Skip if no processes available
            }

            var firstProcessName = allProcesses.First().Name;

            // Act
            var lowerResult = await service.SearchProcessesAsync(firstProcessName.ToLower());
            var upperResult = await service.SearchProcessesAsync(firstProcessName.ToUpper());

            // Assert
            lowerResult.Count.Should().Be(upperResult.Count);
        }

        [Fact]
        public async Task SearchProcessesAsync_AfterDispose_ReturnsEmptyList()
        {
            // Arrange
            var service = new ProcessMonitorService(_loggerMock.Object, _iconExtractorMock.Object);
            service.Dispose();

            // Act
            var result = await service.SearchProcessesAsync("test");

            // Assert
            result.Should().BeEmpty();
        }

        #endregion

        #region StartMonitoring Tests

        [Fact]
        public void StartMonitoring_SetsIsMonitoringToTrue()
        {
            // Arrange
            using var service = new ProcessMonitorService(_loggerMock.Object, _iconExtractorMock.Object);

            // Act
            service.StartMonitoring();

            // Assert
            service.IsMonitoring.Should().BeTrue();
        }

        [Fact]
        public void StartMonitoring_CalledTwice_DoesNotThrow()
        {
            // Arrange
            using var service = new ProcessMonitorService(_loggerMock.Object, _iconExtractorMock.Object);

            // Act
            service.StartMonitoring();
            var act = () => service.StartMonitoring();

            // Assert
            act.Should().NotThrow();
        }

        [Fact]
        public void StartMonitoring_AfterDispose_DoesNotThrow()
        {
            // Arrange
            var service = new ProcessMonitorService(_loggerMock.Object, _iconExtractorMock.Object);
            service.Dispose();

            // Act
            var act = () => service.StartMonitoring();

            // Assert
            act.Should().NotThrow();
        }

        #endregion

        #region StopMonitoring Tests

        [Fact]
        public void StopMonitoring_AfterStart_SetsIsMonitoringToFalse()
        {
            // Arrange
            using var service = new ProcessMonitorService(_loggerMock.Object, _iconExtractorMock.Object);
            service.StartMonitoring();

            // Act
            service.StopMonitoring();

            // Assert
            service.IsMonitoring.Should().BeFalse();
        }

        [Fact]
        public void StopMonitoring_WithoutStart_DoesNotThrow()
        {
            // Arrange
            using var service = new ProcessMonitorService(_loggerMock.Object, _iconExtractorMock.Object);

            // Act
            var act = () => service.StopMonitoring();

            // Assert
            act.Should().NotThrow();
        }

        [Fact]
        public void StopMonitoring_CalledTwice_DoesNotThrow()
        {
            // Arrange
            using var service = new ProcessMonitorService(_loggerMock.Object, _iconExtractorMock.Object);
            service.StartMonitoring();

            // Act
            service.StopMonitoring();
            var act = () => service.StopMonitoring();

            // Assert
            act.Should().NotThrow();
        }

        #endregion

        #region Event Tests

        [Fact]
        public void ProcessStarted_CanSubscribeAndUnsubscribe()
        {
            // Arrange
            using var service = new ProcessMonitorService(_loggerMock.Object, _iconExtractorMock.Object);
            var handler = new EventHandler<ProcessInfo>((s, e) => { });

            // Act & Assert
            var subscribeAct = () => service.ProcessStarted += handler;
            var unsubscribeAct = () => service.ProcessStarted -= handler;

            subscribeAct.Should().NotThrow();
            unsubscribeAct.Should().NotThrow();
        }

        [Fact]
        public void ProcessStopped_CanSubscribeAndUnsubscribe()
        {
            // Arrange
            using var service = new ProcessMonitorService(_loggerMock.Object, _iconExtractorMock.Object);
            var handler = new EventHandler<ProcessStoppedEventArgs>((s, e) => { });

            // Act & Assert
            var subscribeAct = () => service.ProcessStopped += handler;
            var unsubscribeAct = () => service.ProcessStopped -= handler;

            subscribeAct.Should().NotThrow();
            unsubscribeAct.Should().NotThrow();
        }

        [Fact]
        public void ProcessListChanged_CanSubscribeAndUnsubscribe()
        {
            // Arrange
            using var service = new ProcessMonitorService(_loggerMock.Object, _iconExtractorMock.Object);
            var handler = new EventHandler<IReadOnlyList<ProcessInfo>>((s, e) => { });

            // Act & Assert
            var subscribeAct = () => service.ProcessListChanged += handler;
            var unsubscribeAct = () => service.ProcessListChanged -= handler;

            subscribeAct.Should().NotThrow();
            unsubscribeAct.Should().NotThrow();
        }

        #endregion

        #region Dispose Tests

        [Fact]
        public void Dispose_CanBeCalledMultipleTimes()
        {
            // Arrange
            var service = new ProcessMonitorService(_loggerMock.Object, _iconExtractorMock.Object);

            // Act
            service.Dispose();
            var act = () => service.Dispose();

            // Assert
            act.Should().NotThrow();
        }

        [Fact]
        public void Dispose_StopsMonitoring()
        {
            // Arrange
            var service = new ProcessMonitorService(_loggerMock.Object, _iconExtractorMock.Object);
            service.StartMonitoring();

            // Act
            service.Dispose();

            // Assert
            service.IsMonitoring.Should().BeFalse();
        }

        #endregion
    }
}
