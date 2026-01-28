using SystemTrayProcessManager.Core.Enums;
using Xunit;

namespace SystemTrayProcessManager.Tests.Core
{
    /// <summary>
    /// Unit tests for the ProcessPriority enum.
    /// </summary>
    public class ProcessPriorityTests
    {
        [Theory]
        [InlineData(ProcessPriority.Idle, 64)]
        [InlineData(ProcessPriority.BelowNormal, 16384)]
        [InlineData(ProcessPriority.Normal, 32)]
        [InlineData(ProcessPriority.AboveNormal, 32768)]
        [InlineData(ProcessPriority.High, 128)]
        [InlineData(ProcessPriority.RealTime, 256)]
        public void EnumValues_ShouldMatchWindowsApiConstants(ProcessPriority priority, int expectedValue)
        {
            Assert.Equal(expectedValue, (int)priority);
        }

        [Fact]
        public void EnumValues_ShouldHaveCorrectCount()
        {
            var values = Enum.GetValues<ProcessPriority>();
            Assert.Equal(6, values.Length);
        }

        [Fact]
        public void ToString_ShouldReturnEnumName()
        {
            Assert.Equal("Normal", ProcessPriority.Normal.ToString());
            Assert.Equal("High", ProcessPriority.High.ToString());
            Assert.Equal("RealTime", ProcessPriority.RealTime.ToString());
        }

        [Fact]
        public void Parse_ShouldReturnCorrectValue()
        {
            Assert.Equal(ProcessPriority.High, Enum.Parse<ProcessPriority>("High"));
            Assert.Equal(ProcessPriority.Idle, Enum.Parse<ProcessPriority>("Idle"));
        }

        [Theory]
        [InlineData(ProcessPriority.Idle)]
        [InlineData(ProcessPriority.BelowNormal)]
        [InlineData(ProcessPriority.Normal)]
        [InlineData(ProcessPriority.AboveNormal)]
        [InlineData(ProcessPriority.High)]
        [InlineData(ProcessPriority.RealTime)]
        public void IsDefined_AllValues_ShouldReturnTrue(ProcessPriority priority)
        {
            Assert.True(Enum.IsDefined(priority));
        }

        [Fact]
        public void CastFromInt_ValidValue_ShouldSucceed()
        {
            var priority = (ProcessPriority)32;
            Assert.Equal(ProcessPriority.Normal, priority);
        }

        [Fact]
        public void CastToInt_ShouldReturnCorrectValue()
        {
            Assert.Equal(128, (int)ProcessPriority.High);
        }
    }
}
