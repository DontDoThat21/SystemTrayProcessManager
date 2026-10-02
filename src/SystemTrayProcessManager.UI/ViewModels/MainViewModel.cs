using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using System.Collections.ObjectModel;
using SystemTrayProcessManager.Core.Models;
using SystemTrayProcessManager.Core.Services;

namespace SystemTrayProcessManager.UI.ViewModels
{
    /// <summary>
    /// Main dashboard ViewModel providing process list, search/filter, and notification management.
    /// Acts as the central coordinator between process services and the main window UI.
    /// </summary>
    public partial class MainViewModel : ObservableObject, IDisposable
    {
        private readonly IProcessService _processService;
        private readonly IWindowService _windowService;
        private readonly IAudioService _audioService;
        private readonly INotificationService _notificationService;
        private readonly ILogger<MainViewModel> _logger;
        private readonly List<ProcessCardViewModel> _allProcessCards = new();
        private bool _disposed;
        private readonly IHotkeyConfigurationService? _hotkeyConfiguration;
        private Dictionary<string, string?> _applications = new(StringComparer.OrdinalIgnoreCase);

        /// <summary>Selects an executable when a legacy shortcut has no saved launch path.</summary>
        public Func<string?>? SelectExecutablePath { get; set; }

        /// <summary>
        /// Gets the filtered collection of process cards displayed in the dashboard.
        /// </summary>
        public ObservableCollection<ProcessCardViewModel> ProcessCards { get; } = new();

        /// <summary>
        /// Gets the collection of active notifications.
        /// </summary>
        public ObservableCollection<NotificationViewModel> Notifications { get; } = new();

        [ObservableProperty]
        private string _searchText = string.Empty;

        [ObservableProperty]
        private bool _isLoading;

        [ObservableProperty]
        private int _totalProcessCount;

        [ObservableProperty]
        private int _filteredProcessCount;

        [ObservableProperty]
        private string _statusText = "Ready";

        [ObservableProperty]
        private bool _hasNotifications;

        [ObservableProperty]
        private bool _isNotificationPanelOpen;

        /// <summary>
        /// Initializes a new instance of the <see cref="MainViewModel"/> class.
        /// </summary>
        /// <param name="processService">Process discovery and monitoring service.</param>
        /// <param name="windowService">Window manipulation service.</param>
        /// <param name="audioService">Audio control service.</param>
        /// <param name="notificationService">In-app notification service.</param>
        /// <param name="logger">Logger instance.</param>
        /// <param name="hotkeyConfiguration">Saved application and hotkey configuration.</param>
        public MainViewModel(
            IProcessService processService,
            IWindowService windowService,
            IAudioService audioService,
            INotificationService notificationService,
            ILogger<MainViewModel> logger,
            IHotkeyConfigurationService? hotkeyConfiguration = null)
        {
            _processService = processService ?? throw new ArgumentNullException(nameof(processService));
            _windowService = windowService ?? throw new ArgumentNullException(nameof(windowService));
            _audioService = audioService ?? throw new ArgumentNullException(nameof(audioService));
            _notificationService = notificationService ?? throw new ArgumentNullException(nameof(notificationService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            _hotkeyConfiguration = hotkeyConfiguration;

            // Subscribe to process events
            _processService.ProcessStarted += OnProcessStarted;
            _processService.ProcessStopped += OnProcessStopped;

            // Subscribe to notification events
            _notificationService.NotificationAdded += OnNotificationAdded;
            _notificationService.NotificationDismissed += OnNotificationDismissed;

            _logger.LogInformation("MainViewModel initialized");
        }

        /// <summary>
        /// Loads or refreshes the process list from the process service.
        /// </summary>
        [RelayCommand]
        private async Task RefreshProcessesAsync()
        {
            try
            {
                IsLoading = true;
                StatusText = "Refreshing processes...";

                _logger.LogDebug("Refreshing process list");

                var processes = await _processService.GetRunningProcessesAsync();
                var processList = processes.ToList();
                if (_hotkeyConfiguration != null)
                {
                    var configuration = await _hotkeyConfiguration.LoadConfigurationAsync();
                    _applications = new(configuration.Applications, StringComparer.OrdinalIgnoreCase);
                    bool changed = false;
                    foreach (var item in configuration.Items.Where(i => !string.IsNullOrWhiteSpace(i.TargetProcessName)))
                        changed |= _applications.TryAdd(HotkeyConfiguration.ApplicationName(item.TargetProcessName!),
                            System.IO.Path.IsPathFullyQualified(item.TargetProcessName!) ? item.TargetProcessName : null);
                    foreach (var process in processList)
                    {
                        if (_applications.ContainsKey(process.Name) && !string.IsNullOrWhiteSpace(process.ExecutablePath)
                            && _applications[process.Name] != process.ExecutablePath)
                        {
                            _applications[process.Name] = process.ExecutablePath;
                            changed = true;
                        }
                    }
                    if (changed)
                    {
                        configuration.Applications = _applications;
                        await _hotkeyConfiguration.SaveConfigurationAsync(configuration);
                    }
                }

                _allProcessCards.Clear();

                foreach (var process in processList)
                {
                    var card = new ProcessCardViewModel(
                        process,
                        _windowService,
                        _audioService,
                        _logger, LaunchApplicationAsync);

                    _allProcessCards.Add(card);
                }

                AddSavedApplications();
                TotalProcessCount = _allProcessCards.Count;
                ApplyFilter();

                // Refresh audio states in background
                _ = RefreshAudioStatesAsync();

                StatusText = $"{TotalProcessCount} processes loaded";
                _logger.LogInformation("Process list refreshed: {Count} processes", TotalProcessCount);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to refresh process list");
                StatusText = "Error loading processes";
            }
            finally
            {
                IsLoading = false;
            }
        }

        private void AddSavedApplications()
        {
            foreach (var app in _applications)
            {
                if (_allProcessCards.Any(c => c.Name.Equals(app.Key, StringComparison.OrdinalIgnoreCase))) continue;
                _allProcessCards.Add(new ProcessCardViewModel(new ProcessInfo
                {
                    Name = app.Key, ExecutablePath = app.Value
                }, _windowService, _audioService, _logger, LaunchApplicationAsync));
            }
        }

        private async Task<bool> LaunchApplicationAsync(string name)
        {
            try
            {
                _applications.TryGetValue(name, out var path);
                if (string.IsNullOrWhiteSpace(path) || !System.IO.File.Exists(path))
                {
                    path = SelectExecutablePath?.Invoke();
                    if (string.IsNullOrWhiteSpace(path)) return false;
                    if (!HotkeyConfiguration.ApplicationName(path).Equals(name, StringComparison.OrdinalIgnoreCase))
                    {
                        StatusText = $"Select {name}.exe to launch this application.";
                        return false;
                    }
                    if (_hotkeyConfiguration != null)
                    {
                        var configuration = await _hotkeyConfiguration.LoadConfigurationAsync();
                        configuration.Applications[name] = path;
                        if (!await _hotkeyConfiguration.SaveConfigurationAsync(configuration))
                        {
                            StatusText = "Unable to save executable path.";
                            return false;
                        }
                    }
                    _applications[name] = path;
                }
                var launched = await _processService.LaunchAsync(path);
                StatusText = launched ? $"Launched {name}" : $"Unable to launch {name}. Check its executable path and permissions.";
                return launched;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unable to launch {Name}", name);
                StatusText = $"Unable to launch {name}.";
                return false;
            }
        }

        /// <summary>
        /// Toggles the notification panel visibility.
        /// </summary>
        [RelayCommand]
        private void ToggleNotificationPanel()
        {
            IsNotificationPanelOpen = !IsNotificationPanelOpen;
            _logger.LogDebug("Notification panel toggled: {IsOpen}", IsNotificationPanelOpen);
        }

        /// <summary>
        /// Dismisses all active notifications.
        /// </summary>
        [RelayCommand]
        private void DismissAllNotifications()
        {
            _notificationService.DismissAll();
            Notifications.Clear();
            HasNotifications = false;
            _logger.LogDebug("All notifications dismissed");
        }

        /// <summary>
        /// Dismisses a single notification by ID.
        /// </summary>
        [RelayCommand]
        private void DismissNotification(string? notificationId)
        {
            if (string.IsNullOrEmpty(notificationId)) return;

            _notificationService.DismissNotification(notificationId);
        }

        /// <summary>
        /// Handles the SearchText property change to apply filtering.
        /// </summary>
        partial void OnSearchTextChanged(string value)
        {
            ApplyFilter();
        }

        /// <summary>
        /// Applies the current search filter to the process card collection.
        /// </summary>
        private void ApplyFilter()
        {
            try
            {
                // Get dispatcher for UI thread access
                var dispatcher = System.Windows.Application.Current?.Dispatcher;
                if (dispatcher != null && !dispatcher.CheckAccess())
                {
                    dispatcher.Invoke(ApplyFilter);
                    return;
                }

                ProcessCards.Clear();

                var filtered = string.IsNullOrWhiteSpace(SearchText)
                    ? _allProcessCards
                    : _allProcessCards.Where(p =>
                        p.Name.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ||
                        (p.WindowTitle?.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ?? false) ||
                        p.ProcessId.ToString().Contains(SearchText, StringComparison.OrdinalIgnoreCase));

                foreach (var card in filtered.OrderBy(c => c.Name, StringComparer.OrdinalIgnoreCase))
                {
                    ProcessCards.Add(card);
                }

                FilteredProcessCount = ProcessCards.Count;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error applying process filter");
            }
        }

        /// <summary>
        /// Refreshes audio state for all process cards in background.
        /// </summary>
        private async Task RefreshAudioStatesAsync()
        {
            try
            {
                foreach (var card in _allProcessCards.ToList())
                {
                    await card.RefreshAudioStateAsync();
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error refreshing audio states");
            }
        }

        /// <summary>
        /// Handles new process detection events.
        /// </summary>
        private void OnProcessStarted(object? sender, ProcessInfo process)
        {
            try
            {
                var dispatcher = System.Windows.Application.Current?.Dispatcher;
                void UpdateCards()
                {
                    var card = new ProcessCardViewModel(
                        process,
                        _windowService,
                        _audioService,
                        _logger, LaunchApplicationAsync);

                    _allProcessCards.RemoveAll(c => !c.IsRunning && c.Name.Equals(process.Name, StringComparison.OrdinalIgnoreCase));
                    if (!_allProcessCards.Any(c => c.ProcessId == process.ProcessId && c.WindowHandle == process.WindowHandle))
                        _allProcessCards.Add(card);
                    TotalProcessCount = _allProcessCards.Count;
                    ApplyFilter();
                }
                if (dispatcher != null && !dispatcher.CheckAccess()) dispatcher.Invoke(UpdateCards);
                else UpdateCards();

                _notificationService.AddNotification(
                    AppNotification.Info("Process Started", $"{process.Name} (PID: {process.ProcessId})"));

                _logger.LogDebug("Process started event handled: {Name}", process.Name);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling process started event");
            }
        }

        /// <summary>
        /// Handles process termination events.
        /// </summary>
        private void OnProcessStopped(object? sender, ProcessStoppedEventArgs e)
        {
            try
            {
                var dispatcher = System.Windows.Application.Current?.Dispatcher;
                void UpdateCards()
                {
                    var card = _allProcessCards.FirstOrDefault(c => c.ProcessId == e.ProcessId);
                    if (card != null)
                    {
                        _allProcessCards.Remove(card);
                        AddSavedApplications();
                        TotalProcessCount = _allProcessCards.Count;
                        ApplyFilter();
                    }
                }
                if (dispatcher != null && !dispatcher.CheckAccess()) dispatcher.Invoke(UpdateCards);
                else UpdateCards();

                _notificationService.AddNotification(
                    AppNotification.Info("Process Stopped", $"{e.ProcessName ?? "Unknown"} (PID: {e.ProcessId})"));

                _logger.LogDebug("Process stopped event handled: PID {PID}", e.ProcessId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling process stopped event");
            }
        }

        /// <summary>
        /// Handles new notification added events.
        /// </summary>
        private void OnNotificationAdded(object? sender, AppNotification notification)
        {
            try
            {
                var dispatcher = System.Windows.Application.Current?.Dispatcher;
                if (dispatcher == null) return;

                dispatcher.Invoke(() =>
                {
                    Notifications.Insert(0, new NotificationViewModel(notification));

                    // Limit visible notifications
                    while (Notifications.Count > 20)
                    {
                        Notifications.RemoveAt(Notifications.Count - 1);
                    }

                    HasNotifications = Notifications.Count > 0;
                });
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error handling notification added event");
            }
        }

        /// <summary>
        /// Handles notification dismissed events.
        /// </summary>
        private void OnNotificationDismissed(object? sender, string notificationId)
        {
            try
            {
                var dispatcher = System.Windows.Application.Current?.Dispatcher;
                if (dispatcher == null) return;

                dispatcher.Invoke(() =>
                {
                    var vm = Notifications.FirstOrDefault(n => n.Id == notificationId);
                    if (vm != null)
                    {
                        Notifications.Remove(vm);
                    }

                    HasNotifications = Notifications.Count > 0;
                });
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error handling notification dismissed event");
            }
        }

        /// <summary>
        /// Releases resources and unsubscribes from events.
        /// </summary>
        public void Dispose()
        {
            if (_disposed) return;

            _processService.ProcessStarted -= OnProcessStarted;
            _processService.ProcessStopped -= OnProcessStopped;
            _notificationService.NotificationAdded -= OnNotificationAdded;
            _notificationService.NotificationDismissed -= OnNotificationDismissed;

            _disposed = true;
            _logger.LogDebug("MainViewModel disposed");
        }
    }
}
