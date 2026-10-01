using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using SystemTrayProcessManager.Core.Enums;
using SystemTrayProcessManager.Core.Models;
using SystemTrayProcessManager.Core.Services;
using SystemTrayProcessManager.UI.ViewModels;

namespace SystemTrayProcessManager.Tests.UI;

/// <summary>Regression tests for configuring and applying application mute shortcuts.</summary>
public class HotkeyConfigViewModelTests
{
    [Theory]
    [InlineData(" Spotify.exe ", "Spotify.exe", ActionMode.PinnedProcess)]
    [InlineData("", null, ActionMode.QuickAction)]
    [InlineData("   ", null, ActionMode.QuickAction)]
    public async Task SaveMuteShortcut_PersistsTargetAndAppliesImmediately(string target, string? expectedTarget, ActionMode expectedMode)
    {
        var config = new Mock<IHotkeyConfigurationService>();
        var actions = new Mock<IActionMappingService>();
        HotkeyConfiguration? saved = null;
        config.Setup(c => c.SaveConfigurationAsync(It.IsAny<HotkeyConfiguration>()))
            .Callback<HotkeyConfiguration>(c => saved = c).ReturnsAsync(true);
        actions.Setup(a => a.ReloadMappingsAsync()).Callback(() => Assert.NotNull(saved)).Returns(Task.CompletedTask);
        var vm = new HotkeyConfigViewModel(NullLogger<HotkeyConfigViewModel>.Instance, config.Object, actions.Object);

        vm.AddHotkeyCommand.Execute(null);
        vm.EditingItem!.TargetProcessName = target;
        vm.OnHotkeyCaptured(new HotkeyBinding(0x4D, HotkeyModifier.Ctrl | HotkeyModifier.Alt));
        vm.SaveEditCommand.Execute(null);
        await vm.SaveAllChangesCommand.ExecuteAsync(null);

        var item = Assert.Single(saved!.Items);
        Assert.Equal("ToggleMute", item.ActionType);
        Assert.Equal(expectedTarget, item.TargetProcessName);
        Assert.Equal(expectedMode, item.ActionMode);
        Assert.Equal(0x4D, item.VirtualKeyCode);
        Assert.Equal(HotkeyModifier.Ctrl | HotkeyModifier.Alt, item.Modifiers);
        Assert.False(vm.HasUnsavedChanges);
        actions.Verify(a => a.ReloadMappingsAsync(), Times.Once);
    }

    [Fact]
    public async Task ClearingTarget_ChangesPinnedShortcutToFocusedWindow()
    {
        var config = new Mock<IHotkeyConfigurationService>();
        config.Setup(c => c.LoadConfigurationAsync()).ReturnsAsync(new HotkeyConfiguration(new[]
        {
            new HotkeyConfigItem("Mute player", 0x4D, HotkeyModifier.Ctrl, "ToggleMute")
            { TargetProcessName = "Spotify", ActionMode = ActionMode.PinnedProcess }
        }));
        var vm = new HotkeyConfigViewModel(NullLogger<HotkeyConfigViewModel>.Instance, config.Object, Mock.Of<IActionMappingService>());
        await vm.LoadConfigurationAsync();
        vm.SelectedItem = Assert.Single(vm.HotkeyItems);
        vm.EditHotkeyCommand.Execute(null);
        vm.EditingItem!.TargetProcessName = "";
        vm.SaveEditCommand.Execute(null);
        Assert.Equal(ActionMode.QuickAction, vm.SelectedItem.ActionMode);
        Assert.Null(vm.SelectedItem.TargetProcessName);
    }

    [Fact]
    public async Task EnableCheckbox_MarksChangesAndSavesDisabledBinding()
    {
        var item = new HotkeyConfigItem("Mute player", 0x4D, HotkeyModifier.Ctrl, "ToggleMute");
        var config = new Mock<IHotkeyConfigurationService>();
        config.Setup(c => c.LoadConfigurationAsync()).ReturnsAsync(new HotkeyConfiguration(new[] { item }));
        config.Setup(c => c.SaveConfigurationAsync(It.IsAny<HotkeyConfiguration>())).ReturnsAsync(true);
        var actions = new Mock<IActionMappingService>();
        var vm = new HotkeyConfigViewModel(NullLogger<HotkeyConfigViewModel>.Instance, config.Object, actions.Object);
        await vm.LoadConfigurationAsync();

        Assert.False(vm.HasUnsavedChanges);
        item.IsEnabled = false;
        Assert.True(vm.SaveAllChangesCommand.CanExecute(null));
        await vm.SaveAllChangesCommand.ExecuteAsync(null);
        config.Verify(c => c.SaveConfigurationAsync(It.Is<HotkeyConfiguration>(v => !v.Items.Single().IsEnabled)), Times.Once);
        actions.Verify(a => a.ReloadMappingsAsync(), Times.Once);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task SaveFailure_KeepsChangesAvailableForRetry(bool reloadFails)
    {
        var config = new Mock<IHotkeyConfigurationService>();
        config.Setup(c => c.SaveConfigurationAsync(It.IsAny<HotkeyConfiguration>())).ReturnsAsync(reloadFails);
        var actions = new Mock<IActionMappingService>();
        actions.Setup(a => a.ReloadMappingsAsync()).ThrowsAsync(new InvalidOperationException("Cannot register"));
        var vm = new HotkeyConfigViewModel(NullLogger<HotkeyConfigViewModel>.Instance, config.Object, actions.Object)
        { HasUnsavedChanges = true };

        await vm.SaveAllChangesCommand.ExecuteAsync(null);
        Assert.True(vm.HasUnsavedChanges);
        Assert.False(vm.IsBusy);
        Assert.DoesNotContain("successfully", vm.StatusMessage!);
        actions.Verify(a => a.ReloadMappingsAsync(), reloadFails ? Times.Once() : Times.Never());
    }

    [Fact]
    public void ConflictingShortcut_IsNotAdded()
    {
        var config = new Mock<IHotkeyConfigurationService>();
        config.Setup(c => c.HasConflict(It.IsAny<HotkeyBinding>(), It.IsAny<IEnumerable<HotkeyConfigItem>>(), It.IsAny<Guid?>())).Returns(true);
        var vm = new HotkeyConfigViewModel(NullLogger<HotkeyConfigViewModel>.Instance, config.Object, Mock.Of<IActionMappingService>());
        vm.AddHotkeyCommand.Execute(null);
        vm.OnHotkeyCaptured(new HotkeyBinding(0x4D, HotkeyModifier.Ctrl));
        vm.SaveEditCommand.Execute(null);
        Assert.Empty(vm.HotkeyItems);
        Assert.True(vm.IsEditing);
        Assert.NotNull(vm.ValidationMessage);
    }
}
