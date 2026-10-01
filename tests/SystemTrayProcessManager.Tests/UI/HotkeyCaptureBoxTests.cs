using System.Windows.Data;
using Binding = System.Windows.Data.Binding;
using SystemTrayProcessManager.Core.Enums;
using SystemTrayProcessManager.Core.Models;
using SystemTrayProcessManager.UI.Controls;

namespace SystemTrayProcessManager.Tests.UI;

/// <summary>Exercises the real WPF capture control and its two-way bindings.</summary>
[Collection("Wpf")]
public class HotkeyCaptureBoxTests
{
    [Fact]
    public async Task CaptureAndClear_PreserveBindingsAcrossEditedItems()
    {
        await RunStaAsync(() =>
        {
            var first = new HotkeyConfigItem("First", 0x4D, HotkeyModifier.Ctrl, "ToggleMute");
            var second = new HotkeyConfigItem("Second", 0x55, HotkeyModifier.Alt, "Minimize");
            var control = new HotkeyCaptureBox { DataContext = first };
            BindingOperations.SetBinding(control, HotkeyCaptureBox.VirtualKeyCodeProperty,
                new Binding(nameof(HotkeyConfigItem.VirtualKeyCode)) { Mode = BindingMode.TwoWay });
            BindingOperations.SetBinding(control, HotkeyCaptureBox.ModifiersProperty,
                new Binding(nameof(HotkeyConfigItem.Modifiers)) { Mode = BindingMode.TwoWay });

            control.SetBinding(new HotkeyBinding(0x41, HotkeyModifier.Ctrl | HotkeyModifier.Shift));
            Assert.True(BindingOperations.IsDataBound(control, HotkeyCaptureBox.VirtualKeyCodeProperty));
            Assert.True(BindingOperations.IsDataBound(control, HotkeyCaptureBox.ModifiersProperty));
            Assert.Equal(0x41, first.VirtualKeyCode);

            control.DataContext = second;
            Assert.Equal(0x55, control.VirtualKeyCode);
            Assert.Equal(HotkeyModifier.Alt, control.Modifiers);
            control.Clear();
            Assert.Equal(0, second.VirtualKeyCode);
            Assert.Equal(HotkeyModifier.None, second.Modifiers);
            Assert.True(BindingOperations.IsDataBound(control, HotkeyCaptureBox.VirtualKeyCodeProperty));
        });
    }

    internal static Task RunStaAsync(Action action)
    {
        var completion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var thread = new Thread(() =>
        {
            try { action(); completion.SetResult(); }
            catch (Exception ex) { completion.SetException(ex); }
            finally { System.Windows.Threading.Dispatcher.CurrentDispatcher.InvokeShutdown(); }
        });
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        return completion.Task;
    }
}
