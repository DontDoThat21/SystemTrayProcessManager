namespace SystemTrayProcessManager.Infrastructure.WindowsAPI
{
    /// <summary>
    /// Contains Windows API constants used for window manipulation operations.
    /// </summary>
    internal static class WindowConstants
    {
        #region ShowWindow Commands (nCmdShow)

        /// <summary>
        /// Hides the window and activates another window.
        /// </summary>
        public const int SW_HIDE = 0;

        /// <summary>
        /// Activates and displays the window. If the window is minimized or maximized, 
        /// Windows restores it to its original size and position.
        /// </summary>
        public const int SW_SHOWNORMAL = 1;

        /// <summary>
        /// Activates the window and displays it as a minimized window.
        /// </summary>
        public const int SW_SHOWMINIMIZED = 2;

        /// <summary>
        /// Activates the window and displays it as a maximized window.
        /// </summary>
        public const int SW_SHOWMAXIMIZED = 3;

        /// <summary>
        /// Displays the window in its most recent size and position. 
        /// The active window remains active.
        /// </summary>
        public const int SW_SHOWNOACTIVATE = 4;

        /// <summary>
        /// Activates the window and displays it in its current size and position.
        /// </summary>
        public const int SW_SHOW = 5;

        /// <summary>
        /// Minimizes the specified window and activates the next top-level window in the Z order.
        /// </summary>
        public const int SW_MINIMIZE = 6;

        /// <summary>
        /// Displays the window as a minimized window. The active window remains active.
        /// </summary>
        public const int SW_SHOWMINNOACTIVE = 7;

        /// <summary>
        /// Displays the window in its current size and position. The active window remains active.
        /// </summary>
        public const int SW_SHOWNA = 8;

        /// <summary>
        /// Activates and displays the window. If the window is minimized or maximized, 
        /// Windows restores it to its original size and position.
        /// </summary>
        public const int SW_RESTORE = 9;

        /// <summary>
        /// Sets the show state based on the SW_ value specified in the STARTUPINFO structure.
        /// </summary>
        public const int SW_SHOWDEFAULT = 10;

        /// <summary>
        /// Minimizes a window, even if the thread that owns the window is not responding.
        /// </summary>
        public const int SW_FORCEMINIMIZE = 11;

        #endregion

        #region Window Messages (WM_)

        /// <summary>
        /// Sent as a signal that a window or an application should terminate.
        /// </summary>
        public const uint WM_CLOSE = 0x0010;

        /// <summary>
        /// Sent when a window is about to be destroyed.
        /// </summary>
        public const uint WM_DESTROY = 0x0002;

        /// <summary>
        /// Sent to indicate that the user wishes to terminate the application.
        /// </summary>
        public const uint WM_QUIT = 0x0012;

        /// <summary>
        /// A window receives this message when the user chooses a command from the Window menu.
        /// </summary>
        public const uint WM_SYSCOMMAND = 0x0112;

        /// <summary>
        /// Sent to a window when it is about to lose the keyboard focus.
        /// </summary>
        public const uint WM_KILLFOCUS = 0x0008;

        /// <summary>
        /// Sent to a window to retrieve a handle to the large or small icon.
        /// </summary>
        public const uint WM_GETICON = 0x007F;

        /// <summary>
        /// Sent to a window to associate a new large or small icon.
        /// </summary>
        public const uint WM_SETICON = 0x0080;

        #endregion

        #region System Command Values (SC_)

        /// <summary>
        /// Closes the window.
        /// </summary>
        public const int SC_CLOSE = 0xF060;

        /// <summary>
        /// Minimizes the window.
        /// </summary>
        public const int SC_MINIMIZE = 0xF020;

        /// <summary>
        /// Maximizes the window.
        /// </summary>
        public const int SC_MAXIMIZE = 0xF030;

        /// <summary>
        /// Restores the window to its normal position and size.
        /// </summary>
        public const int SC_RESTORE = 0xF120;

        #endregion

        #region GetWindowLong/SetWindowLong Indices (GWL_)

        /// <summary>
        /// Retrieves the extended window styles.
        /// </summary>
        public const int GWL_EXSTYLE = -20;

        /// <summary>
        /// Retrieves the window styles.
        /// </summary>
        public const int GWL_STYLE = -16;

        /// <summary>
        /// Retrieves the window procedure address or handle.
        /// </summary>
        public const int GWL_WNDPROC = -4;

        /// <summary>
        /// Retrieves the handle to the application instance.
        /// </summary>
        public const int GWL_HINSTANCE = -6;

        /// <summary>
        /// Retrieves the identifier of the window.
        /// </summary>
        public const int GWL_ID = -12;

        /// <summary>
        /// Retrieves the user data associated with the window.
        /// </summary>
        public const int GWL_USERDATA = -21;

        #endregion

        #region Window Styles (WS_)

        /// <summary>
        /// The window has a thin-line border.
        /// </summary>
        public const long WS_BORDER = 0x00800000L;

        /// <summary>
        /// The window has a title bar (includes WS_BORDER).
        /// </summary>
        public const long WS_CAPTION = 0x00C00000L;

        /// <summary>
        /// The window is initially disabled.
        /// </summary>
        public const long WS_DISABLED = 0x08000000L;

        /// <summary>
        /// The window is initially visible.
        /// </summary>
        public const long WS_VISIBLE = 0x10000000L;

        /// <summary>
        /// The window is initially minimized.
        /// </summary>
        public const long WS_MINIMIZE = 0x20000000L;

        /// <summary>
        /// The window is initially maximized.
        /// </summary>
        public const long WS_MAXIMIZE = 0x01000000L;

        #endregion

        #region Extended Window Styles (WS_EX_)

        /// <summary>
        /// The window should be placed above all non-topmost windows.
        /// </summary>
        public const long WS_EX_TOPMOST = 0x00000008L;

        /// <summary>
        /// The window is a layered window. Cannot be used for child windows.
        /// </summary>
        public const long WS_EX_LAYERED = 0x00080000L;

        /// <summary>
        /// The window is transparent.
        /// </summary>
        public const long WS_EX_TRANSPARENT = 0x00000020L;

        /// <summary>
        /// The window does not render to a redirection surface.
        /// </summary>
        public const long WS_EX_NOREDIRECTIONBITMAP = 0x00200000L;

        /// <summary>
        /// The window has a border with a raised edge.
        /// </summary>
        public const long WS_EX_WINDOWEDGE = 0x00000100L;

        /// <summary>
        /// The window has a border with a sunken edge.
        /// </summary>
        public const long WS_EX_CLIENTEDGE = 0x00000200L;

        /// <summary>
        /// The window is a tool window (thin title bar).
        /// </summary>
        public const long WS_EX_TOOLWINDOW = 0x00000080L;

        /// <summary>
        /// The window is an application window.
        /// </summary>
        public const long WS_EX_APPWINDOW = 0x00040000L;

        #endregion

        #region SetLayeredWindowAttributes Flags (LWA_)

        /// <summary>
        /// Use crKey as the transparency color.
        /// </summary>
        public const uint LWA_COLORKEY = 0x00000001;

        /// <summary>
        /// Use bAlpha to determine the opacity of the layered window.
        /// </summary>
        public const uint LWA_ALPHA = 0x00000002;

        #endregion

        #region SetWindowPos Flags (SWP_)

        /// <summary>
        /// Retains the current size (ignores cx and cy).
        /// </summary>
        public const uint SWP_NOSIZE = 0x0001;

        /// <summary>
        /// Retains the current position (ignores X and Y).
        /// </summary>
        public const uint SWP_NOMOVE = 0x0002;

        /// <summary>
        /// Retains the current Z order (ignores hWndInsertAfter).
        /// </summary>
        public const uint SWP_NOZORDER = 0x0004;

        /// <summary>
        /// Does not activate the window.
        /// </summary>
        public const uint SWP_NOACTIVATE = 0x0010;

        /// <summary>
        /// Applies new frame styles set using SetWindowLong.
        /// </summary>
        public const uint SWP_FRAMECHANGED = 0x0020;

        /// <summary>
        /// Displays the window.
        /// </summary>
        public const uint SWP_SHOWWINDOW = 0x0040;

        /// <summary>
        /// Hides the window.
        /// </summary>
        public const uint SWP_HIDEWINDOW = 0x0080;

        /// <summary>
        /// Does not redraw changes.
        /// </summary>
        public const uint SWP_NOREDRAW = 0x0008;

        /// <summary>
        /// Does not change the owner window's position in the Z order.
        /// </summary>
        public const uint SWP_NOOWNERZORDER = 0x0200;

        /// <summary>
        /// Prevents the window from receiving WM_WINDOWPOSCHANGING.
        /// </summary>
        public const uint SWP_NOSENDCHANGING = 0x0400;

        #endregion

        #region SetWindowPos Z-Order Values (HWND_)

        /// <summary>
        /// Places the window at the top of the Z order.
        /// </summary>
        public static readonly IntPtr HWND_TOP = IntPtr.Zero;

        /// <summary>
        /// Places the window at the bottom of the Z order.
        /// </summary>
        public static readonly IntPtr HWND_BOTTOM = new(1);

        /// <summary>
        /// Places the window above all non-topmost windows. The window maintains its topmost position.
        /// </summary>
        public static readonly IntPtr HWND_TOPMOST = new(-1);

        /// <summary>
        /// Places the window above all non-topmost windows but below all topmost windows.
        /// </summary>
        public static readonly IntPtr HWND_NOTOPMOST = new(-2);

        #endregion

        #region Win32 Error Codes

        /// <summary>
        /// The operation completed successfully.
        /// </summary>
        public const int ERROR_SUCCESS = 0;

        /// <summary>
        /// Access is denied.
        /// </summary>
        public const int ERROR_ACCESS_DENIED = 5;

        /// <summary>
        /// The window handle is not valid.
        /// </summary>
        public const int ERROR_INVALID_WINDOW_HANDLE = 1400;

        /// <summary>
        /// The parameter is incorrect.
        /// </summary>
        public const int ERROR_INVALID_PARAMETER = 87;

        #endregion
    }
}
