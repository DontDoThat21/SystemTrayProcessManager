using SystemTrayProcessManager.Core.Enums;

namespace SystemTrayProcessManager.Tests.Core
{
    /// <summary>
    /// Unit tests for the WindowState enumeration.
    /// </summary>
    public class WindowStateTests
    {
        [Fact]
        public void WindowState_Normal_HasExpectedValue()
        {
            // Assert
            Assert.Equal(0, (int)WindowState.Normal);
        }

        [Fact]
        public void WindowState_Minimized_HasExpectedValue()
        {
            // Assert
            Assert.Equal(1, (int)WindowState.Minimized);
        }

        [Fact]
        public void WindowState_Maximized_HasExpectedValue()
        {
            // Assert
            Assert.Equal(2, (int)WindowState.Maximized);
        }

        [Fact]
        public void WindowState_Hidden_HasExpectedValue()
        {
            // Assert
            Assert.Equal(3, (int)WindowState.Hidden);
        }

        [Fact]
        public void WindowState_Invalid_HasExpectedValue()
        {
            // Assert
            Assert.Equal(-1, (int)WindowState.Invalid);
        }

        [Theory]
        [InlineData(WindowState.Normal)]
        [InlineData(WindowState.Minimized)]
        [InlineData(WindowState.Maximized)]
        [InlineData(WindowState.Hidden)]
        [InlineData(WindowState.Invalid)]
        public void WindowState_AllValues_CanBeCastToInt(WindowState state)
        {
            // Act
            int value = (int)state;

            // Assert
            Assert.True(value >= -1 && value <= 3, $"WindowState value {value} out of expected range");
        }

        [Fact]
        public void WindowState_AllValues_AreUnique()
        {
            // Arrange
            var values = Enum.GetValues<WindowState>();

            // Act
            var uniqueValues = values.Select(v => (int)v).Distinct().ToList();

            // Assert
            Assert.Equal(values.Length, uniqueValues.Count);
        }

        [Fact]
        public void WindowState_ToString_ReturnsExpectedNames()
        {
            // Assert
            Assert.Equal("Normal", WindowState.Normal.ToString());
            Assert.Equal("Minimized", WindowState.Minimized.ToString());
            Assert.Equal("Maximized", WindowState.Maximized.ToString());
            Assert.Equal("Hidden", WindowState.Hidden.ToString());
            Assert.Equal("Invalid", WindowState.Invalid.ToString());
        }

        [Theory]
        [InlineData("Normal", WindowState.Normal)]
        [InlineData("Minimized", WindowState.Minimized)]
        [InlineData("Maximized", WindowState.Maximized)]
        [InlineData("Hidden", WindowState.Hidden)]
        [InlineData("Invalid", WindowState.Invalid)]
        public void WindowState_Parse_ReturnsCorrectValue(string name, WindowState expected)
        {
            // Act
            var result = Enum.Parse<WindowState>(name);

            // Assert
            Assert.Equal(expected, result);
        }

        [Theory]
        [InlineData(0, WindowState.Normal)]
        [InlineData(1, WindowState.Minimized)]
        [InlineData(2, WindowState.Maximized)]
        [InlineData(3, WindowState.Hidden)]
        [InlineData(-1, WindowState.Invalid)]
        public void WindowState_CastFromInt_ReturnsCorrectValue(int value, WindowState expected)
        {
            // Act
            var result = (WindowState)value;

            // Assert
            Assert.Equal(expected, result);
        }

        [Fact]
        public void WindowState_IsDefined_ReturnsTrue_ForValidValues()
        {
            // Assert
            Assert.True(Enum.IsDefined(WindowState.Normal));
            Assert.True(Enum.IsDefined(WindowState.Minimized));
            Assert.True(Enum.IsDefined(WindowState.Maximized));
            Assert.True(Enum.IsDefined(WindowState.Hidden));
            Assert.True(Enum.IsDefined(WindowState.Invalid));
        }

        [Theory]
        [InlineData(-2)]
        [InlineData(4)]
        [InlineData(100)]
        public void WindowState_IsDefined_ReturnsFalse_ForInvalidValues(int invalidValue)
        {
            // Act
            var state = (WindowState)invalidValue;

            // Assert
            Assert.False(Enum.IsDefined(state));
        }
    }
}
