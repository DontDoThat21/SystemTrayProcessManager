using Microsoft.Extensions.Logging;
using NAudio.CoreAudioApi;
using NAudio.CoreAudioApi.Interfaces;
using System.Diagnostics;
using SystemTrayProcessManager.Core.Services;

namespace SystemTrayProcessManager.Infrastructure.Services;

/// <summary>Uses fresh WASAPI enumerators to discover sessions on every active render endpoint.</summary>
public sealed class WindowsAudioSessionProvider : IAudioSessionProvider
{
    private readonly ILogger _logger;
    private volatile bool _isAvailable;

    /// <summary>Creates the native session provider.</summary>
    public WindowsAudioSessionProvider(ILogger logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        try
        {
            using var enumerator = new MMDeviceEnumerator();
            _isAvailable = enumerator.EnumerateAudioEndPoints(DataFlow.Render, DeviceState.Active).Count > 0;
        }
        catch (Exception ex) { _logger.LogWarning(ex, "Unable to discover playback devices"); }
    }

    /// <inheritdoc/>
    public bool IsAvailable => _isAvailable;

    /// <inheritdoc/>
    public IReadOnlyList<IAudioSession> GetSessions()
    {
        var result = new List<IAudioSession>();
        var processNames = new Dictionary<int, string>();
        try
        {
            using var enumerator = new MMDeviceEnumerator();
            var devices = enumerator.EnumerateAudioEndPoints(DataFlow.Render, DeviceState.Active);
            _isAvailable = devices.Count > 0;
            foreach (var device in devices)
            {
                using (device)
                {
                    try
                    {
                        var manager = device.AudioSessionManager;
                        // NAudio's Sessions property is a snapshot, not a live list.
                        manager.RefreshSessions();
                        var sessions = manager.Sessions;
                        for (int index = 0; index < sessions.Count; index++)
                        {
                            AudioSessionControl? control = null;
                            try
                            {
                                control = sessions[index];
                                int processId = (int)control.GetProcessID;
                                if (processId <= 0 || control.State == AudioSessionState.AudioSessionStateExpired) continue;
                                if (!processNames.TryGetValue(processId, out var name))
                                {
                                    name = GetProcessName(processId) ?? "Unknown";
                                    processNames[processId] = name;
                                }
                                result.Add(new WindowsAudioSession(control, processId, name, device.ID));
                                control = null; // Ownership transfers to the caller through the wrapper.
                            }
                            catch (Exception ex) { _logger.LogDebug(ex, "Unable to read audio session {Index}", index); }
                            finally { control?.Dispose(); }
                        }
                    }
                    catch (Exception ex) { _logger.LogWarning(ex, "Unable to enumerate an audio output; continuing with other outputs"); }
                }
            }
        }
        catch (Exception ex)
        {
            _isAvailable = false;
            _logger.LogError(ex, "Unable to enumerate playback sessions");
        }
        return result;
    }

    /// <inheritdoc/>
    public string? GetProcessName(int processId)
    {
        try { using var process = Process.GetProcessById(processId); return process.ProcessName; }
        catch { return null; }
    }

    private sealed class WindowsAudioSession : IAudioSession
    {
        private readonly AudioSessionControl _control;
        private readonly SimpleAudioVolume _volume;
        public WindowsAudioSession(AudioSessionControl control, int processId, string name, string deviceId)
        {
            _control = control;
            _volume = control.SimpleAudioVolume;
            ProcessId = processId;
            ProcessName = name;
            DeviceId = deviceId;
            SessionIdentifier = control.GetSessionInstanceIdentifier;
            DisplayName = control.DisplayName;
            IconPath = control.IconPath;
        }
        public int ProcessId { get; }
        public string ProcessName { get; }
        public string DeviceId { get; }
        public string SessionIdentifier { get; }
        public string DisplayName { get; }
        public string? IconPath { get; }
        public bool IsActive => _control.State == AudioSessionState.AudioSessionStateActive;
        public bool IsMuted { get => _volume.Mute; set => _volume.Mute = value; }
        public float Volume { get => _volume.Volume; set => _volume.Volume = value; }
        public void Dispose()
        {
            _volume.Dispose();
            _control.Dispose();
        }
    }
}
