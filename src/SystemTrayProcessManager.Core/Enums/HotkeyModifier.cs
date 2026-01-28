namespace SystemTrayProcessManager.Core.Enums
{
    /// <summary>
    /// Represents keyboard modifier keys for hotkey combinations.
    /// This is a flags enum allowing combinations of multiple modifiers.
    /// </summary>
    [Flags]
    public enum HotkeyModifier
    {
        /// <summary>
        /// No modifier key.
        /// </summary>
        None = 0,

        /// <summary>
        /// Control key (left or right).
        /// </summary>
        Ctrl = 1,

        /// <summary>
        /// Alt key (left or right).
        /// </summary>
        Alt = 2,

        /// <summary>
        /// Shift key (left or right).
        /// </summary>
        Shift = 4,

        /// <summary>
        /// Windows key (left or right).
        /// </summary>
        Win = 8
    }
}
