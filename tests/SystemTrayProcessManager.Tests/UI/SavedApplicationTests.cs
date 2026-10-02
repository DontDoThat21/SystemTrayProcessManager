using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using SystemTrayProcessManager.Core.Enums;
using SystemTrayProcessManager.Core.Models;
using SystemTrayProcessManager.Core.Services;
using SystemTrayProcessManager.Infrastructure.Services;
using SystemTrayProcessManager.UI.ViewModels;

namespace SystemTrayProcessManager.Tests.UI;

public class SavedApplicationTests
{
    private readonly Mock<IProcessService> _processes = new();
    private readonly Mock<IWindowService> _windows = new();
    private readonly Mock<IHotkeyConfigurationService> _configuration = new();
    private readonly HotkeyConfiguration _saved = new();

    public SavedApplicationTests()
    {
        _processes.Setup(p => p.GetRunningProcessesAsync(default)).ReturnsAsync(Array.Empty<ProcessInfo>());
        _configuration.Setup(c => c.LoadConfigurationAsync()).ReturnsAsync(_saved);
        _configuration.Setup(c => c.SaveConfigurationAsync(It.IsAny<HotkeyConfiguration>())).ReturnsAsync(true);
    }

    private MainViewModel Workspace() => new(_processes.Object, _windows.Object, Mock.Of<IAudioService>(),
        Mock.Of<INotificationService>(), NullLogger<MainViewModel>.Instance, _configuration.Object);

    [Fact]
    public async Task DisabledLegacyShortcutsRemainVisibleAndDeduplicate()
    {
        _saved.Items.Add(new() { TargetProcessName = "Player.exe", IsEnabled = false });
        _saved.Items.Add(new() { TargetProcessName = "player" });
        using var workspace = Workspace();
        await workspace.RefreshProcessesCommand.ExecuteAsync(null);
        var card = Assert.Single(workspace.ProcessCards);
        Assert.False(card.IsRunning);
        Assert.True(card.LaunchCommand.CanExecute(null));
        Assert.False(card.CloseCommand.CanExecute(null));
        _saved.Items.Clear();
        await workspace.RefreshProcessesCommand.ExecuteAsync(null);
        Assert.Single(workspace.ProcessCards);
        workspace.SearchText = "player";
        Assert.Single(workspace.ProcessCards);
    }

    [Fact]
    public async Task StartAndStopReplacePlaceholderWithoutLosingApplication()
    {
        _saved.Applications["player"] = @"C:\Apps\player.exe";
        using var workspace = Workspace();
        await workspace.RefreshProcessesCommand.ExecuteAsync(null);
        var running = new ProcessInfo { Name = "player", ProcessId = 42, WindowHandle = (IntPtr)123 };
        _processes.Raise(p => p.ProcessStarted += null, _processes.Object, running);
        Assert.True(Assert.Single(workspace.ProcessCards).IsRunning);
        _processes.Raise(p => p.ProcessStopped += null, new ProcessStoppedEventArgs(42, "player"));
        Assert.False(Assert.Single(workspace.ProcessCards).IsRunning);
    }

    [Fact]
    public async Task LaunchSelectsAndPersistsMissingPath()
    {
        _saved.Applications["player"] = null;
        _processes.Setup(p => p.LaunchAsync(@"C:\Apps\player.exe")).ReturnsAsync(true);
        using var workspace = Workspace();
        workspace.SelectExecutablePath = () => @"C:\Apps\player.exe";
        await workspace.RefreshProcessesCommand.ExecuteAsync(null);
        await Assert.Single(workspace.ProcessCards).LaunchCommand.ExecuteAsync(null);
        Assert.Equal(@"C:\Apps\player.exe", _saved.Applications["player"]);
        _processes.Verify(p => p.LaunchAsync(@"C:\Apps\player.exe"), Times.Once);
    }

    [Theory]
    [InlineData(ProcessActionType.Open)]
    [InlineData(ProcessActionType.ToggleOpenClose)]
    public async Task StoppedApplicationHotkeyLaunchesExactSavedPath(ProcessActionType action)
    {
        _saved.Applications["unique-test-app"] = @"C:\Apps\unique-test-app.exe";
        _processes.Setup(p => p.LaunchAsync(@"C:\Apps\unique-test-app.exe")).ReturnsAsync(true);
        using var actions = Actions();
        var result = await actions.ExecutePinnedActionAsync(action, "unique-test-app.exe");
        Assert.True(result.Success);
        _processes.Verify(p => p.LaunchAsync(@"C:\Apps\unique-test-app.exe"), Times.Once);
    }

    [Theory]
    [InlineData(ProcessActionType.Open)]
    [InlineData(ProcessActionType.ToggleOpenClose)]
    public async Task RunningApplicationFocusesOrClosesWithoutLaunching(ProcessActionType action)
    {
        _processes.Setup(p => p.GetRunningProcessesAsync(default)).ReturnsAsync(new[]
        {
            new ProcessInfo { Name = "player", ProcessId = 42, WindowHandle = (IntPtr)123 }
        });
        _windows.Setup(w => w.IsValidWindow((IntPtr)123)).Returns(true);
        _windows.Setup(w => w.CloseAsync((IntPtr)123, false)).ReturnsAsync(true);
        _windows.Setup(w => w.BringToFrontAsync((IntPtr)123)).ReturnsAsync(true);
        using var actions = Actions();
        Assert.True((await actions.ExecutePinnedActionAsync(action, "player")).Success);
        _processes.Verify(p => p.LaunchAsync(It.IsAny<string>()), Times.Never);
        _windows.Verify(w => w.CloseAsync((IntPtr)123, false), action == ProcessActionType.ToggleOpenClose ? Times.Once() : Times.Never());
    }

    [Fact]
    public async Task ToggleDoesNotClosePartialNameMatchAndReportsMissingPath()
    {
        _processes.Setup(p => p.GetRunningProcessesAsync(default)).ReturnsAsync(new[]
        {
            new ProcessInfo { Name = "unique-test-app-helper", ProcessId = 42, WindowHandle = (IntPtr)123 }
        });
        using var actions = Actions();
        Assert.False((await actions.ExecutePinnedActionAsync(ProcessActionType.ToggleOpenClose, "unique-test-app")).Success);
        _windows.Verify(w => w.CloseAsync(It.IsAny<IntPtr>(), It.IsAny<bool>()), Times.Never);
        _processes.Verify(p => p.LaunchAsync(It.IsAny<string>()), Times.Never);
    }

    private ActionMappingService Actions() => new(NullLogger<ActionMappingService>.Instance,
        Mock.Of<IHotkeyService>(), _configuration.Object, _windows.Object, Mock.Of<IAudioService>(), _processes.Object);

    [Theory]
    [InlineData(ProcessActionType.Open)]
    [InlineData(ProcessActionType.ToggleOpenClose)]
    public async Task LaunchRequiresTargetAndReportsLaunchFailure(ProcessActionType action)
    {
        using var actions = Actions();
        Assert.False((await actions.ExecuteQuickActionAsync(action)).Success);
        _saved.Applications["unique-test-app"] = @"C:\Apps\unique-test-app.exe";
        _processes.Setup(p => p.LaunchAsync(It.IsAny<string>())).ReturnsAsync(false);
        Assert.False((await actions.ExecutePinnedActionAsync(action, "unique-test-app")).Success);
    }
}
