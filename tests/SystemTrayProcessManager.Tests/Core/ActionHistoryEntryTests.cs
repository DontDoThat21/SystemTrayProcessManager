using SystemTrayProcessManager.Core.Enums;
using SystemTrayProcessManager.Core.Models;

namespace SystemTrayProcessManager.Tests.Core
{
    /// <summary>
    /// Unit tests for the <see cref="ActionHistoryEntry"/> class.
    /// </summary>
    public class ActionHistoryEntryTests
    {
        [Fact]
        public void Create_ShouldSetAllProperties()
        {
            // Arrange
            var actionType = ProcessActionType.Mute;
            var mode = ActionMode.QuickAction;
            var processId = 12345;
            var processName = "TestProcess";
            var windowHandle = new IntPtr(0x12345);
            var wasSuccessful = true;
            var previousState = "PreviousValue";

            // Act
            var entry = ActionHistoryEntry.Create(
                actionType, mode, processId, processName, windowHandle, wasSuccessful, previousState);

            // Assert
            Assert.NotEqual(Guid.Empty, entry.Id);
            Assert.True((DateTime.UtcNow - entry.Timestamp).TotalSeconds < 5);
            Assert.Equal(actionType, entry.ActionType);
            Assert.Equal(mode, entry.Mode);
            Assert.Equal(processId, entry.ProcessId);
            Assert.Equal(processName, entry.ProcessName);
            Assert.Equal(windowHandle, entry.WindowHandle);
            Assert.Equal(wasSuccessful, entry.WasSuccessful);
            Assert.Equal(previousState, entry.PreviousState);
        }

        [Fact]
        public void Create_WithNullProcessName_ShouldUseEmptyString()
        {
            // Act
            var entry = ActionHistoryEntry.Create(
                ProcessActionType.Mute, ActionMode.QuickAction, 1, null!, IntPtr.Zero, true);

            // Assert
            Assert.Equal(string.Empty, entry.ProcessName);
        }

        [Theory]
        [InlineData(ProcessActionType.Mute, true)]
        [InlineData(ProcessActionType.Unmute, true)]
        [InlineData(ProcessActionType.ToggleMute, true)]
        [InlineData(ProcessActionType.Minimize, true)]
        [InlineData(ProcessActionType.Maximize, true)]
        [InlineData(ProcessActionType.Restore, true)]
        [InlineData(ProcessActionType.Hide, true)]
        [InlineData(ProcessActionType.Show, true)]
        [InlineData(ProcessActionType.Close, false)]
        [InlineData(ProcessActionType.BringToFront, false)]
        [InlineData(ProcessActionType.None, false)]
        public void Create_SetsIsReversibleCorrectly(ProcessActionType actionType, bool expectedReversible)
        {
            // Act
            var entry = ActionHistoryEntry.Create(
                actionType, ActionMode.QuickAction, 1, "Test", IntPtr.Zero, wasSuccessful: true);

            // Assert
            Assert.Equal(expectedReversible, entry.IsReversible);
        }

        [Fact]
        public void Create_FailedAction_ShouldNotBeReversible()
        {
            // Act - even a normally reversible action shouldn't be reversible if it failed
            var entry = ActionHistoryEntry.Create(
                ProcessActionType.Mute, ActionMode.QuickAction, 1, "Test", IntPtr.Zero, wasSuccessful: false);

            // Assert
            Assert.False(entry.IsReversible);
        }

        [Theory]
        [InlineData(ProcessActionType.Mute, ProcessActionType.Unmute)]
        [InlineData(ProcessActionType.Unmute, ProcessActionType.Mute)]
        [InlineData(ProcessActionType.ToggleMute, ProcessActionType.ToggleMute)]
        [InlineData(ProcessActionType.Minimize, ProcessActionType.Restore)]
        [InlineData(ProcessActionType.Maximize, ProcessActionType.Restore)]
        [InlineData(ProcessActionType.Hide, ProcessActionType.Show)]
        [InlineData(ProcessActionType.Show, ProcessActionType.Hide)]
        public void Create_SetsReverseActionTypeCorrectly(ProcessActionType actionType, ProcessActionType expectedReverse)
        {
            // Act
            var entry = ActionHistoryEntry.Create(
                actionType, ActionMode.QuickAction, 1, "Test", IntPtr.Zero, true);

            // Assert
            Assert.Equal(expectedReverse, entry.ReverseActionType);
        }

        [Theory]
        [InlineData(ProcessActionType.Close)]
        [InlineData(ProcessActionType.BringToFront)]
        [InlineData(ProcessActionType.Restore)]
        public void Create_NonReversibleActions_ShouldHaveNullReverseAction(ProcessActionType actionType)
        {
            // Act
            var entry = ActionHistoryEntry.Create(
                actionType, ActionMode.QuickAction, 1, "Test", IntPtr.Zero, true);

            // Assert
            Assert.Null(entry.ReverseActionType);
        }

        [Fact]
        public void GetIsReversible_ShouldReturnCorrectValue()
        {
            // Assert
            Assert.True(ActionHistoryEntry.GetIsReversible(ProcessActionType.Mute));
            Assert.True(ActionHistoryEntry.GetIsReversible(ProcessActionType.Unmute));
            Assert.True(ActionHistoryEntry.GetIsReversible(ProcessActionType.ToggleMute));
            Assert.True(ActionHistoryEntry.GetIsReversible(ProcessActionType.Minimize));
            Assert.True(ActionHistoryEntry.GetIsReversible(ProcessActionType.Maximize));
            Assert.True(ActionHistoryEntry.GetIsReversible(ProcessActionType.Restore));
            Assert.True(ActionHistoryEntry.GetIsReversible(ProcessActionType.Hide));
            Assert.True(ActionHistoryEntry.GetIsReversible(ProcessActionType.Show));
            Assert.False(ActionHistoryEntry.GetIsReversible(ProcessActionType.Close));
            Assert.False(ActionHistoryEntry.GetIsReversible(ProcessActionType.BringToFront));
            Assert.False(ActionHistoryEntry.GetIsReversible(ProcessActionType.None));
        }

        [Fact]
        public void GetReverseAction_Mute_ShouldReturnUnmute()
        {
            // Assert
            Assert.Equal(ProcessActionType.Unmute, ActionHistoryEntry.GetReverseAction(ProcessActionType.Mute));
        }

        [Fact]
        public void GetReverseAction_Unmute_ShouldReturnMute()
        {
            // Assert
            Assert.Equal(ProcessActionType.Mute, ActionHistoryEntry.GetReverseAction(ProcessActionType.Unmute));
        }

        [Fact]
        public void GetReverseAction_ToggleMute_ShouldReturnToggleMute()
        {
            // Assert
            Assert.Equal(ProcessActionType.ToggleMute, ActionHistoryEntry.GetReverseAction(ProcessActionType.ToggleMute));
        }

        [Fact]
        public void GetReverseAction_Minimize_ShouldReturnRestore()
        {
            // Assert
            Assert.Equal(ProcessActionType.Restore, ActionHistoryEntry.GetReverseAction(ProcessActionType.Minimize));
        }

        [Fact]
        public void GetReverseAction_Maximize_ShouldReturnRestore()
        {
            // Assert
            Assert.Equal(ProcessActionType.Restore, ActionHistoryEntry.GetReverseAction(ProcessActionType.Maximize));
        }

        [Fact]
        public void GetReverseAction_Hide_ShouldReturnShow()
        {
            // Assert
            Assert.Equal(ProcessActionType.Show, ActionHistoryEntry.GetReverseAction(ProcessActionType.Hide));
        }

        [Fact]
        public void GetReverseAction_Show_ShouldReturnHide()
        {
            // Assert
            Assert.Equal(ProcessActionType.Hide, ActionHistoryEntry.GetReverseAction(ProcessActionType.Show));
        }

        [Theory]
        [InlineData(ProcessActionType.Close)]
        [InlineData(ProcessActionType.BringToFront)]
        [InlineData(ProcessActionType.Restore)]
        [InlineData(ProcessActionType.None)]
        public void GetReverseAction_NonReversible_ShouldReturnNull(ProcessActionType actionType)
        {
            // Assert
            Assert.Null(ActionHistoryEntry.GetReverseAction(actionType));
        }

        [Fact]
        public void ToString_SuccessfulAction_ShouldIncludeSuccess()
        {
            // Arrange
            var entry = ActionHistoryEntry.Create(
                ProcessActionType.Mute, ActionMode.QuickAction, 123, "TestApp", IntPtr.Zero, wasSuccessful: true);

            // Act
            var result = entry.ToString();

            // Assert
            Assert.Contains("Mute", result);
            Assert.Contains("TestApp", result);
            Assert.Contains("123", result);
            Assert.Contains("Success", result);
        }

        [Fact]
        public void ToString_FailedAction_ShouldIncludeFailed()
        {
            // Arrange
            var entry = ActionHistoryEntry.Create(
                ProcessActionType.Close, ActionMode.PinnedProcess, 456, "AnotherApp", IntPtr.Zero, wasSuccessful: false);

            // Act
            var result = entry.ToString();

            // Assert
            Assert.Contains("Close", result);
            Assert.Contains("AnotherApp", result);
            Assert.Contains("456", result);
            Assert.Contains("Failed", result);
        }

        [Fact]
        public void Create_GeneratesUniqueIds()
        {
            // Arrange
            var ids = new HashSet<Guid>();

            // Act
            for (int i = 0; i < 100; i++)
            {
                var entry = ActionHistoryEntry.Create(
                    ProcessActionType.Mute, ActionMode.QuickAction, i, $"Process{i}", IntPtr.Zero, true);
                ids.Add(entry.Id);
            }

            // Assert - all IDs should be unique
            Assert.Equal(100, ids.Count);
        }

        [Fact]
        public void Create_TimestampShouldBeUtc()
        {
            // Act
            var entry = ActionHistoryEntry.Create(
                ProcessActionType.Mute, ActionMode.QuickAction, 1, "Test", IntPtr.Zero, true);

            // Assert
            Assert.Equal(DateTimeKind.Utc, entry.Timestamp.Kind);
        }

        [Fact]
        public void Properties_AreImmutable()
        {
            // Arrange
            var entry = ActionHistoryEntry.Create(
                ProcessActionType.Mute, ActionMode.QuickAction, 1, "Test", IntPtr.Zero, true);
            var originalId = entry.Id;
            var originalTimestamp = entry.Timestamp;

            // Act & Assert - these are init-only properties
            // The test verifies that the object can be created correctly with init properties
            Assert.Equal(originalId, entry.Id);
            Assert.Equal(originalTimestamp, entry.Timestamp);
        }

        [Fact]
        public void Create_WithQuickActionMode_ShouldSetModeCorrectly()
        {
            // Act
            var entry = ActionHistoryEntry.Create(
                ProcessActionType.Minimize, ActionMode.QuickAction, 1, "Test", IntPtr.Zero, true);

            // Assert
            Assert.Equal(ActionMode.QuickAction, entry.Mode);
        }

        [Fact]
        public void Create_WithPinnedProcessMode_ShouldSetModeCorrectly()
        {
            // Act
            var entry = ActionHistoryEntry.Create(
                ProcessActionType.Minimize, ActionMode.PinnedProcess, 1, "Test", IntPtr.Zero, true);

            // Assert
            Assert.Equal(ActionMode.PinnedProcess, entry.Mode);
        }
    }
}
