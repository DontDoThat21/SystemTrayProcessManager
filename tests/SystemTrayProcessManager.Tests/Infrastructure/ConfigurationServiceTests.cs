using System.IO;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using SystemTrayProcessManager.Core.Models;
using SystemTrayProcessManager.Infrastructure.Services;

namespace SystemTrayProcessManager.Tests.Infrastructure
{
    /// <summary>
    /// Unit tests for the <see cref="ConfigurationService"/> class.
    /// Uses a temporary directory for file I/O isolation.
    /// </summary>
    public class ConfigurationServiceTests : IDisposable
    {
        private readonly Mock<ILogger<ConfigurationService>> _loggerMock;
        private readonly string _tempDir;
        private readonly ConfigurationService _service;

        public ConfigurationServiceTests()
        {
            _loggerMock = new Mock<ILogger<ConfigurationService>>();
            _tempDir = Path.Combine(Path.GetTempPath(), "STPM_Tests_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_tempDir);
            _service = new ConfigurationService(_loggerMock.Object, _tempDir);
        }

        public void Dispose()
        {
            try
            {
                if (Directory.Exists(_tempDir))
                {
                    Directory.Delete(_tempDir, true);
                }
            }
            catch
            {
                // Cleanup is best-effort
            }
        }

        #region Constructor

        [Fact]
        public void Constructor_NullLogger_ThrowsArgumentNullException()
        {
            var act = () => new ConfigurationService(null!, _tempDir);
            act.Should().Throw<ArgumentNullException>().WithParameterName("logger");
        }

        [Fact]
        public void Constructor_NullConfigDir_ThrowsArgumentException()
        {
            var act = () => new ConfigurationService(_loggerMock.Object, null!);
            act.Should().Throw<ArgumentException>();
        }

        [Fact]
        public void Constructor_EmptyConfigDir_ThrowsArgumentException()
        {
            var act = () => new ConfigurationService(_loggerMock.Object, "");
            act.Should().Throw<ArgumentException>();
        }

        [Fact]
        public void SettingsFilePath_ContainsSettingsJson()
        {
            _service.SettingsFilePath.Should().EndWith("settings.json");
        }

        [Fact]
        public void CurrentSettings_InitializesToDefaults()
        {
            _service.CurrentSettings.Should().NotBeNull();
            _service.CurrentSettings.IsFirstRun.Should().BeTrue();
        }

        #endregion

        #region LoadSettingsAsync

        [Fact]
        public async Task LoadSettingsAsync_NoFile_ReturnsDefaults()
        {
            var settings = await _service.LoadSettingsAsync();
            settings.Should().NotBeNull();
            settings.IsFirstRun.Should().BeTrue();
        }

        [Fact]
        public async Task LoadSettingsAsync_ValidFile_ReturnsSettings()
        {
            // Arrange: save settings first
            var original = new AppSettings
            {
                IsFirstRun = false,
                Theme = "Light",
                HotkeysEnabled = false,
                DefaultVolume = 0.5f
            };
            await _service.SaveSettingsAsync(original);

            // Act
            var loaded = await _service.LoadSettingsAsync();

            // Assert
            loaded.IsFirstRun.Should().BeFalse();
            loaded.Theme.Should().Be("Light");
            loaded.HotkeysEnabled.Should().BeFalse();
            loaded.DefaultVolume.Should().BeApproximately(0.5f, 0.01f);
        }

        [Fact]
        public async Task LoadSettingsAsync_CorruptedFile_ReturnsDefaults()
        {
            // Arrange: write invalid JSON
            Directory.CreateDirectory(_tempDir);
            await File.WriteAllTextAsync(Path.Combine(_tempDir, "settings.json"), "NOT VALID JSON{{{");

            // Act
            var settings = await _service.LoadSettingsAsync();

            // Assert - should recover gracefully
            settings.Should().NotBeNull();
        }

        [Fact]
        public async Task LoadSettingsAsync_CorruptedFile_RestoresFromBackup()
        {
            // Arrange: save twice so a backup is created
            var initial = new AppSettings { IsFirstRun = false, Theme = "Dark" };
            await _service.SaveSettingsAsync(initial);

            // Second save creates backup of the first
            var updated = new AppSettings { IsFirstRun = false, Theme = "Light" };
            await _service.SaveSettingsAsync(updated);

            // Now backup contains { Theme = "Light" } because backup is taken
            // before the second write. Actually backup contains the first save.
            // Backup = first save (Dark), primary = second save (Light)
            // When we corrupt primary, restore from backup should give Dark.

            // Corrupt the primary file
            await File.WriteAllTextAsync(Path.Combine(_tempDir, "settings.json"), "CORRUPT");

            // Act
            var loaded = await _service.LoadSettingsAsync();

            // Assert - should restore from backup (which is the first save)
            loaded.Should().NotBeNull();
            loaded.Theme.Should().Be("Dark");
        }

        [Fact]
        public async Task LoadSettingsAsync_SanitizesInvalidValues()
        {
            // Arrange: write settings with out-of-range values
            var invalidJson = """
            {
                "version": "1.0",
                "defaultVolume": 5.0,
                "processRefreshIntervalMs": 100,
                "theme": ""
            }
            """;
            Directory.CreateDirectory(_tempDir);
            await File.WriteAllTextAsync(Path.Combine(_tempDir, "settings.json"), invalidJson);

            // Act
            var loaded = await _service.LoadSettingsAsync();

            // Assert - values should be sanitized
            loaded.DefaultVolume.Should().BeLessOrEqualTo(1f);
            loaded.ProcessRefreshIntervalMs.Should().BeGreaterOrEqualTo(1000);
            loaded.Theme.Should().NotBeNullOrEmpty();
        }

        [Fact]
        public async Task LoadSettingsAsync_RaisesSettingsChanged()
        {
            AppSettings? changedSettings = null;
            _service.SettingsChanged += (s, settings) => changedSettings = settings;

            // Save a file first
            await _service.SaveSettingsAsync(new AppSettings { IsFirstRun = false });

            // Act
            await _service.LoadSettingsAsync();

            // Assert
            changedSettings.Should().NotBeNull();
        }

        #endregion

        #region SaveSettingsAsync

        [Fact]
        public async Task SaveSettingsAsync_NullSettings_ThrowsArgumentNullException()
        {
            var act = async () => await _service.SaveSettingsAsync(null!);
            await act.Should().ThrowAsync<ArgumentNullException>();
        }

        [Fact]
        public async Task SaveSettingsAsync_ValidSettings_ReturnsTrue()
        {
            var settings = new AppSettings { Theme = "Light" };
            var result = await _service.SaveSettingsAsync(settings);
            result.Should().BeTrue();
        }

        [Fact]
        public async Task SaveSettingsAsync_CreatesFile()
        {
            await _service.SaveSettingsAsync(AppSettings.Default);
            File.Exists(Path.Combine(_tempDir, "settings.json")).Should().BeTrue();
        }

        [Fact]
        public async Task SaveSettingsAsync_CreatesBackupOnSubsequentSave()
        {
            // First save
            await _service.SaveSettingsAsync(new AppSettings { Theme = "Dark" });

            // Second save should create backup
            await _service.SaveSettingsAsync(new AppSettings { Theme = "Light" });

            File.Exists(Path.Combine(_tempDir, "settings.backup.json")).Should().BeTrue();
        }

        [Fact]
        public async Task SaveSettingsAsync_UpdatesLastModified()
        {
            var before = DateTime.UtcNow.AddSeconds(-1);
            var settings = new AppSettings();

            await _service.SaveSettingsAsync(settings);

            settings.LastModified.Should().BeAfter(before);
        }

        [Fact]
        public async Task SaveSettingsAsync_UpdatesCurrentSettings()
        {
            var settings = new AppSettings { Theme = "Light" };
            await _service.SaveSettingsAsync(settings);
            _service.CurrentSettings.Theme.Should().Be("Light");
        }

        [Fact]
        public async Task SaveSettingsAsync_RaisesSettingsChanged()
        {
            AppSettings? changedSettings = null;
            _service.SettingsChanged += (s, settings) => changedSettings = settings;

            await _service.SaveSettingsAsync(new AppSettings { Theme = "Light" });

            changedSettings.Should().NotBeNull();
            changedSettings!.Theme.Should().Be("Light");
        }

        [Fact]
        public async Task SaveSettingsAsync_CreatesDirectory()
        {
            var subDir = Path.Combine(_tempDir, "nested", "sub");
            var service = new ConfigurationService(_loggerMock.Object, subDir);

            await service.SaveSettingsAsync(AppSettings.Default);

            Directory.Exists(subDir).Should().BeTrue();
        }

        #endregion

        #region BackupAsync

        [Fact]
        public async Task BackupAsync_NoFile_ReturnsTrue()
        {
            var result = await _service.BackupAsync();
            result.Should().BeTrue();
        }

        [Fact]
        public async Task BackupAsync_WithFile_CreatesBackup()
        {
            await _service.SaveSettingsAsync(AppSettings.Default);

            var result = await _service.BackupAsync();

            result.Should().BeTrue();
            File.Exists(Path.Combine(_tempDir, "settings.backup.json")).Should().BeTrue();
        }

        [Fact]
        public async Task BackupAsync_BackupContainsSameContent()
        {
            await _service.SaveSettingsAsync(new AppSettings { Theme = "Light" });
            await _service.BackupAsync();

            var primary = await File.ReadAllTextAsync(Path.Combine(_tempDir, "settings.json"));
            var backup = await File.ReadAllTextAsync(Path.Combine(_tempDir, "settings.backup.json"));

            primary.Should().Be(backup);
        }

        #endregion

        #region RestoreFromBackupAsync

        [Fact]
        public async Task RestoreFromBackupAsync_NoBackup_ReturnsNull()
        {
            var result = await _service.RestoreFromBackupAsync();
            result.Should().BeNull();
        }

        [Fact]
        public async Task RestoreFromBackupAsync_WithBackup_ReturnsSettings()
        {
            // Save then backup
            await _service.SaveSettingsAsync(new AppSettings { Theme = "Light" });
            await _service.BackupAsync();

            var result = await _service.RestoreFromBackupAsync();
            result.Should().NotBeNull();
            result!.Theme.Should().Be("Light");
        }

        [Fact]
        public async Task RestoreFromBackupAsync_UpdatesCurrentSettings()
        {
            await _service.SaveSettingsAsync(new AppSettings { Theme = "Light" });
            await _service.BackupAsync();

            // Change current settings
            await _service.SaveSettingsAsync(new AppSettings { Theme = "Dark" });

            // Restore
            await _service.RestoreFromBackupAsync();
            _service.CurrentSettings.Theme.Should().Be("Light");
        }

        [Fact]
        public async Task RestoreFromBackupAsync_CorruptedBackup_ReturnsNull()
        {
            Directory.CreateDirectory(_tempDir);
            await File.WriteAllTextAsync(Path.Combine(_tempDir, "settings.backup.json"), "CORRUPT");

            var result = await _service.RestoreFromBackupAsync();
            result.Should().BeNull();
        }

        #endregion

        #region ResetToDefaultsAsync

        [Fact]
        public async Task ResetToDefaultsAsync_ReturnsDefaults()
        {
            // First save non-default settings
            await _service.SaveSettingsAsync(new AppSettings { Theme = "Light", HotkeysEnabled = false });

            // Reset
            var result = await _service.ResetToDefaultsAsync();

            result.Theme.Should().Be("Dark");
            result.HotkeysEnabled.Should().BeTrue();
            result.IsFirstRun.Should().BeFalse(); // Not first run when explicitly resetting
        }

        [Fact]
        public async Task ResetToDefaultsAsync_SavesDefaultsToFile()
        {
            await _service.SaveSettingsAsync(new AppSettings { Theme = "Light" });
            await _service.ResetToDefaultsAsync();

            var loaded = await _service.LoadSettingsAsync();
            loaded.Theme.Should().Be("Dark");
        }

        #endregion

        #region GetDefaults

        [Fact]
        public void GetDefaults_ReturnsNewInstance()
        {
            var a = _service.GetDefaults();
            var b = _service.GetDefaults();
            a.Should().NotBeSameAs(b);
        }

        [Fact]
        public void GetDefaults_ReturnsValidSettings()
        {
            var defaults = _service.GetDefaults();
            defaults.IsValid.Should().BeTrue();
        }

        #endregion

        #region File Existence Checks

        [Fact]
        public void SettingsFileExists_NoFile_ReturnsFalse()
        {
            _service.SettingsFileExists().Should().BeFalse();
        }

        [Fact]
        public async Task SettingsFileExists_AfterSave_ReturnsTrue()
        {
            await _service.SaveSettingsAsync(AppSettings.Default);
            _service.SettingsFileExists().Should().BeTrue();
        }

        [Fact]
        public void BackupFileExists_NoFile_ReturnsFalse()
        {
            _service.BackupFileExists().Should().BeFalse();
        }

        [Fact]
        public async Task BackupFileExists_AfterBackup_ReturnsTrue()
        {
            await _service.SaveSettingsAsync(AppSettings.Default);
            await _service.BackupAsync();
            _service.BackupFileExists().Should().BeTrue();
        }

        #endregion

        #region Export/Import

        [Fact]
        public async Task ExportAsync_NullPath_ReturnsFalse()
        {
            var result = await _service.ExportAsync(null!, AppSettings.Default);
            result.Should().BeFalse();
        }

        [Fact]
        public async Task ExportAsync_EmptyPath_ReturnsFalse()
        {
            var result = await _service.ExportAsync("", AppSettings.Default);
            result.Should().BeFalse();
        }

        [Fact]
        public async Task ExportAsync_NullSettings_ThrowsArgumentNullException()
        {
            var path = Path.Combine(_tempDir, "export.json");
            var act = async () => await _service.ExportAsync(path, null!);
            await act.Should().ThrowAsync<ArgumentNullException>();
        }

        [Fact]
        public async Task ExportAsync_ValidPath_ReturnsTrue()
        {
            var path = Path.Combine(_tempDir, "export.json");
            var result = await _service.ExportAsync(path, AppSettings.Default);
            result.Should().BeTrue();
            File.Exists(path).Should().BeTrue();
        }

        [Fact]
        public async Task ImportAsync_NullPath_ReturnsNull()
        {
            var result = await _service.ImportAsync(null!);
            result.Should().BeNull();
        }

        [Fact]
        public async Task ImportAsync_EmptyPath_ReturnsNull()
        {
            var result = await _service.ImportAsync("");
            result.Should().BeNull();
        }

        [Fact]
        public async Task ImportAsync_NonExistentFile_ReturnsNull()
        {
            var result = await _service.ImportAsync(Path.Combine(_tempDir, "nonexistent.json"));
            result.Should().BeNull();
        }

        [Fact]
        public async Task ImportAsync_CorruptedFile_ReturnsNull()
        {
            var path = Path.Combine(_tempDir, "corrupt.json");
            await File.WriteAllTextAsync(path, "NOT JSON");

            var result = await _service.ImportAsync(path);
            result.Should().BeNull();
        }

        [Fact]
        public async Task ImportAsync_ValidFile_ReturnsSettings()
        {
            var path = Path.Combine(_tempDir, "export.json");
            await _service.ExportAsync(path, new AppSettings { Theme = "Light" });

            var result = await _service.ImportAsync(path);
            result.Should().NotBeNull();
            result!.Theme.Should().Be("Light");
        }

        [Fact]
        public async Task ExportImport_RoundTrip_PreservesSettings()
        {
            var original = new AppSettings
            {
                IsFirstRun = false,
                Theme = "Light",
                HotkeysEnabled = false,
                DefaultVolume = 0.7f,
                ProcessRefreshIntervalMs = 10000,
                AnimationsEnabled = false
            };

            var path = Path.Combine(_tempDir, "roundtrip.json");
            await _service.ExportAsync(path, original);
            var imported = await _service.ImportAsync(path);

            imported.Should().NotBeNull();
            imported!.Theme.Should().Be(original.Theme);
            imported.HotkeysEnabled.Should().Be(original.HotkeysEnabled);
            imported.DefaultVolume.Should().BeApproximately(original.DefaultVolume, 0.01f);
            imported.ProcessRefreshIntervalMs.Should().Be(original.ProcessRefreshIntervalMs);
            imported.AnimationsEnabled.Should().Be(original.AnimationsEnabled);
        }

        #endregion
    }
}
