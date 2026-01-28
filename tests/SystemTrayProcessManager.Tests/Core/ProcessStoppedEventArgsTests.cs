using FluentAssertions;
using SystemTrayProcessManager.Core.Models;

namespace SystemTrayProcessManager.Tests.Core
{
    /// <summary>
    /// Unit tests for the ProcessStoppedEventArgs model.
    /// Tests cover initialization and property values.
    /// </summary>
    public class ProcessStoppedEventArgsTests
    {
        #region Constructor Tests

        [Fact]
        public void Constructor_WithValidParameters_SetsProperties()
        {
            // Arrange & Act
            var eventArgs = new ProcessStoppedEventArgs(1234, "TestProcess");

            // Assert
            eventArgs.ProcessId.Should().Be(1234);
            eventArgs.ProcessName.Should().Be("TestProcess");
            eventArgs.StoppedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        }

        [Fact]
        public void Constructor_WithNullProcessName_SetsEmptyString()
        {
            // Arrange & Act
            var eventArgs = new ProcessStoppedEventArgs(1234, null!);

            // Assert
            eventArgs.ProcessName.Should().BeEmpty();
        }

        [Fact]
        public void Constructor_WithZeroProcessId_SetsCorrectly()
        {
            // Arrange & Act
            var eventArgs = new ProcessStoppedEventArgs(0, "TestProcess");

            // Assert
            eventArgs.ProcessId.Should().Be(0);
        }

        [Fact]
        public void Constructor_WithNegativeProcessId_SetsCorrectly()
        {
            // Arrange & Act
            var eventArgs = new ProcessStoppedEventArgs(-1, "TestProcess");

            // Assert
            eventArgs.ProcessId.Should().Be(-1);
        }

        #endregion

        #region Property Tests

        [Fact]
        public void StoppedAt_IsSetAtCreation()
        {
            // Arrange
            var beforeCreation = DateTime.UtcNow;

            // Act
            var eventArgs = new ProcessStoppedEventArgs(1234, "TestProcess");

            // Assert
            eventArgs.StoppedAt.Should().BeOnOrAfter(beforeCreation);
            eventArgs.StoppedAt.Should().BeOnOrBefore(DateTime.UtcNow);
        }

        #endregion

        #region Inheritance Tests

        [Fact]
        public void ProcessStoppedEventArgs_InheritsFromEventArgs()
        {
            // Arrange & Act
            var eventArgs = new ProcessStoppedEventArgs(1234, "TestProcess");

            // Assert
            eventArgs.Should().BeAssignableTo<EventArgs>();
        }

        #endregion
    }
}
