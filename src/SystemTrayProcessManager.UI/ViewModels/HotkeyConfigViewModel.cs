using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using System.Collections.ObjectModel;
using System.ComponentModel;
using SystemTrayProcessManager.Core.Enums;
using SystemTrayProcessManager.Core.Models;
using SystemTrayProcessManager.Core.Services;
using OpenFileDialog = Microsoft.Win32.OpenFileDialog;
using SaveFileDialog = Microsoft.Win32.SaveFileDialog;

namespace SystemTrayProcessManager.UI.ViewModels
{
    /// <summary>
    /// ViewModel for the Hotkey Configuration window.
    /// Manages hotkey bindings, CRUD operations, and import/export functionality.
    /// </summary>
    public class HotkeyConfigViewModel : ObservableObject
    {
        private readonly ILogger<HotkeyConfigViewModel> _logger;
        private readonly IHotkeyConfigurationService _configService;
        private readonly IActionMappingService _actionMappingService;

        #region Backing Fields

        private ObservableCollection<HotkeyConfigItem> _hotkeyItems = [];
        private HotkeyConfigItem? _selectedItem;
        private bool _isEditing;
        private HotkeyConfigItem? _editingItem;
        private bool _isAddingNew;
        private string? _validationMessage;
        private string? _conflictWarning;
        private bool _hasUnsavedChanges;
        private string? _statusMessage;
        private bool _isBusy;

        #endregion

        #region Observable Properties

        /// <summary>
        /// Gets the collection of hotkey configuration items.
        /// </summary>
        public ObservableCollection<HotkeyConfigItem> HotkeyItems
        {
            get => _hotkeyItems;
            set => SetProperty(ref _hotkeyItems, value);
        }

        /// <summary>
        /// Gets or sets the currently selected hotkey item.
        /// </summary>
        public HotkeyConfigItem? SelectedItem
        {
            get => _selectedItem;
            set
            {
                if (SetProperty(ref _selectedItem, value))
                {
                    EditHotkeyCommand.NotifyCanExecuteChanged();
                    DeleteHotkeyCommand.NotifyCanExecuteChanged();
                }
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether the edit panel is visible.
        /// </summary>
        public bool IsEditing
        {
            get => _isEditing;
            set
            {
                if (SetProperty(ref _isEditing, value))
                {
                    SaveEditCommand.NotifyCanExecuteChanged();
                    CancelEditCommand.NotifyCanExecuteChanged();
                    SaveAllChangesCommand.NotifyCanExecuteChanged();
                }
            }
        }

        /// <summary>
        /// Gets or sets the item currently being edited.
        /// </summary>
        public HotkeyConfigItem? EditingItem
        {
            get => _editingItem;
            set => SetProperty(ref _editingItem, value);
        }

        /// <summary>
        /// Gets or sets whether we're adding a new item (vs editing existing).
        /// </summary>
        public bool IsAddingNew
        {
            get => _isAddingNew;
            set => SetProperty(ref _isAddingNew, value);
        }

        /// <summary>
        /// Gets or sets the validation message for the current edit.
        /// </summary>
        public string? ValidationMessage
        {
            get => _validationMessage;
            set => SetProperty(ref _validationMessage, value);
        }

        /// <summary>
        /// Gets or sets whether there's a conflict with system shortcuts.
        /// </summary>
        public string? ConflictWarning
        {
            get => _conflictWarning;
            set => SetProperty(ref _conflictWarning, value);
        }

        /// <summary>
        /// Gets or sets whether there are unsaved changes.
        /// </summary>
        public bool HasUnsavedChanges
        {
            get => _hasUnsavedChanges;
            set
            {
                if (SetProperty(ref _hasUnsavedChanges, value))
                {
                    SaveAllChangesCommand.NotifyCanExecuteChanged();
                }
            }
        }

        /// <summary>
        /// Gets or sets the status message.
        /// </summary>
        public string? StatusMessage
        {
            get => _statusMessage;
            set => SetProperty(ref _statusMessage, value);
        }

        /// <summary>
        /// Gets or sets whether an operation is in progress.
        /// </summary>
        public bool IsBusy
        {
            get => _isBusy;
            set
            {
                if (SetProperty(ref _isBusy, value))
                {
                    SaveEditCommand.NotifyCanExecuteChanged();
                    SaveAllChangesCommand.NotifyCanExecuteChanged();
                }
            }
        }

        #endregion

        #region Collections for ComboBoxes

        /// <summary>
        /// Gets the available action types.
        /// </summary>
        public IReadOnlyList<string> ActionTypes => AvailableActionTypes;

        /// <summary>Gets the actions configurable globally or for an individual application.</summary>
        public static IReadOnlyList<string> AvailableActionTypes { get; } =
        [
            "ToggleMute",
            "Mute",
            "Unmute",
            "PreviousTrack",
            "NextTrack",
            "Minimize",
            "Maximize",
            "Restore",
            "Close",
            "BringToFront",
            "SetFocus",
            "Hide",
            "Show",
            "ToggleAlwaysOnTop"
        ];

        #endregion

        #region Commands

        /// <summary>
        /// Gets the command to add a new hotkey.
        /// </summary>
        public RelayCommand AddHotkeyCommand { get; }

        /// <summary>
        /// Gets the command to edit the selected hotkey.
        /// </summary>
        public RelayCommand<HotkeyConfigItem> EditHotkeyCommand { get; }

        /// <summary>
        /// Gets the command to delete the selected hotkey.
        /// </summary>
        public RelayCommand<HotkeyConfigItem> DeleteHotkeyCommand { get; }

        /// <summary>
        /// Gets the command to save the current edit.
        /// </summary>
        public AsyncRelayCommand SaveEditCommand { get; }

        /// <summary>
        /// Gets the command to cancel the current edit.
        /// </summary>
        public RelayCommand CancelEditCommand { get; }

        /// <summary>
        /// Gets the command to save all changes.
        /// </summary>
        public AsyncRelayCommand SaveAllChangesCommand { get; }

        /// <summary>
        /// Gets the command to import configuration.
        /// </summary>
        public AsyncRelayCommand ImportConfigurationCommand { get; }

        /// <summary>
        /// Gets the command to export configuration.
        /// </summary>
        public AsyncRelayCommand ExportConfigurationCommand { get; }

        /// <summary>
        /// Gets the command to reset to defaults.
        /// </summary>
        public RelayCommand ResetToDefaultsCommand { get; }

        /// <summary>
        /// Gets the command to toggle enabled state.
        /// </summary>
        public RelayCommand ToggleEnabledCommand { get; }

        #endregion

        /// <summary>
        /// Initializes a new instance of the <see cref="HotkeyConfigViewModel"/> class.
        /// </summary>
        /// <param name="logger">The logger instance.</param>
        /// <param name="configService">The hotkey configuration service.</param>
        /// <param name="actionMappingService">The service that applies saved hotkeys to running actions.</param>
        public HotkeyConfigViewModel(
            ILogger<HotkeyConfigViewModel> logger,
            IHotkeyConfigurationService configService,
            IActionMappingService actionMappingService)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _configService = configService ?? throw new ArgumentNullException(nameof(configService));
            _actionMappingService = actionMappingService ?? throw new ArgumentNullException(nameof(actionMappingService));

            // Initialize commands
            AddHotkeyCommand = new RelayCommand(ExecuteAddHotkey);
            EditHotkeyCommand = new RelayCommand<HotkeyConfigItem>(item => { SelectedItem = item ?? SelectedItem; ExecuteEditHotkey(); }, item => item != null || CanExecuteEditHotkey());
            DeleteHotkeyCommand = new RelayCommand<HotkeyConfigItem>(item => { SelectedItem = item ?? SelectedItem; ExecuteDeleteHotkey(); }, item => item != null || CanExecuteDeleteHotkey());
            SaveEditCommand = new AsyncRelayCommand(ExecuteSaveAllChangesAsync, CanExecuteSaveEdit);
            CancelEditCommand = new RelayCommand(ExecuteCancelEdit, CanExecuteCancelEdit);
            SaveAllChangesCommand = new AsyncRelayCommand(ExecuteSaveAllChangesAsync, CanExecuteSaveAllChanges);
            ImportConfigurationCommand = new AsyncRelayCommand(ExecuteImportConfigurationAsync);
            ExportConfigurationCommand = new AsyncRelayCommand(ExecuteExportConfigurationAsync);
            ResetToDefaultsCommand = new RelayCommand(ExecuteResetToDefaults);
            ToggleEnabledCommand = new RelayCommand(ExecuteToggleEnabled);

            _logger.LogDebug("HotkeyConfigViewModel created");
        }

        #region Initialization

        /// <summary>
        /// Loads the hotkey configuration asynchronously.
        /// </summary>
        public async Task LoadConfigurationAsync()
        {
            try
            {
                IsBusy = true;
                StatusMessage = "Loading configuration...";

                _logger.LogDebug("Loading hotkey configuration");

                var config = await _configService.LoadConfigurationAsync().ConfigureAwait(true);

                ClearItems();
                foreach (var item in config.Items)
                {
                    AddItem(item);
                }

                HasUnsavedChanges = false;
                StatusMessage = $"Loaded {config.Count} hotkeys";

                _logger.LogInformation("Loaded {Count} hotkey configurations", config.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load hotkey configuration");
                StatusMessage = "Failed to load configuration";
            }
            finally
            {
                IsBusy = false;
            }
        }

        #endregion

        #region Command Implementations

        private void ExecuteAddHotkey()
        {
            _logger.LogDebug("Adding new hotkey");

            EditingItem = new HotkeyConfigItem
            {
                Name = "New Hotkey",
                ActionType = "ToggleMute",
                IsEnabled = true
            };

            IsAddingNew = true;
            IsEditing = true;
            ValidationMessage = null;
            ConflictWarning = null;
        }

        private void ExecuteEditHotkey()
        {
            if (SelectedItem == null) return;

            _logger.LogDebug("Editing hotkey: {Name}", SelectedItem.Name);

            EditingItem = SelectedItem.Clone();
            EditingItem.Id = SelectedItem.Id;

            IsAddingNew = false;
            IsEditing = true;
            ValidationMessage = null;

            CheckForConflicts();
        }

        private bool CanExecuteEditHotkey() => SelectedItem != null;

        private void ExecuteDeleteHotkey()
        {
            if (SelectedItem == null) return;

            _logger.LogDebug("Deleting hotkey: {Name}", SelectedItem.Name);

            var itemToRemove = SelectedItem;
            itemToRemove.PropertyChanged -= OnItemPropertyChanged;
            HotkeyItems.Remove(itemToRemove);

            HasUnsavedChanges = true;
            StatusMessage = $"Deleted: {itemToRemove.Name}";

            SelectedItem = null;
        }

        private bool CanExecuteDeleteHotkey() => SelectedItem != null;

        private bool CommitEdit()
        {
            if (EditingItem == null) return false;

            if (!ValidateEdit())
            {
                return false;
            }

            _logger.LogDebug("Saving edit for hotkey: {Name}", EditingItem.Name);

            // The target field is the UI's source of truth for the action mode.
            EditingItem.TargetProcessName = string.IsNullOrWhiteSpace(EditingItem.TargetProcessName)
                ? null : EditingItem.TargetProcessName.Trim();
            EditingItem.ActionMode = EditingItem.TargetProcessName == null
                ? ActionMode.QuickAction : ActionMode.PinnedProcess;

            if (IsAddingNew)
            {
                AddItem(EditingItem);
                StatusMessage = $"Added: {EditingItem.Name}";
            }
            else
            {
                var existingItem = HotkeyItems.FirstOrDefault(i => i.Id == EditingItem.Id);
                if (existingItem != null)
                {
                    existingItem.UpdateFrom(EditingItem);
                    StatusMessage = $"Updated: {EditingItem.Name}";
                }
            }

            HasUnsavedChanges = true;
            CloseEditPanel();
            return true;
        }

        private bool CanExecuteSaveEdit() => IsEditing && !IsBusy;

        private void ExecuteCancelEdit()
        {
            _logger.LogDebug("Cancelling edit");
            CloseEditPanel();
        }

        private bool CanExecuteCancelEdit() => IsEditing;

        private async Task ExecuteSaveAllChangesAsync()
        {
            // Both Save buttons commit the visible edit before persisting and applying it.
            if (IsEditing && !CommitEdit()) return;
            try
            {
                IsBusy = true;
                StatusMessage = "Saving changes...";

                _logger.LogDebug("Saving all hotkey changes");

                var config = new HotkeyConfiguration(HotkeyItems);
                bool success = await _configService.SaveConfigurationAsync(config).ConfigureAwait(true);

                if (success)
                {
                    _logger.LogInformation("Saved {Count} hotkey configurations", config.Count);

                    try
                    {
                        await _actionMappingService.ReloadMappingsAsync().ConfigureAwait(true);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Saved hotkeys could not be applied");
                        StatusMessage = "Changes saved, but hotkeys could not be applied. Check for duplicate shortcuts and try Save Changes again.";
                        return;
                    }
                    HasUnsavedChanges = false;
                    StatusMessage = "Changes saved and hotkeys applied";
                }
                else
                {
                    StatusMessage = "Failed to save changes";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to save hotkey configuration");
                StatusMessage = "Error saving changes";
            }
            finally
            {
                IsBusy = false;
            }
        }

        private bool CanExecuteSaveAllChanges() => (HasUnsavedChanges || IsEditing) && !IsBusy;

        private async Task ExecuteImportConfigurationAsync()
        {
            try
            {
                var dialog = new OpenFileDialog
                {
                    Title = "Import Hotkey Configuration",
                    Filter = "JSON files (*.json)|*.json|All files (*.*)|*.*",
                    DefaultExt = ".json"
                };

                if (dialog.ShowDialog() != true) return;

                IsBusy = true;
                StatusMessage = "Importing...";

                _logger.LogDebug("Importing configuration from {Path}", dialog.FileName);

                var config = await _configService.ImportAsync(dialog.FileName).ConfigureAwait(true);

                if (config != null)
                {
                    ClearItems();
                    foreach (var item in config.Items)
                    {
                        AddItem(item);
                    }

                    HasUnsavedChanges = true;
                    StatusMessage = $"Imported {config.Count} hotkeys";
                    _logger.LogInformation("Imported {Count} hotkeys from {Path}", config.Count, dialog.FileName);
                }
                else
                {
                    StatusMessage = "Failed to import configuration";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to import configuration");
                StatusMessage = "Import failed";
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async Task ExecuteExportConfigurationAsync()
        {
            try
            {
                var dialog = new SaveFileDialog
                {
                    Title = "Export Hotkey Configuration",
                    Filter = "JSON files (*.json)|*.json|All files (*.*)|*.*",
                    DefaultExt = ".json",
                    FileName = "hotkeys-export.json"
                };

                if (dialog.ShowDialog() != true) return;

                IsBusy = true;
                StatusMessage = "Exporting...";

                _logger.LogDebug("Exporting configuration to {Path}", dialog.FileName);

                var config = new HotkeyConfiguration(HotkeyItems);
                bool success = await _configService.ExportAsync(dialog.FileName, config).ConfigureAwait(true);

                StatusMessage = success
                    ? $"Exported {config.Count} hotkeys"
                    : "Failed to export configuration";

                if (success)
                {
                    _logger.LogInformation("Exported {Count} hotkeys to {Path}", config.Count, dialog.FileName);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to export configuration");
                StatusMessage = "Export failed";
            }
            finally
            {
                IsBusy = false;
            }
        }

        private void ExecuteResetToDefaults()
        {
            _logger.LogDebug("Resetting to default configuration");

            var defaultConfig = _configService.GetDefaultConfiguration();

            ClearItems();
            foreach (var item in defaultConfig.Items)
            {
                AddItem(item);
            }

            HasUnsavedChanges = true;
            StatusMessage = $"Reset to {defaultConfig.Count} default hotkeys";

            _logger.LogInformation("Reset to default configuration with {Count} hotkeys", defaultConfig.Count);
        }

        private void ExecuteToggleEnabled()
        {
            if (SelectedItem == null) return;

            SelectedItem.IsEnabled = !SelectedItem.IsEnabled;
            HasUnsavedChanges = true;

            _logger.LogDebug(
                "Toggled hotkey '{Name}' enabled state to {Enabled}",
                SelectedItem.Name,
                SelectedItem.IsEnabled);
        }

        #endregion

        #region Validation

        /// <summary>
        /// Called when the editing item's binding changes.
        /// </summary>
        /// <param name="binding">The new hotkey binding.</param>
        public void OnHotkeyCaptured(HotkeyBinding binding)
        {
            if (EditingItem == null) return;

            EditingItem.VirtualKeyCode = binding.VirtualKeyCode;
            EditingItem.Modifiers = binding.Modifiers;

            CheckForConflicts();
        }

        private bool ValidateEdit()
        {
            if (EditingItem == null)
            {
                ValidationMessage = "No item to save";
                return false;
            }

            if (string.IsNullOrWhiteSpace(EditingItem.Name))
            {
                ValidationMessage = "Name is required";
                return false;
            }

            if (EditingItem.VirtualKeyCode == 0)
            {
                ValidationMessage = "Please capture a key combination";
                return false;
            }

            if (string.IsNullOrWhiteSpace(EditingItem.ActionType))
            {
                ValidationMessage = "Action type is required";
                return false;
            }

            var binding = EditingItem.ToHotkeyBinding();
            var excludeId = IsAddingNew ? (Guid?)null : EditingItem.Id;

            if (_configService.HasConflict(binding, HotkeyItems, excludeId))
            {
                ValidationMessage = "This key combination is already used by another hotkey";
                return false;
            }

            ValidationMessage = null;
            return true;
        }

        private void CheckForConflicts()
        {
            if (EditingItem == null || EditingItem.VirtualKeyCode == 0)
            {
                ConflictWarning = null;
                return;
            }

            var binding = EditingItem.ToHotkeyBinding();

            var systemConflict = _configService.GetSystemShortcutConflict(binding);
            if (systemConflict != null)
            {
                ConflictWarning = $"⚠️ Warning: Conflicts with {systemConflict}";
                return;
            }

            var excludeId = IsAddingNew ? (Guid?)null : EditingItem.Id;
            if (_configService.HasConflict(binding, HotkeyItems, excludeId))
            {
                ConflictWarning = "⚠️ Warning: This combination is already in use";
                return;
            }

            ConflictWarning = null;
        }

        #endregion

        #region Private Methods

        private void CloseEditPanel()
        {
            IsEditing = false;
            IsAddingNew = false;
            EditingItem = null;
            ValidationMessage = null;
            ConflictWarning = null;
        }

        private void AddItem(HotkeyConfigItem item)
        {
            item.PropertyChanged += OnItemPropertyChanged;
            HotkeyItems.Add(item);
        }

        private void ClearItems()
        {
            foreach (var item in HotkeyItems)
            {
                item.PropertyChanged -= OnItemPropertyChanged;
            }
            HotkeyItems.Clear();
        }

        private void OnItemPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            HasUnsavedChanges = true;
        }

        #endregion
    }
}
