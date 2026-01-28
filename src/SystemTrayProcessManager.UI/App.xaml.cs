using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;
using System;
using System.IO;
using System.Threading;
using System.Windows;
using System.Windows.Threading;

namespace SystemTrayProcessManager.UI
{
    /// <summary>
    /// Main application class with dependency injection, logging, and lifecycle management.
    /// </summary>
    public partial class App : Application
    {
        private const string MutexName = "Global\\SystemTrayProcessManager_SingleInstance_4E9F2A1B";
        
        private IServiceProvider? _serviceProvider;
        private Mutex? _instanceMutex;
        private bool _mutexCreated;

        /// <summary>
        /// Gets the application's service provider for dependency injection.
        /// </summary>
        public IServiceProvider Services => _serviceProvider 
            ?? throw new InvalidOperationException("Service provider not initialized.");

        /// <summary>
        /// Initializes the application on startup with logging, DI, and single instance check.
        /// </summary>
        /// <param name="e">Startup event arguments.</param>
        protected override void OnStartup(StartupEventArgs e)
        {
            try
            {
                // Step 1: Configure logging before anything else
                ConfigureLogging();

                Log.Information("========================================");
                Log.Information("SystemTrayProcessManager starting...");
                Log.Information("Version: {Version}", GetType().Assembly.GetName().Version);
                Log.Information("========================================");

                // Step 2: Check for single instance
                if (!CheckSingleInstance())
                {
                    Log.Warning("Another instance is already running. Shutting down.");
                    Shutdown(1);
                    return;
                }

                Log.Information("Single instance check passed.");

                // Step 3: Configure global exception handlers
                ConfigureExceptionHandlers();

                Log.Information("Exception handlers configured.");

                // Step 4: Configure dependency injection
                _serviceProvider = ConfigureServices();

                Log.Information("Dependency injection configured.");

                // Step 5: Initialize and show main window
                var mainWindow = _serviceProvider.GetRequiredService<MainWindow>();
                mainWindow.Show();

                Log.Information("Application startup completed successfully.");

                base.OnStartup(e);
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, "Fatal error during application startup");
                
                MessageBox.Show(
                    $"A fatal error occurred during startup:\n\n{ex.Message}\n\nThe application will now close.",
                    "Startup Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
                
                Shutdown(2);
            }
        }

        /// <summary>
        /// Handles application shutdown and cleanup.
        /// </summary>
        /// <param name="e">Exit event arguments.</param>
        protected override void OnExit(ExitEventArgs e)
        {
            try
            {
                Log.Information("Application shutting down...");

                // Dispose services
                if (_serviceProvider is IDisposable disposable)
                {
                    Log.Debug("Disposing service provider...");
                    disposable.Dispose();
                }

                // Release mutex
                if (_mutexCreated && _instanceMutex != null)
                {
                    Log.Debug("Releasing instance mutex...");
                    _instanceMutex.ReleaseMutex();
                    _instanceMutex.Dispose();
                }

                Log.Information("Application shutdown completed. Exit code: {ExitCode}", e.ApplicationExitCode);
                Log.Information("========================================");
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error during application shutdown");
            }
            finally
            {
                // Ensure all logs are flushed
                Log.CloseAndFlush();
                
                base.OnExit(e);
            }
        }

        /// <summary>
        /// Configures Serilog logging with file output to AppData.
        /// </summary>
        private void ConfigureLogging()
        {
            // Get AppData folder path
            var appDataPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "SystemTrayProcessManager");

            var logsPath = Path.Combine(appDataPath, "logs");

            // Ensure directory exists
            Directory.CreateDirectory(logsPath);

            var logFilePath = Path.Combine(logsPath, "app.log");

            // Configure Serilog
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .Enrich.FromLogContext()
                .Enrich.WithProperty("Application", "SystemTrayProcessManager")
                .Enrich.WithProperty("MachineName", Environment.MachineName)
                .Enrich.WithProperty("UserName", Environment.UserName)
                .WriteTo.File(
                    logFilePath,
                    rollingInterval: RollingInterval.Day,
                    retainedFileCountLimit: 7,
                    fileSizeLimitBytes: 52428800, // 50 MB
                    outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}")
                .CreateLogger();
        }

        /// <summary>
        /// Configures global exception handlers to log unhandled exceptions.
        /// </summary>
        private void ConfigureExceptionHandlers()
        {
            // Handle exceptions on UI thread
            DispatcherUnhandledException += OnDispatcherUnhandledException;

            // Handle exceptions on background threads
            AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;

            // Handle unobserved task exceptions
            TaskScheduler.UnobservedTaskException += OnUnobservedTaskException;
        }

        /// <summary>
        /// Handles unhandled exceptions on the UI dispatcher thread.
        /// </summary>
        private void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            Log.Fatal(e.Exception, "Unhandled exception on UI thread");

            var result = MessageBox.Show(
                $"An unexpected error occurred:\n\n{e.Exception.Message}\n\nWould you like to continue running?",
                "Unexpected Error",
                MessageBoxButton.YesNo,
                MessageBoxImage.Error);

            if (result == MessageBoxResult.Yes)
            {
                e.Handled = true;
                Log.Information("User chose to continue after unhandled exception");
            }
            else
            {
                Log.Information("User chose to exit after unhandled exception");
                e.Handled = false;
            }
        }

        /// <summary>
        /// Handles unhandled exceptions on background threads.
        /// </summary>
        private void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            var exception = e.ExceptionObject as Exception;
            
            if (e.IsTerminating)
            {
                Log.Fatal(exception, "Fatal unhandled exception - application will terminate");
            }
            else
            {
                Log.Error(exception, "Unhandled exception on background thread");
            }
        }

        /// <summary>
        /// Handles unobserved task exceptions.
        /// </summary>
        private void OnUnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
        {
            Log.Error(e.Exception, "Unobserved task exception");
            e.SetObserved(); // Prevent process termination
        }

        /// <summary>
        /// Checks if another instance of the application is already running.
        /// </summary>
        /// <returns>True if this is the only instance; false if another instance is running.</returns>
        private bool CheckSingleInstance()
        {
            try
            {
                _instanceMutex = new Mutex(true, MutexName, out _mutexCreated);

                if (!_mutexCreated)
                {
                    MessageBox.Show(
                        "SystemTrayProcessManager is already running.\n\nOnly one instance of the application can run at a time.",
                        "Already Running",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);

                    return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error during single instance check");
                
                // If mutex creation fails, allow application to start anyway
                return true;
            }
        }

        /// <summary>
        /// Configures the dependency injection container and registers all services.
        /// </summary>
        /// <returns>Configured service provider.</returns>
        private IServiceProvider ConfigureServices()
        {
            var services = new ServiceCollection();

            // Register logging
            services.AddLogging(loggingBuilder =>
            {
                loggingBuilder.ClearProviders();
                loggingBuilder.AddSerilog(dispose: true);
            });

            // Register windows
            services.AddTransient<MainWindow>();

            // TODO: Register services from Infrastructure project as they are created
            // Example:
            // services.AddSingleton<IProcessService, ProcessMonitorService>();
            // services.AddSingleton<IWindowService, WindowManipulationService>();
            // services.AddSingleton<IAudioService, AudioManagerService>();
            // services.AddSingleton<IHotkeyService, HotkeyManagerService>();

            // TODO: Register ViewModels as they are created
            // Example:
            // services.AddTransient<MainViewModel>();
            // services.AddTransient<SettingsViewModel>();

            var serviceProvider = services.BuildServiceProvider();

            // Log registered services (useful for debugging)
            Log.Debug("Service provider built with {Count} services", services.Count);

            return serviceProvider;
        }
    }
}
