using System.Runtime.InteropServices;

namespace SystemTrayProcessManager.Infrastructure.WindowsAPI
{
    /// <summary>
    /// Contains P/Invoke declarations for Windows API functions used for window manipulation.
    /// All methods are internal to prevent direct access from other assemblies.
    /// </summary>
    internal static partial class NativeMethods
    {
        #region User32.dll - Window Functions

        /// <summary>
        /// Brings the thread that created the specified window into the foreground and activates the window.
        /// </summary>
        /// <param name="hWnd">Handle to the window to activate.</param>
        /// <returns>True if the window was brought to the foreground; false otherwise.</returns>
        [LibraryImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static partial bool SetForegroundWindow(IntPtr hWnd);

        /// <summary>
        /// Sets the specified window's show state.
        /// </summary>
        /// <param name="hWnd">Handle to the window.</param>
        /// <param name="nCmdShow">Controls how the window is to be shown.</param>
        /// <returns>True if the window was previously visible; false otherwise.</returns>
        [LibraryImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static partial bool ShowWindow(IntPtr hWnd, int nCmdShow);

        /// <summary>
        /// Determines whether the specified window handle identifies an existing window.
        /// </summary>
        /// <param name="hWnd">Handle to the window to test.</param>
        /// <returns>True if the window handle identifies a valid window; false otherwise.</returns>
        [LibraryImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static partial bool IsWindow(IntPtr hWnd);

        /// <summary>
        /// Determines the visibility state of the specified window.
        /// </summary>
        /// <param name="hWnd">Handle to the window to test.</param>
        /// <returns>True if the window is visible; false otherwise.</returns>
        [LibraryImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static partial bool IsWindowVisible(IntPtr hWnd);

        /// <summary>
        /// Determines whether the specified window is minimized (iconic).
        /// </summary>
        /// <param name="hWnd">Handle to the window to test.</param>
        /// <returns>True if the window is minimized; false otherwise.</returns>
        [LibraryImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static partial bool IsIconic(IntPtr hWnd);

        /// <summary>
        /// Determines whether the specified window is maximized.
        /// </summary>
        /// <param name="hWnd">Handle to the window to test.</param>
        /// <returns>True if the window is maximized; false otherwise.</returns>
        [LibraryImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static partial bool IsZoomed(IntPtr hWnd);

        /// <summary>
        /// Retrieves information about the specified window.
        /// </summary>
        /// <param name="hWnd">Handle to the window.</param>
        /// <param name="nIndex">The zero-based offset to the value to retrieve.</param>
        /// <returns>The requested value, or zero on failure.</returns>
        [LibraryImport("user32.dll", EntryPoint = "GetWindowLongW", SetLastError = true)]
        public static partial int GetWindowLong(IntPtr hWnd, int nIndex);

        /// <summary>
        /// Retrieves information about the specified window (64-bit compatible).
        /// </summary>
        /// <param name="hWnd">Handle to the window.</param>
        /// <param name="nIndex">The zero-based offset to the value to retrieve.</param>
        /// <returns>The requested value, or zero on failure.</returns>
        [LibraryImport("user32.dll", EntryPoint = "GetWindowLongPtrW", SetLastError = true)]
        public static partial IntPtr GetWindowLongPtr(IntPtr hWnd, int nIndex);

        /// <summary>
        /// Changes an attribute of the specified window.
        /// </summary>
        /// <param name="hWnd">Handle to the window.</param>
        /// <param name="nIndex">The zero-based offset to the value to set.</param>
        /// <param name="dwNewLong">The replacement value.</param>
        /// <returns>The previous value of the specified offset, or zero on failure.</returns>
        [LibraryImport("user32.dll", EntryPoint = "SetWindowLongW", SetLastError = true)]
        public static partial int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

        /// <summary>
        /// Changes an attribute of the specified window (64-bit compatible).
        /// </summary>
        /// <param name="hWnd">Handle to the window.</param>
        /// <param name="nIndex">The zero-based offset to the value to set.</param>
        /// <param name="dwNewLong">The replacement value.</param>
        /// <returns>The previous value of the specified offset, or zero on failure.</returns>
        [LibraryImport("user32.dll", EntryPoint = "SetWindowLongPtrW", SetLastError = true)]
        public static partial IntPtr SetWindowLongPtr(IntPtr hWnd, int nIndex, IntPtr dwNewLong);

        /// <summary>
        /// Sets the opacity and transparency color key of a layered window.
        /// </summary>
        /// <param name="hwnd">Handle to the layered window.</param>
        /// <param name="crKey">The transparency color key (COLORREF).</param>
        /// <param name="bAlpha">Alpha value for opacity (0-255).</param>
        /// <param name="dwFlags">Flags indicating the layered window attributes to set.</param>
        /// <returns>True if successful; false otherwise.</returns>
        [LibraryImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static partial bool SetLayeredWindowAttributes(IntPtr hwnd, uint crKey, byte bAlpha, uint dwFlags);

        /// <summary>
        /// Retrieves the opacity and transparency color key of a layered window.
        /// </summary>
        /// <param name="hwnd">Handle to the layered window.</param>
        /// <param name="pcrKey">Pointer to receive the transparency color key.</param>
        /// <param name="pbAlpha">Pointer to receive the alpha value.</param>
        /// <param name="pdwFlags">Pointer to receive the layered window attributes.</param>
        /// <returns>True if successful; false otherwise.</returns>
        [LibraryImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static partial bool GetLayeredWindowAttributes(IntPtr hwnd, out uint pcrKey, out byte pbAlpha, out uint pdwFlags);

        /// <summary>
        /// Changes the size, position, and Z order of a child, pop-up, or top-level window.
        /// </summary>
        /// <param name="hWnd">Handle to the window.</param>
        /// <param name="hWndInsertAfter">Handle to the window to precede the positioned window in the Z order.</param>
        /// <param name="X">The new position of the left side of the window.</param>
        /// <param name="Y">The new position of the top of the window.</param>
        /// <param name="cx">The new width of the window, in pixels.</param>
        /// <param name="cy">The new height of the window, in pixels.</param>
        /// <param name="uFlags">The window sizing and positioning flags.</param>
        /// <returns>True if successful; false otherwise.</returns>
        [LibraryImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static partial bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

        /// <summary>
        /// Sends the specified message to a window or windows.
        /// </summary>
        /// <param name="hWnd">Handle to the window whose window procedure will receive the message.</param>
        /// <param name="Msg">The message to be sent.</param>
        /// <param name="wParam">Additional message-specific information.</param>
        /// <param name="lParam">Additional message-specific information.</param>
        /// <returns>The result of the message processing; depends on the message sent.</returns>
        [LibraryImport("user32.dll", EntryPoint = "SendMessageW", SetLastError = true)]
        public static partial IntPtr SendMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

        /// <summary>
        /// Places (posts) a message in the message queue and returns immediately.
        /// </summary>
        /// <param name="hWnd">Handle to the window whose window procedure is to receive the message.</param>
        /// <param name="Msg">The message to be posted.</param>
        /// <param name="wParam">Additional message-specific information.</param>
        /// <param name="lParam">Additional message-specific information.</param>
        /// <returns>True if the function succeeds; false otherwise.</returns>
        [LibraryImport("user32.dll", EntryPoint = "PostMessageW", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static partial bool PostMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

        /// <summary>
        /// Retrieves the identifier of the thread that created the specified window.
        /// </summary>
        /// <param name="hWnd">Handle to the window.</param>
        /// <param name="lpdwProcessId">Pointer to a variable that receives the process identifier.</param>
        /// <returns>The identifier of the thread that created the window.</returns>
        [LibraryImport("user32.dll", SetLastError = true)]
        public static partial uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

        /// <summary>
        /// Retrieves the window handle to the active window attached to the calling thread's message queue.
        /// </summary>
        /// <returns>Handle to the active window, or IntPtr.Zero if there is no active window.</returns>
        [LibraryImport("user32.dll")]
        public static partial IntPtr GetForegroundWindow();

        /// <summary>
        /// Attaches or detaches the input processing mechanism of one thread to that of another thread.
        /// </summary>
        /// <param name="idAttach">The identifier of the thread to be attached to another thread.</param>
        /// <param name="idAttachTo">The identifier of the thread to which idAttach will be attached.</param>
        /// <param name="fAttach">True to attach, false to detach.</param>
        /// <returns>True if successful; false otherwise.</returns>
        [LibraryImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static partial bool AttachThreadInput(uint idAttach, uint idAttachTo, [MarshalAs(UnmanagedType.Bool)] bool fAttach);

        /// <summary>
        /// Brings the specified window to the top of the Z order.
        /// </summary>
        /// <param name="hWnd">Handle to the window to bring to the top.</param>
        /// <returns>True if successful; false otherwise.</returns>
        [LibraryImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static partial bool BringWindowToTop(IntPtr hWnd);

        #endregion

        #region Kernel32.dll - Thread Functions

        /// <summary>
        /// Retrieves the thread identifier of the calling thread.
        /// </summary>
        /// <returns>The thread identifier of the calling thread.</returns>
        [LibraryImport("kernel32.dll")]
        public static partial uint GetCurrentThreadId();

        #endregion

        #region Helper Methods

        /// <summary>
        /// Gets window style using the appropriate function for the current platform (32/64-bit).
        /// </summary>
        /// <param name="hWnd">Handle to the window.</param>
        /// <param name="nIndex">The index of the value to retrieve.</param>
        /// <returns>The window style value.</returns>
        public static long GetWindowLongAuto(IntPtr hWnd, int nIndex)
        {
            if (IntPtr.Size == 8)
            {
                return GetWindowLongPtr(hWnd, nIndex).ToInt64();
            }
            else
            {
                return GetWindowLong(hWnd, nIndex);
            }
        }

        /// <summary>
        /// Sets window style using the appropriate function for the current platform (32/64-bit).
        /// </summary>
        /// <param name="hWnd">Handle to the window.</param>
        /// <param name="nIndex">The index of the value to set.</param>
        /// <param name="dwNewLong">The new value to set.</param>
        /// <returns>The previous value, or zero on failure.</returns>
        public static long SetWindowLongAuto(IntPtr hWnd, int nIndex, long dwNewLong)
        {
            if (IntPtr.Size == 8)
            {
                return SetWindowLongPtr(hWnd, nIndex, new IntPtr(dwNewLong)).ToInt64();
            }
            else
            {
                return SetWindowLong(hWnd, nIndex, (int)dwNewLong);
            }
        }

                        #endregion

                        #region User32.dll - Keyboard Hook Functions

                        /// <summary>
                        /// Installs an application-defined hook procedure into a hook chain.
                        /// </summary>
                        /// <param name="idHook">The type of hook procedure to be installed.</param>
                        /// <param name="lpfn">A pointer to the hook procedure.</param>
                        /// <param name="hMod">A handle to the DLL containing the hook procedure (IntPtr.Zero for managed code with WH_KEYBOARD_LL).</param>
                        /// <param name="dwThreadId">The identifier of the thread with which the hook procedure is to be associated.</param>
                        /// <returns>Handle to the hook procedure, or IntPtr.Zero if the function fails.</returns>
                        [LibraryImport("user32.dll", SetLastError = true)]
                        public static partial IntPtr SetWindowsHookEx(int idHook, IntPtr lpfn, IntPtr hMod, uint dwThreadId);

                        /// <summary>
                        /// Removes a hook procedure installed in a hook chain.
                        /// </summary>
                        /// <param name="hhk">Handle to the hook to be removed.</param>
                        /// <returns>True if successful; false otherwise.</returns>
                        [LibraryImport("user32.dll", SetLastError = true)]
                        [return: MarshalAs(UnmanagedType.Bool)]
                        public static partial bool UnhookWindowsHookEx(IntPtr hhk);

                        /// <summary>
                        /// Passes the hook information to the next hook procedure in the current hook chain.
                        /// </summary>
                        /// <param name="hhk">This parameter is ignored.</param>
                        /// <param name="nCode">The hook code passed to the current hook procedure.</param>
                        /// <param name="wParam">The wParam value passed to the current hook procedure.</param>
                        /// <param name="lParam">The lParam value passed to the current hook procedure.</param>
                        /// <returns>The value returned by the next hook procedure in the chain.</returns>
                        [LibraryImport("user32.dll", SetLastError = true)]
                        public static partial IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

                        /// <summary>
                        /// Determines whether a key is up or down at the time the function is called.
                        /// </summary>
                        /// <param name="vKey">The virtual-key code.</param>
                        /// <returns>If the high-order bit is 1, the key is down; otherwise, it is up.</returns>
                        [LibraryImport("user32.dll")]
                        public static partial short GetAsyncKeyState(int vKey);

                        /// <summary>
                        /// Checks if the specified key is currently pressed.
                        /// </summary>
                        /// <param name="vKey">The virtual-key code to check.</param>
                        /// <returns>True if the key is pressed; false otherwise.</returns>
                        public static bool IsKeyPressed(int vKey)
                        {
                            return (GetAsyncKeyState(vKey) & 0x8000) != 0;
                        }

                        #endregion
                    }
                }


