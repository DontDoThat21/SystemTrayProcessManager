using System.Text.Json.Serialization;

namespace SystemTrayProcessManager.Core.Models
{
    /// <summary>
    /// Represents the unified application settings persisted to disk.
    /// Contains all user-configurable settings organized by category.
    /// </summary>
    public sealed class AppSettings
    {
        /// <summary>
        /// The current settings file format version for migration support.
        /// </summary>
        public const string CurrentVersion = "1.0";

        #region Metadata

        /// <summary>
        /// Gets or sets the settings file format version.
        /// </summary>
        public string Version { get; set; } = CurrentVersion;

        /// <summary>
        /// Gets or sets the date and time when settings were last modified.
        /// </summary>
        public DateTime LastModified { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Gets or sets a value indicating whether this is the first time the application runs.
        /// </summary>
        public bool IsFirstRun { get; set; } = true;

        #endregion

        #region Hotkeys

        /// <summary>
        /// Gets or sets a value indicating whether global hotkeys are enabled.
        /// </summary>
        public bool HotkeysEnabled { get; set; } = true;

        #endregion

        #region Audio Defaults

        /// <summary>
        /// Gets or sets the default volume level for new audio sessions (0.0 to 1.0).
        /// </summary>
        public float DefaultVolume { get; set; } = 1.0f;

        /// <summary>
        /// Gets or sets a value indicating whether to auto-mute processes when minimized.
        /// </summary>
        public bool MuteOnMinimize { get; set; }

        #endregion

        #region Behavior

        /// <summary>
        /// Gets or sets a value indicating whether the application should start with Windows.
        /// </summary>
        public bool StartWithWindows { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the application should start minimized to tray.
        /// </summary>
        public bool StartMinimized { get; set; } = true;

        /// <summary>
        /// Gets or sets a value indicating whether system tray notifications are enabled.
        /// </summary>
        public bool EnableNotifications { get; set; } = true;

        /// <summary>
        /// Gets or sets a value indicating whether minimizing the window sends it to the system tray.
        /// </summary>
        public bool MinimizeToTray { get; set; } = true;

        /// <summary>
        /// Gets or sets the process refresh interval in milliseconds (1000-30000).
        /// </summary>
        public int ProcessRefreshIntervalMs { get; set; } = 5000;

        #endregion

        #region Appearance

        /// <summary>
        /// Gets or sets the selected theme name. Supported values: "Dark", "Light".
        /// </summary>
        public string Theme { get; set; } = "Dark";

        /// <summary>
        /// Gets or sets a value indicating whether process icons are shown in the UI.
        /// </summary>
        public bool ShowProcessIcons { get; set; } = true;

        /// <summary>
        /// Gets or sets a value indicating whether UI animations are enabled.
        /// </summary>
        public bool AnimationsEnabled { get; set; } = true;

        #endregion

        #region Computed Properties

        /// <summary>
        /// Gets a value indicating whether the settings are valid.
        /// </summary>
        [JsonIgnore]
        public bool IsValid =>
            !string.IsNullOrWhiteSpace(Version) &&
            DefaultVolume >= 0f && DefaultVolume <= 1f &&
            ProcessRefreshIntervalMs >= 1000 && ProcessRefreshIntervalMs <= 30000 &&
            !string.IsNullOrWhiteSpace(Theme);

        #endregion

        /// <summary>
        /// Creates a default application settings instance.
        /// </summary>
        /// <returns>A new <see cref="AppSettings"/> with default values.</returns>
        public static AppSettings Default => new();

        /// <summary>
        /// Creates a deep copy of this settings instance.
        /// </summary>
        /// <returns>A new <see cref="AppSettings"/> with identical values.</returns>
        public AppSettings Clone()
        {
            return new AppSettings
            {
                Version = Version,
                LastModified = LastModified,
                IsFirstRun = IsFirstRun,
                HotkeysEnabled = HotkeysEnabled,
                DefaultVolume = DefaultVolume,
                MuteOnMinimize = MuteOnMinimize,
                StartWithWindows = StartWithWindows,
                StartMinimized = StartMinimized,
                EnableNotifications = EnableNotifications,
                MinimizeToTray = MinimizeToTray,
                ProcessRefreshIntervalMs = ProcessRefreshIntervalMs,
                Theme = Theme,
                ShowProcessIcons = ShowProcessIcons,
                AnimationsEnabled = AnimationsEnabled
            };
        }

        /// <summary>
        /// Validates settings values and clamps them to acceptable ranges.
        /// </summary>
        /// <returns>A new <see cref="AppSettings"/> with values clamped to valid ranges.</returns>
        public AppSettings Sanitize()
        {
            var sanitized = Clone();
            sanitized.DefaultVolume = Math.Clamp(sanitized.DefaultVolume, 0f, 1f);
            sanitized.ProcessRefreshIntervalMs = Math.Clamp(sanitized.ProcessRefreshIntervalMs, 1000, 30000);

            if (string.IsNullOrWhiteSpace(sanitized.Theme))
            {
                sanitized.Theme = "Dark";
            }

            if (string.IsNullOrWhiteSpace(sanitized.Version))
            {
                sanitized.Version = CurrentVersion;
            }

            return sanitized;
        }

        /// <summary>
        /// Validates the settings and returns any validation errors.
        /// </summary>
        /// <returns>A collection of validation error messages.</returns>
        public IEnumerable<string> Validate()
        {
            var errors = new List<string>();

            if (string.IsNullOrWhiteSpace(Version))
            {
                errors.Add("Version is required.");
            }

            if (DefaultVolume < 0f || DefaultVolume > 1f)
            {
                errors.Add($"DefaultVolume must be between 0.0 and 1.0, was {DefaultVolume}.");
            }

            if (ProcessRefreshIntervalMs < 1000 || ProcessRefreshIntervalMs > 30000)
            {
                errors.Add($"ProcessRefreshIntervalMs must be between 1000 and 30000, was {ProcessRefreshIntervalMs}.");
            }

            if (string.IsNullOrWhiteSpace(Theme))
            {
                errors.Add("Theme is required.");
            }
            else if (Theme != "Dark" && Theme != "Light")
            {
                errors.Add($"Theme must be 'Dark' or 'Light', was '{Theme}'.");
            }

            return errors;
        }

        /// <summary>
        /// Determines whether this settings instance is equal to another.
        /// </summary>
        /// <param name="other">The other settings to compare.</param>
        /// <returns>True if all settings values are equal; otherwise, false.</returns>
        public bool Equals(AppSettings? other)
        {
            if (other is null) return false;
            if (ReferenceEquals(this, other)) return true;

            return Version == other.Version &&
                   IsFirstRun == other.IsFirstRun &&
                   HotkeysEnabled == other.HotkeysEnabled &&
                   Math.Abs(DefaultVolume - other.DefaultVolume) < 0.001f &&
                   MuteOnMinimize == other.MuteOnMinimize &&
                   StartWithWindows == other.StartWithWindows &&
                   StartMinimized == other.StartMinimized &&
                   EnableNotifications == other.EnableNotifications &&
                   MinimizeToTray == other.MinimizeToTray &&
                   ProcessRefreshIntervalMs == other.ProcessRefreshIntervalMs &&
                   Theme == other.Theme &&
                   ShowProcessIcons == other.ShowProcessIcons &&
                   AnimationsEnabled == other.AnimationsEnabled;
        }

        /// <inheritdoc/>
        public override bool Equals(object? obj)
        {
            return Equals(obj as AppSettings);
        }

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            var hash = new HashCode();
            hash.Add(Version);
            hash.Add(IsFirstRun);
            hash.Add(HotkeysEnabled);
            hash.Add(DefaultVolume);
            hash.Add(MuteOnMinimize);
            hash.Add(StartWithWindows);
            hash.Add(StartMinimized);
            hash.Add(EnableNotifications);
            hash.Add(MinimizeToTray);
            hash.Add(ProcessRefreshIntervalMs);
            hash.Add(Theme);
            hash.Add(ShowProcessIcons);
            hash.Add(AnimationsEnabled);
            return hash.ToHashCode();
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            return $"AppSettings v{Version}: Theme={Theme}, Hotkeys={HotkeysEnabled}, Notifications={EnableNotifications}, FirstRun={IsFirstRun}";
        }
    }
}
