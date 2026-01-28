namespace SystemTrayProcessManager.Core.Models
{
    /// <summary>
    /// Provides data for the HotkeyError event.
    /// </summary>
    public sealed class HotkeyErrorEventArgs : EventArgs
    {
        /// <summary>
        /// Gets the exception that occurred.
        /// </summary>
        public Exception Exception { get; }

        /// <summary>
        /// Gets the hotkey binding that caused the error, if known.
        /// </summary>
        public HotkeyBinding? Binding { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="HotkeyErrorEventArgs"/> class.
        /// </summary>
        /// <param name="exception">The exception that occurred.</param>
        /// <param name="binding">The hotkey binding that caused the error, if known.</param>
        public HotkeyErrorEventArgs(Exception exception, HotkeyBinding? binding = null)
        {
            ArgumentNullException.ThrowIfNull(exception);
            Exception = exception;
            Binding = binding;
        }
    }
}
