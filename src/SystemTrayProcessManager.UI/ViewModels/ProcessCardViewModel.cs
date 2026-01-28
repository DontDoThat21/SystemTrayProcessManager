using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using SystemTrayProcessManager.Core.Models;
using SystemTrayProcessManager.Core.Services;
using WpfImageSource = System.Windows.Media.ImageSource;

namespace SystemTrayProcessManager.UI.ViewModels
{
    /// <summary>
    /// ViewModel representing a single process card in the main dashboard.
    /// Provides process information display and quick action commands.
    /// </summary>
    public partial class ProcessCardViewModel : ObservableObject
    {
        private readonly IWindowService _windowService;
        private readonly IAudioService _audioService;
        private readonly ILogger _logger;

        /// <summary>
        /// Gets the process ID.
        /// </summary>
        public int ProcessId { get; }

        /// <summary>
        /// Gets the process name.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Gets the window title.
        /// </summary>
        public string? WindowTitle { get; }

        /// <summary>
        /// Gets the display name combining process name and window title.
        /// </summary>
        public string DisplayName { get; }

        /// <summary>
        /// Gets the process icon.
        /// </summary>
        public WpfImageSource? Icon { get; }

        /// <summary>
        /// Gets the window handle for manipulation.
        /// </summary>
        public IntPtr WindowHandle { get; }

        [ObservableProperty]
        private bool _isMuted;

        [ObservableProperty]
        private float _volume = 1.0f;

        [ObservableProperty]
        private bool _isSelected;

        [ObservableProperty]
        private bool _isBusy;

        /// <summary>
        /// Initializes a new instance of the <see cref="ProcessCardViewModel"/> class.
        /// </summary>
        /// <param name="processInfo">The process information to display.</param>
        /// <param name="windowService">Window manipulation service.</param>
        /// <param name="audioService">Audio control service.</param>
        /// <param name="logger">Logger instance.</param>
        public ProcessCardViewModel(
            ProcessInfo processInfo,
            IWindowService windowService,
            IAudioService audioService,
            ILogger logger)
        {
            _windowService = windowService ?? throw new ArgumentNullException(nameof(windowService));
            _audioService = audioService ?? throw new ArgumentNullException(nameof(audioService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            if (processInfo == null) throw new ArgumentNullException(nameof(processInfo));

            ProcessId = processInfo.ProcessId;
            Name = processInfo.Name;
            WindowTitle = processInfo.WindowTitle;
            DisplayName = processInfo.DisplayName;
            Icon = processInfo.Icon;
            WindowHandle = processInfo.WindowHandle;
        }

        /// <summary>
        /// Toggles the mute state of the process audio.
        /// </summary>
        [RelayCommand]
        private async Task ToggleMuteAsync()
        {
            try
            {
                IsBusy = true;
                _logger.LogDebug("Toggling mute for process {Name} (PID: {PID})", Name, ProcessId);

                var result = await _audioService.ToggleMuteProcessAsync(ProcessId);
                if (result)
                {
                    IsMuted = await _audioService.IsProcessMutedAsync(ProcessId) ?? false;
                    _logger.LogInformation("Mute toggled for {Name}: IsMuted={IsMuted}", Name, IsMuted);
                }
                else
                {
                    _logger.LogWarning("Failed to toggle mute for {Name}", Name);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error toggling mute for {Name}", Name);
            }
            finally
            {
                IsBusy = false;
            }
        }

        /// <summary>
        /// Minimizes the process window.
        /// </summary>
        [RelayCommand]
        private async Task MinimizeAsync()
        {
            try
            {
                IsBusy = true;
                _logger.LogDebug("Minimizing window for process {Name}", Name);

                var result = await _windowService.MinimizeAsync(WindowHandle);
                if (!result)
                {
                    _logger.LogWarning("Failed to minimize {Name}", Name);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error minimizing {Name}", Name);
            }
            finally
            {
                IsBusy = false;
            }
        }

        /// <summary>
        /// Closes the process window gracefully.
        /// </summary>
        [RelayCommand]
        private async Task CloseAsync()
        {
            try
            {
                IsBusy = true;
                _logger.LogDebug("Closing window for process {Name}", Name);

                var result = await _windowService.CloseAsync(WindowHandle, force: false);
                if (!result)
                {
                    _logger.LogWarning("Failed to close {Name}", Name);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error closing {Name}", Name);
            }
            finally
            {
                IsBusy = false;
            }
        }

        /// <summary>
        /// Brings the process window to the foreground.
        /// </summary>
        [RelayCommand]
        private async Task BringToFrontAsync()
        {
            try
            {
                IsBusy = true;
                _logger.LogDebug("Bringing to front: {Name}", Name);

                var result = await _windowService.BringToFrontAsync(WindowHandle);
                if (!result)
                {
                    _logger.LogWarning("Failed to bring {Name} to front", Name);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error bringing {Name} to front", Name);
            }
            finally
            {
                IsBusy = false;
            }
        }

        /// <summary>
        /// Refreshes the audio state (muted/volume) for this process.
        /// </summary>
        public async Task RefreshAudioStateAsync()
        {
            try
            {
                IsMuted = await _audioService.IsProcessMutedAsync(ProcessId) ?? false;
                var vol = await _audioService.GetProcessVolumeAsync(ProcessId);
                Volume = vol ?? 1.0f;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to refresh audio state for {Name}", Name);
            }
        }

        /// <inheritdoc/>
        public override string ToString() => $"{Name} (PID: {ProcessId})";
    }
}
