using SystemTrayProcessManager.Core.Enums;
using SystemTrayProcessManager.Core.Models;

namespace SystemTrayProcessManager.Tests.Core
{
    public class ProcessPriorityConfigTests
    {
        [Fact]
        public void Create_WithValidParameters_ReturnsConfiguredInstance()
        {
            string processName = "notepad";
            ProcessPriority priority = ProcessPriority.High;
            bool autoApply = false;

            ProcessPriorityConfig config = ProcessPriorityConfig.Create(processName, priority, autoApply);

            Assert.NotEqual(Guid.Empty, config.Id);
            Assert.Equal(processName, config.ProcessName);
            Assert.Equal(priority, config.Priority);
            Assert.Equal(autoApply, config.AutoApply);
            Assert.True(config.CreatedAt <= DateTime.UtcNow);
            Assert.True(config.LastModified <= DateTime.UtcNow);
        }

        [Fact]
        public void Create_WithDefaults_HasAutoApplyTrue()
        {
            ProcessPriorityConfig config = ProcessPriorityConfig.Create("test", ProcessPriority.Normal);

            Assert.True(config.AutoApply);
        }

        [Fact]
        public void Create_WithNullProcessName_UsesEmptyString()
        {
            ProcessPriorityConfig config = ProcessPriorityConfig.Create(null!, ProcessPriority.Normal);

            Assert.Equal(string.Empty, config.ProcessName);
        }

        [Fact]
        public void WithPriority_UpdatesPriorityAndLastModified()
        {
            ProcessPriorityConfig original = ProcessPriorityConfig.Create("test", ProcessPriority.Normal);
            DateTime originalModified = original.LastModified;

            ProcessPriorityConfig updated = original.WithPriority(ProcessPriority.AboveNormal);

            Assert.Equal(ProcessPriority.AboveNormal, updated.Priority);
            Assert.True(updated.LastModified >= originalModified);
            Assert.Equal(original.Id, updated.Id);
            Assert.Equal(original.ProcessName, updated.ProcessName);
            Assert.Equal(original.AutoApply, updated.AutoApply);
        }

        [Fact]
        public void WithAutoApply_UpdatesAutoApplyAndLastModified()
        {
            ProcessPriorityConfig original = ProcessPriorityConfig.Create("test", ProcessPriority.High, autoApply: true);
            DateTime originalModified = original.LastModified;

            ProcessPriorityConfig updated = original.WithAutoApply(false);

            Assert.False(updated.AutoApply);
            Assert.True(updated.LastModified >= originalModified);
            Assert.Equal(original.Id, updated.Id);
            Assert.Equal(original.ProcessName, updated.ProcessName);
            Assert.Equal(original.Priority, updated.Priority);
        }

        [Theory]
        [InlineData("notepad", "notepad", true)]
        [InlineData("notepad", "NOTEPAD", true)]
        [InlineData("notepad", "Notepad", true)]
        [InlineData("notepad", "notepad.exe", false)]
        [InlineData("notepad", "chrome", false)]
        [InlineData("", "", true)]
        public void Matches_ComparesProcessNameCaseInsensitively(string configName, string processName, bool expected)
        {
            ProcessPriorityConfig config = ProcessPriorityConfig.Create(configName, ProcessPriority.Normal);

            bool result = config.Matches(processName);

            Assert.Equal(expected, result);
        }

        [Fact]
        public void RecordEquality_SameValues_AreEqual()
        {
            Guid id = Guid.NewGuid();
            DateTime timestamp = DateTime.UtcNow;

            ProcessPriorityConfig config1 = new()
            {
                Id = id,
                ProcessName = "test",
                Priority = ProcessPriority.High,
                AutoApply = true,
                CreatedAt = timestamp,
                LastModified = timestamp
            };

            ProcessPriorityConfig config2 = new()
            {
                Id = id,
                ProcessName = "test",
                Priority = ProcessPriority.High,
                AutoApply = true,
                CreatedAt = timestamp,
                LastModified = timestamp
            };

            Assert.Equal(config1, config2);
            Assert.True(config1 == config2);
        }

        [Fact]
        public void RecordEquality_DifferentPriority_AreNotEqual()
        {
            Guid id = Guid.NewGuid();

            ProcessPriorityConfig config1 = new() { Id = id, ProcessName = "test", Priority = ProcessPriority.High };
            ProcessPriorityConfig config2 = new() { Id = id, ProcessName = "test", Priority = ProcessPriority.Idle };

            Assert.NotEqual(config1, config2);
            Assert.False(config1 == config2);
        }
    }
}
