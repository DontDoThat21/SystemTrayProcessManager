using SystemTrayProcessManager.Core.Models;

namespace SystemTrayProcessManager.Tests.Core
{
    /// <summary>
    /// Unit tests for the AudioProcessInfo model.
    /// </summary>
    public class AudioProcessInfoTests
    {
        #region Default Values Tests

        [Fact]
        public void DefaultValues_ProcessId_IsZero()
        {
            // Arrange & Act
            var info = new AudioProcessInfo();

            // Assert
            Assert.Equal(0, info.ProcessId);
        }

        [Fact]
        public void DefaultValues_ProcessName_IsEmpty()
        {
            // Arrange & Act
            var info = new AudioProcessInfo();

            // Assert
            Assert.Equal(string.Empty, info.ProcessName);
        }

        [Fact]
        public void DefaultValues_DisplayName_IsEmpty()
        {
            // Arrange & Act
            var info = new AudioProcessInfo();

            // Assert
            Assert.Equal(string.Empty, info.DisplayName);
        }

        [Fact]
        public void DefaultValues_Volume_IsZero()
        {
            // Arrange & Act
            var info = new AudioProcessInfo();

            // Assert
            Assert.Equal(0.0f, info.Volume);
        }

        [Fact]
        public void DefaultValues_IsMuted_IsFalse()
        {
            // Arrange & Act
            var info = new AudioProcessInfo();

            // Assert
            Assert.False(info.IsMuted);
        }

        [Fact]
        public void DefaultValues_IsActive_IsFalse()
        {
            // Arrange & Act
            var info = new AudioProcessInfo();

            // Assert
            Assert.False(info.IsActive);
        }

        [Fact]
        public void DefaultValues_SessionIdentifier_IsEmpty()
        {
            // Arrange & Act
            var info = new AudioProcessInfo();

            // Assert
            Assert.Equal(string.Empty, info.SessionIdentifier);
        }

        [Fact]
        public void DefaultValues_IconPath_IsNull()
        {
            // Arrange & Act
            var info = new AudioProcessInfo();

            // Assert
            Assert.Null(info.IconPath);
        }

        #endregion

        #region Property Initialization Tests

        [Fact]
        public void Init_ProcessId_SetsCorrectValue()
        {
            // Arrange & Act
            var info = new AudioProcessInfo { ProcessId = 1234 };

            // Assert
            Assert.Equal(1234, info.ProcessId);
        }

        [Fact]
        public void Init_ProcessName_SetsCorrectValue()
        {
            // Arrange & Act
            var info = new AudioProcessInfo { ProcessName = "TestProcess" };

            // Assert
            Assert.Equal("TestProcess", info.ProcessName);
        }

        [Fact]
        public void Init_DisplayName_SetsCorrectValue()
        {
            // Arrange & Act
            var info = new AudioProcessInfo { DisplayName = "Test Display Name" };

            // Assert
            Assert.Equal("Test Display Name", info.DisplayName);
        }

        [Fact]
        public void Init_Volume_SetsCorrectValue()
        {
            // Arrange & Act
            var info = new AudioProcessInfo { Volume = 0.75f };

            // Assert
            Assert.Equal(0.75f, info.Volume);
        }

        [Fact]
        public void Init_Volume_MinValue()
        {
            // Arrange & Act
            var info = new AudioProcessInfo { Volume = 0.0f };

            // Assert
            Assert.Equal(0.0f, info.Volume);
        }

        [Fact]
        public void Init_Volume_MaxValue()
        {
            // Arrange & Act
            var info = new AudioProcessInfo { Volume = 1.0f };

            // Assert
            Assert.Equal(1.0f, info.Volume);
        }

        [Fact]
        public void Init_IsMuted_True()
        {
            // Arrange & Act
            var info = new AudioProcessInfo { IsMuted = true };

            // Assert
            Assert.True(info.IsMuted);
        }

        [Fact]
        public void Init_IsActive_True()
        {
            // Arrange & Act
            var info = new AudioProcessInfo { IsActive = true };

            // Assert
            Assert.True(info.IsActive);
        }

        [Fact]
        public void Init_SessionIdentifier_SetsCorrectValue()
        {
            // Arrange & Act
            var info = new AudioProcessInfo { SessionIdentifier = "session-123" };

            // Assert
            Assert.Equal("session-123", info.SessionIdentifier);
        }

        [Fact]
        public void Init_IconPath_SetsCorrectValue()
        {
            // Arrange & Act
            var info = new AudioProcessInfo { IconPath = @"C:\app\icon.ico" };

            // Assert
            Assert.Equal(@"C:\app\icon.ico", info.IconPath);
        }

        #endregion

        #region Object Initializer Tests

        [Fact]
        public void FullInitialization_AllPropertiesSet()
        {
            // Arrange & Act
            var info = new AudioProcessInfo
            {
                ProcessId = 5678,
                ProcessName = "MyApp",
                DisplayName = "My Application",
                Volume = 0.5f,
                IsMuted = true,
                IsActive = true,
                SessionIdentifier = "session-abc",
                IconPath = @"C:\MyApp\icon.ico"
            };

            // Assert
            Assert.Equal(5678, info.ProcessId);
            Assert.Equal("MyApp", info.ProcessName);
            Assert.Equal("My Application", info.DisplayName);
            Assert.Equal(0.5f, info.Volume);
            Assert.True(info.IsMuted);
            Assert.True(info.IsActive);
            Assert.Equal("session-abc", info.SessionIdentifier);
            Assert.Equal(@"C:\MyApp\icon.ico", info.IconPath);
        }

        [Fact]
        public void PartialInitialization_OtherPropertiesRemainDefault()
        {
            // Arrange & Act
            var info = new AudioProcessInfo
            {
                ProcessId = 9999,
                ProcessName = "PartialApp"
            };

            // Assert
            Assert.Equal(9999, info.ProcessId);
            Assert.Equal("PartialApp", info.ProcessName);
            Assert.Equal(string.Empty, info.DisplayName);
            Assert.Equal(0.0f, info.Volume);
            Assert.False(info.IsMuted);
            Assert.False(info.IsActive);
            Assert.Equal(string.Empty, info.SessionIdentifier);
            Assert.Null(info.IconPath);
        }

        #endregion

        #region ToString Tests

        [Fact]
        public void ToString_UnmutedProcess_FormatsCorrectly()
        {
            // Arrange
            var info = new AudioProcessInfo
            {
                ProcessId = 1234,
                ProcessName = "TestApp",
                Volume = 0.75f,
                IsMuted = false
            };

            // Act
            string result = info.ToString();

            // Assert
            Assert.Equal("TestApp (PID: 1234) - 75% [Unmuted]", result);
        }

        [Fact]
        public void ToString_MutedProcess_FormatsCorrectly()
        {
            // Arrange
            var info = new AudioProcessInfo
            {
                ProcessId = 5678,
                ProcessName = "MutedApp",
                Volume = 0.5f,
                IsMuted = true
            };

            // Act
            string result = info.ToString();

            // Assert
            Assert.Equal("MutedApp (PID: 5678) - 50% [Muted]", result);
        }

        [Fact]
        public void ToString_ZeroVolume_ShowsZeroPercent()
        {
            // Arrange
            var info = new AudioProcessInfo
            {
                ProcessId = 1111,
                ProcessName = "SilentApp",
                Volume = 0.0f,
                IsMuted = false
            };

            // Act
            string result = info.ToString();

            // Assert
            Assert.Equal("SilentApp (PID: 1111) - 0% [Unmuted]", result);
        }

        [Fact]
        public void ToString_FullVolume_Shows100Percent()
        {
            // Arrange
            var info = new AudioProcessInfo
            {
                ProcessId = 2222,
                ProcessName = "LoudApp",
                Volume = 1.0f,
                IsMuted = false
            };

            // Act
            string result = info.ToString();

            // Assert
            Assert.Equal("LoudApp (PID: 2222) - 100% [Unmuted]", result);
        }

        [Fact]
        public void ToString_FractionalVolume_TruncatesCorrectly()
        {
            // Arrange
            var info = new AudioProcessInfo
            {
                ProcessId = 3333,
                ProcessName = "FractionApp",
                Volume = 0.333f,
                IsMuted = false
            };

            // Act
            string result = info.ToString();

            // Assert
            Assert.Equal("FractionApp (PID: 3333) - 33% [Unmuted]", result);
        }

        [Fact]
        public void ToString_EmptyProcessName_ShowsEmptyName()
        {
            // Arrange
            var info = new AudioProcessInfo
            {
                ProcessId = 4444,
                ProcessName = string.Empty,
                Volume = 0.5f,
                IsMuted = false
            };

            // Act
            string result = info.ToString();

            // Assert
            Assert.Equal(" (PID: 4444) - 50% [Unmuted]", result);
        }

        #endregion

        #region Sealed Class Tests

        [Fact]
        public void AudioProcessInfo_IsSealed()
        {
            // Assert
            Assert.True(typeof(AudioProcessInfo).IsSealed);
        }

        #endregion

        #region Edge Case Tests

        [Fact]
        public void ProcessId_NegativeValue_CanBeSet()
        {
            // Arrange & Act
            var info = new AudioProcessInfo { ProcessId = -1 };

            // Assert
            Assert.Equal(-1, info.ProcessId);
        }

        [Fact]
        public void ProcessId_MaxIntValue_CanBeSet()
        {
            // Arrange & Act
            var info = new AudioProcessInfo { ProcessId = int.MaxValue };

            // Assert
            Assert.Equal(int.MaxValue, info.ProcessId);
        }

        [Fact]
        public void Volume_NegativeValue_CanBeSet()
        {
            // Arrange & Act (model doesn't validate, that's service responsibility)
            var info = new AudioProcessInfo { Volume = -0.5f };

            // Assert
            Assert.Equal(-0.5f, info.Volume);
        }

        [Fact]
        public void Volume_AboveOne_CanBeSet()
        {
            // Arrange & Act (model doesn't validate, that's service responsibility)
            var info = new AudioProcessInfo { Volume = 1.5f };

            // Assert
            Assert.Equal(1.5f, info.Volume);
        }

        [Fact]
        public void ProcessName_WithSpecialCharacters_CanBeSet()
        {
            // Arrange
            string specialName = "Test App (x86) - v2.0 [Debug]";

            // Act
            var info = new AudioProcessInfo { ProcessName = specialName };

            // Assert
            Assert.Equal(specialName, info.ProcessName);
        }

        [Fact]
        public void DisplayName_WithUnicode_CanBeSet()
        {
            // Arrange
            string unicodeName = "测试应用 🎵 Müsik App";

            // Act
            var info = new AudioProcessInfo { DisplayName = unicodeName };

            // Assert
            Assert.Equal(unicodeName, info.DisplayName);
        }

        [Fact]
        public void IconPath_WithNetworkPath_CanBeSet()
        {
            // Arrange
            string networkPath = @"\\server\share\icons\app.ico";

            // Act
            var info = new AudioProcessInfo { IconPath = networkPath };

            // Assert
            Assert.Equal(networkPath, info.IconPath);
        }

        #endregion
    }
}
