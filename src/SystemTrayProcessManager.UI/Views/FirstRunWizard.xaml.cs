using Microsoft.Extensions.Logging;
using SystemTrayProcessManager.Core.Models;
using SystemTrayProcessManager.Core.Services;

namespace SystemTrayProcessManager.UI.Views
{
    /// <summary>
    /// Interaction logic for FirstRunWizard.xaml.
    /// A multi-step wizard shown on first application launch for initial setup.
    /// </summary>
    public partial class FirstRunWizard : Window
    {
        private readonly IConfigurationService _configurationService;
        private readonly ILogger<FirstRunWizard> _logger;
        private int _currentPage;
        private const int TotalPages = 4;

        /// <summary>
        /// Gets a value indicating whether the wizard completed (user reached the final step).
        /// </summary>
        public bool WizardCompleted { get; private set; }

        /// <summary>
        /// Gets the settings configured during the wizard.
        /// </summary>
        public AppSettings? ConfiguredSettings { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="FirstRunWizard"/> class.
        /// </summary>
        /// <param name="configurationService">The configuration service.</param>
        /// <param name="logger">The logger instance.</param>
        public FirstRunWizard(
            IConfigurationService configurationService,
            ILogger<FirstRunWizard> logger)
        {
            _configurationService = configurationService ?? throw new ArgumentNullException(nameof(configurationService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            InitializeComponent();
            UpdateNavigation();
        }

        /// <summary>
        /// Handles the Next button click.
        /// </summary>
        private async void OnNextClicked(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_currentPage < TotalPages - 1)
                {
                    _currentPage++;
                    WizardPages.SelectedIndex = _currentPage;
                    UpdateNavigation();
                }
                else
                {
                    // Final page - save settings and close
                    await CompleteWizardAsync();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error navigating wizard");
            }
        }

        /// <summary>
        /// Handles the Back button click.
        /// </summary>
        private void OnBackClicked(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_currentPage > 0)
                {
                    _currentPage--;
                    WizardPages.SelectedIndex = _currentPage;
                    UpdateNavigation();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error navigating wizard back");
            }
        }

        /// <summary>
        /// Updates navigation buttons and page indicator based on current step.
        /// </summary>
        private void UpdateNavigation()
        {
            PageIndicator.Text = $"Step {_currentPage + 1} of {TotalPages}";
            BackButton.Visibility = _currentPage > 0
                ? System.Windows.Visibility.Visible
                : System.Windows.Visibility.Collapsed;
            NextButton.Content = _currentPage == TotalPages - 1 ? "Finish" : "Next";
        }

        /// <summary>
        /// Completes the wizard by saving initial settings.
        /// </summary>
        private async Task CompleteWizardAsync()
        {
            try
            {
                _logger.LogInformation("First-run wizard completing");

                var settings = _configurationService.GetDefaults();
                settings.IsFirstRun = false;
                settings.HotkeysEnabled = EnableHotkeysCheck.IsChecked ?? true;

                var success = await _configurationService.SaveSettingsAsync(settings);
                if (success)
                {
                    _logger.LogInformation("First-run wizard settings saved successfully");
                }
                else
                {
                    _logger.LogWarning("Failed to save first-run wizard settings");
                }

                ConfiguredSettings = settings;
                WizardCompleted = true;
                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error completing first-run wizard");
                WizardCompleted = false;
                DialogResult = false;
                Close();
            }
        }
    }
}
