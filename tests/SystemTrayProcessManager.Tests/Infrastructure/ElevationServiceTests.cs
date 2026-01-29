using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using SystemTrayProcessManager.Infrastructure.Services;

namespace SystemTrayProcessManager.Tests.Infrastructure
{
    /// <summary>
    /// Unit tests for <see cref="ElevationService"/>.
    /// </summary>
    public class ElevationServiceTests
    {
        private readonly Mock<ILogger<ElevationService>> _mockLogger;
        private readonly ElevationService _service;

        public ElevationServiceTests()
        {
            _mockLogger = new Mock<ILogger<ElevationService>>();
            _service = new ElevationService(_mockLogger.Object);
        }

        // Constructor tests

        [Fact]
        public void Constructor_WithNullLogger_ShouldThrow()
        {
            var act = () => new ElevationService(null!);
            act.Should().Throw<ArgumentNullException>();
        }

        // IsRunningAsAdministrator tests

        [Fact]
        public void IsRunningAsAdministrator_ShouldReturnBoolean()
        {
            // Just verify it doesn't throw - actual value depends on runtime context
            var result = _service.IsRunningAsAdministrator;
            result.Should().Be(_service.IsRunningAsAdministrator, "should be cached and consistent");
        }

        [Fact]
        public void IsRunningAsAdministrator_ShouldBeCached()
        {
            var first = _service.IsRunningAsAdministrator;
            var second = _service.IsRunningAsAdministrator;

            first.Should().Be(second);
        }

        // IsElevationRequired tests

        [Fact]
        public void IsElevationRequired_WithInvalidProcessId_ShouldReturnFalse()
        {
            _service.IsElevationRequired(-1).Should().BeFalse();
            _service.IsElevationRequired(0).Should().BeFalse();
        }

        [Fact]
        public void IsElevationRequired_WithSystemProcess4_ShouldReturnTrue()
        {
            // Process ID 4 is the System process, which always requires elevation
            _service.IsElevationRequired(4).Should().BeTrue();
        }

        [Fact]
        public void IsElevationRequired_WithNonexistentProcess_ShouldReturnFalse()
        {
            // Use a very high PID that is unlikely to exist
            _service.IsElevationRequired(99999999).Should().BeFalse();
        }

        [Fact]
        public void IsElevationRequired_WithCurrentProcess_ShouldReturnFalse()
        {
            var currentPid = System.Diagnostics.Process.GetCurrentProcess().Id;
            _service.IsElevationRequired(currentPid).Should().BeFalse();
        }

        [Fact]
        public void IsElevationRequired_WithProcessId1_ShouldHandleGracefully()
        {
            // Process ID 1 doesn't exist on Windows, so this should return false
            // (handled via ArgumentException catch)
            _service.IsElevationRequired(1).Should().BeFalse();
        }

        // RequestElevation tests - limited since we can't actually trigger UAC in tests

        [Fact]
        public void RequestElevation_ShouldNotThrow()
        {
            // We can't fully test UAC in unit tests, but we can verify it doesn't crash
            // Note: This will likely return false in a test environment
            var act = () => _service.RequestElevation();
            act.Should().NotThrow();
        }
    }
}
