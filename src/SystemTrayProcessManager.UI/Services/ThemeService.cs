using Microsoft.Win32;
using Serilog;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using SystemTrayProcessManager.Core.Models;
using SystemTrayProcessManager.Core.Services;

namespace SystemTrayProcessManager.UI.Services;

/// <summary>Applies the selected palette and native title bars, including live Windows theme changes.</summary>
public sealed class ThemeService : IDisposable
{
    private readonly IConfigurationService _configuration;
    private string _theme = "System";
    private bool _dark;
    private bool _disposed;

    /// <summary>Subscribes to application and Windows preferences.</summary>
    public ThemeService(IConfigurationService configuration)
    {
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        _configuration.SettingsChanged += SettingsChanged;
        SystemEvents.UserPreferenceChanged += PreferencesChanged;
        EventManager.RegisterClassHandler(typeof(Window), FrameworkElement.LoadedEvent,
            new RoutedEventHandler(WindowLoaded));
    }

    /// <summary>Applies System, Light, or Dark to all open windows.</summary>
    public void Apply(string theme)
    {
        if (_disposed) return;
        _theme = theme;
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize");
            _dark = theme == "Dark" || (theme == "System" && key?.GetValue("AppsUseLightTheme") is int value && value == 0);
            var dictionaries = Application.Current.Resources.MergedDictionaries;
            dictionaries[0] = new ResourceDictionary
            {
                Source = new Uri($"pack://application:,,,/SystemTrayProcessManager;component/Resources/Themes/{(_dark ? "Dark" : "Light")}Theme.xaml", UriKind.Absolute)
            };
            foreach (Window window in Application.Current.Windows) ApplyTitleBar(window);
            Log.Information("Applied {Theme} theme; dark palette: {Dark}", theme, _dark);
        }
        catch (Exception ex) { Log.Error(ex, "Unable to apply application theme"); }
    }

    private void WindowLoaded(object sender, RoutedEventArgs e)
    {
        if (!_disposed && sender is Window window && ReferenceEquals(e.OriginalSource, window)) ApplyTitleBar(window);
    }

    private void ApplyTitleBar(Window window)
    {
        var handle = new WindowInteropHelper(window).Handle;
        if (handle == IntPtr.Zero) return;
        var enabled = _dark ? 1 : 0;
        var result = DwmSetWindowAttribute(handle, 20, ref enabled, sizeof(int));
        if (result < 0) Log.Debug("Native title bar theme unavailable: {Result}", result);
    }

    private void SettingsChanged(object? sender, AppSettings settings) =>
        Application.Current.Dispatcher.InvokeAsync(() => Apply(settings.Theme));

    private void PreferencesChanged(object sender, UserPreferenceChangedEventArgs e) =>
        Application.Current.Dispatcher.InvokeAsync(() => Apply(_theme));

    [DllImport("dwmapi.dll", ExactSpelling = true)]
    private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attribute, ref int value, int size);

    /// <summary>Releases subscriptions to Windows and application events.</summary>
    public void Dispose()
    {
        _disposed = true;
        SystemEvents.UserPreferenceChanged -= PreferencesChanged;
        _configuration.SettingsChanged -= SettingsChanged;
    }
}
