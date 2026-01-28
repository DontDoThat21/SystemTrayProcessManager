using SystemTrayProcessManager.Core.Enums;
using SystemTrayProcessManager.Core.Models;

namespace SystemTrayProcessManager.Tests.Core
{
    /// <summary>
    /// Unit tests for the StartupProcess model.
    /// </summary>
    public class StartupProcessTests
    {
        #region Constructor and Default Values

        [Fact]
        public void Constructor_ShouldSetDefaultId()
        {
            var process = new StartupProcess();
            Assert.NotEqual(Guid.Empty, process.Id);
        }

        [Fact]
        public void Constructor_ShouldSetEmptyName()
        {
            var process = new StartupProcess();
            Assert.Equal(string.Empty, process.Name);
        }

        [Fact]
        public void Constructor_ShouldSetEmptyExecutablePath()
        {
            var process = new StartupProcess();
            Assert.Equal(string.Empty, process.ExecutablePath);
        }

        [Fact]
        public void Constructor_ShouldSetEnabledToTrue()
        {
            var process = new StartupProcess();
            Assert.True(process.IsEnabled);
        }

        [Fact]
        public void Constructor_ShouldSetNormalWindowState()
        {
            var process = new StartupProcess();
            Assert.Equal(WindowState.Normal, process.LaunchWindowState);
        }

        [Fact]
        public void Constructor_ShouldSetZeroDelay()
        {
            var process = new StartupProcess();
            Assert.Equal(0, process.DelayMilliseconds);
        }

        #endregion

        #region IsValid Property

        [Fact]
        public void IsValid_WithNameAndPath_ShouldReturnTrue()
        {
            var process = new StartupProcess
            {
                Name = "Test Process",
                ExecutablePath = @"C:\Program Files\Test\test.exe"
            };
            
            Assert.True(process.IsValid);
        }

        [Fact]
        public void IsValid_EmptyName_ShouldReturnFalse()
        {
            var process = new StartupProcess
            {
                Name = "",
                ExecutablePath = @"C:\test.exe"
            };
            
            Assert.False(process.IsValid);
        }

        [Fact]
        public void IsValid_WhitespaceName_ShouldReturnFalse()
        {
            var process = new StartupProcess
            {
                Name = "   ",
                ExecutablePath = @"C:\test.exe"
            };
            
            Assert.False(process.IsValid);
        }

        [Fact]
        public void IsValid_EmptyPath_ShouldReturnFalse()
        {
            var process = new StartupProcess
            {
                Name = "Test",
                ExecutablePath = ""
            };
            
            Assert.False(process.IsValid);
        }

        #endregion

        #region ExecutableExists Property

        [Fact]
        public void ExecutableExists_EmptyPath_ShouldReturnFalse()
        {
            var process = new StartupProcess { ExecutablePath = "" };
            
            Assert.False(process.ExecutableExists);
        }

        [Fact]
        public void ExecutableExists_NonExistentPath_ShouldReturnFalse()
        {
            var process = new StartupProcess 
            { 
                ExecutablePath = @"C:\NonExistent\Path\app.exe" 
            };
            
            Assert.False(process.ExecutableExists);
        }

        [Fact]
        public void ExecutableExists_ExistingFile_ShouldReturnTrue()
        {
            // Using notepad as it exists on all Windows systems
            var process = new StartupProcess 
            { 
                ExecutablePath = @"C:\Windows\notepad.exe" 
            };
            
            Assert.True(process.ExecutableExists);
        }

        #endregion

        #region DisplayName Property

        [Fact]
        public void DisplayName_WithName_ShouldReturnName()
        {
            var process = new StartupProcess
            {
                Name = "My Application",
                ExecutablePath = @"C:\app.exe"
            };
            
            Assert.Equal("My Application", process.DisplayName);
        }

        [Fact]
        public void DisplayName_EmptyName_ShouldReturnFileNameWithoutExtension()
        {
            var process = new StartupProcess
            {
                Name = "",
                ExecutablePath = @"C:\Program Files\MyApp\application.exe"
            };
            
            Assert.Equal("application", process.DisplayName);
        }

        [Fact]
        public void DisplayName_WhitespaceName_ShouldReturnFileNameWithoutExtension()
        {
            var process = new StartupProcess
            {
                Name = "   ",
                ExecutablePath = @"C:\test.exe"
            };
            
            Assert.Equal("test", process.DisplayName);
        }

        [Fact]
        public void DisplayName_NoNameNoPath_ShouldReturnUnknown()
        {
            var process = new StartupProcess
            {
                Name = "",
                ExecutablePath = ""
            };
            
            Assert.Equal("Unknown", process.DisplayName);
        }

        #endregion

        #region WithEnabled Method

        [Fact]
        public void WithEnabled_True_ShouldSetIsEnabled()
        {
            var process = new StartupProcess { IsEnabled = false };
            
            var updated = process.WithEnabled(true);
            
            Assert.True(updated.IsEnabled);
        }

        [Fact]
        public void WithEnabled_False_ShouldClearIsEnabled()
        {
            var process = new StartupProcess { IsEnabled = true };
            
            var updated = process.WithEnabled(false);
            
            Assert.False(updated.IsEnabled);
        }

        [Fact]
        public void WithEnabled_ShouldPreserveId()
        {
            var id = Guid.NewGuid();
            var process = new StartupProcess { Id = id };
            
            var updated = process.WithEnabled(true);
            
            Assert.Equal(id, updated.Id);
        }

        [Fact]
        public void WithEnabled_ShouldPreservePath()
        {
            var process = new StartupProcess 
            { 
                ExecutablePath = @"C:\test.exe" 
            };
            
            var updated = process.WithEnabled(false);
            
            Assert.Equal(@"C:\test.exe", updated.ExecutablePath);
        }

        [Fact]
        public void WithEnabled_ShouldPreserveArguments()
        {
            var process = new StartupProcess { Arguments = "--silent" };
            
            var updated = process.WithEnabled(true);
            
            Assert.Equal("--silent", updated.Arguments);
        }

        #endregion

        #region Equality

        [Fact]
        public void Equals_SameId_ShouldReturnTrue()
        {
            var id = Guid.NewGuid();
            var process1 = new StartupProcess { Id = id };
            var process2 = new StartupProcess { Id = id };
            
            Assert.Equal(process1, process2);
        }

        [Fact]
        public void Equals_DifferentId_ShouldReturnFalse()
        {
            var process1 = new StartupProcess();
            var process2 = new StartupProcess();
            
            Assert.NotEqual(process1, process2);
        }

        [Fact]
        public void Equals_Null_ShouldReturnFalse()
        {
            var process = new StartupProcess();
            
            Assert.False(process.Equals(null));
        }

        [Fact]
        public void GetHashCode_SameId_ShouldBeSame()
        {
            var id = Guid.NewGuid();
            var process1 = new StartupProcess { Id = id };
            var process2 = new StartupProcess { Id = id };
            
            Assert.Equal(process1.GetHashCode(), process2.GetHashCode());
        }

        [Fact]
        public void OperatorEquals_SameId_ShouldReturnTrue()
        {
            var id = Guid.NewGuid();
            var process1 = new StartupProcess { Id = id };
            var process2 = new StartupProcess { Id = id };
            
            Assert.True(process1 == process2);
        }

        [Fact]
        public void OperatorNotEquals_DifferentId_ShouldReturnTrue()
        {
            var process1 = new StartupProcess();
            var process2 = new StartupProcess();
            
            Assert.True(process1 != process2);
        }

        #endregion

        #region ToString

        [Fact]
        public void ToString_ShouldContainDisplayName()
        {
            var process = new StartupProcess { Name = "TestProcess" };
            
            var result = process.ToString();
            
            Assert.Contains("TestProcess", result);
        }

        [Fact]
        public void ToString_ShouldContainPath()
        {
            var process = new StartupProcess 
            { 
                ExecutablePath = @"C:\app.exe" 
            };
            
            var result = process.ToString();
            
            Assert.Contains(@"C:\app.exe", result);
        }

        #endregion
    }
}
