using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using SystemTrayProcessManager.Core.Enums;
using SystemTrayProcessManager.Core.Models;
using SystemTrayProcessManager.Core.Services;
using SystemTrayProcessManager.Infrastructure.Services;
using SystemTrayProcessManager.UI.ViewModels;

namespace SystemTrayProcessManager.Tests.UI;

/// <summary>Tests per-application editing through persistence and actual action registration.</summary>
public class ProcessHotkeysViewModelTests
{
    [Fact]
    public async Task SaveEditReopen_UpdatesLiveShortcutAndPreservesOtherAppsAndActions()
    {
        var saved = new HotkeyConfiguration();
        var config = new Mock<IHotkeyConfigurationService>();
        config.Setup(c => c.LoadConfigurationAsync()).ReturnsAsync(() => new HotkeyConfiguration(saved.Items.Select(i => i.Clone())));
        config.Setup(c => c.SaveConfigurationAsync(It.IsAny<HotkeyConfiguration>()))
            .Callback<HotkeyConfiguration>(value => saved = new HotkeyConfiguration(value.Items.Select(i => i.Clone())))
            .ReturnsAsync(true);
        var callbacks = new Dictionary<HotkeyBinding, Func<Task>>();
        var hotkeys = new Mock<IHotkeyService>();
        hotkeys.Setup(h => h.IsInitialized).Returns(true);
        hotkeys.Setup(h => h.RegisterHotkey(It.IsAny<HotkeyBinding>(), It.IsAny<Func<Task>>(), true))
            .Returns<HotkeyBinding, Func<Task>, bool>((binding, action, _) => callbacks.TryAdd(binding, action));
        hotkeys.Setup(h => h.UnregisterHotkey(It.IsAny<HotkeyBinding>())).Returns<HotkeyBinding>(key => callbacks.Remove(key));
        var processes = new Mock<IProcessService>();
        processes.Setup(p => p.GetRunningProcessesAsync()).ReturnsAsync(new[]
        {
            new ProcessInfo { ProcessId = 10, Name = "Player", WindowHandle = new IntPtr(10) },
            new ProcessInfo { ProcessId = 20, Name = "Browser", WindowHandle = new IntPtr(20) }
        });
        var audio = new Mock<IAudioService>();
        audio.Setup(a => a.ToggleMuteProcessAsync(It.IsAny<int>())).ReturnsAsync(true);
        using var actions = new ActionMappingService(NullLogger<ActionMappingService>.Instance, hotkeys.Object,
            config.Object, Mock.Of<IWindowService>(), audio.Object, processes.Object);
        ProcessHotkeysViewModel Create() => new(config.Object, actions, NullLogger<ProcessHotkeysViewModel>.Instance);
        var player = Create();
        await player.LoadAsync("Player.exe");
        Assert.Equal(HotkeyConfigViewModel.AvailableActionTypes.Count, player.Rows.Count);
        SetKey(player, "ToggleMute", 0x4D);
        SetKey(player, "Minimize", 0x4E);
        await player.SaveCommand.ExecuteAsync(null);
        var browser = Create();
        await browser.LoadAsync("Browser");
        SetKey(browser, "ToggleMute", 0x50);
        await browser.SaveCommand.ExecuteAsync(null);

        // Editing a key must unregister the old combination and preserve every other app/action.
        SetKey(player, "ToggleMute", 0x51);
        await player.SaveCommand.ExecuteAsync(null);
        Assert.False(callbacks.ContainsKey(Key(0x4D)));
        Assert.Equal(3, callbacks.Count);
        await callbacks[Key(0x51)]();
        await callbacks[Key(0x50)]();
        audio.Verify(a => a.ToggleMuteProcessAsync(10), Times.Once);
        audio.Verify(a => a.ToggleMuteProcessAsync(20), Times.Once);

        var reopened = Create();
        await reopened.LoadAsync("PLAYER");
        Assert.Equal(0x51, reopened.Rows.Single(r => r.ActionType == "ToggleMute").VirtualKeyCode);
        reopened.Rows.Single(r => r.ActionType == "ToggleMute").IsEnabled = false;
        reopened.ClearCommand.Execute(reopened.Rows.Single(r => r.ActionType == "Minimize"));
        await reopened.SaveCommand.ExecuteAsync(null);
        Assert.Single(callbacks);
        Assert.True(callbacks.ContainsKey(Key(0x50)));
        Assert.Contains(saved.Items, i => i.TargetProcessName == "PLAYER" && !i.IsEnabled);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task DuplicateShortcut_IsRejectedAcrossAppsAndWithinOneApp(bool otherApp)
    {
        var config = new Mock<IHotkeyConfigurationService>();
        var existing = new HotkeyConfigItem("Browser mute", 0x4D, HotkeyModifier.Ctrl, "ToggleMute") { TargetProcessName = "Browser" };
        config.Setup(c => c.LoadConfigurationAsync()).ReturnsAsync(new HotkeyConfiguration(otherApp ? new[] { existing } : []));
        var vm = new ProcessHotkeysViewModel(config.Object, Mock.Of<IActionMappingService>(), NullLogger<ProcessHotkeysViewModel>.Instance);
        await vm.LoadAsync("Player");
        SetKey(vm, "ToggleMute", 0x4D);
        if (!otherApp) SetKey(vm, "Minimize", 0x4D);
        await vm.SaveCommand.ExecuteAsync(null);
        Assert.Contains("already assigned", vm.StatusMessage);
        Assert.True(vm.HasUnsavedChanges);
        config.Verify(c => c.SaveConfigurationAsync(It.IsAny<HotkeyConfiguration>()), Times.Never);
    }

    [Fact]
    public async Task FailedSave_LeavesEditsAndDoesNotReloadRuntime()
    {
        var config = new Mock<IHotkeyConfigurationService>();
        config.Setup(c => c.LoadConfigurationAsync()).ReturnsAsync(new HotkeyConfiguration());
        config.Setup(c => c.SaveConfigurationAsync(It.IsAny<HotkeyConfiguration>())).ReturnsAsync(false);
        var actions = new Mock<IActionMappingService>();
        var vm = new ProcessHotkeysViewModel(config.Object, actions.Object, NullLogger<ProcessHotkeysViewModel>.Instance);
        await vm.LoadAsync("Player");
        SetKey(vm, "ToggleMute", 0x4D);
        await vm.SaveCommand.ExecuteAsync(null);
        Assert.True(vm.HasUnsavedChanges);
        Assert.True(vm.SaveCommand.CanExecute(null));
        actions.Verify(a => a.ReloadMappingsAsync(), Times.Never);
    }

    private static HotkeyBinding Key(int key) => new(key, HotkeyModifier.Ctrl);
    private static void SetKey(ProcessHotkeysViewModel vm, string action, int key)
    {
        var row = vm.Rows.Single(r => r.ActionType == action);
        row.VirtualKeyCode = key;
        row.Modifiers = HotkeyModifier.Ctrl;
    }
}
