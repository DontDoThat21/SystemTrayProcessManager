using System.Collections.Concurrent;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using SystemTrayProcessManager.Core.Enums;
using SystemTrayProcessManager.Core.Models;
using SystemTrayProcessManager.Core.Services;
using SystemTrayProcessManager.Infrastructure.WindowsAPI;

namespace SystemTrayProcessManager.Infrastructure.Services
{
    /// <summary>
    /// Provides process profile management with file-based persistence.
    /// Handles saving, loading, and applying process-specific configurations.
    /// </summary>
    public sealed class ProfileService : IProfileService
    {
        private readonly ILogger<ProfileService> _logger;
        private readonly IWindowService _windowService;
        private readonly IAudioService _audioService;
        private readonly IProcessService _processService;
        private readonly string _configDirectory;
        private readonly string _configFilePath;
        private readonly string _backupFilePath;
        private readonly ConcurrentDictionary<string, ProcessProfile> _profileCache;
        private readonly SemaphoreSlim _fileLock = new(1, 1);
        private bool _isInitialized;

        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
        };

        /// <inheritdoc/>
        public event EventHandler<ProfileAppliedEventArgs>? ProfileApplied;

        /// <summary>
        /// Initializes a new instance of the <see cref="ProfileService"/> class.
        /// </summary>
        /// <param name="logger">The logger instance.</param>
        /// <param name="windowService">The window manipulation service.</param>
        /// <param name="audioService">The audio control service.</param>
        /// <param name="processService">The process monitoring service.</param>
        public ProfileService(
            ILogger<ProfileService> logger,
            IWindowService windowService,
            IAudioService audioService,
            IProcessService processService)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _windowService = windowService ?? throw new ArgumentNullException(nameof(windowService));
            _audioService = audioService ?? throw new ArgumentNullException(nameof(audioService));
            _processService = processService ?? throw new ArgumentNullException(nameof(processService));

            _profileCache = new ConcurrentDictionary<string, ProcessProfile>(StringComparer.OrdinalIgnoreCase);

            _configDirectory = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "SystemTrayProcessManager");

            _configFilePath = Path.Combine(_configDirectory, "profiles.json");
            _backupFilePath = Path.Combine(_configDirectory, "profiles.backup.json");

            _logger.LogDebug("ProfileService initialized. Config path: {Path}", _configFilePath);

            // Subscribe to process started events for auto-apply
            _processService.ProcessStarted += OnProcessStarted;
        }

        /// <summary>
        /// Ensures the service is initialized by loading profiles from disk.
        /// </summary>
        private async Task EnsureInitializedAsync()
        {
            if (_isInitialized) return;

            await _fileLock.WaitAsync().ConfigureAwait(false);
            try
            {
                if (_isInitialized) return;

                var config = await LoadConfigurationInternalAsync().ConfigureAwait(false);
                foreach (var profile in config.Profiles)
                {
                    _profileCache[profile.ProcessName] = profile;
                }

                _isInitialized = true;
                _logger.LogInformation("ProfileService initialized with {Count} profiles", _profileCache.Count);
            }
            finally
            {
                _fileLock.Release();
            }
        }

        /// <inheritdoc/>
        public async Task<ProcessProfile?> GetProfileAsync(string processName)
        {
            if (string.IsNullOrWhiteSpace(processName))
            {
                _logger.LogWarning("GetProfileAsync called with empty process name");
                return null;
            }

            await EnsureInitializedAsync().ConfigureAwait(false);

            if (_profileCache.TryGetValue(processName, out var profile))
            {
                _logger.LogDebug("Found profile for process {ProcessName}: {ProfileId}", processName, profile.Id);
                return profile;
            }

            _logger.LogDebug("No profile found for process {ProcessName}", processName);
            return null;
        }

        /// <inheritdoc/>
        public async Task<ProcessProfile?> GetProfileByIdAsync(Guid profileId)
        {
            await EnsureInitializedAsync().ConfigureAwait(false);

            var profile = _profileCache.Values.FirstOrDefault(p => p.Id == profileId);
            if (profile != null)
            {
                _logger.LogDebug("Found profile by ID {ProfileId}", profileId);
            }

            return profile;
        }

        /// <inheritdoc/>
        public async Task<IReadOnlyList<ProcessProfile>> GetAllProfilesAsync()
        {
            await EnsureInitializedAsync().ConfigureAwait(false);

            var profiles = _profileCache.Values.ToList();
            _logger.LogDebug("Returning {Count} profiles", profiles.Count);
            return profiles.AsReadOnly();
        }

        /// <inheritdoc/>
        public async Task<bool> SaveProfileAsync(ProcessProfile profile)
        {
            ArgumentNullException.ThrowIfNull(profile);

            if (string.IsNullOrWhiteSpace(profile.ProcessName))
            {
                _logger.LogWarning("Cannot save profile with empty process name");
                return false;
            }

            try
            {
                await EnsureInitializedAsync().ConfigureAwait(false);

                var updatedProfile = profile.WithUpdatedTimestamp();
                _profileCache[profile.ProcessName] = updatedProfile;

                await SaveConfigurationAsync().ConfigureAwait(false);

                _logger.LogInformation("Saved profile for process {ProcessName}: {ProfileId}", 
                    profile.ProcessName, profile.Id);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to save profile for process {ProcessName}", profile.ProcessName);
                return false;
            }
        }

        /// <inheritdoc/>
        public async Task<bool> DeleteProfileAsync(Guid profileId)
        {
            try
            {
                await EnsureInitializedAsync().ConfigureAwait(false);

                var profile = _profileCache.Values.FirstOrDefault(p => p.Id == profileId);
                if (profile == null)
                {
                    _logger.LogWarning("Cannot delete profile - not found: {ProfileId}", profileId);
                    return false;
                }

                if (_profileCache.TryRemove(profile.ProcessName, out _))
                {
                    await SaveConfigurationAsync().ConfigureAwait(false);
                    _logger.LogInformation("Deleted profile {ProfileId} for process {ProcessName}", 
                        profileId, profile.ProcessName);
                    return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to delete profile {ProfileId}", profileId);
                return false;
            }
        }

        /// <inheritdoc/>
        public async Task<bool> ApplyProfileAsync(ProcessProfile profile, IntPtr windowHandle, int processId)
        {
            ArgumentNullException.ThrowIfNull(profile);

            if (windowHandle == IntPtr.Zero && processId <= 0)
            {
                _logger.LogWarning("ApplyProfileAsync called with invalid window handle and process ID");
                return false;
            }

            try
            {
                _logger.LogDebug("Applying profile {ProfileId} to window 0x{WindowHandle:X} / process {ProcessId}",
                    profile.Id, windowHandle, processId);

                var allSucceeded = true;

                // Apply window position if specified
                if (profile.HasWindowPosition && windowHandle != IntPtr.Zero)
                {
                    var positionResult = await ApplyWindowPositionAsync(profile, windowHandle).ConfigureAwait(false);
                    allSucceeded = allSucceeded && positionResult;
                }

                // Apply window state if specified
                if (profile.TargetWindowState.HasValue && windowHandle != IntPtr.Zero)
                {
                    var stateResult = await ApplyWindowStateAsync(profile.TargetWindowState.Value, windowHandle).ConfigureAwait(false);
                    allSucceeded = allSucceeded && stateResult;
                }

                // Apply always-on-top if specified
                if (profile.AlwaysOnTop.HasValue && windowHandle != IntPtr.Zero)
                {
                    var topResult = await _windowService.SetAlwaysOnTopAsync(windowHandle, profile.AlwaysOnTop.Value).ConfigureAwait(false);
                    allSucceeded = allSucceeded && topResult;
                    _logger.LogDebug("Applied always-on-top={AlwaysOnTop}: {Result}", profile.AlwaysOnTop.Value, topResult);
                }

                // Apply audio settings if specified
                if (profile.HasAudioSettings && processId > 0)
                {
                    var audioResult = await ApplyAudioSettingsAsync(profile, processId).ConfigureAwait(false);
                    allSucceeded = allSucceeded && audioResult;
                }

                _logger.LogInformation("Applied profile {ProfileId} to process {ProcessId}: Success={Success}",
                    profile.Id, processId, allSucceeded);

                ProfileApplied?.Invoke(this, new ProfileAppliedEventArgs(profile, processId, allSucceeded));

                return allSucceeded;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to apply profile {ProfileId}", profile.Id);
                return false;
            }
        }

        /// <inheritdoc/>
        public async Task<ProcessProfile?> CaptureProfileAsync(ProcessInfo process)
        {
            ArgumentNullException.ThrowIfNull(process);

            try
            {
                _logger.LogDebug("Capturing profile for process {ProcessName} (PID: {ProcessId})",
                    process.Name, process.ProcessId);

                int? windowX = null, windowY = null, windowWidth = null, windowHeight = null;
                WindowState? windowState = null;
                bool? alwaysOnTop = null;
                float? audioLevel = null;
                bool? isMuted = null;

                // Capture window position and state
                if (process.WindowHandle != IntPtr.Zero)
                {
                    RECT rect = default;
                    if (NativeMethods.GetWindowRect(process.WindowHandle, out rect))
                    {
                        windowX = rect.Left;
                        windowY = rect.Top;
                        windowWidth = rect.Width;
                        windowHeight = rect.Height;
                    }

                    windowState = await _windowService.GetWindowStateAsync(process.WindowHandle).ConfigureAwait(false);
                    alwaysOnTop = await _windowService.IsAlwaysOnTopAsync(process.WindowHandle).ConfigureAwait(false);
                }

                // Capture audio settings
                audioLevel = await _audioService.GetProcessVolumeAsync(process.ProcessId).ConfigureAwait(false);
                isMuted = await _audioService.IsProcessMutedAsync(process.ProcessId).ConfigureAwait(false);

                var profile = new ProcessProfile
                {
                    Id = Guid.NewGuid(),
                    ProcessName = process.Name,
                    DisplayName = $"{process.Name} Profile",
                    WindowX = windowX,
                    WindowY = windowY,
                    WindowWidth = windowWidth,
                    WindowHeight = windowHeight,
                    TargetWindowState = windowState,
                    AlwaysOnTop = alwaysOnTop,
                    AudioLevel = audioLevel,
                    IsMuted = isMuted,
                    AutoApplyOnDetection = false,
                    CreatedAt = DateTime.UtcNow,
                    LastModified = DateTime.UtcNow
                };

                _logger.LogInformation("Captured profile for process {ProcessName}: {ProfileId}",
                    process.Name, profile.Id);

                return profile;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to capture profile for process {ProcessName}", process.Name);
                return null;
            }
        }

        /// <inheritdoc/>
        public async Task<IReadOnlyList<ProcessProfile>> GetAutoApplyProfilesAsync()
        {
            await EnsureInitializedAsync().ConfigureAwait(false);

            var autoApplyProfiles = _profileCache.Values
                .Where(p => p.AutoApplyOnDetection)
                .ToList();

            _logger.LogDebug("Found {Count} auto-apply profiles", autoApplyProfiles.Count);
            return autoApplyProfiles.AsReadOnly();
        }

        /// <inheritdoc/>
        public async Task<bool> SetAutoApplyAsync(Guid profileId, bool enabled)
        {
            try
            {
                await EnsureInitializedAsync().ConfigureAwait(false);

                var profile = _profileCache.Values.FirstOrDefault(p => p.Id == profileId);
                if (profile == null)
                {
                    _logger.LogWarning("Cannot set auto-apply - profile not found: {ProfileId}", profileId);
                    return false;
                }

                var updatedProfile = new ProcessProfile
                {
                    Id = profile.Id,
                    ProcessName = profile.ProcessName,
                    DisplayName = profile.DisplayName,
                    WindowX = profile.WindowX,
                    WindowY = profile.WindowY,
                    WindowWidth = profile.WindowWidth,
                    WindowHeight = profile.WindowHeight,
                    AudioLevel = profile.AudioLevel,
                    IsMuted = profile.IsMuted,
                    AlwaysOnTop = profile.AlwaysOnTop,
                    TargetWindowState = profile.TargetWindowState,
                    AutoApplyOnDetection = enabled,
                    CreatedAt = profile.CreatedAt,
                    LastModified = DateTime.UtcNow
                };

                _profileCache[profile.ProcessName] = updatedProfile;
                await SaveConfigurationAsync().ConfigureAwait(false);

                _logger.LogInformation("Set auto-apply={Enabled} for profile {ProfileId}", enabled, profileId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to set auto-apply for profile {ProfileId}", profileId);
                return false;
            }
        }

        private async void OnProcessStarted(object? sender, ProcessInfo process)
        {
            try
            {
                if (!_isInitialized) return;

                if (_profileCache.TryGetValue(process.Name, out var profile) && profile.AutoApplyOnDetection)
                {
                    _logger.LogDebug("Auto-applying profile for detected process {ProcessName}", process.Name);

                    // Small delay to let the window initialize
                    await Task.Delay(500).ConfigureAwait(false);

                    await ApplyProfileAsync(profile, process.WindowHandle, process.ProcessId).ConfigureAwait(false);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in auto-apply handler for process {ProcessName}", process.Name);
            }
        }

        private async Task<bool> ApplyWindowPositionAsync(ProcessProfile profile, IntPtr windowHandle)
        {
            try
            {
                // Get current position
                RECT currentRect = default;
                if (!NativeMethods.GetWindowRect(windowHandle, out currentRect))
                {
                    _logger.LogWarning("Failed to get current window rect for 0x{WindowHandle:X}", windowHandle);
                    return false;
                }

                var x = profile.WindowX ?? currentRect.Left;
                var y = profile.WindowY ?? currentRect.Top;
                var width = profile.WindowWidth ?? currentRect.Width;
                var height = profile.WindowHeight ?? currentRect.Height;

                var result = NativeMethods.MoveWindow(windowHandle, x, y, width, height, true);

                _logger.LogDebug("Applied window position ({X}, {Y}, {Width}x{Height}): {Result}",
                    x, y, width, height, result);

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to apply window position");
                return false;
            }
        }

        private async Task<bool> ApplyWindowStateAsync(WindowState state, IntPtr windowHandle)
        {
            return state switch
            {
                WindowState.Normal => await _windowService.RestoreAsync(windowHandle).ConfigureAwait(false),
                WindowState.Minimized => await _windowService.MinimizeAsync(windowHandle).ConfigureAwait(false),
                WindowState.Maximized => await _windowService.MaximizeAsync(windowHandle).ConfigureAwait(false),
                WindowState.Hidden => await _windowService.HideAsync(windowHandle).ConfigureAwait(false),
                _ => true
            };
        }

        private async Task<bool> ApplyAudioSettingsAsync(ProcessProfile profile, int processId)
        {
            var allSucceeded = true;

            if (profile.AudioLevel.HasValue)
            {
                var volumeResult = await _audioService.SetProcessVolumeAsync(processId, profile.AudioLevel.Value).ConfigureAwait(false);
                allSucceeded = allSucceeded && volumeResult;
                _logger.LogDebug("Applied audio level {Level}: {Result}", profile.AudioLevel.Value, volumeResult);
            }

            if (profile.IsMuted.HasValue)
            {
                var muteResult = profile.IsMuted.Value
                    ? await _audioService.MuteProcessAsync(processId).ConfigureAwait(false)
                    : await _audioService.UnmuteProcessAsync(processId).ConfigureAwait(false);
                allSucceeded = allSucceeded && muteResult;
                _logger.LogDebug("Applied mute={Muted}: {Result}", profile.IsMuted.Value, muteResult);
            }

            return allSucceeded;
        }

        private async Task<ProfileConfiguration> LoadConfigurationInternalAsync()
        {
            try
            {
                if (!Directory.Exists(_configDirectory))
                {
                    Directory.CreateDirectory(_configDirectory);
                }

                if (!File.Exists(_configFilePath))
                {
                    _logger.LogDebug("No existing profiles file found, returning default configuration");
                    return ProfileConfiguration.CreateDefault();
                }

                var json = await File.ReadAllTextAsync(_configFilePath).ConfigureAwait(false);
                var config = JsonSerializer.Deserialize<ProfileConfiguration>(json, JsonOptions);

                if (config == null)
                {
                    _logger.LogWarning("Failed to deserialize profiles configuration, returning default");
                    return ProfileConfiguration.CreateDefault();
                }

                _logger.LogDebug("Loaded profiles configuration with {Count} profiles", config.Profiles.Count);
                return config;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load profiles configuration");
                return ProfileConfiguration.CreateDefault();
            }
        }

        private async Task SaveConfigurationAsync()
        {
            await _fileLock.WaitAsync().ConfigureAwait(false);
            try
            {
                if (!Directory.Exists(_configDirectory))
                {
                    Directory.CreateDirectory(_configDirectory);
                }

                // Create backup if file exists
                if (File.Exists(_configFilePath))
                {
                    File.Copy(_configFilePath, _backupFilePath, true);
                }

                var config = new ProfileConfiguration
                {
                    Version = 1,
                    Profiles = _profileCache.Values.ToList(),
                    LastSaved = DateTime.UtcNow
                };

                var json = JsonSerializer.Serialize(config, JsonOptions);
                await File.WriteAllTextAsync(_configFilePath, json).ConfigureAwait(false);

                _logger.LogDebug("Saved profiles configuration with {Count} profiles", config.Profiles.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to save profiles configuration");
                throw;
            }
            finally
            {
                _fileLock.Release();
            }
        }
    }
}
