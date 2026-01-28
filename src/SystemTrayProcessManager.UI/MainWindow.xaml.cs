using Microsoft.Extensions.Logging;
using System.Windows;

namespace SystemTrayProcessManager.UI
{
    /// <summary>
    /// Main application window.
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly ILogger<MainWindow> _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="MainWindow"/> class.
        /// </summary>
        /// <param name="logger">Logger instance for this window.</param>
        public MainWindow(ILogger<MainWindow> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            
            InitializeComponent();
            
            _logger.LogInformation("MainWindow initialized");
        }
    }
}