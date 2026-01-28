using WpfImageSource = System.Windows.Media.ImageSource;

namespace SystemTrayProcessManager.Core.Models
{
    /// <summary>
    /// Represents information about a running Windows process with a visible window.
    /// This is an immutable data transfer object used throughout the application.
    /// </summary>
    public sealed class ProcessInfo : IEquatable<ProcessInfo>
    {
        /// <summary>
        /// Gets the unique process identifier (PID).
        /// </summary>
        public int ProcessId { get; init; }

        /// <summary>
        /// Gets the process name (executable name without extension).
        /// </summary>
        public string Name { get; init; } = string.Empty;

        /// <summary>
        /// Gets the main window title, if available.
        /// </summary>
        public string? WindowTitle { get; init; }

        /// <summary>
        /// Gets the full path to the process executable, if accessible.
        /// </summary>
        public string? ExecutablePath { get; init; }

        /// <summary>
        /// Gets the handle to the main window of the process.
        /// </summary>
        public IntPtr WindowHandle { get; init; }

        /// <summary>
        /// Gets the cached icon for the process, if available.
        /// </summary>
        public WpfImageSource? Icon { get; init; }

        /// <summary>
        /// Gets the time when the process was started.
        /// </summary>
        public DateTime StartTime { get; init; }

        /// <summary>
        /// Gets a value indicating whether the process is responding to Windows messages.
        /// </summary>
        public bool IsResponding { get; init; }

        /// <summary>
        /// Gets a display-friendly name combining the process name and window title.
        /// </summary>
        public string DisplayName => string.IsNullOrWhiteSpace(WindowTitle) 
            ? Name 
            : $"{Name} - {WindowTitle}";

        /// <inheritdoc/>
        public bool Equals(ProcessInfo? other)
        {
            if (other is null)
            {
                return false;
            }

            return ProcessId == other.ProcessId;
        }

        /// <inheritdoc/>
        public override bool Equals(object? obj)
        {
            return obj is ProcessInfo other && Equals(other);
        }

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            return ProcessId.GetHashCode();
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            return $"ProcessInfo {{ PID={ProcessId}, Name={Name}, WindowTitle={WindowTitle ?? "(none)"} }}";
        }

        /// <summary>
        /// Determines whether two ProcessInfo instances are equal.
        /// </summary>
        public static bool operator ==(ProcessInfo? left, ProcessInfo? right)
        {
            if (left is null)
            {
                return right is null;
            }

            return left.Equals(right);
        }

        /// <summary>
        /// Determines whether two ProcessInfo instances are not equal.
        /// </summary>
        public static bool operator !=(ProcessInfo? left, ProcessInfo? right)
        {
            return !(left == right);
        }
    }
}
