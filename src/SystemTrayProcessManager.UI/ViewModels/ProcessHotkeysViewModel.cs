using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using System.Collections.ObjectModel;
using System.ComponentModel;
using SystemTrayProcessManager.Core.Enums;
using SystemTrayProcessManager.Core.Models;
using SystemTrayProcessManager.Core.Services;

namespace SystemTrayProcessManager.UI.ViewModels;

/// <summary>Edits an independent shortcut for each action of one application.</summary>
public partial class ProcessHotkeysViewModel : ObservableObject
{
    private readonly IHotkeyConfigurationService _configuration;
    private readonly IActionMappingService _actions;
    private readonly ILogger<ProcessHotkeysViewModel> _logger;
    private bool _loaded;

    [ObservableProperty] private string _processName = string.Empty;
    [ObservableProperty] private string _statusMessage = string.Empty;
    [ObservableProperty] private bool _isBusy;
    [ObservableProperty] private bool _hasUnsavedChanges;

    /// <summary>Gets one shortcut row for each supported action.</summary>
    public ObservableCollection<HotkeyConfigItem> Rows { get; } = [];

    /// <summary>Creates an application-specific shortcut editor.</summary>
    public ProcessHotkeysViewModel(IHotkeyConfigurationService configuration, IActionMappingService actions,
        ILogger<ProcessHotkeysViewModel> logger)
    {
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        _actions = actions ?? throw new ArgumentNullException(nameof(actions));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>Loads saved shortcuts for an executable name rather than a transient process ID.</summary>
    public async Task LoadAsync(string processName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(processName);
        try
        {
            IsBusy = true;
            _loaded = false;
            ProcessName = NormalizeName(processName);
            var configuration = await _configuration.LoadConfigurationAsync();
            foreach (var row in Rows) row.PropertyChanged -= OnRowChanged;
            Rows.Clear();
            foreach (var action in HotkeyConfigViewModel.AvailableActionTypes)
            {
                var existing = configuration.Items.FirstOrDefault(item => MatchesApplication(item) && item.ActionType == action);
                var row = existing?.Clone() ?? new HotkeyConfigItem
                {
                    Name = $"{ProcessName}: {action}", ActionType = action,
                    TargetProcessName = ProcessName, ActionMode = ActionMode.PinnedProcess
                };
                row.PropertyChanged += OnRowChanged;
                Rows.Add(row);
            }
            _loaded = true;
            HasUnsavedChanges = false;
            StatusMessage = "Click a shortcut field, press a key or combination, then Save & Apply. Empty fields have no shortcut.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load shortcuts for {ProcessName}", processName);
            StatusMessage = "Unable to load shortcuts. Close this window and try again.";
        }
        finally { IsBusy = false; SaveCommand.NotifyCanExecuteChanged(); }
    }

    [RelayCommand(CanExecute = nameof(CanSave))]
    private async Task SaveAsync()
    {
        bool persisted = false;
        try
        {
            IsBusy = true;
            // Re-read so changing one app never overwrites another app's newer settings.
            var configuration = await _configuration.LoadConfigurationAsync();
            var items = configuration.Items.Where(item =>
                !MatchesApplication(item) || !HotkeyConfigViewModel.AvailableActionTypes.Contains(item.ActionType))
                .Select(item => item.Clone()).ToList();
            foreach (var row in Rows.Where(row => row.VirtualKeyCode != 0))
            {
                var conflict = items.FirstOrDefault(item => item.IsEnabled && row.IsEnabled &&
                    item.ToHotkeyBinding().Equals(row.ToHotkeyBinding()));
                if (conflict != null)
                {
                    StatusMessage = $"{row.DisplayString} is already assigned to {conflict.Name}. Choose another shortcut.";
                    return;
                }
                var item = row.Clone();
                item.TargetProcessName = ProcessName;
                item.ActionMode = ActionMode.PinnedProcess;
                items.Add(item);
            }
            persisted = await _configuration.SaveConfigurationAsync(new HotkeyConfiguration(items));
            if (!persisted)
            {
                StatusMessage = "Unable to save shortcuts. Your edits are still available; try again.";
                return;
            }
            await _actions.ReloadMappingsAsync();
            HasUnsavedChanges = false;
            StatusMessage = "Shortcuts saved and applied. You can use them immediately.";
            _logger.LogInformation("Applied shortcuts for {ProcessName}", ProcessName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save/apply shortcuts for {ProcessName}", ProcessName);
            StatusMessage = persisted ? "Saved, but shortcuts could not be activated. Try Save & Apply again."
                : "Unable to save shortcuts. Your edits are still available; try again.";
        }
        finally { IsBusy = false; }
    }

    [RelayCommand]
    private void Clear(HotkeyConfigItem? row)
    {
        if (row == null) return;
        row.VirtualKeyCode = 0;
        row.Modifiers = HotkeyModifier.None;
    }

    private bool CanSave() => _loaded && !IsBusy && HasUnsavedChanges;
    private bool MatchesApplication(HotkeyConfigItem item) =>
        string.Equals(NormalizeName(item.TargetProcessName), ProcessName, StringComparison.OrdinalIgnoreCase);
    private static string NormalizeName(string? name)
    {
        return string.IsNullOrWhiteSpace(name) ? string.Empty : HotkeyConfiguration.ApplicationName(name);
    }
    private void OnRowChanged(object? sender, PropertyChangedEventArgs e) => HasUnsavedChanges = true;
    partial void OnIsBusyChanged(bool value) => SaveCommand.NotifyCanExecuteChanged();
    partial void OnHasUnsavedChangesChanged(bool value) => SaveCommand.NotifyCanExecuteChanged();
}
