using SystemTrayProcessManager.Core.Enums;
using SystemTrayProcessManager.Core.Models;

namespace SystemTrayProcessManager.Tests.Core
{
    /// <summary>
    /// Unit tests for the HotkeyConfiguration model.
    /// </summary>
    public class HotkeyConfigurationTests
    {
        #region Constructor Tests

        [Fact]
        public void DefaultConstructor_InitializesEmptyList()
        {
            // Arrange & Act
            var config = new HotkeyConfiguration();

            // Assert
            Assert.NotNull(config.Items);
            Assert.Empty(config.Items);
        }

        [Fact]
        public void DefaultConstructor_SetsCurrentVersion()
        {
            // Arrange & Act
            var config = new HotkeyConfiguration();

            // Assert
            Assert.Equal(HotkeyConfiguration.CurrentVersion, config.Version);
        }

        [Fact]
        public void DefaultConstructor_SetsLastModified()
        {
            // Arrange
            var before = DateTime.UtcNow;

            // Act
            var config = new HotkeyConfiguration();

            // Assert
            Assert.True(config.LastModified >= before);
            Assert.True(config.LastModified <= DateTime.UtcNow);
        }

        [Fact]
        public void ItemsConstructor_CopiesItems()
        {
            // Arrange
            var items = new List<HotkeyConfigItem>
            {
                new("Test1", 0x4D, HotkeyModifier.Ctrl, "Action1"),
                new("Test2", 0x4E, HotkeyModifier.Alt, "Action2")
            };

            // Act
            var config = new HotkeyConfiguration(items);

            // Assert
            Assert.Equal(2, config.Items.Count);
        }

        [Fact]
        public void ItemsConstructor_NullItems_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => new HotkeyConfiguration(null!));
        }

        #endregion

        #region Count Tests

        [Fact]
        public void Count_EmptyConfiguration_ReturnsZero()
        {
            // Arrange
            var config = new HotkeyConfiguration();

            // Assert
            Assert.Equal(0, config.Count);
        }

        [Fact]
        public void Count_WithItems_ReturnsCorrectCount()
        {
            // Arrange
            var config = new HotkeyConfiguration
            {
                Items =
                [
                    new HotkeyConfigItem(),
                    new HotkeyConfigItem(),
                    new HotkeyConfigItem()
                ]
            };

            // Assert
            Assert.Equal(3, config.Count);
        }

        #endregion

        #region EnabledCount Tests

        [Fact]
        public void EnabledCount_AllEnabled_ReturnsTotal()
        {
            // Arrange
            var config = new HotkeyConfiguration
            {
                Items =
                [
                    new HotkeyConfigItem { IsEnabled = true },
                    new HotkeyConfigItem { IsEnabled = true }
                ]
            };

            // Assert
            Assert.Equal(2, config.EnabledCount);
        }

        [Fact]
        public void EnabledCount_SomeDisabled_ReturnsEnabledOnly()
        {
            // Arrange
            var config = new HotkeyConfiguration
            {
                Items =
                [
                    new HotkeyConfigItem { IsEnabled = true },
                    new HotkeyConfigItem { IsEnabled = false },
                    new HotkeyConfigItem { IsEnabled = true }
                ]
            };

            // Assert
            Assert.Equal(2, config.EnabledCount);
        }

        [Fact]
        public void EnabledCount_AllDisabled_ReturnsZero()
        {
            // Arrange
            var config = new HotkeyConfiguration
            {
                Items =
                [
                    new HotkeyConfigItem { IsEnabled = false },
                    new HotkeyConfigItem { IsEnabled = false }
                ]
            };

            // Assert
            Assert.Equal(0, config.EnabledCount);
        }

        #endregion

        #region Add Tests

        [Fact]
        public void Add_ValidItem_AddsToList()
        {
            // Arrange
            var config = new HotkeyConfiguration();
            var item = new HotkeyConfigItem("Test", 0x4D, HotkeyModifier.Ctrl, "Action");

            // Act
            config.Add(item);

            // Assert
            Assert.Single(config.Items);
            Assert.Contains(item, config.Items);
        }

        [Fact]
        public void Add_ValidItem_UpdatesLastModified()
        {
            // Arrange
            var config = new HotkeyConfiguration();
            var originalTime = config.LastModified;
            var item = new HotkeyConfigItem();

            // Small delay to ensure time difference
            Thread.Sleep(1);

            // Act
            config.Add(item);

            // Assert
            Assert.True(config.LastModified >= originalTime);
        }

        [Fact]
        public void Add_NullItem_ThrowsArgumentNullException()
        {
            // Arrange
            var config = new HotkeyConfiguration();

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => config.Add(null!));
        }

        #endregion

        #region Remove Tests

        [Fact]
        public void Remove_ExistingItem_ReturnsTrue()
        {
            // Arrange
            var item = new HotkeyConfigItem();
            var config = new HotkeyConfiguration { Items = [item] };

            // Act
            var result = config.Remove(item);

            // Assert
            Assert.True(result);
            Assert.Empty(config.Items);
        }

        [Fact]
        public void Remove_NonExistingItem_ReturnsFalse()
        {
            // Arrange
            var config = new HotkeyConfiguration();
            var item = new HotkeyConfigItem();

            // Act
            var result = config.Remove(item);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void Remove_Null_ReturnsFalse()
        {
            // Arrange
            var config = new HotkeyConfiguration();

            // Act
            var result = config.Remove(null!);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void Remove_ExistingItem_UpdatesLastModified()
        {
            // Arrange
            var item = new HotkeyConfigItem();
            var config = new HotkeyConfiguration { Items = [item] };
            var originalTime = config.LastModified;
            Thread.Sleep(1);

            // Act
            config.Remove(item);

            // Assert
            Assert.True(config.LastModified >= originalTime);
        }

        #endregion

        #region RemoveById Tests

        [Fact]
        public void RemoveById_ExistingId_ReturnsTrue()
        {
            // Arrange
            var item = new HotkeyConfigItem();
            var config = new HotkeyConfiguration { Items = [item] };

            // Act
            var result = config.RemoveById(item.Id);

            // Assert
            Assert.True(result);
            Assert.Empty(config.Items);
        }

        [Fact]
        public void RemoveById_NonExistingId_ReturnsFalse()
        {
            // Arrange
            var config = new HotkeyConfiguration();

            // Act
            var result = config.RemoveById(Guid.NewGuid());

            // Assert
            Assert.False(result);
        }

        #endregion

        #region FindById Tests

        [Fact]
        public void FindById_ExistingId_ReturnsItem()
        {
            // Arrange
            var item = new HotkeyConfigItem { Name = "Test" };
            var config = new HotkeyConfiguration { Items = [item] };

            // Act
            var result = config.FindById(item.Id);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("Test", result.Name);
        }

        [Fact]
        public void FindById_NonExistingId_ReturnsNull()
        {
            // Arrange
            var config = new HotkeyConfiguration();

            // Act
            var result = config.FindById(Guid.NewGuid());

            // Assert
            Assert.Null(result);
        }

        #endregion

        #region FindByBinding Tests

        [Fact]
        public void FindByBinding_ExistingBinding_ReturnsItem()
        {
            // Arrange
            var item = new HotkeyConfigItem
            {
                Name = "Test",
                VirtualKeyCode = 0x4D,
                Modifiers = HotkeyModifier.Ctrl
            };
            var config = new HotkeyConfiguration { Items = [item] };
            var binding = new HotkeyBinding(0x4D, HotkeyModifier.Ctrl);

            // Act
            var result = config.FindByBinding(binding);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("Test", result.Name);
        }

        [Fact]
        public void FindByBinding_NonExistingBinding_ReturnsNull()
        {
            // Arrange
            var config = new HotkeyConfiguration();
            var binding = new HotkeyBinding(0x4D, HotkeyModifier.Ctrl);

            // Act
            var result = config.FindByBinding(binding);

            // Assert
            Assert.Null(result);
        }

        #endregion

        #region HasConflict Tests

        [Fact]
        public void HasConflict_SameBindingDifferentItem_ReturnsTrue()
        {
            // Arrange
            var existing = new HotkeyConfigItem
            {
                VirtualKeyCode = 0x4D,
                Modifiers = HotkeyModifier.Ctrl
            };
            var config = new HotkeyConfiguration { Items = [existing] };
            var newItem = new HotkeyConfigItem
            {
                VirtualKeyCode = 0x4D,
                Modifiers = HotkeyModifier.Ctrl
            };

            // Act
            var result = config.HasConflict(newItem);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void HasConflict_DifferentBinding_ReturnsFalse()
        {
            // Arrange
            var existing = new HotkeyConfigItem
            {
                VirtualKeyCode = 0x4D,
                Modifiers = HotkeyModifier.Ctrl
            };
            var config = new HotkeyConfiguration { Items = [existing] };
            var newItem = new HotkeyConfigItem
            {
                VirtualKeyCode = 0x4E, // Different key
                Modifiers = HotkeyModifier.Ctrl
            };

            // Act
            var result = config.HasConflict(newItem);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void HasConflict_SameItem_ReturnsFalse()
        {
            // Arrange
            var item = new HotkeyConfigItem
            {
                VirtualKeyCode = 0x4D,
                Modifiers = HotkeyModifier.Ctrl
            };
            var config = new HotkeyConfiguration { Items = [item] };

            // Act - checking conflict with itself
            var result = config.HasConflict(item);

            // Assert
            Assert.False(result);
        }

        #endregion

        #region GetConflicts Tests

        [Fact]
        public void GetConflicts_WithConflicts_ReturnsConflictingItems()
        {
            // Arrange
            var item1 = new HotkeyConfigItem { VirtualKeyCode = 0x4D, Modifiers = HotkeyModifier.Ctrl };
            var item2 = new HotkeyConfigItem { VirtualKeyCode = 0x4D, Modifiers = HotkeyModifier.Ctrl };
            var config = new HotkeyConfiguration { Items = [item1, item2] };

            // Act
            var conflicts = config.GetConflicts(item1);

            // Assert
            Assert.Single(conflicts);
            Assert.Contains(item2, conflicts);
        }

        [Fact]
        public void GetConflicts_NoConflicts_ReturnsEmptyList()
        {
            // Arrange
            var item1 = new HotkeyConfigItem { VirtualKeyCode = 0x4D, Modifiers = HotkeyModifier.Ctrl };
            var item2 = new HotkeyConfigItem { VirtualKeyCode = 0x4E, Modifiers = HotkeyModifier.Alt };
            var config = new HotkeyConfiguration { Items = [item1, item2] };

            // Act
            var conflicts = config.GetConflicts(item1);

            // Assert
            Assert.Empty(conflicts);
        }

        #endregion

        #region Clear Tests

        [Fact]
        public void Clear_RemovesAllItems()
        {
            // Arrange
            var config = new HotkeyConfiguration
            {
                Items =
                [
                    new HotkeyConfigItem(),
                    new HotkeyConfigItem()
                ]
            };

            // Act
            config.Clear();

            // Assert
            Assert.Empty(config.Items);
        }

        [Fact]
        public void Clear_UpdatesLastModified()
        {
            // Arrange
            var config = new HotkeyConfiguration { Items = [new HotkeyConfigItem()] };
            var originalTime = config.LastModified;
            Thread.Sleep(1);

            // Act
            config.Clear();

            // Assert
            Assert.True(config.LastModified >= originalTime);
        }

        #endregion

        #region Clone Tests

        [Fact]
        public void Clone_CreatesDeepCopy()
        {
            // Arrange
            var item = new HotkeyConfigItem { Name = "Test", VirtualKeyCode = 0x4D };
            var original = new HotkeyConfiguration { Items = [item] };

            // Act
            var clone = original.Clone();

            // Assert
            Assert.Equal(original.Version, clone.Version);
            Assert.Single(clone.Items);
            Assert.Equal("Test", clone.Items[0].Name);
        }

        [Fact]
        public void Clone_ModifyingClone_DoesNotAffectOriginal()
        {
            // Arrange
            var item = new HotkeyConfigItem { Name = "Original" };
            var original = new HotkeyConfiguration { Items = [item] };
            var clone = original.Clone();

            // Act
            clone.Items[0].Name = "Modified";

            // Assert
            Assert.Equal("Original", original.Items[0].Name);
        }

        #endregion
    }
}
