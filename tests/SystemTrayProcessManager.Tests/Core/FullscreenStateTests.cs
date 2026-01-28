using SystemTrayProcessManager.Core.Models;
using Xunit;

namespace SystemTrayProcessManager.Tests.Core
{
    /// <summary>
    /// Unit tests for the FullscreenState model.
    /// </summary>
    public class FullscreenStateTests
    {
        [Fact]
        public void Create_WithValidParameters_ShouldSetAllProperties()
        {
            var handle = new IntPtr(12345);
            var processId = 100;
            var processName = "game";
            var isFullscreen = true;

            var state = FullscreenState.Create(handle, processId, processName, isFullscreen);

            Assert.Equal(handle, state.WindowHandle);
            Assert.Equal(processId, state.ProcessId);
            Assert.Equal(processName, state.ProcessName);
            Assert.True(state.IsFullscreen);
            Assert.True(state.DetectedAt <= DateTime.UtcNow);
        }

        [Fact]
        public void Create_WithNullProcessName_ShouldUseEmptyString()
        {
            var state = FullscreenState.Create(new IntPtr(1), 1, null!, true);

            Assert.Equal(string.Empty, state.ProcessName);
        }

        [Fact]
        public void Create_NotFullscreen_ShouldSetCorrectly()
        {
            var state = FullscreenState.Create(new IntPtr(1), 1, "test", false);

            Assert.False(state.IsFullscreen);
        }

        [Fact]
        public void None_ShouldReturnEmptyState()
        {
            var none = FullscreenState.None;

            Assert.Equal(IntPtr.Zero, none.WindowHandle);
            Assert.Equal(0, none.ProcessId);
            Assert.Equal(string.Empty, none.ProcessName);
            Assert.False(none.IsFullscreen);
        }

        [Fact]
        public void DefaultConstructor_ShouldHaveDefaultValues()
        {
            var state = new FullscreenState();

            Assert.Equal(IntPtr.Zero, state.WindowHandle);
            Assert.Equal(0, state.ProcessId);
            Assert.Equal(string.Empty, state.ProcessName);
            Assert.False(state.IsFullscreen);
        }

        [Fact]
        public void Equality_SameValues_ShouldBeEqual()
        {
            var state1 = new FullscreenState
            {
                WindowHandle = new IntPtr(123),
                ProcessId = 456,
                ProcessName = "test",
                IsFullscreen = true,
                DetectedAt = DateTime.UtcNow
            };

            var state2 = state1 with { };

            Assert.Equal(state1, state2);
        }

        [Fact]
        public void Equality_DifferentFullscreenState_ShouldNotBeEqual()
        {
            var state1 = FullscreenState.Create(new IntPtr(1), 1, "test", true);
            var state2 = FullscreenState.Create(new IntPtr(1), 1, "test", false);

            Assert.NotEqual(state1, state2);
        }

        [Fact]
        public void WithExpression_ShouldCreateModifiedCopy()
        {
            var original = FullscreenState.Create(new IntPtr(1), 100, "Original", true);
            var modified = original with { IsFullscreen = false };

            Assert.True(original.IsFullscreen);
            Assert.False(modified.IsFullscreen);
        }
    }
}
