using CommunityToolkit.Mvvm.ComponentModel;
using System.Text.Json.Serialization;
using SystemTrayProcessManager.Core.Enums;

namespace SystemTrayProcessManager.Core.Models
{
    /// <summary>
    /// Represents a hotkey configuration item for UI display and persistence.
    /// Contains all metadata needed for hotkey management including the binding, action, and display properties.
    /// </summary>
    /// <remarks>
    /// This class extends <see cref="HotkeyBinding"/> with additional properties needed for
    /// configuration UI such as name, description, action type, and enabled state.
    /// </remarks>
    public partial class HotkeyConfigItem : ObservableObject, IEquatable<HotkeyConfigItem>
    {
        private Guid _id;
        private string _name = string.Empty;
        private string _description = string.Empty;
        private int _virtualKeyCode;
        private HotkeyModifier _modifiers;
        private string _actionType = string.Empty;
        private string? _targetProcessName;
        private bool _isEnabled = true;

        /// <summary>
        /// Gets or sets the unique identifier for this configuration item.
        /// </summary>
        public Guid Id
        {
            get => _id;
            set => SetProperty(ref _id, value);
        }

        /// <summary>
        /// Gets or sets the user-friendly name for this hotkey (e.g., "Mute Focused App").
        /// </summary>
        public string Name
        {
            get => _name;
            set
            {
                if (SetProperty(ref _name, value ?? string.Empty))
                {
                    OnPropertyChanged(nameof(IsValid));
                }
            }
        }

        /// <summary>
        /// Gets or sets the detailed description of what this hotkey does.
        /// </summary>
        public string Description
        {
            get => _description;
            set => SetProperty(ref _description, value ?? string.Empty);
        }

        /// <summary>
        /// Gets or sets the virtual key code for the primary key.
        /// </summary>
        public int VirtualKeyCode
        {
            get => _virtualKeyCode;
            set
            {
                if (SetProperty(ref _virtualKeyCode, value))
                {
                    OnPropertyChanged(nameof(DisplayString));
                    OnPropertyChanged(nameof(KeyName));
                    OnPropertyChanged(nameof(IsValid));
                }
            }
        }

        /// <summary>
        /// Gets or sets the modifier keys required for this hotkey.
        /// </summary>
        public HotkeyModifier Modifiers
        {
            get => _modifiers;
            set
            {
                if (SetProperty(ref _modifiers, value))
                {
                    OnPropertyChanged(nameof(DisplayString));
                    OnPropertyChanged(nameof(HasCtrl));
                    OnPropertyChanged(nameof(HasAlt));
                    OnPropertyChanged(nameof(HasShift));
                    OnPropertyChanged(nameof(HasWin));
                }
            }
        }

        /// <summary>
        /// Gets or sets the action type to execute when this hotkey is triggered.
        /// </summary>
        public string ActionType
        {
            get => _actionType;
            set
            {
                if (SetProperty(ref _actionType, value ?? string.Empty))
                {
                    OnPropertyChanged(nameof(IsValid));
                }
            }
        }

        /// <summary>
        /// Gets or sets the target process name for process-specific actions.
        /// Null or empty means the action applies to the focused window.
        /// </summary>
        public string? TargetProcessName
        {
            get => _targetProcessName;
            set => SetProperty(ref _targetProcessName, value);
        }

        /// <summary>
        /// Gets or sets a value indicating whether this hotkey is enabled.
        /// </summary>
        public bool IsEnabled
        {
            get => _isEnabled;
            set => SetProperty(ref _isEnabled, value);
        }

        /// <summary>
        /// Gets the display string representation of the hotkey (e.g., "Ctrl+Alt+M").
        /// </summary>
        [JsonIgnore]
        public string DisplayString
        {
            get
            {
                var parts = new List<string>();

                if (HasCtrl) parts.Add("Ctrl");
                if (HasAlt) parts.Add("Alt");
                if (HasShift) parts.Add("Shift");
                if (HasWin) parts.Add("Win");

                if (_virtualKeyCode > 0)
                {
                    parts.Add(KeyName);
                }

                return parts.Count > 0 ? string.Join("+", parts) : "(Not set)";
            }
        }

        /// <summary>
        /// Gets the human-readable name of the primary key.
        /// </summary>
        [JsonIgnore]
        public string KeyName => GetKeyName(_virtualKeyCode);

        /// <summary>
        /// Gets a value indicating whether the Ctrl modifier is set.
        /// </summary>
        [JsonIgnore]
        public bool HasCtrl => _modifiers.HasFlag(HotkeyModifier.Ctrl);

        /// <summary>
        /// Gets a value indicating whether the Alt modifier is set.
        /// </summary>
        [JsonIgnore]
        public bool HasAlt => _modifiers.HasFlag(HotkeyModifier.Alt);

        /// <summary>
        /// Gets a value indicating whether the Shift modifier is set.
        /// </summary>
        [JsonIgnore]
        public bool HasShift => _modifiers.HasFlag(HotkeyModifier.Shift);

        /// <summary>
        /// Gets a value indicating whether the Win modifier is set.
        /// </summary>
        [JsonIgnore]
        public bool HasWin => _modifiers.HasFlag(HotkeyModifier.Win);

        /// <summary>
        /// Gets a value indicating whether this configuration item is valid.
        /// </summary>
        [JsonIgnore]
        public bool IsValid =>
            !string.IsNullOrWhiteSpace(_name) &&
            !string.IsNullOrWhiteSpace(_actionType) &&
            _virtualKeyCode > 0;

        /// <summary>
        /// Initializes a new instance of the <see cref="HotkeyConfigItem"/> class.
        /// </summary>
        public HotkeyConfigItem()
        {
            _id = Guid.NewGuid();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="HotkeyConfigItem"/> class
        /// with specified values.
        /// </summary>
        /// <param name="name">The user-friendly name for this hotkey.</param>
        /// <param name="virtualKeyCode">The virtual key code for the primary key.</param>
        /// <param name="modifiers">The modifier keys required.</param>
        /// <param name="actionType">The action type to execute.</param>
        public HotkeyConfigItem(string name, int virtualKeyCode, HotkeyModifier modifiers, string actionType)
            : this()
        {
            _name = name ?? string.Empty;
            _virtualKeyCode = virtualKeyCode;
            _modifiers = modifiers;
            _actionType = actionType ?? string.Empty;
        }

        /// <summary>
        /// Creates a <see cref="HotkeyBinding"/> from this configuration item.
        /// </summary>
        /// <returns>A <see cref="HotkeyBinding"/> representing this hotkey combination.</returns>
        public HotkeyBinding ToHotkeyBinding()
        {
            return new HotkeyBinding(_virtualKeyCode, _modifiers);
        }

        /// <summary>
        /// Creates a new <see cref="HotkeyConfigItem"/> from a <see cref="HotkeyBinding"/>.
        /// </summary>
        /// <param name="binding">The hotkey binding to convert.</param>
        /// <param name="name">The name for the configuration item.</param>
        /// <param name="actionType">The action type for the configuration item.</param>
        /// <returns>A new <see cref="HotkeyConfigItem"/>.</returns>
        public static HotkeyConfigItem FromHotkeyBinding(HotkeyBinding binding, string name, string actionType)
        {
            return new HotkeyConfigItem(name, binding.VirtualKeyCode, binding.Modifiers, actionType);
        }

        /// <summary>
        /// Creates a deep copy of this configuration item.
        /// </summary>
        /// <returns>A new <see cref="HotkeyConfigItem"/> with the same values.</returns>
        public HotkeyConfigItem Clone()
        {
            return new HotkeyConfigItem
            {
                Id = _id,
                Name = _name,
                Description = _description,
                VirtualKeyCode = _virtualKeyCode,
                Modifiers = _modifiers,
                ActionType = _actionType,
                TargetProcessName = _targetProcessName,
                IsEnabled = _isEnabled
            };
        }

        /// <summary>
        /// Updates this item's values from another item (preserving Id).
        /// </summary>
        /// <param name="other">The item to copy values from.</param>
        public void UpdateFrom(HotkeyConfigItem other)
        {
            ArgumentNullException.ThrowIfNull(other);

            Name = other.Name;
            Description = other.Description;
            VirtualKeyCode = other.VirtualKeyCode;
            Modifiers = other.Modifiers;
            ActionType = other.ActionType;
            TargetProcessName = other.TargetProcessName;
            IsEnabled = other.IsEnabled;
        }

        /// <summary>
        /// Determines whether this item conflicts with another (same key combination).
        /// </summary>
        /// <param name="other">The other item to check.</param>
        /// <returns>True if there is a conflict; otherwise, false.</returns>
        public bool ConflictsWith(HotkeyConfigItem other)
        {
            if (other == null) return false;
            if (other.Id == _id) return false; // Same item

            return _virtualKeyCode == other.VirtualKeyCode &&
                   _modifiers == other.Modifiers;
        }

        /// <inheritdoc/>
        public bool Equals(HotkeyConfigItem? other)
        {
            if (other is null) return false;
            if (ReferenceEquals(this, other)) return true;

            return _id == other._id;
        }

        /// <inheritdoc/>
        public override bool Equals(object? obj)
        {
            return Equals(obj as HotkeyConfigItem);
        }

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            return _id.GetHashCode();
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            return $"{_name}: {DisplayString} -> {_actionType}";
        }

        /// <summary>
        /// Gets the human-readable name for a virtual key code.
        /// </summary>
        /// <param name="vkCode">The virtual key code.</param>
        /// <returns>The key name.</returns>
        private static string GetKeyName(int vkCode)
        {
            // Handle special keys
            return vkCode switch
            {
                0 => string.Empty,

                // Function keys
                >= 0x70 and <= 0x7B => $"F{vkCode - 0x70 + 1}",

                // Number keys
                >= 0x30 and <= 0x39 => ((char)vkCode).ToString(),

                // Letter keys
                >= 0x41 and <= 0x5A => ((char)vkCode).ToString(),

                // Numpad
                >= 0x60 and <= 0x69 => $"Num{vkCode - 0x60}",

                // Special keys
                0x08 => "Backspace",
                0x09 => "Tab",
                0x0D => "Enter",
                0x1B => "Escape",
                0x20 => "Space",
                0x21 => "PageUp",
                0x22 => "PageDown",
                0x23 => "End",
                0x24 => "Home",
                0x25 => "Left",
                0x26 => "Up",
                0x27 => "Right",
                0x28 => "Down",
                0x2D => "Insert",
                0x2E => "Delete",
                0x6A => "Num*",
                0x6B => "Num+",
                0x6D => "Num-",
                0x6E => "Num.",
                0x6F => "Num/",
                0x90 => "NumLock",
                0x91 => "ScrollLock",
                0xBA => ";",
                0xBB => "=",
                0xBC => ",",
                0xBD => "-",
                0xBE => ".",
                0xBF => "/",
                0xC0 => "`",
                0xDB => "[",
                0xDC => "\\",
                0xDD => "]",
                0xDE => "'",

                _ => $"Key(0x{vkCode:X2})"
            };
        }

        /// <summary>
        /// Equality operator.
        /// </summary>
        public static bool operator ==(HotkeyConfigItem? left, HotkeyConfigItem? right)
        {
            if (left is null) return right is null;
            return left.Equals(right);
        }

        /// <summary>
        /// Inequality operator.
        /// </summary>
        public static bool operator !=(HotkeyConfigItem? left, HotkeyConfigItem? right)
        {
            return !(left == right);
        }
    }
}
