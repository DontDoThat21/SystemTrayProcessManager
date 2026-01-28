namespace SystemTrayProcessManager.UI.Controls
{
    /// <summary>
    /// In-app notification panel that displays process events and system status messages.
    /// Hosted within the main window as an overlay panel.
    /// </summary>
    public partial class NotificationPanel : System.Windows.Controls.UserControl
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="NotificationPanel"/> class.
        /// </summary>
        public NotificationPanel()
        {
            InitializeComponent();
        }
    }
}
