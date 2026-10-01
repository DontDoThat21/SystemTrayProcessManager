namespace SystemTrayProcessManager.Core.Services;

/// <summary>A disposable application audio session on a particular output device.</summary>
public interface IAudioSession : IDisposable
{
    /// <summary>Gets the owning process identifier.</summary>
    int ProcessId { get; }
    /// <summary>Gets the owning executable name.</summary>
    string ProcessName { get; }
    /// <summary>Gets the audio output identifier.</summary>
    string DeviceId { get; }
    /// <summary>Gets the session instance identifier.</summary>
    string SessionIdentifier { get; }
    /// <summary>Gets the session display name.</summary>
    string DisplayName { get; }
    /// <summary>Gets the optional application icon path.</summary>
    string? IconPath { get; }
    /// <summary>Gets whether the session currently produces audio.</summary>
    bool IsActive { get; }
    /// <summary>Gets or sets the mute state.</summary>
    bool IsMuted { get; set; }
    /// <summary>Gets or sets volume between zero and one.</summary>
    float Volume { get; set; }
}
