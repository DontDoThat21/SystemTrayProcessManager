using SystemTrayProcessManager.Core.Enums;

namespace SystemTrayProcessManager.Tests.Core
{
    /// <summary>
    /// Unit tests for the <see cref="ProcessActionType"/> enum.
    /// </summary>
    public class ProcessActionTypeTests
    {
        [Fact]
        public void ProcessActionType_None_ShouldHaveZeroValue()
        {
            // Assert
            Assert.Equal(0, (int)ProcessActionType.None);
        }

        [Fact]
        public void ProcessActionType_Mute_ShouldHaveCorrectValue()
        {
            // Assert
            Assert.Equal(1, (int)ProcessActionType.Mute);
        }

        [Fact]
        public void ProcessActionType_Unmute_ShouldHaveCorrectValue()
        {
            // Assert
            Assert.Equal(2, (int)ProcessActionType.Unmute);
        }

        [Fact]
        public void ProcessActionType_ToggleMute_ShouldHaveCorrectValue()
        {
            // Assert
            Assert.Equal(4, (int)ProcessActionType.ToggleMute);
        }

        [Fact]
        public void ProcessActionType_Close_ShouldHaveCorrectValue()
        {
            // Assert
            Assert.Equal(8, (int)ProcessActionType.Close);
        }

        [Fact]
        public void ProcessActionType_Minimize_ShouldHaveCorrectValue()
        {
            // Assert
            Assert.Equal(16, (int)ProcessActionType.Minimize);
        }

        [Fact]
        public void ProcessActionType_Maximize_ShouldHaveCorrectValue()
        {
            // Assert
            Assert.Equal(32, (int)ProcessActionType.Maximize);
        }

        [Fact]
        public void ProcessActionType_Restore_ShouldHaveCorrectValue()
        {
            // Assert
            Assert.Equal(64, (int)ProcessActionType.Restore);
        }

        [Fact]
        public void ProcessActionType_BringToFront_ShouldHaveCorrectValue()
        {
            // Assert
            Assert.Equal(128, (int)ProcessActionType.BringToFront);
        }

        [Fact]
        public void ProcessActionType_Hide_ShouldHaveCorrectValue()
        {
            // Assert
            Assert.Equal(256, (int)ProcessActionType.Hide);
        }

        [Fact]
        public void ProcessActionType_Show_ShouldHaveCorrectValue()
        {
            // Assert
            Assert.Equal(512, (int)ProcessActionType.Show);
        }

        [Fact]
        public void ProcessActionType_ShouldHaveFlagsAttribute()
        {
            // Assert
            var flagsAttribute = typeof(ProcessActionType).GetCustomAttributes(typeof(FlagsAttribute), false);
            Assert.Single(flagsAttribute);
        }

        [Fact]
        public void ProcessActionType_CanCombineAudioFlags()
        {
            // Arrange
            var combined = ProcessActionType.Mute | ProcessActionType.Unmute | ProcessActionType.ToggleMute;

            // Assert
            Assert.True(combined.HasFlag(ProcessActionType.Mute));
            Assert.True(combined.HasFlag(ProcessActionType.Unmute));
            Assert.True(combined.HasFlag(ProcessActionType.ToggleMute));
        }

        [Fact]
        public void ProcessActionType_CanCombineWindowFlags()
        {
            // Arrange
            var combined = ProcessActionType.Minimize | ProcessActionType.Maximize | ProcessActionType.Close;

            // Assert
            Assert.True(combined.HasFlag(ProcessActionType.Minimize));
            Assert.True(combined.HasFlag(ProcessActionType.Maximize));
            Assert.True(combined.HasFlag(ProcessActionType.Close));
        }

        [Fact]
        public void ProcessActionType_ValuesAreUnique()
        {
            // Arrange
            var values = Enum.GetValues<ProcessActionType>()
                .Where(v => v != ProcessActionType.None)
                .ToArray();

            // Assert - all values should be distinct powers of 2
            var distinctValues = values.Select(v => (int)v).Distinct().ToArray();
            Assert.Equal(values.Length, distinctValues.Length);
        }

        [Fact]
        public void ProcessActionType_ValuesArePowersOfTwo()
        {
            // Arrange
            var values = Enum.GetValues<ProcessActionType>()
                .Where(v => v != ProcessActionType.None)
                .Select(v => (int)v);

            // Assert - all values should be powers of 2
            foreach (var value in values)
            {
                Assert.True(IsPowerOfTwo(value), $"Value {value} is not a power of 2");
            }
        }

        [Theory]
        [InlineData(ProcessActionType.Mute, "Mute")]
        [InlineData(ProcessActionType.Unmute, "Unmute")]
        [InlineData(ProcessActionType.ToggleMute, "ToggleMute")]
        [InlineData(ProcessActionType.Close, "Close")]
        [InlineData(ProcessActionType.Minimize, "Minimize")]
        [InlineData(ProcessActionType.Maximize, "Maximize")]
        [InlineData(ProcessActionType.Restore, "Restore")]
        [InlineData(ProcessActionType.BringToFront, "BringToFront")]
        [InlineData(ProcessActionType.Hide, "Hide")]
        [InlineData(ProcessActionType.Show, "Show")]
        public void ProcessActionType_ToString_ReturnsExpectedName(ProcessActionType action, string expectedName)
        {
            // Assert
            Assert.Equal(expectedName, action.ToString());
        }

        [Theory]
        [InlineData("Mute", ProcessActionType.Mute)]
        [InlineData("Unmute", ProcessActionType.Unmute)]
        [InlineData("ToggleMute", ProcessActionType.ToggleMute)]
        [InlineData("Close", ProcessActionType.Close)]
        [InlineData("Minimize", ProcessActionType.Minimize)]
        [InlineData("Maximize", ProcessActionType.Maximize)]
        [InlineData("Restore", ProcessActionType.Restore)]
        [InlineData("BringToFront", ProcessActionType.BringToFront)]
        [InlineData("Hide", ProcessActionType.Hide)]
        [InlineData("Show", ProcessActionType.Show)]
        public void ProcessActionType_Parse_ParsesValidStrings(string name, ProcessActionType expected)
        {
            // Act
            var result = Enum.Parse<ProcessActionType>(name);

            // Assert
            Assert.Equal(expected, result);
        }

        [Theory]
        [InlineData("mute")]
        [InlineData("MUTE")]
        [InlineData("Mute")]
        public void ProcessActionType_ParseIgnoreCase_ParsesDifferentCases(string name)
        {
            // Act
            var result = Enum.Parse<ProcessActionType>(name, ignoreCase: true);

            // Assert
            Assert.Equal(ProcessActionType.Mute, result);
        }

        private static bool IsPowerOfTwo(int value)
        {
            return value > 0 && (value & (value - 1)) == 0;
        }
    }
}
