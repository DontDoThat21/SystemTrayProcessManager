namespace SystemTrayProcessManager.Core.Models
{
    /// <summary>
    /// Associates a hotkey binding with an action to execute when triggered.
    /// </summary>
    /// <remarks>
    /// Supports both synchronous and asynchronous action callbacks.
    /// </remarks>
    public sealed class HotkeyRegistration
    {
        /// <summary>
        /// Gets the hotkey binding for this registration.
        /// </summary>
        public HotkeyBinding Binding { get; }

        /// <summary>
        /// Gets the synchronous action to execute, if any.
        /// </summary>
        public Action? Action { get; }

        /// <summary>
        /// Gets the asynchronous action to execute, if any.
        /// </summary>
        public Func<Task>? AsyncAction { get; }

        /// <summary>
        /// Gets whether the key event should be suppressed (not passed to other applications).
        /// </summary>
        public bool SuppressKey { get; }

        /// <summary>
        /// Gets whether this registration uses an async action.
        /// </summary>
        public bool IsAsync => AsyncAction != null;

        /// <summary>
        /// Initializes a new instance of the <see cref="HotkeyRegistration"/> class with a synchronous action.
        /// </summary>
        /// <param name="binding">The hotkey binding.</param>
        /// <param name="action">The action to execute when the hotkey is triggered.</param>
        /// <param name="suppressKey">Whether to suppress the key event.</param>
        /// <exception cref="ArgumentNullException">Thrown when action is null.</exception>
        public HotkeyRegistration(HotkeyBinding binding, Action action, bool suppressKey = false)
        {
            ArgumentNullException.ThrowIfNull(action);
            Binding = binding;
            Action = action;
            SuppressKey = suppressKey;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="HotkeyRegistration"/> class with an asynchronous action.
        /// </summary>
        /// <param name="binding">The hotkey binding.</param>
        /// <param name="asyncAction">The async action to execute when the hotkey is triggered.</param>
        /// <param name="suppressKey">Whether to suppress the key event.</param>
        /// <exception cref="ArgumentNullException">Thrown when asyncAction is null.</exception>
        public HotkeyRegistration(HotkeyBinding binding, Func<Task> asyncAction, bool suppressKey = false)
        {
            ArgumentNullException.ThrowIfNull(asyncAction);
            Binding = binding;
            AsyncAction = asyncAction;
            SuppressKey = suppressKey;
        }

        /// <summary>
        /// Executes the registered action (sync or async).
        /// </summary>
        /// <returns>A task representing the completion of the action.</returns>
        public async Task ExecuteAsync()
        {
            if (IsAsync && AsyncAction != null)
            {
                await AsyncAction().ConfigureAwait(false);
            }
            else if (Action != null)
            {
                await Task.Run(Action).ConfigureAwait(false);
            }
        }
    }
}
