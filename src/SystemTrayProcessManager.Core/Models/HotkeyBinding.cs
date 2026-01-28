using SystemTrayProcessManager.Core.Enums;

namespace SystemTrayProcessManager.Core.Models
{
    /// <summary>
    /// Represents a keyboard combination of a key and optional modifiers.
    /// This immutable value type is used to identify hotkey registrations.
    /// </summary>
    /// <remarks>
    /// HotkeyBinding uses value semantics for equality, allowing use as dictionary keys.
    /// </remarks>
    public readonly struct HotkeyBinding : IEquatable<HotkeyBinding>
    {
        /// <summary>
        /// Gets the virtual key code for the primary key.
        /// </summary>
        public int VirtualKeyCode { get; }

        /// <summary>
        /// Gets the modifier keys required for this hotkey.
        /// </summary>
        public HotkeyModifier Modifiers { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="HotkeyBinding"/> struct.
        /// </summary>
        /// <param name="virtualKeyCode">The virtual key code for the primary key.</param>
        /// <param name="modifiers">The modifier keys required for this hotkey.</param>
        public HotkeyBinding(int virtualKeyCode, HotkeyModifier modifiers = HotkeyModifier.None)
        {
            VirtualKeyCode = virtualKeyCode;
            Modifiers = modifiers;
        }

        /// <summary>
        /// Determines whether the specified binding is equal to this binding.
        /// </summary>
        public bool Equals(HotkeyBinding other)
        {
            return VirtualKeyCode == other.VirtualKeyCode && Modifiers == other.Modifiers;
        }

        /// <summary>
        /// Determines whether the specified object is equal to this binding.
        /// </summary>
        public override bool Equals(object? obj)
        {
            return obj is HotkeyBinding other && Equals(other);
        }

        /// <summary>
        /// Returns a hash code for this binding.
        /// </summary>
        public override int GetHashCode()
        {
            return HashCode.Combine(VirtualKeyCode, Modifiers);
        }

        /// <summary>
        /// Returns a string representation of this binding.
        /// </summary>
        public override string ToString()
        {
            var parts = new List<string>();

            if (Modifiers.HasFlag(HotkeyModifier.Ctrl))
                parts.Add("Ctrl");
            if (Modifiers.HasFlag(HotkeyModifier.Alt))
                parts.Add("Alt");
            if (Modifiers.HasFlag(HotkeyModifier.Shift))
                parts.Add("Shift");
            if (Modifiers.HasFlag(HotkeyModifier.Win))
                parts.Add("Win");

            parts.Add($"0x{VirtualKeyCode:X2}");

            return string.Join("+", parts);
        }

        /// <summary>
        /// Determines whether two bindings are equal.
        /// </summary>
        public static bool operator ==(HotkeyBinding left, HotkeyBinding right) => left.Equals(right);

        /// <summary>
        /// Determines whether two bindings are not equal.
        /// </summary>
        public static bool operator !=(HotkeyBinding left, HotkeyBinding right) => !left.Equals(right);
    }
}
