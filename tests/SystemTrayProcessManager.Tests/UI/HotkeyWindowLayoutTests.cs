using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using SystemTrayProcessManager.Core.Models;
using SystemTrayProcessManager.Core.Enums;
using SystemTrayProcessManager.UI.Controls;
using SystemTrayProcessManager.Core.Services;
using SystemTrayProcessManager.UI;
using SystemTrayProcessManager.UI.ViewModels;
using SystemTrayProcessManager.UI.Views;

namespace SystemTrayProcessManager.Tests.UI;

/// <summary>Loads actual XAML, checks equal card sizes, and optionally renders review images.</summary>
[Collection("Wpf")]
public class HotkeyWindowLayoutTests
{
    [Fact]
    public async Task DashboardAndShortcutEditor_LayoutActualTemplates()
    {
        await HotkeyCaptureBoxTests.RunStaAsync(() =>
        {
            var app = new App();
            app.InitializeComponent();
            var audio = Mock.Of<IAudioService>();
            var windows = Mock.Of<IWindowService>();
            using var main = new MainViewModel(Mock.Of<IProcessService>(), windows, audio,
                Mock.Of<INotificationService>(), NullLogger<MainViewModel>.Instance);
            foreach (var name in new[] { "Spotify", "Browser with a very long application name", "Editor" })
            {
                main.ProcessCards.Add(new ProcessCardViewModel(new ProcessInfo
                { Name = name, ProcessId = main.ProcessCards.Count + 1, WindowTitle = name == "Editor" ? "" : "An example window title that should truncate" },
                    windows, audio, NullLogger.Instance));
            }
            main.FilteredProcessCount = main.TotalProcessCount = 3;
            main.StatusText = "3 processes loaded";
            var dashboard = new MainWindow(main, NullLogger<MainWindow>.Instance);
            var dashboardContent = Render(dashboard, 950, 600, "dashboard");
            var cards = Descendants(dashboardContent).OfType<System.Windows.Controls.Border>().Where(b => b.Name == "CardBorder").ToList();
            Assert.Equal(3, cards.Count);
            Assert.All(cards, card => { Assert.Equal(292, card.ActualWidth); Assert.Equal(148, card.ActualHeight); });
            Assert.Equal(3, Descendants(dashboardContent).OfType<System.Windows.Controls.Button>().Count(b => Equals(b.Content, "Hotkeys")));

            var config = new Mock<IHotkeyConfigurationService>();
            config.Setup(c => c.LoadConfigurationAsync()).ReturnsAsync(new HotkeyConfiguration());
            config.Setup(c => c.SaveConfigurationAsync(It.IsAny<HotkeyConfiguration>())).ReturnsAsync(true);
            var globalEditor = new HotkeyConfigViewModel(NullLogger<HotkeyConfigViewModel>.Instance, config.Object, Mock.Of<IActionMappingService>());
            var globalWindow = new HotkeyConfigWindow(NullLogger<HotkeyConfigWindow>.Instance, globalEditor, Mock.Of<IHotkeyService>());
            globalEditor.AddHotkeyCommand.Execute(null);
            var globalContent = Render(globalWindow, 850, 600, "global-hotkeys");
            var capture = Assert.Single(Descendants(globalContent).OfType<HotkeyCaptureBox>());
            capture.SetBinding(new HotkeyBinding(0x4D, HotkeyModifier.Ctrl | HotkeyModifier.Alt));
            globalEditor.EditingItem!.TargetProcessName = "Spotify";
            globalEditor.SaveEditCommand.ExecuteAsync(null).GetAwaiter().GetResult();
            config.Verify(c => c.SaveConfigurationAsync(It.Is<HotkeyConfiguration>(saved =>
                saved.Items.Single().VirtualKeyCode == 0x4D && saved.Items.Single().TargetProcessName == "Spotify")), Times.Once);
            globalWindow.Close();

            var editor = new ProcessHotkeysViewModel(config.Object, Mock.Of<IActionMappingService>(), NullLogger<ProcessHotkeysViewModel>.Instance);
            editor.LoadAsync("Spotify").GetAwaiter().GetResult();
            editor.Rows[0].VirtualKeyCode = 0x4D;
            editor.Rows[0].Modifiers = HotkeyModifier.Ctrl | HotkeyModifier.Alt;
            var editorWindow = new ProcessHotkeysWindow(editor, Mock.Of<IHotkeyService>());
            var editorContent = Render(editorWindow, 700, 650, "application-hotkeys");
            Assert.Equal(editor.Rows.Count, Descendants(editorContent).OfType<HotkeyCaptureBox>().Count());
            Assert.All(Descendants(editorContent).OfType<HotkeyCaptureBox>(), c => Assert.True(c.ActualWidth >= 180));
            editor.HasUnsavedChanges = false;
            editorWindow.Close();
            dashboard.Close();
        });
    }

    private static FrameworkElement Render(Window window, double width, double height, string name)
    {
        var content = (FrameworkElement)window.Content;
        content.Measure(new System.Windows.Size(width, height));
        content.Arrange(new Rect(0, 0, width, height));
        content.UpdateLayout();
        var directory = Environment.GetEnvironmentVariable("STPM_UI_CAPTURE_DIR");
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
            var bitmap = new RenderTargetBitmap((int)width, (int)height, 96, 96, PixelFormats.Pbgra32);
            bitmap.Render(content);
            var encoder = new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(bitmap));
            using var stream = File.Create(Path.Combine(directory, name + ".png"));
            encoder.Save(stream);
        }
        return content;
    }

    private static IEnumerable<DependencyObject> Descendants(DependencyObject root)
    {
        for (int i = 0; i < VisualTreeHelper.GetChildrenCount(root); i++)
        {
            var child = VisualTreeHelper.GetChild(root, i);
            yield return child;
            foreach (var descendant in Descendants(child)) yield return descendant;
        }
    }
}
