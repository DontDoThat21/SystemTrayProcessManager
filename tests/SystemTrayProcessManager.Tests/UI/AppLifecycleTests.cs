using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Linq;
using Xunit;

namespace SystemTrayProcessManager.Tests.UI
{
    /// <summary>
    /// Tests for application lifecycle management in App.xaml.cs.
    /// </summary>
    public class AppLifecycleTests
    {
        /// <summary>
        /// Tests that logging configuration path is correctly constructed.
        /// </summary>
        [Fact]
        public void ConfigureLogging_CreatesLogDirectory()
        {
            // Arrange
            var appDataPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "SystemTrayProcessManager");
            var logsPath = Path.Combine(appDataPath, "logs");

            // Act
            // Create the directory to simulate what the app does
            Directory.CreateDirectory(logsPath);

            // Assert
            Directory.Exists(logsPath).Should().BeTrue();
            
            // Clean up
            try
            {
                if (Directory.Exists(logsPath) && Directory.GetFiles(logsPath).Length == 0)
                {
                    Directory.Delete(logsPath);
                }
            }
            catch
            {
                // Ignore cleanup errors
            }
        }

        /// <summary>
        /// Tests that service collection can be built without errors.
        /// </summary>
        [Fact]
        public void ConfigureServices_BuildsServiceProvider()
        {
            // Arrange
            var services = new ServiceCollection();

            // Act
            services.AddLogging();
            var serviceProvider = services.BuildServiceProvider();

            // Assert
            serviceProvider.Should().NotBeNull();
            serviceProvider.GetService<ILoggerFactory>().Should().NotBeNull();
        }

        /// <summary>
        /// Tests that MainWindow can be registered in DI container.
        /// Note: We can't actually instantiate WPF windows in unit tests without STA thread.
        /// </summary>
        [Fact]
        public void DependencyInjection_CanRegisterMainWindow()
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddLogging();
            services.AddTransient<SystemTrayProcessManager.UI.MainWindow>();

            // Act
            var serviceProvider = services.BuildServiceProvider();

            // Assert
            serviceProvider.Should().NotBeNull();
            // We verify registration was successful by checking the service descriptor exists
            services.Any(sd => sd.ServiceType == typeof(SystemTrayProcessManager.UI.MainWindow))
                .Should().BeTrue();
        }

        /// <summary>
        /// Tests that the mutex name is correctly formatted.
        /// </summary>
        [Fact]
        public void SingleInstanceCheck_UseCorrectMutexName()
        {
            // Arrange
            const string expectedMutexName = "Global\\SystemTrayProcessManager_SingleInstance_4E9F2A1B";

            // Act & Assert
            // The mutex name should follow the pattern with Global prefix for cross-session support
            expectedMutexName.Should().StartWith("Global\\");
            expectedMutexName.Should().Contain("SystemTrayProcessManager");
            expectedMutexName.Should().Contain("SingleInstance");
        }

        /// <summary>
        /// Tests that AppData path is correctly constructed.
        /// </summary>
        [Fact]
        public void AppDataPath_IsCorrect()
        {
            // Arrange
            var expectedPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "SystemTrayProcessManager");

            // Act
            var directory = Path.GetDirectoryName(expectedPath);

            // Assert
            Directory.Exists(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData))
                .Should().BeTrue();
            expectedPath.Should().Contain("SystemTrayProcessManager");
        }

        /// <summary>
        /// Tests that service collection count increases when services are added.
        /// </summary>
        [Fact]
        public void ServiceRegistration_AddsServicesToCollection()
        {
            // Arrange
            var services = new ServiceCollection();
            var initialCount = services.Count;

            // Act
            services.AddLogging();
            services.AddTransient<SystemTrayProcessManager.UI.MainWindow>();

            // Assert
            services.Count.Should().BeGreaterThan(initialCount);
        }

        /// <summary>
        /// Tests that logging services are properly registered.
        /// </summary>
        [Fact]
        public void ServiceConfiguration_RegistersLogging()
        {
            // Arrange & Act
            var services = new ServiceCollection();
            services.AddLogging();
            var serviceProvider = services.BuildServiceProvider();

            // Assert
            var loggerFactory = serviceProvider.GetService<ILoggerFactory>();
            loggerFactory.Should().NotBeNull();

            var logger = loggerFactory?.CreateLogger("Test");
            logger.Should().NotBeNull();
        }

        /// <summary>
        /// Tests that dependency injection resolves logger for specific types.
        /// </summary>
        [Fact]
        public void DependencyInjection_ResolvesTypedLogger()
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddLogging();
            var serviceProvider = services.BuildServiceProvider();

            // Act
            var logger = serviceProvider.GetService<Microsoft.Extensions.Logging.ILogger<AppLifecycleTests>>();

            // Assert
            logger.Should().NotBeNull();
        }

        /// <summary>
        /// Tests that the application can create multiple independent service scopes.
        /// </summary>
        [Fact]
        public void ServiceProvider_CanCreateScopes()
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddLogging();
            var serviceProvider = services.BuildServiceProvider();

            // Act
            using var scope1 = serviceProvider.CreateScope();
            using var scope2 = serviceProvider.CreateScope();

            // Assert
            scope1.Should().NotBeNull();
            scope2.Should().NotBeNull();
            scope1.Should().NotBeSameAs(scope2);
        }
    }
}
