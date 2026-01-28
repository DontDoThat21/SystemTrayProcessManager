using SystemTrayProcessManager.Core.Models;
using Xunit;

namespace SystemTrayProcessManager.Tests.Core
{
    /// <summary>
    /// Unit tests for the FocusHistoryEntry model.
    /// </summary>
    public class FocusHistoryEntryTests
    {
        [Fact]
        public void Create_WithValidParameters_ShouldSetAllProperties()
        {
            var handle = new IntPtr(12345);
            var processId = 100;
            var processName = "notepad";
            var windowTitle = "Untitled - Notepad";

            var entry = FocusHistoryEntry.Create(handle, processId, processName, windowTitle);

            Assert.Equal(handle, entry.WindowHandle);
            Assert.Equal(processId, entry.ProcessId);
            Assert.Equal(processName, entry.ProcessName);
            Assert.Equal(windowTitle, entry.WindowTitle);
            Assert.True(entry.FocusedAt <= DateTime.UtcNow);
        }

        [Fact]
        public void Create_WithNullProcessName_ShouldUseEmptyString()
        {
            var entry = FocusHistoryEntry.Create(new IntPtr(1), 1, null!, "Title");

            Assert.Equal(string.Empty, entry.ProcessName);
        }

        [Fact]
        public void Create_WithNullWindowTitle_ShouldUseEmptyString()
        {
            var entry = FocusHistoryEntry.Create(new IntPtr(1), 1, "Name", null!);

            Assert.Equal(string.Empty, entry.WindowTitle);
        }

        [Fact]
        public void IsValid_WithValidHandle_ShouldReturnTrue()
        {
            var entry = FocusHistoryEntry.Create(new IntPtr(1), 1, "Name", "Title");

            Assert.True(entry.IsValid);
        }

        [Fact]
        public void IsValid_WithZeroHandle_ShouldReturnFalse()
        {
            var entry = FocusHistoryEntry.Create(IntPtr.Zero, 1, "Name", "Title");

            Assert.False(entry.IsValid);
        }

        [Fact]
        public void IsValid_WithZeroProcessId_ShouldReturnFalse()
        {
            var entry = FocusHistoryEntry.Create(new IntPtr(1), 0, "Name", "Title");

            Assert.False(entry.IsValid);
        }

        [Fact]
        public void DefaultConstructor_ShouldHaveDefaultValues()
        {
            var entry = new FocusHistoryEntry();

            Assert.Equal(IntPtr.Zero, entry.WindowHandle);
            Assert.Equal(0, entry.ProcessId);
            Assert.Equal(string.Empty, entry.ProcessName);
            Assert.Equal(string.Empty, entry.WindowTitle);
        }

        [Fact]
        public void Equality_SameValues_ShouldBeEqual()
        {
            var entry1 = new FocusHistoryEntry
            {
                WindowHandle = new IntPtr(123),
                ProcessId = 456,
                ProcessName = "test",
                WindowTitle = "title",
                FocusedAt = DateTime.UtcNow
            };

            var entry2 = entry1 with { };

            Assert.Equal(entry1, entry2);
        }

        [Fact]
        public void Equality_DifferentHandle_ShouldNotBeEqual()
        {
            var entry1 = new FocusHistoryEntry { WindowHandle = new IntPtr(1) };
            var entry2 = new FocusHistoryEntry { WindowHandle = new IntPtr(2) };

            Assert.NotEqual(entry1, entry2);
        }

        [Fact]
        public void WithExpression_ShouldCreateModifiedCopy()
        {
            var original = FocusHistoryEntry.Create(new IntPtr(1), 100, "Original", "Title");
            var modified = original with { ProcessName = "Modified" };

            Assert.Equal("Original", original.ProcessName);
            Assert.Equal("Modified", modified.ProcessName);
            Assert.Equal(original.WindowHandle, modified.WindowHandle);
        }
    }
}
