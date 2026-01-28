using SystemTrayProcessManager.Core.Enums;
using SystemTrayProcessManager.Core.Models;
using Xunit;

namespace SystemTrayProcessManager.Tests.Core
{
    /// <summary>
    /// Unit tests for the GamingModeConfig model.
    /// </summary>
    public class GamingModeConfigTests
    {
        [Fact]
        public void Default_ShouldHaveExpectedValues()
        {
            var config = GamingModeConfig.Default;

            Assert.False(config.IsEnabled);
            Assert.NotNull(config.ProcessesToClose);
            Assert.NotNull(config.ProcessesToMute);
            Assert.NotNull(config.ProcessesToMinimize);
            Assert.Equal(ProcessPriority.High, config.GamePriority);
            Assert.True(config.SuppressNotifications);
            Assert.Null(config.ActiveGameProcess);
            Assert.Null(config.EnabledAt);
        }

        [Fact]
        public void Default_ShouldHaveDefaultMuteList()
        {
            var config = GamingModeConfig.Default;

            Assert.Contains("Discord", config.ProcessesToMute);
            Assert.Contains("Slack", config.ProcessesToMute);
            Assert.Contains("Teams", config.ProcessesToMute);
            Assert.Contains("Skype", config.ProcessesToMute);
        }

        [Fact]
        public void Enable_ShouldSetEnabledAndTimestamp()
        {
            var config = GamingModeConfig.Default;
            var enabled = config.Enable();

            Assert.True(enabled.IsEnabled);
            Assert.NotNull(enabled.EnabledAt);
            Assert.True(enabled.EnabledAt <= DateTime.UtcNow);
        }

        [Fact]
        public void Enable_WithGameProcess_ShouldSetActiveGame()
        {
            var config = GamingModeConfig.Default;
            var enabled = config.Enable("MyGame");

            Assert.True(enabled.IsEnabled);
            Assert.Equal("MyGame", enabled.ActiveGameProcess);
        }

        [Fact]
        public void Disable_ShouldClearEnabledAndActiveGame()
        {
            var config = GamingModeConfig.Default.Enable("MyGame");
            var disabled = config.Disable();

            Assert.False(disabled.IsEnabled);
            Assert.Null(disabled.ActiveGameProcess);
            Assert.Null(disabled.EnabledAt);
        }

        [Fact]
        public void WithProcessLists_ShouldUpdateLists()
        {
            var config = GamingModeConfig.Default;
            var updated = config.WithProcessLists(
                processesToClose: new[] { "browser.exe" },
                processesToMute: new[] { "chat.exe" },
                processesToMinimize: new[] { "email.exe" });

            Assert.Single(updated.ProcessesToClose);
            Assert.Single(updated.ProcessesToMute);
            Assert.Single(updated.ProcessesToMinimize);
            Assert.Contains("browser.exe", updated.ProcessesToClose);
            Assert.Contains("chat.exe", updated.ProcessesToMute);
            Assert.Contains("email.exe", updated.ProcessesToMinimize);
        }

        [Fact]
        public void WithProcessLists_NullParameter_ShouldKeepExisting()
        {
            var config = GamingModeConfig.Default;
            var originalMuteCount = config.ProcessesToMute.Count;
            var updated = config.WithProcessLists(
                processesToClose: new[] { "browser.exe" },
                processesToMute: null,
                processesToMinimize: null);

            Assert.Single(updated.ProcessesToClose);
            Assert.Equal(originalMuteCount, updated.ProcessesToMute.Count);
        }

        [Fact]
        public void RecordEquality_SameValues_ShouldBeEqual()
        {
            var config1 = new GamingModeConfig
            {
                IsEnabled = true,
                GamePriority = ProcessPriority.High,
                SuppressNotifications = true
            };

            var config2 = new GamingModeConfig
            {
                IsEnabled = true,
                GamePriority = ProcessPriority.High,
                SuppressNotifications = true
            };

            Assert.Equal(config1, config2);
        }

        [Fact]
        public void RecordEquality_DifferentPriority_ShouldNotBeEqual()
        {
            var config1 = new GamingModeConfig { GamePriority = ProcessPriority.High };
            var config2 = new GamingModeConfig { GamePriority = ProcessPriority.RealTime };

            Assert.NotEqual(config1, config2);
        }
    }
}
