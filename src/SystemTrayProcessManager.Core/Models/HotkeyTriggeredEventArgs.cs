namespace SystemTrayProcessManager.Core.Models
{
    /// <summary>
    /// Provides data for the HotkeyTriggered event.
    /// </summary>
    public sealed class HotkeyTriggeredEventArgs : EventArgs
    {
        /// <summary>
        /// Gets the hotkey binding that was triggered.
        /// </summary>
        public HotkeyBinding Binding { get; }

        /// <summary>
        /// Gets the response time in milliseconds from key press to action execution.
        /// </summary>
        public double ResponseTimeMs { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="HotkeyTriggeredEventArgs"/> class.
        /// </summary>
        /// <param name="binding">The hotkey binding that was triggered.</param>
        /// <param name="responseTimeMs">The response time in milliseconds.</param>
        public HotkeyTriggeredEventArgs(HotkeyBinding binding, double responseTimeMs)
        {
            Binding = binding;
            ResponseTimeMs = responseTimeMs;
        }
    }
}
