using SystemTrayProcessManager.Core.Enums;
using SystemTrayProcessManager.Core.Models;

namespace SystemTrayProcessManager.Tests.Core
{
    public class WindowPositionTests
    {
        [Fact]
        public void Create_WithValidParameters_ReturnsConfiguredInstance()
        {
            string processName = "notepad";
            int x = 100, y = 200, width = 800, height = 600;
            WindowState windowState = WindowState.Normal;
            string layoutHash = "abc123";
            string windowTitle = "Untitled";

            WindowPosition position = WindowPosition.Create(
                processName, x, y, width, height, windowState, layoutHash, windowTitle);

            Assert.NotEqual(Guid.Empty, position.Id);
            Assert.Equal(processName, position.ProcessName);
            Assert.Equal(x, position.X);
            Assert.Equal(y, position.Y);
            Assert.Equal(width, position.Width);
            Assert.Equal(height, position.Height);
            Assert.Equal(windowState, position.WindowState);
            Assert.Equal(layoutHash, position.MonitorLayoutHash);
            Assert.Equal(windowTitle, position.WindowTitle);
            Assert.True(position.SavedAt <= DateTime.UtcNow);
        }

        [Fact]
        public void Create_WithoutWindowTitle_LeavesItNull()
        {
            WindowPosition position = WindowPosition.Create(
                "notepad", 0, 0, 800, 600, WindowState.Normal, "hash123");

            Assert.Null(position.WindowTitle);
        }

        [Fact]
        public void Create_WithNullProcessName_UsesEmptyString()
        {
            WindowPosition position = WindowPosition.Create(
                null!, 0, 0, 800, 600, WindowState.Normal, "hash");

            Assert.Equal(string.Empty, position.ProcessName);
        }

        [Fact]
        public void Create_WithNullLayoutHash_UsesEmptyString()
        {
            WindowPosition position = WindowPosition.Create(
                "notepad", 0, 0, 800, 600, WindowState.Normal, null!);

            Assert.Equal(string.Empty, position.MonitorLayoutHash);
        }

        [Theory]
        [InlineData("notepad", "notepad", null, true)]
        [InlineData("notepad", "NOTEPAD", null, true)]
        [InlineData("notepad", "Notepad", null, true)]
        [InlineData("notepad", "chrome", null, false)]
        public void Matches_ComparesProcessNameCaseInsensitively(
            string configName, string processName, string? windowTitle, bool expected)
        {
            WindowPosition position = WindowPosition.Create(
                configName, 0, 0, 800, 600, WindowState.Normal, "hash");

            bool result = position.Matches(processName, windowTitle);

            Assert.Equal(expected, result);
        }

        [Fact]
        public void Matches_WithWindowTitleConstraint_MatchesContaining()
        {
            WindowPosition position = new()
            {
                ProcessName = "notepad",
                WindowTitle = "Document"
            };

            Assert.True(position.Matches("notepad", "My Document.txt"));
            Assert.True(position.Matches("notepad", "Document 1"));
            Assert.False(position.Matches("notepad", "Untitled"));
        }

        [Fact]
        public void Matches_WithWindowTitleConstraint_IsCaseInsensitive()
        {
            WindowPosition position = new()
            {
                ProcessName = "notepad",
                WindowTitle = "Document"
            };

            Assert.True(position.Matches("notepad", "MY DOCUMENT"));
            Assert.True(position.Matches("notepad", "document"));
        }

        [Fact]
        public void Matches_EmptyWindowTitle_IgnoresTitleMatching()
        {
            WindowPosition position = new()
            {
                ProcessName = "notepad",
                WindowTitle = ""
            };

            Assert.True(position.Matches("notepad", "Any Title"));
            Assert.True(position.Matches("notepad", null));
        }

        [Fact]
        public void Matches_NullWindowTitle_IgnoresTitleMatching()
        {
            WindowPosition position = new()
            {
                ProcessName = "notepad",
                WindowTitle = null
            };

            Assert.True(position.Matches("notepad", "Any Title"));
        }

        [Theory]
        [InlineData(WindowState.Normal)]
        [InlineData(WindowState.Minimized)]
        [InlineData(WindowState.Maximized)]
        [InlineData(WindowState.Hidden)]
        public void Create_SupportsAllWindowStates(WindowState windowState)
        {
            WindowPosition position = WindowPosition.Create(
                "notepad", 100, 100, 800, 600, windowState, "hash");

            Assert.Equal(windowState, position.WindowState);
        }

        [Fact]
        public void RecordEquality_SameValues_AreEqual()
        {
            Guid id = Guid.NewGuid();
            DateTime timestamp = DateTime.UtcNow;

            WindowPosition position1 = new()
            {
                Id = id,
                ProcessName = "notepad",
                WindowTitle = "Test",
                X = 100,
                Y = 200,
                    Width = 800,
                    Height = 600,
                    WindowState = WindowState.Normal,
                    MonitorLayoutHash = "hash123",
                    SavedAt = timestamp
                };

                WindowPosition position2 = new()
                {
                    Id = id,
                    ProcessName = "notepad",
                    WindowTitle = "Test",
                    X = 100,
                    Y = 200,
                    Width = 800,
                    Height = 600,
                    WindowState = WindowState.Normal,
                MonitorLayoutHash = "hash123",
                SavedAt = timestamp
            };

            Assert.Equal(position1, position2);
            Assert.True(position1 == position2);
        }

        [Fact]
        public void RecordEquality_DifferentPosition_AreNotEqual()
        {
            Guid id = Guid.NewGuid();

            WindowPosition position1 = new() { Id = id, ProcessName = "notepad", X = 100, Y = 200 };
            WindowPosition position2 = new() { Id = id, ProcessName = "notepad", X = 300, Y = 400 };

            Assert.NotEqual(position1, position2);
        }

        [Fact]
        public void WithExpression_CreatesCopyWithModifiedValues()
        {
            WindowPosition original = WindowPosition.Create(
                "notepad", 100, 200, 800, 600, WindowState.Normal, "hash");

            WindowPosition modified = original with { X = 500, Y = 300 };

            Assert.Equal(original.Id, modified.Id);
            Assert.Equal(original.ProcessName, modified.ProcessName);
            Assert.Equal(500, modified.X);
            Assert.Equal(300, modified.Y);
            Assert.Equal(original.Width, modified.Width);
            Assert.Equal(original.Height, modified.Height);
        }
    }
}
