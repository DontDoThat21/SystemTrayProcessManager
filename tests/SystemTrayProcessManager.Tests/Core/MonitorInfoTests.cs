using SystemTrayProcessManager.Core.Models;
using Xunit;

namespace SystemTrayProcessManager.Tests.Core
{
    /// <summary>
    /// Unit tests for the MonitorInfo model.
    /// </summary>
    public class MonitorInfoTests
    {
        [Fact]
        public void Create_WithValidParameters_ShouldSetAllProperties()
        {
            var info = MonitorInfo.Create(@"\\.\DISPLAY1", 0, 0, 1920, 1080, true);

            Assert.Equal(@"\\.\DISPLAY1", info.DeviceName);
            Assert.Equal(0, info.X);
            Assert.Equal(0, info.Y);
            Assert.Equal(1920, info.Width);
            Assert.Equal(1080, info.Height);
            Assert.True(info.IsPrimary);
        }

        [Fact]
        public void Create_WithNullDeviceName_ShouldUseEmptyString()
        {
            var info = MonitorInfo.Create(null!, 0, 0, 1920, 1080, false);

            Assert.Equal(string.Empty, info.DeviceName);
        }

        [Fact]
        public void Create_SecondaryMonitor_ShouldHaveCorrectOffset()
        {
            var info = MonitorInfo.Create(@"\\.\DISPLAY2", 1920, 0, 1920, 1080, false);

            Assert.Equal(1920, info.X);
            Assert.Equal(0, info.Y);
            Assert.False(info.IsPrimary);
        }

        [Fact]
        public void ToHashString_ShouldContainAllProperties()
        {
            var info = MonitorInfo.Create(@"\\.\DISPLAY1", 0, 0, 1920, 1080, true);
            var hash = info.ToHashString();

            Assert.Contains(@"\\.\DISPLAY1", hash);
            Assert.Contains("0", hash);
            Assert.Contains("1920", hash);
            Assert.Contains("1080", hash);
            Assert.Contains("True", hash);
        }

        [Fact]
        public void ToHashString_DifferentMonitors_ShouldProduceDifferentStrings()
        {
            var info1 = MonitorInfo.Create(@"\\.\DISPLAY1", 0, 0, 1920, 1080, true);
            var info2 = MonitorInfo.Create(@"\\.\DISPLAY2", 1920, 0, 1920, 1080, false);

            Assert.NotEqual(info1.ToHashString(), info2.ToHashString());
        }

        [Fact]
        public void DefaultConstructor_ShouldHaveDefaultValues()
        {
            var info = new MonitorInfo();

            Assert.Equal(string.Empty, info.DeviceName);
            Assert.Equal(0, info.X);
            Assert.Equal(0, info.Y);
            Assert.Equal(0, info.Width);
            Assert.Equal(0, info.Height);
            Assert.False(info.IsPrimary);
        }

        [Fact]
        public void Equality_SameValues_ShouldBeEqual()
        {
            var info1 = MonitorInfo.Create(@"\\.\DISPLAY1", 0, 0, 1920, 1080, true);
            var info2 = MonitorInfo.Create(@"\\.\DISPLAY1", 0, 0, 1920, 1080, true);

            Assert.Equal(info1, info2);
        }

        [Fact]
        public void Equality_DifferentPosition_ShouldNotBeEqual()
        {
            var info1 = MonitorInfo.Create(@"\\.\DISPLAY1", 0, 0, 1920, 1080, true);
            var info2 = MonitorInfo.Create(@"\\.\DISPLAY1", 100, 0, 1920, 1080, true);

            Assert.NotEqual(info1, info2);
        }

        [Fact]
        public void WithExpression_ShouldCreateModifiedCopy()
        {
            var original = MonitorInfo.Create(@"\\.\DISPLAY1", 0, 0, 1920, 1080, true);
            var modified = original with { Width = 2560, Height = 1440 };

            Assert.Equal(1920, original.Width);
            Assert.Equal(2560, modified.Width);
        }
    }
}
