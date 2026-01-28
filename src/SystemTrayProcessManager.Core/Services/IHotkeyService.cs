using SystemTrayProcessManager.Core.Models;

namespace SystemTrayProcessManager.Core.Services
{
    /// <summary>
    /// Provides global hotkey registration and management capabilities.
    /// </summary>
    public interface IHotkeyService : IDisposable
    {
        /// <summary>
        /// Gets a value indicating whether the hotkey service is initialized.
        /// </summary>
        bool IsInitialized { get; }

        /// <summary>
        /// Gets a value indicating whether the hotkey service is suspended.
        /// </summary>
        bool IsSuspended { get; }

        /// <summary>
        /// Gets the number of registered hotkeys.
        /// </summary>
        int RegisteredCount { get; }

        /// <summary>
        /// Occurs when a registered hotkey is triggered.
        /// </summary>
        event EventHandler<HotkeyTriggeredEventArgs>? HotkeyTriggered;

        /// <summary>
        /// Occurs when an error occurs during hotkey processing.
        /// </summary>
        event EventHandler<HotkeyErrorEventArgs>? HotkeyError;

        /// <summary>
        /// Initializes the hotkey service and installs the keyboard hook.
        /// </summary>
        /// <exception cref="InvalidOperationException">Thrown when initialization fails.</exception>
        void Initialize();

        /// <summary>
        /// Registers a hotkey with a synchronous action.
        /// </summary>
        /// <param name="binding">The hotkey binding to register.</param>
        /// <param name="action">The action to execute when the hotkey is triggered.</param>
        /// <param name="suppressKey">Whether to suppress the key event.</param>
        /// <returns>True if registration succeeded; otherwise, false.</returns>
        bool RegisterHotkey(HotkeyBinding binding, Action action, bool suppressKey = false);

        /// <summary>
        /// Registers a hotkey with an asynchronous action.
        /// </summary>
        /// <param name="binding">The hotkey binding to register.</param>
        /// <param name="asyncAction">The async action to execute when the hotkey is triggered.</param>
        /// <param name="suppressKey">Whether to suppress the key event.</param>
        /// <returns>True if registration succeeded; otherwise, false.</returns>
        bool RegisterHotkey(HotkeyBinding binding, Func<Task> asyncAction, bool suppressKey = false);

        /// <summary>
        /// Unregisters a previously registered hotkey.
        /// </summary>
        /// <param name="binding">The hotkey binding to unregister.</param>
        /// <returns>True if unregistration succeeded; otherwise, false.</returns>
        bool UnregisterHotkey(HotkeyBinding binding);

        /// <summary>
        /// Unregisters all registered hotkeys.
        /// </summary>
        void UnregisterAllHotkeys();

        /// <summary>
        /// Determines whether the specified hotkey is registered.
        /// </summary>
        /// <param name="binding">The hotkey binding to check.</param>
        /// <returns>True if the hotkey is registered; otherwise, false.</returns>
        bool IsHotkeyRegistered(HotkeyBinding binding);

        /// <summary>
        /// Gets a collection of all registered hotkey bindings.
        /// </summary>
        /// <returns>A read-only collection of registered hotkey bindings.</returns>
        IReadOnlyCollection<HotkeyBinding> GetRegisteredHotkeys();

        /// <summary>
        /// Temporarily suspends hotkey processing.
        /// </summary>
        void Suspend();

        /// <summary>
        /// Resumes hotkey processing after suspension.
        /// </summary>
        void Resume();

        /// <summary>
        /// Shuts down the hotkey service and removes the keyboard hook.
        /// </summary>
        void Shutdown();
    }
}
