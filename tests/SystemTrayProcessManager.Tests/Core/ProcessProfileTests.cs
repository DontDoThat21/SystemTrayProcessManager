using SystemTrayProcessManager.Core.Enums;
using SystemTrayProcessManager.Core.Models;

namespace SystemTrayProcessManager.Tests.Core
{
    /// <summary>
    /// Unit tests for the ProcessProfile model.
    /// </summary>
    public class ProcessProfileTests
    {
        #region Constructor and Default Values

        [Fact]
        public void Constructor_ShouldSetDefaultId()
        {
            var profile = new ProcessProfile();
            Assert.NotEqual(Guid.Empty, profile.Id);
        }

        [Fact]
        public void Constructor_ShouldSetEmptyProcessName()
        {
            var profile = new ProcessProfile();
            Assert.Equal(string.Empty, profile.ProcessName);
        }

        [Fact]
        public void Constructor_ShouldSetEmptyDisplayName()
        {
            var profile = new ProcessProfile();
            Assert.Equal(string.Empty, profile.DisplayName);
        }

        [Fact]
        public void Constructor_ShouldSetNullWindowPosition()
        {
            var profile = new ProcessProfile();
            Assert.Null(profile.WindowX);
            Assert.Null(profile.WindowY);
            Assert.Null(profile.WindowWidth);
            Assert.Null(profile.WindowHeight);
        }

        [Fact]
        public void Constructor_ShouldSetNullAudioSettings()
        {
            var profile = new ProcessProfile();
            Assert.Null(profile.AudioLevel);
            Assert.Null(profile.IsMuted);
        }

        [Fact]
        public void Constructor_ShouldSetFalseAutoApply()
        {
            var profile = new ProcessProfile();
            Assert.False(profile.AutoApplyOnDetection);
        }

        #endregion

        #region HasWindowPosition Property

        [Fact]
        public void HasWindowPosition_NoValues_ShouldReturnFalse()
        {
            var profile = new ProcessProfile();
            Assert.False(profile.HasWindowPosition);
        }

        [Fact]
        public void HasWindowPosition_WithWindowX_ShouldReturnTrue()
        {
            var profile = new ProcessProfile { WindowX = 100 };
            Assert.True(profile.HasWindowPosition);
        }

        [Fact]
        public void HasWindowPosition_WithWindowY_ShouldReturnTrue()
        {
            var profile = new ProcessProfile { WindowY = 100 };
            Assert.True(profile.HasWindowPosition);
        }

        [Fact]
        public void HasWindowPosition_WithWindowWidth_ShouldReturnTrue()
        {
            var profile = new ProcessProfile { WindowWidth = 800 };
            Assert.True(profile.HasWindowPosition);
        }

        [Fact]
        public void HasWindowPosition_WithWindowHeight_ShouldReturnTrue()
        {
            var profile = new ProcessProfile { WindowHeight = 600 };
            Assert.True(profile.HasWindowPosition);
        }

        #endregion

        #region HasAudioSettings Property

        [Fact]
        public void HasAudioSettings_NoValues_ShouldReturnFalse()
        {
            var profile = new ProcessProfile();
            Assert.False(profile.HasAudioSettings);
        }

        [Fact]
        public void HasAudioSettings_WithAudioLevel_ShouldReturnTrue()
        {
            var profile = new ProcessProfile { AudioLevel = 0.5f };
            Assert.True(profile.HasAudioSettings);
        }

        [Fact]
        public void HasAudioSettings_WithIsMuted_ShouldReturnTrue()
        {
            var profile = new ProcessProfile { IsMuted = true };
            Assert.True(profile.HasAudioSettings);
        }

        #endregion

        #region HasAnySettings Property

        [Fact]
        public void HasAnySettings_NoValues_ShouldReturnFalse()
        {
            var profile = new ProcessProfile();
            Assert.False(profile.HasAnySettings);
        }

        [Fact]
        public void HasAnySettings_WithWindowPosition_ShouldReturnTrue()
        {
            var profile = new ProcessProfile { WindowX = 100 };
            Assert.True(profile.HasAnySettings);
        }

        [Fact]
        public void HasAnySettings_WithAudioLevel_ShouldReturnTrue()
        {
            var profile = new ProcessProfile { AudioLevel = 0.8f };
            Assert.True(profile.HasAnySettings);
        }

        [Fact]
        public void HasAnySettings_WithAlwaysOnTop_ShouldReturnTrue()
        {
            var profile = new ProcessProfile { AlwaysOnTop = true };
            Assert.True(profile.HasAnySettings);
        }

        [Fact]
        public void HasAnySettings_WithWindowState_ShouldReturnTrue()
        {
            var profile = new ProcessProfile { TargetWindowState = WindowState.Maximized };
            Assert.True(profile.HasAnySettings);
        }

        #endregion

        #region WithUpdatedTimestamp Method

        [Fact]
        public void WithUpdatedTimestamp_ShouldPreserveId()
        {
            var originalId = Guid.NewGuid();
            var profile = new ProcessProfile { Id = originalId };
            
            var updated = profile.WithUpdatedTimestamp();
            
            Assert.Equal(originalId, updated.Id);
        }

        [Fact]
        public void WithUpdatedTimestamp_ShouldPreserveProcessName()
        {
            var profile = new ProcessProfile { ProcessName = "TestProcess" };
            
            var updated = profile.WithUpdatedTimestamp();
            
            Assert.Equal("TestProcess", updated.ProcessName);
        }

        [Fact]
        public void WithUpdatedTimestamp_ShouldUpdateLastModified()
        {
            var oldTimestamp = DateTime.UtcNow.AddDays(-1);
            var profile = new ProcessProfile { LastModified = oldTimestamp };
            
            var updated = profile.WithUpdatedTimestamp();
            
            Assert.True(updated.LastModified > oldTimestamp);
        }

        [Fact]
        public void WithUpdatedTimestamp_ShouldPreserveCreatedAt()
        {
            var createdAt = DateTime.UtcNow.AddDays(-7);
            var profile = new ProcessProfile { CreatedAt = createdAt };
            
            var updated = profile.WithUpdatedTimestamp();
            
            Assert.Equal(createdAt, updated.CreatedAt);
        }

        #endregion

        #region Equality

        [Fact]
        public void Equals_SameId_ShouldReturnTrue()
        {
            var id = Guid.NewGuid();
            var profile1 = new ProcessProfile { Id = id };
            var profile2 = new ProcessProfile { Id = id };
            
            Assert.Equal(profile1, profile2);
        }

        [Fact]
        public void Equals_DifferentId_ShouldReturnFalse()
        {
            var profile1 = new ProcessProfile { Id = Guid.NewGuid() };
            var profile2 = new ProcessProfile { Id = Guid.NewGuid() };
            
            Assert.NotEqual(profile1, profile2);
        }

        [Fact]
        public void Equals_Null_ShouldReturnFalse()
        {
            var profile = new ProcessProfile();
            
            Assert.False(profile.Equals(null));
        }

        [Fact]
        public void GetHashCode_SameId_ShouldBeSame()
        {
            var id = Guid.NewGuid();
            var profile1 = new ProcessProfile { Id = id };
            var profile2 = new ProcessProfile { Id = id };
            
            Assert.Equal(profile1.GetHashCode(), profile2.GetHashCode());
        }

        [Fact]
        public void OperatorEquals_SameId_ShouldReturnTrue()
        {
            var id = Guid.NewGuid();
            var profile1 = new ProcessProfile { Id = id };
            var profile2 = new ProcessProfile { Id = id };
            
            Assert.True(profile1 == profile2);
        }

        [Fact]
        public void OperatorNotEquals_DifferentId_ShouldReturnTrue()
        {
            var profile1 = new ProcessProfile { Id = Guid.NewGuid() };
            var profile2 = new ProcessProfile { Id = Guid.NewGuid() };
            
            Assert.True(profile1 != profile2);
        }

        [Fact]
        public void OperatorEquals_LeftNull_ShouldReturnFalseIfRightNotNull()
        {
            ProcessProfile? left = null;
            var right = new ProcessProfile();
            
            Assert.False(left == right);
        }

        [Fact]
        public void OperatorEquals_BothNull_ShouldReturnTrue()
        {
            ProcessProfile? left = null;
            ProcessProfile? right = null;
            
            Assert.True(left == right);
        }

        #endregion

        #region ToString

        [Fact]
        public void ToString_ShouldContainId()
        {
            var id = Guid.NewGuid();
            var profile = new ProcessProfile { Id = id };
            
            var result = profile.ToString();
            
            Assert.Contains(id.ToString(), result);
        }

        [Fact]
        public void ToString_ShouldContainProcessName()
        {
            var profile = new ProcessProfile { ProcessName = "TestProcess" };
            
            var result = profile.ToString();
            
            Assert.Contains("TestProcess", result);
        }

        #endregion
    }
}
