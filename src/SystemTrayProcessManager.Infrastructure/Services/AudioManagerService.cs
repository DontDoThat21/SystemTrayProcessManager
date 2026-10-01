using Microsoft.Extensions.Logging;
using SystemTrayProcessManager.Core.Models;
using SystemTrayProcessManager.Core.Services;

namespace SystemTrayProcessManager.Infrastructure.Services;

/// <summary>Controls all current audio sessions of an application across playback devices.</summary>
public sealed class AudioManagerService : IAudioService
{
    private readonly ILogger<AudioManagerService> _logger;
    private readonly IAudioSessionProvider _sessions;
    private readonly object _sessionLock = new();
    private volatile bool _disposed;

    /// <summary>Creates an audio manager using Windows audio session discovery.</summary>
    public AudioManagerService(ILogger<AudioManagerService> logger)
        : this(logger, new WindowsAudioSessionProvider(logger)) { }

    /// <summary>Creates an audio manager with an injected source of current audio sessions.</summary>
    public AudioManagerService(ILogger<AudioManagerService> logger, IAudioSessionProvider sessions)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _sessions = sessions ?? throw new ArgumentNullException(nameof(sessions));
    }

    /// <inheritdoc/>
    public bool IsAudioAvailable => !_disposed && _sessions.IsAvailable;

    /// <inheritdoc/>
    public Task<bool> MuteProcessAsync(int processId) => SetMuteAsync(processId, true);

    /// <inheritdoc/>
    public Task<bool> UnmuteProcessAsync(int processId) => SetMuteAsync(processId, false);

    /// <inheritdoc/>
    public Task<bool> ToggleMuteProcessAsync(int processId) => SetMuteAsync(processId, null);

    private Task<bool> SetMuteAsync(int processId, bool? mute) => WithProcessAsync(processId, sessions =>
    {
        // Mixed states converge: mute everything unless every session is already muted.
        bool targetMute = mute ?? !sessions.All(session => session.IsMuted);
        bool success = ApplyToAll(sessions, session => session.IsMuted = targetMute);
        _logger.LogInformation("Audio mute for PID {ProcessId}: {Muted}, {Count} sessions across {Devices} outputs, success={Success}",
            processId, targetMute, sessions.Count, sessions.Select(s => s.DeviceId).Distinct().Count(), success);
        return success;
    }, false);

    /// <inheritdoc/>
    public Task<bool> SetProcessVolumeAsync(int processId, float volume) => WithProcessAsync(processId,
        sessions => ApplyToAll(sessions, session => session.Volume = Math.Clamp(volume, 0, 1)), false);

    /// <inheritdoc/>
    public Task<float?> GetProcessVolumeAsync(int processId) => WithProcessAsync<float?>(processId,
        sessions => sessions.Max(session => session.Volume), null);

    /// <inheritdoc/>
    public Task<bool?> IsProcessMutedAsync(int processId) => WithProcessAsync<bool?>(processId,
        sessions => sessions.All(session => session.IsMuted), null);

    /// <inheritdoc/>
    public Task<AudioProcessInfo?> GetAudioProcessInfoAsync(int processId) => WithProcessAsync<AudioProcessInfo?>(processId,
        sessions => CreateInfo(processId, _sessions.GetProcessName(processId) ?? sessions[0].ProcessName, sessions), null);

    /// <inheritdoc/>
    public Task<IReadOnlyList<AudioProcessInfo>> GetAudioProcessesAsync() => WithSnapshotAsync<IReadOnlyList<AudioProcessInfo>>(sessions =>
    {
        var result = new List<AudioProcessInfo>();
        foreach (var group in sessions.GroupBy(session => session.ProcessId))
        {
            try { result.Add(CreateInfo(group.Key, group.First().ProcessName, group.ToList())); }
            catch (Exception ex) { _logger.LogDebug(ex, "Audio session disappeared for PID {ProcessId}", group.Key); }
        }
        return result;
    }, Array.Empty<AudioProcessInfo>());

    /// <inheritdoc/>
    public async Task RefreshAudioSessionsAsync() => await WithSnapshotAsync(sessions =>
    {
        _logger.LogDebug("Discovered {Count} current audio sessions", sessions.Count);
        return true;
    }, false).ConfigureAwait(false);

    private Task<T> WithProcessAsync<T>(int processId, Func<IReadOnlyList<IAudioSession>, T> action, T fallback)
    {
        if (processId <= 0 || _disposed) return Task.FromResult(fallback);
        return WithSnapshotAsync(sessions =>
        {
            var matches = sessions.Where(session => session.ProcessId == processId).ToList();
            // Retain the browser/multi-process-app fallback, but never match unknown names.
            if (matches.Count == 0)
            {
                string? processName = _sessions.GetProcessName(processId);
                if (!string.IsNullOrWhiteSpace(processName) && processName != "Unknown")
                {
                    matches = sessions.Where(session => string.Equals(session.ProcessName, processName, StringComparison.OrdinalIgnoreCase)).ToList();
                }
            }
            if (matches.Count == 0)
            {
                _logger.LogWarning("No audio session for PID {ProcessId} on any active playback device ({Count} sessions examined)", processId, sessions.Count);
                return fallback;
            }
            return action(matches);
        }, fallback);
    }

    private Task<T> WithSnapshotAsync<T>(Func<IReadOnlyList<IAudioSession>, T> action, T fallback) => Task.Run(() =>
    {
        // Hold the lock through the COM operation and disposal; snapshots never escape it.
        lock (_sessionLock)
        {
            if (_disposed) return fallback;
            IReadOnlyList<IAudioSession> snapshot = Array.Empty<IAudioSession>();
            try
            {
                // Always discover anew, including after device changes or game startup.
                snapshot = _sessions.GetSessions();
                return action(snapshot);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Audio session operation failed");
                return fallback;
            }
            finally
            {
                foreach (var session in snapshot)
                {
                    try { session.Dispose(); }
                    catch (Exception ex) { _logger.LogDebug(ex, "Failed to release an audio session"); }
                }
            }
        }
    });

    private bool ApplyToAll(IReadOnlyList<IAudioSession> sessions, Action<IAudioSession> action)
    {
        bool success = true;
        foreach (var session in sessions)
        {
            try { action(session); }
            catch (Exception ex)
            {
                success = false;
                _logger.LogWarning(ex, "Failed to update audio for PID {ProcessId} on {DeviceId}", session.ProcessId, session.DeviceId);
            }
        }
        return success;
    }

    private static AudioProcessInfo CreateInfo(int processId, string name, IReadOnlyList<IAudioSession> sessions)
    {
        var representative = sessions.FirstOrDefault(s => s.IsActive) ?? sessions[0];
        return new AudioProcessInfo
        {
            ProcessId = processId, ProcessName = name,
            DisplayName = string.IsNullOrEmpty(representative.DisplayName) ? name : representative.DisplayName,
            Volume = sessions.Max(session => session.Volume),
            IsMuted = sessions.All(session => session.IsMuted),
            IsActive = sessions.Any(session => session.IsActive),
            SessionIdentifier = representative.SessionIdentifier, IconPath = representative.IconPath
        };
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        lock (_sessionLock)
        {
            if (_disposed) return;
            _disposed = true;
            _logger.LogDebug("Audio manager disposed; all session snapshots released");
        }
    }
}
