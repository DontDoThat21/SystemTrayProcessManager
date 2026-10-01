using Microsoft.Extensions.Logging;
using System.ComponentModel;
using System.Runtime.InteropServices;
using SystemTrayProcessManager.Core.Enums;
using SystemTrayProcessManager.Core.Services;
using SystemTrayProcessManager.Infrastructure.WindowsAPI;

namespace SystemTrayProcessManager.Infrastructure.Services
{
    /// <summary>
    /// Provides window manipulation capabilities using Windows API P/Invoke.
    /// Implements comprehensive error handling and logging for all operations.
    /// </summary>
    public sealed class WindowManipulationService : IWindowService
    {
        private readonly ILogger<WindowManipulationService> _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="WindowManipulationService"/> class.
        /// </summary>
        /// <param name="logger">Logger instance for diagnostic output.</param>
        /// <exception cref="ArgumentNullException">Thrown when logger is null.</exception>
        public WindowManipulationService(ILogger<WindowManipulationService> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _logger.LogDebug("WindowManipulationService initialized");
        }

        /// <inheritdoc/>
        public bool IsValidWindow(IntPtr windowHandle)
        {
            if (windowHandle == IntPtr.Zero)
            {
                return false;
            }

            return NativeMethods.IsWindow(windowHandle);
        }

        /// <inheritdoc/>
        public Task<bool> BringToFrontAsync(IntPtr windowHandle)
        {
            return Task.Run(() =>
            {
                try
                {
                    if (!ValidateWindowHandle(windowHandle, nameof(BringToFrontAsync)))
                    {
                        return false;
                    }

                    _logger.LogDebug("Bringing window to front: 0x{Handle:X}", windowHandle.ToInt64());

                    // If window is minimized, restore it first
                    if (NativeMethods.IsIconic(windowHandle))
                    {
                        _logger.LogDebug("Window is minimized, restoring first");
                        NativeMethods.ShowWindow(windowHandle, WindowConstants.SW_RESTORE);
                    }

                    // Get the foreground window's thread
                    IntPtr foregroundWindow = NativeMethods.GetForegroundWindow();
                    uint foregroundThreadId = 0;
                    uint currentThreadId = NativeMethods.GetCurrentThreadId();
                    uint targetThreadId = NativeMethods.GetWindowThreadProcessId(windowHandle, out _);

                    if (foregroundWindow != IntPtr.Zero)
                    {
                        foregroundThreadId = NativeMethods.GetWindowThreadProcessId(foregroundWindow, out _);
                    }

                    bool attached = false;
                    try
                    {
                        // Attach to the foreground thread to allow SetForegroundWindow to work
                        if (foregroundThreadId != 0 && foregroundThreadId != currentThreadId)
                        {
                            attached = NativeMethods.AttachThreadInput(currentThreadId, foregroundThreadId, true);
                        }

                        // Try multiple approaches to bring window to front
                        bool result = NativeMethods.BringWindowToTop(windowHandle);
                        result = NativeMethods.SetForegroundWindow(windowHandle) || result;

                        // Also show the window to ensure it's visible
                        NativeMethods.ShowWindow(windowHandle, WindowConstants.SW_SHOW);

                        if (result)
                        {
                            _logger.LogInformation("Successfully brought window to front: 0x{Handle:X}", windowHandle.ToInt64());
                        }
                        else
                        {
                            int errorCode = Marshal.GetLastWin32Error();
                            _logger.LogWarning("Failed to bring window to front: 0x{Handle:X}. Win32 error: {ErrorCode}", 
                                windowHandle.ToInt64(), errorCode);
                        }

                        return result;
                    }
                    finally
                    {
                        // Detach from the foreground thread
                        if (attached)
                        {
                            NativeMethods.AttachThreadInput(currentThreadId, foregroundThreadId, false);
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Exception in BringToFrontAsync for handle 0x{Handle:X}", windowHandle.ToInt64());
                    return false;
                }
            });
        }

        /// <inheritdoc/>
        public Task<bool> PreviousTrackAsync(IntPtr windowHandle) =>
            PostMediaCommandAsync(windowHandle, WindowConstants.APPCOMMAND_MEDIA_PREVIOUSTRACK);

        /// <inheritdoc/>
        public Task<bool> NextTrackAsync(IntPtr windowHandle) =>
            PostMediaCommandAsync(windowHandle, WindowConstants.APPCOMMAND_MEDIA_NEXTTRACK);

        /// <inheritdoc/>
        public Task<bool> MinimizeAsync(IntPtr windowHandle)
        {
            return Task.Run(() =>
            {
                try
                {
                    if (!ValidateWindowHandle(windowHandle, nameof(MinimizeAsync)))
                    {
                        return false;
                    }

                    _logger.LogDebug("Minimizing window: 0x{Handle:X}", windowHandle.ToInt64());

                    bool result = NativeMethods.ShowWindow(windowHandle, WindowConstants.SW_MINIMIZE);

                    // ShowWindow returns false if window was not previously visible, which is not an error
                    // Check if window is now minimized
                    bool isMinimized = NativeMethods.IsIconic(windowHandle);

                    if (isMinimized)
                    {
                        _logger.LogInformation("Successfully minimized window: 0x{Handle:X}", windowHandle.ToInt64());
                        return true;
                    }
                    else
                    {
                        int errorCode = Marshal.GetLastWin32Error();
                        _logger.LogWarning("Failed to minimize window: 0x{Handle:X}. Win32 error: {ErrorCode}", 
                            windowHandle.ToInt64(), errorCode);
                        return false;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Exception in MinimizeAsync for handle 0x{Handle:X}", windowHandle.ToInt64());
                    return false;
                }
            });
        }

        /// <inheritdoc/>
        public Task<bool> MaximizeAsync(IntPtr windowHandle)
        {
            return Task.Run(() =>
            {
                try
                {
                    if (!ValidateWindowHandle(windowHandle, nameof(MaximizeAsync)))
                    {
                        return false;
                    }

                    _logger.LogDebug("Maximizing window: 0x{Handle:X}", windowHandle.ToInt64());

                    NativeMethods.ShowWindow(windowHandle, WindowConstants.SW_SHOWMAXIMIZED);

                    // Check if window is now maximized
                    bool isMaximized = NativeMethods.IsZoomed(windowHandle);

                    if (isMaximized)
                    {
                        _logger.LogInformation("Successfully maximized window: 0x{Handle:X}", windowHandle.ToInt64());
                        return true;
                    }
                    else
                    {
                        int errorCode = Marshal.GetLastWin32Error();
                        _logger.LogWarning("Failed to maximize window: 0x{Handle:X}. Win32 error: {ErrorCode}", 
                            windowHandle.ToInt64(), errorCode);
                        return false;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Exception in MaximizeAsync for handle 0x{Handle:X}", windowHandle.ToInt64());
                    return false;
                }
            });
        }

        /// <inheritdoc/>
        public Task<bool> RestoreAsync(IntPtr windowHandle)
        {
            return Task.Run(() =>
            {
                try
                {
                    if (!ValidateWindowHandle(windowHandle, nameof(RestoreAsync)))
                    {
                        return false;
                    }

                    _logger.LogDebug("Restoring window: 0x{Handle:X}", windowHandle.ToInt64());

                    NativeMethods.ShowWindow(windowHandle, WindowConstants.SW_RESTORE);

                    // Check if window is now in normal state (not minimized or maximized)
                    bool isMinimized = NativeMethods.IsIconic(windowHandle);
                    bool isMaximized = NativeMethods.IsZoomed(windowHandle);

                    if (!isMinimized && !isMaximized)
                    {
                        _logger.LogInformation("Successfully restored window: 0x{Handle:X}", windowHandle.ToInt64());
                        return true;
                    }
                    else
                    {
                        // Window might still be in a valid state, just not "normal"
                        _logger.LogDebug("Window restored but may still be minimized/maximized: 0x{Handle:X}", 
                            windowHandle.ToInt64());
                        return true; // Consider it a success if the operation was sent
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Exception in RestoreAsync for handle 0x{Handle:X}", windowHandle.ToInt64());
                    return false;
                }
            });
        }

        /// <inheritdoc/>
        public Task<bool> CloseAsync(IntPtr windowHandle, bool force = false)
        {
            return Task.Run(() =>
            {
                try
                {
                    if (!ValidateWindowHandle(windowHandle, nameof(CloseAsync)))
                    {
                        return false;
                    }

                    _logger.LogDebug("Closing window: 0x{Handle:X}, Force: {Force}", windowHandle.ToInt64(), force);

                    bool result;
                    if (force)
                    {
                        // Use PostMessage for non-blocking close (doesn't wait for response)
                        result = NativeMethods.PostMessage(windowHandle, WindowConstants.WM_CLOSE, IntPtr.Zero, IntPtr.Zero);
                    }
                    else
                    {
                        // Use SendMessage for graceful close (waits for response, allows cancel)
                        NativeMethods.SendMessage(windowHandle, WindowConstants.WM_CLOSE, IntPtr.Zero, IntPtr.Zero);
                        result = true; // SendMessage always "succeeds" in terms of delivery
                    }

                    if (result)
                    {
                        _logger.LogInformation("Close message sent to window: 0x{Handle:X}, Force: {Force}", 
                            windowHandle.ToInt64(), force);
                    }
                    else
                    {
                        int errorCode = Marshal.GetLastWin32Error();
                        _logger.LogWarning("Failed to send close message to window: 0x{Handle:X}. Win32 error: {ErrorCode}", 
                            windowHandle.ToInt64(), errorCode);
                    }

                    return result;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Exception in CloseAsync for handle 0x{Handle:X}", windowHandle.ToInt64());
                    return false;
                }
            });
        }

        /// <inheritdoc/>
        public Task<bool> HideAsync(IntPtr windowHandle)
        {
            return Task.Run(() =>
            {
                try
                {
                    if (!ValidateWindowHandle(windowHandle, nameof(HideAsync)))
                    {
                        return false;
                    }

                    _logger.LogDebug("Hiding window: 0x{Handle:X}", windowHandle.ToInt64());

                    NativeMethods.ShowWindow(windowHandle, WindowConstants.SW_HIDE);

                    // Verify window is now hidden
                    bool isVisible = NativeMethods.IsWindowVisible(windowHandle);

                    if (!isVisible)
                    {
                        _logger.LogInformation("Successfully hid window: 0x{Handle:X}", windowHandle.ToInt64());
                        return true;
                    }
                    else
                    {
                        int errorCode = Marshal.GetLastWin32Error();
                        _logger.LogWarning("Failed to hide window: 0x{Handle:X}. Win32 error: {ErrorCode}", 
                            windowHandle.ToInt64(), errorCode);
                        return false;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Exception in HideAsync for handle 0x{Handle:X}", windowHandle.ToInt64());
                    return false;
                }
            });
        }

        /// <inheritdoc/>
        public Task<bool> ShowAsync(IntPtr windowHandle)
        {
            return Task.Run(() =>
            {
                try
                {
                    if (!ValidateWindowHandle(windowHandle, nameof(ShowAsync)))
                    {
                        return false;
                    }

                    _logger.LogDebug("Showing window: 0x{Handle:X}", windowHandle.ToInt64());

                    NativeMethods.ShowWindow(windowHandle, WindowConstants.SW_SHOW);

                    // Verify window is now visible
                    bool isVisible = NativeMethods.IsWindowVisible(windowHandle);

                    if (isVisible)
                    {
                        _logger.LogInformation("Successfully showed window: 0x{Handle:X}", windowHandle.ToInt64());
                        return true;
                    }
                    else
                    {
                        int errorCode = Marshal.GetLastWin32Error();
                        _logger.LogWarning("Failed to show window: 0x{Handle:X}. Win32 error: {ErrorCode}", 
                            windowHandle.ToInt64(), errorCode);
                        return false;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Exception in ShowAsync for handle 0x{Handle:X}", windowHandle.ToInt64());
                    return false;
                }
            });
        }

        /// <inheritdoc/>
        public Task<bool> SetTransparencyAsync(IntPtr windowHandle, byte alpha)
        {
            return Task.Run(() =>
            {
                try
                {
                    if (!ValidateWindowHandle(windowHandle, nameof(SetTransparencyAsync)))
                    {
                        return false;
                    }

                    _logger.LogDebug("Setting transparency for window: 0x{Handle:X}, Alpha: {Alpha}", 
                        windowHandle.ToInt64(), alpha);

                    // First, ensure the window has the WS_EX_LAYERED style
                    long currentStyle = NativeMethods.GetWindowLongAuto(windowHandle, WindowConstants.GWL_EXSTYLE);
                    
                    if ((currentStyle & WindowConstants.WS_EX_LAYERED) == 0)
                    {
                        _logger.LogDebug("Adding WS_EX_LAYERED style to window: 0x{Handle:X}", windowHandle.ToInt64());
                        long newStyle = currentStyle | WindowConstants.WS_EX_LAYERED;
                        NativeMethods.SetWindowLongAuto(windowHandle, WindowConstants.GWL_EXSTYLE, newStyle);
                    }

                    // Now set the transparency
                    bool result = NativeMethods.SetLayeredWindowAttributes(
                        windowHandle, 
                        0, // crKey - not used with LWA_ALPHA
                        alpha, 
                        WindowConstants.LWA_ALPHA);

                    if (result)
                    {
                        _logger.LogInformation("Successfully set transparency for window: 0x{Handle:X}, Alpha: {Alpha}", 
                            windowHandle.ToInt64(), alpha);
                    }
                    else
                    {
                        int errorCode = Marshal.GetLastWin32Error();
                        _logger.LogWarning("Failed to set transparency for window: 0x{Handle:X}. Win32 error: {ErrorCode}", 
                            windowHandle.ToInt64(), errorCode);
                    }

                    return result;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Exception in SetTransparencyAsync for handle 0x{Handle:X}", windowHandle.ToInt64());
                    return false;
                }
            });
        }

        /// <inheritdoc/>
        public Task<bool> SetAlwaysOnTopAsync(IntPtr windowHandle, bool enabled)
        {
            return Task.Run(() =>
            {
                try
                {
                    if (!ValidateWindowHandle(windowHandle, nameof(SetAlwaysOnTopAsync)))
                    {
                        return false;
                    }

                    _logger.LogDebug("Setting always-on-top for window: 0x{Handle:X}, Enabled: {Enabled}", 
                        windowHandle.ToInt64(), enabled);

                    IntPtr insertAfter = enabled ? WindowConstants.HWND_TOPMOST : WindowConstants.HWND_NOTOPMOST;

                    bool result = NativeMethods.SetWindowPos(
                        windowHandle,
                        insertAfter,
                        0, 0, 0, 0,
                        WindowConstants.SWP_NOMOVE | WindowConstants.SWP_NOSIZE | WindowConstants.SWP_NOACTIVATE);

                    if (result)
                    {
                        _logger.LogInformation("Successfully set always-on-top for window: 0x{Handle:X}, Enabled: {Enabled}", 
                            windowHandle.ToInt64(), enabled);
                    }
                    else
                    {
                        int errorCode = Marshal.GetLastWin32Error();
                        _logger.LogWarning("Failed to set always-on-top for window: 0x{Handle:X}. Win32 error: {ErrorCode}", 
                            windowHandle.ToInt64(), errorCode);
                    }

                    return result;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Exception in SetAlwaysOnTopAsync for handle 0x{Handle:X}", windowHandle.ToInt64());
                    return false;
                }
            });
        }

        /// <inheritdoc/>
        public Task<WindowState> GetWindowStateAsync(IntPtr windowHandle)
        {
            return Task.Run(() =>
            {
                try
                {
                    if (windowHandle == IntPtr.Zero)
                    {
                        return WindowState.Invalid;
                    }

                    if (!NativeMethods.IsWindow(windowHandle))
                    {
                        _logger.LogDebug("Window handle is invalid: 0x{Handle:X}", windowHandle.ToInt64());
                        return WindowState.Invalid;
                    }

                    if (!NativeMethods.IsWindowVisible(windowHandle))
                    {
                        return WindowState.Hidden;
                    }

                    if (NativeMethods.IsIconic(windowHandle))
                    {
                        return WindowState.Minimized;
                    }

                    if (NativeMethods.IsZoomed(windowHandle))
                    {
                        return WindowState.Maximized;
                    }

                    return WindowState.Normal;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Exception in GetWindowStateAsync for handle 0x{Handle:X}", windowHandle.ToInt64());
                    return WindowState.Invalid;
                }
            });
        }

        /// <inheritdoc/>
        public Task<bool> IsWindowVisibleAsync(IntPtr windowHandle)
        {
            return Task.Run(() =>
            {
                try
                {
                    if (windowHandle == IntPtr.Zero || !NativeMethods.IsWindow(windowHandle))
                    {
                        return false;
                    }

                    return NativeMethods.IsWindowVisible(windowHandle);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Exception in IsWindowVisibleAsync for handle 0x{Handle:X}", windowHandle.ToInt64());
                    return false;
                }
            });
        }

        /// <inheritdoc/>
        public Task<bool> IsAlwaysOnTopAsync(IntPtr windowHandle)
        {
            return Task.Run(() =>
            {
                try
                {
                    if (windowHandle == IntPtr.Zero || !NativeMethods.IsWindow(windowHandle))
                    {
                        return false;
                    }

                    long exStyle = NativeMethods.GetWindowLongAuto(windowHandle, WindowConstants.GWL_EXSTYLE);
                    return (exStyle & WindowConstants.WS_EX_TOPMOST) != 0;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Exception in IsAlwaysOnTopAsync for handle 0x{Handle:X}", windowHandle.ToInt64());
                    return false;
                }
            });
        }

        /// <inheritdoc/>
        public Task<byte> GetTransparencyAsync(IntPtr windowHandle)
        {
            return Task.Run(() =>
            {
                try
                {
                    if (windowHandle == IntPtr.Zero || !NativeMethods.IsWindow(windowHandle))
                    {
                        return (byte)255;
                    }

                    // Check if window has layered style
                    long exStyle = NativeMethods.GetWindowLongAuto(windowHandle, WindowConstants.GWL_EXSTYLE);
                    if ((exStyle & WindowConstants.WS_EX_LAYERED) == 0)
                    {
                        // Window is not layered, return fully opaque
                        return (byte)255;
                    }

                    byte alpha = 255;
                    uint flags = 0;
                    if (NativeMethods.GetLayeredWindowAttributes(windowHandle, out _, out alpha, out flags))
                    {
                        // Only return alpha if LWA_ALPHA flag was used
                        if ((flags & WindowConstants.LWA_ALPHA) != 0)
                        {
                            return alpha;
                        }
                    }

                    return (byte)255;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Exception in GetTransparencyAsync for handle 0x{Handle:X}", windowHandle.ToInt64());
                    return (byte)255;
                }
            });
        }

        #region Private Helper Methods

        private Task<bool> PostMediaCommandAsync(IntPtr windowHandle, int command)
        {
            return Task.Run(() =>
            {
                try
                {
                    if (!ValidateWindowHandle(windowHandle, nameof(PostMediaCommandAsync)))
                    {
                        return false;
                    }

                    bool result = NativeMethods.PostMessage(windowHandle, WindowConstants.WM_APPCOMMAND,
                        IntPtr.Zero, new IntPtr(command << 16));
                    if (!result)
                    {
                        _logger.LogWarning("Failed to post media command {Command} to window 0x{Handle:X}. Win32 error: {ErrorCode}",
                            command, windowHandle.ToInt64(), Marshal.GetLastWin32Error());
                    }

                    return result;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Exception posting media command {Command} to window 0x{Handle:X}",
                        command, windowHandle.ToInt64());
                    return false;
                }
            });
        }

        /// <summary>
        /// Validates the window handle and logs appropriate messages.
        /// </summary>
        /// <param name="windowHandle">Handle to validate.</param>
        /// <param name="operationName">Name of the calling operation for logging.</param>
        /// <returns>True if handle is valid; false otherwise.</returns>
        private bool ValidateWindowHandle(IntPtr windowHandle, string operationName)
        {
            if (windowHandle == IntPtr.Zero)
            {
                _logger.LogWarning("{Operation}: Window handle is IntPtr.Zero", operationName);
                return false;
            }

            if (!NativeMethods.IsWindow(windowHandle))
            {
                _logger.LogWarning("{Operation}: Window handle 0x{Handle:X} is not valid", 
                    operationName, windowHandle.ToInt64());
                return false;
            }

            return true;
        }

        #endregion
    }
}
