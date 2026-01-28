# Task 7 Design Document: Hotkey Configuration UI

## Task Overview
**Task**: 3.2 Hotkey Configuration UI (Task 7 in Feature Status)  
**Status**: 📋 In Progress  
**Priority**: High  
**Estimated Time**: 4-5 hours  
**Dependencies**: Task 6 (Low-Level Keyboard Hook) - ✅ Complete

## Objective
Create a comprehensive hotkey configuration UI that allows users to:
- View and manage registered hotkey bindings
- Capture new hotkeys using a visual control
- Add, edit, and delete hotkey configurations
- Import/export configurations (JSON format)
- Detect and warn about conflicting bindings
- Reset to default configurations

## Architecture Design

### Component Diagram
```
┌─────────────────────────────────────────────────────────────────────┐
│                         UI Layer                                     │
├─────────────────────────────────────────────────────────────────────┤
│  HotkeyConfigWindow.xaml                                            │
│  ├── HotkeyCaptureBox (Custom Control)                              │
│  │   └── Captures key combinations with visual feedback             │
│  ├── Hotkey List (DataGrid/ListView)                                │
│  │   └── Displays registered bindings with actions                  │
│  └── Action Buttons (Add, Edit, Delete, Import, Export, Reset)      │
├─────────────────────────────────────────────────────────────────────┤
│  HotkeyConfigViewModel (CommunityToolkit.Mvvm)                      │
│  ├── ObservableCollection<HotkeyConfigItem>                         │
│  ├── RelayCommands for CRUD operations                              │
│  └── Validation logic and conflict detection                        │
├─────────────────────────────────────────────────────────────────────┤
│                      Core Layer                                      │
├─────────────────────────────────────────────────────────────────────┤
│  Models                                                              │
│  ├── HotkeyConfigItem (UI display model)                            │
│  └── HotkeyConfigurationFile (Import/Export DTO)                    │
├─────────────────────────────────────────────────────────────────────┤
│  Interfaces                                                          │
│  └── IHotkeyConfigurationService (persistence operations)           │
├─────────────────────────────────────────────────────────────────────┤
│                  Infrastructure Layer                                │
├─────────────────────────────────────────────────────────────────────┤
│  HotkeyConfigurationService                                          │
│  ├── LoadConfigurationAsync() - Load from JSON file                 │
│  ├── SaveConfigurationAsync() - Save to JSON file                   │
│  ├── ExportAsync() - Export to custom path                          │
│  ├── ImportAsync() - Import from custom path                        │
│  └── GetDefaultConfiguration() - Return default hotkeys             │
└─────────────────────────────────────────────────────────────────────┘
```

## File Structure

### Core Project
```
SystemTrayProcessManager.Core/
├── Models/
│   ├── HotkeyConfigItem.cs           # UI-friendly hotkey configuration item
│   └── HotkeyConfiguration.cs         # Root configuration for serialization
├── Services/
│   └── IHotkeyConfigurationService.cs # Configuration persistence interface
```

### Infrastructure Project
```
SystemTrayProcessManager.Infrastructure/
├── Services/
│   └── HotkeyConfigurationService.cs  # Configuration persistence implementation
```

### UI Project
```
SystemTrayProcessManager.UI/
├── ViewModels/
│   └── HotkeyConfigViewModel.cs       # ViewModel for hotkey configuration
├── Views/
│   └── HotkeyConfigWindow.xaml        # Main configuration window
├── Controls/
│   └── HotkeyCaptureBox.cs            # Custom control for hotkey capture
├── Converters/
│   └── HotkeyModifierToStringConverter.cs  # Converts modifiers for display
```

## Detailed Component Design

### 1. HotkeyConfigItem Model (Core)
```csharp
public class HotkeyConfigItem : ObservableObject
{
    // Properties
    - Id: Guid (unique identifier)
    - Name: string (user-friendly name like "Mute Focused App")
    - Description: string (detailed description)
    - VirtualKeyCode: int (primary key)
    - Modifiers: HotkeyModifier (Ctrl, Alt, Shift, Win flags)
    - ActionType: string (action to execute)
    - TargetProcessName: string? (optional specific process)
    - IsEnabled: bool (can be temporarily disabled)
    - DisplayString: string (computed: "Ctrl+Alt+M")
    
    // Methods
    - ToHotkeyBinding(): HotkeyBinding
    - FromHotkeyBinding(HotkeyBinding): HotkeyConfigItem
}
```

### 2. HotkeyConfiguration Model (Core)
```csharp
public class HotkeyConfiguration
{
    // Properties
    - Version: string (for migration support)
    - LastModified: DateTime
    - Items: List<HotkeyConfigItem>
}
```

### 3. IHotkeyConfigurationService Interface (Core)
```csharp
public interface IHotkeyConfigurationService
{
    // Configuration file operations
    Task<HotkeyConfiguration> LoadConfigurationAsync();
    Task SaveConfigurationAsync(HotkeyConfiguration configuration);
    
    // Import/Export
    Task<HotkeyConfiguration?> ImportAsync(string filePath);
    Task ExportAsync(string filePath, HotkeyConfiguration configuration);
    
    // Defaults
    HotkeyConfiguration GetDefaultConfiguration();
    
    // Validation
    IEnumerable<string> ValidateConfiguration(HotkeyConfiguration configuration);
    bool HasConflict(HotkeyBinding binding, IEnumerable<HotkeyConfigItem> existingItems);
}
```

### 4. HotkeyCaptureBox Custom Control (UI)
```
Features:
- Captures keyboard input when focused
- Shows current pressed modifiers in real-time
- Validates key combinations
- Visual states: Idle, Capturing, Captured, Error
- Clears on Escape key
- Supports binding to HotkeyBinding

Visual Design:
┌─────────────────────────────────────────┐
│ 🎹 Press a key combination...          │ <- Placeholder text
│    Ctrl + Alt + M                       │ <- Captured keys
│ ✓ Valid combination                     │ <- Validation indicator
└─────────────────────────────────────────┘
```

### 5. HotkeyConfigViewModel (UI)
```csharp
public partial class HotkeyConfigViewModel : ObservableObject
{
    // Observable Properties
    [ObservableProperty] private ObservableCollection<HotkeyConfigItem> _hotkeyItems;
    [ObservableProperty] private HotkeyConfigItem? _selectedItem;
    [ObservableProperty] private bool _isEditing;
    [ObservableProperty] private HotkeyConfigItem? _editingItem;
    [ObservableProperty] private string? _validationMessage;
    [ObservableProperty] private bool _hasUnsavedChanges;
    
    // Commands
    [RelayCommand] private void AddHotkey();
    [RelayCommand] private void EditHotkey();
    [RelayCommand] private void DeleteHotkey();
    [RelayCommand] private Task SaveChangesAsync();
    [RelayCommand] private Task ImportConfigurationAsync();
    [RelayCommand] private Task ExportConfigurationAsync();
    [RelayCommand] private void ResetToDefaults();
    [RelayCommand] private void CancelEdit();
    
    // Validation
    private bool ValidateBinding(HotkeyBinding binding);
    private void CheckForConflicts();
}
```

### 6. HotkeyConfigWindow (UI)
```
Layout:
┌──────────────────────────────────────────────────────────────────┐
│ Hotkey Configuration                                        [X]  │
├──────────────────────────────────────────────────────────────────┤
│                                                                   │
│  ┌─────────────────────────────────────────────────────────────┐ │
│  │ Name          │ Hotkey        │ Action       │ Enabled │ ⚙  │ │
│  ├───────────────┼───────────────┼──────────────┼─────────┼────│ │
│  │ Mute Focused  │ Ctrl+Alt+M    │ Toggle Mute  │ ✓       │ ✏🗑│ │
│  │ Close Window  │ Ctrl+Alt+W    │ Close        │ ✓       │ ✏🗑│ │
│  │ Minimize All  │ Win+D         │ Minimize     │ ✗       │ ✏🗑│ │
│  └─────────────────────────────────────────────────────────────┘ │
│                                                                   │
│  ┌─ Edit Hotkey ─────────────────────────────────────────────┐   │
│  │ Name:   [________________________]                         │   │
│  │ Hotkey: [🎹 Press keys... Ctrl+Alt+M]                     │   │
│  │ Action: [Toggle Mute          ▼]                          │   │
│  │ Target: [Focused Window       ▼] or [Process: ______]     │   │
│  │                                                            │   │
│  │ ⚠ Warning: This conflicts with system shortcut            │   │
│  │                                                            │   │
│  │              [Cancel]  [Save]                              │   │
│  └────────────────────────────────────────────────────────────┘   │
│                                                                   │
│  [+ Add] [Import] [Export] [Reset to Defaults]         [Close]   │
└──────────────────────────────────────────────────────────────────┘
```

## Implementation Plan

### Phase 1: Core Models and Interface (30 min)
1. Create `HotkeyConfigItem` model with ObservableObject base
2. Create `HotkeyConfiguration` model for serialization
3. Create `IHotkeyConfigurationService` interface

### Phase 2: Infrastructure Service (45 min)
1. Implement `HotkeyConfigurationService`
   - JSON serialization/deserialization
   - File I/O with error handling
   - Default configuration generation
   - Conflict detection logic

### Phase 3: HotkeyCaptureBox Control (60 min)
1. Create custom control with dependency properties
2. Implement keyboard event handling
3. Add visual state management
4. Implement modifier key tracking
5. Add validation feedback

### Phase 4: ViewModel Implementation (60 min)
1. Create `HotkeyConfigViewModel` with CommunityToolkit.Mvvm
2. Implement all commands
3. Add validation logic
4. Wire up configuration service
5. Handle unsaved changes

### Phase 5: Window UI Implementation (45 min)
1. Create `HotkeyConfigWindow.xaml`
2. Build DataGrid/ListView for hotkey list
3. Create edit panel with HotkeyCaptureBox
4. Add action buttons and styling
5. Wire up DataContext

### Phase 6: Integration and Testing (45 min)
1. Register services in DI container
2. Add menu item to open configuration
3. Create unit tests
4. Manual testing and refinement

## Key Design Decisions

### 1. Separation of HotkeyBinding and HotkeyConfigItem
- `HotkeyBinding` (existing): Minimal struct for runtime registration
- `HotkeyConfigItem`: Rich model for UI binding with metadata

### 2. JSON Configuration Storage
- Location: `%LOCALAPPDATA%\SystemTrayProcessManager\hotkeys.json`
- Format: Human-readable, version-tagged
- Backup: Auto-backup before save

### 3. Conflict Detection Strategy
- System shortcuts: Warn but allow
- Application shortcuts: Block duplicates
- Visual indicator for conflicts

### 4. Custom Control vs User Control
- Using custom control for HotkeyCaptureBox for maximum reusability
- Allows templating and styling flexibility

## Testing Strategy

### Unit Tests
1. `HotkeyConfigItemTests`
   - Property change notifications
   - DisplayString generation
   - ToHotkeyBinding conversion

2. `HotkeyConfigurationServiceTests`
   - Load/Save operations
   - Import/Export
   - Default configuration
   - Conflict detection

3. `HotkeyConfigViewModelTests`
   - Command execution
   - Validation logic
   - State management

### Integration Tests
1. Full flow: Add → Save → Reload → Verify
2. Import/Export round-trip
3. Conflict detection accuracy

## Success Criteria
- [ ] Users can view all registered hotkeys
- [ ] Users can add new hotkey configurations
- [ ] Users can edit existing hotkeys
- [ ] Users can delete hotkeys
- [ ] HotkeyCaptureBox captures keys correctly
- [ ] Conflict detection works accurately
- [ ] Import/Export functions correctly
- [ ] Reset to defaults works
- [ ] All unit tests pass
- [ ] Zero compiler warnings

## Risk Assessment

| Risk | Probability | Impact | Mitigation |
|------|-------------|--------|------------|
| Keyboard capture conflicts with system | Medium | High | Use Preview events, proper focus handling |
| JSON corruption | Low | Medium | Backup before save, validation on load |
| Memory leaks in custom control | Low | Medium | Proper event unsubscription |
| Thread safety in ViewModel | Medium | Medium | Use dispatcher for UI updates |

## Dependencies
- Task 6 (Low-Level Keyboard Hook): ✅ Complete
- CommunityToolkit.Mvvm: Already installed
- System.Text.Json: Built-in .NET
