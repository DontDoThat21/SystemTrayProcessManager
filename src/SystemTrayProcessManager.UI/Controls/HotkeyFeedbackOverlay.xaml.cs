using System.Runtime.InteropServices;
using System.Windows.Interop;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using WpfSystemParameters = System.Windows.SystemParameters;

namespace SystemTrayProcessManager.UI.Controls
{
    /// <summary>
    /// An OBS-style transparent overlay window that briefly displays which hotkey was pressed.
    /// Appears at the bottom-right of the primary screen and fades out after 2 seconds.
    /// </summary>
    public partial class HotkeyFeedbackOverlay : Window
    {
        private readonly DispatcherTimer _dismissTimer;

        // P/Invoke for setting extended window styles (tool window + transparent)
        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr GetWindowLongPtr(IntPtr hWnd, int nIndex);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr SetWindowLongPtr(IntPtr hWnd, int nIndex, IntPtr dwNewLong);

        private const int GWL_EXSTYLE = -20;
        private const int WS_EX_TOOLWINDOW = 0x00000080;
        private const int WS_EX_TRANSPARENT = 0x00000020;

        /// <summary>
        /// Initializes a new instance of the <see cref="HotkeyFeedbackOverlay"/> class.
        /// </summary>
        public HotkeyFeedbackOverlay()
        {
            InitializeComponent();

            _dismissTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(2)
            };
            _dismissTimer.Tick += OnDismissTimerTick;
        }

        /// <summary>
        /// Shows the overlay with the specified hotkey text and auto-dismisses after 2 seconds.
        /// </summary>
        /// <param name="hotkeyDisplayText">The hotkey combination text to display (e.g., "Ctrl+Shift+M").</param>
        public void ShowHotkey(string hotkeyDisplayText)
        {
            HotkeyText.Text = hotkeyDisplayText;

            // Position at bottom-right of primary screen
            var workArea = WpfSystemParameters.WorkArea;
            Left = workArea.Right - 250;
            Top = workArea.Bottom - 80;

            // Reset opacity and show
            OverlayBorder.Opacity = 1;
            Show();

            // Restart dismiss timer
            _dismissTimer.Stop();
            _dismissTimer.Start();
        }

        /// <summary>
        /// Handles the dismiss timer tick - triggers fade-out animation.
        /// </summary>
        private void OnDismissTimerTick(object? sender, EventArgs e)
        {
            _dismissTimer.Stop();

            var fadeOut = new DoubleAnimation(1, 0, TimeSpan.FromMilliseconds(400))
            {
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseIn }
            };

            fadeOut.Completed += (s, args) => Hide();
            OverlayBorder.BeginAnimation(OpacityProperty, fadeOut);
        }

        /// <summary>
        /// Prevents the overlay from appearing in Alt+Tab window switcher
        /// and makes it click-through (transparent to input).
        /// </summary>
        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);

            var helper = new WindowInteropHelper(this);
            var exStyle = GetWindowLongPtr(helper.Handle, GWL_EXSTYLE);
            exStyle = (IntPtr)((long)exStyle | WS_EX_TOOLWINDOW | WS_EX_TRANSPARENT);
            SetWindowLongPtr(helper.Handle, GWL_EXSTYLE, exStyle);
        }
    }
}
