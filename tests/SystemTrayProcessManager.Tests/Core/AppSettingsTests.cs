using FluentAssertions;
using SystemTrayProcessManager.Core.Models;

namespace SystemTrayProcessManager.Tests.Core
{
    /// <summary>
    /// Unit tests for the <see cref="AppSettings"/> model.
    /// </summary>
    public class AppSettingsTests
    {
        #region Default Values

        [Fact]
        public void Default_ReturnsExpectedDefaults()
        {
            var settings = AppSettings.Default;

            settings.Version.Should().Be(AppSettings.CurrentVersion);
            settings.IsFirstRun.Should().BeTrue();
            settings.HotkeysEnabled.Should().BeTrue();
            settings.DefaultVolume.Should().Be(1.0f);
            settings.MuteOnMinimize.Should().BeFalse();
            settings.StartWithWindows.Should().BeFalse();
            settings.StartMinimized.Should().BeTrue();
            settings.EnableNotifications.Should().BeTrue();
            settings.MinimizeToTray.Should().BeTrue();
            settings.ProcessRefreshIntervalMs.Should().Be(5000);
            settings.Theme.Should().Be("System");
            settings.ShowProcessIcons.Should().BeTrue();
            settings.AnimationsEnabled.Should().BeTrue();
        }

        [Fact]
        public void CurrentVersion_Is1Point0()
        {
            AppSettings.CurrentVersion.Should().Be("1.0");
        }

        [Fact]
        public void Default_IsValid()
        {
            var settings = AppSettings.Default;
            settings.IsValid.Should().BeTrue();
        }

        #endregion

        #region Validation

        [Fact]
        public void Validate_DefaultSettings_ReturnsNoErrors()
        {
            var settings = AppSettings.Default;
            settings.Validate().Should().BeEmpty();
        }

        [Fact]
        public void Validate_NullVersion_ReturnsError()
        {
            var settings = new AppSettings { Version = null! };
            settings.Validate().Should().Contain(e => e.Contains("Version"));
        }

        [Fact]
        public void Validate_EmptyVersion_ReturnsError()
        {
            var settings = new AppSettings { Version = "" };
            settings.Validate().Should().Contain(e => e.Contains("Version"));
        }

        [Fact]
        public void Validate_VolumeTooHigh_ReturnsError()
        {
            var settings = new AppSettings { DefaultVolume = 1.5f };
            settings.Validate().Should().Contain(e => e.Contains("DefaultVolume"));
        }

        [Fact]
        public void Validate_VolumeTooLow_ReturnsError()
        {
            var settings = new AppSettings { DefaultVolume = -0.1f };
            settings.Validate().Should().Contain(e => e.Contains("DefaultVolume"));
        }

        [Fact]
        public void Validate_VolumeAtBoundary_Zero_ReturnsNoError()
        {
            var settings = new AppSettings { DefaultVolume = 0f };
            settings.Validate().Should().NotContain(e => e.Contains("DefaultVolume"));
        }

        [Fact]
        public void Validate_VolumeAtBoundary_One_ReturnsNoError()
        {
            var settings = new AppSettings { DefaultVolume = 1f };
            settings.Validate().Should().NotContain(e => e.Contains("DefaultVolume"));
        }

        [Fact]
        public void Validate_RefreshIntervalTooLow_ReturnsError()
        {
            var settings = new AppSettings { ProcessRefreshIntervalMs = 500 };
            settings.Validate().Should().Contain(e => e.Contains("ProcessRefreshIntervalMs"));
        }

        [Fact]
        public void Validate_RefreshIntervalTooHigh_ReturnsError()
        {
            var settings = new AppSettings { ProcessRefreshIntervalMs = 60000 };
            settings.Validate().Should().Contain(e => e.Contains("ProcessRefreshIntervalMs"));
        }

        [Fact]
        public void Validate_RefreshIntervalAtBoundary_1000_NoError()
        {
            var settings = new AppSettings { ProcessRefreshIntervalMs = 1000 };
            settings.Validate().Should().NotContain(e => e.Contains("ProcessRefreshIntervalMs"));
        }

        [Fact]
        public void Validate_RefreshIntervalAtBoundary_30000_NoError()
        {
            var settings = new AppSettings { ProcessRefreshIntervalMs = 30000 };
            settings.Validate().Should().NotContain(e => e.Contains("ProcessRefreshIntervalMs"));
        }

        [Fact]
        public void Validate_NullTheme_ReturnsError()
        {
            var settings = new AppSettings { Theme = null! };
            settings.Validate().Should().Contain(e => e.Contains("Theme"));
        }

        [Fact]
        public void Validate_InvalidTheme_ReturnsError()
        {
            var settings = new AppSettings { Theme = "Blue" };
            settings.Validate().Should().Contain(e => e.Contains("Theme"));
        }

        [Fact]
        public void Validate_DarkTheme_NoError()
        {
            var settings = new AppSettings { Theme = "Dark" };
            settings.Validate().Should().NotContain(e => e.Contains("Theme"));
        }

        [Fact]
        public void Validate_LightTheme_NoError()
        {
            var settings = new AppSettings { Theme = "Light" };
            settings.Validate().Should().NotContain(e => e.Contains("Theme"));
        }

        [Fact]
        public void IsValid_WithInvalidVolume_ReturnsFalse()
        {
            var settings = new AppSettings { DefaultVolume = 2f };
            settings.IsValid.Should().BeFalse();
        }

        [Fact]
        public void IsValid_WithInvalidRefreshInterval_ReturnsFalse()
        {
            var settings = new AppSettings { ProcessRefreshIntervalMs = 100 };
            settings.IsValid.Should().BeFalse();
        }

        [Fact]
        public void IsValid_WithNullTheme_ReturnsFalse()
        {
            var settings = new AppSettings { Theme = null! };
            settings.IsValid.Should().BeFalse();
        }

        [Fact]
        public void IsValid_WithEmptyVersion_ReturnsFalse()
        {
            var settings = new AppSettings { Version = "" };
            settings.IsValid.Should().BeFalse();
        }

        #endregion

        #region Clone

        [Fact]
        public void Clone_CreatesDeepCopy()
        {
            var original = new AppSettings
            {
                IsFirstRun = false,
                HotkeysEnabled = false,
                DefaultVolume = 0.5f,
                MuteOnMinimize = true,
                StartWithWindows = true,
                StartMinimized = false,
                EnableNotifications = false,
                MinimizeToTray = false,
                ProcessRefreshIntervalMs = 10000,
                Theme = "Light",
                ShowProcessIcons = false,
                AnimationsEnabled = false
            };

            var clone = original.Clone();

            clone.Should().NotBeSameAs(original);
            clone.Equals(original).Should().BeTrue();
        }

        [Fact]
        public void Clone_ModifyingClone_DoesNotAffectOriginal()
        {
            var original = AppSettings.Default;
            var clone = original.Clone();

            clone.Theme = "Light";
            clone.HotkeysEnabled = false;

            original.Theme.Should().Be("System");
            original.HotkeysEnabled.Should().BeTrue();
        }

        #endregion

        #region Sanitize

        [Fact]
        public void Sanitize_ClampsTooHighVolume()
        {
            var settings = new AppSettings { DefaultVolume = 2f };
            var sanitized = settings.Sanitize();
            sanitized.DefaultVolume.Should().Be(1f);
        }

        [Fact]
        public void Sanitize_ClampsTooLowVolume()
        {
            var settings = new AppSettings { DefaultVolume = -1f };
            var sanitized = settings.Sanitize();
            sanitized.DefaultVolume.Should().Be(0f);
        }

        [Fact]
        public void Sanitize_ClampsTooLowRefreshInterval()
        {
            var settings = new AppSettings { ProcessRefreshIntervalMs = 100 };
            var sanitized = settings.Sanitize();
            sanitized.ProcessRefreshIntervalMs.Should().Be(1000);
        }

        [Fact]
        public void Sanitize_ClampsTooHighRefreshInterval()
        {
            var settings = new AppSettings { ProcessRefreshIntervalMs = 60000 };
            var sanitized = settings.Sanitize();
            sanitized.ProcessRefreshIntervalMs.Should().Be(30000);
        }

        [Fact]
        public void Sanitize_FixesNullTheme()
        {
            var settings = new AppSettings { Theme = null! };
            var sanitized = settings.Sanitize();
            sanitized.Theme.Should().Be("System");
        }

        [Fact]
        public void Sanitize_FixesEmptyTheme()
        {
            var settings = new AppSettings { Theme = "" };
            var sanitized = settings.Sanitize();
            sanitized.Theme.Should().Be("System");
        }

        [Fact]
        public void Sanitize_FixesNullVersion()
        {
            var settings = new AppSettings { Version = null! };
            var sanitized = settings.Sanitize();
            sanitized.Version.Should().Be(AppSettings.CurrentVersion);
        }

        [Fact]
        public void Sanitize_PreservesValidValues()
        {
            var settings = new AppSettings
            {
                DefaultVolume = 0.7f,
                ProcessRefreshIntervalMs = 3000,
                Theme = "Light"
            };

            var sanitized = settings.Sanitize();
            sanitized.DefaultVolume.Should().Be(0.7f);
            sanitized.ProcessRefreshIntervalMs.Should().Be(3000);
            sanitized.Theme.Should().Be("Light");
        }

        [Fact]
        public void Sanitize_ReturnsNewInstance()
        {
            var settings = AppSettings.Default;
            var sanitized = settings.Sanitize();
            sanitized.Should().NotBeSameAs(settings);
        }

        #endregion

        #region Equality

        [Fact]
        public void Equals_SameValues_ReturnsTrue()
        {
            var a = AppSettings.Default;
            var b = AppSettings.Default;
            a.Equals(b).Should().BeTrue();
        }

        [Fact]
        public void Equals_DifferentTheme_ReturnsFalse()
        {
            var a = new AppSettings { Theme = "Dark" };
            var b = new AppSettings { Theme = "Light" };
            a.Equals(b).Should().BeFalse();
        }

        [Fact]
        public void Equals_DifferentVolume_ReturnsFalse()
        {
            var a = new AppSettings { DefaultVolume = 0.5f };
            var b = new AppSettings { DefaultVolume = 0.8f };
            a.Equals(b).Should().BeFalse();
        }

        [Fact]
        public void Equals_Null_ReturnsFalse()
        {
            var settings = AppSettings.Default;
            settings.Equals(null).Should().BeFalse();
        }

        [Fact]
        public void Equals_SameReference_ReturnsTrue()
        {
            var settings = AppSettings.Default;
            settings.Equals(settings).Should().BeTrue();
        }

        [Fact]
        public void Equals_Object_SameValues_ReturnsTrue()
        {
            var a = AppSettings.Default;
            object b = AppSettings.Default;
            a.Equals(b).Should().BeTrue();
        }

        [Fact]
        public void Equals_Object_DifferentType_ReturnsFalse()
        {
            var settings = AppSettings.Default;
            settings.Equals("not a settings").Should().BeFalse();
        }

        [Fact]
        public void GetHashCode_SameValues_SameHash()
        {
            var a = AppSettings.Default;
            var b = AppSettings.Default;
            a.GetHashCode().Should().Be(b.GetHashCode());
        }

        [Fact]
        public void GetHashCode_DifferentValues_DifferentHash()
        {
            var a = new AppSettings { Theme = "Dark" };
            var b = new AppSettings { Theme = "Light" };
            a.GetHashCode().Should().NotBe(b.GetHashCode());
        }

        #endregion

        #region ToString

        [Fact]
        public void ToString_ContainsVersion()
        {
            var settings = AppSettings.Default;
            settings.ToString().Should().Contain("1.0");
        }

        [Fact]
        public void ToString_ContainsTheme()
        {
            var settings = new AppSettings { Theme = "Light" };
            settings.ToString().Should().Contain("Light");
        }

        [Fact]
        public void ToString_ContainsHotkeyStatus()
        {
            var settings = new AppSettings { HotkeysEnabled = true };
            settings.ToString().Should().Contain("True");
        }

        #endregion
    }
}
