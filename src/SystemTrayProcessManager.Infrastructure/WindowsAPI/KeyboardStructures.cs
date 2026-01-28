using System.Runtime.InteropServices;

namespace SystemTrayProcessManager.Infrastructure.WindowsAPI
{
    /// <summary>
    /// Delegate for the low-level keyboard hook callback function.
    /// </summary>
    /// <param name="nCode">A code that the hook procedure uses to determine how to process the message.</param>
    /// <param name="wParam">The identifier of the keyboard message.</param>
    /// <param name="lParam">A pointer to a KBDLLHOOKSTRUCT structure.</param>
    /// <returns>The value returned by CallNextHookEx.</returns>
    internal delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);

    /// <summary>
    /// Contains information about a low-level keyboard input event.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    internal struct KBDLLHOOKSTRUCT
    {
        /// <summary>
        /// A virtual-key code.
        /// </summary>
        public int vkCode;

        /// <summary>
        /// A hardware scan code for the key.
        /// </summary>
        public int scanCode;

        /// <summary>
        /// The extended-key flag, event-injected flags, context code, and transition-state flag.
        /// </summary>
        public int flags;

        /// <summary>
        /// The time stamp for this message.
        /// </summary>
        public int time;

        /// <summary>
        /// Additional information associated with the message.
        /// </summary>
        public IntPtr dwExtraInfo;
    }

    /// <summary>
    /// Contains common virtual key codes used for hotkey detection.
    /// </summary>
    internal static class VirtualKeyCodes
    {
        // Modifier keys
        public const int VK_SHIFT = 0x10;
        public const int VK_CONTROL = 0x11;
        public const int VK_MENU = 0x12; // Alt key
        public const int VK_LWIN = 0x5B;
        public const int VK_RWIN = 0x5C;
        public const int VK_LSHIFT = 0xA0;
        public const int VK_RSHIFT = 0xA1;
        public const int VK_LCONTROL = 0xA2;
        public const int VK_RCONTROL = 0xA3;
        public const int VK_LMENU = 0xA4;
        public const int VK_RMENU = 0xA5;

        // Common keys
        public const int VK_ESCAPE = 0x1B;
        public const int VK_SPACE = 0x20;
        public const int VK_RETURN = 0x0D;
        public const int VK_TAB = 0x09;
        public const int VK_BACK = 0x08;
        public const int VK_DELETE = 0x2E;
        public const int VK_INSERT = 0x2D;
        public const int VK_HOME = 0x24;
        public const int VK_END = 0x23;
        public const int VK_PRIOR = 0x21; // Page Up
        public const int VK_NEXT = 0x22;  // Page Down

        // Function keys
        public const int VK_F1 = 0x70;
        public const int VK_F2 = 0x71;
        public const int VK_F3 = 0x72;
        public const int VK_F4 = 0x73;
        public const int VK_F5 = 0x74;
        public const int VK_F6 = 0x75;
        public const int VK_F7 = 0x76;
        public const int VK_F8 = 0x77;
        public const int VK_F9 = 0x78;
        public const int VK_F10 = 0x79;
        public const int VK_F11 = 0x7A;
        public const int VK_F12 = 0x7B;

        // Letter keys (A-Z are 0x41-0x5A)
        public const int VK_A = 0x41;
        public const int VK_Z = 0x5A;

        // Number keys (0-9 are 0x30-0x39)
        public const int VK_0 = 0x30;
        public const int VK_9 = 0x39;

        /// <summary>
        /// Determines whether the specified virtual key code is a modifier key.
        /// </summary>
        /// <param name="vkCode">The virtual key code to check.</param>
        /// <returns>True if the key is a modifier; otherwise, false.</returns>
        public static bool IsModifierKey(int vkCode)
        {
            return vkCode == VK_SHIFT ||
                   vkCode == VK_CONTROL ||
                   vkCode == VK_MENU ||
                   vkCode == VK_LWIN ||
                   vkCode == VK_RWIN ||
                   vkCode == VK_LSHIFT ||
                   vkCode == VK_RSHIFT ||
                   vkCode == VK_LCONTROL ||
                   vkCode == VK_RCONTROL ||
                   vkCode == VK_LMENU ||
                   vkCode == VK_RMENU;
        }
    }
}
