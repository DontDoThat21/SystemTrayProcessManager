using FluentAssertions;
using SystemTrayProcessManager.Core.Enums;

namespace SystemTrayProcessManager.Tests.Core
{
    /// <summary>
    /// Unit tests for <see cref="ErrorSeverity"/> enum.
    /// </summary>
    public class ErrorSeverityTests
    {
        [Fact]
        public void None_ShouldHaveValue0()
        {
            ((int)ErrorSeverity.None).Should().Be(0);
        }

        [Fact]
        public void Low_ShouldHaveValue1()
        {
            ((int)ErrorSeverity.Low).Should().Be(1);
        }

        [Fact]
        public void Medium_ShouldHaveValue2()
        {
            ((int)ErrorSeverity.Medium).Should().Be(2);
        }

        [Fact]
        public void High_ShouldHaveValue3()
        {
            ((int)ErrorSeverity.High).Should().Be(3);
        }

        [Fact]
        public void Critical_ShouldHaveValue4()
        {
            ((int)ErrorSeverity.Critical).Should().Be(4);
        }

        [Fact]
        public void Enum_ShouldHave5Values()
        {
            Enum.GetValues<ErrorSeverity>().Should().HaveCount(5);
        }

        [Theory]
        [InlineData(ErrorSeverity.None, "None")]
        [InlineData(ErrorSeverity.Low, "Low")]
        [InlineData(ErrorSeverity.Medium, "Medium")]
        [InlineData(ErrorSeverity.High, "High")]
        [InlineData(ErrorSeverity.Critical, "Critical")]
        public void ToString_ShouldReturnExpectedName(ErrorSeverity severity, string expected)
        {
            severity.ToString().Should().Be(expected);
        }

        [Fact]
        public void SeverityLevels_ShouldBeOrdered()
        {
            ((int)ErrorSeverity.None).Should().BeLessThan((int)ErrorSeverity.Low);
            ((int)ErrorSeverity.Low).Should().BeLessThan((int)ErrorSeverity.Medium);
            ((int)ErrorSeverity.Medium).Should().BeLessThan((int)ErrorSeverity.High);
            ((int)ErrorSeverity.High).Should().BeLessThan((int)ErrorSeverity.Critical);
        }
    }
}
