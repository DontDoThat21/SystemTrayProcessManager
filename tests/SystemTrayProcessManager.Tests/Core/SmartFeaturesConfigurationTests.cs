using SystemTrayProcessManager.Core.Enums;
using SystemTrayProcessManager.Core.Models;

namespace SystemTrayProcessManager.Tests.Core
{
    public class SmartFeaturesConfigurationTests
    {
        [Fact]
        public void Default_ReturnsNewInstance()
        {
            SmartFeaturesConfiguration config1 = SmartFeaturesConfiguration.Default;
            SmartFeaturesConfiguration config2 = SmartFeaturesConfiguration.Default;

            Assert.NotSame(config1, config2);
        }

        [Fact]
        public void Default_HasExpectedDefaultValues()
        {
            SmartFeaturesConfiguration config = SmartFeaturesConfiguration.Default;

            Assert.Equal(10, config.FocusHistorySize);
            Assert.True(config.FocusHistoryEnabled);
            Assert.False(config.AutoMuteOnFullscreen);
            Assert.False(config.WindowPositionMemoryEnabled);
            Assert.False(config.StartWithWindows);
            Assert.True(config.StartMinimized);
            Assert.Null(config.LastMonitorLayoutHash);
        }

        [Fact]
        public void Default_HasAutoMuteProcessesList()
        {
            SmartFeaturesConfiguration config = SmartFeaturesConfiguration.Default;

            Assert.NotEmpty(config.AutoMuteProcesses);
            Assert.Contains("Discord", config.AutoMuteProcesses);
            Assert.Contains("Slack", config.AutoMuteProcesses);
            Assert.Contains("Teams", config.AutoMuteProcesses);
            Assert.Contains("Skype", config.AutoMuteProcesses);
        }

        [Fact]
        public void Default_HasDefaultGamingModeConfig()
        {
            SmartFeaturesConfiguration config = SmartFeaturesConfiguration.Default;

            Assert.NotNull(config.GamingMode);
            Assert.False(config.GamingMode.IsEnabled);
        }

        [Fact]
        public void Default_HasEmptyLists()
        {
            SmartFeaturesConfiguration config = SmartFeaturesConfiguration.Default;

            Assert.Empty(config.SavedPositions);
            Assert.Empty(config.ProcessPriorities);
        }

        [Fact]
        public void Clone_CreatesDeepCopy()
        {
            SmartFeaturesConfiguration original = new()
            {
                FocusHistorySize = 20,
                FocusHistoryEnabled = false,
                AutoMuteOnFullscreen = true,
                WindowPositionMemoryEnabled = true,
                StartWithWindows = true,
                StartMinimized = false,
                LastMonitorLayoutHash = "hash123"
            };
            original.AutoMuteProcesses.Add("CustomApp");
            original.SavedPositions.Add(WindowPosition.Create("notepad", 0, 0, 800, 600, WindowState.Normal, "hash"));
            original.ProcessPriorities.Add(ProcessPriorityConfig.Create("chrome", ProcessPriority.Idle));

            SmartFeaturesConfiguration clone = original.Clone();

            Assert.NotSame(original, clone);
            Assert.Equal(original.FocusHistorySize, clone.FocusHistorySize);
            Assert.Equal(original.FocusHistoryEnabled, clone.FocusHistoryEnabled);
            Assert.Equal(original.AutoMuteOnFullscreen, clone.AutoMuteOnFullscreen);
            Assert.Equal(original.WindowPositionMemoryEnabled, clone.WindowPositionMemoryEnabled);
            Assert.Equal(original.StartWithWindows, clone.StartWithWindows);
            Assert.Equal(original.StartMinimized, clone.StartMinimized);
            Assert.Equal(original.LastMonitorLayoutHash, clone.LastMonitorLayoutHash);
        }

        [Fact]
        public void Clone_ClonesAutoMuteProcessesList()
        {
            SmartFeaturesConfiguration original = new();
            original.AutoMuteProcesses.Add("TestApp");

            SmartFeaturesConfiguration clone = original.Clone();

            Assert.NotSame(original.AutoMuteProcesses, clone.AutoMuteProcesses);
            Assert.Contains("TestApp", clone.AutoMuteProcesses);

            // Verify independence
            clone.AutoMuteProcesses.Add("AnotherApp");
            Assert.DoesNotContain("AnotherApp", original.AutoMuteProcesses);
        }

        [Fact]
        public void Clone_ClonesSavedPositionsList()
        {
            SmartFeaturesConfiguration original = new();
            WindowPosition position = WindowPosition.Create("notepad", 100, 200, 800, 600, WindowState.Normal, "hash");
            original.SavedPositions.Add(position);

            SmartFeaturesConfiguration clone = original.Clone();

            Assert.NotSame(original.SavedPositions, clone.SavedPositions);
            Assert.Single(clone.SavedPositions);
            Assert.Equal(position.ProcessName, clone.SavedPositions[0].ProcessName);

            // Verify independence
            clone.SavedPositions.Clear();
            Assert.Single(original.SavedPositions);
        }

        [Fact]
        public void Clone_ClonesProcessPrioritiesList()
        {
            SmartFeaturesConfiguration original = new();
            ProcessPriorityConfig priorityConfig = ProcessPriorityConfig.Create("chrome", ProcessPriority.Idle);
            original.ProcessPriorities.Add(priorityConfig);

            SmartFeaturesConfiguration clone = original.Clone();

            Assert.NotSame(original.ProcessPriorities, clone.ProcessPriorities);
            Assert.Single(clone.ProcessPriorities);
            Assert.Equal(priorityConfig.ProcessName, clone.ProcessPriorities[0].ProcessName);

            // Verify independence
            clone.ProcessPriorities.Clear();
            Assert.Single(original.ProcessPriorities);
        }

        [Fact]
        public void Clone_ClonesGamingModeConfig()
        {
            SmartFeaturesConfiguration original = new()
            {
                GamingMode = GamingModeConfig.Default.Enable()
            };

            SmartFeaturesConfiguration clone = original.Clone();

            Assert.NotSame(original.GamingMode, clone.GamingMode);
            Assert.True(clone.GamingMode.IsEnabled);
        }

        [Fact]
        public void ModifyingProperties_DoesNotAffectDefault()
        {
            SmartFeaturesConfiguration config = SmartFeaturesConfiguration.Default;
            config.FocusHistorySize = 50;
            config.FocusHistoryEnabled = false;
            config.AutoMuteProcesses.Add("NewApp");

            SmartFeaturesConfiguration newDefault = SmartFeaturesConfiguration.Default;

            Assert.Equal(10, newDefault.FocusHistorySize);
            Assert.True(newDefault.FocusHistoryEnabled);
            Assert.DoesNotContain("NewApp", newDefault.AutoMuteProcesses);
        }

        [Fact]
        public void LastSaved_DefaultsToUtcNow()
        {
            DateTime before = DateTime.UtcNow;
            SmartFeaturesConfiguration config = new();
            DateTime after = DateTime.UtcNow;

            Assert.True(config.LastSaved >= before);
            Assert.True(config.LastSaved <= after);
        }

        [Fact]
        public void CanSetAllProperties()
        {
            DateTime testTime = new(2024, 1, 15, 12, 0, 0, DateTimeKind.Utc);

            SmartFeaturesConfiguration config = new()
            {
                FocusHistorySize = 25,
                FocusHistoryEnabled = false,
                AutoMuteOnFullscreen = true,
                AutoMuteProcesses = new List<string> { "App1", "App2" },
                WindowPositionMemoryEnabled = true,
                SavedPositions = new List<WindowPosition>(),
                GamingMode = GamingModeConfig.Default.Enable(),
                ProcessPriorities = new List<ProcessPriorityConfig>(),
                StartWithWindows = true,
                StartMinimized = false,
                LastMonitorLayoutHash = "customhash",
                LastSaved = testTime
            };

            Assert.Equal(25, config.FocusHistorySize);
            Assert.False(config.FocusHistoryEnabled);
            Assert.True(config.AutoMuteOnFullscreen);
            Assert.Equal(2, config.AutoMuteProcesses.Count);
            Assert.True(config.WindowPositionMemoryEnabled);
            Assert.True(config.GamingMode.IsEnabled);
            Assert.True(config.StartWithWindows);
            Assert.False(config.StartMinimized);
            Assert.Equal("customhash", config.LastMonitorLayoutHash);
            Assert.Equal(testTime, config.LastSaved);
        }
    }
}
