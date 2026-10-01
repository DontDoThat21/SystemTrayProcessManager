namespace SystemTrayProcessManager.Core.Services;

/// <summary>Discovers current application sessions across all active playback devices.</summary>
public interface IAudioSessionProvider
{
    /// <summary>Gets whether an active playback device was found during the latest discovery.</summary>
    bool IsAvailable { get; }
    /// <summary>Enumerates a fresh snapshot. The caller must dispose every returned session.</summary>
    IReadOnlyList<IAudioSession> GetSessions();
    /// <summary>Resolves an executable name, or returns null if the process is inaccessible.</summary>
    string? GetProcessName(int processId);
}
