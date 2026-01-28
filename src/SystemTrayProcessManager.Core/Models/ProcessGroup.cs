using System.Text.RegularExpressions;

namespace SystemTrayProcessManager.Core.Models
{
    /// <summary>
    /// Represents a group of processes that can be targeted for batch operations.
    /// Processes are matched by name patterns supporting wildcard syntax (* and ?).
    /// </summary>
    public sealed class ProcessGroup : IEquatable<ProcessGroup>
    {
        /// <summary>
        /// Gets or sets the unique identifier for this process group.
        /// </summary>
        public Guid Id { get; init; } = Guid.NewGuid();

        /// <summary>
        /// Gets or sets the name of this process group.
        /// </summary>
        public string Name { get; init; } = string.Empty;

        /// <summary>
        /// Gets or sets an optional description of this group.
        /// </summary>
        public string Description { get; init; } = string.Empty;

        /// <summary>
        /// Gets or sets the list of process name patterns.
        /// Supports wildcards: * (any characters) and ? (single character).
        /// </summary>
        public List<string> ProcessPatterns { get; init; } = [];

        /// <summary>
        /// Gets or sets the timestamp when this group was created.
        /// </summary>
        public DateTime CreatedAt { get; init; } = DateTime.UtcNow;

        /// <summary>
        /// Gets or sets the timestamp when this group was last modified.
        /// </summary>
        public DateTime LastModified { get; init; } = DateTime.UtcNow;

        /// <summary>
        /// Gets a value indicating whether this group has any patterns defined.
        /// </summary>
        public bool HasPatterns => ProcessPatterns.Count > 0;

        /// <summary>
        /// Gets a value indicating whether this group configuration is valid.
        /// </summary>
        public bool IsValid => !string.IsNullOrWhiteSpace(Name) && HasPatterns;

        /// <summary>
        /// Gets the display string for the patterns in this group.
        /// </summary>
        public string PatternsDisplay => ProcessPatterns.Count > 0 
            ? string.Join(", ", ProcessPatterns.Take(3)) + (ProcessPatterns.Count > 3 ? $" (+{ProcessPatterns.Count - 3} more)" : "")
            : "(no patterns)";

        /// <summary>
        /// Determines whether a process name matches any pattern in this group.
        /// </summary>
        /// <param name="processName">The process name to match.</param>
        /// <returns>True if the process name matches any pattern; otherwise, false.</returns>
        public bool MatchesProcess(string processName)
        {
            if (string.IsNullOrWhiteSpace(processName))
                return false;

            foreach (var pattern in ProcessPatterns)
            {
                if (MatchesPattern(processName, pattern))
                    return true;
            }

            return false;
        }

        /// <summary>
        /// Determines whether a process name matches a wildcard pattern.
        /// </summary>
        /// <param name="processName">The process name to match.</param>
        /// <param name="pattern">The pattern with optional wildcards (* and ?).</param>
        /// <returns>True if the name matches the pattern; otherwise, false.</returns>
        private static bool MatchesPattern(string processName, string pattern)
        {
            if (string.IsNullOrWhiteSpace(pattern))
                return false;

            // Exact match (case-insensitive)
            if (!pattern.Contains('*') && !pattern.Contains('?'))
            {
                return processName.Equals(pattern, StringComparison.OrdinalIgnoreCase);
            }

            // Convert wildcard pattern to regex
            var regexPattern = "^" + Regex.Escape(pattern)
                .Replace("\\*", ".*")
                .Replace("\\?", ".") + "$";

            try
            {
                return Regex.IsMatch(processName, regexPattern, 
                    RegexOptions.IgnoreCase | RegexOptions.Singleline,
                    TimeSpan.FromMilliseconds(100));
            }
            catch (RegexMatchTimeoutException)
            {
                return false;
            }
        }

        /// <summary>
        /// Creates a copy with an additional pattern added.
        /// </summary>
        /// <param name="pattern">The pattern to add.</param>
        /// <returns>A new ProcessGroup with the pattern added.</returns>
        public ProcessGroup WithAddedPattern(string pattern)
        {
            var patterns = new List<string>(ProcessPatterns);
            if (!string.IsNullOrWhiteSpace(pattern) && !patterns.Contains(pattern, StringComparer.OrdinalIgnoreCase))
            {
                patterns.Add(pattern);
            }

            return new ProcessGroup
            {
                Id = Id,
                Name = Name,
                Description = Description,
                ProcessPatterns = patterns,
                CreatedAt = CreatedAt,
                LastModified = DateTime.UtcNow
            };
        }

        /// <summary>
        /// Creates a copy with a pattern removed.
        /// </summary>
        /// <param name="pattern">The pattern to remove.</param>
        /// <returns>A new ProcessGroup with the pattern removed.</returns>
        public ProcessGroup WithRemovedPattern(string pattern)
        {
            var patterns = ProcessPatterns
                .Where(p => !p.Equals(pattern, StringComparison.OrdinalIgnoreCase))
                .ToList();

            return new ProcessGroup
            {
                Id = Id,
                Name = Name,
                Description = Description,
                ProcessPatterns = patterns,
                CreatedAt = CreatedAt,
                LastModified = DateTime.UtcNow
            };
        }

        /// <inheritdoc/>
        public bool Equals(ProcessGroup? other)
        {
            if (other is null) return false;
            return Id == other.Id;
        }

        /// <inheritdoc/>
        public override bool Equals(object? obj) => obj is ProcessGroup other && Equals(other);

        /// <inheritdoc/>
        public override int GetHashCode() => Id.GetHashCode();

        /// <inheritdoc/>
        public override string ToString()
        {
            return $"ProcessGroup {{ Id={Id}, Name={Name}, Patterns=[{string.Join(", ", ProcessPatterns)}] }}";
        }

        /// <summary>
        /// Determines whether two ProcessGroup instances are equal.
        /// </summary>
        public static bool operator ==(ProcessGroup? left, ProcessGroup? right)
        {
            if (left is null) return right is null;
            return left.Equals(right);
        }

        /// <summary>
        /// Determines whether two ProcessGroup instances are not equal.
        /// </summary>
        public static bool operator !=(ProcessGroup? left, ProcessGroup? right) => !(left == right);
    }
}
