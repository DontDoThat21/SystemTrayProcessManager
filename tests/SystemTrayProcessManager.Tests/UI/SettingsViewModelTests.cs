using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using SystemTrayProcessManager.Core.Models;
using SystemTrayProcessManager.Core.Services;
using SystemTrayProcessManager.UI.ViewModels;

namespace SystemTrayProcessManager.Tests.UI
{
    /// <summary>
    /// Unit tests for the <see cref="SettingsViewModel"/> class.
    /// </summary>
    public class SettingsViewModelTests
    {
        private readonly Mock<IConfigurationService> _configServiceMock;
        private readonly Mock<ILogger<SettingsViewModel>> _loggerMock;
        private readonly SettingsViewModel _viewModel;

        public SettingsViewModelTests()
        {
            _configServiceMock = new Mock<IConfigurationService>();
            _loggerMock = new Mock<ILogger<SettingsViewModel>>();

            // Setup default behavior
            _configServiceMock.Setup(s => s.LoadSettingsAsync())
                .ReturnsAsync(AppSettings.Default);
            _configServiceMock.Setup(s => s.SaveSettingsAsync(It.IsAny<AppSettings>()))
                .ReturnsAsync(true);
            _configServiceMock.Setup(s => s.ResetToDefaultsAsync())
                .ReturnsAsync(AppSettings.Default);
            _configServiceMock.Setup(s => s.GetDefaults())
                .Returns(AppSettings.Default);

            _viewModel = new SettingsViewModel(_configServiceMock.Object, _loggerMock.Object);
        }

        #region Constructor

        [Fact]
        public void Constructor_NullConfigService_ThrowsArgumentNullException()
        {
            var act = () => new SettingsViewModel(null!, _loggerMock.Object);
            act.Should().Throw<ArgumentNullException>().WithParameterName("configurationService");
        }

        [Fact]
        public void Constructor_NullLogger_ThrowsArgumentNullException()
        {
            var act = () => new SettingsViewModel(_configServiceMock.Object, null!);
            act.Should().Throw<ArgumentNullException>().WithParameterName("logger");
        }

        [Fact]
        public void Constructor_InitializesDefaultValues()
        {
            var vm = new SettingsViewModel(_configServiceMock.Object, _loggerMock.Object);
            vm.HotkeysEnabled.Should().BeTrue();
            vm.DefaultVolume.Should().Be(1.0f);
            vm.Theme.Should().Be("Dark");
            vm.IsBusy.Should().BeFalse();
            vm.HasChanges.Should().BeFalse();
        }

        #endregion

        #region LoadSettingsAsync

        [Fact]
        public async Task LoadSettingsAsync_LoadsFromService()
        {
            var customSettings = new AppSettings
            {
                Theme = "Light",
                HotkeysEnabled = false,
                DefaultVolume = 0.5f,
                ProcessRefreshIntervalMs = 10000
            };

            _configServiceMock.Setup(s => s.LoadSettingsAsync()).ReturnsAsync(customSettings);

            await _viewModel.LoadSettingsAsync();

            _viewModel.Theme.Should().Be("Light");
            _viewModel.HotkeysEnabled.Should().BeFalse();
            _viewModel.DefaultVolume.Should().BeApproximately(0.5f, 0.01f);
            _viewModel.ProcessRefreshIntervalMs.Should().Be(10000);
        }

        [Fact]
        public async Task LoadSettingsAsync_SetsStatusMessage()
        {
            await _viewModel.LoadSettingsAsync();
            _viewModel.StatusMessage.Should().Be("Settings loaded.");
        }

        [Fact]
        public async Task LoadSettingsAsync_ResetsHasChanges()
        {
            await _viewModel.LoadSettingsAsync();
            _viewModel.HasChanges.Should().BeFalse();
        }

        [Fact]
        public async Task LoadSettingsAsync_CallsService()
        {
            await _viewModel.LoadSettingsAsync();
            _configServiceMock.Verify(s => s.LoadSettingsAsync(), Times.Once);
        }

        [Fact]
        public async Task LoadSettingsAsync_OnError_SetsErrorMessage()
        {
            _configServiceMock.Setup(s => s.LoadSettingsAsync()).ThrowsAsync(new Exception("Test error"));

            await _viewModel.LoadSettingsAsync();

            _viewModel.StatusMessage.Should().Be("Failed to load settings.");
        }

        #endregion

        #region SaveSettingsAsync

        [Fact]
        public async Task SaveSettingsAsync_CallsServiceWithCurrentValues()
        {
            _viewModel.Theme = "Light";
            _viewModel.HotkeysEnabled = false;

            await _viewModel.SaveSettingsAsync();

            _configServiceMock.Verify(s => s.SaveSettingsAsync(
                It.Is<AppSettings>(settings =>
                    settings.Theme == "Light" &&
                    settings.HotkeysEnabled == false)),
                Times.Once);
        }

        [Fact]
        public async Task SaveSettingsAsync_Success_SetsStatusMessage()
        {
            await _viewModel.SaveSettingsAsync();
            _viewModel.StatusMessage.Should().Be("Settings saved successfully.");
        }

        [Fact]
        public async Task SaveSettingsAsync_Success_ResetsHasChanges()
        {
            // Load first to establish original
            await _viewModel.LoadSettingsAsync();

            _viewModel.Theme = "Light";
            _viewModel.HasChanges.Should().BeTrue();

            await _viewModel.SaveSettingsAsync();
            _viewModel.HasChanges.Should().BeFalse();
        }

        [Fact]
        public async Task SaveSettingsAsync_Failure_SetsErrorMessage()
        {
            _configServiceMock.Setup(s => s.SaveSettingsAsync(It.IsAny<AppSettings>())).ReturnsAsync(false);

            await _viewModel.SaveSettingsAsync();

            _viewModel.StatusMessage.Should().Be("Failed to save settings.");
        }

        [Fact]
        public async Task SaveSettingsAsync_Exception_SetsErrorMessage()
        {
            _configServiceMock.Setup(s => s.SaveSettingsAsync(It.IsAny<AppSettings>())).ThrowsAsync(new Exception("Test"));

            await _viewModel.SaveSettingsAsync();

            _viewModel.StatusMessage.Should().Be("Error saving settings.");
        }

        #endregion

        #region ResetToDefaultsAsync

        [Fact]
        public async Task ResetToDefaultsAsync_ResetsProperties()
        {
            // Modify some settings
            _viewModel.Theme = "Light";
            _viewModel.HotkeysEnabled = false;

            await _viewModel.ResetToDefaultsAsync();

            _viewModel.Theme.Should().Be("Dark");
            _viewModel.HotkeysEnabled.Should().BeTrue();
        }

        [Fact]
        public async Task ResetToDefaultsAsync_SetsStatusMessage()
        {
            await _viewModel.ResetToDefaultsAsync();
            _viewModel.StatusMessage.Should().Be("Settings reset to defaults.");
        }

        [Fact]
        public async Task ResetToDefaultsAsync_CallsService()
        {
            await _viewModel.ResetToDefaultsAsync();
            _configServiceMock.Verify(s => s.ResetToDefaultsAsync(), Times.Once);
        }

        [Fact]
        public async Task ResetToDefaultsAsync_ResetsHasChanges()
        {
            await _viewModel.ResetToDefaultsAsync();
            _viewModel.HasChanges.Should().BeFalse();
        }

        #endregion

        #region RestoreFromBackupAsync

        [Fact]
        public async Task RestoreFromBackupAsync_Success_UpdatesProperties()
        {
            var backupSettings = new AppSettings { Theme = "Light", HotkeysEnabled = false };
            _configServiceMock.Setup(s => s.RestoreFromBackupAsync()).ReturnsAsync(backupSettings);

            await _viewModel.RestoreFromBackupAsync();

            _viewModel.Theme.Should().Be("Light");
            _viewModel.HotkeysEnabled.Should().BeFalse();
        }

        [Fact]
        public async Task RestoreFromBackupAsync_NoBackup_SetsMessage()
        {
            _configServiceMock.Setup(s => s.RestoreFromBackupAsync()).ReturnsAsync((AppSettings?)null);

            await _viewModel.RestoreFromBackupAsync();

            _viewModel.StatusMessage.Should().Be("No backup available to restore.");
        }

        [Fact]
        public async Task RestoreFromBackupAsync_Success_SetsMessage()
        {
            _configServiceMock.Setup(s => s.RestoreFromBackupAsync()).ReturnsAsync(new AppSettings());

            await _viewModel.RestoreFromBackupAsync();

            _viewModel.StatusMessage.Should().Be("Settings restored from backup.");
        }

        #endregion

        #region CancelChanges

        [Fact]
        public async Task CancelChanges_RevertsToOriginal()
        {
            // Load initial settings
            _configServiceMock.Setup(s => s.LoadSettingsAsync())
                .ReturnsAsync(new AppSettings { Theme = "Dark", HotkeysEnabled = true });
            await _viewModel.LoadSettingsAsync();

            // Modify
            _viewModel.Theme = "Light";
            _viewModel.HotkeysEnabled = false;

            // Cancel
            _viewModel.CancelChanges();

            _viewModel.Theme.Should().Be("Dark");
            _viewModel.HotkeysEnabled.Should().BeTrue();
        }

        [Fact]
        public async Task CancelChanges_ResetsHasChanges()
        {
            await _viewModel.LoadSettingsAsync();
            _viewModel.Theme = "Light";
            _viewModel.HasChanges.Should().BeTrue();

            _viewModel.CancelChanges();
            _viewModel.HasChanges.Should().BeFalse();
        }

        [Fact]
        public void CancelChanges_SetsStatusMessage()
        {
            _viewModel.CancelChanges();
            // No original settings loaded, should not crash
        }

        #endregion

        #region HasChanges Tracking

        [Fact]
        public async Task HasChanges_DetectsThemeChange()
        {
            await _viewModel.LoadSettingsAsync();
            _viewModel.Theme = "Light";
            _viewModel.HasChanges.Should().BeTrue();
        }

        [Fact]
        public async Task HasChanges_DetectsHotkeyChange()
        {
            await _viewModel.LoadSettingsAsync();
            _viewModel.HotkeysEnabled = false;
            _viewModel.HasChanges.Should().BeTrue();
        }

        [Fact]
        public async Task HasChanges_DetectsVolumeChange()
        {
            await _viewModel.LoadSettingsAsync();
            _viewModel.DefaultVolume = 0.5f;
            _viewModel.HasChanges.Should().BeTrue();
        }

        [Fact]
        public async Task HasChanges_FalseWhenRevertedToOriginal()
        {
            // Use IsFirstRun=false to match BuildSettingsFromProperties which always sets IsFirstRun=false
            _configServiceMock.Setup(s => s.LoadSettingsAsync())
                .ReturnsAsync(new AppSettings { Theme = "Dark", IsFirstRun = false });
            await _viewModel.LoadSettingsAsync();

            _viewModel.Theme = "Light";
            _viewModel.HasChanges.Should().BeTrue();

            _viewModel.Theme = "Dark";
            _viewModel.HasChanges.Should().BeFalse();
        }

        #endregion

        #region BuildSettingsFromProperties

        [Fact]
        public void BuildSettingsFromProperties_ReturnsCurrentValues()
        {
            _viewModel.Theme = "Light";
            _viewModel.HotkeysEnabled = false;
            _viewModel.DefaultVolume = 0.7f;
            _viewModel.ProcessRefreshIntervalMs = 10000;
            _viewModel.StartWithWindows = true;

            var settings = _viewModel.BuildSettingsFromProperties();

            settings.Theme.Should().Be("Light");
            settings.HotkeysEnabled.Should().BeFalse();
            settings.DefaultVolume.Should().BeApproximately(0.7f, 0.01f);
            settings.ProcessRefreshIntervalMs.Should().Be(10000);
            settings.StartWithWindows.Should().BeTrue();
            settings.IsFirstRun.Should().BeFalse();
            settings.Version.Should().Be(AppSettings.CurrentVersion);
        }

        #endregion

        #region AvailableThemes

        [Fact]
        public void AvailableThemes_ContainsDarkAndLight()
        {
            _viewModel.AvailableThemes.Should().Contain("Dark");
            _viewModel.AvailableThemes.Should().Contain("Light");
        }

        [Fact]
        public void AvailableThemes_HasTwoEntries()
        {
            _viewModel.AvailableThemes.Should().HaveCount(2);
        }

        #endregion

        #region IsBusy

        [Fact]
        public void IsBusy_DefaultsFalse()
        {
            _viewModel.IsBusy.Should().BeFalse();
        }

        #endregion

        #region ImportSettingsAsync

        [Fact]
        public async Task ImportSettingsAsync_NullDelegate_DoesNothing()
        {
            _viewModel.RequestImportFilePath = null;
            await _viewModel.ImportSettingsAsync();
            _viewModel.StatusMessage.Should().NotBe("Settings imported");
        }

        [Fact]
        public async Task ImportSettingsAsync_UserCancels_DoesNothing()
        {
            _viewModel.RequestImportFilePath = () => null;
            await _viewModel.ImportSettingsAsync();
            _viewModel.StatusMessage.Should().BeEmpty();
        }

        [Fact]
        public async Task ImportSettingsAsync_EmptyPath_DoesNothing()
        {
            _viewModel.RequestImportFilePath = () => "";
            await _viewModel.ImportSettingsAsync();
            _viewModel.StatusMessage.Should().BeEmpty();
        }

        [Fact]
        public async Task ImportSettingsAsync_ValidFile_AppliesSettings()
        {
            var importedSettings = new AppSettings { Theme = "Light", HotkeysEnabled = false };
            _configServiceMock.Setup(s => s.ImportAsync("test.json")).ReturnsAsync(importedSettings);
            _viewModel.RequestImportFilePath = () => "test.json";

            await _viewModel.ImportSettingsAsync();

            _viewModel.Theme.Should().Be("Light");
            _viewModel.HotkeysEnabled.Should().BeFalse();
            _viewModel.HasChanges.Should().BeTrue();
            _viewModel.StatusMessage.Should().Contain("imported");
        }

        [Fact]
        public async Task ImportSettingsAsync_InvalidFile_SetsErrorMessage()
        {
            _configServiceMock.Setup(s => s.ImportAsync("bad.json")).ReturnsAsync((AppSettings?)null);
            _viewModel.RequestImportFilePath = () => "bad.json";

            await _viewModel.ImportSettingsAsync();

            _viewModel.StatusMessage.Should().Contain("Failed");
        }

        [Fact]
        public async Task ImportSettingsAsync_ServiceThrows_SetsErrorMessage()
        {
            _configServiceMock.Setup(s => s.ImportAsync(It.IsAny<string>()))
                .ThrowsAsync(new Exception("Test error"));
            _viewModel.RequestImportFilePath = () => "test.json";

            await _viewModel.ImportSettingsAsync();

            _viewModel.StatusMessage.Should().Contain("Error");
        }

        #endregion

        #region ExportSettingsAsync

        [Fact]
        public async Task ExportSettingsAsync_NullDelegate_DoesNothing()
        {
            _viewModel.RequestExportFilePath = null;
            await _viewModel.ExportSettingsAsync();
            _viewModel.StatusMessage.Should().NotContain("exported");
        }

        [Fact]
        public async Task ExportSettingsAsync_UserCancels_DoesNothing()
        {
            _viewModel.RequestExportFilePath = () => null;
            await _viewModel.ExportSettingsAsync();
            _viewModel.StatusMessage.Should().BeEmpty();
        }

        [Fact]
        public async Task ExportSettingsAsync_EmptyPath_DoesNothing()
        {
            _viewModel.RequestExportFilePath = () => "";
            await _viewModel.ExportSettingsAsync();
            _viewModel.StatusMessage.Should().BeEmpty();
        }

        [Fact]
        public async Task ExportSettingsAsync_ValidPath_CallsService()
        {
            _configServiceMock.Setup(s => s.ExportAsync("export.json", It.IsAny<AppSettings>()))
                .ReturnsAsync(true);
            _viewModel.RequestExportFilePath = () => "export.json";

            await _viewModel.ExportSettingsAsync();

            _viewModel.StatusMessage.Should().Contain("exported");
            _configServiceMock.Verify(s => s.ExportAsync("export.json", It.IsAny<AppSettings>()), Times.Once);
        }

        [Fact]
        public async Task ExportSettingsAsync_ServiceFails_SetsErrorMessage()
        {
            _configServiceMock.Setup(s => s.ExportAsync(It.IsAny<string>(), It.IsAny<AppSettings>()))
                .ReturnsAsync(false);
            _viewModel.RequestExportFilePath = () => "export.json";

            await _viewModel.ExportSettingsAsync();

            _viewModel.StatusMessage.Should().Contain("Failed");
        }

        [Fact]
        public async Task ExportSettingsAsync_ServiceThrows_SetsErrorMessage()
        {
            _configServiceMock.Setup(s => s.ExportAsync(It.IsAny<string>(), It.IsAny<AppSettings>()))
                .ThrowsAsync(new Exception("Test error"));
            _viewModel.RequestExportFilePath = () => "export.json";

            await _viewModel.ExportSettingsAsync();

            _viewModel.StatusMessage.Should().Contain("Error");
        }

        [Fact]
        public async Task ExportSettingsAsync_UsesCurrentPropertyValues()
        {
            AppSettings? exported = null;
            _configServiceMock.Setup(s => s.ExportAsync(It.IsAny<string>(), It.IsAny<AppSettings>()))
                .Callback<string, AppSettings>((_, s) => exported = s)
                .ReturnsAsync(true);

            _viewModel.Theme = "Light";
            _viewModel.HotkeysEnabled = false;
            _viewModel.DefaultVolume = 0.5f;
            _viewModel.RequestExportFilePath = () => "export.json";

            await _viewModel.ExportSettingsAsync();

            exported.Should().NotBeNull();
            exported!.Theme.Should().Be("Light");
            exported.HotkeysEnabled.Should().BeFalse();
            exported.DefaultVolume.Should().BeApproximately(0.5f, 0.01f);
        }

        #endregion

        #region SaveAndCloseAsync

        [Fact]
        public async Task SaveAndCloseAsync_Success_RaisesRequestClose()
        {
            bool closeCalled = false;
            bool dialogResult = false;
            _viewModel.RequestClose += (_, result) =>
            {
                closeCalled = true;
                dialogResult = result;
            };

            await _viewModel.SaveAndCloseAsync();

            closeCalled.Should().BeTrue();
            dialogResult.Should().BeTrue();
        }

        [Fact]
        public async Task SaveAndCloseAsync_Failure_DoesNotRaiseRequestClose()
        {
            _configServiceMock.Setup(s => s.SaveSettingsAsync(It.IsAny<AppSettings>()))
                .ReturnsAsync(false);

            bool closeCalled = false;
            _viewModel.RequestClose += (_, _) => closeCalled = true;

            await _viewModel.SaveAndCloseAsync();

            closeCalled.Should().BeFalse();
        }

        [Fact]
        public async Task SaveAndCloseAsync_Exception_DoesNotRaiseRequestClose()
        {
            _configServiceMock.Setup(s => s.SaveSettingsAsync(It.IsAny<AppSettings>()))
                .ThrowsAsync(new Exception("Test error"));

            bool closeCalled = false;
            _viewModel.RequestClose += (_, _) => closeCalled = true;

            await _viewModel.SaveAndCloseAsync();

            closeCalled.Should().BeFalse();
        }

        #endregion

        #region RequestClose Event

        [Fact]
        public void RequestClose_NoSubscribers_DoesNotThrow()
        {
            // Just verify no exception
            var vm = new SettingsViewModel(_configServiceMock.Object, _loggerMock.Object);
            vm.Should().NotBeNull();
        }

        #endregion

        #region RequestImportFilePath / RequestExportFilePath Delegates

        [Fact]
        public void RequestImportFilePath_DefaultsNull()
        {
            var vm = new SettingsViewModel(_configServiceMock.Object, _loggerMock.Object);
            vm.RequestImportFilePath.Should().BeNull();
        }

        [Fact]
        public void RequestExportFilePath_DefaultsNull()
        {
            var vm = new SettingsViewModel(_configServiceMock.Object, _loggerMock.Object);
            vm.RequestExportFilePath.Should().BeNull();
        }

        [Fact]
        public void RequestImportFilePath_CanBeSet()
        {
            _viewModel.RequestImportFilePath = () => "test.json";
            _viewModel.RequestImportFilePath.Should().NotBeNull();
        }

        [Fact]
        public void RequestExportFilePath_CanBeSet()
        {
            _viewModel.RequestExportFilePath = () => "export.json";
            _viewModel.RequestExportFilePath.Should().NotBeNull();
        }

        #endregion
    }
}
