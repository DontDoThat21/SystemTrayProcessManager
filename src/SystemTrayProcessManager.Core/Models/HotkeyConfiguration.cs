using System.Text.Json.Serialization;

namespace SystemTrayProcessManager.Core.Models
{
    /// <summary>
    /// Represents the root configuration for hotkey settings, used for persistence.
    /// Contains all hotkey configuration items and metadata for versioning.
    /// </summary>
    public class HotkeyConfiguration
    {
        /// <summary>
        /// The current configuration file format version.
        /// </summary>
        public const string CurrentVersion = "1.0";

        /// <summary>
        /// Gets or sets the configuration file format version for migration support.
        /// </summary>
        public string Version { get; set; } = CurrentVersion;

        /// <summary>
        /// Gets or sets the date and time when the configuration was last modified.
        /// </summary>
        public DateTime LastModified { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Gets or sets the collection of hotkey configuration items.
        /// </summary>
        public List<HotkeyConfigItem> Items { get; set; } = [];

        /// <summary>
        /// Initializes a new instance of the <see cref="HotkeyConfiguration"/> class.
        /// </summary>
        public HotkeyConfiguration()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="HotkeyConfiguration"/> class
        /// with the specified items.
        /// </summary>
        /// <param name="items">The hotkey configuration items.</param>
        public HotkeyConfiguration(IEnumerable<HotkeyConfigItem> items)
        {
            ArgumentNullException.ThrowIfNull(items);
            Items = [.. items];
            LastModified = DateTime.UtcNow;
        }

        /// <summary>
        /// Gets the number of hotkey configuration items.
        /// </summary>
        [JsonIgnore]
        public int Count => Items.Count;

        /// <summary>
        /// Gets the number of enabled hotkey configuration items.
        /// </summary>
        [JsonIgnore]
        public int EnabledCount => Items.Count(i => i.IsEnabled);

        /// <summary>
        /// Adds a hotkey configuration item to the collection.
        /// </summary>
        /// <param name="item">The item to add.</param>
        /// <exception cref="ArgumentNullException">Thrown when item is null.</exception>
        public void Add(HotkeyConfigItem item)
        {
            ArgumentNullException.ThrowIfNull(item);
            Items.Add(item);
            LastModified = DateTime.UtcNow;
        }

        /// <summary>
        /// Removes a hotkey configuration item from the collection.
        /// </summary>
        /// <param name="item">The item to remove.</param>
        /// <returns>True if the item was removed; otherwise, false.</returns>
        public bool Remove(HotkeyConfigItem item)
        {
            if (item == null) return false;

            bool removed = Items.Remove(item);
            if (removed)
            {
                LastModified = DateTime.UtcNow;
            }

            return removed;
        }

        /// <summary>
        /// Removes a hotkey configuration item by its ID.
        /// </summary>
        /// <param name="id">The ID of the item to remove.</param>
        /// <returns>True if the item was removed; otherwise, false.</returns>
        public bool RemoveById(Guid id)
        {
            var item = Items.FirstOrDefault(i => i.Id == id);
            return item != null && Remove(item);
        }

        /// <summary>
        /// Finds a hotkey configuration item by its ID.
        /// </summary>
        /// <param name="id">The ID to search for.</param>
        /// <returns>The item if found; otherwise, null.</returns>
        public HotkeyConfigItem? FindById(Guid id)
        {
            return Items.FirstOrDefault(i => i.Id == id);
        }

        /// <summary>
        /// Finds a hotkey configuration item by its key binding.
        /// </summary>
        /// <param name="binding">The binding to search for.</param>
        /// <returns>The item if found; otherwise, null.</returns>
        public HotkeyConfigItem? FindByBinding(HotkeyBinding binding)
        {
            return Items.FirstOrDefault(i =>
                i.VirtualKeyCode == binding.VirtualKeyCode &&
                i.Modifiers == binding.Modifiers);
        }

        /// <summary>
        /// Determines whether the configuration contains a conflicting binding.
        /// </summary>
        /// <param name="item">The item to check for conflicts.</param>
        /// <returns>True if a conflict exists; otherwise, false.</returns>
        public bool HasConflict(HotkeyConfigItem item)
        {
            if (item == null) return false;

            return Items.Any(i => i.ConflictsWith(item));
        }

        /// <summary>
        /// Gets all conflicting items for a given item.
        /// </summary>
        /// <param name="item">The item to check for conflicts.</param>
        /// <returns>A collection of conflicting items.</returns>
        public IEnumerable<HotkeyConfigItem> GetConflicts(HotkeyConfigItem item)
        {
            if (item == null) return [];

            return Items.Where(i => i.ConflictsWith(item));
        }

        /// <summary>
        /// Clears all hotkey configuration items.
        /// </summary>
        public void Clear()
        {
            Items.Clear();
            LastModified = DateTime.UtcNow;
        }

        /// <summary>
        /// Creates a deep copy of this configuration.
        /// </summary>
        /// <returns>A new <see cref="HotkeyConfiguration"/> with cloned items.</returns>
        public HotkeyConfiguration Clone()
        {
            return new HotkeyConfiguration
            {
                Version = Version,
                LastModified = LastModified,
                Items = Items.Select(i => i.Clone()).ToList()
            };
        }

        /// <summary>
        /// Validates the configuration and returns any validation errors.
        /// </summary>
        /// <returns>A collection of validation error messages.</returns>
        public IEnumerable<string> Validate()
        {
            var errors = new List<string>();

            // Check for empty configuration
            if (Items.Count == 0)
            {
                errors.Add("Configuration contains no hotkey items.");
            }

            // Check for invalid items
            foreach (var item in Items)
            {
                if (!item.IsValid)
                {
                    errors.Add($"Item '{item.Name}' is invalid: missing required fields.");
                }
            }

            // Check for duplicate IDs
            var duplicateIds = Items
                .GroupBy(i => i.Id)
                .Where(g => g.Count() > 1)
                .Select(g => g.Key);

            foreach (var id in duplicateIds)
            {
                errors.Add($"Duplicate item ID found: {id}");
            }

            // Check for conflicting bindings
            for (int i = 0; i < Items.Count; i++)
            {
                for (int j = i + 1; j < Items.Count; j++)
                {
                    if (Items[i].ConflictsWith(Items[j]))
                    {
                        errors.Add($"Conflicting hotkey bindings: '{Items[i].Name}' and '{Items[j].Name}' both use {Items[i].DisplayString}");
                    }
                }
            }

            return errors;
        }

        /// <summary>
        /// Returns a string representation of the configuration.
        /// </summary>
        public override string ToString()
        {
            return $"HotkeyConfiguration v{Version}: {Count} items ({EnabledCount} enabled)";
        }
    }
}
