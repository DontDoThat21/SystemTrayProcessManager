using FluentAssertions;
using SystemTrayProcessManager.Core.Models;

namespace SystemTrayProcessManager.Tests.Core
{
    /// <summary>
    /// Unit tests for the ProcessInfo model.
    /// Tests cover property initialization, equality, and display name formatting.
    /// </summary>
    public class ProcessInfoTests
    {
        #region Property Initialization Tests

        [Fact]
        public void ProcessInfo_DefaultValues_AreCorrect()
        {
            // Arrange & Act
            var processInfo = new ProcessInfo();

            // Assert
            processInfo.ProcessId.Should().Be(0);
            processInfo.Name.Should().BeEmpty();
            processInfo.WindowTitle.Should().BeNull();
            processInfo.ExecutablePath.Should().BeNull();
            processInfo.WindowHandle.Should().Be(IntPtr.Zero);
            processInfo.Icon.Should().BeNull();
            processInfo.StartTime.Should().Be(DateTime.MinValue);
            processInfo.IsResponding.Should().BeFalse();
        }

        [Fact]
        public void ProcessInfo_CanSetAllProperties()
        {
            // Arrange
            var startTime = DateTime.Now;
            var handle = new IntPtr(12345);

            // Act
            var processInfo = new ProcessInfo
            {
                ProcessId = 1234,
                Name = "TestProcess",
                WindowTitle = "Test Window",
                ExecutablePath = @"C:\Test\test.exe",
                WindowHandle = handle,
                StartTime = startTime,
                IsResponding = true
            };

            // Assert
            processInfo.ProcessId.Should().Be(1234);
            processInfo.Name.Should().Be("TestProcess");
            processInfo.WindowTitle.Should().Be("Test Window");
            processInfo.ExecutablePath.Should().Be(@"C:\Test\test.exe");
            processInfo.WindowHandle.Should().Be(handle);
            processInfo.StartTime.Should().Be(startTime);
            processInfo.IsResponding.Should().BeTrue();
        }

        #endregion

        #region DisplayName Tests

        [Fact]
        public void DisplayName_WithWindowTitle_CombinesNameAndTitle()
        {
            // Arrange
            var processInfo = new ProcessInfo
            {
                Name = "notepad",
                WindowTitle = "Untitled - Notepad"
            };

            // Act & Assert
            processInfo.DisplayName.Should().Be("notepad - Untitled - Notepad");
        }

        [Fact]
        public void DisplayName_WithNullWindowTitle_ReturnsNameOnly()
        {
            // Arrange
            var processInfo = new ProcessInfo
            {
                Name = "notepad",
                WindowTitle = null
            };

            // Act & Assert
            processInfo.DisplayName.Should().Be("notepad");
        }

        [Fact]
        public void DisplayName_WithEmptyWindowTitle_ReturnsNameOnly()
        {
            // Arrange
            var processInfo = new ProcessInfo
            {
                Name = "notepad",
                WindowTitle = ""
            };

            // Act & Assert
            processInfo.DisplayName.Should().Be("notepad");
        }

        [Fact]
        public void DisplayName_WithWhitespaceWindowTitle_ReturnsNameOnly()
        {
            // Arrange
            var processInfo = new ProcessInfo
            {
                Name = "notepad",
                WindowTitle = "   "
            };

            // Act & Assert
            processInfo.DisplayName.Should().Be("notepad");
        }

        #endregion

        #region Equality Tests

        [Fact]
        public void Equals_SameProcessId_ReturnsTrue()
        {
            // Arrange
            var process1 = new ProcessInfo { ProcessId = 1234, Name = "Test1" };
            var process2 = new ProcessInfo { ProcessId = 1234, Name = "Test2" };

            // Act & Assert
            process1.Equals(process2).Should().BeTrue();
        }

        [Fact]
        public void Equals_DifferentProcessId_ReturnsFalse()
        {
            // Arrange
            var process1 = new ProcessInfo { ProcessId = 1234, Name = "Test" };
            var process2 = new ProcessInfo { ProcessId = 5678, Name = "Test" };

            // Act & Assert
            process1.Equals(process2).Should().BeFalse();
        }

        [Fact]
        public void Equals_WithNull_ReturnsFalse()
        {
            // Arrange
            var process = new ProcessInfo { ProcessId = 1234 };

            // Act & Assert
            process.Equals(null).Should().BeFalse();
        }

        [Fact]
        public void Equals_ObjectOverload_WorksCorrectly()
        {
            // Arrange
            var process1 = new ProcessInfo { ProcessId = 1234 };
            object process2 = new ProcessInfo { ProcessId = 1234 };
            object notAProcess = "not a process";

            // Act & Assert
            process1.Equals(process2).Should().BeTrue();
            process1.Equals(notAProcess).Should().BeFalse();
        }

        [Fact]
        public void EqualityOperator_SameProcessId_ReturnsTrue()
        {
            // Arrange
            var process1 = new ProcessInfo { ProcessId = 1234 };
            var process2 = new ProcessInfo { ProcessId = 1234 };

            // Act & Assert
            (process1 == process2).Should().BeTrue();
        }

        [Fact]
        public void EqualityOperator_DifferentProcessId_ReturnsFalse()
        {
            // Arrange
            var process1 = new ProcessInfo { ProcessId = 1234 };
            var process2 = new ProcessInfo { ProcessId = 5678 };

            // Act & Assert
            (process1 == process2).Should().BeFalse();
        }

        [Fact]
        public void EqualityOperator_BothNull_ReturnsTrue()
        {
            // Arrange
            ProcessInfo? process1 = null;
            ProcessInfo? process2 = null;

            // Act & Assert
            (process1 == process2).Should().BeTrue();
        }

        [Fact]
        public void EqualityOperator_OneNull_ReturnsFalse()
        {
            // Arrange
            var process1 = new ProcessInfo { ProcessId = 1234 };
            ProcessInfo? process2 = null;

            // Act & Assert
            (process1 == process2).Should().BeFalse();
            (process2 == process1).Should().BeFalse();
        }

        [Fact]
        public void InequalityOperator_DifferentProcessId_ReturnsTrue()
        {
            // Arrange
            var process1 = new ProcessInfo { ProcessId = 1234 };
            var process2 = new ProcessInfo { ProcessId = 5678 };

            // Act & Assert
            (process1 != process2).Should().BeTrue();
        }

        #endregion

        #region GetHashCode Tests

        [Fact]
        public void GetHashCode_SameProcessId_ReturnsSameHash()
        {
            // Arrange
            var process1 = new ProcessInfo { ProcessId = 1234, Name = "Test1" };
            var process2 = new ProcessInfo { ProcessId = 1234, Name = "Test2" };

            // Act & Assert
            process1.GetHashCode().Should().Be(process2.GetHashCode());
        }

        [Fact]
        public void GetHashCode_DifferentProcessId_ReturnsDifferentHash()
        {
            // Arrange
            var process1 = new ProcessInfo { ProcessId = 1234 };
            var process2 = new ProcessInfo { ProcessId = 5678 };

            // Act & Assert
            process1.GetHashCode().Should().NotBe(process2.GetHashCode());
        }

        #endregion

        #region ToString Tests

        [Fact]
        public void ToString_ReturnsFormattedString()
        {
            // Arrange
            var processInfo = new ProcessInfo
            {
                ProcessId = 1234,
                Name = "TestProcess",
                WindowTitle = "Test Window"
            };

            // Act
            var result = processInfo.ToString();

            // Assert
            result.Should().Contain("PID=1234");
            result.Should().Contain("Name=TestProcess");
            result.Should().Contain("WindowTitle=Test Window");
        }

        [Fact]
        public void ToString_WithNullWindowTitle_ShowsNone()
        {
            // Arrange
            var processInfo = new ProcessInfo
            {
                ProcessId = 1234,
                Name = "TestProcess",
                WindowTitle = null
            };

            // Act
            var result = processInfo.ToString();

            // Assert
            result.Should().Contain("(none)");
        }

        #endregion
    }
}
