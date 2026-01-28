using SystemTrayProcessManager.Core.Enums;
using SystemTrayProcessManager.Core.Models;

namespace SystemTrayProcessManager.Tests.Core
{
    /// <summary>
    /// Unit tests for the HotkeyConfigItem model.
    /// </summary>
    public class HotkeyConfigItemTests
    {
        #region Constructor Tests

        [Fact]
        public void DefaultConstructor_GeneratesNewGuid()
        {
            // Arrange & Act
            var item = new HotkeyConfigItem();

            // Assert
            Assert.NotEqual(Guid.Empty, item.Id);
        }

        [Fact]
        public void DefaultConstructor_SetsDefaultValues()
        {
            // Arrange & Act
            var item = new HotkeyConfigItem();

            // Assert
            Assert.Equal(string.Empty, item.Name);
            Assert.Equal(string.Empty, item.Description);
            Assert.Equal(0, item.VirtualKeyCode);
            Assert.Equal(HotkeyModifier.None, item.Modifiers);
            Assert.Equal(string.Empty, item.ActionType);
            Assert.Null(item.TargetProcessName);
            Assert.True(item.IsEnabled);
        }

        [Fact]
        public void ParameterizedConstructor_SetsValues()
        {
            // Arrange & Act
            var item = new HotkeyConfigItem("Test Hotkey", 0x4D, HotkeyModifier.Ctrl | HotkeyModifier.Alt, "ToggleMute");

            // Assert
            Assert.Equal("Test Hotkey", item.Name);
            Assert.Equal(0x4D, item.VirtualKeyCode);
            Assert.Equal(HotkeyModifier.Ctrl | HotkeyModifier.Alt, item.Modifiers);
            Assert.Equal("ToggleMute", item.ActionType);
        }

        [Fact]
        public void ParameterizedConstructor_HandlesNullName()
        {
            // Arrange & Act
            var item = new HotkeyConfigItem(null!, 0x4D, HotkeyModifier.Ctrl, "ToggleMute");

            // Assert
            Assert.Equal(string.Empty, item.Name);
        }

        [Fact]
        public void ParameterizedConstructor_HandlesNullActionType()
        {
            // Arrange & Act
            var item = new HotkeyConfigItem("Test", 0x4D, HotkeyModifier.Ctrl, null!);

            // Assert
            Assert.Equal(string.Empty, item.ActionType);
        }

        #endregion

        #region Property Tests

        [Fact]
        public void Name_SetValue_UpdatesProperty()
        {
            // Arrange
            var item = new HotkeyConfigItem();

            // Act
            item.Name = "New Name";

            // Assert
            Assert.Equal("New Name", item.Name);
        }

        [Fact]
        public void Name_SetNull_CoercesToEmpty()
        {
            // Arrange
            var item = new HotkeyConfigItem();

            // Act
            item.Name = null!;

            // Assert
            Assert.Equal(string.Empty, item.Name);
        }

        [Fact]
        public void VirtualKeyCode_SetValue_UpdatesDisplayString()
        {
            // Arrange
            var item = new HotkeyConfigItem();

            // Act
            item.VirtualKeyCode = 0x4D; // M key

            // Assert
            Assert.Equal("M", item.KeyName);
            Assert.Contains("M", item.DisplayString);
        }

        [Fact]
        public void Modifiers_SetValue_UpdatesModifierFlags()
        {
            // Arrange
            var item = new HotkeyConfigItem();

            // Act
            item.Modifiers = HotkeyModifier.Ctrl | HotkeyModifier.Alt;

            // Assert
            Assert.True(item.HasCtrl);
            Assert.True(item.HasAlt);
            Assert.False(item.HasShift);
            Assert.False(item.HasWin);
        }

        [Fact]
        public void ActionType_SetNull_CoercesToEmpty()
        {
            // Arrange
            var item = new HotkeyConfigItem();

            // Act
            item.ActionType = null!;

            // Assert
            Assert.Equal(string.Empty, item.ActionType);
        }

        #endregion

        #region DisplayString Tests

        [Fact]
        public void DisplayString_NoKey_ReturnsNotSet()
        {
            // Arrange
            var item = new HotkeyConfigItem();

            // Assert
            Assert.Equal("(Not set)", item.DisplayString);
        }

        [Fact]
        public void DisplayString_WithModifiersAndKey_ReturnsFormattedString()
        {
            // Arrange
            var item = new HotkeyConfigItem
            {
                VirtualKeyCode = 0x4D, // M
                Modifiers = HotkeyModifier.Ctrl | HotkeyModifier.Alt
            };

            // Assert
            Assert.Equal("Ctrl+Alt+M", item.DisplayString);
        }

        [Fact]
        public void DisplayString_AllModifiers_ShowsAllInOrder()
        {
            // Arrange
            var item = new HotkeyConfigItem
            {
                VirtualKeyCode = 0x70, // F1
                Modifiers = HotkeyModifier.Ctrl | HotkeyModifier.Alt | HotkeyModifier.Shift | HotkeyModifier.Win
            };

            // Assert
            Assert.Equal("Ctrl+Alt+Shift+Win+F1", item.DisplayString);
        }

        #endregion

        #region KeyName Tests

        [Fact]
        public void KeyName_FunctionKey_ReturnsCorrectName()
        {
            // Arrange
            var item = new HotkeyConfigItem { VirtualKeyCode = 0x70 };

            // Assert
            Assert.Equal("F1", item.KeyName);
        }

        [Fact]
        public void KeyName_LetterKey_ReturnsLetter()
        {
            // Arrange
            var item = new HotkeyConfigItem { VirtualKeyCode = 0x41 }; // A

            // Assert
            Assert.Equal("A", item.KeyName);
        }

        [Fact]
        public void KeyName_NumberKey_ReturnsNumber()
        {
            // Arrange
            var item = new HotkeyConfigItem { VirtualKeyCode = 0x35 }; // 5

            // Assert
            Assert.Equal("5", item.KeyName);
        }

        [Fact]
        public void KeyName_NumpadKey_ReturnsNumpadName()
        {
            // Arrange
            var item = new HotkeyConfigItem { VirtualKeyCode = 0x63 }; // Numpad 3

            // Assert
            Assert.Equal("Num3", item.KeyName);
        }

        [Fact]
        public void KeyName_SpecialKey_ReturnsSpecialName()
        {
            // Arrange
            var item = new HotkeyConfigItem { VirtualKeyCode = 0x20 }; // Space

            // Assert
            Assert.Equal("Space", item.KeyName);
        }

        [Fact]
        public void KeyName_ZeroVkCode_ReturnsEmpty()
        {
            // Arrange
            var item = new HotkeyConfigItem { VirtualKeyCode = 0 };

            // Assert
            Assert.Equal(string.Empty, item.KeyName);
        }

        #endregion

        #region IsValid Tests

        [Fact]
        public void IsValid_AllFieldsSet_ReturnsTrue()
        {
            // Arrange
            var item = new HotkeyConfigItem
            {
                Name = "Test",
                VirtualKeyCode = 0x4D,
                ActionType = "ToggleMute"
            };

            // Assert
            Assert.True(item.IsValid);
        }

        [Fact]
        public void IsValid_MissingName_ReturnsFalse()
        {
            // Arrange
            var item = new HotkeyConfigItem
            {
                VirtualKeyCode = 0x4D,
                ActionType = "ToggleMute"
            };

            // Assert
            Assert.False(item.IsValid);
        }

        [Fact]
        public void IsValid_MissingVirtualKeyCode_ReturnsFalse()
        {
            // Arrange
            var item = new HotkeyConfigItem
            {
                Name = "Test",
                ActionType = "ToggleMute"
            };

            // Assert
            Assert.False(item.IsValid);
        }

        [Fact]
        public void IsValid_MissingActionType_ReturnsFalse()
        {
            // Arrange
            var item = new HotkeyConfigItem
            {
                Name = "Test",
                VirtualKeyCode = 0x4D
            };

            // Assert
            Assert.False(item.IsValid);
        }

        #endregion

        #region ToHotkeyBinding Tests

        [Fact]
        public void ToHotkeyBinding_ReturnsCorrectBinding()
        {
            // Arrange
            var item = new HotkeyConfigItem
            {
                VirtualKeyCode = 0x4D,
                Modifiers = HotkeyModifier.Ctrl | HotkeyModifier.Alt
            };

            // Act
            var binding = item.ToHotkeyBinding();

            // Assert
            Assert.Equal(0x4D, binding.VirtualKeyCode);
            Assert.Equal(HotkeyModifier.Ctrl | HotkeyModifier.Alt, binding.Modifiers);
        }

        #endregion

        #region FromHotkeyBinding Tests

        [Fact]
        public void FromHotkeyBinding_CreatesConfigItem()
        {
            // Arrange
            var binding = new HotkeyBinding(0x4D, HotkeyModifier.Ctrl);

            // Act
            var item = HotkeyConfigItem.FromHotkeyBinding(binding, "Test", "ToggleMute");

            // Assert
            Assert.Equal("Test", item.Name);
            Assert.Equal(0x4D, item.VirtualKeyCode);
            Assert.Equal(HotkeyModifier.Ctrl, item.Modifiers);
            Assert.Equal("ToggleMute", item.ActionType);
        }

        #endregion

        #region Clone Tests

        [Fact]
        public void Clone_CreatesDeepCopy()
        {
            // Arrange
            var original = new HotkeyConfigItem
            {
                Name = "Original",
                Description = "Test description",
                VirtualKeyCode = 0x4D,
                Modifiers = HotkeyModifier.Ctrl,
                ActionType = "ToggleMute",
                TargetProcessName = "notepad.exe",
                IsEnabled = true
            };

            // Act
            var clone = original.Clone();

            // Assert
            Assert.Equal(original.Id, clone.Id);
            Assert.Equal(original.Name, clone.Name);
            Assert.Equal(original.Description, clone.Description);
            Assert.Equal(original.VirtualKeyCode, clone.VirtualKeyCode);
            Assert.Equal(original.Modifiers, clone.Modifiers);
            Assert.Equal(original.ActionType, clone.ActionType);
            Assert.Equal(original.TargetProcessName, clone.TargetProcessName);
            Assert.Equal(original.IsEnabled, clone.IsEnabled);
        }

        [Fact]
        public void Clone_ModifyingClone_DoesNotAffectOriginal()
        {
            // Arrange
            var original = new HotkeyConfigItem { Name = "Original" };
            var clone = original.Clone();

            // Act
            clone.Name = "Modified";

            // Assert
            Assert.Equal("Original", original.Name);
            Assert.Equal("Modified", clone.Name);
        }

        #endregion

        #region UpdateFrom Tests

        [Fact]
        public void UpdateFrom_CopiesAllPropertiesExceptId()
        {
            // Arrange
            var target = new HotkeyConfigItem { Name = "Original" };
            var originalId = target.Id;

            var source = new HotkeyConfigItem
            {
                Name = "Updated",
                Description = "New description",
                VirtualKeyCode = 0x4D,
                Modifiers = HotkeyModifier.Alt,
                ActionType = "Minimize",
                TargetProcessName = "chrome.exe",
                IsEnabled = false
            };

            // Act
            target.UpdateFrom(source);

            // Assert
            Assert.Equal(originalId, target.Id); // Id should NOT change
            Assert.Equal("Updated", target.Name);
            Assert.Equal("New description", target.Description);
            Assert.Equal(0x4D, target.VirtualKeyCode);
            Assert.Equal(HotkeyModifier.Alt, target.Modifiers);
            Assert.Equal("Minimize", target.ActionType);
            Assert.Equal("chrome.exe", target.TargetProcessName);
            Assert.False(target.IsEnabled);
        }

        [Fact]
        public void UpdateFrom_NullSource_ThrowsArgumentNullException()
        {
            // Arrange
            var item = new HotkeyConfigItem();

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => item.UpdateFrom(null!));
        }

        #endregion

        #region ConflictsWith Tests

        [Fact]
        public void ConflictsWith_SameKeyAndModifiers_ReturnsTrue()
        {
            // Arrange
            var item1 = new HotkeyConfigItem
            {
                VirtualKeyCode = 0x4D,
                Modifiers = HotkeyModifier.Ctrl
            };
            var item2 = new HotkeyConfigItem
            {
                VirtualKeyCode = 0x4D,
                Modifiers = HotkeyModifier.Ctrl
            };

            // Assert
            Assert.True(item1.ConflictsWith(item2));
        }

        [Fact]
        public void ConflictsWith_DifferentKey_ReturnsFalse()
        {
            // Arrange
            var item1 = new HotkeyConfigItem
            {
                VirtualKeyCode = 0x4D,
                Modifiers = HotkeyModifier.Ctrl
            };
            var item2 = new HotkeyConfigItem
            {
                VirtualKeyCode = 0x4E, // Different key
                Modifiers = HotkeyModifier.Ctrl
            };

            // Assert
            Assert.False(item1.ConflictsWith(item2));
        }

        [Fact]
        public void ConflictsWith_DifferentModifiers_ReturnsFalse()
        {
            // Arrange
            var item1 = new HotkeyConfigItem
            {
                VirtualKeyCode = 0x4D,
                Modifiers = HotkeyModifier.Ctrl
            };
            var item2 = new HotkeyConfigItem
            {
                VirtualKeyCode = 0x4D,
                Modifiers = HotkeyModifier.Alt // Different modifier
            };

            // Assert
            Assert.False(item1.ConflictsWith(item2));
        }

        [Fact]
        public void ConflictsWith_SameItem_ReturnsFalse()
        {
            // Arrange
            var item = new HotkeyConfigItem
            {
                VirtualKeyCode = 0x4D,
                Modifiers = HotkeyModifier.Ctrl
            };

            // Assert
            Assert.False(item.ConflictsWith(item));
        }

        [Fact]
        public void ConflictsWith_Null_ReturnsFalse()
        {
            // Arrange
            var item = new HotkeyConfigItem();

            // Assert
            Assert.False(item.ConflictsWith(null!));
        }

        #endregion

        #region Equality Tests

        [Fact]
        public void Equals_SameId_ReturnsTrue()
        {
            // Arrange
            var item1 = new HotkeyConfigItem();
            var item2 = item1.Clone();

            // Assert
            Assert.True(item1.Equals(item2));
        }

        [Fact]
        public void Equals_DifferentId_ReturnsFalse()
        {
            // Arrange
            var item1 = new HotkeyConfigItem();
            var item2 = new HotkeyConfigItem();

            // Assert
            Assert.False(item1.Equals(item2));
        }

        [Fact]
        public void Equals_Null_ReturnsFalse()
        {
            // Arrange
            var item = new HotkeyConfigItem();

            // Assert
            Assert.False(item.Equals(null));
        }

        [Fact]
        public void GetHashCode_SameId_ReturnsSameHash()
        {
            // Arrange
            var item1 = new HotkeyConfigItem();
            var item2 = item1.Clone();

            // Assert
            Assert.Equal(item1.GetHashCode(), item2.GetHashCode());
        }

        #endregion

        #region ToString Tests

        [Fact]
        public void ToString_ReturnsFormattedString()
        {
            // Arrange
            var item = new HotkeyConfigItem
            {
                Name = "Mute App",
                VirtualKeyCode = 0x4D,
                Modifiers = HotkeyModifier.Ctrl | HotkeyModifier.Alt,
                ActionType = "ToggleMute"
            };

            // Act
            var result = item.ToString();

            // Assert
            Assert.Equal("Mute App: Ctrl+Alt+M -> ToggleMute", result);
        }

        #endregion
    }
}
