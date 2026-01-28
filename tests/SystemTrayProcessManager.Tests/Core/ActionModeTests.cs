using SystemTrayProcessManager.Core.Enums;

namespace SystemTrayProcessManager.Tests.Core
{
    /// <summary>
    /// Unit tests for the <see cref="ActionMode"/> enum.
    /// </summary>
    public class ActionModeTests
    {
        [Fact]
        public void ActionMode_QuickAction_ShouldHaveZeroValue()
        {
            // Assert
            Assert.Equal(0, (int)ActionMode.QuickAction);
        }

        [Fact]
        public void ActionMode_PinnedProcess_ShouldHaveValueOne()
        {
            // Assert
            Assert.Equal(1, (int)ActionMode.PinnedProcess);
        }

        [Fact]
        public void ActionMode_ShouldHaveExactlyTwoValues()
        {
            // Arrange
            var values = Enum.GetValues<ActionMode>();

            // Assert
            Assert.Equal(2, values.Length);
        }

        [Fact]
        public void ActionMode_QuickAction_ShouldBeDefault()
        {
            // Arrange
            ActionMode defaultMode = default;

            // Assert
            Assert.Equal(ActionMode.QuickAction, defaultMode);
        }

        [Theory]
        [InlineData(ActionMode.QuickAction, "QuickAction")]
        [InlineData(ActionMode.PinnedProcess, "PinnedProcess")]
        public void ActionMode_ToString_ReturnsExpectedName(ActionMode mode, string expectedName)
        {
            // Assert
            Assert.Equal(expectedName, mode.ToString());
        }

        [Theory]
        [InlineData("QuickAction", ActionMode.QuickAction)]
        [InlineData("PinnedProcess", ActionMode.PinnedProcess)]
        public void ActionMode_Parse_ParsesValidStrings(string name, ActionMode expected)
        {
            // Act
            var result = Enum.Parse<ActionMode>(name);

            // Assert
            Assert.Equal(expected, result);
        }

        [Theory]
        [InlineData("quickaction")]
        [InlineData("QUICKACTION")]
        [InlineData("QuickAction")]
        public void ActionMode_ParseIgnoreCase_ParsesDifferentCases(string name)
        {
            // Act
            var result = Enum.Parse<ActionMode>(name, ignoreCase: true);

            // Assert
            Assert.Equal(ActionMode.QuickAction, result);
        }

        [Fact]
        public void ActionMode_IsDefined_ReturnsTrueForValidValues()
        {
            // Assert
            Assert.True(Enum.IsDefined(ActionMode.QuickAction));
            Assert.True(Enum.IsDefined(ActionMode.PinnedProcess));
        }

        [Fact]
        public void ActionMode_IsDefined_ReturnsFalseForInvalidValues()
        {
            // Assert
            Assert.False(Enum.IsDefined((ActionMode)99));
            Assert.False(Enum.IsDefined((ActionMode)(-1)));
        }

        [Fact]
        public void ActionMode_GetNames_ReturnsAllNames()
        {
            // Arrange
            var names = Enum.GetNames<ActionMode>();

            // Assert
            Assert.Equal(2, names.Length);
            Assert.Contains("QuickAction", names);
            Assert.Contains("PinnedProcess", names);
        }
    }
}
