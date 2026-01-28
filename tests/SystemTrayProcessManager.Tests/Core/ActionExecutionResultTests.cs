using SystemTrayProcessManager.Core.Enums;
using SystemTrayProcessManager.Core.Models;

namespace SystemTrayProcessManager.Tests.Core
{
    /// <summary>
    /// Unit tests for the <see cref="ActionExecutionResult"/> class.
    /// </summary>
    public class ActionExecutionResultTests
    {
        [Fact]
        public void CreateSuccess_ShouldSetAllProperties()
        {
            // Arrange
            var actionType = ProcessActionType.Mute;
            var processId = 12345;
            var processName = "TestProcess";
            var windowHandle = new IntPtr(0x12345);
            var historyEntry = ActionHistoryEntry.Create(actionType, ActionMode.QuickAction, processId, processName, windowHandle, true);

            // Act
            var result = ActionExecutionResult.CreateSuccess(actionType, processId, processName, windowHandle, historyEntry);

            // Assert
            Assert.True(result.Success);
            Assert.Equal(actionType, result.ActionType);
            Assert.Equal(processId, result.ProcessId);
            Assert.Equal(processName, result.ProcessName);
            Assert.Equal(windowHandle, result.WindowHandle);
            Assert.Null(result.ErrorMessage);
            Assert.NotNull(result.HistoryEntry);
        }

        [Fact]
        public void CreateSuccess_WithoutHistoryEntry_ShouldHaveNullHistoryEntry()
        {
            // Act
            var result = ActionExecutionResult.CreateSuccess(
                ProcessActionType.Mute, 123, "Test", IntPtr.Zero);

            // Assert
            Assert.True(result.Success);
            Assert.Null(result.HistoryEntry);
        }

        [Fact]
        public void CreateFailure_ShouldSetAllProperties()
        {
            // Arrange
            var actionType = ProcessActionType.Close;
            var errorMessage = "Operation failed";
            var processId = 999;
            var processName = "FailedProcess";

            // Act
            var result = ActionExecutionResult.CreateFailure(actionType, errorMessage, processId, processName);

            // Assert
            Assert.False(result.Success);
            Assert.Equal(actionType, result.ActionType);
            Assert.Equal(processId, result.ProcessId);
            Assert.Equal(processName, result.ProcessName);
            Assert.Null(result.WindowHandle);
            Assert.Equal(errorMessage, result.ErrorMessage);
            Assert.Null(result.HistoryEntry);
        }

        [Fact]
        public void CreateFailure_WithMinimalParameters_ShouldWork()
        {
            // Act
            var result = ActionExecutionResult.CreateFailure(ProcessActionType.Hide, "Error occurred");

            // Assert
            Assert.False(result.Success);
            Assert.Equal(ProcessActionType.Hide, result.ActionType);
            Assert.Null(result.ProcessId);
            Assert.Null(result.ProcessName);
            Assert.Equal("Error occurred", result.ErrorMessage);
        }

        [Fact]
        public void CreateProcessNotFound_WithProcessName_ShouldIncludeNameInMessage()
        {
            // Arrange
            var processName = "MissingApp";

            // Act
            var result = ActionExecutionResult.CreateProcessNotFound(ProcessActionType.BringToFront, processName);

            // Assert
            Assert.False(result.Success);
            Assert.Equal(ProcessActionType.BringToFront, result.ActionType);
            Assert.Equal(processName, result.ProcessName);
            Assert.Contains(processName, result.ErrorMessage);
            Assert.Contains("not found", result.ErrorMessage);
        }

        [Fact]
        public void CreateProcessNotFound_WithNullProcessName_ShouldUseGenericMessage()
        {
            // Act
            var result = ActionExecutionResult.CreateProcessNotFound(ProcessActionType.Minimize, null);

            // Assert
            Assert.False(result.Success);
            Assert.Null(result.ProcessName);
            Assert.Contains("Target process not found", result.ErrorMessage);
        }

        [Fact]
        public void CreateNoForegroundWindow_ShouldReturnCorrectResult()
        {
            // Arrange
            var actionType = ProcessActionType.Maximize;

            // Act
            var result = ActionExecutionResult.CreateNoForegroundWindow(actionType);

            // Assert
            Assert.False(result.Success);
            Assert.Equal(actionType, result.ActionType);
            Assert.Contains("foreground window", result.ErrorMessage);
            Assert.Contains("Quick Action", result.ErrorMessage);
        }

        [Fact]
        public void ToString_SuccessResult_ShouldIncludeSuccessAndDetails()
        {
            // Arrange
            var result = ActionExecutionResult.CreateSuccess(
                ProcessActionType.Mute, 123, "TestApp", IntPtr.Zero);

            // Act
            var str = result.ToString();

            // Assert
            Assert.Contains("Success", str);
            Assert.Contains("Mute", str);
            Assert.Contains("TestApp", str);
            Assert.Contains("123", str);
        }

        [Fact]
        public void ToString_FailureResult_ShouldIncludeFailedAndError()
        {
            // Arrange
            var result = ActionExecutionResult.CreateFailure(
                ProcessActionType.Close, "Window already closed");

            // Act
            var str = result.ToString();

            // Assert
            Assert.Contains("Failed", str);
            Assert.Contains("Close", str);
            Assert.Contains("Window already closed", str);
        }

        [Fact]
        public void ToString_SuccessWithUnknownProcess_ShouldShowUnknown()
        {
            // Arrange
            var result = ActionExecutionResult.CreateSuccess(
                ProcessActionType.Hide, 0, null!, IntPtr.Zero);

            // Act
            var str = result.ToString();

            // Assert
            Assert.Contains("unknown", str);
        }

        [Fact]
        public void ToString_SuccessWithNullProcessId_ShouldShowNA()
        {
            // Arrange
            var result = new ActionExecutionResult
            {
                Success = true,
                ActionType = ProcessActionType.Show,
                ProcessId = null,
                ProcessName = "Test"
            };

            // Act
            var str = result.ToString();

            // Assert
            Assert.Contains("N/A", str);
        }

        [Theory]
        [InlineData(ProcessActionType.Mute)]
        [InlineData(ProcessActionType.Unmute)]
        [InlineData(ProcessActionType.ToggleMute)]
        [InlineData(ProcessActionType.Close)]
        [InlineData(ProcessActionType.Minimize)]
        [InlineData(ProcessActionType.Maximize)]
        [InlineData(ProcessActionType.Restore)]
        [InlineData(ProcessActionType.BringToFront)]
        [InlineData(ProcessActionType.Hide)]
        [InlineData(ProcessActionType.Show)]
        public void CreateSuccess_AllActionTypes_ShouldWork(ProcessActionType actionType)
        {
            // Act
            var result = ActionExecutionResult.CreateSuccess(actionType, 1, "Test", IntPtr.Zero);

            // Assert
            Assert.True(result.Success);
            Assert.Equal(actionType, result.ActionType);
        }

        [Theory]
        [InlineData(ProcessActionType.Mute)]
        [InlineData(ProcessActionType.Unmute)]
        [InlineData(ProcessActionType.ToggleMute)]
        [InlineData(ProcessActionType.Close)]
        [InlineData(ProcessActionType.Minimize)]
        [InlineData(ProcessActionType.Maximize)]
        [InlineData(ProcessActionType.Restore)]
        [InlineData(ProcessActionType.BringToFront)]
        [InlineData(ProcessActionType.Hide)]
        [InlineData(ProcessActionType.Show)]
        public void CreateFailure_AllActionTypes_ShouldWork(ProcessActionType actionType)
        {
            // Act
            var result = ActionExecutionResult.CreateFailure(actionType, "Error");

            // Assert
            Assert.False(result.Success);
            Assert.Equal(actionType, result.ActionType);
        }

        [Fact]
        public void WindowHandle_ShouldBeNullableIntPtr()
        {
            // Arrange
            var resultWithHandle = ActionExecutionResult.CreateSuccess(
                ProcessActionType.BringToFront, 1, "Test", new IntPtr(0x12345));
            var resultWithoutHandle = ActionExecutionResult.CreateFailure(
                ProcessActionType.BringToFront, "Error");

            // Assert
            Assert.NotNull(resultWithHandle.WindowHandle);
            Assert.Equal(new IntPtr(0x12345), resultWithHandle.WindowHandle);
            Assert.Null(resultWithoutHandle.WindowHandle);
        }

        [Fact]
        public void Properties_AreInitOnly()
        {
            // Arrange & Act - use object initializer
            var result = new ActionExecutionResult
            {
                Success = true,
                ActionType = ProcessActionType.Mute,
                ProcessId = 123,
                ProcessName = "Test",
                WindowHandle = new IntPtr(0x100),
                ErrorMessage = null,
                HistoryEntry = null
            };

            // Assert - properties should retain their values
            Assert.True(result.Success);
            Assert.Equal(ProcessActionType.Mute, result.ActionType);
            Assert.Equal(123, result.ProcessId);
            Assert.Equal("Test", result.ProcessName);
            Assert.Equal(new IntPtr(0x100), result.WindowHandle);
        }
    }
}
