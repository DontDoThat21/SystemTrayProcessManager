using Microsoft.Extensions.Logging;
using NAudio.CoreAudioApi;
using NAudio.CoreAudioApi.Interfaces;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Runtime.InteropServices;
using SystemTrayProcessManager.Core.Models;
using SystemTrayProcessManager.Core.Services;

namespace SystemTrayProcessManager.Infrastructure.Services
{
    /// <summary>
    /// Provides per-process audio control using Windows Core Audio API (WASAPI) via NAudio.
    /// Manages audio sessions for mute/unmute and volume control operations.
    /// </summary>
    /// <remarks>
    /// This service uses the Windows Audio Session API (WASAPI) to interact with
    /// individual application audio sessions. Audio sessions are cached for performance
    /// and refreshed as needed when sessions become invalid.
    /// </remarks>
    public sealed class AudioManagerService : IAudioService, IMMNotificationClient
    {
        private readonly ILogger<AudioManagerService> _logger;
        private readonly SemaphoreSlim _sessionLock = new(1, 1);
        private readonly ConcurrentDictionary<int, CachedAudioSession> _sessionCache = new();

        private MMDeviceEnumerator? _deviceEnumerator;
        private MMDevice? _defaultDevice;
        private bool _disposed;
        private bool _isAudioAvailable;

        /// <summary>
        /// Represents a cached audio session with its control interfaces.
        /// </summary>
        private sealed class CachedAudioSession : IDisposable
        {
            public AudioSessionControl SessionControl { get; }
            public SimpleAudioVolume VolumeControl { get; }
            public int ProcessId { get; }
            public string DisplayName { get; }
            public string SessionIdentifier { get; }
            public string? IconPath { get; }
            public DateTime CachedAt { get; }

            public CachedAudioSession(
                AudioSessionControl sessionControl,
                int processId,
                string displayName,
                string sessionIdentifier,
                string? iconPath)
            {
                SessionControl = sessionControl;
                VolumeControl = sessionControl.SimpleAudioVolume;
                ProcessId = processId;
                DisplayName = displayName;
                SessionIdentifier = sessionIdentifier;
                IconPath = iconPath;
                CachedAt = DateTime.UtcNow;
            }

            public void Dispose()
            {
                try
                {
                    SessionControl?.Dispose();
                }
                catch
                {
                    // Ignore disposal errors
                }
            }
        }

        /// <inheritdoc/>
        public bool IsAudioAvailable => _isAudioAvailable && !_disposed;

        /// <summary>
        /// Initializes a new instance of the <see cref="AudioManagerService"/> class.
        /// </summary>
        /// <param name="logger">Logger instance for diagnostic output.</param>
        /// <exception cref="ArgumentNullException">Thrown when logger is null.</exception>
        public AudioManagerService(ILogger<AudioManagerService> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            InitializeAudioDevices();
        }

        /// <summary>
        /// Initializes the audio device enumerator and gets the default audio endpoint.
        /// </summary>
        private void InitializeAudioDevices()
        {
            try
            {
                _logger.LogDebug("Initializing audio devices...");

                _deviceEnumerator = new MMDeviceEnumerator();
                _deviceEnumerator.RegisterEndpointNotificationCallback(this);

                _defaultDevice = _deviceEnumerator.GetDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia);
                _isAudioAvailable = _defaultDevice != null;

                if (_isAudioAvailable)
                {
                    _logger.LogInformation("Audio device initialized: {DeviceName}", _defaultDevice!.FriendlyName);
                }
                else
                {
                    _logger.LogWarning("No default audio device found");
                }
            }
            catch (COMException ex)
            {
                _logger.LogError(ex, "COM error initializing audio devices. Error code: 0x{ErrorCode:X8}", ex.HResult);
                _isAudioAvailable = false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to initialize audio devices");
                _isAudioAvailable = false;
            }
        }

        /// <inheritdoc/>
        public async Task<bool> MuteProcessAsync(int processId)
        {
            if (_disposed || !_isAudioAvailable)
            {
                _logger.LogDebug("MuteProcessAsync called but audio is not available");
                return false;
            }

            if (processId <= 0)
            {
                _logger.LogWarning("Invalid process ID: {ProcessId}", processId);
                return false;
            }

            try
            {
                _logger.LogDebug("Muting process: {ProcessId}", processId);

                var session = await GetOrCreateSessionAsync(processId);
                if (session == null)
                {
                    _logger.LogDebug("No audio session found for process: {ProcessId}", processId);
                    return false;
                }

                session.VolumeControl.Mute = true;
                _logger.LogInformation("Successfully muted process: {ProcessId}", processId);
                return true;
            }
            catch (COMException ex)
            {
                _logger.LogError(ex, "COM error muting process {ProcessId}. Error code: 0x{ErrorCode:X8}", processId, ex.HResult);
                InvalidateSession(processId);
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to mute process: {ProcessId}", processId);
                return false;
            }
        }

        /// <inheritdoc/>
        public async Task<bool> UnmuteProcessAsync(int processId)
        {
            if (_disposed || !_isAudioAvailable)
            {
                _logger.LogDebug("UnmuteProcessAsync called but audio is not available");
                return false;
            }

            if (processId <= 0)
            {
                _logger.LogWarning("Invalid process ID: {ProcessId}", processId);
                return false;
            }

            try
            {
                _logger.LogDebug("Unmuting process: {ProcessId}", processId);

                var session = await GetOrCreateSessionAsync(processId);
                if (session == null)
                {
                    _logger.LogDebug("No audio session found for process: {ProcessId}", processId);
                    return false;
                }

                session.VolumeControl.Mute = false;
                _logger.LogInformation("Successfully unmuted process: {ProcessId}", processId);
                return true;
            }
            catch (COMException ex)
            {
                _logger.LogError(ex, "COM error unmuting process {ProcessId}. Error code: 0x{ErrorCode:X8}", processId, ex.HResult);
                InvalidateSession(processId);
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to unmute process: {ProcessId}", processId);
                return false;
            }
        }

        /// <inheritdoc/>
        public async Task<bool> ToggleMuteProcessAsync(int processId)
        {
            if (_disposed || !_isAudioAvailable)
            {
                _logger.LogDebug("ToggleMuteProcessAsync called but audio is not available");
                return false;
            }

            if (processId <= 0)
            {
                _logger.LogWarning("Invalid process ID: {ProcessId}", processId);
                return false;
            }

            try
            {
                _logger.LogDebug("Toggling mute for process: {ProcessId}", processId);

                var session = await GetOrCreateSessionAsync(processId);
                if (session == null)
                {
                    _logger.LogDebug("No audio session found for process: {ProcessId}", processId);
                    return false;
                }

                bool currentMute = session.VolumeControl.Mute;
                session.VolumeControl.Mute = !currentMute;

                _logger.LogInformation("Successfully toggled mute for process {ProcessId}: {PreviousState} -> {NewState}",
                    processId, currentMute ? "Muted" : "Unmuted", !currentMute ? "Muted" : "Unmuted");
                return true;
            }
            catch (COMException ex)
            {
                _logger.LogError(ex, "COM error toggling mute for process {ProcessId}. Error code: 0x{ErrorCode:X8}", processId, ex.HResult);
                InvalidateSession(processId);
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to toggle mute for process: {ProcessId}", processId);
                return false;
            }
        }

        /// <inheritdoc/>
        public async Task<bool> SetProcessVolumeAsync(int processId, float volume)
        {
            if (_disposed || !_isAudioAvailable)
            {
                _logger.LogDebug("SetProcessVolumeAsync called but audio is not available");
                return false;
            }

            if (processId <= 0)
            {
                _logger.LogWarning("Invalid process ID: {ProcessId}", processId);
                return false;
            }

            // Clamp volume to valid range
            float clampedVolume = Math.Clamp(volume, 0.0f, 1.0f);
            if (Math.Abs(volume - clampedVolume) > 0.0001f)
            {
                _logger.LogDebug("Volume clamped from {RequestedVolume} to {ClampedVolume}", volume, clampedVolume);
            }

            try
            {
                _logger.LogDebug("Setting volume for process {ProcessId} to {Volume:P0}", processId, clampedVolume);

                var session = await GetOrCreateSessionAsync(processId);
                if (session == null)
                {
                    _logger.LogDebug("No audio session found for process: {ProcessId}", processId);
                    return false;
                }

                session.VolumeControl.Volume = clampedVolume;
                _logger.LogInformation("Successfully set volume for process {ProcessId} to {Volume:P0}", processId, clampedVolume);
                return true;
            }
            catch (COMException ex)
            {
                _logger.LogError(ex, "COM error setting volume for process {ProcessId}. Error code: 0x{ErrorCode:X8}", processId, ex.HResult);
                InvalidateSession(processId);
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to set volume for process: {ProcessId}", processId);
                return false;
            }
        }

        /// <inheritdoc/>
        public async Task<float?> GetProcessVolumeAsync(int processId)
        {
            if (_disposed || !_isAudioAvailable)
            {
                _logger.LogDebug("GetProcessVolumeAsync called but audio is not available");
                return null;
            }

            if (processId <= 0)
            {
                _logger.LogWarning("Invalid process ID: {ProcessId}", processId);
                return null;
            }

            try
            {
                _logger.LogDebug("Getting volume for process: {ProcessId}", processId);

                var session = await GetOrCreateSessionAsync(processId);
                if (session == null)
                {
                    _logger.LogDebug("No audio session found for process: {ProcessId}", processId);
                    return null;
                }

                float volume = session.VolumeControl.Volume;
                _logger.LogDebug("Process {ProcessId} volume: {Volume:P0}", processId, volume);
                return volume;
            }
            catch (COMException ex)
            {
                _logger.LogError(ex, "COM error getting volume for process {ProcessId}. Error code: 0x{ErrorCode:X8}", processId, ex.HResult);
                InvalidateSession(processId);
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get volume for process: {ProcessId}", processId);
                return null;
            }
        }

        /// <inheritdoc/>
        public async Task<bool?> IsProcessMutedAsync(int processId)
        {
            if (_disposed || !_isAudioAvailable)
            {
                _logger.LogDebug("IsProcessMutedAsync called but audio is not available");
                return null;
            }

            if (processId <= 0)
            {
                _logger.LogWarning("Invalid process ID: {ProcessId}", processId);
                return null;
            }

            try
            {
                _logger.LogDebug("Checking mute status for process: {ProcessId}", processId);

                var session = await GetOrCreateSessionAsync(processId);
                if (session == null)
                {
                    _logger.LogDebug("No audio session found for process: {ProcessId}", processId);
                    return null;
                }

                bool isMuted = session.VolumeControl.Mute;
                _logger.LogDebug("Process {ProcessId} mute status: {IsMuted}", processId, isMuted);
                return isMuted;
            }
            catch (COMException ex)
            {
                _logger.LogError(ex, "COM error checking mute for process {ProcessId}. Error code: 0x{ErrorCode:X8}", processId, ex.HResult);
                InvalidateSession(processId);
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to check mute status for process: {ProcessId}", processId);
                return null;
            }
        }

        /// <inheritdoc/>
        public async Task<IReadOnlyList<AudioProcessInfo>> GetAudioProcessesAsync()
        {
            if (_disposed || !_isAudioAvailable)
            {
                _logger.LogDebug("GetAudioProcessesAsync called but audio is not available");
                return Array.Empty<AudioProcessInfo>();
            }

            try
            {
                _logger.LogDebug("Enumerating audio sessions...");

                await _sessionLock.WaitAsync();
                try
                {
                    var audioProcesses = new List<AudioProcessInfo>();
                    await RefreshSessionCacheInternalAsync();

                    foreach (var kvp in _sessionCache)
                    {
                        try
                        {
                            var session = kvp.Value;
                            string processName = GetProcessNameSafe(session.ProcessId);

                            var audioInfo = new AudioProcessInfo
                            {
                                ProcessId = session.ProcessId,
                                ProcessName = processName,
                                DisplayName = string.IsNullOrEmpty(session.DisplayName) ? processName : session.DisplayName,
                                Volume = session.VolumeControl.Volume,
                                IsMuted = session.VolumeControl.Mute,
                                IsActive = session.SessionControl.State == AudioSessionState.AudioSessionStateActive,
                                SessionIdentifier = session.SessionIdentifier,
                                IconPath = session.IconPath
                            };

                            audioProcesses.Add(audioInfo);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogDebug(ex, "Error getting audio info for cached session (PID: {ProcessId})", kvp.Key);
                            InvalidateSession(kvp.Key);
                        }
                    }

                    _logger.LogDebug("Found {Count} audio processes", audioProcesses.Count);
                    return audioProcesses.AsReadOnly();
                }
                finally
                {
                    _sessionLock.Release();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to enumerate audio processes");
                return Array.Empty<AudioProcessInfo>();
            }
        }

        /// <inheritdoc/>
        public async Task<AudioProcessInfo?> GetAudioProcessInfoAsync(int processId)
        {
            if (_disposed || !_isAudioAvailable)
            {
                return null;
            }

            if (processId <= 0)
            {
                return null;
            }

            try
            {
                var session = await GetOrCreateSessionAsync(processId);
                if (session == null)
                {
                    return null;
                }

                string processName = GetProcessNameSafe(processId);
                return new AudioProcessInfo
                {
                    ProcessId = processId,
                    ProcessName = processName,
                    DisplayName = string.IsNullOrEmpty(session.DisplayName) ? processName : session.DisplayName,
                    Volume = session.VolumeControl.Volume,
                    IsMuted = session.VolumeControl.Mute,
                    IsActive = session.SessionControl.State == AudioSessionState.AudioSessionStateActive,
                    SessionIdentifier = session.SessionIdentifier,
                    IconPath = session.IconPath
                };
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Error getting audio process info for PID: {ProcessId}", processId);
                return null;
            }
        }

        /// <inheritdoc/>
        public async Task RefreshAudioSessionsAsync()
        {
            if (_disposed || !_isAudioAvailable)
            {
                _logger.LogDebug("RefreshAudioSessionsAsync called but audio is not available");
                return;
            }

            try
            {
                _logger.LogDebug("Refreshing audio sessions...");

                await _sessionLock.WaitAsync();
                try
                {
                    // Clear existing cache
                    ClearSessionCache();

                    // Rebuild cache
                    await RefreshSessionCacheInternalAsync();

                    _logger.LogInformation("Audio sessions refreshed. Found {Count} sessions", _sessionCache.Count);
                }
                finally
                {
                    _sessionLock.Release();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to refresh audio sessions");
            }
        }

        /// <summary>
        /// Gets or creates a cached audio session for the specified process.
        /// Falls back to matching by process name when the exact PID has no session
        /// (common for multi-process apps like Chrome where audio lives under a child PID).
        /// </summary>
        private async Task<CachedAudioSession?> GetOrCreateSessionAsync(int processId)
        {
            // Check cache first
            if (_sessionCache.TryGetValue(processId, out var cachedSession))
            {
                return cachedSession;
            }

            // Refresh cache and try again
            await _sessionLock.WaitAsync();
            try
            {
                // Double-check after acquiring lock
                if (_sessionCache.TryGetValue(processId, out cachedSession))
                {
                    return cachedSession;
                }

                await RefreshSessionCacheInternalAsync();

                if (_sessionCache.TryGetValue(processId, out cachedSession))
                {
                    return cachedSession;
                }

                // Fallback: find a session from a process with the same name.
                // Multi-process apps (e.g. Chrome) have audio under a child PID,
                // not the main-window PID that is displayed in the process list.
                string? targetName = GetProcessNameSafe(processId);
                if (targetName is not null && targetName != "Unknown")
                {
                    cachedSession = _sessionCache.Values
                        .FirstOrDefault(s =>
                        {
                            string sessionProcessName = GetProcessNameSafe(s.ProcessId);
                            return string.Equals(sessionProcessName, targetName, StringComparison.OrdinalIgnoreCase);
                        });
                }

                return cachedSession;
            }
            finally
            {
                _sessionLock.Release();
            }
        }

        /// <summary>
        /// Refreshes the internal session cache from the audio device.
        /// Must be called with _sessionLock held.
        /// </summary>
        private Task RefreshSessionCacheInternalAsync()
        {
            if (_defaultDevice == null || _disposed)
            {
                return Task.CompletedTask;
            }

            try
            {
                var sessionManager = _defaultDevice.AudioSessionManager;
                var sessions = sessionManager.Sessions;

                for (int i = 0; i < sessions.Count; i++)
                {
                    try
                    {
                        var session = sessions[i];
                        int pid = (int)session.GetProcessID;

                        // Skip system sounds (PID 0) and already cached sessions
                        if (pid == 0 || _sessionCache.ContainsKey(pid))
                        {
                            continue;
                        }

                        var cachedSession = new CachedAudioSession(
                            session,
                            pid,
                            session.DisplayName,
                            session.GetSessionIdentifier,
                            session.IconPath);

                        _sessionCache.TryAdd(pid, cachedSession);

                        _logger.LogDebug("Cached audio session for process {ProcessId} ({DisplayName})",
                            pid, cachedSession.DisplayName);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogDebug(ex, "Error caching audio session at index {Index}", i);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error refreshing session cache");
            }

            return Task.CompletedTask;
        }

        /// <summary>
        /// Invalidates and removes a session from the cache.
        /// </summary>
        private void InvalidateSession(int processId)
        {
            if (_sessionCache.TryRemove(processId, out var session))
            {
                _logger.LogDebug("Invalidated audio session for process: {ProcessId}", processId);
                session.Dispose();
            }
        }

        /// <summary>
        /// Clears all cached sessions.
        /// </summary>
        private void ClearSessionCache()
        {
            foreach (var kvp in _sessionCache)
            {
                kvp.Value.Dispose();
            }
            _sessionCache.Clear();
            _logger.LogDebug("Session cache cleared");
        }

        /// <summary>
        /// Gets the process name safely, returning "Unknown" if the process is not accessible.
        /// </summary>
        private string GetProcessNameSafe(int processId)
        {
            try
            {
                using var process = Process.GetProcessById(processId);
                return process.ProcessName;
            }
            catch
            {
                return "Unknown";
            }
        }

        #region IMMNotificationClient Implementation

        /// <summary>
        /// Called when the default audio device changes.
        /// </summary>
        void IMMNotificationClient.OnDefaultDeviceChanged(DataFlow flow, Role role, string defaultDeviceId)
        {
            if (flow == DataFlow.Render && role == Role.Multimedia)
            {
                _logger.LogInformation("Default audio device changed. Refreshing...");

                try
                {
                    // Clear cache and reinitialize
                    ClearSessionCache();
                    _defaultDevice?.Dispose();
                    _defaultDevice = _deviceEnumerator?.GetDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia);
                    _isAudioAvailable = _defaultDevice != null;

                    if (_isAudioAvailable)
                    {
                        _logger.LogInformation("New default audio device: {DeviceName}", _defaultDevice!.FriendlyName);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error handling default device change");
                    _isAudioAvailable = false;
                }
            }
        }

        void IMMNotificationClient.OnDeviceAdded(string pwstrDeviceId)
        {
            _logger.LogDebug("Audio device added: {DeviceId}", pwstrDeviceId);
        }

        void IMMNotificationClient.OnDeviceRemoved(string deviceId)
        {
            _logger.LogDebug("Audio device removed: {DeviceId}", deviceId);
        }

        void IMMNotificationClient.OnDeviceStateChanged(string deviceId, DeviceState newState)
        {
            _logger.LogDebug("Audio device state changed: {DeviceId} -> {State}", deviceId, newState);
        }

        void IMMNotificationClient.OnPropertyValueChanged(string pwstrDeviceId, PropertyKey key)
        {
            // Ignored - too verbose
        }

        #endregion

        /// <summary>
        /// Disposes the audio manager service and releases all resources.
        /// </summary>
        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            _logger.LogDebug("Disposing AudioManagerService...");

            try
            {
                // Clear session cache
                ClearSessionCache();

                // Unregister notification callback
                if (_deviceEnumerator != null)
                {
                    try
                    {
                        _deviceEnumerator.UnregisterEndpointNotificationCallback(this);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogDebug(ex, "Error unregistering notification callback");
                    }
                }

                // Dispose COM objects
                _defaultDevice?.Dispose();
                _deviceEnumerator?.Dispose();

                _sessionLock.Dispose();

                _logger.LogInformation("AudioManagerService disposed successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error disposing AudioManagerService");
            }
        }
    }
}
