using System.ComponentModel;
using System.Windows.Input;
using SystemTrayProcessManager.Core.Services;
using SystemTrayProcessManager.UI.ViewModels;

namespace SystemTrayProcessManager.UI.Views;

/// <summary>Displays all independently configurable shortcuts for one application.</summary>
public partial class ProcessHotkeysWindow : Window
{
    private readonly ProcessHotkeysViewModel _viewModel;
    private readonly IHotkeyService _hotkeys;
    private bool _resumeAfterCapture;

    /// <summary>Creates the application shortcut window using shared runtime services.</summary>
    public ProcessHotkeysWindow(ProcessHotkeysViewModel viewModel, IHotkeyService hotkeys)
    {
        _viewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
        _hotkeys = hotkeys ?? throw new ArgumentNullException(nameof(hotkeys));
        InitializeComponent();
        DataContext = viewModel;
        Closing += OnClosing;
        Closed += (_, _) => ResumeAfterCapture();
    }

    /// <summary>Loads shortcuts for the process selected in the dashboard.</summary>
    public Task LoadAsync(string processName) => _viewModel.LoadAsync(processName);

    private void Capture_GotKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
    {
        if (!_hotkeys.IsSuspended)
        {
            _hotkeys.Suspend();
            _resumeAfterCapture = true;
        }
    }
    private void Capture_LostKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e) => ResumeAfterCapture();
    private void ResumeAfterCapture()
    {
        if (!_resumeAfterCapture) return;
        _hotkeys.Resume();
        _resumeAfterCapture = false;
    }
    private void Close_Click(object sender, RoutedEventArgs e) => Close();

    private async void OnClosing(object? sender, CancelEventArgs e)
    {
        if (_viewModel.IsBusy) { e.Cancel = true; return; }
        if (!_viewModel.HasUnsavedChanges) return;
        var result = MessageBox.Show(this, "Save changes to this application's shortcuts?", "Unsaved shortcuts",
            MessageBoxButton.YesNoCancel, MessageBoxImage.Question);
        if (result == MessageBoxResult.No) return;
        e.Cancel = true;
        if (result != MessageBoxResult.Yes) return;
        await _viewModel.SaveCommand.ExecuteAsync(null);
        if (!_viewModel.HasUnsavedChanges) _ = Dispatcher.BeginInvoke(new Action(Close));
    }
}
