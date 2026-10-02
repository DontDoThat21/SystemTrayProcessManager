using System.IO;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using SystemTrayProcessManager.Core.Models;
using SystemTrayProcessManager.Core.Services;
using SystemTrayProcessManager.Infrastructure.Services;

namespace SystemTrayProcessManager.Tests.Infrastructure;

public class SavedApplicationPersistenceTests
{
    [Fact]
    public async Task CatalogueSurvivesHotkeyDeletionAndServiceRestart()
    {
        var directory = Path.Combine(Path.GetTempPath(), "saved-app-tests-" + Guid.NewGuid());
        try
        {
            var service = new HotkeyConfigurationService(NullLogger<HotkeyConfigurationService>.Instance, directory);
            var original = new HotkeyConfiguration(new[] { new HotkeyConfigItem { TargetProcessName = "sample.exe" } });
            original.Applications["sample"] = @"C:\Apps\sample.exe";
            Assert.True(await service.SaveConfigurationAsync(original));
            Assert.True(await service.SaveConfigurationAsync(new HotkeyConfiguration()));
            var restarted = new HotkeyConfigurationService(NullLogger<HotkeyConfigurationService>.Instance, directory);
            var loaded = await restarted.LoadConfigurationAsync();
            Assert.Empty(loaded.Items);
            Assert.Equal(@"C:\Apps\sample.exe", loaded.Applications["sample"]);
            var cloned = loaded.Clone();
            cloned.Applications.Clear();
            Assert.Single(loaded.Applications);
        }
        finally { if (Directory.Exists(directory)) Directory.Delete(directory, true); }
    }

    [Fact]
    public async Task LegacyTargetIsRememberedWhenItsLastHotkeyIsDeleted()
    {
        var directory = Path.Combine(Path.GetTempPath(), "saved-app-tests-" + Guid.NewGuid());
        try
        {
            Directory.CreateDirectory(directory);
            await File.WriteAllTextAsync(Path.Combine(directory, "hotkeys.json"),
                """{"items":[{"targetProcessName":"legacy-test-app.exe"}]}""");
            var service = new HotkeyConfigurationService(NullLogger<HotkeyConfigurationService>.Instance, directory);
            Assert.True(await service.SaveConfigurationAsync(new HotkeyConfiguration()));
            Assert.True((await service.LoadConfigurationAsync()).Applications.ContainsKey("legacy-test-app"));
        }
        finally { if (Directory.Exists(directory)) Directory.Delete(directory, true); }
    }

    [Theory]
    [InlineData("")]
    [InlineData("sample.exe")]
    [InlineData("cmd.exe /c echo test")]
    [InlineData("C:\\missing-app-test\\app.exe")]
    public async Task LaunchRejectsInvalidPaths(string path)
    {
        using var service = new ProcessMonitorService(NullLogger<ProcessMonitorService>.Instance, Mock.Of<IIconExtractor>());
        Assert.False(await service.LaunchAsync(path));
    }
}
